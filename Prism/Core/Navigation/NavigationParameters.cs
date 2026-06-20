using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Prism.Navigation;

public class NavigationParameters : Dictionary<string, object?>, INavigationParameters
{
    private readonly Dictionary<NavigationParameterScope, Dictionary<string, object?>> _scopedParameters = new();

    public NavigationParameters()
    {
    }

    public NavigationParameters(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split(new[] { '=' }, 2);
            var key = Uri.UnescapeDataString(pair[0]);
            var value = pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
            if (!string.IsNullOrWhiteSpace(key))
            {
                Add(key, value, NavigationParameterScope.Query);
            }
        }
    }

    public new void Add(string key, object? value)
    {
        Add(key, value, NavigationParameterScope.Parameters);
    }

    public void Add(string key, object? value, NavigationParameterScope scope)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Navigation parameter key cannot be empty.", nameof(key));
        }

        AddOrAppend(this, key, value);
        AddOrAppend(GetScope(scope), key, value);
    }

    public T? GetValue<T>(string key)
    {
        return TryGetValue(key, out var value) ? ConvertValue<T>(UnwrapFirst(value)) : default;
    }

    public T? GetValue<T>(string key, NavigationParameterScope scope)
    {
        return TryGetScopedValue(key, scope, out var value) ? ConvertValue<T>(UnwrapFirst(value)) : default;
    }

    public IEnumerable<T?> GetValues<T>(string key)
    {
        if (!TryGetValue(key, out var value))
        {
            return Enumerable.Empty<T?>();
        }

        return ExpandValues(value).Select(ConvertValue<T>);
    }

    public IEnumerable<T?> GetValues<T>(string key, NavigationParameterScope scope)
    {
        if (!TryGetScopedValue(key, scope, out var value))
        {
            return Enumerable.Empty<T?>();
        }

        return ExpandValues(value).Select(ConvertValue<T>);
    }

    public bool ContainsKey(string key, NavigationParameterScope scope)
    {
        return _scopedParameters.TryGetValue(scope, out var values) && values.ContainsKey(key);
    }

    public string ToQueryString()
    {
        return ToQueryString(this);
    }

    public string ToQueryString(NavigationParameterScope scope)
    {
        return _scopedParameters.TryGetValue(scope, out var values)
            ? ToQueryString(values)
            : string.Empty;
    }

    public INavigationParameters Clone()
    {
        var clone = new NavigationParameters();
        foreach (var (scope, values) in _scopedParameters)
        {
            foreach (var (key, value) in values)
            {
                foreach (var item in ExpandValues(value))
                {
                    clone.Add(key, item, scope);
                }
            }
        }

        foreach (var (key, value) in this)
        {
            if (clone.ContainsKey(key))
            {
                continue;
            }

            foreach (var item in ExpandValues(value))
            {
                clone.Add(key, item);
            }
        }

        return clone;
    }

    public INavigationParameters AsReadOnly()
    {
        return new ReadOnlyNavigationParameters(this);
    }

    public NavigationParameters Merge(INavigationParameters parameters, bool overwrite = true)
    {
        return Merge(parameters, NavigationParameterScope.Parameters, overwrite);
    }

    public NavigationParameters Merge(INavigationParameters parameters, NavigationParameterScope scope, bool overwrite = true)
    {
        foreach (var parameter in parameters)
        {
            if (!overwrite && ContainsKey(parameter.Key))
            {
                continue;
            }

            if (overwrite && ContainsKey(parameter.Key))
            {
                Remove(parameter.Key);
            }

            Add(parameter.Key, parameter.Value, scope);
        }

        return this;
    }

    public NavigationParameters Filter(params string[] keys)
    {
        return Filter((IEnumerable<string>)keys);
    }

    public NavigationParameters Filter(IEnumerable<string> keys)
    {
        var keySet = new HashSet<string>(keys, StringComparer.Ordinal);
        var filtered = new NavigationParameters();
        foreach (var (key, value) in this)
        {
            if (keySet.Contains(key))
            {
                filtered.Add(key, value);
            }
        }

        foreach (var scope in _scopedParameters.Keys)
        {
            if (!_scopedParameters.TryGetValue(scope, out var values))
            {
                continue;
            }

            foreach (var (key, value) in values)
            {
                if (keySet.Contains(key))
                {
                    filtered.Add(key, value, scope);
                }
            }
        }

        return filtered;
    }

    private Dictionary<string, object?> GetScope(NavigationParameterScope scope)
    {
        if (!_scopedParameters.TryGetValue(scope, out var values))
        {
            values = new Dictionary<string, object?>(StringComparer.Ordinal);
            _scopedParameters[scope] = values;
        }

        return values;
    }

    private bool TryGetScopedValue(string key, NavigationParameterScope scope, out object? value)
    {
        if (_scopedParameters.TryGetValue(scope, out var values) && values.TryGetValue(key, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    private static void AddOrAppend(IDictionary<string, object?> target, string key, object? value)
    {
        if (!target.TryGetValue(key, out var existingValue))
        {
            target.Add(key, value);
            return;
        }

        if (existingValue is List<object?> list)
        {
            list.Add(value);
            return;
        }

        target[key] = new List<object?> { existingValue, value };
    }

    private static object? UnwrapFirst(object? value)
    {
        return value is IEnumerable enumerable && value is not string
            ? enumerable.Cast<object?>().FirstOrDefault()
            : value;
    }

    private static IEnumerable<object?> ExpandValues(object? value)
    {
        if (value is null || value is string)
        {
            yield return value;
            yield break;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                yield return item;
            }

            yield break;
        }

        yield return value;
    }

    private static T? ConvertValue<T>(object? value)
    {
        if (value is null)
        {
            return default;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (targetType == typeof(object))
        {
            return (T?)value;
        }

        if (targetType.IsEnum)
        {
            return (T)Enum.Parse(targetType, value.ToString() ?? string.Empty, ignoreCase: true);
        }

        if (targetType == typeof(Guid))
        {
            return (T)(object)Guid.Parse(value.ToString() ?? string.Empty);
        }

        if (targetType == typeof(Uri))
        {
            return (T)(object)new Uri(value.ToString() ?? string.Empty, UriKind.RelativeOrAbsolute);
        }

        if (value is string stringValue && IsJsonLike(stringValue))
        {
            return JsonSerializer.Deserialize<T>(stringValue);
        }

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(value.GetType()))
        {
            return (T?)converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
        }

        return (T?)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static bool IsJsonLike(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static string ToQueryString(IEnumerable<KeyValuePair<string, object?>> values)
    {
        var parts = new List<string>();
        foreach (var (key, value) in values)
        {
            foreach (var item in ExpandValues(value))
            {
                parts.Add(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(SerializeQueryValue(item)));
            }
        }

        return string.Join("&", parts);
    }

    private static string SerializeQueryValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string stringValue)
        {
            return stringValue;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is decimal || value is Guid || value is DateTime || value is DateTimeOffset || value is Uri)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return JsonSerializer.Serialize(value);
    }
}