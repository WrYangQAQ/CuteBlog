using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Entity
{
    // 单条评估结果
    public class AgentEvaluationResult
    {
        public long Id { get; set; }

        public long RunId { get; set; }

        public int TestCaseId { get; set; }

        public int? WorkflowLogId { get; set; }

        public string CaseName { get; set; } = string.Empty;

        public bool Passed { get; set; }

        public string ErrorsJson { get; set; } = "[]";

        public string Answer { get; set; } = string.Empty;

        public string ActualActionsJson { get; set; } = "[]";

        public bool ActualSuccess { get; set; }

        public bool ActualRequiresConfirmation { get; set; }

        public double? SemanticScore { get; set; }

        public string? SemanticJudgeReason { get; set; }

        public bool? SemanticJudgePassed { get; set; }

        public AgentEvaluationFailureType FailureType { get; set; } = AgentEvaluationFailureType.None;

        public DateTime CreatedAt { get; set; }

        public string TestCaseSnapshotJson { get; set; } = "{}";

        public AgentEvaluationRun? Run { get; set; }

        public AgentTestCase? TestCase { get; set; }
    }
}
