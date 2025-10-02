using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Ava.Xioa.Common.Models;

namespace Ava.Xioa.Common.Themes.I18n;

public class I18nManager
{
    private static I18nManager? _i18NManager;

    public static I18nManager Instance
    {
        get
        {
            if (_i18NManager is null)
            {
                _i18NManager = new I18nManager();
            }

            return _i18NManager;
        }
    }


    public I18nJsonMode I18NJsonMode { get; private set; } = I18nJsonMode.OnApplicationResources;

    public void JsonMode(I18nJsonMode mode)
    {
        I18NJsonMode = mode;
    }

    // 允许外部设置资源所在的程序集
    public Assembly ResourceAssembly { get; private set; }

    public void I18nResourceAssembly(Assembly assembly)
    {
        ResourceAssembly = assembly;
    }

    // 资源文件所在的命名空间/路径
    public string ResourceNamespace { get; private set; }

    public void I18nResourceNamespace(string namespaceName)
    {
        ResourceNamespace = namespaceName;
    }

    // 默认语言代码
    public string DefaultLanguage { get; private set; } = "en";

    public void DefaultLang(string langCode)
    {
        DefaultLanguage = langCode;
    }

    public string UsingLanguage { get; set; }

    private Dictionary<string, object> _currentLangDict;
    public event Action OnLanguageChanged;

    private I18nManager()
    {
        // 构造函数保持为空，避免在这里初始化
    }

    // 初始化方法，需要显式调用
    public void Initialize()
    {
        UsingLanguage = DefaultLanguage;
        if (this.I18NJsonMode == I18nJsonMode.OnApplicationResources)
        {
            // 验证必要的配置
            if (ResourceAssembly == null)
                throw new InvalidOperationException("请先设置ResourceAssembly属性");

            if (string.IsNullOrEmpty(ResourceNamespace))
                throw new InvalidOperationException("请先设置ResourceNamespace属性");

            // 使用默认语言初始化
            ChangeLanguage(DefaultLanguage);
        }
        else if (I18NJsonMode == I18nJsonMode.OnFileDir)
        {
            if (string.IsNullOrEmpty(ResourceDirectory))
            {
                throw new InvalidOperationException("请先设置ResourceDirectory属性");
            }

            ChangeLanguage(DefaultLanguage);
        }
    }

    public void ChangeLanguage(string langCode)
    {
        if (I18NJsonMode == I18nJsonMode.OnApplicationResources)
        {
            ChangeAppResourcesLanguage(langCode);
        }
        else if (I18NJsonMode == I18nJsonMode.OnFileDir)
        {
            ChangeFileDirLanguage(langCode);
        }
    }

    private void ChangeFileDirLanguage(string langCode)
    {
        UsingLanguage = langCode;
        if (string.IsNullOrEmpty(ResourceDirectory))
        {
            throw new InvalidOperationException("请先设置ResourceDirectory属性");
        }

        var filePath = Path.Combine(ResourceDirectory, $"{langCode}.json");

        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException($"找不到语言资源文件: {langCode}.json");
        }

        var json = System.IO.File.ReadAllText(filePath);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _currentLangDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

        // 处理嵌套结构
        _currentLangDict = FlattenDictionary(_currentLangDict);
        OnLanguageChanged?.Invoke();
    }

    private void ChangeAppResourcesLanguage(string langCode)
    {
        UsingLanguage = langCode;
        // 再次验证配置，确保调用者已正确设置
        if (ResourceAssembly == null)
            throw new InvalidOperationException("ResourceAssembly未设置，请先配置资源所在的程序集");

        if (string.IsNullOrEmpty(ResourceNamespace))
            throw new InvalidOperationException("ResourceNamespace未设置，请先配置资源路径");

        // 构建资源名称
        var resourceName = $"{ResourceNamespace}.{langCode}.json";
        resourceName = resourceName.Replace("..", "."); // 处理可能的连续点号

        // 尝试加载指定语言
        using (var stream = ResourceAssembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                LoadLanguageFromStream(stream);
                OnLanguageChanged?.Invoke();
                return;
            }
        }

        // 尝试加载默认语言
        var defaultResourceName = $"{ResourceNamespace}.{DefaultLanguage}.json";
        defaultResourceName = defaultResourceName.Replace("..", ".");

        using (var defaultStream = ResourceAssembly.GetManifestResourceStream(defaultResourceName))
        {
            if (defaultStream != null)
            {
                LoadLanguageFromStream(defaultStream);
                OnLanguageChanged?.Invoke();
                return;
            }
        }

        // 所有尝试都失败时抛出异常
        throw new FileNotFoundException($"找不到语言资源文件: {langCode}.json 和默认的 {DefaultLanguage}.json",
            $"{langCode}.json");
    }

    private void LoadLanguageFromStream(Stream stream)
    {
        using (var reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _currentLangDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

            // 处理嵌套结构
            _currentLangDict = FlattenDictionary(_currentLangDict);
        }
    }
    
    /// <summary>
    /// 将嵌套的字典结构扁平化为单层字典，使用点号表示嵌套关系
    /// </summary>
    /// <param name="dict">要扁平化的原始字典</param>
    /// <param name="prefix">用于构建键前缀的字符串，默认为空字符串</param>
    /// <returns>扁平化后的单层字典</returns>
    private Dictionary<string, object> FlattenDictionary(Dictionary<string, object> dict, string prefix = "")
    {
        // 创建结果字典
        var result = new Dictionary<string, object>();

        // 遍历字典中的每个键值对
        foreach (var item in dict)
        {
            // 构建当前键，如果存在前缀则用点号连接
            var key = string.IsNullOrEmpty(prefix) ? item.Key : $"{prefix}.{item.Key}";

            // 检查值是否为JSON对象
            if (item.Value is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                // 将JSON元素反序列化为嵌套字典
                var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                // 递归处理嵌套字典
                var flattened = FlattenDictionary(nestedDict, key);

                // 将扁平化后的嵌套字典项合并到结果中
                foreach (var nestedItem in flattened)
                {
                    result[nestedItem.Key] = nestedItem.Value;
                }
            }
            else
            {
                // 非对象值直接转换为字符串并添加到结果中
                result[key] = item.Value?.ToString() ?? string.Empty;
            }
        }

        // 返回扁平化后的字典
        return result;
    }

    public string ResourceDirectory { get; private set; }

    public void I18nResourceDirectory(string directory)
    {
        ResourceDirectory = directory;
    }
    
    /// <summary>
    /// 根据指定的键获取对应的字符串值
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <returns>
    /// 如果找到对应的值，则返回该值的字符串表示形式；
    /// 如果未找到，则返回一个包含键的格式化字符串 "[key]"
    /// </returns>
    public string GetString(string key)
    {
        // 使用空条件运算符检查 _currentLangDict 是否为空
        // 尝试从字典中获取与指定键关联的值
        if (_currentLangDict?.TryGetValue(key, out var value) == true)
        {
            // 如果找到值，则将其转换为字符串并返回
            return value.ToString();
        }

        // 如果未找到值，先查询补充字符，则返回包含键的格式化字符串，表示未找到该键对应的翻译

        if (this._extensionKeyValue != null)
        {
            return this._extensionKeyValue(key);
        }

        return $"[{key}]";
    }

    private Func<string, string>? _extensionKeyValue;

    public void ExtensionValueFunc(Func<string, string> func)
    {
        _extensionKeyValue = func;
    }
}