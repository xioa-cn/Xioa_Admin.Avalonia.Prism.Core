using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Ava.Xioa.Common.Services;

public abstract class ChainedSetProperty<T> : ObservableBindBase, ISetPropertyable<T> where T : ChainedSetProperty<T>
{
    private readonly ConcurrentDictionary<string, PropertyInfo> _propertyExpressionsCache = new();

    public T SetProperty<TProp>(Expression<Func<T, TProp>> propertySelector, TProp value)
    {
        var prop = GetPropertyInfo(propertySelector);

        // 校验属性可写性
        if (!prop.CanWrite)
            throw new InvalidOperationException($"属性「{prop.Name}」是只读的，无法更新");

        // 校验类型兼容性
        if (value != null && !prop.PropertyType.IsAssignableFrom(value.GetType()))
            throw new ArgumentException($"属性「{prop.Name}」类型不匹配，需{prop.PropertyType.Name}类型");

        // 设置值并触发通知
        SetPropertyWithNotify(prop, value, false);
        return (T)this;
    }
    
    public T SetPropertyNotify<TProp>(Expression<Func<T, TProp>> propertySelector, TProp value)
    {
        var prop = GetPropertyInfo(propertySelector);

        // 校验属性可写性
        if (!prop.CanWrite)
            throw new InvalidOperationException($"属性「{prop.Name}」是只读的，无法更新");

        // 校验类型兼容性
        if (value != null && !prop.PropertyType.IsAssignableFrom(value.GetType()))
            throw new ArgumentException($"属性「{prop.Name}」类型不匹配，需{prop.PropertyType.Name}类型");

        // 设置值并触发通知
        SetPropertyWithNotify(prop, value, true);
        return (T)this;
    }

    private void SetPropertyWithNotify(PropertyInfo prop, object value, bool isNotify = true)
    {
        var fieldName = $"_{char.ToLowerInvariant(prop.Name[0])}{prop.Name.Substring(1)}";
        var field = GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

        if (field != null)
        {
            var setMethod = typeof(ObservableBindBase)
                .GetMethod("SetProperty", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(field.FieldType);

            if (setMethod != null)
            {
                // 传入参数：ref字段值、新值
                var parameters = new object?[] { field.GetValue(this), value };
                setMethod.Invoke(this, parameters);
                return;
            }
        }

        prop.SetValue(this, value);

        if (isNotify)
        {
            OnPropertyChanged(prop.Name);
        }
    }

    private PropertyInfo GetPropertyInfo<TProp>(Expression<Func<T, TProp>> propertySelector)
    {
        var cacheKey = propertySelector.ToString();
        // 缓存命中直接返回
        if (_propertyExpressionsCache.TryGetValue(cacheKey, out var prop))
            return prop;

        // 解析Lambda表达式获取属性
        if (propertySelector.Body is not MemberExpression memberExpr || memberExpr.Member is not PropertyInfo property)
            throw new ArgumentException("表达式必须是属性访问，格式如：p => p.PropertyName");

        // 加入缓存
        _propertyExpressionsCache.TryAdd(cacheKey, property);
        return property;
    }
}