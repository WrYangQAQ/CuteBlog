using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace CuteBlogSystem.DTO.Agent
{
    // Agent 自动化测试请求
    public class AgentEvaluationRunRequest
    {
        // 测试用例的 Id
        public int CaseId { get; set; }

        // 测试用例的名称，用于标识本次评估
        public string CaseName { get; set; } = string.Empty;

        // 测试用例快照，以json格式保存用例当前版本内容，便于回溯和对比
        public string TestCaseSnapshotJson { get; set; } = "{}";

        // 模拟用户发送的消息内容
        public string UserMessage { get; set; } = string.Empty;

        // 可选的会话 ID，用于模拟带上下文的多轮对话
        public string SessionId { get; set; } = string.Empty;

        // 期望 Agent 执行的动作列表（用于验证计划生成是否正确）
        public List<string> ExpectedActions { get; set; } = new();

        // 期望是否需要用户确认（用于测试高权限操作的确认流程）
        public bool ExpectRequiresConfirmation { get; set; }

        // 期望模型回答输出包含词
        public List<string> ExpectedAnswerContains { get; set; } = new();

        // 预期模型回答输出摘要
        public string ExpectedAnswerSummary { get; set; } = string.Empty;

        // 是否启用语义检测判断
        public bool EnabledSemanticJudge { get; set; }

        // 语义检测得分阈值（低于阈值则不被认可为相似片段）
        public double SemanticJudgeThreshold { get; set; } = 0.75;

        // 期望最终执行是否成功
        public bool ExpectSuccess { get; set; }

        // 根据评估测试案例构造评估请求体
        public AgentEvaluationRunRequest(
            AgentTestCase testCase, 
            List<string> expectedActions, 
            List<string> expectedAnswerContains)
        {
            CaseId = testCase.Id;
            CaseName = testCase.CaseName;
            UserMessage = testCase.UserMessage;
            SessionId = testCase.SessionId ?? string.Empty;
            ExpectedActions = expectedActions;
            ExpectSuccess = testCase.ExpectedSuccess;
            ExpectedAnswerContains = expectedAnswerContains;
            ExpectRequiresConfirmation = testCase.ExpectedRequiresConfirmation;
            ExpectedAnswerSummary = testCase.ExpectedAnswerSummary ?? string.Empty;
            EnabledSemanticJudge = testCase.EnableSemanticJudge;
            SemanticJudgeThreshold = testCase.SemanticJudgeThreshold;
            TestCaseSnapshotJson = JsonSerializer.Serialize
            (
                new AgentTestCaseSnapshotDto(testCase),
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                }
            );
        }

        // 根据评估测试案例快照构造评估请求体
        public AgentEvaluationRunRequest(
            AgentTestCaseSnapshotDto snapshot,
            List<string> expectedActions,
            List<string> expectedAnswerContains)
        {
            CaseId = snapshot.CaseId;
            CaseName = snapshot.CaseName;
            UserMessage = snapshot.UserMessage;
            SessionId = snapshot.SessionId ?? string.Empty;
            ExpectedActions = expectedActions;
            ExpectSuccess = snapshot.ExpectedSuccess;
            ExpectedAnswerContains = expectedAnswerContains;
            ExpectRequiresConfirmation = snapshot.ExpectedRequiresConfirmation;
            ExpectedAnswerSummary = snapshot.ExpectedAnswerSummary ?? string.Empty;
            EnabledSemanticJudge = snapshot.EnableSemanticJudge;
            SemanticJudgeThreshold = snapshot.SemanticJudgeThreshold;
            TestCaseSnapshotJson = JsonSerializer.Serialize
            (
                snapshot,
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                }
            );
        }
    }

    // Agent 自动化测试结果
    public class AgentEvaluationRunResultDTO
    {
        // 测试用例名称
        public string CaseName { get; set; } = string.Empty;

        // 整体测试是否通过（所有期望均匹配）
        public bool Passed { get; set; }

        // 测试结果保存的WorkflowLogId（用于追踪执行日志）
        public int? WorkflowLogId { get; set; }

        // 测试失败时的错误信息列表
        public List<string> Errors { get; set; } = new();

        // Agent 返回的最终答案
        public string Answer { get; set; } = string.Empty;

        // 实际执行的动作列表（用于对比期望动作）
        public List<string> ActualActions { get; set; } = new();

        // 实际是否需要确认
        public bool ActualRequiresConfirmation { get; set; }

        // 实际执行是否成功
        public bool ActualSuccess { get; set; }

        // 可能存在的语义相似度得分
        public double? SemanticScore { get; set; }

        // 可能存在的语义检测原因
        public string? SemanticJudgeReason { get; set; }

        // 语义相似检测是否通过结果
        public bool? SemanticJudgePassed { get; set; }

        // 如果未通过，其错误类型
        public AgentEvaluationFailureType FailureType { get; set; } = AgentEvaluationFailureType.None;
    }

    // Agent 批量自动化测试结果汇总
    public class AgentEvaluationBatchResultDTO
    {
        // 总测试用例数
        public int Total { get; set; }

        // 通过的测试用例数
        public int Passed { get; set; }

        // 失败的测试用例数
        public int Failed { get; set; }

        // 每个测试用例的详细结果列表
        public List<AgentEvaluationRunResultDTO> Results { get; set; } = new();

        public AgentEvaluationBatchResultDTO(List<AgentEvaluationRunResultDTO> runRequests)
        {
            Total = runRequests.Count;
            Passed = runRequests.Count(result => result.Passed);
            Failed = runRequests.Count(result => !result.Passed);
            Results = runRequests;
        }
    }

    // Agent 输出片段语义详细性识别结果
    public class AgentSemanticJudgeResult
    {
        // 语义相似性识别分数
        public double Score { get; set; } = 0.0;

        // 语义检测是否通过结果
        public bool Passed { get; set; }

        // 语义检测通过与否原因
        public string Reason { get; set; } = string.Empty;
    }

    // Agent 评估回归测试的摘要
    public class AgentEvaluationRegressionSummaryDTO
    {
        // 基准运行的 ID（即对比中的旧版本）
        public long BaseRunId { get; set; }

        // 目标运行的 ID（即对比中的新版本）
        public long TargetRunId { get; set; }

        // 整体决策结果：Pass（通过）/ Warning（警告）/ Blocked（阻断）
        public EvluationDecisionResultType Decision { get; set; } = EvluationDecisionResultType.Warning;

        // 回归报告的标题（如“回归测试结果摘要”）
        public string Title { get; set; } = string.Empty;

        // 回归报告的文本摘要，总结关键变化
        public string Summary { get; set; } = string.Empty;

        // 从失败变为成功的用例数（修复数量）
        public int FixedCount { get; set; }

        // 从成功变为失败的用例数（退化数量）
        public int RegressedCount { get; set; }

        // 两次运行均通过的用例数
        public int StillPassedCount { get; set; }

        // 两次运行均失败的用例数
        public int StillFailedCount { get; set; }

        // 目标运行中新增的用例数
        public int NewCaseCount { get; set; }

        // 目标运行中缺失的用例数
        public int MissingCaseCount { get; set; }

        // 需要重点关注的好消息或积极变化列表
        public List<string> Highlights { get; set; } = new();

        // 需要关注的风险点或潜在问题列表
        public List<string> Risks { get; set; } = new();

        // 建议的后续操作列表（如需要人工复核、修复特定用例等）
        public List<string> NextActions { get; set; } = new();

        public AgentEvaluationRegressionSummaryDTO(AgentEvaluationCompareDTO compareDto)
        {
        
            BaseRunId = compareDto.BaseRunId;
            TargetRunId = compareDto.TargetRunId;
            FixedCount = compareDto.FixedCount;
            RegressedCount = compareDto.RegressedCount;
            StillPassedCount = compareDto.StillPassedCount;
            StillFailedCount = compareDto.StillFailedCount;
            NewCaseCount = compareDto.NewCaseCount;
            MissingCaseCount = compareDto.MissingCaseCount;
        }
    }
}