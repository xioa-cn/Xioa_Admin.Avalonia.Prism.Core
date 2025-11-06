using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Impl.Implementations.WindowServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ava.Xioa.InfrastructureModule.Components;

public partial class NavigableMenuContent : UserControl
{
    public NavigableMenuContent()
    {
        InitializeComponent();
    }

    private void NavigationBarInfoModel_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton btn && btn.Tag is NavigableBarInfoModel model)
        {
            if (this.DataContext is MainWindowViewModel viewModel)
            {
                var navigationParameters =
                    NavigationParametersHelper.TargetNavigationParameters(model.TargetView, model.RegionName);
                viewModel.ExecuteNavigate(navigationParameters);
            }
        }
        
        
    }
}