using CuteBlogSystem.DTO;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.Security.Claims;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly AdminStatisticsService _adminStatisticsService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserService userService,
                              ILogger<AuthController> logger,
                              AdminStatisticsService adminStatisticsService)
        {
            _userService = userService;
            _logger = logger;
            _adminStatisticsService = adminStatisticsService;
        }

        // POST: api/auth/register
        // 用户注册
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            ApiResponse response = await _userService.RegisterUserAsync(registerDTO);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        // POST: api/auth/login
        // 用户登录
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            ApiResponse response = await _userService.LoginUserAsync(loginDTO);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return Unauthorized(response);
            }
        }

        // GET: api/auth/profile
        // 获取用户信息
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool success = int.TryParse(userId, out int userIdInt);
            if (success)
            {
                var response = await _userService.GetUserInfoAsync(userIdInt);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return NotFound(response);
                }
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法获取用户信息。");
                return Unauthorized(new ApiResponse(false, "用户ID无效！"));
            }
        }

        // PUT: api/auth/profile
        // 更新用户信息
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserInformationDTO userInformationDTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool success = int.TryParse(userId, out int userIdInt);
            if (success)
            {
                var response = await _userService.UpdateUserInfoAsync(userIdInt, userInformationDTO);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法更新用户信息。");
                return Unauthorized(new ApiResponse(false, "用户ID无效！"));
            }
        }

        // POST: api/auth/avatar
        // 上传用户头像
        [Authorize]
        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar([FromForm] UploadImageRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool success = int.TryParse(userId, out int userIdInt);
            if (success)
            {
                var response = await _userService.UploadAvatarAsync(userIdInt, request.Image);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    if (response.Code == ResponseCode.FileMissing
                        || response.Code == ResponseCode.FileTooLarge
                        || response.Code == ResponseCode.InvalidFileType
                        || response.Code == ResponseCode.InvalidFileContent)
                    {
                        return BadRequest(response);
                    }
                    else if (response.Code == ResponseCode.UserNotFound)
                    {
                        return NotFound(response);
                    }
                    else
                    {
                        return Unauthorized(response);
                    }


                }
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法上传头像。");
                return Unauthorized(new ApiResponse(false, "用户ID无效！"));
            }

        }

        // 获取自己的文章列表
        [Authorize]
        [HttpGet("articles")]
        public async Task<IActionResult> GetMyArticles([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool success = int.TryParse(userId, out int userIdInt);
            if (success)
            {
                var response = await _userService.GetMyArticlesAsync(userIdInt, page, pageSize);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return NotFound(response);
                }
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法获取用户文章列表。");
                return Unauthorized(new ApiResponse(false, "用户ID无效！"));
            }
        }

        // 管理员获取仪表盘信息
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/dashboard")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var response = await _adminStatisticsService.GetStatisticsAsync();
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(response);
            }
        }

        // 管理员上传图片以帮助网页内容展示
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAdminImage([FromForm] UploadImageRequest request)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userIdInt);
            var response = await _userService.UploadWebsiteImageAsync(userIdInt, request.Image);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                if (response.Code == ResponseCode.FileMissing
                    || response.Code == ResponseCode.FileTooLarge
                    || response.Code == ResponseCode.InvalidFileType
                    || response.Code == ResponseCode.InvalidFileContent)
                {
                    return BadRequest(response);
                }
                else
                {
                    return NotFound(response);
                }
            }
        }
    }
}
