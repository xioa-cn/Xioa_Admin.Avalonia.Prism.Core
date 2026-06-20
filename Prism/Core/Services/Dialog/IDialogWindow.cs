namespace Prism.Dialogs;

public interface IDialogWindow
{
    object? Content { get; set; }

    string? Title { get; set; }
}
