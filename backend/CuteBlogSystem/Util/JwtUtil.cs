using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CuteBlogSystem.Util
{
    public class JwtUtil
    {
        private readonly IConfiguration _configuration;

        private const int TokenExpirationHours = 7 * 24; // Token 有效期（小时）

        public JwtUtil(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(int id, string username, string email, string role)
        {
            // 1. 从配置中读取 JWT 参数
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            // 2. 构建 Claims（声明）
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()), // 用户唯一标识
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // 唯一标识
            };

            // 3. 创建 token 描述对象
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(TokenExpirationHours), // 有效期 1 星期
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            // 4. 生成 token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
