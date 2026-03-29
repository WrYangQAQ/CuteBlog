# CuteBlogSystem Frontend

## 启动方式

1. 安装依赖

```bash
npm install
```

2. 启动开发服务器

```bash
npm run dev
```

默认地址为 `http://localhost:5173`，已在 `vite.config.js` 里将 `/api` 与 `/Picture` 代理到 `https://localhost:7181`。

## 环境变量（可选）

当前仓库已包含 `.env`，默认指向你当前的 Sakura 隧道地址。  
如果后续隧道地址变化，可修改 `.env`：

```env
VITE_API_BASE_URL=https://119.84.246.218:24170
VITE_API_ORIGIN=https://119.84.246.218:24170
```
