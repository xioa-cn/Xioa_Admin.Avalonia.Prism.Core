using Ava.Xioa.Common.Attributes;
using Avalonia.Controls;
using AvaloniaApplication.ViewModels;

namespace AvaloniaApplication.Views;

[PrismView]
public partial class MainView : UserControl
{
    public MainView(MainViewViewModel viewViewModel)
    {
        this.DataContext = viewViewModel;
        InitializeComponent();
    }
}