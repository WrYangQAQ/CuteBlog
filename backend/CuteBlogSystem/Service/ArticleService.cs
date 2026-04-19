using CuteBlogSystem.DTO;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Config;

namespace CuteBlogSystem.Service
{
    public class ArticleService
    {
        private readonly ArticleRepository _articleRepository;
        private readonly ArticleLikeRepository _articleLikeRepository;
        private readonly ImageUploadService _imageUploadService;
        private readonly ArticleTagRepository _articleTagRepository;
        private readonly UserRepository _userRepository;
        private readonly MyDbContext _dbContext; // 用于事务处理
        private readonly ILogger<ArticleService> _logger;
       
        public ArticleService(ArticleRepository articleRepository,
                              ArticleLikeRepository articleLikeRepository,
                              ImageUploadService imageUploadService,
                              ArticleTagRepository articleTagRepository,
                              UserRepository userRepository,
                              MyDbContext dbContext,
                              ILogger<ArticleService> logger)
        {
            _articleRepository = articleRepository;
            _articleLikeRepository = articleLikeRepository;
            _imageUploadService = imageUploadService;
            _articleTagRepository = articleTagRepository;
            _userRepository = userRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        // 获取文章列表
        public async Task<ApiResponse> GetAllArticlesAsync()
        {
            try
            {
                // 调用仓储层获取文章列表
                List<Article> articles = await _articleRepository.GetArticlesAsync();
                List<GetArticleListDTO> articleListDTOs = new List<GetArticleListDTO>();

                // 将文章列表转换为 DTO
                for (int i = 0; i < articles.Count; i++)
                {
                    GetArticleListDTO articleListDTO = new GetArticleListDTO(articles[i]);
                    articleListDTOs.Add(articleListDTO);
                }

                // 对文章按发布时间进行降序排序
                articleListDTOs = articleListDTOs.OrderByDescending(a => a.CreatedAt).ToList();

                // 返回成功响应
                return new ApiResponse(true, "获取文章列表成功！", articleListDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"获取文章列表失败！\nex.message:{ex.Message}");

                // 返回失败响应
                return new ApiResponse(false, $"获取文章列表失败！", null);
            }

        }

        // 模糊搜索
        public async Task<ApiResponse> SearchArticlesAsync(SearchArticleDTO searchArticleDTO)
        {
            try
            {
                // 调用仓储层，根据 SearchArticleDTO 查询文章列表
                List<Article> articles = await _articleRepository.SearchArticlesAsync(searchArticleDTO.Keyword, 
                                                                                      searchArticleDTO.ArticleTag,
                                                                                      searchArticleDTO.Category);
                List<GetArticleListDTO> articleListDTOs = new List<GetArticleListDTO>();

                // 将文章列表转换为 DTO
                for (int i = 0; i < articles.Count; i++)
                {
                    GetArticleListDTO articleListDTO = new GetArticleListDTO(articles[i]);
                    articleListDTOs.Add(articleListDTO);
                }

                // 返回成功响应
                return new ApiResponse(true, "搜索文章成功！", articleListDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"搜索文章失败！\nex.message:{ex.Message}");
                // 返回失败响应
                return new ApiResponse(false, $"搜索文章失败！");
            }
        }

        // 根据文章ID获取文章内容
        public async Task<ApiResponse> GetArticleContentByIdAsync(int articleId)
        {
            try
            {
                // 调用仓储层，根据id查询文章
                Article article = await _articleRepository.GetArticleByIdAsync(articleId);

                // 验证是否存在这篇文章
                if (article == null)
                {
                    return new ApiResponse(false, $"未找到ID为{articleId}的文章！");
                }

                // 文章存在，将文章内容转换为 DTO
                DisplayArticleDTO displayArticleDTO = new DisplayArticleDTO(article);
                
                // 返回成功响应
                return new ApiResponse(true, "获取文章内容成功！", displayArticleDTO);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"获取文章内容失败！\nex.message:{ex.Message}");

                // 返回失败响应
                return new ApiResponse(false, $"获取文章内容失败！");
            }
        }

