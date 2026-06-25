using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace CuteBlogSystem.Service
{
    public class ArticleService
    {
        private readonly ArticleRepository _articleRepository;
        private readonly ArticleLikeRepository _articleLikeRepository;
        private readonly ImageUploadService _imageUploadService;
        private readonly ArticleTagRepository _articleTagRepository;
        private readonly UserRepository _userRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;
        private readonly MyDbContext _dbContext; // 用于事务处理
        private readonly ILogger<ArticleService> _logger;

        public ArticleService(ArticleRepository articleRepository,
                              ArticleLikeRepository articleLikeRepository,
                              ImageUploadService imageUploadService,
                              ArticleTagRepository articleTagRepository,
                              UserRepository userRepository,
                              CategoryRepository categoryRepository,
                              TagRepository tagRepository,
                              MyDbContext dbContext,
                              ILogger<ArticleService> logger)
        {
            _articleRepository = articleRepository;
            _articleLikeRepository = articleLikeRepository;
            _imageUploadService = imageUploadService;
            _articleTagRepository = articleTagRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        // 获取所有文章列表
        public async Task<ApiResponse> GetAllArticlesAsync()
        {
            try
            {
                // 调用仓储层获取文章列表
                List<Article> articles = await _articleRepository.GetArticlesAsync();

                // 将文章列表转换为 DTO
                var articleListDTOs = articles.Select(article => 
                    new GetArticleListDTO(article)).ToList();

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
                List<Article> articles = await _articleRepository.SearchArticlesAsync(
                    searchArticleDTO.Keyword,
                    searchArticleDTO.ArticleTag,
                    searchArticleDTO.Category);

                // 将文章列表转换为 DTO
                var articleListDTOs = articles.Select(article => new GetArticleListDTO(article)).ToList();

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

        // 根据文章ID获取文章分类
        public async Task<ApiResponse> GetArticleCategoryByIdAsync(int articleId)
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
                // 文章存在，根据分类ID查询分类名称
                var category = await _categoryRepository.GetCategoryByIdAsync(article.CategoryId);
                var categoryName = category != null ? category.Name : "未知分类";
                // 返回成功响应
                return new ApiResponse(true, "获取文章分类成功！", categoryName);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"获取文章分类失败！\nex.message:{ex.Message}");
                // 返回失败响应
                return new ApiResponse(false, $"获取文章分类失败！");
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
            string? finalCoverUrl = null;
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // ToDo：对文章进行内容审核：例如敏感词过滤、垃圾内容检测等，如果审核不通过，则返回失败响应


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

                // 将封面图片从临时路径移动到正式路径（如果有封面图片）
                var finalize = await _imageUploadService.FinalizeTempCoverAsync(
                       publishArticleDTO.CoverUrl, userId,
                       "Picture/ArticleImage/CoverTemp",
                       "Picture/ArticleImage/Cover");

                if (!finalize.Success)
                {
                    return finalize; // 如果移动失败，直接返回失败响应
                }

                finalCoverUrl = finalize.Data?.ToString();
                article.CoverUrl = finalCoverUrl!;


                // 调用仓储层的方法将文章保存到数据库中
                await _articleRepository.AddArticleAsync(article);

                // 将文章与标签关联起来，并保存到数据库中
                if (publishArticleDTO.TagIds?.Any() == true)
                {
                    var validTagIds = await _dbContext.Tags
                        .Where(t => publishArticleDTO.TagIds.Contains(t.Id)
                                    && t.CategoryId == publishArticleDTO.CategoryId)
                        .Select(t => t.Id)
                        .ToListAsync();

                    foreach (int tagId in validTagIds.Distinct())
                    {
                        await _articleTagRepository.AddArticleTagAsync(article.Id, tagId);
                    }

                    // 如果某些传入的 ID 无效，记录警告
                    var invalidIds = publishArticleDTO.TagIds.Except(validTagIds);
                    if (invalidIds.Any())
                    {
                        _logger.LogWarning("无效的标签ID: {InvalidIds}", string.Join(",", invalidIds));
                    }
                }

                await transaction.CommitAsync();
                return new ApiResponse(true, "发布文章成功！");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // 记录异常日志
                _logger.LogError(ex, $"发布文章失败！\nex.message:{ex.Message}");

                // 关键补偿：DB失败时删掉刚转正的封面
                if (!string.IsNullOrWhiteSpace(finalCoverUrl))
                {
                    await _imageUploadService.TryDeleteFinalCoverAsync(
                        finalCoverUrl,
                        "Picture/ArticleImage/Cover");
                }

                return new ApiResponse(false, $"发布文章失败！");
            }
        }

        // （取消）点赞文章
        public async Task<ApiResponse> ToggleArticleLikeAsync(int articleId, int userId)
        {

            // 注入同一个 DbContext 实例
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
        public async Task<ApiResponse> UploadArticleCoverAsync(IFormFile file, int userId)
        {
            // 1. 验证用户是否登录
            if (userId == 0)
            {
                return new ApiResponse(false, $"请先登录！", code: ResponseCode.Unauthorized);
            }

            var MaxPictureCoverSize = 5 * 1024 * 1024; // 5MB

            // 2. 清理用户临时存储空间中过期的封面图片
            var tempCleanup = await _imageUploadService.CleanupExpiredTempCoversAsync(
                $"Picture/ArticleImage/CoverTemp/{userId}");
            if (!tempCleanup.Success)
            {
                return tempCleanup; // 如果清理失败，直接返回失败响应
            }

            // 3. 检测用户临时存储空间是否已满
            var tempQuotaCheck = await _imageUploadService.CheckUserTempQuotaAsync(userId, file.Length);
            if (!tempQuotaCheck.Success)
            {
                return tempQuotaCheck; // 如果检测失败，直接返回失败响应
            }

            // 4. 调用图片上传方法
            var uploadResult = await _imageUploadService.UploadImageAsync(
                file,
                $"Picture/ArticleImage/CoverTemp/{userId}",
                MaxPictureCoverSize
            );

            // 5. 上传失败直接返回
            if (!uploadResult.Success)
            {
                // 记录异常日志
                _logger.LogWarning($"上传文章封面失败！");
                return uploadResult;
            }

            // 6. 上传成功，取出图片路径
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
                    return new ApiResponse(false, $"没有权限删除这篇文章！", code: ResponseCode.Unauthorized);
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
                
                // 将置顶文章列表转换为 DTO
                var topArticleDTOs = topArticles.Select(article =>  new GetArticleListDTO(article));

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

                // 将推荐文章列表转换为 DTO
                var recommendArticleDTOs = recommendArticles.Select(article => new GetArticleListDTO(article)).ToList();

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

        // 获取最新发布的五篇文章
        public async Task<ApiResponse> GetLatestArticlesAsync()
        {
            try
            {
                // 调用仓储层获取最新发布的五篇文章
                List<Article> latestArticles = await _articleRepository.GetLatestArticlesAsync();

                // 将文章列表转换为 DTO
                var latestArticleDTOs = latestArticles.Select(article => new GetArticleListDTO(article)).ToList();

                // 返回成功响应
                return new ApiResponse(true, "获取最新发布的五篇文章成功！", latestArticleDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"获取最新发布的五篇文章失败！\nex.message:{ex.Message}");
                // 返回失败响应
                return new ApiResponse(false, $"获取最新发布的五篇文章失败！", null);
            }
        }

        // 根据分类名查询文章列表
        public async Task<ApiResponse> GetArticlesByCategoryNameAsync(string categoryName)
        {
            try
            {
                // 根据分类名查询该分类id
                var categoryId = await _categoryRepository.SearchCategoriesByNameAsync(categoryName);

                if (categoryId == null)
                {
                    return new ApiResponse(false, $"未找到分类名为'{categoryName}'的分类！", null);
                }

                // 调用仓储层，根据分类名查询文章列表
                List<Article> articles = await _articleRepository.GetArticlesByCategoryAsync(categoryId);

                // 将文章列表转换为 DTO
                var articleListDTOs = articles.Select(article => new GetArticleListDTO(article));

                // 返回成功响应
                return new ApiResponse(true, $"根据分类名'{categoryName}'查询文章列表成功！", articleListDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"根据分类名'{categoryName}'查询文章列表失败！\nex.message:{ex.Message}");
                // 返回失败响应
                return new ApiResponse(false, $"根据分类名'{categoryName}'查询文章列表失败！", null);
            }
        }

        // 根据分类名查询文章列表(重载加上具体返回数量)
        public async Task<ApiResponse> GetArticlesByCategoryNameAsync(string categoryName, int count)
        {
            try
            {
                // 根据分类名查询该分类id
                var categoryId = await _categoryRepository.SearchCategoriesByNameAsync(categoryName);

                if (categoryId == null)
                {
                    return new ApiResponse(false, $"未找到分类名为'{categoryName}'的分类！", null);
                }

                // 调用仓储层，根据分类名查询文章列表
                List<Article> articles = await _articleRepository.GetArticlesByCategoryAsync(categoryId);

                // 对文章列表按发布时间进行降序排序，将文章列表转换为 DTO
                var articleListDTOs = articles
                    .OrderByDescending(article => article.CreatedAt)
                    .Select(article => new GetArticleListDTO(article)).Take(count).ToList();

                // 返回成功响应
                return new ApiResponse(true, $"根据分类名'{categoryName}'查询文章列表成功！", articleListDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"根据分类名'{categoryName}'查询文章列表失败！\nex.message:{ex.Message}");
                // 返回失败响应
                return new ApiResponse(false, $"根据分类名'{categoryName}'查询文章列表失败！", null);
            }
        }

        // 根据分类名查询文章列表(重载加上具体返回数量和排序方式)
        public async Task<ApiResponse> GetArticlesByCategoryNameAsync(string categoryName, int count, string sortBy)
        {
            try
            {
                // 根据分类名查询该分类id
                var categoryId = await _categoryRepository.SearchCategoriesByNameAsync(categoryName);

                if (categoryId == null)
                {
                    return new ApiResponse(false, $"未找到分类名为'{categoryName}'的分类！", null);
                }

                // 调用仓储层，根据分类名查询文章列表
                List<Article> articles = await _articleRepository.GetArticlesByCategoryAsync(categoryId);

                // 对文章列表按排序方式进行排序
                articles = sortBy switch
                {
                    "Latest" => articles.OrderByDescending(a => a.CreatedAt).ToList(),
                    "MostLiked" => articles.OrderByDescending(a => a.LikeCount).ToList(),
                    "MostViewed" => articles.OrderByDescending(a => a.ViewCount).ToList(),
                    _ => articles.OrderByDescending(a => a.CreatedAt).ToList()
                };

                // 将文章列表转换为 DTO
                var articleListDTOs = articles
                    .Select(article => new GetArticleListDTO(article)).Take(count).ToList();
                // 返回成功响应
                return new ApiResponse(true, $"根据分类名'{categoryName}'查询文章列表成功！", articleListDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"根据分类名'{categoryName}'查询文章列表失败！\nex.message:{ex.Message}");
                // 返回失败响应
                return new ApiResponse(false, $"根据分类名'{categoryName}'查询文章列表失败！", null);
            }
        }

        // 根据分类id查询文章列表
        public async Task<ApiResponse> GetArticlesByCategoryIdAsync(int categoryId)
        {
            try
            {
                // 调用仓储层，根据分类id查询文章列表
                List<Article> articles = await _articleRepository.GetArticlesByCategoryAsync(categoryId);

                // 将文章列表转换为 DTO
                var articleListDTOs = articles.Select(article => new GetArticleListDTO(article)).ToList();

                // 返回成功响应
                return new ApiResponse(true, $"根据分类ID'{categoryId}'查询文章列表成功！", articleListDTOs);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                _logger.LogError(ex, $"根据分类ID'{categoryId}'查询文章列表失败！\nex.message:{ex.Message}");
                // 返回失败响应
                return new ApiResponse(false, $"根据分类ID'{categoryId}'查询文章列表失败！", code: ResponseCode.NotFound);
            }
        }

        // 根据标签名查询对应列表下的文章
        public async Task<ApiResponse> GetArticlesByTagNameAsync(string tagName)
        {
            int? tagId = await _tagRepository.GetTagIdByTagname(tagName);
            if (tagId == null)
            {
                return new ApiResponse(false, $"未找到标签名为'{tagName}'的标签！", code: ResponseCode.NotFound);
            }
            else
            {
                List<int> articleIds = await _articleTagRepository.GetArticleIdsByTagIdAsync(tagId.Value);
                List<Article> articles = await _articleRepository.GetArticlesByIdsAsync(articleIds);
                List<GetArticleListDTO> articleListDTOs = new List<GetArticleListDTO>();
                foreach (int i in articleIds)
                {
                    articleListDTOs.Add(new GetArticleListDTO(articles.FirstOrDefault(a => a.Id == i)!));
                }
                return new ApiResponse(true, $"根据标签名'{tagName}'查询文章列表成功！", articleListDTOs);
            }
        }

        // 根据文章id查询对应文章具体内容(id集合多次查询)
        public async Task<List<string>> GetArticleContentByIdAsync(List<int> articleIds)
        {
            List<Article> articles = await _articleRepository.GetArticlesByIdsAsync(articleIds);
            List<string> result = new List<string>();
            foreach (int id in articleIds)
            {
                Article? article = articles.FirstOrDefault(a => a.Id == id);
                if (article != null)
                {
                    result.Add($"文章ID: {article.Id}\n标题: {article.Title}\n内容: {article.Content}\n\n");
                }
                else
                {
                    result.Add($"未找到ID为{id}的文章！\n\n");
                }
            }
            return result;
        }

        // 根据文章id查询对应标签列表(单次查询文章，批量查询标签)
        public async Task<ApiResponse> GetArticleTagsListByArticleIdAsync(int articleId)
        {
            Article article = await _articleRepository.GetArticleByIdAsync(articleId);
            if (article == null)
            {
                return new ApiResponse(false, $"未找到ID为{articleId}的文章！", code: ResponseCode.ArticleNotFound);
            }
            List<int> tagIds = await _articleTagRepository.GetTagIdsByArticleIdAsync(articleId);
            List<string> tagNames = await _tagRepository.GetTagNamesByIdsAsync(tagIds);
            return new ApiResponse(true, $"根据文章ID'{articleId}'查询标签列表成功！", tagNames);
        }
    }
}
