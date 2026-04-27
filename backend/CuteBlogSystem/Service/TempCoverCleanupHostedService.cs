namespace CuteBlogSystem.Service
{
    public class TempCoverCleanupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TempCoverCleanupHostedService> _logger;

        public TempCoverCleanupHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<TempCoverCleanupHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 每30分钟清理一次
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));

            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var imageService = scope.ServiceProvider.GetRequiredService<ImageUploadService>();

                    var result = await imageService.CleanupExpiredTempCoversAsync("Picture/ArticleImage/CoverTemp");
                    if (!result.Success)
                    {
                        _logger.LogWarning("临时封面定时清理失败：{Message}", result.Message);
                    }
                    else
                    {
                        _logger.LogInformation("临时封面定时清理完成：{Message}", result.Message);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "临时封面定时清理任务异常");
                }
            }
        }
    }
}
