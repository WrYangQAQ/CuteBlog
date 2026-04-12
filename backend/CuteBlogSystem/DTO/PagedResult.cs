namespace CuteBlogSystem.DTO
{
    public class PagedResult<T>
    {
        public required T Items { get; set; }
        public required int TotalCount { get; set; }

        public PagedResult() { }

        public PagedResult(T items, int totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }
    }
}
