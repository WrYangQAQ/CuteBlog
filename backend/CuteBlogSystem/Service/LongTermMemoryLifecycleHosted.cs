using CuteBlogSystem.Config;
using Microsoft.Extensions.Options;

namespace CuteBlogSystem.Service
{
    // 定期执行长期记忆生命周期维护任务
    public sealed class LongTermMemoryLifecycleHosted : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LongTermMemoryLifecycleHosted> _logger;
        private readonly LongTermMemoryLifecycleJobOptions _options;

        public LongTermMemoryLifecycleHosted(
            IServiceScopeFactory scopeFactory,
            IOptions<LongTermMemoryLifecycleJobOptions> options,
            ILogger<LongTermMemoryLifecycleHosted> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation(
                    "长期记忆生命周期定时任务未启用");

                return;
            }

            _logger.LogInformation(
                "长期记忆生命周期定时任务已启动。" +
                "RunAtLocalTime={RunAtLocalTime}",
                _options.RunAtLocalTime);

            // 应用启动后立即补做一次维护
            if (_options.RunOnStartup)
            {
                await RunMaintenanceAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = CalculateDelayUntilNextRun(_options.RunAtLocalTime);

                _logger.LogInformation(
                    "下一次长期记忆生命周期维护将在 {Delay} 后执行",
                    delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await RunMaintenanceAsync(stoppingToken);
            }
        }

        // 按生命周期顺序执行所有维护操作
        private async Task RunMaintenanceAsync(CancellationToken stoppingToken)
        {
            var startedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "开始执行长期记忆生命周期维护");

            try
            {
                // 1. 先将已经到期的活跃记忆标记为Expired
                var expiredCount =
                    await ProcessInBatchesAsync(
                        "过期处理",
                        service => service.ExpireDueMemoriesAsync(_options.BatchSize),
                        stoppingToken);

                // 2. 对仍然有效的记忆执行置信度衰减
                var decayedCount =
                    await ProcessInBatchesAsync(
                        "置信度衰减",
                        service => service.DecayActiveMemoriesAsync(_options.BatchSize),
                        stoppingToken);

                // 3. 归档低置信度且长期未活动的记忆
                var archivedCount =
                    await ProcessInBatchesAsync(
                        "低置信度归档",
                        service =>
                            service.ArchiveWeakMemoriesAsync(
                                _options.ArchiveConfidenceThreshold,
                                _options.ArchiveIdleDays,
                                _options.BatchSize),
                        stoppingToken);

                // 4. 软删除超过保留期限的非活跃记忆
                var deletedCount =
                    await ProcessInBatchesAsync(
                        "保留期清理",
                        service =>
                            service.SoftDeleteRetainedMemoriesAsync(
                                _options.SoftDeleteRetentionDays,
                                _options.BatchSize),
                        stoppingToken);

                var elapsed = DateTime.UtcNow - startedAt;

                _logger.LogInformation(
                    "长期记忆生命周期维护完成。" +
                    "ExpiredCount={ExpiredCount}，" +
                    "DecayedCount={DecayedCount}，" +
                    "ArchivedCount={ArchivedCount}，" +
                    "DeletedCount={DeletedCount}，" +
                    "ElapsedMilliseconds={ElapsedMilliseconds}",
                    expiredCount,
                    decayedCount,
                    archivedCount,
                    deletedCount,
                    elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "长期记忆生命周期维护因应用停止而取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "长期记忆生命周期维护发生异常");
            }
        }

        // 分批执行某一项维护操作
        private async Task<int> ProcessInBatchesAsync(
            string operationName,
            Func<UserLongTermMemoryService, Task<int>> operation,
            CancellationToken stoppingToken)
        {
            var totalCount = 0;

            for (var batchNumber = 1;
                 batchNumber <= _options.MaxBatchesPerRun;
                 batchNumber++)
            {
                stoppingToken.ThrowIfCancellationRequested();

                // HostedService是单例，业务Service通常是Scoped，
                // 所以每一批都需要创建独立作用域
                using var scope = _scopeFactory.CreateScope();

                var memoryService = scope.ServiceProvider.GetRequiredService<UserLongTermMemoryService>();

                var currentBatchCount = await operation(memoryService);

                totalCount += currentBatchCount;

                _logger.LogDebug(
                    "长期记忆维护批次完成。" +
                    "Operation={Operation}，" +
                    "BatchNumber={BatchNumber}，" +
                    "CurrentBatchCount={CurrentBatchCount}",
                    operationName,
                    batchNumber,
                    currentBatchCount);

                // 当前批次没有取满，说明已经没有剩余候选记录
                if (currentBatchCount < _options.BatchSize)
                {
                    return totalCount;
                }
            }

            _logger.LogWarning(
                "长期记忆维护达到最大批次数。" +
                "Operation={Operation}，" +
                "MaxBatchesPerRun={MaxBatchesPerRun}，" +
                "ProcessedCount={ProcessedCount}",
                operationName,
                _options.MaxBatchesPerRun,
                totalCount);

            return totalCount;
        }

        // 计算距离下一次服务器本地执行时间的等待时长
        private static TimeSpan CalculateDelayUntilNextRun(TimeSpan runAtLocalTime)
        {
            var now = DateTime.Now;

            var nextRun =
                now.Date.Add(runAtLocalTime);

            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            return nextRun - now;
        }
    }
}
