using System.Threading.Tasks;

namespace Prism.Dialogs;

public interface IDialogAnimation
{
    Task OnOpeningAsync(DialogAnimationContext context);

    Task OnClosingAsync(DialogAnimationContext context);
}