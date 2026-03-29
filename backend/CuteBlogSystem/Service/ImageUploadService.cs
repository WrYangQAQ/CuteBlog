using CuteBlogSystem.DTO;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Util;

namespace CuteBlogSystem.Service
{
    public class ImageUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageUploadService> _logger;

        // 通用允许的图片扩展名
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public ImageUploadService(IWebHostEnvironment environment, ILogger<ImageUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// 通用图片上传方法
        /// </summary>
        /// <param name="file">上传的图片文件</param>
        /// <param name="relativeFolder">相对于 wwwroot 的目录，例如 Picture/Avatar/UserUploadAvatar</param>
        /// <param name="maxSize">最大允许大小，单位：字节</param>
        /// <returns>成功时 Data 为图片相对路径；失败时返回错误信息</returns>
        public async Task<ApiResponse> UploadImageAsync(IFormFile file, string relativeFolder, long maxSize)
        {
            // 1. 判空
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("上传图片失败：未选择文件");
                return new ApiResponse(false, "请选择文件", code: ResponseCode.FileMissing);
            }

            // 2. 校验文件大小
            if (file.Length > maxSize)
            {
                _logger.LogWarning("上传图片失败：文件大小 {FileSize} 超过限制 {MaxSize}", file.Length, maxSize);
                return new ApiResponse(false, $"文件大小不能超过 {maxSize / 1024 / 1024} MB", code: ResponseCode.FileTooLarge);
            }

            // 3. 校验扩展名
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("上传图片失败：不支持的文件类型 {FileExtension}", extension);
                return new ApiResponse(false, $"不支持的文件类型，请上传 {string.Join(", ", _allowedExtensions)} 格式的图片",
                    code: ResponseCode.InvalidFileType);
            }

            // 4. 校验图片魔数
            bool isValidImage = await FileValidationHelper.IsValidImageAsync(file);
            if (!isValidImage)
            {
                _logger.LogWarning("上传图片失败：文件内容不合法，可能不是有效的图片");
                return new ApiResponse(false, "文件内容不合法，请上传有效的图片文件", code: ResponseCode.InvalidFileContent);
            }

            // 5. 物理目录
            var uploadFolder = Path.Combine(_environment.WebRootPath, relativeFolder);

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // 6. 生成唯一文件名
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            // 7. 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 8. 返回数据库中保存的相对路径
            var fileUrl = $"/{relativeFolder.Replace("\\", "/")}/{fileName}";

            return new ApiResponse(true, "图片上传成功！", fileUrl);
        }
    }
}