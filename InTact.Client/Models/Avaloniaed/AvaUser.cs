using Avalonia.Media;
using InTact.Client.Net;
using InTactCommon.Objects;

namespace InTact.Client.Models.Avaloniaed;

public class AvaUser
{
    public string Name { get; set; }
    public IBrush Avatar { get; set; }
    public IBrush StatusColor { get; set; }
    public ulong Id { get; set; }
    
    public static AvaUser FromUser(User user)
    {
        var usr = Global.client.Users.Find(x => x.Id == user.Id);
        var name = usr?.Username ?? "Null";
        IBrush statusColor = usr == null ? Brushes.Gray : usr.Status == UserStatus.Offline ? Brushes.DarkSlateGray : Brushes.Green;
        IBrush avatar = usr == null
            ? Brushes.Coral : new SolidColorBrush(Color.FromRgb((byte)user.PfpColor.R, (byte)user.PfpColor.G, (byte)user.PfpColor.B)); 
        
        return new AvaUser()
        {
            Name = name,
            StatusColor = statusColor,
            Avatar = avatar,
            Id = user.Id,
        };
    }

    public static AvaUser WithStatus(AvaUser usr, UserStatus status)
    {
        IBrush statusColor = status == UserStatus.Offline ? Brushes.DarkSlateGray : Brushes.Green;
        return new AvaUser()
        {
            StatusColor = statusColor,
            Name = usr.Name,
            Id = usr.Id,
            Avatar = usr.Avatar,
        };
    }
}