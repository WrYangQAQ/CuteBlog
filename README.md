# CuteBlogSystem

一个前后端分离的个人博客系统，后端使用 ASP.NET Core + EF Core，前端使用 Vue 3 + Vite，实现了用户认证、文章管理、评论、分类标签、头像/封面上传、管理员统计面板等功能。

## 功能概览

- 用户注册、登录（JWT）
- 获取/修改个人信息
- 上传头像
- 文章列表、文章详情、模糊搜索
- 发布文章、编辑文章、删除文章
- 文章点赞、阅读量上报
- 评论发布、评论列表、评论删除（按后端权限）
- 分类管理、标签管理
- 管理员仪表盘统计
- Swagger 接口文档

## 技术栈

### 后端（`backend/CuteBlogSystem`）

- ASP.NET Core 9
- Entity Framework Core 9（SQL Server）
- JWT 鉴权
- Swashbuckle / Swagger
- BCrypt 密码哈希

### 前端（`frontend`）

- Vue 3
- Vue Router
- Pinia
- Axios
- Vite

## 项目结构

```text
最终项目挑战
├─ backend/
│  └─ CuteBlogSystem/         # ASP.NET Core Web API
├─ frontend/                  # Vue3 前端项目
├─ database/                  # 数据库脚本（如建表/索引/约束）
└─ docu/                      # 设计文档与 review 记录
```

## 环境要求

- .NET SDK 9.x
- Node.js 18+（推荐 20+）
- SQL Server 2019+

## 本地运行

### 1. 配置数据库与后端

1. 打开并配置后端连接字符串：  
   `backend/CuteBlogSystem/appsettings.json`
2. 配置 JWT（`Jwt:Key` 必填）：
   - 可放在 User Secrets，或
   - 配置系统环境变量，或
   - 写入本地配置文件（不建议提交到仓库）
3. 数据库迁移（在 `backend/CuteBlogSystem` 目录）：

```bash
dotnet ef database update
```

4. 启动后端：

```bash
dotnet run
```

默认 Swagger 地址（按你本机端口为准）：

- `https://localhost:7181/swagger/index.html`

### 2. 启动前端

在 `frontend` 目录执行：

```bash
npm install
npm run dev
```

默认前端地址：

- `http://localhost:5173`

前端已在 `vite.config.js` 配置代理到后端：

- `/api` -> `https://localhost:7181`
- `/Picture` -> `https://localhost:7181`

## 前后端接口说明

接口以后端控制器为准，核心路由包括：

- `api/Auth`
- `api/Articles`
- `api/Comments`
- `api/Categories`
- `api/Tags`

建议通过 Swagger 在线查看当前版本完整接口与请求参数。

## Git 提交建议

请确保忽略以下目录/文件：

- `**/bin/`
- `**/obj/`
- `frontend/node_modules/`
- `frontend/dist/`
- 本地敏感配置（如密钥、用户私有配置）

## 备注

- 本项目目前处于持续迭代中，部分接口返回结构会以最新后端代码为准。
- 若启动报 `Jwt:Key` 缺失，请先补全 JWT 配置再运行。
