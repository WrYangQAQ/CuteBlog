namespace CuteBlogSystem.Entity
{
    public class ArticleTag
    {
        public ArticleTag() { }

        public ArticleTag(int articleId, int tagId)
        {
            ArticleId = articleId;
            TagId = tagId;
        }

        public int ArticleId { get; set; }
        public int TagId { get; set; }

        // 导航属性
        public virtual Article Article { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
