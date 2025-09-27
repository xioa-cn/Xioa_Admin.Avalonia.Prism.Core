using Ava.Xioa.Common.Attributes;
using Avalonia.Controls;
using AvaloniaApplication.ViewModels;

namespace AvaloniaApplication.Views;

[PrismView]
public partial class MainWindow : Window
{
    public MainWindow(UserControl userControl)
    {
        InitializeComponent();
        this.WindowContentControl.Content = userControl;
    }
}