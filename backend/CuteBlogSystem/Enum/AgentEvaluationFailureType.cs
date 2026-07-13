namespace CuteBlogSystem.Enum
{
    public enum AgentEvaluationFailureType
    {
        // 没有错误，测试用例符合预期
        None = 0,

        // Agent 执行过程中出现异常
        RunTimeError = 1,

        // 实际 Action 列表缺少预期 Action 行为
        PlanActionMissing = 2,

        // 执行结果 Success 与预期不一致
        SuccessMismatch = 3,

        // 行为确认性 RequireConfirmation 与预期不一致
        ConfirmationMismatch = 4,

        // 最终回答缺少预期关键词
        KeywordMismatch = 5,

        // 语义评估未通过
        SemanticMismatch = 6,

        // 返回结构不符合预期，造成反序列化失败，没有拿到DTO
        ResultFormatError = 7,

        // 其他未知错误
        Unknown = 99
    }
}
