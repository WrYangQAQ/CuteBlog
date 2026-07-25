using CuteBlogSystem.AI.Filters;
using CuteBlogSystem.AI.Plugins;
using CuteBlogSystem.Config;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Service;
using CuteBlogSystem.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using System;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Yitter.IdGenerator;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddOpenApi();


// 注册CORS跨域服务，接收所有HTTP请求
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "https://localhost:5173",
                "https://127.0.0.1:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5174",
                "https://localhost:5174",
                "https://127.0.0.1:5174"
            )
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 配置控制器服务
builder.Services.AddControllers();

// 配置控制器接口访问频率限制服务
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("CoverUploadPolicy", httpContext =>
    {
        // 优先按用户ID限流，未登录回退到IP
        string key = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,                 // 每窗口最多 10 次上传
                Window = TimeSpan.FromMinutes(1), // 1 分钟窗口
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0                    // 不排队，超出直接429
            });
    });
});


// 注册MyDbContext，使用SQL Server数据库连接字符串
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// 配置JWT认证
builder.Configuration.AddEnvironmentVariables();
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured. Please set Jwt:Key in user secrets or environment variables.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 注册授权服务
builder.Services.AddAuthorization();

// 注册仓储接口
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ArticleRepository>();
builder.Services.AddScoped<ArticleTagRepository>();
builder.Services.AddScoped<ArticleLikeRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<TagRepository>();
builder.Services.AddScoped<AgentWorkflowLogRepository>();
builder.Services.AddScoped<AgentConversationMemoryRepository>();
builder.Services.AddScoped<AgentConversationRepository>();
builder.Services.AddScoped<AgentMessageRepository>();
builder.Services.AddScoped<AgentPendingConfirmationRepository>();
builder.Services.AddScoped<AgentTestCaseRepository>();
builder.Services.AddScoped<AgentEvaluationResultRepository>();
builder.Services.AddScoped<AgentEvaluationRunRepository>();
builder.Services.AddScoped<AgentEvaluationReportSnapshotRepository>();
builder.Services.AddScoped<UserLongTermMemoryRepository>();

// 注册自定义服务
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ImageUploadService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<AdminStatisticsService>();
builder.Services.AddScoped<AiChatService>();
builder.Services.AddScoped<AiAgentService>();
builder.Services.AddScoped<AiPlannerService>();
builder.Services.AddScoped<AgentPlanExecutorService>();
builder.Services.AddScoped<AgentPlanValidatorService>();
builder.Services.AddScoped<AgentPlanRepairService>();
builder.Services.AddScoped<AgentExecutionFailureAnalyzerService>();
builder.Services.AddScoped<AgentReplannerService>();
builder.Services.AddScoped<AgentWorkflowService>();
builder.Services.AddScoped<AgentWorkflowLogService>();
builder.Services.AddScoped<AgentConversationMemoryService>();
builder.Services.AddScoped<AgentMessageService>();
builder.Services.AddScoped<AgentIntentRouterService>();
builder.Services.AddScoped<AgentPendingConfirmationService>();
builder.Services.AddScoped<AgentEvaluationService>();
builder.Services.AddScoped<AgentParameterPermissionService>();
builder.Services.AddScoped<AgentParameterRiskService>();
builder.Services.AddScoped<UserLongTermMemoryService>();

// 绑定并校验长期记忆生命周期任务配置
builder.Services
    .AddOptions<LongTermMemoryLifecycleJobOptions>()
    .Bind(
        builder.Configuration.GetSection(
            LongTermMemoryLifecycleJobOptions.SectionName))
    .Validate(
        options =>
            options.RunAtLocalTime >= TimeSpan.Zero &&
            options.RunAtLocalTime < TimeSpan.FromDays(1),
        "长期记忆生命周期任务执行时间必须在00:00:00到23:59:59之间")
    .Validate(
        options => options.BatchSize > 0,
        "长期记忆生命周期任务BatchSize必须大于0")
    .Validate(
        options => options.MaxBatchesPerRun > 0,
        "长期记忆生命周期任务MaxBatchesPerRun必须大于0")
    .Validate(
        options =>
            options.ArchiveConfidenceThreshold >= 0m &&
            options.ArchiveConfidenceThreshold <= 1m,
        "长期记忆归档置信度阈值必须在0到1之间")
    .Validate(
        options => options.ArchiveIdleDays > 0,
        "长期记忆归档闲置天数必须大于0")
    .Validate(
        options => options.SoftDeleteRetentionDays > 0,
        "长期记忆软删除保留天数必须大于0")
    .ValidateOnStart();

// 注册 AIShield 安全防护服务，用于 Agent 输入、输出和工具调用检测
builder.Services.AddHttpClient<AIShieldService>(client =>
{
    // 从配置读取 AIShield 后端地址，未配置时使用本地默认端口
    var baseUrl = builder.Configuration["AIShield:BaseUrl"] ?? "http://localhost:5069";

    // 设置 AIShield HttpClient 的基础地址和超时时间
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
});

// 注册JwtUtil服务
builder.Services.AddScoped<JwtHelper>();

// 注册Swagger服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册 AI Chat 服务
builder.Services.AddChatClient(_ =>
{
    var apiKey = builder.Configuration["DeepSeek:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("DeepSeek:ApiKey 未配置。");
    }

    var client = new OpenAI.Chat.ChatClient(
        model: "deepseek-chat",
        credential: new System.ClientModel.ApiKeyCredential(apiKey),
        options: new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com/v1")
        });

    return client.AsIChatClient();
});


// 注册 AI Kernel 服务
builder.Services.AddScoped<Kernel>(sp =>
{
    var apiKey = builder.Configuration["DeepSeek:ApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("DeepSeek:ApiKey 未配置。");
    }

    var kernelBuilder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "deepseek-chat",
        apiKey: apiKey,
        endpoint: new Uri("https://api.deepseek.com/v1")
    );
#pragma warning restore SKEXP0010

    // 使用 KernelPluginFactory 创建插件实例，然后添加到 Plugins 集合
    var weatherPlugin = KernelPluginFactory.CreateFromType<WeatherPlugin>(serviceProvider: sp);
    var articlePlugin = KernelPluginFactory.CreateFromType<ArticlePlugin>(serviceProvider: sp);
    var categoryPlugin = KernelPluginFactory.CreateFromType<CategoryPlugin>(serviceProvider: sp);
    var tagPlugin = KernelPluginFactory.CreateFromType<TagPlugin>(serviceProvider: sp);

    kernelBuilder.Plugins.Add(weatherPlugin);
    kernelBuilder.Plugins.Add(articlePlugin);
    kernelBuilder.Plugins.Add(categoryPlugin);
    kernelBuilder.Plugins.Add(tagPlugin);

    // 构建 Kernel
    return kernelBuilder.Build();
});

// 过滤器全局注册
builder.Services.AddScoped<IFunctionInvocationFilter, FunctionInvocationLoggingFilter>();

// 注册长期临时封面清理后台服务
builder.Services.AddHostedService<TempCoverCleanupHostedService>();

// 注册长期记忆生命周期后台服务
builder.Services.AddHostedService<LongTermMemoryLifecycleHosted>();

// 注册雪花ID生成器为单例服务
var snowflakeConfig = new IdGeneratorOptions { WorkerId = 1 };
YitIdHelper.SetIdGenerator(snowflakeConfig);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseCors("MyPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// 访问 / 时重定向到 /app/index.html
app.MapGet("/", context =>
{
    context.Response.Redirect("/app/index.html");
    return Task.CompletedTask;
});


// 对于未经处理的异常，将其重定向到 /error 端点
app.UseExceptionHandler("/error");


app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
