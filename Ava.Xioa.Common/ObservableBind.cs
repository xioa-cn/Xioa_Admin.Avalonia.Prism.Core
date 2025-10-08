using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ava.Xioa.Common;

public abstract partial class ObservableBindBase : INotifyPropertyChanged, INotifyPropertyChanging
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public virtual void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
    {
        if (this.PropertyChanged == null)
            return;
        string propertyName = ObservableBindBase.GetPropertyName<T>(propertyExpression);
        if (string.IsNullOrEmpty(propertyName))
            return;
        this.OnPropertyChanged(propertyName);
    }

    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        OnPropertyChanging(propertyName);
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
    {
        if (propertyExpression == null)
            throw new ArgumentNullException(nameof(propertyExpression));
        if (!(propertyExpression.Body is MemberExpression body))
            throw new ArgumentException("Invalid argument", nameof(propertyExpression));
        return (body.Member as PropertyInfo ??
                throw new ArgumentException("Argument is not a property", nameof(propertyExpression))).Name;
    }

    protected bool Set<T>(string? propertyName, ref T field, T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
            return false;
        field = newValue;
        this.OnPropertyChanged(propertyName);
        return true;
    }
    
    protected bool Set<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
    {
        return this.Set<T>(propertyName, ref field, newValue);
    }
    
    protected bool Set<T>(Expression<Func<T>> propertyExpression, ref T field, T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
            return false;
        field = newValue;
        this.OnPropertyChanged<T>(propertyExpression);
        return true;
    }
}