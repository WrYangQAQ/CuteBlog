using System.Security.Cryptography;
using System.Text;

namespace CuteBlogSystem.Helper
{
    public static class EncryptionHelper
    {
        // SHA-256 哈希算法，输入 string ，输出 string
        public static string Hash(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            using var sha256 = SHA256.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(content);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            // 将字节数组转换为小写十六进制字符串
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
