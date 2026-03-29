namespace CuteBlogSystem.Entity
{
    public class Category
    {
        public Category() { }
        public Category(int id, string name, string description, int sortOrder)
        {
            Id = id;
            Name = name;
            Description = description;
            SortOrder = sortOrder;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }

        // 导航属性
        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}
