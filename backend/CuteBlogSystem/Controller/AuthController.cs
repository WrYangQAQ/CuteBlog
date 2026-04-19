using CuteBlogSystem.Config;
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
    public class AuthController : BaseController
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
            return ReturnResponse(response);
        }

        // POST: api/auth/login
        // 用户登录
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            ApiResponse response = await _userService.LoginUserAsync(loginDTO);
            return ReturnResponse(response);
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
                ApiResponse response = await _userService.GetUserInfoAsync(userIdInt);
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法获取用户信息。");
                return ReturnResponse(new ApiResponse(false, "用户ID无效！", code:ResponseCode.Unauthorized));
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
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法更新用户信息。");
                return ReturnResponse(new ApiResponse(false, "用户ID无效！", code: ResponseCode.Unauthorized));
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
                ApiResponse response = await _userService.UploadAvatarAsync(userIdInt, request.Image);
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法上传头像。");
                return ReturnResponse(new ApiResponse(false, "用户ID无效！", code: ResponseCode.Unauthorized));
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
                ApiResponse response = await _userService.GetMyArticlesAsync(userIdInt, page, pageSize);
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法获取用户文章列表。");
                return ReturnResponse(new ApiResponse(false, "用户ID无效！", code: ResponseCode.Unauthorized));
            }
        }

        // 管理员获取仪表盘信息
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/dashboard")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            ApiResponse response = await _adminStatisticsService.GetStatisticsAsync();
            return ReturnResponse(response);
        }

        // 管理员上传图片以帮助网页内容展示
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAdminImage([FromForm] UploadImageRequest request)
        {
            bool success = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userIdInt);
            if (success)
            {
                ApiResponse response = await _userService.UploadWebsiteImageAsync(userIdInt, request.Image);
                return ReturnResponse(response);
            }
            else
            {
                _logger.LogWarning("用户ID无效，无法上传图片。");
                return ReturnResponse(new ApiResponse(false, "用户ID无效！", code: ResponseCode.Unauthorized));
            }
        }
    }
}
