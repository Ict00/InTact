using MessagePack;

namespace InTactCommon.Objects;

[MessagePackObject]
public class ServerChatInfo
{
    [Key(0)] public ulong Id { get; set; }
    [Key(1)] public string Name { get; set; }
    
    public ServerChatInfo(ulong Id, string Name)
    {
        this.Id = Id;
        this.Name = Name;
    }

    public ServerChatInfo()
    {
        
    }
}