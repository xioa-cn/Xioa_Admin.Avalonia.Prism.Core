using System;
using System.IO;
using System.Text;

namespace Module.CreateAssistant;

public partial class Program
{
   static void Main(string[] args)
    {
        // 初始化
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        Console.ForegroundColor = COLOR_DEFAULT;

        // 标题与分割线
        WriteSeparatorLine();
        WriteColorLine("===== 项目创建工具（.NET 8）=====", ConsoleColor.Magenta);
        WriteSeparatorLine();

        // 自动搜索 .sln
        WriteColor("\n开始自动搜索解决方案文件（.sln）...", COLOR_PROMPT);
        string slnPath = FindSlnFileUpward();
        if (string.IsNullOrEmpty(slnPath))
        {
            WriteColorLine("\n错误：未找到任何解决方案文件（.sln）！", COLOR_ERROR);
            ResetColor();
            return;
        }
        WriteColorLine($"\n找到解决方案：{slnPath}", COLOR_SUCCESS);

        // 用户输入
        string projectName = GetUserInput("请输入项目名称（如 Ava.Xioa.Utils）：", COLOR_PROMPT);

        if (string.IsNullOrEmpty(projectName))
        {
            WriteColorLine("\n错误：项目名称不能为空！", COLOR_ERROR);
            ResetColor();
            return;
        }
        
        projectName = string.IsNullOrEmpty(projectName) ? "NewProject" : projectName;

        // 明确说明解决方案文件夹的作用和示例
        string solutionFolder = GetUserInput(
            "请输入解决方案文件夹（用于在解决方案中组织项目，如 Common , Modules , Utils，直接回车不指定）：", 
            COLOR_PROMPT
        );

        // 固定配置
        string projectType = "classlib";
        string framework = "net8.0";
        WriteColorLine($"\n配置确认：类型={projectType} | 框架={framework}", COLOR_WARNING);
        if (!string.IsNullOrEmpty(solutionFolder))
        {
            WriteColorLine($"解决方案文件夹：{solutionFolder}", COLOR_WARNING);
        }

        // 路径计算
        string slnDirectory = Path.GetDirectoryName(slnPath);
        string projectPath = Path.Combine(slnDirectory, projectName);
        string csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");

        try
        {
            // 步骤1：创建项目
            WriteColor($"\n[步骤1/3] 正在创建 {projectType} 项目 {projectName}...", COLOR_PROMPT);
            ShowProgressBar("项目创建中");
            bool createSuccess = RunDotnetCommand(
                $"new {projectType} -n {projectName} --framework {framework} -o \"{projectPath}\" --force"
            );
            if (!createSuccess) throw new Exception("项目创建失败");
            WriteColorLine(" 完成！", COLOR_SUCCESS);

            // 新增步骤：处理默认类文件和创建Module文件
            WriteColor($"\n[步骤2/4] 正在处理默认类文件...", COLOR_PROMPT);
            ShowProgressBar("文件处理中");
            ProcessDefaultFiles(projectPath, projectName);
            WriteColorLine(" 完成！", COLOR_SUCCESS);

            // 步骤2：修改 .csproj 文件（原步骤2变为步骤3）
            WriteColor($"\n[步骤3/4] 正在修改 {projectName}.csproj...", COLOR_PROMPT);
            ShowProgressBar("配置文件写入中");
            ModifyCsprojFile(csprojPath);
            WriteColorLine(" 完成！", COLOR_SUCCESS);

            // 步骤3：添加到解决方案（原步骤3变为步骤4）
            WriteColor($"\n[步骤4/4] 正在将项目添加到解决方案文件夹...", COLOR_PROMPT);
            ShowProgressBar("解决方案关联中");
            string addCommand = string.IsNullOrEmpty(solutionFolder)
                ? $"sln \"{slnPath}\" add \"{csprojPath}\""
                : $"sln \"{slnPath}\" add -s \"{solutionFolder}\" \"{csprojPath}\""; // 核心：-s 指定解决方案文件夹
            bool addSuccess = RunDotnetCommand(addCommand);
            if (!addSuccess) throw new Exception("添加到解决方案失败");
            WriteColorLine(" 完成！", COLOR_SUCCESS);

            // 结果总结（显示解决方案文件夹）
            WriteSeparatorLine();
            WriteColorLine("✅ 全部操作完成！", COLOR_SUCCESS);
            WriteColorLine($"📁 项目路径：{projectPath}", COLOR_PROMPT);
            WriteColorLine($"📄 .csproj 路径：{csprojPath}", COLOR_PROMPT);
            WriteColorLine($"📋 解决方案：{slnPath}", COLOR_PROMPT);
            if (!string.IsNullOrEmpty(solutionFolder))
                WriteColorLine($"📂 已添加到解决方案文件夹：{solutionFolder}", COLOR_PROMPT); // 明确反馈
            WriteSeparatorLine();
        }
        catch (Exception ex)
        {
            WriteColorLine($"\n❌ 操作失败：{ex.Message}", COLOR_ERROR);
        }
        finally
        {
            ResetColor();
        }

        WriteColor("\n按任意键退出...", COLOR_WARNING);
        Console.ReadKey();
    }
   
}