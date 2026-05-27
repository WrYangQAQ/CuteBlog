using System.ComponentModel.DataAnnotations;

namespace CuteBlogSystem.DTO
{
    public class PublishArticleDTO
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public int CategoryId { get; set; }
        public List<int> TagIds { get; set; } = new List<int>();

        [Required(ErrorMessage = "封面图片URL不能为空")]
        public string CoverUrl { get; set; }
        public PublishArticleDTO() { }

        public PublishArticleDTO(string title, string summary, string content, int categoryId, List<int> tagIds, string coverUrl)
        {
            Title = title;
            Summary = summary;
            Content = content;
            CategoryId = categoryId;
            TagIds = tagIds;
            CoverUrl = coverUrl;
        }
    }
}
