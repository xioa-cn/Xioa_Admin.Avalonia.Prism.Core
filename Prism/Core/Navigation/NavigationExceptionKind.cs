namespace Prism.Navigation;

public enum NavigationExceptionKind
{
    None,
    Unknown,
    TargetNotFound,
    ConfirmationRejected,
    InterceptorRejected,
    Timeout,
    NavigationException
}