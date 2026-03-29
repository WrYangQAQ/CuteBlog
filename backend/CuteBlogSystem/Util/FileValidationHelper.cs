namespace CuteBlogSystem.Util
{
    public static class FileValidationHelper
    {
        
        public static async Task<bool> IsValidImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // 定义常见图片格式的魔数
            var magicNumbers = new Dictionary<string, byte[][]>()
            {
                ["jpeg"] = new byte[][] { new byte[] { 0xFF, 0xD8, 0xFF } },               // 简化：检查前3字节为FF D8 FF
                ["png"] = new byte[][] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
                ["gif"] = new byte[][] { new byte[] { 0x47, 0x49, 0x46, 0x38 } },         // GIF8
                ["bmp"] = new byte[][] { new byte[] { 0x42, 0x4D } },
                ["webp"] = new byte[][] {
                    new byte[] { 0x52, 0x49, 0x46, 0x46 }, // RIFF
                    new byte[] { 0x57, 0x45, 0x42, 0x50 }  // WEBP，但中间有4字节长度，所以需要跳过
                }
            };

            // 读取前 12 个字节足够覆盖所有图片格式的魔数
            byte[] header = new byte[12];
            using (var stream = file.OpenReadStream())
            {
                await stream.ReadAsync(header, 0, header.Length);
            }

            // 检查 JPEG
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // 检查 PNG
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                return true;

            // 检查 GIF
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                return true;

            // 检查 BMP
            if (header[0] == 0x42 && header[1] == 0x4D)
                return true;

            // 检查 WebP
            if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && // "RIFF"
                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)   // "WEBP"
                return true;

            return false;
        }
    }
}
