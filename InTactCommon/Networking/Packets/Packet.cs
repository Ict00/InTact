using InTactCommon.Objects;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;

namespace InTactCommon.Networking.Packets;

[Union(0, typeof(MessageSentPacket))]
[Union(1, typeof(RequestChats))]
[Union(2, typeof(RequestUsers))]
[Union(3, typeof(ResponseChats))]
[Union(4, typeof(ResponseUsers))]
[Union(5, typeof(UserChangedStatus))]
[Union(6, typeof(JoinServerRequest))]
[Union(7, typeof(JoinServerResponse))]
[Union(8, typeof(BatchPacket))]
[Union(9, typeof(RequestChat))]
[Union(10, typeof(ResponseChat))]
[Union(11, typeof(IntroductionUser))]
[Union(12, typeof(UserAdded))]
[Union(13, typeof(MessageSentDm))]
[Union(14, typeof(ResponseDms))]
[MessagePackObject]
public abstract class Packet
{
    public void SendPacketTo(NetPeer peer, NetDataWriter writer, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        byte[] data = MessagePackSerializer.Serialize(this);
        writer.Reset();
        writer.Put(data);
        
        peer.Send(writer, method);
    }
    
    public void SendPacketToAll(NetManager manager, NetDataWriter writer, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        byte[] data = MessagePackSerializer.Serialize(this);
        writer.Reset();
        writer.Put(data);
        
        manager.SendToAll(writer, method);
    }

    public void SendPacketToAllAuthorized(NetManager manager, List<int> authorized, NetDataWriter writer,
        DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        List<NetPeer> peers = new();
        manager.GetPeersNonAlloc(peers, ConnectionState.Connected);

        var filteredPeers = peers.FindAll(x => authorized.Contains(x.Id));
        
        foreach (var i in filteredPeers)
        {
            SendPacketTo(i, writer, method);
        }
    }
}

[MessagePackObject]
public class BatchPacket : Packet
{
    [Key(0)] public List<Packet> Packets { get; set; }
}

[MessagePackObject]
public class JoinServerRequest : Packet
{
    [Key(0)] public string Token { get; set; }
}

[MessagePackObject]
public class JoinServerResponse : Packet
{
    [Key(0)] public ulong User { get; set; }
    [Key(1)] public JoinStatus Status { get; set; }
}

[MessagePackObject]
public class MessageSentPacket : Packet
{
    [Key(0)] public ulong ChatId { get; set; }
    [Key(1)] public Message MessageSent { get; set; }
}

[MessagePackObject]
public class ResponseChat : Packet
{
    [Key(0)] public Chat Chat { get; set; }
}

[MessagePackObject]
public class ResponseDms : Packet
{
    [Key(0)] public Dictionary<ulong, List<Message>> DMs { get; set; }
}

[MessagePackObject]
public class RequestChat : Packet
{
    [Key(0)] public ulong ChatId { get; set; }
    [Key(1)] public int Amount { get; set; }
}

[MessagePackObject]
public class RequestChats : Packet
{
    [Key(0)] public int OnlyLastAmount { get; set; }
}

[MessagePackObject]
public class RequestUsers : Packet
{
    
}

[MessagePackObject]
public class IntroductionUser : Packet
{
    [Key(0)] public string Username { get; set; }
    [Key(1)] public SColor PfpColor { get; set; }
    [Key(2)] public string Token { get; set; }
}

[MessagePackObject]
public class UserAdded : Packet
{
    [Key(0)] public User User { get; set; }
}

[MessagePackObject]
public class ResponseUsers : Packet
{
    [Key(0)] public List<User> Users { get; set; } = [];
}

[MessagePackObject]
public class ResponseChats : Packet
{
    [Key(0)] public List<Chat> Chats { get; set; } = [];
}

[MessagePackObject]
public class UserChangedStatus : Packet
{
    [Key(0)] public ulong UserId { get; set; }
    [Key(1)] public UserStatus NewStatus { get; set; }
}

[MessagePackObject]
public class MessageSentDm : Packet
{
    [Key(0)] public ulong FromUser { get; set; }
    [Key(1)] public ulong ToUser { get; set; }
    [Key(2)] public Message Message { get; set; }
}

public enum JoinStatus
{
    Joined,
    InvalidToken,
    TokenInUse,
    IntroduceYourself
}