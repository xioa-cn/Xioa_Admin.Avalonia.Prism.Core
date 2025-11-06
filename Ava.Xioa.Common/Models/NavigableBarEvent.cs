using Ava.Xioa.Common.Events;
using Prism.Events;

namespace Ava.Xioa.Common.Models;

public class NavigableBarEvent : PubSubEvent<TokenKeyPubSubEvent<NavigableBarInfoModel>>
{
}