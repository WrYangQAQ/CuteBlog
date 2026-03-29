using CuteBlogSystem.Entity;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Repository;

namespace CuteBlogSystem.Service
{
    public class CategoryService
    {
        public readonly CategoryRepository _categoryRepository;
        public CategoryService(CategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // 新增分类
        public async Task<ApiResponse> AddCategoryAsync(Category category)
        {
            bool success = await _categoryRepository.AddCategoryAsync(category);
            if (success)
            {
                return new ApiResponse(true, "分类添加成功！", category);
            }
            else
            {
                return new ApiResponse(false, "分类添加失败！");
            }
        }

        // 获取所有分类
        public async Task<ApiResponse> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllCategoriesAsync();
            if (categories == null || categories.Count == 0)
            {
                return new ApiResponse(false, "目前还没有分类！");
            }
            return new ApiResponse(true, "获取分类成功！", categories);
        }

        // 根据id删除分类
        public async Task<ApiResponse> DeleteCategoryAsync(int categoryId)
        {
            bool success = await _categoryRepository.DeleteCategoryAsync(categoryId);
            if (success)
            {
                return new ApiResponse(true, "分类删除成功！");
            }
            else
            {
                return new ApiResponse(false, "分类删除失败！");
            }
        }

        // 修改分类
        public async Task<ApiResponse> UpdateCategoryAsync(int categoryId, Category updatedCategory)
        {
            Category? category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
            if (category == null)
            {
                return new ApiResponse(false, "分类不存在！");
            }
            category.Name = updatedCategory.Name;
            bool success = await _categoryRepository.AddCategoryAsync(category);
            if (success)
            {
                return new ApiResponse(true, "分类修改成功！", category);
            }
            else
            {
                return new ApiResponse(false, "分类修改失败！");
            }
        }
    }
}
