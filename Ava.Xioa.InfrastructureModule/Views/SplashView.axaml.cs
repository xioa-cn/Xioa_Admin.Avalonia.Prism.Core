using Avalonia.Controls;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.SplashServices;
using Avalonia.Interactivity;

namespace Ava.Xioa.InfrastructureModule.Views
{
    [PrismRegisterForNavigation(navigationName: nameof(SplashView), region: AppRegions.MainRegion, zIndex: 9999)]
    public partial class SplashView : UserControl
    {
        public SplashView(ISplashServices splashServices)
        {
            this.DataContext = splashServices;
            InitializeComponent();
            Loaded += OnLoaded;
            this.sText.Text =
                $"{AppAuthor.DllCreateTime:yyyy} © AvaloniaApplication BY {AppAuthor.Author}. {AppAuthor.DllCreateTime.TimeYearMonthDayHourString()}";
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (this.DataContext is IInitializedAsyncable initializedAsyncable)
            {
                await initializedAsyncable.InitializedAsync();
            }
        }
    }
}