namespace CuteBlogSystem.Entity
{
    public class Tag
    {
        public Tag() { }

        public Tag(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public int CategoryId { get; set; }

        // 导航属性
        public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
        public virtual Category Category { get; set; }
    }
}
