namespace CuteBlogSystem.Entity
{
    public class ArticleLike
    {
        public ArticleLike(int userId, int articleId, DateTime likeAt)
        {
            UserId = userId;
            ArticleId = articleId;
            LikeAt = likeAt;
        }

        public ArticleLike() { }

        public int UserId { get; set; }
        public int ArticleId { get; set; }
        public DateTime LikeAt { get; set; }

        // 导航属性
        public virtual User User { get; set; }
        public virtual Article Article { get; set; }
    }
}
