using System;
using System.Collections.Generic;
using System.Linq;

namespace Prism.Navigation;

public sealed class ReadOnlyNavigationParameters : Dictionary<string, object?>, INavigationParameters, IDictionary<string, object?>
{
    private readonly INavigationParameters _inner;

    public ReadOnlyNavigationParameters(INavigationParameters inner)
        : base(inner.ToDictionary(parameter => parameter.Key, parameter => parameter.Value), StringComparer.Ordinal)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public new object? this[string key]
    {
        get => _inner.First(parameter => string.Equals(parameter.Key, key, StringComparison.Ordinal)).Value;
        set => ThrowReadOnly();
    }

    public new ICollection<string> Keys => _inner.Select(parameter => parameter.Key).ToArray();

    public new ICollection<object?> Values => _inner.Select(parameter => parameter.Value).ToArray();

    public new int Count => _inner.Count();

    public new void Add(string key, object? value) => ThrowReadOnly();

    public void Add(string key, object? value, NavigationParameterScope scope) => ThrowReadOnly();

    public new bool Remove(string key) => ThrowReadOnly<bool>();

    public new void Clear() => ThrowReadOnly();

    public new bool ContainsKey(string key) => _inner.ContainsKey(key);

    public bool ContainsKey(string key, NavigationParameterScope scope) => _inner.ContainsKey(key, scope);

    public new bool TryGetValue(string key, out object? value)
    {
        foreach (var parameter in _inner)
        {
            if (string.Equals(parameter.Key, key, StringComparison.Ordinal))
            {
                value = parameter.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public T? GetValue<T>(string key) => _inner.GetValue<T>(key);

    public T? GetValue<T>(string key, NavigationParameterScope scope) => _inner.GetValue<T>(key, scope);

    public IEnumerable<T?> GetValues<T>(string key) => _inner.GetValues<T>(key);

    public IEnumerable<T?> GetValues<T>(string key, NavigationParameterScope scope) => _inner.GetValues<T>(key, scope);

    public string ToQueryString() => _inner.ToQueryString();

    public string ToQueryString(NavigationParameterScope scope) => _inner.ToQueryString(scope);

    public INavigationParameters Clone() => _inner.Clone();

    public INavigationParameters AsReadOnly() => this;

    public new IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _inner.GetEnumerator();

    void IDictionary<string, object?>.Add(string key, object? value) => ThrowReadOnly();

    bool IDictionary<string, object?>.Remove(string key) => ThrowReadOnly<bool>();

    object? IDictionary<string, object?>.this[string key]
    {
        get => this[key];
        set => ThrowReadOnly();
    }

    ICollection<string> IDictionary<string, object?>.Keys => Keys;

    ICollection<object?> IDictionary<string, object?>.Values => Values;

    private static void ThrowReadOnly()
    {
        throw new InvalidOperationException("Navigation parameters are read-only.");
    }

    private static T ThrowReadOnly<T>()
    {
        throw new InvalidOperationException("Navigation parameters are read-only.");
    }
}
