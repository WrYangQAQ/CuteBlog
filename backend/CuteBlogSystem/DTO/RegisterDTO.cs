using System.ComponentModel.DataAnnotations;

namespace CuteBlogSystem.DTO
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "用户名不能为空！")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "用户名长度必须在3到20个字符之间！")]
        public string Username { get; set; }

        [Required(ErrorMessage = "邮箱不能为空！")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址！")]
        public string Email { get; set; }

        [Required(ErrorMessage = "密码不能为空！")]
        [StringLength(100, MinimumLength = 10, ErrorMessage = "密码长度必须至少为10个字符！")]
        public string Password { get; set; }

        [StringLength(20, ErrorMessage = "昵称长度不能超过20个字符！")]
        public string? NickName { get; set; }
        
    }
}
