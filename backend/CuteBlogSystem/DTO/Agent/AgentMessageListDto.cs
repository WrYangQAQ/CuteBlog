using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentMessageListDto
    {
        // 消息唯一标识
        public long MessageId { get; set; }

        // 所属会话ID（如需在前端分组或定位会话，可保留）
        public string SessionId { get; set; } = string.Empty;

        // 消息角色枚举值（便于前端做逻辑判断）
        public AgentMessageRole Role { get; set; }

        // 消息内容（支持纯文本或 Markdown）
        public string Content { get; set; } = string.Empty;
        
        // 原始创建时间（UTC 或服务器时间）
        public DateTime CreatedAt { get; set; }

        public AgentMessageListDto(AgentMessage message)
        {
            MessageId = message.MessageId;
            SessionId = message.SessionId;
            Role = message.Role;
            Content = message.Content;
            CreatedAt = message.CreatedAt;
        }
    }


}
