using CuteBlogSystem.DTO;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Enum;
using System.Text;

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
            List<GetCategoryDTO> categoryListDTOs = categories.Select(c => new GetCategoryDTO(c)).ToList();
            if (categoryListDTOs == null || categoryListDTOs.Count == 0)
            {
                return new ApiResponse(false, "目前还没有分类！", code: ResponseCode.NotFound);
            }
            return new ApiResponse(true, "获取分类成功！", categoryListDTOs, ResponseCode.Success);
        }

        // 根据id删除分类
        public async Task<ApiResponse> DeleteCategoryAsync(int categoryId)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
            if (category == null)
            {
                return new ApiResponse(false, "分类不存在！");
            }

            var hasTags = await _categoryRepository.HasTagsAsync(categoryId);
            if (hasTags)
            {
                return new ApiResponse(false, "该分类下仍有关联标签，不能删除！");
            }

            bool success = await _categoryRepository.DeleteCategoryAsync(categoryId);
            return success
                ? new ApiResponse(true, "分类删除成功！")
                : new ApiResponse(false, "分类删除失败！");
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
            bool success = await _categoryRepository.UpdateCategoryAsync(category.Id, category);
            if (success)
            {
                return new ApiResponse(true, "分类修改成功！", category);
            }
            else
            {
                return new ApiResponse(false, "分类修改失败！");
            }
        }

        // 根据id查询分类
        public async Task<string> GetCategoryByIdAsync(int categoryId)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
            if (category == null)
            {
                return "分类不存在！";
            }
            else
            {
                return $"分类ID：{category.Id}\n分类名称：{category.Name}";
            }
        }

        
    }
}
