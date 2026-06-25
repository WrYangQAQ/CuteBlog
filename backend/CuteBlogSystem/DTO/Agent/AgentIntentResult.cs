using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentIntentResult
    {
        public AgentIntentResult(AgentIntentType intent, double confidence, string reason)
        {
            Intent = intent;
            Confidence = confidence;
            Reason = reason;
        }

        // 识别出的用户意图类型（如查询文章、总结、对比等）
        public AgentIntentType Intent { get; set; }

        // 意图识别的置信度（0~1之间的浮点数，越高表示越确信）
        public double Confidence { get; set; }

        // 意图判断的理由或解释文本（用于调试和日志）
        public string Reason { get; set; } = string.Empty;
    }
}
