namespace CuteBlogSystem.DTO
{
    public class PublishCommentDTO
    {
        public string Content { get; set; }
        public int? ParentCommentId { get; set; }
        
        public PublishCommentDTO() { }
    }
}
