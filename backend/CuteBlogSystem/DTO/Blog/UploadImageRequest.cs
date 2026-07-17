namespace CuteBlogSystem.DTO.Blog
{
    public class UploadImageRequest
    {
        public IFormFile Image { get; set; } = default!;
    }
}
