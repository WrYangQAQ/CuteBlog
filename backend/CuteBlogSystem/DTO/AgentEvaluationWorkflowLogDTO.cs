using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class AgentEvaluationWorkflowLogDTO
    {
        public int Id { get; set; }

        // 用户原始问题
        public string UserMessage { get; set; } = string.Empty;

        // 执行状态
        public bool Success { get; set; }

        public bool Recovered { get; set; }

        public string Message { get; set; } = string.Empty;

        // 最终回答
        public string Answer { get; set; } = string.Empty;

        // 耗时与时间
        public long DurationMs { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        // 原始计划
        public string? PlanJson { get; set; }

        // 原始计划执行结果
        public string? ExecutionResultJson { get; set; }

        // 失败分析
        public string? FailureAnalysis { get; set; }

        // 补救计划
        public string? RecoveryPlanJson { get; set; }

        // 补救计划执行结果
        public string? RecoveryExecutionResultJson { get; set; }

        public AgentEvaluationWorkflowLogDTO()
        {
        }

        public AgentEvaluationWorkflowLogDTO(AgentWorkflowLog log)
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
