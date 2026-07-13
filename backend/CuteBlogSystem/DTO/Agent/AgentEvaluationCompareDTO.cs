namespace CuteBlogSystem.DTO.Agent
{
    public class AgentEvaluationCompareDTO
    {
        // 基准运行的 ID
        public long BaseRunId { get; set; }

        // 目标运行的 ID
        public long TargetRunId { get; set; }

        // 基准运行的详情数据
        public AgentEvaluationCompareRunDTO? BaseRun { get; set; }

        // 目标运行的详情数据
        public AgentEvaluationCompareRunDTO? TargetRun { get; set; }

        // 从失败变为成功的用例数（修复数）
        public int FixedCount { get; set; }

        // 从成功变为失败的用例数（退步数）
        public int RegressedCount { get; set; }

        // 两次运行均通过的用例数
        public int StillPassedCount { get; set; }

        // 两次运行均失败的用例数
        public int StillFailedCount { get; set; }

        // 目标运行中新增的用例数（基准中不存在）
        public int NewCaseCount { get; set; }

        // 目标运行中缺失的用例数（基准中存在但目标中不存在）
        public int MissingCaseCount { get; set; }

        // 每个测试用例的详细对比结果列表
        public List<AgentEvaluationCompareCaseDTO> Cases { get; set; } = new();
    }

    public class AgentEvaluationCompareRunDTO
    {
        // 运行记录的 ID
        public long RunId { get; set; }

        // 该批次的总用例数
        public int TotalCount { get; set; }

        // 通过的用例数
        public int PassedCount { get; set; }

        // 失败的用例数
        public int FailedCount { get; set; }

        // 运行开始时间
        public DateTime StartedAt { get; set; }

        // 运行结束时间（可为空）
        public DateTime? FinishedAt { get; set; }

        // 运行时使用的 Planner 提示词版本
        public string? PlannerPromptVersion { get; set; }

        // 运行时使用的动作注册表版本
        public string? ActionRegistryVersion { get; set; }

        // 运行时使用的评估逻辑版本
        public string? EvaluationVersion { get; set; }

        // 运行时使用的最终答案生成提示词版本
        public string? FinalAnswerPromptVersion { get; set; }
    }

    public class AgentEvaluationCompareCaseDTO
    {
        // 测试用例的数据库 ID
        public int TestCaseId { get; set; }

        // 测试用例名称
        public string CaseName { get; set; } = string.Empty;

        // 基准运行是否通过
        public bool? BasePassed { get; set; }

        // 目标运行是否通过
        public bool? TargetPassed { get; set; }

        // 基准运行的语义评分（0~1，可为空）
        public double? BaseSemanticScore { get; set; }

        // 目标运行的语义评分（0~1，可为空）
        public double? TargetSemanticScore { get; set; }

        // 变化类型：Fixed/Regressed/StillPassed/StillFailed/New/Missing
        public string ChangeType { get; set; } = string.Empty;

        // 基准运行的失败类型（如 ActionMismatch、SemanticLowScore 等）
        public string? BaseFailureType { get; set; }

        // 目标运行的失败类型
        public string? TargetFailureType { get; set; }

        // 基准运行的最终回答内容
        public string? BaseAnswer { get; set; }

        // 目标运行的最终回答内容
        public string? TargetAnswer { get; set; }

        // 基准运行的实际动作列表（JSON 字符串）
        public string BaseActionsJson { get; set; } = "[]";

        // 目标运行的实际动作列表（JSON 字符串）
        public string TargetActionsJson { get; set; } = "[]";
    }
}
