# CuteBlog

CuteBlog 是一个前后端分离的个人博客系统，后端使用 ASP.NET Core 9 + EF Core，前端使用 Vue 3 + Vite。项目在基础博客能力之外，新增了面向站内内容管理的 AI Agent：支持自然语言规划、工具调用、执行确认、对话记忆、工作流日志和 AIShield 安全检测。

## 功能概览

### 博客系统

- 用户注册、登录与 JWT 鉴权
- 个人信息查询与修改、头像上传
- 文章列表、文章详情、模糊搜索
- 文章发布、编辑、删除、点赞和阅读量上报
- 评论发布、评论列表和权限内删除
- 分类管理、标签管理
- 管理员统计面板
- Swagger 接口文档

### AI Agent

- 自然语言问答与任务规划
- 基于 Semantic Kernel 的文章、分类、标签和天气插件调用
- Agent 工作流执行、失败分析、计划修复和重新规划
- 高风险操作确认机制，避免 Agent 直接执行敏感动作
- 对话列表、消息历史、归档/恢复和上下文重置边界
- 对话长期记忆与站内文章上下文记忆
- 管理员可查看 Agent 工作流日志与近期执行记录
- AIShield 输入、输出和工具调用安全检测接口预留

## 技术栈

### 后端 `backend/CuteBlogSystem`

- ASP.NET Core 9
- Entity Framework Core 9 + SQL Server
- JWT Bearer Authentication
- Semantic Kernel
- Microsoft.Extensions.AI / OpenAI-compatible Chat Client
- DeepSeek Chat API
- Dapper
- BCrypt 密码哈希
- Swashbuckle / Swagger
- Yitter.IdGenerator

### 前端 `frontend`

- Vue 3
- Vue Router
- Pinia
- Axios
- Vite
- TipTap 编辑器
- Marked / Highlight.js / DOMPurify
- Lucide Vue Next

## 项目结构

```text
最终项目挑战
├─ backend/
│  └─ CuteBlogSystem/        # ASP.NET Core Web API
├─ frontend/                 # Vue 3 前端项目
├─ database/                 # 数据库脚本
├─ docu/                     # 设计文档与准备材料
└─ README.md
```

## 环境要求

- .NET SDK 9.x
- Node.js 18+，推荐 20+
- SQL Server 2019+
- 可用的 DeepSeek API Key
- 可选：AIShield 服务，默认地址 `http://localhost:5069`

## 本地配置

仓库中的 `backend/CuteBlogSystem/appsettings.json` 只保留占位配置，真实密钥不要提交到 Git。建议放在 User Secrets、环境变量或本地 `appsettings.Development.json` 中。

后端至少需要配置：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CuteBlogDatabase;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_JWT_KEY"
  },
  "DeepSeek": {
    "ApiKey": "YOUR_DEEPSEEK_API_KEY"
  },
  "AIShield": {
    "Enabled": false,
    "BaseUrl": "http://localhost:5069",
    "AgentKey": "YOUR_AISHIELD_AGENT_KEY",
    "FailOpenOnError": false
  }
}
```

## 本地运行

### 1. 启动后端

在 `backend/CuteBlogSystem` 目录执行：

```bash
dotnet restore
dotnet ef database update
dotnet run
```

Swagger 地址以本机运行端口为准，例如：

- `https://localhost:7181/swagger/index.html`
- `http://localhost:5125/swagger/index.html`

### 2. 启动前端

在 `frontend` 目录执行：

```bash
npm install
npm run dev
```

默认前端地址：

- `http://localhost:5174/app/`

前端代理配置位于 `frontend/vite.config.js`，默认将 `/api` 和 `/Picture` 转发到后端服务。

## 常用接口

接口以后端控制器和 Swagger 为准，核心路由包括：

- `api/Auth`
- `api/Articles`
- `api/Comments`
- `api/Categories`
- `api/Tags`
- `api/AiAgent`

AI Agent 相关接口包括：

- `POST api/AiAgent/planner-ask`
- `POST api/AiAgent/confirm`
- `POST api/AiAgent/cancel-confirmation`
- `GET api/AiAgent/conversations`
- `GET api/AiAgent/conversations/{sessionId}/messages`
- `GET api/AiAgent/workflow-logs/recent`

## 构建检查

后端：

```bash
dotnet build backend/CuteBlogSystem/CuteBlogSystem.sln
```

前端：

```bash
cd frontend
npm run build
```

## Git 注意事项

请不要提交以下内容：

- `**/bin/`、`**/obj/`
- `frontend/node_modules/`、`frontend/dist/`
- `.vs/`、`.vscode/`、`*.code-workspace`
- `tmp/` 和本地构建输出
- 数据库密码、JWT Key、API Key、AIShield AgentKey 等敏感配置

## 备注

项目仍在持续迭代中，接口返回结构、Agent 工具能力和数据库迁移以最新后端代码为准。
