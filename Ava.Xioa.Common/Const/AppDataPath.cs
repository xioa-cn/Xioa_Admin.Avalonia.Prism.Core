using System;

namespace Ava.Xioa.Common.Const;

public class AppDataPath
{
    private static string _localFolder = string.Empty;

    private static string LocalFolder
    {
        get
        {
            if (!string.IsNullOrEmpty(_localFolder))
            {
                return _localFolder;
            }

            _localFolder =
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Ava.Xioa");
            if (!System.IO.Directory.Exists(_localFolder))
            {
                System.IO.Directory.CreateDirectory(_localFolder);
            }

            return _localFolder;
        }
    }

    public static string GetLocalFilePath(string fileName)
    {
        return System.IO.Path.Combine(LocalFolder, fileName);
    }
}