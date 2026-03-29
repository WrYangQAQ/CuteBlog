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

如果你不想使用代理，可创建 `.env`：

```env
VITE_API_BASE_URL=https://localhost:7181
VITE_API_ORIGIN=https://localhost:7181
```
