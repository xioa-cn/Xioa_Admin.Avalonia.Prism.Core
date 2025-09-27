using Ava.Xioa.Common.Services;
using System.Threading.Tasks;
using Ava.Xioa.Common.Attributes;
using AvaloniaApplication.Models;
using System;
using System.Windows.Input;
using Ava.Xioa.Common.Input;


namespace AvaloniaApplication.ViewModels;

[PrismVm(typeof(MainWindowViewModel))]
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IMessageService _messageService;

    [ObservableBindProperty] private string _name;

    [ObservableBindProperty] private bool _isLoadingAsync;

    [ObservableBindProperty] private string _greeting;

    [ObservableBindProperty] private string _asyncMessage;

    [ObservableBindProperty] private string _nowTime;

    [ObservableBindProperty] private TestModel _TestModel;
    
    
    
    public ICommand RelayParameterCommand { get; set; } 
    
    public ICommand RelayCommand { get; set; } 
    
    public ICommand AsyncRelayCommand { get; set; }

    public MainWindowViewModel(IMessageService messageService)
    {
        _messageService = messageService;
        _greeting = _messageService.GetWelcomeMessage();
        Task.Factory.StartNew(Test, TaskCreationOptions.LongRunning);
        
        RelayParameterCommand = new RelayCommand<string>(LoadMessageAsync);
        RelayCommand = new RelayCommand(LoadMessageAsync);
        
        AsyncRelayCommand = new AsyncRelayCommand(LoadAsyncMessageAsync);
    }

    private async Task Test()
    {
        while (true)
        {
            NowTime = DateTime.Now.ToString("hh:mm:ss");
            await Task.Delay(1000);
        }
    }

    partial void OnNameChanged();

   
    private async Task LoadAsyncMessageAsync()
    {
        IsLoadingAsync = true;
        try
        {
            AsyncMessage = await _messageService.GetAsyncMessageAsync();
        }
        finally
        {
            IsLoadingAsync = false;
        }
    }
    
    private async void LoadAsyncMessageAsync(string str)
    {
        IsLoadingAsync = true;
        try
        {
            AsyncMessage = "AsyncRelayCommand" + str;
        }
        finally
        {
            IsLoadingAsync = false;
        }
    }
    
    private void LoadMessageAsync()
    {
        IsLoadingAsync = true;
        try
        {
            AsyncMessage = "RelayCommand";
        }
        finally
        {
            IsLoadingAsync = false;
        }
    }
    
    private void LoadMessageAsync(string str)
    {
        IsLoadingAsync = true;
        try
        {
            AsyncMessage = "RelayCommand" + str;
        }
        finally
        {
            IsLoadingAsync = false;
        }
    }
}