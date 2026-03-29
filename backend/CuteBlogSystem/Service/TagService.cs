using CuteBlogSystem.Entity;
using CuteBlogSystem.Repository;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Service
{
    public class TagService
    {
        private readonly TagRepository _tagRepository;
        private readonly ArticleRepository _articleRepository;
        private readonly ILogger<TagService> _logger;
        public TagService(TagRepository tagRepository, ArticleRepository articleRepository, ILogger<TagService> logger)
        {
            _tagRepository = tagRepository;
            _articleRepository = articleRepository;
            _logger = logger;
        }


        // 获取所有标签
        public async Task<ApiResponse> GetAllTagsAsync()
        {
            List<Tag> tags = await _tagRepository.GetAllTagsAsync();
            if (tags == null || tags.Count == 0)
            {
                return new ApiResponse(false, "没有找到任何标签", code: ResponseCode.NotFound);
            }
            else
            {
                return new ApiResponse(true, "标签列表获取成功", tags);
            }
        }


        // 添加标签
        public async Task<ApiResponse> AddTagAsync(Tag tag)
        {
            bool success = await _tagRepository.AddTagAsync(tag);
            if (success)
            {
                return new ApiResponse(true, "标签添加成功！", tag);
            }
            else
            {
                return new ApiResponse(false, "标签添加失败！");
            }
        }


        // 根据ID删除标签
        public async Task<ApiResponse> DeleteTagAsync(int tagId)
        {
            // 删除标签前先删除关联的文章标签关系
            var articles = await _articleRepository.GetArticlesAsync();
            foreach (var article in articles)
            {
                var articleTagsToRemove = article.ArticleTags.Where(at => at.TagId == tagId).ToList();
                foreach (var articleTag in articleTagsToRemove)
                {
                    article.ArticleTags.Remove(articleTag);
                }
                if (articleTagsToRemove.Count > 0)
                {
                    await _articleRepository.UpdateArticleAsync(article);
                }
            }
            
            bool success = await _tagRepository.DeleteTagAsync(tagId);
            if (success)
            {
                return new ApiResponse(true, "标签删除成功！");
            }
            else
            {
                return new ApiResponse(false, "标签删除失败！");
            }
        }

        // 根据ID更新标签
        public async Task<ApiResponse> UpdateTagAsync(Tag updatedTag)
        {
            try
            {
                bool success = await _tagRepository.UpdateTagAsync(updatedTag);
                if (success)
                {
                    return new ApiResponse(true, "标签更新成功！", updatedTag);
                }
                else
                {
                    return new ApiResponse(false, "标签更新失败！");
                }
            }
            catch (Exception ex)
            {
                // 记录日志
                _logger.LogError($"标签更新失败！\nex.message:{ex.Message}");
                return new ApiResponse(false, "标签更新失败！"); ;
            }
        }
    }
}
