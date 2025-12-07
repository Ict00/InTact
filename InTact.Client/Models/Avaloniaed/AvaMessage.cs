using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using InTact.Client.Net;
using InTactCommon.Objects;

namespace InTact.Client.Models.Avaloniaed;

public class AvaMessage
{
    public string Author { get; set; }
    public string Date { get; set; }
    public string Content { get; set; }
    public IBrush AuthorColor { get; set; }
    public IBrush MentionedColor { get; set; }

    public static AvaMessage FromMessage(Message msg)
    {
        var authorUser = Global.client.Users.Find(x => x.Id == msg.AuthorId);
        var author = authorUser == null ? "NULL" : authorUser.Username;
        IBrush authorColor = authorUser == null ? Brushes.Black : new SolidColorBrush()
        {
            Color = Color.FromRgb((byte)authorUser.PfpColor.R, (byte)authorUser.PfpColor.G, (byte)authorUser.PfpColor.B),
        };
        string date = DateTime.FromBinary(msg.Timestamp).ToString("HH:mm:ss");
        IBrush mentiones = msg.Mentions.Contains(Global.client.SelfId) ? Brushes.Coral : Brushes.Transparent;

        return new AvaMessage()
        {
            Author = author,
            Date = date,
            Content = msg.Content,
            AuthorColor = authorColor,
            MentionedColor = mentiones,
        };
    }
}