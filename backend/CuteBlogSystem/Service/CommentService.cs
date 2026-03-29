using CuteBlogSystem.DTO;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Util;

namespace CuteBlogSystem.Service
{
    public class CommentService
    {
        private readonly UserRepository _userRepository;
        private readonly ArticleRepository _articleRepository;
        private readonly CommentRepository _commentRepository;
        private readonly ILogger<CommentService> _logger;
       


        public CommentService(UserRepository userRepository, ArticleRepository articleRepository, CommentRepository commentRepository, ILogger<CommentService> logger)
        {
            _userRepository = userRepository;
            _articleRepository = articleRepository;
            _commentRepository = commentRepository;
            _logger = logger;
        }

        // 发布评论
        public async Task<ApiResponse> PublishCommentAsync(PublishCommentDTO commentDto, int userId, int articleId)
        {
            if(string.IsNullOrWhiteSpace(commentDto.Content))
            {
                return new ApiResponse(false, "评论内容不能为空！");
            }

            // 检测用户是否存在
            if(!await _userRepository.UserExistsByIdAsync(userId))
            {
                return new ApiResponse(false, "用户不存在！");
            }
            
            // 检测文章是否存在
            if(!await _articleRepository.ArticleExistsByIdAsync(articleId))
            {
                return new ApiResponse(false, "文章不存在！");
            }

            // 检测父评论是否存在（如果有）
            if(commentDto.ParentCommentId.HasValue && !await _commentRepository.GetCommentExistByIdAsync(commentDto.ParentCommentId.Value))
            {
                return new ApiResponse(false, "父评论不存在！");
            }

            // 对评论内容进行检测
            if(!CommentAuditHelper.IsCommentApproved(commentDto.Content))
            {
                return new ApiResponse(false, "评论内容包含敏感词，请修改后再试！");
            }

            // TODO: 验证输入数据，检查文章是否存在，检查用户是否有权限评论等
            Comment comment = new Comment
            {
                Content = commentDto.Content,
                ParentCommentId = commentDto.ParentCommentId,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ArticleId = articleId
            };

            try
            {
                await _commentRepository.AddCommentAsync(comment);
                return new ApiResponse(true, "评论发布成功！");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"发布评论时发生错误！\nex.message:{ex.Message}");
                return new ApiResponse(false, "发布评论时发生错误，请稍后再试！");
            }
        }

        // 删除评论
        public async Task<ApiResponse> DeleteCommentAsync(int commentId, int userId)
        {
            Comment comment = await _commentRepository.GetCommentByIdAsync(commentId);
            if (comment == null)
            {
                return new ApiResponse(false, "评论不存在！", code: ResponseCode.NotFound);
            }
            // 判断是否为评论作者本人或者是管理员
            if (comment.UserId != userId && !await CheckArticleAuthorOrAdminAsync(comment.ArticleId, userId))
            {
                return new ApiResponse(false, "您没有权限删除此评论！", code: ResponseCode.Unauthorized);
            }
            try
            {
                await _commentRepository.DeleteCommentByIdAsync(commentId);
                return new ApiResponse(true, "评论删除成功！");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"删除评论时发生错误！\nex.message:{ex.Message}");
                return new ApiResponse(false, "删除评论时发生错误，请稍后再试！", code: ResponseCode.Conflict);
            }
        }

        // 获取文章的评论列表
        public async Task<ApiResponse> GetCommentsListAsync(int articleId)
        {
            if(!await _articleRepository.ArticleExistsByIdAsync(articleId))
            {
                return new ApiResponse(false, "文章不存在！", code: ResponseCode.NotFound);
            }
            
            List<Comment> comments = await _commentRepository.GetCommentsByArticleIdAsync(articleId);
            List<GetCommentDTO> commentDTOs = new List<GetCommentDTO>();

            // 将评论列表转换为DTO列表
            foreach (var comment in comments)
            {
                User? user = await _userRepository.GetUserByIdAsync(comment.UserId);
                if (user != null)
                {
                    comment.User = user; // 将用户信息附加到评论对象中
                }
                GetCommentDTO commentDTO = new GetCommentDTO(comment);
                commentDTOs.Add(commentDTO);
            }

            // 对评论列表进行排序，先按创建时间升序排序，再按用户昵称升序排序
            commentDTOs = commentDTOs.OrderBy(c => c.CreatedAt).ThenBy(c => c.UserName).ToList();

            return new ApiResponse(true, "获取评论列表成功！", data: commentDTOs);

        }


        // 判断是否为文章作者本人或者是管理员
        public async Task<bool> CheckArticleAuthorOrAdminAsync(int articleId, int userId)
        {
            Article article = await _articleRepository.GetArticleByIdAsync(articleId);
            if (article == null)
            {
                return false; // 文章不存在，返回false
            }
            if (article.UserId == userId)
            {
                return true; // 是文章作者，返回true
            }
            User? user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false; // 用户不存在，返回false
            }
            else
            {
                if (user.Role == UserRole.Admin)
                {
                    return true; // 是管理员，返回true
                }
            }
            return false; // 既不是文章作者也不是管理员，返回false
        }
    }
}
