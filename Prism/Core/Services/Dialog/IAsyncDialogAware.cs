using System.Threading.Tasks;

namespace Prism.Dialogs;

public interface IAsyncDialogAware : IDialogAware
{
    Task<bool> CanCloseDialogAsync();
}