using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.AgentAction
{
    // 根据标签名查询标签动作的输入参数
    public class GetTagByNameInput
    {
        // 标签名称或关键词
        public string TagName { get; set; } = string.Empty;
    }

    // 根据标签名查询标签动作的输出结果
    public class GetTagByNameOutput : IUserReadableOutput
    {
        // 标签 ID
        public int TagId { get; set; }

        // 标签名称
        public string TagName { get; set; } = string.Empty;

        // 所属分类 ID
        public int CategoryId { get; set; }

        // 所属分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 是否找到唯一匹配的标签
        public bool Found => TagId > 0;

        public GetTagByNameOutput()
        {
        }

        public GetTagByNameOutput(Tag tag)
        {
            TagId = tag.Id;
            TagName = tag.Name;
            CategoryId = tag.CategoryId;
            CategoryName = tag.Category?.Name ?? string.Empty;
        }

        public string ToUserReadableText()
        {
            if (!Found)
            {
                return "没有找到匹配的标签。";
            }

            return $"已找到标签：{TagName}（ID：{TagId}，所属分类：{CategoryName}）。";
        }
    }
}
