namespace CuteBlogSystem.DTO.AgentAction
{
    // 根据内容推荐标签动作的输入参数
    public class RecommendTagsInput
    {
        // 待分析正文
        public string Content { get; set; } = string.Empty;

        // 从前置步骤获取正文
        public int? ContentFromStep { get; set; }

        // 可选标题，用于辅助标签推荐
        public string Title { get; set; } = string.Empty;

        // 可选已有标签，作为推荐参考
        public List<string> ExistingTags { get; set; } = new();
    }

    // 根据内容推荐标签动作的输出结果
    public class RecommendTagsOutput : IUserReadableOutput
    {
        // 推荐标签列表
        public List<RecommendedTagItem> Tags { get; set; } = new();

        public string ToUserReadableText()
        {
            if (Tags.Count == 0)
            {
                return "暂时没有生成合适的标签建议。";
            }

            var lines = Tags.Select((tag, index) =>
                $"{index + 1}. {tag.TagName}（置信度：{tag.Confidence:0.00}） - {tag.Reason}");

            return "推荐标签如下：\n" + string.Join("\n", lines);
        }
    }

    public class RecommendedTagItem
    {
        // 标签名称
        public string TagName { get; set; } = string.Empty;

        // 置信度，范围 0~1
        public double Confidence { get; set; }

        // 推荐理由
        public string Reason { get; set; } = string.Empty;
    }
}
