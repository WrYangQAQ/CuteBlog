using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class GetCategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }

        public GetCategoryDTO(Category category)
        {
            Id = category.Id;
            Name = category.Name;
            Description = category.Description;
            SortOrder = category.SortOrder;
        }
    }
}
