using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Util;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Data;

namespace CuteBlogSystem.Service
{
    public class UserService
    {
        // 依赖注入UserRepository，以便在服务中进行数据库操作
        private readonly UserRepository _userRepository;
        private readonly ArticleRepository _articleRepository;
        private readonly ArticleLikeRepository _articleLikeRepository;
        private readonly CommentRepository _commentRepository;
        private readonly JwtUtil _jwtUtil;
        private readonly IWebHostEnvironment _environment;
        private readonly ImageUploadService _imageUploadService;
        private readonly ILogger<UserService> _logger;

        // 定义允许的文件扩展名和最大文件大小常量
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxPictureAvatarSize = 2 * 1024 * 1024;
        private const long MaxPictureWebsiteDecorationImageSize = 20 * 1024 * 1024;

        public UserService(UserRepository userRepository, 
                           JwtUtil jwtUtil, 
                           IWebHostEnvironment environment, 
                           ImageUploadService imageUploadService, 
                           ArticleRepository articleRepository,
                           ILogger<UserService> logger,
                           CommentRepository commentRepository,
                           ArticleLikeRepository articleLikeRepository)
        {
            _userRepository = userRepository;
            _jwtUtil = jwtUtil;
            _environment = environment;
            _imageUploadService = imageUploadService;
            _articleRepository = articleRepository;
            _logger = logger;
            _commentRepository = commentRepository;
            _articleLikeRepository = articleLikeRepository;
        }

        // 用户注册方法，接收一个RegisterDTO对象作为参数，返回一个ApiResponse对象
        public async Task<ApiResponse> RegisterUserAsync(RegisterDTO registerDTO)
        {
            User user = new User();

            // 将dto中的数据映射到实体类中
            user.UserName = registerDTO.Username;
            user.Email = registerDTO.Email;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password);  // 使用BCrypt进行密码哈希处理
            user.NickName = registerDTO.NickName;

            user.Role = UserRole.User;  // 默认角色为普通用户
            user.CreatedAt = DateTime.UtcNow;  // 设置创建时间为当前UTC时间

            // 检查是否已经存在相同用户名的用户
            var checkUserWithUsername = await _userRepository.GetUserByUsernameAsync(user.UserName);
            if (checkUserWithUsername != null)
            {
                _logger.LogWarning("注册失败！用户名已被使用！用户名：{Username}", user.UserName);
                return new ApiResponse(false, "注册失败！用户名已被使用！");
            }

            // 检查是否已经存在相同邮箱的用户
            var checkUserWithEmail = await _userRepository.GetUserByEmailAsync(user.Email);
            if (checkUserWithEmail != null)
            {
                _logger.LogWarning("注册失败！邮箱已被使用！邮箱：{Email}", user.Email);
                return new ApiResponse(false, "注册失败！邮箱已被使用！");
            }

            // 调用UserRepository将用户数据保存到数据库中
            var result = await _userRepository.AddUserAsync(user);

            if (result == true)
            {
                return new ApiResponse(true, "用户注册成功！");
            }
            else
            {
                _logger.LogError("用户注册失败！用户名：{Username}, 邮箱：{Email}", user.UserName, user.Email);
                return new ApiResponse(false, "用户注册失败！");
            }

        }

        // 用户登录方法，接收一个LoginDTO对象作为参数，返回一个ApiResponse对象
        public async Task<ApiResponse> LoginUserAsync(LoginDTO loginDTO)
        {
            // 根据用户名或邮箱获取用户
            var user = await _userRepository.GetUserByUsernameAsync(loginDTO.UsernameOrEmail)
                    ?? await _userRepository.GetUserByEmailAsync(loginDTO.UsernameOrEmail);

            if (user == null)
            {
                _logger.LogWarning("登录失败！用户名或密码错误！输入：{UsernameOrEmail}", loginDTO.UsernameOrEmail);
                return new ApiResponse(false, "登录失败！用户名或密码错误！");
            }

            // 验证密码
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                _logger.LogWarning("登录失败！用户名或密码错误！输入：{UsernameOrEmail}", loginDTO.UsernameOrEmail);
                return new ApiResponse(false, "登录失败！用户名或密码错误！");
            }

