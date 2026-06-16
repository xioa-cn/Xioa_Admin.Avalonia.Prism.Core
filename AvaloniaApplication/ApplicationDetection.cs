using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Services;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AvaloniaApplication;

public partial class App : IExitService
{
    #region 常量

    private const string AppOpenSignal = "OPEN";
    private const string MutexKey = "sdk-sk-ava0616";

    #endregion

    #region 全局持有资源（关键，防止释放失效）

    private Mutex? _appSingleMutex;
    private SharedMemoryPubSub? _memoryPubSub;
    private IDisposable? _wakeSubscribe; // 保存订阅句柄用于释放

    #endregion

    /// <summary>
    /// 返回 true 代表已有程序在运行；false 为本程序首个实例
    /// </summary>
    public bool IsAnotherInstanceRunning
    {
        get
        {
            if (Design.IsDesignMode) return false;
            
            // 跨平台适配Mutex名称
            string mutexName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"Local\\{MutexKey}"
                : $"Local_{MutexKey}";

            // 全局保存Mutex，不能局部变量
            _appSingleMutex = new Mutex(true, mutexName, out var isNewInstance);
            InitSharedMemoryIfNull();
            if (!isNewInstance)
            {
                // 第二个进程：发唤醒信号然后退出

                _memoryPubSub!.Publish(MessageTopics.STATUS_UPDATE, Encoding.UTF8.GetBytes(AppOpenSignal));
                Thread.Sleep(500);
                return true;
            }

            // 首个进程：初始化共享内存 + 订阅唤醒消息
            SubscribeWakeSignal();
            return false;
        }
    }

    /// <summary>懒加载共享内存</summary>
    private void InitSharedMemoryIfNull()
    {
        _memoryPubSub ??= new SharedMemoryPubSub(MutexKey);
    }

    /// <summary>订阅其他进程发来的唤醒指令</summary>
    private void SubscribeWakeSignal()
    {
        // 保存订阅对象，程序退出释放
        _wakeSubscribe = _memoryPubSub!.Subscribe(MessageTopics.STATUS_UPDATE, OnReceiveWakeMsg);
    }

    /// <summary>收到唤醒消息，切UI线程唤起主窗口</summary>
    private void OnReceiveWakeMsg((int MessageId, int TopicId, byte[] Data) msg)
    {
        if (msg.TopicId != MessageTopics.STATUS_UPDATE)
            return;

        string content = Encoding.UTF8.GetString(msg.Data).TrimEnd('\0');
        if (content != AppOpenSignal)
            return;

        // 切换UI线程恢复窗口
        Dispatcher.UIThread.Post(ShowMainWindow);
    }

    public void Exit()
    {
        // 释放订阅
        _wakeSubscribe?.Dispose();
        // 释放共享内存PubSub
        _memoryPubSub?.Dispose();
        // 释放单实例互斥锁
        _appSingleMutex?.ReleaseMutex();
        _appSingleMutex?.Dispose();
    }
}