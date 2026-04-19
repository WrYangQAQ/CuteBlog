using CuteBlogSystem.Config;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using CuteBlogSystem.Util;

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

// 注册自定义服务和仓储
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ArticleRepository>();
builder.Services.AddScoped<ArticleTagRepository>();
builder.Services.AddScoped<ArticleLikeRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<TagRepository>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ImageUploadService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<AdminStatisticsService>();

// 注册JwtUtil服务
builder.Services.AddScoped<JwtUtil>();

// 注册Swagger服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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
