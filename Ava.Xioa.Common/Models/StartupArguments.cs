using System;
using System.Linq;

namespace Ava.Xioa.Common.Models;

public class StartupArguments
{
    public StartupArguments(string[] args)
    {
        Args = args;
    }
    public string[] Args { get; set; } = Array.Empty<string>();

    // 辅助方法方便模块解析参数
    public bool HasArg(string argKey) => Args.Contains(argKey);
}