using Prism.Events;

namespace Ava.Xioa.Common.Events;

public class TokenKeyPubSubEvent<T> : EventBase
{
    public T Value { get; set; }

    public string? TokenKey { get; set; }
    

    public TokenKeyPubSubEvent(string tokenKey, T value)
    {
        this.Value = value;
        this.TokenKey = tokenKey;
    }
}