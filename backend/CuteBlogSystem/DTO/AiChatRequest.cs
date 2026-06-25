namespace CuteBlogSystem.DTO
{
    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;

        public string? ConversationId { get; set; }
    }
}
