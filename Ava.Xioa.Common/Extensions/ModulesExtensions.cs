using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Prism.Modularity;

namespace Ava.Xioa.Common.Extensions;

public static class ModulesExtensions
{
    private const string _configModuleName = "modules.config";

    public static IModuleCatalog AddConfigModule(
        this IModuleCatalog catalog, string? moduleFileOrDir)
    {
        // 1. 修复路径逻辑：区分传入的是文件夹 / 完整文件
        string configPath;
        if (string.IsNullOrWhiteSpace(moduleFileOrDir))
        {
            configPath = _configModuleName;
        }
        else
        {
            if (Directory.Exists(moduleFileOrDir))
            {
                // 传入文件夹，拼接配置文件名
                configPath = Path.Combine(moduleFileOrDir, _configModuleName);
            }
            else
            {
                // 传入完整配置文件路径，直接使用
                configPath = moduleFileOrDir;
            }
        }
        configPath = Path.GetFullPath(configPath);

        // 文件存在校验，携带文件路径
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"模块配置文件不存在", configPath);

        // 加载配置
        var modulesConfiguration = ModulesConfiguration.LoadConfiguration(configPath);
        if (modulesConfiguration == null)
            throw new ConfigurationErrorsException($"配置文件 {configPath} 解析失败，内容为空或格式错误");

        if (modulesConfiguration.Name != "PrismModules.Config")
        {
            throw new ConfigurationErrorsException(
                $"配置文件标识不匹配，期望 PrismModules.Config，当前 {modulesConfiguration.Name}");
        }

        // 遍历加载模块程序集
        foreach (var itemModule in modulesConfiguration.Modules.Modules)
        {
            Assembly assembly;
            try
            {
                //LoadFrom 按文件路径加载dll
                assembly = Assembly.LoadFrom(itemModule.AssemblyFile);
            }
            catch (Exception ex)
            {
                throw new FileLoadException($"加载模块程序集失败：{itemModule.AssemblyFile}", ex);
            }

            foreach (var moduleName in itemModule.ModuleNames)
            {
                // 找不到类型不抛异常，返回null
                var allPublicTypes = assembly.GetExportedTypes();
                // 匹配类名，忽略命名空间
                var matchTypes = allPublicTypes
                    .FirstOrDefault(t => t.Name == moduleName && typeof(IModule).IsAssignableFrom(t))
                     ?? throw new ConfigurationErrorsException(
                        $"程序集 {itemModule.AssemblyFile} 中未找到模块类型：{moduleName}");

                //// 校验必须实现IModule接口
                //if (!typeof(IModule).IsAssignableFrom(matchTypes))
                //{
                //    throw new ConfigurationErrorsException(
                //        $"类型 {moduleName} 未实现 Prism IModule，无法作为模块");
                //}

                catalog.AddModule(matchTypes);
            }
        }

        return catalog;
    }

    public static IModuleCatalog AddAutoModule(
        this IModuleCatalog catalog)
    {
        var compilationLibrary = DependencyCompilation.GetCompilationLibrary();
        if (compilationLibrary == null) return catalog;

        foreach (var compilation in compilationLibrary)
        {
            var types = AssemblyLoadContext.Default
                .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                .GetTypes().Where(a =>
                    a.GetCustomAttribute<AutoModuleAttribute>() != null)
                .ToList();
            if (types.Count <= 0) continue;
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<AutoModuleAttribute>();
                if (attr == null) continue;

                catalog.AddModule(type);
            }
        }


        return catalog;
    }
}