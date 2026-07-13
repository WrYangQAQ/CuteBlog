namespace CuteBlogSystem.Entity
{
    // 评估批次
    public class AgentEvaluationRun
    {
        public long Id { get; set; }

        public long? SourceId { get; set; }

        public int TotalCount { get; set; }

        public int PassedCount { get; set; }

        public int FailedCount { get; set; }

        public string? ModelUsed { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public string? Remark { get; set; }

        // 计划器提示版本
        public string? PlannerPromptVersion { get; set; } = "planner-prompt-v1";

        // 任务提示版本
        public string? ActionRegistryVersion { get; set; } = "action-registry-v1";

        // 评估版本
        public string? EvaluationVersion { get; set; } = "evaluation-v1";

        // 最终答案提示版本
        public string? FinalAnswerPromptVersion { get; set; } = "final-answer-v1";

        // 如果为旧批次复现，存在来源批次导航属性
        public AgentEvaluationRun? SourceRun { get; set; }
    }
}
