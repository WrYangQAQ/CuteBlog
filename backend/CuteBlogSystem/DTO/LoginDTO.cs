using System.ComponentModel.DataAnnotations;

namespace CuteBlogSystem.DTO
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "用户名或邮箱不能为空！")]
        public string UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "密码不能为空！")]
        public string Password { get; set; }
    }
}
