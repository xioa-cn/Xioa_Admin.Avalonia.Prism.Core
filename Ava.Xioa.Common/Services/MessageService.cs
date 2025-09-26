using System;
using System.Threading.Tasks;
using Ava.Xioa.Common.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Services;

/// <summary>
/// 消息服务的默认实现
/// </summary>
[PrismService(typeof(IMessageService), ServiceLifetime.Singleton)]
public class MessageService : IMessageService
{
    public string GetWelcomeMessage()
    {
        return "欢迎使用 Avalonia 与 Prism！这条消息来自 Common 项目中依赖注入的服务。";
    }

    public async Task<string> GetAsyncMessageAsync()
    {
        // 模拟异步操作
        await Task.Delay(1000);
        return $"异步消息加载完成！时间：{DateTime.Now:HH:mm:ss}";
    }
}