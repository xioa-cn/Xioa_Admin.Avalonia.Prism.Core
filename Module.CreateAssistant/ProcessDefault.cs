using System.IO;
using System.Text;

namespace Module.CreateAssistant;

public partial class Program
{
    static void ProcessDefaultFiles(string projectPath, string projectName)
    {
        // 1. 删除默认生成的Class1.cs
        string class1Path = Path.Combine(projectPath, "Class1.cs");
        if (File.Exists(class1Path))
        {
            File.Delete(class1Path);
        }
        else
        {
            throw new FileNotFoundException("未找到默认生成的Class1.cs文件", class1Path);
        }

        // 2. 生成Module文件名
        string moduleFileName = GenerateModuleFileName(projectName);
        
        // 3. 创建Module.cs文件
        string moduleFilePath = Path.Combine(projectPath, $"{moduleFileName}.cs");
        string namespaceName = projectName; // 使用项目名作为命名空间
        string classContent = $@"using Ava.Xioa.Common.Modularity;

namespace {namespaceName};

public class {moduleFileName}: PrismAutoModule<{moduleFileName}>
{{
    // 模块核心功能实现
}}
";
        File.WriteAllText(moduleFilePath, classContent, Encoding.UTF8);
    }

}