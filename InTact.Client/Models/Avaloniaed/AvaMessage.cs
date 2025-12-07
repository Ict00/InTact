using Avalonia.Media;

namespace InTact.Client.Models.Avaloniaed;

public class AvaMessage
{
    public string Author { get; set; }
    public string Date { get; set; }
    public string Content { get; set; }
    public Brush AuthorColor { get; set; }
    public Brush MentionedColor { get; set; }
    
}