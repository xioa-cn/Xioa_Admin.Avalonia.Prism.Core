using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Rougamo;
using Rougamo.Context;

namespace Ava.Xioa.Common.Services;

// 允许标记类+方法
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class LogAspect : AsyncMoAttribute
{
    public LogAspect()
    {
       
    }

    // 进入方法（异步标准钩子）
    public override ValueTask OnEntryAsync(MethodContext context)
    {
        var type = context.Method.DeclaringType;
        var argsStr = context.Arguments == null || !context.Arguments.Any()
            ? "无参数"
            : string.Join(",", context.Arguments.Select(x => x?.ToString() ?? "null"));

        Debug.WriteLine($"【进入】{type?.Name}.{context.Method.Name} 参数：{argsStr}");
        return base.OnEntryAsync(context);
    }

    // 方法正常返回（异步钩子）
    public override ValueTask OnSuccessAsync(MethodContext context)
    {
        Debug.WriteLine($"【成功】{context.Method.Name} 返回值：{context.ReturnValue ?? "null"}");
        return base.OnSuccessAsync(context);
    }

    // 异常（异步钩子）
    public override ValueTask OnExceptionAsync(MethodContext context)
    {
        Debug.WriteLine($"【异常】{context.Method.Name} 错误信息：{context.Exception?.Message}");
        return base.OnExceptionAsync(context);
    }

    // 最终退出（异步钩子）
    public override ValueTask OnExitAsync(MethodContext context)
    {
        Debug.WriteLine($"【退出】{context.Method.Name}");
        return base.OnExitAsync(context);
    }
}