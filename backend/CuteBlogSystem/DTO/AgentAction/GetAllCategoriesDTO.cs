using CuteBlogSystem.DTO.Blog;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class GetAllCategoriesOutput : IUserReadableOutput
    {
        // 分类列表
        public List<CategoryItem> Categories { get; set; } = new();

        // 分类总数（只读属性，由 Categories 计算）
        public int TotalCount => Categories.Count;

        // 无参构造函数
        public GetAllCategoriesOutput()
        {
        }

        // 从 GetCategoryDTO 集合构造 GetAllCategoriesOutput
        public GetAllCategoriesOutput(IEnumerable<GetCategoryDTO> categories)
        {
            Categories = categories
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    SortOrder = c.SortOrder
                })
                .ToList();
        }

        // 生成对用户友好的分类列表文本
        public string ToUserReadableText()
        {
            if (Categories.Count == 0)
            {
                return "当前系统中暂无可用分类。";
            }

            var lines = Categories
                .Select((category, index) =>
                    $"{index + 1}. {category.Name}" +
                    (string.IsNullOrWhiteSpace(category.Description)
                        ? ""
                        : $" - {category.Description}"));

            return $"当前系统共有 {TotalCount} 个分类：\n" + string.Join("\n", lines);
        }
    }

    public class CategoryItem
    {
        // 分类 ID
        public int Id { get; set; }

        // 分类名称
        public string Name { get; set; } = string.Empty;

        // 分类描述
        public string Description { get; set; } = string.Empty;

        // 分类排序序号
        public int SortOrder { get; set; }
    }
}