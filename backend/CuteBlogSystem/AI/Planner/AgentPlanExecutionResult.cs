namespace CuteBlogSystem.AI.Planner
{
    public class AgentPlanExecutionResult
    {
        // 计划要达成的最终目标，与 AgentPlan.Goal 对应
        public string Goal { get; set; } = string.Empty;

        // 计划中每个步骤的执行结果列表，按顺序排列
        public List<AgentStepExecutionResult> StepResults { get; set; } = new();

        // 最终生成的回答文本，汇总所有步骤的执行结果后给出的最终输出
        public string FinalAnswer { get; set; } = string.Empty;
    }

    public class AgentStepExecutionResult
    {
        // 步骤在计划中的顺序编号，与 AgentPlanStep.StepNumber 对应
        public int StepNumber { get; set; }

        // 执行的动作名称，例如 SearchArticlesByCategory、GetArticleContentById 等
        public string Action { get; set; } = string.Empty;

        // 表示该步骤是否执行成功
        public bool Success { get; set; }

        // 执行结果的消息描述，成功时可为“成功”，失败时描述错误原因
        public string Message { get; set; } = string.Empty;

        // 执行后返回的附加数据，例如文章列表、正文内容或摘要等，无数据时为 null
        public object? Data { get; set; }
    }
}