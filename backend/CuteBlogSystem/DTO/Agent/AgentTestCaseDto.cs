using System.ComponentModel.DataAnnotations;

namespace CuteBlogSystem.DTO.Agent
{
    public class AgentTestCaseAddDto
    {
        [Required(ErrorMessage = "用例名称不能为空")]
        [MaxLength(200, ErrorMessage = "用例名称过长")]
        public string CaseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "用户消息不能为空")]
        public string UserMessage { get; set; } = string.Empty;

        // 会话ID，用于多轮测试。若为空，服务端将自动生成
        public string? SessionId { get; set; }

        // 期望执行的动作列表（如 ["SearchArticles", "Summarize"]）
        [Required]
        public List<string> ExpectedActions { get; set; } = new List<string>();

        [Required]
        public bool ExpectSuccess { get; set; }

        [Required]
        public bool ExpectRequiresConfirmation { get; set; }

        // 期望回答中应包含的关键词列表
        public List<string> ExpectedAnswerContains { get; set; } = new List<string>();

        // 期望回答的语义摘要描述（用于 LLM 评判）
        public string? ExpectedAnswerSummary { get; set; }

        // 是否启用语义评判，默认 false
        public bool EnableSemanticJudge { get; set; } = false;

        // 语义评判阈值，默认 0.7
        public double SemanticJudgeThreshold { get; set; } = 0.7;

        // 分类（用于分组）
        public string? Category { get; set; }

        // 备注
        public string? Remark { get; set; }
    }

    public class AgentTestCaseUpdateDto
    {
        [Required(ErrorMessage = "用例ID不能为空")]
        public int Id { get; set; }

        [Required(ErrorMessage = "用例名称不能为空")]
        [MaxLength(200, ErrorMessage = "用例名称过长")]
        public string CaseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "用户消息不能为空")]
        public string UserMessage { get; set; } = string.Empty;

        // 会话ID，若为空则保持不变（不更新）
        public string? SessionId { get; set; }

        [Required]
        public List<string> ExpectedActions { get; set; } = new List<string>();

        [Required]
        public bool ExpectSuccess { get; set; }

        [Required]
        public bool ExpectRequiresConfirmation { get; set; }

        public List<string> ExpectedAnswerContains { get; set; } = new List<string>();

        public string? ExpectedAnswerSummary { get; set; }

        public bool EnableSemanticJudge { get; set; }

        public double SemanticJudgeThreshold { get; set; } = 0.7;

        public string? Category { get; set; }

        public string? Remark { get; set; }
    }
}
