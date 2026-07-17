namespace CuteBlogSystem.DTO.AgentAction
{
    public class ExplainFailureWithSuggestionsInput
    {
        // 原始失败步骤的编号（必填，用于定位失败来源）
        public int FailureFromStep { get; set; }

        // 获取所有分类的步骤编号（可选，用于提供可用分类上下文）
        public int CategoriesFromStep { get; set; }

        // 获取用户文章列表的步骤编号（可选，用于提供用户已有文章上下文）
        public int ArticlesFromStep { get; set; }

        // 补救查询结果的步骤编号（可选，用于提供搜索结果上下文）
        public int SearchResultsFromStep { get; set; }

        // 获取文章正文的步骤编号（可选，用于提供具体内容上下文）
        public int ContentFromStep { get; set; }

        // 用户原本请求的目标（如分类名称），用于说明用户意图
        public string RequestedCategoryName { get; set; } = string.Empty;
    }

    public class ExplainFailureWithSuggestionsOutput : IUserReadableOutput
    {
        // 生成给用户的建议回答文本（最终输出）
        public string Answer { get; set; } = string.Empty;

        // 原始失败步骤编号（用于追溯）
        public int FailureFromStep { get; set; }

        // 用户请求的目标描述（如分类名称）
        public string RequestedTarget { get; set; } = string.Empty;

        // 实际使用的上下文类型列表
        public List<string> UsedContextTypes { get; set; } = new();

        // 失败原因的简要摘要
        public string FailureSummary { get; set; } = string.Empty;

        // 生成对用户友好的可读文本（如 Answer 为空则返回默认提示）
        public string ToUserReadableText()
        {
            return string.IsNullOrWhiteSpace(Answer)
                ? "任务执行失败，请调整请求后重试。"
                : Answer;
        }
    }
}