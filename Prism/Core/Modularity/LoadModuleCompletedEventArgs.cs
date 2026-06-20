namespace Prism.Modularity;

public sealed class LoadModuleCompletedEventArgs : EventArgs
{
    public LoadModuleCompletedEventArgs(ModuleInfo moduleInfo, Exception? error)
    {
        ModuleInfo = moduleInfo;
        Error = error;
    }

    public ModuleInfo ModuleInfo { get; }

    public Exception? Error { get; }

    public bool IsErrorHandled { get; set; }
}