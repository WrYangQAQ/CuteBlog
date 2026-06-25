namespace CuteBlogSystem.Entity
{
    public class AgentWorkflowLog
    {
        // 日志记录的唯一标识符
        public int Id { get; set; }

        // 用户输入的原始消息
        public string UserMessage { get; set; } = string.Empty;

        // 整个工作流是否成功（原始计划全部步骤成功）
        public bool Success { get; set; }

        // 是否通过补救计划恢复了任务（仅当 Success 为 false 时有效）
        public bool Recovered { get; set; }

        // 对工作流结果的简短描述消息
        public string Message { get; set; } = string.Empty;

        // 最终返回给用户的答案文本
        public string Answer { get; set; } = string.Empty;

        // 使用的计划 JSON 字符串（可能经过修复）
        public string? PlanJson { get; set; }

        // 计划执行结果的 JSON 字符串
        public string? ExecutionResultJson { get; set; }

        // 执行失败时的分析文本
        public string? FailureAnalysis { get; set; }

        // 补救计划的 JSON 字符串（仅在失败后生成）
        public string? RecoveryPlanJson { get; set; }

        // 补救计划执行结果的 JSON 字符串
        public string? RecoveryExecutionResultJson { get; set; }

        // 工作流开始时间
        public DateTime StartedAt { get; set; }

        // 工作流结束时间
        public DateTime FinishedAt { get; set; }

        // 总耗时（毫秒）
        public long DurationMs { get; set; }
    }
}