using System.ComponentModel.DataAnnotations;

namespace CuteBlogSystem.DTO
{
    public class UpdateUserInformationDTO
    {
        
        public string NickName { get; set; }

        [StringLength(200, ErrorMessage = "个人简介长度不能超过200个字符！")]
        public string Bio { get; set; }
    }
}
