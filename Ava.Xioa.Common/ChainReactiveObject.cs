using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Ava.Xioa.Common.Services;

namespace Ava.Xioa.Common;


public abstract class ChainReactiveObject<T> : ObservableBindBase, ISetPropertyable<T> 
    where T : ChainReactiveObject<T>, new() 
{
    // 缓存属性信息（表达式字符串 -> PropertyInfo）
    private readonly ConcurrentDictionary<string, PropertyInfo> _propertyCache = new();
    // 缓存反射方法（类型 -> SetProperty 方法）
    private readonly ConcurrentDictionary<Type, MethodInfo> _setMethodCache = new();

    /// <summary>
    /// 链式设置属性（自动判断是否需要触发通知，默认字段与属性匹配时触发）
    /// </summary>
    public T SetProperty<TProp>(Expression<Func<T, TProp>> propertySelector, TProp value)
    {
        return SetPropertyCore(propertySelector, value, isNotify: null);
    }

    /// <summary>
    /// 链式设置属性并强制触发通知
    /// </summary>
    public T SetPropertyNotify<TProp>(Expression<Func<T, TProp>> propertySelector, TProp value)
    {
        return SetPropertyCore(propertySelector, value, isNotify: true);
    }

    /// <summary>
    /// 核心实现：处理属性设置与通知
    /// </summary>
    private T SetPropertyCore<TProp>(Expression<Func<T, TProp>> propertySelector, TProp value, bool? isNotify)
    {
        // 解析并缓存属性信息
        var prop = GetPropertyInfo(propertySelector);
        ValidatePropertyWritable(prop);
        ValidateValueType(prop, value);

        // 尝试通过字段设置（兼容 ObservableBindBase 的 SetProperty 模式）
        var field = GetBackingField(prop);
        if (field != null)
        {
            SetValueByField(field, value);
           
            return (T)this;
        }

        // 直接设置属性值
        SetValueByProperty(prop, value, isNotify ?? true);
        return (T)this;
    }

    /// <summary>
    /// 获取属性对应的后备字段（支持 _camelCase 或 m_camelCase 命名）
    /// </summary>
    private FieldInfo? GetBackingField(PropertyInfo prop)
    {
        var type = GetType();
        // 支持多种字段命名规范（增加灵活性）
        var possibleNames = new[] 
        { 
            $"_{char.ToLowerInvariant(prop.Name[0])}{prop.Name.Substring(1)}", // _name
            $"m_{char.ToLowerInvariant(prop.Name[0])}{prop.Name.Substring(1)}"  // m_name
        };

        foreach (var name in possibleNames)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) return field;
        }
        return null;
    }

    /// <summary>
    /// 通过字段设置值（调用 ObservableBindBase 的 SetProperty 方法）
    /// </summary>
    private void SetValueByField(FieldInfo field, object value)
    {
        try
        {
            // 缓存 SetProperty 方法（泛型方法按字段类型缓存）
            var fieldType = field.FieldType;
            var setMethod = _setMethodCache.GetOrAdd(fieldType, type =>
                typeof(ObservableBindBase)
                    .GetMethod("SetProperty", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(type)
            );

            // 调用 SetProperty<T>(ref T field, T value)
            var fieldValue = field.GetValue(this);
            var parameters = new object?[] { fieldValue, value };
            var result = setMethod.Invoke(this, parameters);
            
            // 更新字段值（如果方法返回新值）
            if (parameters[0] != null)
                field.SetValue(this, parameters[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"设置字段 {field.Name} 失败（类型：{field.FieldType.Name}）", ex);
        }
    }

    /// <summary>
    /// 直接通过属性设置值
    /// </summary>
    private void SetValueByProperty(PropertyInfo prop, object? value, bool isNotify)
    {
        try
        {
            prop.SetValue(this, value);
            if (isNotify)
                OnPropertyChanged(prop.Name); // 假设基类有 OnPropertyChanged 方法
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"设置属性 {prop.Name} 失败（类型：{prop.PropertyType.Name}）", ex);
        }
    }

    /// <summary>
    /// 解析表达式获取属性信息（带缓存）
    /// </summary>
    private PropertyInfo GetPropertyInfo<TProp>(Expression<Func<T, TProp>> propertySelector)
    {
        if (propertySelector.Body is not MemberExpression memberExpr 
            || memberExpr.Member is not PropertyInfo prop)
        {
            throw new ArgumentException("表达式必须是属性访问，格式如：p => p.PropertyName");
        }

        // 用属性名作为缓存键（比表达式字符串更简洁）
        var cacheKey = $"{prop.DeclaringType?.FullName}.{prop.Name}";
        return _propertyCache.GetOrAdd(cacheKey, prop);
    }

    /// <summary>
    /// 验证属性是否可写
    /// </summary>
    private void ValidatePropertyWritable(PropertyInfo prop)
    {
        if (!prop.CanWrite)
            throw new InvalidOperationException($"属性「{prop.Name}」是只读的，无法更新");
    }

    /// <summary>
    /// 验证值类型是否与属性兼容
    /// </summary>
    private void ValidateValueType(PropertyInfo prop, object? value)
    {
        if (value == null) return; // 允许null（除非属性是值类型）

        var valueType = value.GetType();
        if (!prop.PropertyType.IsAssignableFrom(valueType))
        {
            throw new ArgumentException(
                $"属性「{prop.Name}」类型不匹配，需要「{prop.PropertyType.Name}」，实际是「{valueType.Name}」");
        }
    }
    
}