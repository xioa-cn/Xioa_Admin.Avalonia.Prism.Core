using System.Collections.Generic;

namespace Prism.Navigation;

public interface INavigationParameters : IEnumerable<KeyValuePair<string, object?>>
{
    void Add(string key, object? value);

    void Add(string key, object? value, NavigationParameterScope scope);

    T? GetValue<T>(string key);

    T? GetValue<T>(string key, NavigationParameterScope scope);

    IEnumerable<T?> GetValues<T>(string key);

    IEnumerable<T?> GetValues<T>(string key, NavigationParameterScope scope);

    bool ContainsKey(string key);

    bool ContainsKey(string key, NavigationParameterScope scope);

    string ToQueryString();

    string ToQueryString(NavigationParameterScope scope);

    INavigationParameters Clone();

    INavigationParameters AsReadOnly();
}