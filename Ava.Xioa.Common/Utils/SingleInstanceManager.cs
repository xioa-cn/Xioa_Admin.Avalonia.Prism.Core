using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using DryIoc.ImTools;

namespace Ava.Xioa.Common.Utils;

/// <summary>
/// 跨平台单实例管理器
/// </summary>
public class SingleInstanceManager
{
    // 管道名称（全局唯一，建议用GUID）
    private const string PipeName = "XIOAAVALONIAINSTANCEPIPE";
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// 检查是否为第一个实例，若不是则通知已有实例并退出
    /// </summary>
    /// <returns>是否为新实例</returns>
    public bool IsFirstInstance()
    {
        try
        {
            // 尝试创建管道服务器（第一个实例会成功，后续实例会失败）
            var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough
            );

            // 启动异步监听管道消息（等待后续实例的激活命令）
            _ = ListenForActivation(server);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // 管道已存在（已有实例），发送激活命令
            _ = SendActivationSignal();
            return false;
        }
    }

    /// <summary>
    /// 监听管道，接收激活命令
    /// </summary>
    private async Task ListenForActivation(NamedPipeServerStream server)
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            await server.WaitForConnectionAsync(_cts.Token);
            try
            {
                // 接收到激活命令，触发窗口激活
                ActivateMainWindow?.Invoke();
            }
            finally
            {
                if (server.IsConnected)
                    server.Disconnect();
            }
        }

        server.Dispose();
    }

    /// <summary>
    /// 向已有实例发送激活命令
    /// </summary>
    private async Task SendActivationSignal()
    {
        try
        {
            using var client = new NamedPipeClientStream(
                ".", // 本地计算机
                PipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous
            );
            await client.ConnectAsync(1000); // 1秒超时
            await client.WriteAsync(new byte[] { 1 }, 0, 1); // 发送任意数据作为激活信号
        }
        catch(Exception ex)
        {
            var mapPath = ("sendSignal").MapPath();
            
            FileHelper.WriteFile(
                mapPath,
                $"sysDBlog_{DateTimeExtensions.SystemNow():yyyyMMddHHmmss}.txt",
                ex.Message + ex.StackTrace + ex.Source
            );
            // 连接失败（可能实例已退出），忽略
        }
    }

    /// <summary>
    /// 激活主窗口（跨平台通用逻辑）
    /// </summary>
    public Action? ActivateMainWindow { get; set; }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}