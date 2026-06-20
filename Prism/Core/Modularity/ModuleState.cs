namespace Prism.Modularity;

public enum ModuleState
{
    NotStarted,
    LoadingTypes,
    ReadyForInitialization,
    Initializing,
    Initialized
}