using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class UpdateArticleTitleDTO
    {
        public int ArticleId { get; set; }

        public string OldTitle { get; set; } = string.Empty;

        public string NewTitle { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public UpdateArticleTitleDTO(int articleId, string oldTitle, string newTitle, DateTime updatedAt)
        {
            ArticleId = articleId;
            OldTitle = oldTitle;
            NewTitle = newTitle;
            UpdatedAt = updatedAt;
        }
    }
}
