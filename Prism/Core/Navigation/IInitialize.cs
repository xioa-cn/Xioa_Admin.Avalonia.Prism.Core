namespace Prism.Navigation;

public interface IInitialize
{
    void Initialize(INavigationParameters parameters);
}

public interface IInitializeAsync
{
    Task InitializeAsync(INavigationParameters parameters);
}