using Ava.Xioa.Common.Events;
using Prism.Events;

namespace AvaloniaApplication.PubSubEvents;

public class ThemeChangedEvent : PubSubEvent<TokenKeyPubSubEvent<string>>
{
}