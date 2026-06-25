using CuteBlogSystem.Enum;
using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentConversationListDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string ModelUsed { get; set; } = string.Empty;
        public AgentConversationStatus Status { get; set; }

        public AgentConversationListDto(AgentConversation conversation)
        {
            SessionId = conversation.SessionId;
            Title = conversation.Title;
            ModelUsed = conversation.ModelUsed;
            Status = conversation.Status;
            UpdatedAt = conversation.UpdatedAt;
        }
    }
}
