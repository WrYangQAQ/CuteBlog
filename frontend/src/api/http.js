import axios from "axios";
import { showError } from "../stores/feedback";

function normalizeApiBody(body) {
  if (!body || typeof body !== "object") return null;

  const hasCamel = typeof body.success === "boolean";
  const hasPascal = typeof body.Success === "boolean";
  if (!hasCamel && !hasPascal) return null;

  return {
    ...body,
    success: hasCamel ? body.success : body.Success,
    message: body.message ?? body.Message ?? "",
    data: body.data ?? body.Data,
    code: body.code ?? body.Code
  };
}

function getStatusFallbackMessage(status) {
  const map = {
    400: "请求参数有误，请检查后重试。",
    401: "登录状态已失效，请重新登录。",
    403: "你暂无权限执行该操作。",
    404: "请求的资源不存在。",
    409: "数据冲突，请刷新后重试。",
    422: "提交内容校验失败，请检查输入。",
    429: "请求过于频繁，请稍后再试。",
    500: "服务器开小差了，请稍后再试。",
    502: "网关异常，请稍后再试。",
    503: "服务暂不可用，请稍后再试。",
    504: "服务响应超时，请稍后再试。"
  };
  return map[status] || "";
}

function getErrorMessage(error) {
  const status = error?.response?.status;
  const rawPayload = error?.response?.data;
  const normalized = normalizeApiBody(rawPayload);

  const backendMsg =
    (typeof rawPayload === "string" ? rawPayload : "") ||
    normalized?.message ||
    rawPayload?.message ||
    rawPayload?.Message ||
    "";

  if (typeof backendMsg === "string" && backendMsg.trim()) {
    return backendMsg.trim();
  }

  if (error?.code === "ECONNABORTED") {
    return "请求超时，请检查网络后重试。";
  }

  if (!status) {
    return "网络连接异常，请检查网络后重试。";
  }

  return getStatusFallbackMessage(status) || "请求失败，请稍后再试。";
}

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "",
  timeout: 35000
});

http.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

http.interceptors.response.use(
  (response) => {
    if (response.config?.responseType === "blob") {
      return response;
    }

    const normalized = normalizeApiBody(response.data);

    if (normalized) {
      if (!normalized.success) {
        if (response.config.allowBusinessFailure) {
          return normalized;
        }

        const msg = normalized.message || "请求失败";
        showError(msg, "请求失败");
        const err = new Error(msg);
        err.payload = normalized;
        throw err;
      }
      return normalized;
    }

    return response.data;
  },
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("token");
      localStorage.removeItem("role");
    }

    if (error.response?.data) {
      error.payload = normalizeApiBody(error.response.data) || error.response.data;
    }

    const msg = getErrorMessage(error);
    showError(msg, "请求失败");

    throw error;
  }
);

export default http;

