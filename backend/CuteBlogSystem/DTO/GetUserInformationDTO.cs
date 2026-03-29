using System.ComponentModel.DataAnnotations;
using CuteBlogSystem.Entity;

namespace CuteBlogSystem.DTO
{
    public class GetUserInformationDTO
    {
        
        public string? NickName { get; set; }

        [Required(ErrorMessage = "头像地址不能为空！")]
        public string AvatarUrl { get; set; }

        [StringLength(200, ErrorMessage = "个人简介长度不能超过200个字符！")]
        public string? Bio { get; set; }

        public int CommentCount { get; set; }  // 用户的总评论数

        public List<ArticleSummaryDTO> ArticlesLike { get; set; } = new(); // 用户点赞的文章列表
        public GetUserInformationDTO() { }
    }
}
