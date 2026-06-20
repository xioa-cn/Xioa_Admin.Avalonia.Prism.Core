namespace Prism.Modularity;

public interface IModuleInfo
{
    string ModuleName { get; set; }

    Type ModuleType { get; set; }

    InitializationMode InitializationMode { get; set; }

    ModuleState State { get; set; }

    string? Ref { get; set; }

    IList<string> DependsOn { get; }
}