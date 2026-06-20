namespace Prism.Modularity;

public sealed class ModuleInitializer : IModuleInitializer
{
    private readonly IModuleManager _moduleManager;

    public ModuleInitializer(IModuleManager moduleManager)
    {
        _moduleManager = moduleManager;
    }

    public void Initialize(ModuleInfo moduleInfo)
    {
        ArgumentNullException.ThrowIfNull(moduleInfo);
        _moduleManager.LoadModule(moduleInfo.ModuleName);
    }
}