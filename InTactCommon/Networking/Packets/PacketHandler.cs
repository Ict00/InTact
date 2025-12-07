using LiteNetLib;

namespace InTactCommon.Networking.Packets;

public delegate void PacketNetHandler<T>(T packet, NetPeer peer) where T : Packet;

public class PacketHandler
{
    private Dictionary<Type, List<Delegate>> _handlers = [];
    public Action<Packet>? UnhandledCallback = null;
    
    public void Attach<T>(PacketNetHandler<T> handler) where T : Packet
    {
        if (!_handlers.ContainsKey(typeof(T)))
        {
            _handlers[typeof(T)] = [];
        }
        _handlers[typeof(T)].Add(handler);
    }

    public void SetOnUnhandled(Action<Packet> callback)
    {
        UnhandledCallback = callback;
    }

    public void Handle(Packet packet, NetPeer peer)
    {
        if (_handlers.TryGetValue(packet.GetType(), out var handler))
        {
            handler.ForEach(x => x.DynamicInvoke(packet, peer));
            return;
        }

        if (packet is BatchPacket batch)
        {
            foreach (var i in batch.Packets)
            {
                Handle(i, peer);
            }

            return;
        }

        UnhandledCallback?.Invoke(packet);
    }
}