            // 登录成功，返回Token
            var Token = _jwtUtil.GenerateToken(user.Id, user.UserName, user.Email, user.Role.ToString());  // 生成JWT令牌
            return new ApiResponse(true, "登录成功！", Token);
        }

        // 获取用户信息方法，接收一个用户ID作为参数，返回一个ApiResponse对象
        public async Task<ApiResponse> GetUserInfoAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("获取用户信息失败！用户不存在！用户ID：{UserId}", userId);
                return new ApiResponse(false, "用户不存在！");
            }

            GetUserInformationDTO userInfo = new GetUserInformationDTO
            {
                NickName = user.NickName,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl
            };

            var comments = await _commentRepository.GetCommentsByUserIdAsync(userId);
            userInfo.CommentCount = comments.Count;  // 获取用户的评论数量

            var likedArticles = await _articleLikeRepository.GetLikedArticlesByUserIdAsync(userId);
            var ArticlesLike = likedArticles.Select(a => new ArticleSummaryDTO
            {
                Id = a.Id,
                Title = a.Title,
                CoverUrl = a.CoverUrl,
                ViewCount = a.ViewCount,
                LikeCount = a.LikeCount,
                CreatedAt = a.CreatedAt
            }).ToList();

            userInfo.ArticlesLike = ArticlesLike;  // 获取用户点赞的文章列表

            return new ApiResponse(true, "获取用户信息成功！", userInfo);
        }

        // 更新用户信息方法，接收一个UserInfomationDTO对象和一个用户ID作为参数，返回一个ApiResponse对象
        public async Task<ApiResponse> UpdateUserInfoAsync(int userId, UpdateUserInformationDTO userInfomationDTO)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("更新用户信息失败！用户不存在！用户ID：{UserId}", userId);
                return new ApiResponse(false, "用户不存在！");
            }

            // 更新用户信息（只能更改昵称和简介，用户名和邮箱不允许更改）
            user.NickName = userInfomationDTO.NickName;
            user.Bio = userInfomationDTO.Bio;

            // 保存更新后的用户信息到数据库
            var result = await _userRepository.UpdateUserAsync(user);
            if (result)
            {
                return new ApiResponse(true, "更新用户信息成功！");
            }
            else
            {
                _logger.LogError("更新用户信息失败！用户ID：{UserId}", userId);
                return new ApiResponse(false, "更新用户信息失败！");
            }
        }

        // 用户上传头像方法，接收一个用户ID和一个IFormFile file作为参数，返回一个ApiResponse对象
        public async Task<ApiResponse> UploadAvatarAsync(int userId, IFormFile file)
        {
            // 查用户是否存在
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("上传头像失败！用户不存在！用户ID：{UserId}", userId);
                return new ApiResponse(false, "用户不存在！", code: ResponseCode.UserNotFound);
            }

            // 调用图片上传方法
            var uploadResult = await _imageUploadService.UploadImageAsync(
                file,
                "Picture/Avatar/UserUploadAvatar",
                MaxPictureAvatarSize
            );

            // 3. 上传失败直接返回
            if (!uploadResult.Success)
            {
                return uploadResult;
            }

            // 4. 上传成功，取出图片路径
            var avatarUrl = uploadResult.Data?.ToString();

            // 5. 更新用户头像地址
            user.AvatarUrl = avatarUrl;

            var result = await _userRepository.UpdateUserAsync(user);
            if (!result)
            {
                _logger.LogError("上传头像成功，但保存用户头像地址失败！用户ID：{UserId}", userId);
                return new ApiResponse(false, "上传头像成功，但保存用户头像地址失败！");
            }

            return new ApiResponse(true, "上传头像成功！", avatarUrl);
        }

        // 管理员上传网站装饰素材图片方法，接收一个管理员用户ID和一个IFormFile file作为参数，返回一个ApiResponse对象
        public async Task<ApiResponse> UploadWebsiteImageAsync(int userId, IFormFile file)
        {
            // 查用户是否存在
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("上传文章封面失败！用户不存在！用户ID：{UserId}", userId);
                return new ApiResponse(false, "用户不存在！", code: ResponseCode.UserNotFound);
            }

            // 只有管理员可以上传网页背景等素材图片
            if (user.Role != UserRole.Admin)
            {
                _logger.LogWarning("上传网页装饰素材失败！用户没有权限！用户ID：{UserId}, 用户角色：{UserRole}", userId, user.Role);
                return new ApiResponse(false, "您没有权限上传网页装饰素材图片！");
            }

            // 调用图片上传方法
            var uploadResult = await _imageUploadService.UploadImageAsync(
                file,
                "Picture/WebsiteDecorationImage",
                MaxPictureWebsiteDecorationImageSize
            );
            // 上传失败直接返回
            if (!uploadResult.Success)
            {
                return uploadResult;
            }
            // 上传成功，取出图片路径并返回
            var imageUrl = uploadResult.Data?.ToString();
            return new ApiResponse(true, "上传网页装饰素材图片成功！", imageUrl);
        }

        // 获取自己发布的文章列表方法，接收一个用户ID作为参数，返回一个ApiResponse对象
        public async Task<ApiResponse> GetMyArticlesAsync(int userId, int page, int pageSize)
        {
            if(page <= 0 || pageSize <= 0)
            {
                _logger.LogWarning("分页参数无效！页码和每页记录数必须大于0！用户ID：{UserId}, 页码：{Page}, 每页记录数：{PageSize}", userId, page, pageSize);
                return new ApiResponse(false, "分页参数无效！页码和每页记录数必须大于0！");
            }
            else if(pageSize > 20)
            {
                _logger.LogWarning("每页记录数不能超过20！用户ID：{UserId}, 页码：{Page}, 每页记录数：{PageSize}", userId, page, pageSize);
                return new ApiResponse(false, "每页记录数不能超过20！");
            }
                var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("获取文章列表失败！用户不存在！用户ID：{UserId}", userId);
                return new ApiResponse(false, "用户不存在！");
            }
            List<GetArticleListDTO> getArticleListDTOs;
            List<Article> articles = await _articleRepository.GetArticlesByUserIdAsync(userId);

            // 将Article实体列表转换为GetArticleListDTO列表
            getArticleListDTOs = articles.Select(article => new GetArticleListDTO
            {
                Id = article.Id,
                Title = article.Title,
                Summary = article.Summary,
                CoverUrl = article.CoverUrl,
                ViewCount = article.ViewCount,
                LikeCount = article.LikeCount,
                CreatedAt = article.CreatedAt,
                CategoryName = article.Category?.Name ?? "未分类",
                TagNames = article.ArticleTags?.Select(at => at.Tag.Name).ToList() ?? new List<string>()
            }).ToList();

            // 对GetArticlesListDTO列表按照创建时间进行降序排序
            getArticleListDTOs = getArticleListDTOs.OrderByDescending(dto => dto.CreatedAt).ToList();

            // 对GetArticlesListDTO列表进行分页处理
            getArticleListDTOs = getArticleListDTOs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            int totalCount = articles.Count;  // 获取总记录数

            return new ApiResponse(
                true,
                "获取自己发布的文章列表成功！",
                new PagedResult<List<GetArticleListDTO>>
                {
                    Items = getArticleListDTOs,
                    TotalCount = totalCount
                }
            );
        }
    }
}
