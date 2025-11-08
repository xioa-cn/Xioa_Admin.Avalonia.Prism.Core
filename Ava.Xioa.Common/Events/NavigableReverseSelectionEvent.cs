using Ava.Xioa.Common.Models;
using Prism.Events;

namespace Ava.Xioa.Common.Events;

public class NavigableReverseSelectionEvent : PubSubEvent<TokenKeyPubSubEvent<ReverseSelectionPub>>
{
}