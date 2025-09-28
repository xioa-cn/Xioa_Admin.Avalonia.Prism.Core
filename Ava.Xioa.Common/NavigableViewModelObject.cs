using System;
using System.Windows.Input;
using Avalonia.Threading;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Common;

/// <summary>
/// 适用于Avalonia.Prism的导航视图模型基类
/// 整合了Avalonia的UI线程特性和Prism的导航功能
/// </summary>
public abstract class NavigableViewModelObject : EventEnabledViewModelObject, 
    INavigationAware, IConfirmNavigationRequest, IRegionMemberLifetime
{
    private readonly IRegionManager? _regionManager;
    private bool _isNavigating;
    private bool _keepAlive = true;

    /// <summary>
    /// 是否正在导航中
    /// </summary>
    public bool IsNavigating
    {
        get => _isNavigating;
        protected set => SetProperty(ref _isNavigating, value);
    }

    /// <summary>
    /// 导航命令
    /// </summary>
    public ICommand NavigateCommand { get; }

    /// <summary>
    /// 控制视图模型是否在导航后保留
    /// </summary>
    public virtual bool KeepAlive
    {
        get => _keepAlive;
        protected set => SetProperty(ref _keepAlive, value);
    }

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public NavigableViewModelObject()
    {
        NavigateCommand = new DelegateCommand<NavigationParameters>(ExecuteNavigate, CanNavigate)
            .ObservesProperty(() => IsNavigating);
    }

    /// <summary>
    /// 带事件聚合器和区域管理器的构造函数
    /// </summary>
    public NavigableViewModelObject(IEventAggregator? eventAggregator, IRegionManager? regionManager) 
        : base(eventAggregator)
    {
        _regionManager = regionManager;
        NavigateCommand = new DelegateCommand<NavigationParameters>(ExecuteNavigate, CanNavigate)
            .ObservesProperty(() => IsNavigating);
    }

    /// <summary>
    /// 执行导航
    /// </summary>
    protected virtual void ExecuteNavigate(NavigationParameters? parameters)
    {
        if (parameters == null || !parameters.ContainsKey("TargetView") || !parameters.ContainsKey("RegionName"))
            throw new ArgumentException("导航参数必须包含TargetView和RegionName");

        var targetView = parameters["TargetView"].ToString();
        var regionName = parameters["RegionName"].ToString();

        if (string.IsNullOrEmpty(targetView) || string.IsNullOrEmpty(regionName))
            return;

        try
        {
            IsNavigating = true;
            // 确保在UI线程执行导航（Avalonia要求UI操作在主线程）
            Dispatcher.UIThread.Post(() =>
            {
                _regionManager?.RequestNavigate(regionName, targetView, NavigationCompleted);
            });
        }
        catch (Exception ex)
        {
            OnNavigationFailed(null, ex);
            IsNavigating = false;
        }
    }

    /// <summary>
    /// 导航完成回调
    /// </summary>
    protected virtual void NavigationCompleted(NavigationResult result)
    {
        // 确保在UI线程处理导航结果
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (result.Success == true)
                {
                    OnNavigationCompleted(result.Context);
                }
                else
                {
                    OnNavigationFailed(result.Context, result.Exception);
                }
            }
            finally
            {
                IsNavigating = false;
            }
        });
    }

    /// <summary>
    /// 检查是否可以导航
    /// </summary>
    protected virtual bool CanNavigate(NavigationParameters? parameters)
    {
        return !IsNavigating;
    }

    /// <summary>
    /// 当导航到此实例时调用
    /// </summary>
    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 在UI线程处理导航参数
        Dispatcher.UIThread.Post(() =>
        {
            ProcessNavigationParameters(navigationContext.Parameters);
        });
    }

    /// <summary>
    /// 当导航离开此实例时调用
    /// </summary>
    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 可在此处清理资源
    }

    /// <summary>
    /// 确定此实例是否可以处理指定目标的导航
    /// </summary>
    public virtual bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true; // 默认重用现有实例
    }

    /// <summary>
    /// 确认导航请求（如关闭前保存）
    /// </summary>
    public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
    {
        // 默认允许导航
        continuationCallback(true);
    }

    /// <summary>
    /// 处理导航参数
    /// </summary>
    protected virtual void ProcessNavigationParameters(INavigationParameters parameters)
    {
        // 子类可重写此方法处理导航参数
    }

    /// <summary>
    /// 导航成功完成时调用
    /// </summary>
    protected virtual void OnNavigationCompleted(NavigationContext context)
    {
    }

    /// <summary>
    /// 导航失败时调用
    /// </summary>
    protected virtual void OnNavigationFailed(NavigationContext? context, Exception? error)
    {
        // 可在此处处理导航失败逻辑，如显示错误消息
    }

    /// <summary>
    /// 导航到指定视图的便捷方法
    /// </summary>
    /// <param name="regionName">区域名称</param>
    /// <param name="viewName">视图名称</param>
    /// <param name="parameters">额外导航参数</param>
    protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)
    {
        var navParams = parameters ?? new NavigationParameters();
        navParams.Add("RegionName",regionName);
        navParams.Add("TargetView", viewName);

        NavigateCommand.Execute(navParams);
    }
}
    