        // 阅读文章时增加浏览量
        public async Task<ApiResponse> IncrementArticleViewCountAsync(int articleId, int stayDuration)
        {
            try
            {
                // 调用仓储层，根据id查询文章
                Article article = await _articleRepository.GetArticleByIdAsync(articleId);

                // 验证是否存在这篇文章
                if (article == null)
                {
                    return new ApiResponse(false, $"未找到ID为{articleId}的文章！");
                }

                // 验证停留时间是否满足增加浏览量的条件以及合理性（这里选择停留时间超过60秒，小于1个小时）
                if (stayDuration < 60 || stayDuration > 3600)
                {
                    return new ApiResponse(false, $"停留时间不合理，无法增加浏览量！");
                }

                article.ViewCount += 1; // 增加浏览量
                await _articleRepository.UpdateArticleAsync(article); // 更新文章数据
                return new ApiResponse(true, "增加文章浏览量成功！");
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"增加文章浏览量失败！\nex.message:{ex.Message}");

                return new ApiResponse(false, $"增加文章浏览量失败！");
            }
        }

        // 发布文章
        public async Task<ApiResponse> PublishArticleAsync(PublishArticleDTO publishArticleDTO, int userId)
        {
            try
            {
                // 扩展：对文章进行内容审核：例如敏感词过滤、垃圾内容检测等，如果审核不通过，则返回失败响应

                // 创建新的文章对象
                Article article = new Article
                {
                    UserId = userId,
                    Title = publishArticleDTO.Title,
                    Summary = publishArticleDTO.Summary,
                    Content = publishArticleDTO.Content,
                    CategoryId = publishArticleDTO.CategoryId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CoverUrl = publishArticleDTO.CoverUrl
                };
                // 调用仓储层的方法将文章保存到数据库中
                await _articleRepository.AddArticleAsync(article);
                return new ApiResponse(true, "发布文章成功！");
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"发布文章失败！\nex.message:{ex.Message}");

                return new ApiResponse(false, $"发布文章失败！");
            }
        }

        // （取消）点赞文章
        public async Task<ApiResponse> ToggleArticleLikeAsync(int articleId, int userId)
        {
            // 假设 _dbContext 是注入的 DbContext（或通过工作单元获取）
            // 如果仓储各自持有独立的 DbContext，则需要改为注入同一个 DbContext 实例
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. 获取文章（加行锁防止并发更新）
                var article = await _articleRepository.GetArticleByIdAsync(articleId);
                if (article == null)
                {
                    return new ApiResponse(false, $"未找到ID为{articleId}的文章！");
                }

                // 2. 检查用户是否已点赞
                bool hasLiked = await _articleLikeRepository.SearchArticleLikeExistAsync(userId, articleId);

                if (hasLiked)
                {
                    // 取消点赞：先删除记录，再减少 LikeCount（防止负数）
                    await _articleLikeRepository.RemoveArticleLikeAsync(userId, articleId);
                    if (article.LikeCount > 0)  // 确保 LikeCount 不会变成负数
                    {
                        article.LikeCount -= 1;
                    }
                    await _articleRepository.UpdateArticleAsync(article);
                }
                else
                {
                    // 添加点赞：先插入记录，再增加 LikeCount
                    await _articleLikeRepository.AddArticleLikeAsync(userId, articleId);
                    article.LikeCount += 1;
                    await _articleRepository.UpdateArticleAsync(article);
                }

                // 3. 提交事务
                await transaction.CommitAsync();
                return new ApiResponse(true, "操作成功！");
            }
            catch (Exception ex)
            {
                // 发生异常时自动回滚（using 会在离开时回滚，但最好显式处理）
                await transaction.RollbackAsync();
                // 记录异常日志（根据实际框架实现）
                _logger.LogError(ex, "ToggleArticleLike 失败，文章ID：{ArticleId}，用户ID：{UserId}", articleId, userId);
                return new ApiResponse(false, $"操作失败！");
            }
        }


        // 上传文章封面图片
        public async Task<ApiResponse> UploadArticleCoverAsync(IFormFile file)
        {
            var MaxPictureCoverSize = 5 * 1024 * 1024; // 5MB

            // 调用图片上传方法
            var uploadResult = await _imageUploadService.UploadImageAsync(
                file,
                "Picture/ArticleImage/Cover",
                MaxPictureCoverSize
            );

            // 3. 上传失败直接返回
            if (!uploadResult.Success)
            {
                // 记录异常日志
                _logger.LogWarning($"上传文章封面失败！");
                return uploadResult;
            }

            // 4. 上传成功，取出图片路径
            var coverUrl = uploadResult.Data?.ToString();

            return new ApiResponse(true, "封面上传成功！", coverUrl);
        }

        // 删除文章（同时删除这篇文章下所有评论与二级评论）
        // 只有文章作者或者管理员可以删除文章
        public async Task<ApiResponse> DeleteArticleAsync(int articleId, int userId)
        {
            try
            {
                if (!await CheckArticleAuthorOrAdminAsync(articleId, userId))
                {
                    return new ApiResponse(false, $"没有权限删除这篇文章！", code:ResponseCode.Unauthorized);
                }
                // 调用仓储层，根据id查询文章
                Article article = await _articleRepository.GetArticleByIdAsync(articleId);
                // 验证是否存在这篇文章
                if (article == null)
                {
                    return new ApiResponse(false, $"未找到ID为{articleId}的文章！", code: ResponseCode.NotFound);
                }
                // 调用仓储层的方法，删除这篇文章
                await _articleRepository.DeleteArticleByIdAsync(articleId);
                return new ApiResponse(true, "删除文章成功！");
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"删除文章失败！\nex.message:{ex.Message}");
                return new ApiResponse(false, $"删除文章失败！", code: ResponseCode.UploadFailed);
            }
        }


        // 编辑文章
        public async Task<ApiResponse> UpdateArticleContentAsync(int articleId, UpdateArticleDTO updateArticleDTO, int userId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(); // 开启事务

            try
            {
                Article article = await _articleRepository.GetArticleByIdAsync(articleId);
                if (article == null)
                {
                    return new ApiResponse(false, $"未找到ID为{articleId}的文章！");
                }

                // 验证是否为文章作者本人或者是管理员
                if (!await CheckArticleAuthorOrAdminAsync(articleId, userId))
                {
                    return new ApiResponse(false, $"没有权限编辑这篇文章！", code: ResponseCode.Unauthorized);
                }

                // 更新除标签外的文章内容
                article.Title = updateArticleDTO.Title;
                article.Summary = updateArticleDTO.Summary;
                article.Content = updateArticleDTO.Content;
                article.CategoryId = updateArticleDTO.CategoryId;
                article.UpdatedAt = DateTime.UtcNow;

                // 更新标签
                List<int> oldTagIds = await _articleTagRepository.GetTagIdsByArticleIdAsync(articleId); // 获取文章原有的标签ID列表
                List<int> newTagIds = updateArticleDTO.TagIds; // 获取文章新的标签ID列表

                // 对newTagIds进行判空
                if (newTagIds == null)
                {
                    newTagIds = new List<int>(); // 如果新的标签ID列表为null，则初始化为空列表
                }

                // 根据现在有标签ID对原有标签ID做删除
                foreach (int oldTagId in oldTagIds)
                {
                    if (!newTagIds.Contains(oldTagId))
                    {
                        await _articleTagRepository.DeleteArticleTagAsync(articleId, oldTagId);
                    }
                }

                oldTagIds = await _articleTagRepository.GetTagIdsByArticleIdAsync(articleId); // 重新获取文章原有的标签ID列表（删除后）

                // 根据现在有标签ID对原有标签ID做添加
                foreach (int newTagId in newTagIds)
                {
                    if (!oldTagIds.Contains(newTagId))
                    {
                        await _articleTagRepository.AddArticleTagAsync(articleId, newTagId);
                    }
                }
                // 标签更新完成

                await _articleRepository.UpdateArticleAsync(article);

                // 提交事务
                await transaction.CommitAsync();
                return new ApiResponse(true, "编辑文章成功！");
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"编辑文章失败！\nex.message:{ex.Message}");
                await transaction.RollbackAsync(); // 发生异常时回滚事务
                return new ApiResponse(false, $"编辑文章失败！", code: ResponseCode.UpdateFailed);
            }
        }

        // 置顶文章
        public async Task<ApiResponse> ToggleArticleTopAsync(int articleId)
        {
            try
            {
                // 调用仓储层，根据id查询文章
                Article article = await _articleRepository.GetArticleByIdAsync(articleId);
                // 验证是否存在这篇文章
                if (article == null)
                {
                    return new ApiResponse(false, $"未找到ID为{articleId}的文章！");
                }
                article.IsTop = !article.IsTop; // 取反置顶状态
                await _articleRepository.UpdateArticleAsync(article); // 更新文章数据
                return new ApiResponse(true, "操作成功！");
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"（取消）置顶文章失败！\nex.message:{ex.Message}");
                return new ApiResponse(false, $"（取消）操作失败！");
            }
        }

        // 获取置顶文章列表
        public async Task<ApiResponse> GetTopArticlesAsync()
        {
            try
            {
                // 调用仓储层获取置顶文章列表
                List<Article> topArticles = await _articleRepository.GetTopArticlesAsync();
                List<GetArticleListDTO> topArticleDTOs = new List<GetArticleListDTO>();
                // 将置顶文章列表转换为 DTO
                for (int i = 0; i < topArticles.Count; i++)
                {
                    GetArticleListDTO articleListDTO = new GetArticleListDTO(topArticles[i]);
                    topArticleDTOs.Add(articleListDTO);
                }
                // 返回成功响应
                return new ApiResponse(true, "获取置顶文章列表成功！", topArticleDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"获取置顶文章失败！\nex.message:{ex.Message}");

                // 返回失败响应
                return new ApiResponse(false, $"获取置顶文章列表失败！", null);
            }
        }

        // 推荐文章
        public async Task<ApiResponse> ToggleArticleRecommendAsync(int articleId)
        {
            try
            {
                // 调用仓储层，根据id查询文章
                Article article = await _articleRepository.GetArticleByIdAsync(articleId);
                // 验证是否存在这篇文章
                if (article == null)
                {
                    return new ApiResponse(false, $"未找到ID为{articleId}的文章！");
                }
                article.IsRecommend = !article.IsRecommend; // 取反推荐状态
                await _articleRepository.UpdateArticleAsync(article); // 更新文章数据
                return new ApiResponse(true, "操作成功！");
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"（取消）推荐文章操作失败！\nex.message:{ex.Message}");

                return new ApiResponse(false, $"（取消）推荐文章操作失败！");
            }

        }

        // 获取推荐文章列表
        public async Task<ApiResponse> GetRecommendArticlesAsync()
        {
            try
            {
                // 调用仓储层获取推荐文章列表
                List<Article> recommendArticles = await _articleRepository.GetRecommendedArticlesAsync();
                List<GetArticleListDTO> recommendArticleDTOs = new List<GetArticleListDTO>();
                // 将推荐文章列表转换为 DTO
                for (int i = 0; i < recommendArticles.Count; i++)
                {
                    GetArticleListDTO articleListDTO = new GetArticleListDTO(recommendArticles[i]);
                    recommendArticleDTOs.Add(articleListDTO);
                }
                // 返回成功响应
                return new ApiResponse(true, "获取推荐文章列表成功！", recommendArticleDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"获取推荐文章列表操作失败！\nex.message:{ex.Message}");

                // 返回失败响应
                return new ApiResponse(false, $"获取推荐文章列表失败！", null);
            }
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
