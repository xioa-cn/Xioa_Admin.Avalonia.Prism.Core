using System;
using System.IO;
using System.Text.Json;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.RouterServices;
using Avalonia.Platform;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.RouterServices;

[PrismService(typeof(IRouterServices))]
public class RouterImpl : IRouterServices
{
    /// <summary>
    /// 加载路由配置（优先使用运行目录的router.json，不存在则从Avalonia资源提取）
    /// </summary>
    /// <returns>导航菜单数组（NavigableMenuItemModel[]）</returns>
    public NavigableMenuItemModel[] PrismApplicationRouter()
    {
        // 定义文件路径和资源路径
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "router.json");

        // 若文件不存在，从Avalonia资源提取并写入到运行目录
        if (!File.Exists(filePath))
        {
            string resourcePath = "avares://AvaloniaApplication/router.json"; 
            ExtractResourceToFile(resourcePath, filePath);
        }

        // 读取并解析JSON文件为菜单数组
        return ParseRouterJson(filePath);
    }

    /// <summary>
    /// 从Avalonia资源中提取文件并写入到本地路径
    /// </summary>
    private void ExtractResourceToFile(string resourcePath, string targetFilePath)
    {
        try
        {
            var uri = new Uri(resourcePath);

            using var stream = AssetLoader.Open(new Uri(resourcePath));
            using var reader = new StreamReader(stream);
            var svgContent = reader.ReadToEnd();
            File.WriteAllText(targetFilePath, svgContent);
        }
        catch (Exception ex)
        {
            throw new Exception($"提取资源文件失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 解析本地router.json文件为NavigableMenuItemModel数组
    /// </summary>
    private NavigableMenuItemModel[] ParseRouterJson(string filePath)
    {
        try
        {
            var jsonContent = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(jsonContent))
                throw new InvalidDataException("router.json内容为空");

            // 反序列化为路由配置模型
            var routerConfig =
                JsonSerializer.Deserialize<ResourcesRouters>(jsonContent, GlobalJsonOptions.SerializeOptions);


            if (routerConfig is null)
            {
                throw new InvalidDataException("router.json反序列化失败");
            }

            return routerConfig.Routers;
        }
        catch (Exception ex)
        {
            throw new Exception($"解析router.json失败：{ex.Message}", ex);
        }
    }
}