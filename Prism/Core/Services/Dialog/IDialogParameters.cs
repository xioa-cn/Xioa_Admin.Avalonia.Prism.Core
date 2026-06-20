namespace Prism.Dialogs;

public interface IDialogParameters : IEnumerable<KeyValuePair<string, object?>>
{
    void Add(string key, object? value);

    T? GetValue<T>(string key);

    IEnumerable<T?> GetValues<T>(string key);

    bool ContainsKey(string key);
}