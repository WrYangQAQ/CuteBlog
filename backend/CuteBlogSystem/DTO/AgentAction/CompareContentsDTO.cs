namespace CuteBlogSystem.DTO.AgentAction
{
    public class CompareContentsInput
    {
        // 第一篇正文来源步骤
        public int ContentFromStepA { get; set; }

        // 第二篇正文来源步骤
        public int ContentFromStepB { get; set; }

        // 可选：用户希望重点比较的方向，例如“技术深度”“适合初学者程度”
        public string? CompareFocus { get; set; }

        // 第一篇正文内容，由 Executor 从前置步骤提取
        public string ContentA { get; set; } = string.Empty;

        // 第二篇正文内容，由 Executor 从前置步骤提取
        public string ContentB { get; set; } = string.Empty;
    }

    public class CompareContentsOutput : IUserReadableOutput
    {
        // 对比结果
        public string Comparison { get; set; } = string.Empty;

        // 第一篇正文长度
        public int ContentALength { get; set; }

        // 第二篇正文长度
        public int ContentBLength { get; set; }

        // 对比结果长度
        public int ComparisonLength { get; set; }

        // 比较方向
        public string? CompareFocus { get; set; }

        public string ToUserReadableText()
        {
            return Comparison;
        }
    }
}