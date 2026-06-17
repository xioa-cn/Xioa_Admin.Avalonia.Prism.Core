using System;
using Prism.Ioc;

namespace Ava.Xioa.Common.Utils;

public static class ContainerLocatorUtils
{
    public static T GetService<T>(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? GetService<T>() : ContainerLocator.Container.Resolve<T>(name);
    }
    
    public static T GetService<T>()
    {
        return ContainerLocator.Container.Resolve<T>();
    }
    
    /// <summary>
    /// 判断容器是否初始化完成
    /// </summary>
    public static bool IsContainerReady()
    {
        return ContainerLocator.Container != null;
    }

    /// <summary>
    /// 校验容器，未初始化直接抛清晰异常
    /// </summary>
    private static void CheckContainerValid()
    {
        if (!IsContainerReady())
        {
            throw new InvalidOperationException("Prism容器尚未初始化，请勿在App.RegisterTypes阶段调用Ioc工具类");
        }
    }
}