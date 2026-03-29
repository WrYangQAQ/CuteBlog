using System.ComponentModel.DataAnnotations;

namespace CuteBlogSystem.DTO
{
    public class PublishArticleDTO
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public int CategoryId { get; set; }
        public List<string> TagNames { get; set; } = new List<string>();

        [Required(ErrorMessage = "封面图片URL不能为空")]
        public string CoverUrl { get; set; }
        public PublishArticleDTO() { }

        public PublishArticleDTO(string title, string summary, string content, int categoryId, List<string> tagNames, string coverUrl)
        {
            Title = title;
            Summary = summary;
            Content = content;
            CategoryId = categoryId;
            TagNames = tagNames;
            CoverUrl = coverUrl;

        }
    }
}
