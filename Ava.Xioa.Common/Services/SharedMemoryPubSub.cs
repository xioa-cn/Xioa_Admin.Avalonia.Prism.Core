using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Services;

public class SharedMemoryPubSub : ISharedMemoryPubSub
{
    #region 常量定义

    // 内存布局相关常量
    private const int MAX_MESSAGES = 100; // 最大消息数量
    private const int MESSAGE_SIZE = 1040; // 单条消息大小：4(ID) + 4(Topic) + 8(Timestamp) + 1024(Data)
    private const int HEADER_SIZE = 16; // 头部信息大小：4(WritePos) + 4(ReadPos) + 4(MsgCount) + 4(CurrentMsgId)
    private const int MESSAGE_TIMEOUT_MS = 5000; // 消息过期时间：5秒

    #endregion

    #region 私有字段

    private readonly string _name; // 共享内存名称
    private readonly MemoryMappedFile _mmf; // 内存映射文件
    private readonly MemoryMappedViewAccessor _accessor; // 内存访问器
    private readonly EventWaitHandle _messageEvent; // 消息通知事件
    private readonly PeriodicTimer _cleanupTimer; // 清理定时器
    private readonly Task? _cleanupTask; // 清理任务
    private bool _disposed; // 释放标记

    // 订阅者管理
    // 结构：Dictionary<主题ID, Dictionary<订阅ID, 订阅信息>>
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, SubscriptionInfo>> _subscribers
        = new ConcurrentDictionary<int, ConcurrentDictionary<Guid, SubscriptionInfo>>();

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化共享内存发布订阅系统
    /// </summary>
    /// <param name="name">共享内存名称，相同名称的实例共享同一块内存</param>
    public SharedMemoryPubSub(string name)
    {
        _name = name;
        var totalSize = HEADER_SIZE + (MAX_MESSAGES * MESSAGE_SIZE);

        // 创建或打开共享内存
        _mmf = MemoryMappedFile.CreateOrOpen(name, totalSize);
        _accessor = _mmf.CreateViewAccessor();


        string eventName;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows 可加 Global\ 前缀（跨会话访问）
            eventName = $"Global\\{name}_Event";
        }
        else
        {
            // Linux/macOS 无需前缀，直接用唯一名称
            eventName = $"{name}_Event";
        }

        _messageEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

        // 创建或打开全局命名事件，用于消息通知
        _messageEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

        // 初始化头部信息（仅在首次创建时执行）
        if (_accessor.ReadInt32(0) == 0)
        {
            _accessor.Write(0, HEADER_SIZE); // WritePos：写入位置
            _accessor.Write(4, HEADER_SIZE); // ReadPos：读取位置
            _accessor.Write(8, 0); // MsgCount：消息数量
            _accessor.Write(12, 0); // CurrentMsgId：当前消息ID
        }

