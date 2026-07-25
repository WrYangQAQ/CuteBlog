namespace CuteBlogSystem.Config
{
    // 长期记忆生命周期定时任务配置
    public sealed class LongTermMemoryLifecycleJobOptions
    {
        public const string SectionName = "LongTermMemoryLifecycleJob";

        // 是否启用定时任务
        public bool Enabled { get; set; } = true;

        // 应用启动后是否立即执行一次
        public bool RunOnStartup { get; set; } = true;

        // 每天执行的服务器本地时间
        public TimeSpan RunAtLocalTime { get; set; } = new TimeSpan(2, 0, 0);

        // 每批处理的记忆数量
        public int BatchSize { get; set; } = 100;

        // 单次任务最多处理的批次数，防止异常情况下无限循环
        public int MaxBatchesPerRun { get; set; } = 100;

        // 自动归档的置信度阈值
        public decimal ArchiveConfidenceThreshold { get; set; } = 0.2m;

        // 自动归档前至少闲置的天数
        public int ArchiveIdleDays { get; set; } = 30;

        // 非活跃记忆进入软删除前的保留天数
        public int SoftDeleteRetentionDays { get; set; } = 90;
    }
}
