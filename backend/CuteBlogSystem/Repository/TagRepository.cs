using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using CuteBlogSystem.DTO;
using Microsoft.EntityFrameworkCore;

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
    }
}
