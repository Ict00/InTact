using InTactCommon.Networking.Packets;
using InTactCommon.Objects;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;

namespace InTact.Client.CLI.Utils;

public class ClientNode
{
    int _port;
    private string _ip;
    string _secret;
    string _token;

    public readonly NetDataWriter _writer = new();
    public readonly PacketHandler _handler = new();
    private readonly EventBasedNetListener _listener = new();
    public readonly NetManager _client;
    
    public List<User> Users = new();
    public List<Chat>  Chats = new();

    public bool Joined = false;
    public ulong SelfId = 8;
    public NetPeer? Server = null;

    public User GetByIdNotNull(ulong id)
    {
        return Users.Find(x => x.Id == id)!;
    }
    
    public ClientNode(int port, string ip, string secret, string token)
    {
        _port = port;
        _ip = ip;
        _secret = secret;
        _token = token;
        
        _client = new NetManager(_listener);
        
        _handler.Attach<MessageSentPacket>((x, y) =>
        {
            var chat = Chats.Find(z => z.Id == x.ChatId)!;
            
            chat.Messages.Add(x.MessageSent);
        });

        _handler.Attach<ResponseChats>((x, y) =>
        {
            Chats = x.Chats;
        });
        
        _handler.Attach<ResponseUsers>((x, y) =>
        { 
            Users = x.Users;
        });

        _handler.Attach<ResponseChat>((x, y) =>
        {
            try
            {
                Chats.Find(z => z.Id == x.Chat.Id)!.Messages = x.Chat.Messages;
            } catch { /* Ignore*/ }
        });
        
        _handler.Attach<JoinServerResponse>((x, y) =>
        {
            if (x.Status == JoinStatus.Joined)
            {
                Joined = true;
                
                // Request other stuff; For now - don't
                
                new RequestChats
                {
                    OnlyLastAmount = 50
                }.SendPacketTo(Server!, _writer);
                
                new RequestUsers().SendPacketTo(Server!, _writer);

                SelfId = x.User;
                
                Console.WriteLine($"User: {x.User}");
            }
            else if (x.Status == JoinStatus.Pending)
            {
                /* Do nothing */
            }
            else
            {
                try
                {
                    Server?.Disconnect();
                    _client.Stop();
                }
                catch (Exception) { /* Ignore */ }

                Environment.Exit(0);
            }
        });
        
        _listener.PeerConnectedEvent += PeerConnected;
        _listener.PeerDisconnectedEvent += PeerDisconnected;
        _listener.NetworkReceiveEvent += NetworkReceiveEvent;
    }

    private void PeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        
    }
    
    private void PeerConnected(NetPeer peer)
    {
        new JoinServerRequest()
        {
            Token = _token
        }.SendPacketTo(Server!, _writer);
    }
    
    private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        byte[] rawData = reader.GetRemainingBytes();
                
        try
        {
            var packet = MessagePackSerializer.Deserialize<Packet>(rawData);
            
            _handler.Handle(packet, peer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            
        }
    }

    public void Poll()
    {
        _client.PollEvents();
        Thread.Sleep(20);
    }

    public void Start()
    {
        _client.Start();
        Server = _client.Connect(_ip, _port, _secret);
    }
}