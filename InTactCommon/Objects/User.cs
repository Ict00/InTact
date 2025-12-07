using System.Drawing;
using MessagePack;

namespace InTactCommon.Objects;

[MessagePackObject]
public class User
{
    [Key(0)] public ulong Id { get; set; }
    [Key(1)] public string Username { get; set; } = "";
    [Key(2)] public SColor PfpColor { get; set; }
    [Key(3)] public UserStatus Status { get; set; }

    public User()
    {
        
    }
}

[MessagePackObject]
public class SColor
{
    [Key(0)] public uint R { get; set; }
    [Key(1)] public uint G { get; set; }
    [Key(2)] public uint B { get; set; }
}

public static class ColorExts
{
    public static SColor ToSColor(this Color color)
    {
        return new SColor()
        {
            R = color.R,
            G = color.G,
            B = color.B,
        };
    }

    public static Color ToColor(this SColor color)
    {
        return Color.FromArgb(255,  (int)color.R, (int)color.G, (int)color.B);
    }
}

public enum UserStatus
{
    Online,
    Offline
}