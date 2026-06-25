using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentUserMessage
    {
        // Conversation 的 SessionId，用于关联用户消息到特定的对话会话
        public string SessionId { get; set; } = string.Empty;

        // 用户ID
        public int UserId { get; set; }

        // 用户发送的消息内容
        public string Content { get; set; } = string.Empty;

        // 用户身份
        public AgentMessageRole Role { get; set; } = AgentMessageRole.User;
    }
}
