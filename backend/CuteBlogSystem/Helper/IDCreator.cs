using Yitter.IdGenerator;

namespace CuteBlogSystem.Helper
{
    public static class IDCreator
    {
        // 生成一个新的唯一 ID，使用 Guid 来确保全局唯一性
        public static string CreateID()
        {
            return Guid.NewGuid().ToString();
        }

        // 雪花算法生成一个新的唯一 ID，适用于分布式系统，确保在高并发环境下也能生成唯一 ID
        public static long CreateSnowflakeID()
        {
            return YitIdHelper.NextId();
        }
    }
}
