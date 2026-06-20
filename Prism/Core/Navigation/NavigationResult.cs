namespace Prism.Navigation;

public sealed class NavigationResult
{
    public NavigationResult(bool success)
        : this(
            success ? NavigationResultStatus.Succeeded : NavigationResultStatus.Canceled,
            null,
            null,
            success ? NavigationExceptionKind.None : NavigationExceptionKind.Unknown,
            null)
    {
    }

    public NavigationResult(Exception exception)
        : this(
            NavigationResultStatus.Failed,
            exception,
            null,
            NavigationExceptionKind.NavigationException,
            exception.Message)
    {
    }

    public NavigationResult(bool success, NavigationContext? context)
        : this(
            success ? NavigationResultStatus.Succeeded : NavigationResultStatus.Canceled,
            null,
            context,
            success ? NavigationExceptionKind.None : NavigationExceptionKind.Unknown,
            null)
    {
    }

    public NavigationResult(Exception exception, NavigationContext? context, NavigationExceptionKind kind = NavigationExceptionKind.NavigationException)
        : this(NavigationResultStatus.Failed, exception, context, kind, exception.Message)
    {
    }

    public NavigationResult(NavigationResultStatus status, Exception? exception, NavigationContext? context, NavigationExceptionKind exceptionKind, string? message)
    {
        Status = status;
        Success = status == NavigationResultStatus.Succeeded;
        Exception = exception;
        Context = context;
        ExceptionKind = exceptionKind;
        Message = message;
    }

    public bool Success { get; }

    public Exception? Exception { get; }

    public NavigationResultStatus Status { get; }

    public NavigationContext? Context { get; }

    public NavigationExceptionKind ExceptionKind { get; }

    public string? Message { get; }
}