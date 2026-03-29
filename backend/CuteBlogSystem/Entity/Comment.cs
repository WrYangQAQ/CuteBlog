namespace CuteBlogSystem.Entity
{
    public class Comment
    {
        public Comment() { }

        public Comment(int id, string content, int articleId, int userId, int? parentCommentId, DateTime createdAt, bool isApproved)
        {
            Id = id;
            Content = content;
            ArticleId = articleId;
            UserId = userId;
            ParentCommentId = parentCommentId;
            CreatedAt = createdAt;
            IsApproved = isApproved;
        }

        public int Id { get; set; }
        public string Content { get; set; }
        public int ArticleId { get; set; }
        public int UserId { get; set; }
        public int? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }

        // 导航属性
        public virtual Article Article { get; set; }
        public virtual User User { get; set; }
        public virtual Comment ParentComment { get; set; }
        public virtual ICollection<Comment> InverseParentComment { get; set; } = new List<Comment>();
    }
}
