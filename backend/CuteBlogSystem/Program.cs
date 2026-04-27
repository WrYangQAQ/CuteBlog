using CuteBlogSystem.AI.Filters;
using CuteBlogSystem.AI.Plugins;
using CuteBlogSystem.Config;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Service;
using CuteBlogSystem.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Text;
using System.Threading.RateLimiting;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddOpenApi();


// 注册CORS跨域服务，接收所有HTTP请求
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173"
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

// 注册JwtUtil服务
builder.Services.AddScoped<JwtUtil>();

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

// 注册临时封面清理后台服务
builder.Services.AddHostedService<TempCoverCleanupHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
}

app.UseHttpsRedirection();
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
