namespace CuteBlogSystem.AI.Planner
{
    public class AgentPlan
    {
        // 计划要实现的最终目标描述，例如“撰写一篇关于AI的博客文章”
        public string Goal { get; set; }

        // 构成该计划的所有步骤列表，按顺序执行以达成目标
        public List<AgentPlanStep> Steps { get; set; } = new();
    }

    public class AgentPlanStep
    {
        // 步骤在计划中的顺序编号，从1开始递增，用于明确执行顺序
        public int StepNumber { get; set; }

        // 要执行的动作名称，例如“GenerateContent”、“SearchKeyword”或“PostComment”
        public string Action { get; set; } = string.Empty;

        // 对该步骤的人可读描述，解释该动作的目的或预期效果
        public string Description { get; set; } = string.Empty;

        // 执行该动作时所需的具体参数，键为参数名，值为参数值
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}