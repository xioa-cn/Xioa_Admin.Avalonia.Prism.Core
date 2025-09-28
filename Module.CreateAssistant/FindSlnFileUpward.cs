using System.IO;
using System.Threading;

namespace Module.CreateAssistant;

public partial class Program
{
    static string FindSlnFileUpward()
    {
        string currentDir = Directory.GetCurrentDirectory();
        WriteColorLine($"\n搜索起始目录：{currentDir}", COLOR_PROMPT);

        while (true)
        {
            string[] slnFiles = Directory.GetFiles(currentDir, "*.sln");
            if (slnFiles.Length > 0)
            {
                return slnFiles[0];
            }

            string parentDir = Path.GetDirectoryName(currentDir);
            if (parentDir == currentDir)
            {
                return null;
            }

            currentDir = parentDir;
            WriteColorLine($"未找到，继续向上搜索：{currentDir}", COLOR_PROMPT);
            Thread.Sleep(300);
        }
    }
}