using Prism.Events;

namespace Ava.Xioa.Common.Events;

public class ThemeChangedEvent : PubSubEvent<TokenKeyPubSubEvent<string>>
{
}