using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Services;

public class SharedMemoryPubSub : ISharedMemoryPubSub
{
    #region 常量定义
    private const int MAX_MESSAGES = 100;
    private const int MESSAGE_SIZE = 1040; // 4(ID)+4(Topic)+8(Timestamp)+1024(Data)
    private const int HEADER_SIZE = 16;    // 4(WritePos) + 4(ReadPos) + 4(MsgCount) + 4(CurrentMsgId)
    private const int MESSAGE_TIMEOUT_MS = 5000;
    #endregion

    #region 私有字段
    private readonly string _name;
    private readonly string? _mmapFilePath;
    private readonly FileStream? _mmapFileStream;
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly Mutex _rwMutex;
    private readonly EventWaitHandle? _messageEvent;
    private readonly PeriodicTimer _cleanupTimer;
    private readonly Task? _cleanupTask;
    private bool _disposed;

    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, SubscriptionInfo>> _subscribers
        = new ConcurrentDictionary<int, ConcurrentDictionary<Guid, SubscriptionInfo>>();
    #endregion

    #region 构造函数
    public SharedMemoryPubSub(string name)
    {
        _name = name;
        long totalSize = HEADER_SIZE + (MAX_MESSAGES * MESSAGE_SIZE);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _mmf = MemoryMappedFile.CreateOrOpen($"SM_{name}", totalSize);
        }
        else
        {
            string tempDir = Path.GetTempPath();
            _mmapFilePath = Path.Combine(tempDir, $"sm_pubsub_{name}.dat");
            _mmapFileStream = new FileStream(
                _mmapFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);

            if (_mmapFileStream.Length < totalSize)
                _mmapFileStream.SetLength(totalSize);

            _mmf = MemoryMappedFile.CreateFromFile(
                _mmapFileStream,
                null,
                totalSize,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false);
        }

        _accessor = _mmf.CreateViewAccessor();

