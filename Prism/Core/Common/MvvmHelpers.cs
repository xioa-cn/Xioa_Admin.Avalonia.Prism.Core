using Avalonia.Controls;
using Prism.Mvvm;

namespace Prism.Common;

public static class MvvmHelpers
{
    public static void AutowireViewModel(object view)
    {
        if (view is Control control)
        {
            control.DataContext ??= ViewModelLocationProvider.AutoWireViewModelChanged(control);
        }
    }
}