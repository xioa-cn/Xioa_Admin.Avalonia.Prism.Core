using System;

namespace Prism.Modularity;

public sealed class ModuleDownloadProgressChangedEventArgs : EventArgs
{
    public ModuleDownloadProgressChangedEventArgs(ModuleInfo moduleInfo, long bytesReceived, long totalBytesToReceive)
    {
        ModuleInfo = moduleInfo;
        BytesReceived = bytesReceived;
        TotalBytesToReceive = totalBytesToReceive;
    }

    public ModuleInfo ModuleInfo { get; }

    public long BytesReceived { get; }

    public long TotalBytesToReceive { get; }
}