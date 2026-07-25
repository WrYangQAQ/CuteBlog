using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.AgentAction
{
    // 根据分类 ID 查询标签列表动作的输入参数
    public class GetTagsByCategoryIdInput
    {
        // 分类 ID
        public int CategoryId { get; set; }

        // 从前置步骤提取分类 ID
        public int? CategoryIdFromStep { get; set; }

        // 分类名称（用于直接匹配分类）
        public string CategoryName { get; set; } = string.Empty;
    }

    // 根据分类 ID 查询标签列表动作的输出结果
    public class GetTagsByCategoryIdOutput : IUserReadableOutput
    {
        // 分类 ID
        public int CategoryId { get; set; }

        // 分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 分类描述
        public string CategoryDescription { get; set; } = string.Empty;

        // 标签列表
        public List<TagItem> Tags { get; set; } = new();

        // 标签数量
        public int TotalCount => Tags.Count;

        public string ToUserReadableText()
        {
            if (Tags.Count == 0)
            {
                return $"分类「{CategoryName}」下暂无标签。";
            }

            var lines = Tags.Select((tag, index) => $"{index + 1}. {tag.Name}（ID：{tag.Id}）");
            return $"分类「{CategoryName}」下共有 {TotalCount} 个标签：\n" + string.Join("\n", lines);
        }
    }

    public class TagItem
    {
        // 标签 ID
        public int Id { get; set; }

        // 标签名称
        public string Name { get; set; } = string.Empty;

        public TagItem()
        {
        }

        public TagItem(Tag tag)
        {
            Id = tag.Id;
            Name = tag.Name;
        }
    }
}
