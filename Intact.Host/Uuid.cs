namespace Intact.Host;

public static class Uuid
{
    private static Dictionary<Dm, ulong> _lastForDms = [];
    private static Dictionary<ulong, ulong> _lastForChats = [];
    
    public static ulong GetNext(Dm dm)
    {
        _lastForDms.TryAdd(dm, 0);
        
        var ret = _lastForDms[dm];
        _lastForDms[dm]++;
        
        return ret;
    }

    public static ulong GetNext(ulong chatId)
    {
        _lastForChats.TryAdd(chatId, 0);
        var ret = _lastForChats[chatId];
        _lastForChats[chatId]++;
        return ret;
    }
}