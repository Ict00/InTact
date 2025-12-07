using MessagePack;

namespace InTactCommon.Objects;

[MessagePackObject]
public class Message
{
    [Key(0)] public ulong AuthorId { get; set; }
    [Key(1)] public List<ulong> Mentions { get; set; } = [];
    [Key(2)] public string Content { get; set; } = "";
    [Key(3)] public ulong Id { get; set; }
    [Key(4)] public long Timestamp { get; set; }

    public Message()
    {
        
    }
}