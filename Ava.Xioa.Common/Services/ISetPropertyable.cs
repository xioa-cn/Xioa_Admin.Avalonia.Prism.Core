using System;
using System.Linq.Expressions;

namespace Ava.Xioa.Common.Services;

public interface ISetPropertyable<T>
{
    T SetProperty<TProp>(Expression<Func<T, TProp>> propertySelector, TProp value);
    T SetPropertyNotify<TProp>(Expression<Func<T, TProp>> propertySelector, TProp value);
}