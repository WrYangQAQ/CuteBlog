namespace CuteBlogSystem.DTO.AgentAction
{
    // 根据内容推荐分类动作的输入参数
    public class RecommendCategoryInput
    {
        // 待分析正文
        public string Content { get; set; } = string.Empty;

        // 从前置步骤获取正文
        public int? ContentFromStep { get; set; }

        // 可选标题，用于辅助分类判断
        public string Title { get; set; } = string.Empty;
    }

    // 根据内容推荐分类动作的输出结果
    public class RecommendCategoryOutput : IUserReadableOutput
    {
        // 推荐分类 ID
        public int RecommendedCategoryId { get; set; }

        // 推荐分类名称
        public string RecommendedCategoryName { get; set; } = string.Empty;

        // 置信度，范围 0~1
        public double Confidence { get; set; }

        // 推荐理由
        public string Reason { get; set; } = string.Empty;

        public string ToUserReadableText()
        {
            return $"推荐分类：{RecommendedCategoryName}（ID：{RecommendedCategoryId}，置信度：{Confidence:0.00}）。\n理由：{Reason}";
        }
    }
}
