using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Ava.Xioa.Common.Utils;

public static class OperatingStorageDialogUtil
{
    public static async Task<string?> SelectFolder(string title = "选择目标文件夹", string? startFolder = null)
    {
        var top = OperatingSystemUtil.GetCurrentTopLevel();
        if (top is null) return null;


        var pickerOptions = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (!string.IsNullOrEmpty(startFolder) && System.IO.Directory.Exists(startFolder))
        {
            IStorageFolder? startStorageFolder = await top.StorageProvider
                .TryGetFolderFromPathAsync(startFolder);

            pickerOptions.SuggestedStartLocation = startStorageFolder;
        }


        var resultList = await top.StorageProvider.OpenFolderPickerAsync(pickerOptions);
        if (!resultList.Any()) return null;
        // 单选取第一个
        var storageFolder = resultList.First();
        return storageFolder.Path.LocalPath;
    }

    public static async Task<IEnumerable<string>> SelectFoldersMultiple(string title = "选择多个文件夹",
        string? startFolder = null)
    {
        var top = OperatingSystemUtil.GetCurrentTopLevel();
        if (top is null)
            return new List<string>();

        var pickerOptions = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = true // 开启多选
        };

        // 设置初始目录
        if (!string.IsNullOrEmpty(startFolder) && System.IO.Directory.Exists(startFolder))
        {
            IStorageFolder? startStorageFolder = await top.StorageProvider.TryGetFolderFromPathAsync(startFolder);
            pickerOptions.SuggestedStartLocation = startStorageFolder;
        }

        var resultList = await top.StorageProvider.OpenFolderPickerAsync(pickerOptions);

        // 提取所有本地路径
        var paths = resultList
            .Select(f => f.Path.LocalPath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        return paths;
    }

    /// <summary>
    /// 单选文件弹窗
    /// </summary>
    /// <param name="title">弹窗标题</param>
    /// <param name="startFolder">初始打开目录</param>
    /// <param name="filter">文件筛选，示例：图片|*.png;*.jpg|所有文件|*.*</param>
    /// <returns>选中文件路径，取消返回 null</returns>
    public static async Task<string?> SelectFile(
        string title = "选择文件",
        string? startFolder = null,
        string filter = "所有文件|*.*")
    {
        var top = OperatingSystemUtil.GetCurrentTopLevel();
        if (top is null)
            return null;

        // 解析文件筛选规则
        var fileTypes = new List<FilePickerFileType>();
        var filterParts = filter.Split('|');
        for (int i = 0; i < filterParts.Length; i += 2)
        {
            string name = filterParts[i];
            string[] extensions = filterParts[i + 1].Split(';');
            fileTypes.Add(new FilePickerFileType(name) { Patterns = extensions });
        }

        var pickerOptions = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        };

        // 设置起始目录
        if (!string.IsNullOrEmpty(startFolder) && Directory.Exists(startFolder))
        {
            var startStorageFolder = await top.StorageProvider.TryGetFolderFromPathAsync(startFolder);
            pickerOptions.SuggestedStartLocation = startStorageFolder;
        }

        var resultList = await top.StorageProvider.OpenFilePickerAsync(pickerOptions);
        if (!resultList.Any())
            return null;

        return resultList.First().Path.LocalPath;
    }

    /// <summary>
    /// 多选文件弹窗
    /// </summary>
    public static async Task<List<string>> SelectFilesMultiple(
        string title = "选择多个文件",
        string? startFolder = null,
        string filter = "所有文件|*.*")
    {
        var top = OperatingSystemUtil.GetCurrentTopLevel();
        if (top is null)
            return new List<string>();

        var fileTypes = new List<FilePickerFileType>();
        var filterParts = filter.Split('|');
        for (int i = 0; i < filterParts.Length; i += 2)
        {
            string name = filterParts[i];
            string[] extensions = filterParts[i + 1].Split(';');
            fileTypes.Add(new FilePickerFileType(name) { Patterns = extensions });
        }

        var pickerOptions = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = fileTypes
        };

        if (!string.IsNullOrEmpty(startFolder) && Directory.Exists(startFolder))
        {
            var startStorageFolder = await top.StorageProvider.TryGetFolderFromPathAsync(startFolder);
            pickerOptions.SuggestedStartLocation = startStorageFolder;
        }

        var resultList = await top.StorageProvider.OpenFilePickerAsync(pickerOptions);
        return resultList
            .Select(f => f.Path.LocalPath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }
}