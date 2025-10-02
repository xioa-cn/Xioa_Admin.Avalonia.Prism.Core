using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Ava.Xioa.Common.Themes.I18n;

public class LocalizeBindingExtension : MarkupExtension
{
    private readonly string _key;
    private WeakReference<AvaloniaObject>? _weakTarget; // 弱引用目标对象（避免内存泄漏）
    private AvaloniaProperty? _targetProperty;
    private AvaloniaObject _targetObject;
    private IDisposable? _languageChangeSubscription; // 语言变化事件的可释放订阅
    private readonly BindingBase _binding;

    public LocalizeBindingExtension(BindingBase binding)
    {
        _binding = binding;
        _binding.Mode = BindingMode.OneWay;
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
                        ResolveBindingExpression(obj, targetProperty);
                        //var value = GetLocalizedValue();
                        //obj.SetValue(_targetProperty, value);
                    }
                };
                var uiContext = SynchronizationContext.Current;

                I18nManager.Instance.OnLanguageChanged += updateAction;
            }
        }

        var firstKey = BindingResolver.ResolveValue(_binding);

        if (firstKey is string firstKeyStr)
        {
            return GetLocalizedValue(firstKeyStr);
        }

        return AvaloniaProperty.UnsetValue;
    }

    // 替代 ResolveBindingExpression 的另一种方式
    private BindingExpressionBase? _lastBindingExpression;
    private void ResolveBindingExpression(AvaloniaObject targetObject, AvaloniaProperty targetProperty)
    {
        _lastBindingExpression?.Dispose();
        _binding.Converter = new I18NConverter();
        _lastBindingExpression = targetObject.Bind(targetProperty, _binding);
    }

    private class I18NConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string _key)
            {
                return I18nManager.Instance.GetString(_key) ?? $"[{_key}]";
            }

            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }


    // 获取本地化字符串
    private string GetLocalizedValue(string key)
    {
        return I18nManager.Instance.GetString(key) ?? $"[{_key}]";
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

public class TempBindingTarget : AvaloniaObject
{
    // 临时属性，用于接收绑定值
    public static readonly AvaloniaProperty<object?> TempValueProperty =
        AvaloniaProperty.Register<TempBindingTarget, object?>(nameof(TempValue));

    public object? TempValue
    {
        get => GetValue(TempValueProperty);
        set => SetValue(TempValueProperty, value);
    }
}

public static class BindingResolver
{
    /// <summary>
    /// 尝试从 BindingBase 中同步解析值（适用于源已就绪的场景）
    /// </summary>
    /// <param name="binding">要解析的 BindingBase</param>
    /// <param name="source">绑定的源对象（可选，若绑定已指定 Source 则可忽略）</param>
    /// <returns>解析到的值，若失败返回 null</returns>
    public static object? ResolveValue(BindingBase binding, object? source = null)
    {
        var tempTarget = new TempBindingTarget();
        
        if (source != null && binding is Binding bind && bind.Source == null)
        {
            bind = new Binding(bind.Path)
            {
                Source = source,
                Mode = bind.Mode,
                Converter = bind.Converter,
            };
            binding = bind;
        }
        
        var r = tempTarget.Bind(TempBindingTarget.TempValueProperty, binding);
        
        var value = tempTarget.TempValue;

        // 解绑，避免内存泄漏
        r.Dispose();
        return value;
    }
}