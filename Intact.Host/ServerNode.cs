using InTactCommon.Networking.Packets;
using InTactCommon.Objects;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;

namespace Intact.Host;

public record Dm(ulong User1, ulong User2);

public class ServerNode
{
    
    int _port;
    string _secret;

    private readonly NetDataWriter _writer = new();
    private readonly PacketHandler _handler = new();
    private readonly EventBasedNetListener _listener = new();
    private readonly NetManager _server;
    
    private List<UserToken> _userTokens = new();
    private List<User> _users = new();
    private List<Chat>  _chats = new();

    private Dictionary<int, ulong> _peerToUser = [];
    private Dictionary<Dm, List<Message>> dms = [];

    private int GetPeerFromUser(ulong userId)
    {
        foreach (var i in _peerToUser)
        {
            if (i.Value == userId)
            {
                return i.Key;
            }
        }

        return -1;
    }
    
    private User GetByIdNotNull(ulong id)
    {
        return _users.Find(x => x.Id == id)!;
    }

    private User? GetByIdNull(ulong id)
    {
        return _users.Find(x => x.Id == id);
    }
    
    public ServerNode(int port, string secret, List<UserToken> userTokens, List<User> users, List<ServerChatInfo> chatInfos)
    {
        _port = port;
        _secret = secret;
        _server = new NetManager(_listener);
        
        _userTokens = userTokens;
        _users = users;
        
        chatInfos.ForEach(x =>
        {
            _chats.Add(new Chat()
            {
                Id = x.Id,
                Name = x.Name,
            });
        });
        
        _handler.Attach<MessageSentPacket>((x, y) =>
        {
            if (_peerToUser.ContainsKey(y.Id) && x.MessageSent.AuthorId == _peerToUser[y.Id])
            {
                x.MessageSent.Id = Uuid.GetNext(x.ChatId);
                
                x.SendPacketToAllAuthorized(_server, _peerToUser.Keys.ToList(), _writer);

                var chat = _chats.Find(z => z.Id == x.ChatId);
                chat?.Messages.Add(x.MessageSent);
            }
        });
        
        _handler.Attach<MessageSentDm>((x, y) =>
        {
            if (_peerToUser.ContainsKey(y.Id))
            {
                var usr1 = GetByIdNull(x.FromUser);
                var usr2 = GetByIdNull(x.ToUser);

                if (usr1 == null || usr2 == null) return;
                
                var p1 = GetPeerFromUser(usr1.Id);
                var p2 = GetPeerFromUser(usr2.Id);
                
                if (dms.ContainsKey(new(usr1.Id, usr2.Id)))
                {
                    dms[new(usr1.Id, usr2.Id)].Add(x.Message);
                }
                else if (dms.ContainsKey(new(usr2.Id, usr1.Id)))
                {
                    dms[new(usr2.Id, usr1.Id)].Add(x.Message);
                }
                else
                {
                    dms[new(usr1.Id, usr2.Id)] = [x.Message];
                }
                
                x.Message.Id = Uuid.GetNext(new Dm(usr1.Id, usr2.Id));
                if (p2 != -1) 
                    x.SendPacketToAllAuthorized(_server, [p1, p2], _writer);
                else
                    x.SendPacketTo(y, _writer);
            }
        });
        
        _handler.Attach<RequestUsers>((x, y) =>
        {
            if (_peerToUser.ContainsKey(y.Id))
            {
                new ResponseUsers()
                {
                    Users = _users,
                }.SendPacketTo(y, _writer);
            }
        });

        _handler.Attach<RequestChats>((x, y) =>
        {
            if (_peerToUser.ContainsKey(y.Id))
            {
                int amount = x.OnlyLastAmount;
                List<Chat> returnChats = new();

                foreach (var i in _chats)
                {
                    var newMessages = i.Messages.Slice(0, amount > i.Messages.Count ? i.Messages.Count : amount);
                    returnChats.Add(new Chat()
                    {
                        Id = i.Id,
                        Messages = newMessages,
                        Name = i.Name,
                    });
                }
                
                new ResponseChats()
                {
                    Chats = returnChats,
                }.SendPacketTo(y, _writer);
                
                returnChats.Clear();
                
                var usr = _peerToUser[y.Id];
                
                Dictionary<ulong, List<Message>> _dms = new();
                foreach (var i in dms)
                {
                    if (i.Key.User1 == usr || i.Key.User2 == usr)
                    {
                        var otherId = i.Key.User1 == usr ? i.Key.User2 : i.Key.User1;
                        _dms.Add(otherId, i.Value);
                    }
                }
                new ResponseDms()
                {
                    DMs = _dms
                }.SendPacketTo(y, _writer);
                
                _dms.Clear();
            }
        });
        
        _handler.Attach<RequestChat>((x, y) =>
        {
            if (_peerToUser.ContainsKey(y.Id))
            {
                int amount = x.Amount;

                foreach (var i in _chats)
                {
                    if (i.Id == x.ChatId)
                    {
                        var newMessages = i.Messages.Slice(0, amount > i.Messages.Count ? i.Messages.Count : amount);
                        new ResponseChat()
                        {
                            Chat = new Chat()
                            {
                                Id = i.Id,
                                Messages = newMessages,
                                Name = i.Name,
                            }
                        }.SendPacketTo(y, _writer);
                        return;
                    }
                }
            }
        });

        _handler.Attach<IntroductionUser>((x, y) =>
        {
            var user = _userTokens.Find(z => z.Token == x.Token);
            if (user != null)
            {
                var isUsrHere = GetByIdNull(user.Id) != null;
                
                if (isUsrHere)
                {
                    new JoinServerResponse()
                    {
                        User = 0,
                        Status = JoinStatus.TokenInUse
                    }.SendPacketTo(y, _writer);
                }
                else
                {
                    var newUser = new User()
                    {
                        Status = UserStatus.Online,
                        Username = x.Username,
                        Id = user.Id,
                        PfpColor = x.PfpColor,
                    };
                    _users.Add(newUser);
                    new UserAdded()
                    {
                        User = newUser
                    }.SendPacketToAllAuthorized(_server, _peerToUser.Keys.ToList(), _writer);
                    
                    new JoinServerResponse()
                    {
                        User = newUser.Id,
                        Status = JoinStatus.Joined
                    }.SendPacketTo(y, _writer);
                    
                    _peerToUser[y.Id] = newUser.Id;
                }
            }
            else
            {
                new JoinServerResponse()
                {
                    User = 0,
                    Status = JoinStatus.InvalidToken
                }.SendPacketTo(y, _writer);
            }
        });
        
        _handler.Attach<JoinServerRequest>((x, y) =>
        {
            var user = _userTokens.Find(z => z.Token == x.Token);
            
            if (user != null)
            {
                var inChatUser = GetByIdNull(user.Id);
                if (inChatUser != null)
                {
                    if (inChatUser.Status == UserStatus.Online)
                    {
                        new JoinServerResponse()
                        {
                            User = 0,
                            Status = JoinStatus.TokenInUse
                        }.SendPacketTo(y, _writer);
                        return;
                    }

                    inChatUser.Status = UserStatus.Online;

                    new JoinServerResponse()
                    {
                        User = inChatUser.Id,
                        Status = JoinStatus.Joined
                    }.SendPacketTo(y, _writer);

                    new UserChangedStatus()
                    {
                        NewStatus = UserStatus.Online,
                        UserId = inChatUser.Id,
                    }.SendPacketToAllAuthorized(_server, _peerToUser.Keys.ToList(), _writer);

                    _peerToUser[y.Id] = inChatUser.Id;
                }
                else
                {
                    new JoinServerResponse()
                    {
                        User = 0,
                        Status = JoinStatus.IntroduceYourself
                    }.SendPacketTo(y, _writer);
                }
            }
            else
            {
                new JoinServerResponse()
                {
                    User = 0,
                    Status = JoinStatus.InvalidToken
                }.SendPacketTo(y, _writer);
            }
        });
        
        _listener.ConnectionRequestEvent += ConnectionRequest;
        _listener.PeerConnectedEvent += PeerConnected;
        _listener.PeerDisconnectedEvent += PeerDisconnected;
        _listener.NetworkReceiveEvent += NetworkReceiveEvent;
    }

    private void ConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(_secret);
    }

    private void Disconnect(int peerId)
    {
        var user = _peerToUser[peerId];
        GetByIdNotNull(user).Status =  UserStatus.Offline;
        _peerToUser.Remove(peerId);
        new UserChangedStatus()
        {
            NewStatus = UserStatus.Offline,
            UserId = user
        }.SendPacketToAllAuthorized(_server, _peerToUser.Keys.ToList(), _writer);
    }

    private void PeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"Peer {peer.Id} disconnected");
        try { Disconnect(peer.Id); } catch { /* Ignore */ }
    }
    
    private void PeerConnected(NetPeer peer)
    {
        Console.WriteLine($"Peer {peer.Id} connected");
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

    public void Start()
    {
        Console.WriteLine($"Starting server");
        _server.Start(_port);
        
        while (true)
        {
            _server.PollEvents();
            Thread.Sleep(20);
        }
    }
}