using CuteBlogSystem.Config;
using CuteBlogSystem.Entity;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Repository
{
    public class CategoryRepository
    {
        private readonly MyDbContext _dbContext;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(MyDbContext dbContext, ILogger<CategoryRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // 新增分类
        public async Task<bool> AddCategoryAsync(Category category)
        {
            try
            {
                _dbContext.Categories.Add(category);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // 记录日志
                _logger.LogError($"分类添加失败！\nex.message:{ex.Message}");
                return false;
            }
        }

        // 获取所有分类
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _dbContext.Categories.ToListAsync<Category>();
        }

        // 根据ID删除分类
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                var category = await _dbContext.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    return false; // 分类不存在
                }
                _dbContext.Categories.Remove(category);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // 记录日志
                _logger.LogError($"分类删除失败！\nex.message:{ex.Message}");
                return false;
            }
        }

        // 根据ID更新分类
        public async Task<bool> UpdateCategoryAsync(int categoryId, Category updatedCategory)
        {
            try
            {
                var category = await _dbContext.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    return false; // 分类不存在
                }
                category.Name = updatedCategory.Name;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // 记录日志
                _logger.LogError($"分类更新失败！\nex.message:{ex.Message}");
                return false;
            }
        }

        // 根据ID获取分类
        public async Task<Category?> GetCategoryByIdAsync(int categoryId)
        {
            Category? category = await _dbContext.Categories.FindAsync(categoryId);
            if (category == null)
            {
                _logger.LogWarning($"未找到ID为{categoryId}的分类！");
            }
            return category;
        }
    }
}
