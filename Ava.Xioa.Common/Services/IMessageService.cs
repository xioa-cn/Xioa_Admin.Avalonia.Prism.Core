using System.Threading.Tasks;

namespace Ava.Xioa.Common.Services;

/// <summary>
/// 消息服务接口，用于演示依赖注入
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// 获取欢迎消息
    /// </summary>
    /// <returns>欢迎消息</returns>
    string GetWelcomeMessage();
    
    /// <summary>
    /// 异步获取消息
    /// </summary>
    /// <returns>异步消息</returns>
    Task<string> GetAsyncMessageAsync();
}