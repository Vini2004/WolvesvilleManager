namespace WolvesvilleManager.Domain.Wolvesville;

/// <summary>Mensagem do chat do clã (GET/POST /clans/{id}/chat).</summary>
public class ChatMessage
{
    public string? PlayerId { get; set; }
    public string? PlayerBotId { get; set; }
    public string? PlayerBotOwnerUsername { get; set; }
    public string? Msg { get; set; }
    public string? EmojiId { get; set; }
    public bool IsSystem { get; set; }
    public string? Date { get; set; }
    public bool? IsPinned { get; set; }
}

/// <summary>Anúncio do clã (GET/POST /clans/{id}/announcements).</summary>
public class ClanAnnouncement
{
    public string? Id { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public string? AuthorId { get; set; }
    public string? PlayerBotAuthorId { get; set; }
    public string? Timestamp { get; set; }
    public string? EditTimestamp { get; set; }
    public string? EditAuthor { get; set; }
    public string? EditAuthorId { get; set; }
}
