using System.ComponentModel.DataAnnotations;

namespace CuteBlogSystem.DTO.Blog
{
    public class UpdateUserInformationDTO
    {

        public string NickName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "个人简介长度不能超过200个字符！")]
        public string Bio { get; set; } = string.Empty;
    }
}
