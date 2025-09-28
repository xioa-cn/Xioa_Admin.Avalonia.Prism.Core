
using System.ComponentModel;

namespace Ava.Xioa.Common.Models;

public enum ProgrammingVersion
{
    /// <summary>
    /// 过期版本
    /// </summary>
    [Description("过期版本")]
    Obsolete,

    /// <summary>
    /// 启用中版本
    /// </summary>
    [Description("启用中版本")]
    EnabledStandby
}