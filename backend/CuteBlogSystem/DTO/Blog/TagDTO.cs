using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO.Blog
{
    public class GetTagDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public GetTagDTO(Tag tag)
        {
            Id = tag.Id;
            Name = tag.Name;
            CategoryId = tag.CategoryId;
        }

        public GetTagDTO() { }
    }

    public class TagArticleCountDTO
    {
        public int TagId { get; set; }

        public int Count { get; set; }
    }
}
