using AspectInjector.Broker;
using System;
using System.Diagnostics;

namespace Ava.Xioa.Common.Services;

[Aspect(Scope.Global)]
public class LogAspect
{
    /// <summary>方法执行前打印日志</summary>
    [Advice(Kind.Before, Targets = Target.Method)]
    public void LogBefore(
        [Argument(Source.Name)] string methodName,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Attribute)] LogAttribute logAttr
    )
    {
        Console.WriteLine($"[日志前置] {logAttr.Desc} 方法:{methodName} 参数:{string.Join(",", args)}");
    }

    /// <summary>方法执行完成后打印日志+耗时</summary>
    [Advice(Kind.Around, Targets = Target.Method)]
    public object LogAround(AdviceContext ctx)
    {
        var sw = Stopwatch.StartNew();

        // 执行原始方法
        var result = ctx.Proceed();

        sw.Stop();
        Console.WriteLine($"[日志后置] 方法:{ctx.Method.Name} 耗时:{sw.ElapsedMilliseconds}ms 返回值:{result}");

        return result;
    }

    /// <summary>异常拦截日志</summary>
    [Advice(Kind.Error, Targets = Target.Method)]
    public void LogError(
        [Argument(Source.Exception)] Exception ex,
        [Argument(Source.Name)] string methodName
    )
    {
        Console.WriteLine($"[日志异常] 方法:{methodName} 异常:{ex.Message}");
    }
}

/// <summary>标记需要日志切面的方法</summary>
[Injection(typeof(LogAspect))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class LogAttribute : Attribute
{
    public string Desc { get; }

    public LogAttribute(string desc)
    {
        Desc = desc;
    }
}