using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Prism.Commands;

internal sealed class PropertyObserver
{
    private readonly Action _onPropertyChanged;
    private readonly List<ObservedProperty> _observedProperties = new();

    public PropertyObserver(Action onPropertyChanged)
    {
        _onPropertyChanged = onPropertyChanged;
    }

    public void Observes<T>(Expression<Func<T>> propertyExpression)
    {
        if (!TryGetObservedProperty(propertyExpression.Body, out var observedProperty))
        {
            throw new ArgumentException("Only simple property expressions are supported, for example: () => IsEnabled.", nameof(propertyExpression));
        }

        observedProperty.Source.PropertyChanged += OnSourcePropertyChanged;
        _observedProperties.Add(observedProperty);
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        foreach (var observedProperty in _observedProperties)
        {
            if (ReferenceEquals(sender, observedProperty.Source) &&
                (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == observedProperty.PropertyName))
            {
                _onPropertyChanged();
                return;
            }
        }
    }

    private static bool TryGetObservedProperty(Expression expression, out ObservedProperty observedProperty)
    {
        if (expression is UnaryExpression unaryExpression)
        {
            expression = unaryExpression.Operand;
        }

        if (expression is MemberExpression { Member: PropertyInfo propertyInfo } memberExpression)
        {
            var source = GetSource(memberExpression.Expression);
            if (source is INotifyPropertyChanged notifyPropertyChanged)
            {
                observedProperty = new ObservedProperty(notifyPropertyChanged, propertyInfo.Name);
                return true;
            }
        }

        observedProperty = default;
        return false;
    }

    private static object? GetSource(Expression? expression)
    {
        if (expression is null)
        {
            return null;
        }

        var converted = Expression.Convert(expression, typeof(object));
        return Expression.Lambda<Func<object?>>(converted).Compile().Invoke();
    }

    private readonly struct ObservedProperty
    {
        public ObservedProperty(INotifyPropertyChanged source, string propertyName)
        {
            Source = source;
            PropertyName = propertyName;
        }

        public INotifyPropertyChanged Source { get; }

        public string PropertyName { get; }
    }
}
