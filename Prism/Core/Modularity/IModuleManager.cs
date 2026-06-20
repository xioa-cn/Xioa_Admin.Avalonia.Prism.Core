namespace Prism.Modularity;

public interface IModuleManager
{
    event EventHandler<LoadModuleCompletedEventArgs>? LoadModuleCompleted;

    event EventHandler<ModuleDownloadProgressChangedEventArgs>? ModuleDownloadProgressChanged;

    // void Run();
    void InitializeModules();

    void LoadModule(string moduleName);

    void RegisterModules();
}