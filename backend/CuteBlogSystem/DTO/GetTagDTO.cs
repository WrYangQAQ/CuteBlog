using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class GetTagDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public GetTagDTO(Tag tag)
        {
            Id = tag.Id;
            Name = tag.Name;
            CategoryId = tag.CategoryId;
        }

        public GetTagDTO() { }
    }
}
