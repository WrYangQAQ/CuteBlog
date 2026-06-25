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

默认地址为 `http://localhost:5174/app/`，已在 `vite.config.js` 里将 `/api` 与 `/Picture` 代理到 `http://127.0.0.1:5125`。

## 环境变量

如果需要直接请求外部后端地址，可以在 `.env` 中配置：

```env
VITE_API_BASE_URL=
VITE_API_ORIGIN=
```
