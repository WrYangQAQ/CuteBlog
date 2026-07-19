using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;
using CuteBlogSystem.DTO.Blog;

namespace CuteBlogSystem.Repository
{
    public class TagRepository
    {
        private readonly MyDbContext _dbContext;
        private readonly ILogger<TagRepository> _logger;

        public TagRepository(MyDbContext dbContext, ILogger<TagRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // 获取所有标签
        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _dbContext.Tags.ToListAsync<Tag>();
        }

        // 根据ID获取单个标签
        public async Task<Tag?> GetTagByIdAsync(int tagId)
        {
            return await _dbContext.Tags.FindAsync(tagId);
        }

        // 添加标签
        public async Task<bool> AddTagAsync(GetTagDTO tag)
        {
            try
            {
                var newTag = new Tag
                {
                    Name = tag.Name,
                    CategoryId = tag.CategoryId
                };
                _dbContext.Tags.Add(newTag);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // 记录日志
                _logger.LogError($"标签添加失败！\nex.message:{ex.Message}");
                return false;
            }
        }

        // 根据ID删除标签
        public async Task<bool> DeleteTagAsync(int tagId)
        {
            try
            {
                var tag = await _dbContext.Tags.FindAsync(tagId);
                if (tag == null)
                {
                    return false; // 标签不存在
                }
                _dbContext.Tags.Remove(tag);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // 记录日志
                _logger.LogError($"标签删除失败！\nex.message:{ex.Message}");
                return false;
            }
        }

        // 修改标签
        public async Task<bool> UpdateTagAsync(Tag updatedTag, int tagId)
        {
            try
            {
                var tag = await _dbContext.Tags.FindAsync(tagId);
                if (tag == null)
                {
                    return false; // 标签不存在
                }

                // 更新标签属性
                tag.Name = updatedTag.Name;
                tag.CategoryId = updatedTag.CategoryId;
                _dbContext.Tags.Update(tag);

                // 保存更改
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // 记录日志
                _logger.LogError($"标签修改失败！\nex.message:{ex.Message}");
                return false;
            }
        }

        // 根据分类ID获取标签列表
        public async Task<List<Tag>> GetTagsByCategoryIdAsync(int categoryId)
        {
            return await _dbContext.Tags.Where(t => t.CategoryId == categoryId).ToListAsync();
        }

        // 根据标签id获取文章数量
        public async Task<int> GetArticleCountByTagIdAsync(int tagId)
        {
            int count = await _dbContext.ArticleTags.CountAsync(at => at.TagId == tagId);
            return count;
        }

        // 根据标签名获取对应标签id
        public async Task<int?> GetTagIdByTagname(string tagName)
        {
            var tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
            return tag?.Id;
        }

        // 根据标签id获取标签名（单次查询）
        public async Task<string?> GetTagNamesByIdsAsync(int tagId)
        {
            var tag = await _dbContext.Tags.FindAsync(tagId);
            return tag?.Name;
        }

        // 根据标签id获取标签名（批量查询）
        public async Task<List<string>> GetTagNamesByIdsAsync(List<int> tagIds)
        {
            var tags = await _dbContext.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();
            return tags.Select(t => t.Name).ToList();
        }

        // 批量获取每个标签对应的文章数量，避免按标签逐个 Count 造成 N+1 查询
        public async Task<List<TagArticleCountDTO>> GetArticleCountsByTagIdsAsync(List<int> tagIds)
        {
            if (tagIds == null || tagIds.Count == 0)
            {
                return new List<TagArticleCountDTO>();
            }

            return await _dbContext.ArticleTags
                .Where(at => tagIds.Contains(at.TagId))
                .GroupBy(at => at.TagId)
                .Select(g => new TagArticleCountDTO
                {
                    TagId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
        }

        // 根据标签名称模糊查询标签列表
        public async Task<List<Tag>> GetTagsByNameAsync(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return new List<Tag>();
            }

            tagName = tagName.Trim();

            return await _dbContext.Tags
                .Where(tag => tag.Name.Contains(tagName))
                .ToListAsync();
        }
    }
}
