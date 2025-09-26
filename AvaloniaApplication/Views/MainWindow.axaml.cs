using Ava.Xioa.Common.Attributes;
using Avalonia.Controls;
using AvaloniaApplication.ViewModels;

namespace AvaloniaApplication.Views;

[PrismView]
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        this.DataContext = viewModel;
        InitializeComponent();
    }
}