using Prism.Events;

namespace Ava.Xioa.Common.Events;

public class Exit
{
    public int ExitCode { get; set; }
}

public class ExitApplicationEvent : PubSubEvent<TokenKeyPubSubEvent<Exit>>
{
}