        string mutexName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"Local\\SM_RW_{name}"
            : $"Local_SM_RW_{name}";
        _rwMutex = new Mutex(false, mutexName);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _messageEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"Local\\SM_EVT_{name}");

        _rwMutex.WaitOne();
        try
        {
            if (_accessor.ReadInt32(0) == 0)
            {
                _accessor.Write(0, HEADER_SIZE);
                _accessor.Write(4, HEADER_SIZE);
                _accessor.Write(8, 0);
                _accessor.Write(12, 0);
            }
        }
        finally
        {
            _rwMutex.ReleaseMutex();
        }

        _cleanupTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _cleanupTask = StartCleanupTask();
    }
    #endregion

    #region 发布订阅
    public void Publish(int topicId, byte[] data)
    {
        if (data == null || data.Length > 1024)
            throw new ArgumentException("数据长度不能大于1024");

        long totalBufferSize = HEADER_SIZE + MAX_MESSAGES * MESSAGE_SIZE;
        _rwMutex.WaitOne();
        try
        {
            int writePos = _accessor.ReadInt32(0);
            int msgCount = _accessor.ReadInt32(8);
            int currentMsgId = _accessor.ReadInt32(12);

            if (msgCount >= MAX_MESSAGES)
                throw new InvalidOperationException("共享内存缓冲区已满");

            var buffer = new byte[1024];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);

            _accessor.Write(writePos, currentMsgId + 1);
            _accessor.Write(writePos + 4, topicId);
            _accessor.Write(writePos + 8, DateTime.UtcNow.Ticks);
            _accessor.WriteArray(writePos + 16, buffer, 0, buffer.Length);

            int nextWritePos = writePos + MESSAGE_SIZE;
            if (nextWritePos >= totalBufferSize)
                nextWritePos = HEADER_SIZE;

            _accessor.Write(0, nextWritePos);
            _accessor.Write(8, msgCount + 1);
            _accessor.Write(12, currentMsgId + 1);

            // 【关键修复1】强制刷新内存视图到磁盘，其他进程立刻可见
            _accessor.Flush();
            _messageEvent?.Set();
        }
        finally
        {
            _rwMutex.ReleaseMutex();
        }
    }

    public IDisposable Subscribe(int topicId, Action<(int MessageId, int TopicId, byte[] Data)> handler)
    {
        var subId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var info = new SubscriptionInfo
        {
            Id = subId,
            Handler = handler,
            Cts = cts,
            LastMessageId = GetCurrentMessageId()
        };

        var topicDict = _subscribers.GetOrAdd(topicId, _ => new ConcurrentDictionary<Guid, SubscriptionInfo>());
        topicDict.TryAdd(subId, info);

        Debug.WriteLine($"[{_name}] 新增订阅 Topic:{topicId} SubId:{subId}");
        StartSubscriptionLoop(topicId, info);

        return new Subscription(this, topicId, subId);
    }

    public void Unsubscribe(int topicId)
    {
        if (_subscribers.TryRemove(topicId, out var subs))
        {
            foreach (var s in subs.Values)
            {
                s.Cts.Cancel();
                s.Cts.Dispose();
            }
            Debug.WriteLine($"[{_name}] 取消主题全部订阅 Topic:{topicId}");
        }
    }
    #endregion

    #region 内部任务逻辑
    private async Task StartCleanupTask()
    {
        long totalBufferSize = HEADER_SIZE + MAX_MESSAGES * MESSAGE_SIZE;
        while (await _cleanupTimer.WaitForNextTickAsync())
        {
            var lockTaken = false;
            try
            {
                lockTaken = _rwMutex.WaitOne(500);
                if (!lockTaken)
                    continue;

                int readPos = _accessor.ReadInt32(4);
                int msgCount = _accessor.ReadInt32(8);
                if (msgCount == 0) continue;

                int currentPos = readPos;
                int cleanCount = 0;
                long now = DateTime.UtcNow.Ticks;
                bool nearFull = msgCount >= MAX_MESSAGES * 0.8;

                for (int i = 0; i < msgCount; i++)
                {
                    long msgTick = _accessor.ReadInt64(currentPos + 8);
                    long elapseMs = (now - msgTick) / TimeSpan.TicksPerMillisecond;

                    if (elapseMs > MESSAGE_TIMEOUT_MS || (nearFull && elapseMs > MESSAGE_TIMEOUT_MS / 2))
                    {
                        cleanCount++;
                    }
                    else
                    {
                        if (nearFull && cleanCount < msgCount * 0.2)
                        {
                            cleanCount++;
                            continue;
                        }
                        break;
                    }

                    currentPos += MESSAGE_SIZE;
                    if (currentPos >= totalBufferSize)
                        currentPos = HEADER_SIZE;
                }

                if (cleanCount > 0)
                {
                    int newReadPos = readPos + cleanCount * MESSAGE_SIZE;
                    if (newReadPos >= totalBufferSize)
                        newReadPos = HEADER_SIZE;

                    _accessor.Write(4, newReadPos);
                    _accessor.Write(8, msgCount - cleanCount);
                    _accessor.Flush(); // 清理后也要刷新
                    Debug.WriteLine($"[{_name}] 清理过期消息 {cleanCount} 条");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{_name}] 清理异常：{ex.Message}");
            }
            finally
            {
                if (lockTaken)
                    _rwMutex.ReleaseMutex();
            }
        }
    }

    private void StartSubscriptionLoop(int topicId, SubscriptionInfo sub)
    {
        long totalBufferSize = HEADER_SIZE + MAX_MESSAGES * MESSAGE_SIZE;
        Task.Run(async () =>
        {
            int lastMsgId = sub.LastMessageId;
            while (!sub.Cts.Token.IsCancellationRequested)
            {
                bool hasNewMsg = false;
                var lockTaken = false;
                try
                {
                    lockTaken = _rwMutex.WaitOne(50); // 缩短锁等待
                    if (!lockTaken)
                    {
                        await Task.Delay(20, sub.Cts.Token);
                        continue;
                    }

                    try
                    {
                        int readPos = _accessor.ReadInt32(4);
                        int msgCount = _accessor.ReadInt32(8);
                        if (msgCount > 0)
                        {
                            int cur = readPos;
                            for (int i = 0; i < msgCount; i++)
                            {
                                int mid = _accessor.ReadInt32(cur);
                                int tid = _accessor.ReadInt32(cur + 4);

                                if (mid > lastMsgId && tid == topicId)
                                {
                                    byte[] data = new byte[1024];
                                    _accessor.ReadArray(cur + 16, data, 0, data.Length);
                                    sub.Handler((mid, tid, data));
                                    lastMsgId = mid;
                                    hasNewMsg = true;
                                }

                                cur += MESSAGE_SIZE;
                                if (cur >= totalBufferSize) cur = HEADER_SIZE;
                            }
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                            _rwMutex.ReleaseMutex();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{_name}] 订阅异常：{ex.Message}");
                }

                // 【关键修复2】有新消息立刻循环，无消息仅休眠20ms，大幅降低丢消息概率
                if (!hasNewMsg)
                {
                    if (_messageEvent is not null)
                        _messageEvent.WaitOne(100);
                    else
                        await Task.Delay(20, sub.Cts.Token);
                }
            }
        }, sub.Cts.Token);
    }

    private void WakeAllSubscribers()
    {
    }

    private void UnsubscribeById(int topicId, Guid subId)
    {
        if (_subscribers.TryGetValue(topicId, out var dict))
        {
            if (dict.TryRemove(subId, out var info))
            {
                info.Cts.Cancel();
                info.Cts.Dispose();
                Debug.WriteLine($"[{_name}] 取消订阅 Topic:{topicId} SubId:{subId}");
            }
            if (dict.IsEmpty)
                _subscribers.TryRemove(topicId, out _);
        }
    }

    private int GetCurrentMessageId()
    {
        _rwMutex.WaitOne();
        try
        {
            return _accessor.ReadInt32(12);
        }
        finally
        {
            _rwMutex.ReleaseMutex();
        }
    }
    #endregion

    #region 内部模型与释放
    private class SubscriptionInfo
    {
        public Guid Id { get; set; }
        public Action<(int MessageId, int TopicId, byte[] Data)> Handler { get; set; }
        public CancellationTokenSource Cts { get; set; }
        public int LastMessageId { get; set; }
    }

    private class Subscription : IDisposable
    {
        private readonly SharedMemoryPubSub _owner;
        private readonly int _topic;
        private readonly Guid _subId;
        public Subscription(SharedMemoryPubSub owner, int topic, Guid subId)
        {
            _owner = owner;
            _topic = topic;
            _subId = subId;
        }
        public void Dispose() => _owner.UnsubscribeById(_topic, _subId);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var topic in _subscribers.Keys)
            Unsubscribe(topic);

        _cleanupTimer?.Dispose();
        _messageEvent?.Dispose();
        _accessor?.Dispose();
        _mmf?.Dispose();
        _mmapFileStream?.Dispose();
        _rwMutex?.Dispose();

        _disposed = true;
    }
    #endregion
}
