namespace CuteBlogSystem.DTO.Agent
{
    public class AgentConfirmationRequest
    {
        // 会话id
        public string SessionId { get; set; } = string.Empty;

        // plan待批准记录请求id
        public string ConfirmationId { get; set; } = string.Empty;
    }
}
