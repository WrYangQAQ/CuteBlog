using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentTestCaseSnapshotDto
    {
        // 测试用例的数据库 ID
        public int CaseId { get; set; }

        // 测试用例名称
        public string CaseName { get; set; } = string.Empty;

        // 模拟用户发送的消息内容
        public string UserMessage { get; set; } = string.Empty;

        // 可选的会话 ID，用于多轮对话测试
        public string? SessionId { get; set; }

        // 期望执行的动作列表（JSON 字符串，如 ["SearchArticlesByCategory"]）
        public string ExpectedActionsJson { get; set; } = "[]";

        // 期望最终执行是否成功
        public bool ExpectedSuccess { get; set; }

        // 期望答案中包含的关键词列表（JSON 字符串，如 ["文章", "分类"]）
        public string ExpectedAnswerContainsJson { get; set; } = "[]";

        // 期望是否需要用户确认
        public bool ExpectedRequiresConfirmation { get; set; }

        // 期望答案的摘要描述（用于语义评估的参考文本）
        public string? ExpectedAnswerSummary { get; set; }

        // 是否启用语义评分判断
        public bool EnableSemanticJudge { get; set; }

        // 语义评分的通过阈值（0~1）
        public double SemanticJudgeThreshold { get; set; }

        // 测试用例的分类标签（如 "文章查询"、"系统提示"）
        public string? Category { get; set; }

        // 备注信息
        public string? Remark { get; set; }

        // 原始测试用例的创建时间（快照时记录）
        public DateTime SourceCreatedAt { get; set; }

        // 原始测试用例的最后更新时间（快照时记录）
        public DateTime SourceUpdatedAt { get; set; }

        public AgentTestCaseSnapshotDto() { }

        public AgentTestCaseSnapshotDto(AgentTestCase testCase) 
        {
            CaseId = testCase.Id;
            CaseName = testCase.CaseName;
            UserMessage = testCase.UserMessage;
            SessionId = testCase.SessionId;
            ExpectedActionsJson = testCase.ExpectedActionsJson;
            ExpectedSuccess = testCase.ExpectedSuccess;
            ExpectedAnswerContainsJson = testCase.ExpectedAnswerContainsJson;
            ExpectedRequiresConfirmation = testCase.ExpectedRequiresConfirmation;
            ExpectedAnswerSummary = testCase.ExpectedAnswerSummary;
            EnableSemanticJudge = testCase.EnableSemanticJudge;
            SemanticJudgeThreshold = testCase.SemanticJudgeThreshold;
            Category = testCase.Category;
            Remark = testCase.Remark;
            SourceCreatedAt = testCase.CreatedAt;
            SourceUpdatedAt = testCase.UpdatedAt;
        }
    }
}
