using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Ava.Xioa.Common.Themes.I18n;

public class LocalizeExtension : MarkupExtension
{
    private readonly string _key;
    private WeakReference<AvaloniaObject>? _weakTarget; // 弱引用目标对象（避免内存泄漏）
    private AvaloniaProperty? _targetProperty;
    private AvaloniaObject _targetObject;
    private IDisposable? _languageChangeSubscription; // 语言变化事件的可释放订阅
    

    public LocalizeExtension(string key)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
    }

    public LocalizeExtension(BindingBase  binding)
    {
        
    }
    

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // 获取目标对象和属性（适配 Avalonia 的 IProvideValueTarget）
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target)
        {
            // 处理模板场景：如果目标是模板，返回扩展自身（延迟到实例化时处理）
            if (target.TargetObject is AvaloniaObject targetObject &&
                target.TargetProperty is AvaloniaProperty targetProperty)
            {
                _targetObject = targetObject;
                _targetProperty = targetProperty;

                // 订阅语言变化事件（使用弱引用包装，避免内存泄漏）
                WeakReference<AvaloniaObject> weakTarget = new WeakReference<AvaloniaObject>(targetObject);
                Action updateAction = () =>
                {
                    if (weakTarget.TryGetTarget(out var obj) && _targetProperty != null)
                    {
                        var value = GetLocalizedValue();
                        obj.SetValue(_targetProperty, value);
                    }
                };

                // 关键：确保事件订阅不会持有目标对象的强引用
                I18nManager.Instance.OnLanguageChanged += updateAction;
            }
        }

        // 返回当前本地化值
        return GetLocalizedValue();
    }

    // 更新本地化值到目标属性
    private void UpdateLocalizedValue()
    {
        if (_weakTarget?.TryGetTarget(out var targetObject) == true &&
            _targetProperty != null &&
            targetObject.CheckAccess()) // 确保在UI线程更新
        {
            targetObject.SetValue(_targetProperty, GetLocalizedValue());
        }
    }

    // 获取本地化字符串
    private string GetLocalizedValue()
    {
        return I18nManager.Instance.GetString(_key) ?? $"[{_key}]";
    }

    // 目标对象从视觉树移除时清理资源
    private void OnTargetDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // 取消事件订阅
        _languageChangeSubscription?.Dispose();

        // 移除生命周期监听
        if (sender is Visual visual)
        {
            visual.DetachedFromVisualTree -= OnTargetDetached;
        }
    }
}