using MessagePack;

namespace InTactCommon.Objects;

[MessagePackObject]
public class Chat
{
    [Key(0)] public List<Message> Messages { get; set; } = [];
    [Key(1)] public ulong Id { get; set; }
    [Key(2)] public string Name { get; set; } = "";

    public Chat()
    {
        
    }
}