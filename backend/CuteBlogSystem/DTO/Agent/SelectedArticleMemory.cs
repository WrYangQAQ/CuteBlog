using CuteBlogSystem.DTO.Blog;

namespace CuteBlogSystem.DTO.Agent
{
    public class SelectedArticleMemory
    {
        public int? ArticleId { get; set; }

        public string? Title { get; set; }
    }

    // 文章搜索结果项（概要信息）
    public class ArticleSearchResultItem
    {
        // 文章唯一标识
        public int Id { get; set; }

        // 文章标题
        public string Title { get; set; } = string.Empty;

        // 文章摘要（通常为前200字）
        public string Summary { get; set; } = string.Empty;

        // 文章封面图URL（可选）
        public string? CoverUrl { get; set; }

        // 浏览次数
        public int ViewCount { get; set; }

        // 点赞次数
        public int LikeCount { get; set; }

        // 所属分类名称
        public string CategoryName { get; set; } = string.Empty;

        // 关联的标签名称列表
        public List<string> TagNames { get; set; } = new();

        // 构造函数：从 GetArticleListDTO 映射
        public ArticleSearchResultItem(GetArticleListDTO dto)
        {
            Id = dto.Id;
            Title = dto.Title;
            Summary = dto.Summary;
            CoverUrl = string.IsNullOrEmpty(dto.CoverUrl) ? null : dto.CoverUrl; // 转为可空
            ViewCount = dto.ViewCount;
            LikeCount = dto.LikeCount;
            CategoryName = dto.CategoryName;
            TagNames = new List<string>(dto.TagNames); // 拷贝新列表，避免引用共享
        }
    }
}
