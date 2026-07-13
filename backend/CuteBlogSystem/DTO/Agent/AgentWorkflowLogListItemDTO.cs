using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentWorkflowLogListItemDTO
    {
        // 日志ID
        public long Id { get; set; }

        // 用户输入消息
        public string UserMessage { get; set; } = string.Empty;

        // Agent 工作流执行是否成功
        public bool Success { get; set; }

        // Agent 是否进行了计划恢复
        public bool Recovered { get; set; }

        // Agent 回复信息
        public string Message { get; set; } = string.Empty;

        // 从用户输入到 Agent 输出的总耗时
        public long DurationMs { get; set; }

        // Agent Workflow 开始时间
        public DateTime StartedAt { get; set; }

        // Agent Workflow 结束时间
        public DateTime FinishedAt { get; set; }

        public AgentWorkflowLogListItemDTO(AgentWorkflowLog log)
        {
            Id = log.Id;
            UserMessage = log.UserMessage;
            Success = log.Success;
            Recovered = log.Recovered;
            Message = log.Message;
            DurationMs = log.DurationMs;
            StartedAt = log.StartedAt;
            FinishedAt = log.FinishedAt;
        }
    }

    public class AgentWorkflowLogDetailDTO
    {
        public long Id { get; set; }

        public string UserMessage { get; set; } = string.Empty;

        public bool Success { get; set; }

        public bool Recovered { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public long DurationMs { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public string? PlanJson { get; set; }

        public string? ExecutionResultJson { get; set; }

        public string? FailureAnalysis { get; set; }

        public string? RecoveryPlanJson { get; set; }

        public string? RecoveryExecutionResultJson { get; set; }

        public AgentWorkflowLogDetailDTO(AgentWorkflowLog log)
        {
            Id = log.Id;
            UserMessage = log.UserMessage;
            Success = log.Success;
            Recovered = log.Recovered;
            Message = log.Message;
            Answer = log.Answer;
            DurationMs = log.DurationMs;
            StartedAt = log.StartedAt;
            FinishedAt = log.FinishedAt;
            PlanJson = log.PlanJson;
            ExecutionResultJson = log.ExecutionResultJson;
            FailureAnalysis = log.FailureAnalysis;
            RecoveryPlanJson = log.RecoveryPlanJson;
            RecoveryExecutionResultJson = log.RecoveryExecutionResultJson;
        }
    }
}
