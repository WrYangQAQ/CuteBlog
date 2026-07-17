namespace CuteBlogSystem.DTO.Blog
{
    // 由前端传入更新文章所有内容的DTO
    public class UpdateArticleDTO
    {
        // 要更新的文章ID
        public int ArticleId { get; set; }

        // 文章新标题
        public string Title { get; set; } = string.Empty;

        // 文章新摘要
        public string Summary { get; set; } = string.Empty;

        // 文章新正文内容
        public string Content { get; set; } = string.Empty;

        // 文章新分类ID
        public int CategoryId { get; set; }

        // 文章关联的新标签ID列表
        public List<int> TagIds { get; set; } = new List<int>();

        public UpdateArticleDTO() { }
    }

    // 只编辑文章内容，传入数据层使用的DTO
    public class UpdateArticleInformation
    {
        // 要更新的文章ID
        public int ArticleId { get; set; }

        // 文章新标题
        public string Title { get; set; } = string.Empty;

        // 修改前的内容长度（字符数）
        public int OldLength { get; set; }

        // 修改后的内容长度（字符数）
        public int NewLength { get; set; }

        // 更新操作的时间
        public DateTime UpdatedAt { get; set; }

        public UpdateArticleInformation() { }
    }
}
