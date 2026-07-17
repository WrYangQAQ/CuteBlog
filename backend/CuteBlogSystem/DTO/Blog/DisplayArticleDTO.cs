using CuteBlogSystem.DTO.AgentAction;
using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.Blog
{
    public class DisplayArticleDTO : IAgentContentOutput
    {
        // 文章标题
        public string Title { get; set; }

        // 文章正文内容
        public string Content { get; set; }

        // 作者名称（若作者信息缺失则显示“未知作者”）
        public string AuthorName { get; set; }

        // 所属分类名称（若未分类则显示“未分类”）
        public string CategoryName { get; set; }

        // 关联的标签名称列表
        public List<string> TagNames { get; set; } = new List<string>();

        // 文章创建时间
        public DateTime CreatedAt { get; set; }

        // 根据 Article 实体构造 DisplayArticleDTO 对象
        public DisplayArticleDTO(Article article)
        {
            Title = article.Title;
            Content = article.Content;
            AuthorName = article.User?.UserName ?? "未知作者";
            CategoryName = article.Category?.Name ?? "未分类";
            TagNames = article.ArticleTags.Select(at => at.Tag.Name).ToList();
            CreatedAt = article.CreatedAt;
        }

        public string GetContentText()
        {
            return Content;
        }
    }
}
