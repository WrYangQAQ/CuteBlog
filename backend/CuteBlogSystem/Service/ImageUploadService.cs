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

        
        // 通用图片上传方法
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


        
        // 将临时封面转换为正式封面，移动文件并校验配额，同时触发过期清理
        public async Task<ApiResponse> FinalizeTempCoverAsync(string tempUrl, int userId, string tempRoot, string finalRoot)
        {
            // 校验空串
            if (string.IsNullOrWhiteSpace(tempUrl))
            {
                _logger.LogWarning("FinalizeTempCover: 临时URL为空");
                return new ApiResponse(false, "临时封面路径为空", code: ResponseCode.InvalidInput);
            }

            // 校验 tempUrl 前缀
            string requiredPrefix = $"/Picture/ArticleImage/CoverTemp/{userId}/";
            if (!tempUrl.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("FinalizeTempCover: 临时URL前缀不匹配 {TempUrl}", tempUrl);
                return new ApiResponse(false, "临时封面路径不合法", code: ResponseCode.InvalidInput);
            }

            string wwwRoot = _environment.WebRootPath;
            string relativePath = tempUrl.TrimStart('/');
            string sourcePath = Path.Combine(wwwRoot, relativePath);
            string fullTempRoot = Path.GetFullPath(Path.Combine(wwwRoot, tempRoot));
            string fullSourcePath = Path.GetFullPath(sourcePath);

            // 防止路径穿越
            if (!fullSourcePath.StartsWith(fullTempRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("FinalizeTempCover: 源文件越权 {SourcePath}", sourcePath);
                return new ApiResponse(false, "临时封面路径不合法", code: ResponseCode.Forbidden);
            }

            if (!File.Exists(fullSourcePath))
            {
                _logger.LogWarning("FinalizeTempCover: 临时文件不存在 {fullSourcePath}", fullSourcePath);
                return new ApiResponse(false, "临时封面文件不存在", code: ResponseCode.FileMissing);
            }

            // 移动到正式目录
            string destDir = Path.Combine(wwwRoot, finalRoot);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            string fileName = Path.GetFileName(fullSourcePath);
            string destPath = Path.Combine(destDir, fileName);
            if (File.Exists(destPath))
                File.Delete(destPath);

            try
            {
                File.Move(fullSourcePath, destPath);
                _logger.LogInformation("FinalizeTempCover: 成功移动 {Source} -> {Dest}", sourcePath, destPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FinalizeTempCover: 移动失败 {Source} -> {Dest}", sourcePath, destPath);
                return new ApiResponse(false, "封面文件移动失败", code: ResponseCode.FileProcessingFailed);
            }

            // 根据 finalRoot 构造最终访问的相对 URL
            string finalUrl = $"/{finalRoot.Trim('/')}/{fileName}";

            return new ApiResponse(true, "封面转换成功", finalUrl, ResponseCode.Success);
        }

        
        // 清理临时目录中超过半小时未修改的图片
        public async Task<ApiResponse> CleanupExpiredTempCoversAsync(string tempRoot)
        {
            try
            {
                string tempDir = Path.Combine(_environment.WebRootPath, tempRoot);
                if (!Directory.Exists(tempDir))
                    return new ApiResponse(true, "临时目录不存在，无需清理", code: ResponseCode.Success);

                DateTime threshold = DateTime.Now.AddMinutes(-30);
                var filesToDelete = new List<string>();

                CollectExpiredFiles(tempDir, threshold, filesToDelete);

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogInformation("CleanupExpiredTempCovers: 删除过期临时文件 {File}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "CleanupExpiredTempCovers: 删除失败 {File}", file);
                    }
                }

                DeleteEmptyDirectories(tempDir);
                return new ApiResponse(true, "临时封面清理完成", code: ResponseCode.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CleanupExpiredTempCovers 执行出错");
                return new ApiResponse(false, "清理临时文件时发生内部错误", code: ResponseCode.InternalError);
            }
        }


        // 检查用户临时区配额：最多 20 MB，最多 30 张图片
        public Task<ApiResponse> CheckUserTempQuotaAsync(int userId, long incomingFileSize = 0)
        {
            const long maxBytes = 20 * 1024 * 1024;  // 20 MB
            const int maxCount = 30; // 30 张

            string userTempDir = Path.Combine(_environment.WebRootPath, "Picture", "ArticleImage", "CoverTemp", userId.ToString());

            try
            {
                int count = 0;
                long totalBytes = 0;

                if (Directory.Exists(userTempDir))
                {
                    foreach (var filePath in Directory.GetFiles(userTempDir))
                    {
                        string ext = Path.GetExtension(filePath).ToLowerInvariant();
                        if (Array.Exists(_allowedExtensions, e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                        {
                            count++;
                            totalBytes += new FileInfo(filePath).Length;
                        }
                    }
                }

                // 上传新文件时，预占1个文件名额和本次文件大小
                int expectedCount = incomingFileSize > 0 ? count + 1 : count;
                long expectedBytes = totalBytes + Math.Max(0, incomingFileSize);

                if (expectedCount > maxCount)
                {
                    return Task.FromResult(new ApiResponse(
                        false,
                        $"临时图片数量已达上限（最多 {maxCount} 张）",
                        code: ResponseCode.TempQuotaExceeded));
                }

                if (expectedBytes > maxBytes)
                {
                    return Task.FromResult(new ApiResponse(
                        false,
                        $"临时图片总大小超过 {maxBytes / 1024 / 1024} MB",
                        code: ResponseCode.TempQuotaExceeded));
                }

                return Task.FromResult(new ApiResponse(true, "配额正常", code: ResponseCode.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckUserTempQuota: 检查用户 {UserId} 配额失败", userId);
                return Task.FromResult(new ApiResponse(false, "配额检查失败", code: ResponseCode.InternalError));
            }
        }


        // 安全删除正式封面
        public Task TryDeleteFinalCoverAsync(string? fileUrl, string finalRoot)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileUrl))
                    return Task.CompletedTask;

                string wwwRoot = _environment.WebRootPath;
                string relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

                string fullFilePath = Path.GetFullPath(Path.Combine(wwwRoot, relativePath));
                string fullFinalRoot = Path.GetFullPath(Path.Combine(wwwRoot, finalRoot));

                // 防路径穿越：只允许删正式封面目录下文件
                if (!fullFilePath.StartsWith(fullFinalRoot, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("TryDeleteFinalCoverAsync: 非法路径，拒绝删除 {FileUrl}", fileUrl);
                    return Task.CompletedTask;
                }

                if (File.Exists(fullFilePath))
                {
                    File.Delete(fullFilePath);
                    _logger.LogInformation("TryDeleteFinalCoverAsync: 删除成功 {File}", fullFilePath);
                }
            }
            catch (Exception ex)
            {
                // 补偿删除失败只记日志，不再抛异常影响主流程
                _logger.LogWarning(ex, "TryDeleteFinalCoverAsync: 删除失败 {FileUrl}", fileUrl);
            }

            return Task.CompletedTask;
        }


        // ---------- 辅助私有方法 ----------
        private void CollectExpiredFiles(string directory, DateTime threshold, List<string> result)
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (Array.Exists(_allowedExtensions, e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    if (File.GetLastWriteTime(file) < threshold)
                        result.Add(file);
                }
            }
            foreach (var subDir in Directory.GetDirectories(directory))
                CollectExpiredFiles(subDir, threshold, result);
        }

        private void DeleteEmptyDirectories(string directory)
        {
            foreach (var subDir in Directory.GetDirectories(directory))
            {
                DeleteEmptyDirectories(subDir);
                if (!Directory.EnumerateFileSystemEntries(subDir).Any())
                {
                    try { Directory.Delete(subDir); } catch { /* 忽略删除权限问题 */ }
                }
            }
        }
    }
}