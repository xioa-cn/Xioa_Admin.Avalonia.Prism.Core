using System;
using System.Threading.Tasks;
using Rougamo;
using Rougamo.Context;

namespace Ava.Xioa.Common.Services;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MinAspectAttribute : AsyncMoAttribute
{
    public int MinVal { get; set; }
    public int DefaultVal { get; set; }

    public MinAspectAttribute(int minVal, int defaultVal = 0)
    {
        DefaultVal = defaultVal;
        MinVal = minVal;
    }

    public override ValueTask OnEntryAsync(MethodContext context)
    {
        if (context.Arguments.Length == 0)
            return ValueTask.CompletedTask;

        // 遍历所有int参数校验
        foreach (var arg in context.Arguments)
        {
            if (arg is int val && val < MinVal)
            {
                ReplaceReturnByMethodType(context);
                // 校验失败直接退出，不再遍历其他参数
                return ValueTask.CompletedTask;
            }
        }

        // 全部参数校验通过，正常执行原方法
        return ValueTask.CompletedTask;
    }
    
    private void ReplaceReturnByMethodType(MethodContext context)
    {
        var returnType = context.ReturnType;

        if (returnType == typeof(int))
        {
            // 同步 int 方法
            context.ReplaceReturnValue(this, DefaultVal);
        }
        else if (returnType == typeof(Task<int>))
        {
            // 异步 Task<int>
            context.ReplaceReturnValue(this, Task.FromResult(DefaultVal));
        }
        else if (returnType == typeof(ValueTask<int>))
        {
            // 异步 ValueTask<int>
            context.ReplaceReturnValue(this, new ValueTask<int>(DefaultVal));
        }
        else if (returnType == typeof(void) || returnType == typeof(Task))
        {
            // 无返回值方法，传null截断
            context.ReplaceReturnValue(this, null);
        }
        else
        {
            // 不匹配的返回类型直接抛异常，提前发现问题
            throw new NotSupportedException($"MinAspect 不支持返回类型：{returnType.Name}");
        }
    }
}