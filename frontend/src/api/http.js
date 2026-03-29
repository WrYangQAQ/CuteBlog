import axios from "axios";

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "",
  timeout: 15000
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
    const body = response.data;
    if (body && typeof body.success === "boolean") {
      if (!body.success) {
        const err = new Error(body.message || "请求失败");
        err.payload = body;
        throw err;
      }
      return body;
    }
    return body;
  },
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("token");
      localStorage.removeItem("role");
    }
    if (error.response?.data) {
      error.payload = error.response.data;
    }
    throw error;
  }
);

export default http;
