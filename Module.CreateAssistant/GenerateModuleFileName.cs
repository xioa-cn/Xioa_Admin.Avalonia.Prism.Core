using System;

namespace Module.CreateAssistant;

public partial class Program
{
    static string GenerateModuleFileName(string projectName)
    {
        // 如果包含.，取最后一段
        string[] nameSegments = projectName.Split('.');
        string baseName = nameSegments.Length > 0 ? nameSegments[nameSegments.Length - 1] : projectName;
        
        // 如果不包含Module，添加Module后缀
        if (!baseName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
        {
            baseName += "Module";
        }
        
        // 确保首字母大写（符合类名规范）
        if (baseName.Length > 0)
        {
            return char.ToUpper(baseName[0]) + baseName.Substring(1);
        }
        
        return "Module";
    }
}