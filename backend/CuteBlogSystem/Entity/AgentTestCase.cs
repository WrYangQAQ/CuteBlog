using CuteBlogSystem.DTO.Agent;
using System.Text.Json;

namespace CuteBlogSystem.Entity
{
    // 测试用例
    public class AgentTestCase
    {
        public int Id { get; set; }

        public string CaseName { get; set; } = string.Empty;

        public string UserMessage { get; set; } = string.Empty;

        public string? SessionId { get; set; }

        public string ExpectedActionsJson { get; set; } = "[]";

        public bool ExpectedSuccess { get; set; }

        public string ExpectedAnswerContainsJson { get; set; } = "[]";

        public bool ExpectedRequiresConfirmation { get; set; }

        public string? ExpectedAnswerSummary { get; set; }

        public bool EnableSemanticJudge { get; set; }

        public double SemanticJudgeThreshold { get; set; } = 0.7;

        public bool IsEnabled { get; set; } = true;

        public string? Category { get; set; }

        public string? Remark { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public AgentTestCase() { }

        public AgentTestCase(AgentTestCaseAddDto caseDto, string expectedActionsJson, string expectedAnswerContatins)
        {
            CaseName = caseDto.CaseName;
            UserMessage = caseDto.UserMessage;
            ExpectedSuccess = caseDto.ExpectSuccess;
            ExpectedRequiresConfirmation = caseDto.ExpectRequiresConfirmation;
            EnableSemanticJudge = caseDto.EnableSemanticJudge;
            SemanticJudgeThreshold = caseDto.SemanticJudgeThreshold;
            ExpectedActionsJson = expectedActionsJson;
            ExpectedAnswerContainsJson = expectedAnswerContatins;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
            SessionId = caseDto.SessionId;
            ExpectedAnswerSummary = caseDto.ExpectedAnswerSummary;
            Category = caseDto.Category;
            Remark = caseDto.Remark;
        }
    }
}
