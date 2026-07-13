using CuteBlogSystem.AI.Planner;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentAskResponse
    {
        // 表示计划执行是否成功（原始计划全部步骤成功）
        public bool Success { get; set; }

        // 表示是否通过补救计划恢复了任务（仅当 Success 为 false 时有效）
        public bool Recovered { get; set; }

        // 对执行结果的简短描述消息，如“执行成功”或“已生成补救建议”
        public string Message { get; set; } = string.Empty;

        // 该条执行可能存在的工作流 Id（为方便进行日志追踪）
        public int? WorkflowLogId { get; set; }

        // 最终返回给用户的答案文本（来自原始计划或补救计划的 FinalAnswer）
        public string Answer { get; set; } = string.Empty;

        // 调试信息，仅在需要详细诊断时提供，生产环境可忽略
        public AgentDebugInfo? Debug { get; set; }

        // 表示是否需要用户确认计划中的高权限操作（如删除、修改敏感内容）
        public bool RequiresConfirmation { get; set; }

        // 当 RequiresConfirmation 为 true 时，对应的确认请求唯一标识符
        public string? ConfirmationId { get; set; }

        // 待确认的操作摘要，用于向用户展示确认内容（如“计划删除文章 ID: 123”）
        public string? ConfirmationSummary { get; set; }
    }

    public class AgentDebugInfo
    {
        // 本次执行使用的计划（可能是原始计划或修复后的计划）
        public AgentPlan? Plan { get; set; }

        // 计划执行的结果（包含每个步骤的详细信息）
        public AgentPlanExecutionResult? ExecutionResult { get; set; }

        // 当执行失败时，AI 生成的失败原因分析文本
        public string? FailureAnalysis { get; set; }

        // 当原始计划失败时生成的补救计划
        public AgentPlan? RecoveryPlan { get; set; }

        // 补救计划的执行结果（包含补救步骤的详细信息）
        public AgentPlanExecutionResult? RecoveryExecutionResult { get; set; }

        // 原始计划验证失败时的错误列表
        public List<string>? ValidationErrors { get; set; }

        // 补救计划验证失败时的错误列表
        public List<string>? RecoveryErrors { get; set; }
    }
}