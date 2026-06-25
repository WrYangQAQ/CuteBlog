using CuteBlogSystem.AI.Planner;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentConfirmedPlan
    {
        // Agent 接收用户信息后生成计划
        public AgentPlan Plan { get; set; } = new();

        // 该会话的ID
        public string SessionId { get; set; } = string.Empty;

        // 发起会话用户 ID
        public string UserId { get; set; } = string.Empty;

        // 用户发送消息内容
        public string UserMessage { get; set; } = string.Empty;
    }
}