        // 启动清理任务
        _cleanupTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _cleanupTask = StartCleanupTask();
    }

    #endregion

    #region 发布订阅方法

    /// <summary>
    /// 发布消息到指定主题
    /// </summary>
    /// <param name="topicId">主题ID</param>
    /// <param name="data">消息数据（最大1024字节）</param>
    public void Publish(int topicId, byte[] data)
    {
        if (data == null || data.Length > 1024)
            throw new ArgumentException("数据长度无效");

        // 读取当前内存状态
        int writePos = _accessor.ReadInt32(0); // 当前写入位置
        int msgCount = _accessor.ReadInt32(8); // 当前消息数量
        int currentMsgId = _accessor.ReadInt32(12); // 当前消息ID

        // 检查缓冲区是否已满
        if (msgCount >= MAX_MESSAGES)
        {
            throw new InvalidOperationException("缓冲区已满");
        }

        // 写入消息内容
        _accessor.Write(writePos, currentMsgId + 1); // 消息ID
        _accessor.Write(writePos + 4, topicId); // 主题ID
        _accessor.Write(writePos + 8, DateTime.UtcNow.Ticks); // 时间戳
        _accessor.WriteArray(writePos + 16, data, 0, data.Length); // 消息数据

        // 计算下一个写入位置（循环缓冲区）
        int nextWritePos = (writePos + MESSAGE_SIZE) % (HEADER_SIZE + (MAX_MESSAGES * MESSAGE_SIZE));
        if (nextWritePos < HEADER_SIZE) nextWritePos = HEADER_SIZE;

        // 更新头部信息
        _accessor.Write(0, nextWritePos); // 更新写入位置
        _accessor.Write(8, msgCount + 1); // 更新消息数量
        _accessor.Write(12, currentMsgId + 1); // 更新消息ID
    }

    /// <summary>
    /// 订阅指定主题的消息
    /// </summary>
    /// <param name="topicId">主题ID</param>
    /// <param name="handler">消息处理函数</param>
    /// <returns>订阅对象，可通过Dispose取消订阅</returns>
    public IDisposable Subscribe(int topicId, Action<(int MessageId, int TopicId, byte[] Data)> handler)
    {
        var subscriptionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        var subscriptionInfo = new SubscriptionInfo
        {
            Id = subscriptionId,
            Handler = handler,
            Cts = cts
        };

        // 获取或创建主题的订阅者集合
        var topicSubscribers = _subscribers.GetOrAdd(topicId,
            _ => new ConcurrentDictionary<Guid, SubscriptionInfo>());

        // 添加订阅者
        topicSubscribers.TryAdd(subscriptionId, subscriptionInfo);

        Debug.WriteLine($"[{_name}] 添加订阅: Topic={topicId}, SubscriberId={subscriptionId}");

        // 启动消息监听任务
        StartSubscriptionTask(topicId, subscriptionInfo);

        return new Subscription(this, topicId, subscriptionId);
    }

    /// <summary>
    /// 取消指定主题的所有订阅
    /// </summary>
    /// <param name="topicId">主题ID</param>
    public void Unsubscribe(int topicId)
    {
        if (_subscribers.TryRemove(topicId, out var topicSubscribers))
        {
            foreach (var subscriber in topicSubscribers.Values)
            {
                subscriber.Cts.Cancel();
                subscriber.Cts.Dispose();
            }

            Debug.WriteLine($"[{_name}] 已取消主题 {topicId} 的所有订阅");
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 清理过期消息的任务
    /// </summary>
    private async Task StartCleanupTask()
    {
        while (await _cleanupTimer.WaitForNextTickAsync())
        {
            try
            {
                int readPos = _accessor.ReadInt32(4);
                int msgCount = _accessor.ReadInt32(8);

                if (msgCount == 0) continue;

                int currentPos = readPos;
                int cleanupCount = 0;
                long currentTime = DateTime.UtcNow.Ticks;
                bool isBufferAlmostFull = msgCount >= MAX_MESSAGES * 0.8; // 缓冲区使用超过80%

                // 遍历消息，检查是否需要清理
                for (int i = 0; i < msgCount; i++)
                {
                    long messageTime = _accessor.ReadInt64(currentPos + 8);
                    long messageAge = (currentTime - messageTime) / TimeSpan.TicksPerMillisecond;

                    // 清理条件：消息过期或缓冲区即将满时的旧消息
                    if (messageAge > MESSAGE_TIMEOUT_MS ||
                        (isBufferAlmostFull && messageAge > MESSAGE_TIMEOUT_MS / 2))
                    {
                        cleanupCount++;
                    }
                    else
                    {
                        // 缓冲区快满时，确保至少清理20%的空间
                        if (isBufferAlmostFull && cleanupCount < msgCount * 0.2)
                        {
                            cleanupCount++;
                            continue;
                        }

                        break;
                    }

                    currentPos = (currentPos + MESSAGE_SIZE) % (HEADER_SIZE + (MAX_MESSAGES * MESSAGE_SIZE));
                    if (currentPos < HEADER_SIZE) currentPos = HEADER_SIZE;
                }

                // 执行清理
                if (cleanupCount > 0)
                {
                    int newReadPos = (readPos + (cleanupCount * MESSAGE_SIZE)) %
                                     (HEADER_SIZE + (MAX_MESSAGES * MESSAGE_SIZE));
                    if (newReadPos < HEADER_SIZE) newReadPos = HEADER_SIZE;

                    _accessor.Write(4, newReadPos);
                    _accessor.Write(8, msgCount - cleanupCount);

                    Debug.WriteLine(
                        $"[{_name}] 已清理 {cleanupCount} 条消息, 原因: {(isBufferAlmostFull ? "缓冲区接近满载" : "消息过期")}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{_name}] 清理任务异常: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 启动订阅消息监听任务
    /// </summary>
    private void StartSubscriptionTask(int topicId, SubscriptionInfo subscription)
    {
        Task.Run(async () =>
        {
            int lastMsgId = 0;

            while (!subscription.Cts.Token.IsCancellationRequested)
            {
                try
                {
                    _messageEvent.WaitOne(100); // 等待新消息或超时

                    int readPos = _accessor.ReadInt32(4);
                    int msgCount = _accessor.ReadInt32(8);

                    if (msgCount > 0)
                    {
                        int currentPos = readPos;
                        for (int i = 0; i < msgCount; i++)
                        {
                            // 读取消息
                            int msgId = _accessor.ReadInt32(currentPos);
                            int msgTopicId = _accessor.ReadInt32(currentPos + 4);

                            // 处理新消息
                            if (msgId > lastMsgId && msgTopicId == topicId)
                            {
                                byte[] data = new byte[1024];
                                _accessor.ReadArray(currentPos + 16, data, 0, data.Length);

                                subscription.Handler((msgId, msgTopicId, data));
                                lastMsgId = msgId;
                            }

                            currentPos = (currentPos + MESSAGE_SIZE) % (HEADER_SIZE + (MAX_MESSAGES * MESSAGE_SIZE));
                            if (currentPos < HEADER_SIZE) currentPos = HEADER_SIZE;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{_name}] 订阅处理异常: {ex.Message}");
                    await Task.Delay(100, subscription.Cts.Token);
                }
            }
        }, subscription.Cts.Token);
    }

    /// <summary>
    /// 取消特定ID的订阅
    /// </summary>
    /// <param name="topicId">主题ID</param>
    /// <param name="subscriptionId">订阅ID</param>
    private void UnsubscribeById(int topicId, Guid subscriptionId)
    {
        if (_subscribers.TryGetValue(topicId, out var topicSubscribers))
        {
            if (topicSubscribers.TryRemove(subscriptionId, out var subscription))
            {
                subscription.Cts.Cancel();
                subscription.Cts.Dispose();
                Debug.WriteLine($"[{_name}] 已取消订阅: Topic={topicId}, SubscriberId={subscriptionId}");
            }

            // 如果主题没有订阅者了，移除主题
            if (topicSubscribers.IsEmpty)
            {
                _subscribers.TryRemove(topicId, out _);
            }
        }
    }

    #endregion

    #region 内部类

    /// <summary>
    /// 订阅信息类
    /// </summary>
    private class SubscriptionInfo
    {
        public Guid Id { get; set; } // 订阅ID
        public Action<(int MessageId, int TopicId, byte[] Data)> Handler { get; set; } // 消息处理函数
        public CancellationTokenSource Cts { get; set; } // 取消令牌
    }

    /// <summary>
    /// 订阅对象，用于管理订阅生命周期
    /// </summary>
    private class Subscription : IDisposable
    {
        private readonly SharedMemoryPubSub _pubSub;
        private readonly int _topicId;
        private readonly Guid _subscriptionId;

        public Subscription(SharedMemoryPubSub pubSub, int topicId, Guid subscriptionId)
        {
            _pubSub = pubSub;
            _topicId = topicId;
            _subscriptionId = subscriptionId;
        }

        public void Dispose()
        {
            _pubSub.UnsubscribeById(_topicId, _subscriptionId);
        }
    }

    #endregion

    #region IDisposable实现

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _accessor?.Dispose();
            _mmf?.Dispose();
            _messageEvent?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}