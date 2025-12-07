using InTactCommon.Objects;
using MessagePack;

namespace InTactCommon.Packets;

[Union(0, typeof(MessageSentPacket))]
[MessagePackObject]
public abstract class Packet
{
    
}

[MessagePackObject]
public class MessageSentPacket : Packet
{
    [Key(0)] public ulong ChatId { get; set; }
    [Key(1)] public Message MessageSent { get; set; } = null!;
}