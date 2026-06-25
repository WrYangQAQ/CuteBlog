using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Entity
{
    public class AgentConversation
    {
        public string SessionId { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public string Title { get; set; } = "新对话";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string ModelUsed { get; set; } = string.Empty;

        public AgentConversationStatus Status { get; set; } = AgentConversationStatus.Active;

        public List<AgentMessage> Messages { get; set; } = new();

        public AgentConversationMemory? Memory { get; set; }
    }
}