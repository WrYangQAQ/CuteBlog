using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class GetCommentDTO
    {
        public string UserName { get; set; }
        public string AvatarUrl { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public GetCommentDTO(Comment comment)
        {
            UserName = comment.User?.UserName ?? "匿名用户";
            AvatarUrl = comment.User?.AvatarUrl ?? "/Picture/Avatar/DefaultAvatar/DefaultAvatar_1.png";
            Content = comment.Content;
            CreatedAt = comment.CreatedAt;
        }

        public GetCommentDTO()
        {
        }
    }
}
