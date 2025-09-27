using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Utils;

public static class FileHelper
{
    public static void WriteFile(string path, string file, string content)
    {
        System.IO.File.WriteAllText(System.IO.Path.Combine(path, file), content);
    }

    public static bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    public static async Task ScheduleFileDeletionAsync(string filePath, TimeSpan delay,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            await Task.Delay(delay, cancellationToken);

            if (!File.Exists(filePath))
            {
                return;
            }

            if (IsFileLocked(filePath))
            {
                _ = ScheduleFileDeletionAsync(filePath, TimeSpan.FromMinutes(1), cancellationToken);
                return;
            }

            // 执行删除
            File.Delete(filePath);
            Debug.WriteLine($"File auto deleted successfully :{filePath}");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            var mapPath = ($"Log").MapPath();
            FileHelper.WriteFile(
                mapPath,
                $"DeleteDataFileLog_{DateTimeExtensions.SystemNow():yyyyMMddHHmmss}.txt",
                ex.Message + ex.StackTrace + ex.Source
            );
            _ = ScheduleFileDeletionAsync(filePath, TimeSpan.FromMinutes(1), cancellationToken);
        }
    }
}