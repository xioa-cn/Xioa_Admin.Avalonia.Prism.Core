namespace Ava.Xioa.Common.Models;

/// <summary>
/// 消息主题定义类
/// </summary>
public class MessageTopics
{
    /// <summary>
    /// 数据同步消息
    /// 用于进程间数据同步
    /// </summary>
    public const int DATA_SYNC = 1;

    /// <summary>
    /// 状态更新消息
    /// 用于通知系统状态变化
    /// </summary>
    public const int STATUS_UPDATE = 2;

    /// <summary>
    /// 命令消息
    /// 用于发送控制命令
    /// </summary>
    public const int COMMAND = 3;

    /// <summary>
    /// 心跳消息
    /// 用于检测进程存活状态
    /// </summary>
    public const int HEARTBEAT = 4;

    /// <summary>
    /// 日志消息
    /// 用于进程间传递日志信息
    /// </summary>
    public const int LOG = 5;

    /// <summary>
    /// 警报消息
    /// 用于发送紧急通知或警报
    /// </summary>
    public const int ALERT = 6;

    /// <summary>
    /// 配置更新消息
    /// 用于通知配置变更
    /// </summary>
    public const int CONFIG_CHANGE = 7;

    /// <summary>
    /// 调试消息
    /// 用于开发调试
    /// </summary>
    public const int DEBUG = 8;
}