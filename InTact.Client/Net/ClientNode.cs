using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using InTactCommon.Networking.Packets;
using InTactCommon.Objects;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;

namespace InTact.Client.Net;


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
    
    public Dictionary<ulong, List<Message>> DmMessages = new();

    public User GetByIdNotNull(ulong id)
    {
        return Users.Find(x => x.Id == id)!;
    }

    public delegate bool Condition();

    public void WaitTillLoaded(Condition additional)
    {
        while ((!Global.client.Joined || Global.client.Chats.Count == 0 || Global.client.Users.Count == 0) && additional())
        {
            Global.client.Poll();
        }
    }
    
    public ClientNode(int port, string ip, string secret, string token)
    {
        _port = port;
        _ip = ip;
        _secret = secret;
        _token = token;
        
        _client = new NetManager(_listener);
        
        _handler.Attach<ResponseDms>((x, y) =>
        {
            DmMessages = x.DMs;
        });
        
        _handler.Attach<MessageSentDm>((x, y) =>
        {
            if (x.FromUser == Global.client.SelfId)
            {
                DmMessages.TryAdd(x.ToUser, []);
                DmMessages[x.ToUser].Add(x.Message);
                return;
            }
            
            DmMessages.TryAdd(x.FromUser, []);
            
            DmMessages[x.FromUser].Add(x.Message);
        });
        
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
        
        _handler.Attach<UserAdded>((x, y) =>
        { 
            Users.Add(x.User);
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
            }
            else if (x.Status == JoinStatus.IntroduceYourself)
            {
                
            }
            else
            {
                try
                {
                    _client.Stop();
                    Server?.Disconnect();
                }
                catch (Exception e) { Console.WriteLine(e); }
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