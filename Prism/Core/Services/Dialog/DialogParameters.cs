using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Prism.Dialogs;

public class DialogParameters : Dictionary<string, object?>, IDialogParameters
{
    public DialogParameters()
    {
    }

    public DialogParameters(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split(new[] { '=' }, 2);
            var key = DecodeQueryValue(pair[0]);
            var value = pair.Length == 2 ? DecodeQueryValue(pair[1]) : string.Empty;
            AddValue(key, value);
        }
    }

    public new void Add(string key, object? value)
    {
        AddValue(key, value);
    }

    public T? GetValue<T>(string key)
    {
        if (!TryGetValue(key, out var value))
        {
            return default;
        }

        if (value is IEnumerable values && value is not string)
        {
            foreach (var item in values)
            {
                return ConvertValue<T>(item);
            }

            return default;
        }

        return ConvertValue<T>(value);
    }

    public IEnumerable<T?> GetValues<T>(string key)
    {
        if (!TryGetValue(key, out var value))
        {
            return Array.Empty<T?>();
        }

        if (value is IEnumerable<T?> values && value is not string)
        {
            return values;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            var converted = new List<T?>();
            foreach (var item in enumerable)
            {
                converted.Add(ConvertValue<T>(item));
            }

            return converted;
        }

        return new[] { ConvertValue<T>(value) };
    }

    public string ToQueryString()
    {
        var parts = new List<string>();
        foreach (var (key, value) in this)
        {
            if (value is IEnumerable values && value is not string)
            {
                foreach (var item in values)
                {
                    parts.Add($"{EncodeQueryValue(key)}={EncodeQueryValue(Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty)}");
                }

                continue;
            }

            parts.Add($"{EncodeQueryValue(key)}={EncodeQueryValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private void AddValue(string key, object? value)
    {
        if (!TryGetValue(key, out var existing))
        {
            base.Add(key, value);
            return;
        }

        if (existing is List<object?> list)
        {
            list.Add(value);
            return;
        }

        this[key] = new List<object?> { existing, value };
    }

    private static T? ConvertValue<T>(object? value)
    {
        if (value is null)
        {
            return default;
        }

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (value is T typedValue)
        {
            return typedValue;
        }

        if (targetType.IsEnum && value is string enumText)
        {
            return (T)Enum.Parse(targetType, enumText, true);
        }

        return (T?)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static string DecodeQueryValue(string value)
    {
        return Uri.UnescapeDataString(value.Replace("+", "%20", StringComparison.Ordinal));
    }

    private static string EncodeQueryValue(string value)
    {
        return Uri.EscapeDataString(value).Replace("%20", "+", StringComparison.Ordinal);
    }
}
