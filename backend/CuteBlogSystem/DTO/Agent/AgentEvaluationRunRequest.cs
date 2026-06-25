namespace CuteBlogSystem.DTO.Agent
{
    // Agent 自动化测试请求
    public class AgentEvaluationRunRequest
    {
        // 测试用例的名称，用于标识本次评估
        public string CaseName { get; set; } = string.Empty;

        // 模拟用户发送的消息内容
        public string UserMessage { get; set; } = string.Empty;

        // 可选的会话 ID，用于模拟带上下文的多轮对话
        public string SessionId { get; set; } = string.Empty;

        // 期望 Agent 执行的动作列表（用于验证计划生成是否正确）
        public List<string> ExpectedActions { get; set; } = new();

        // 期望是否需要用户确认（用于测试高权限操作的确认流程）
        public bool ExpectRequiresConfirmation { get; set; }

        // 期望最终执行是否成功
        public bool ExpectSuccess { get; set; }
    }

    // Agent 自动化测试结果
    public class AgentEvaluationRunResultDTO
    {
        // 测试用例名称
        public string CaseName { get; set; } = string.Empty;

        // 整体测试是否通过（所有期望均匹配）
        public bool Passed { get; set; }

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
    }
}