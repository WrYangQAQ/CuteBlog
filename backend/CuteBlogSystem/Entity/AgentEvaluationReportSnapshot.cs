namespace CuteBlogSystem.Entity
{
    public class AgentEvaluationReportSnapshot
    {
        // 主键 ID
        public long Id { get; set; }

        // 评估批次 ID
        public long RunId { get; set; }

        // 评估报告文件名
        public string FileName { get; set; } = string.Empty;

        // 评估报告内容
        public string MarkdownContent { get; set; } = string.Empty;

        // 计划器提示版本
        public string PlannerPromptVersion { get; set; } = "planner-prompt-v1";

        // 任务提示版本
        public string ActionRegistryVersion { get; set; } = "action-registry-v1";

        // 评估版本
        public string EvaluationVersion { get; set; } = "evaluation-v1";

        // 最终答案提示版本
        public string FinalAnswerPromptVersion { get; set; } = "final-answer-v1";

        // 快照生成时间
        public DateTime CreatedAt { get; set; }

        // 快照是否被删除
        public bool IsDeleted { get; set; }

        // 评估批次导航属性
        public AgentEvaluationRun? Run { get; set; }
    }
}
