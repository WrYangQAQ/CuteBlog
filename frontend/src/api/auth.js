import http from "./http";

export function registerApi(payload) {
  return http.post("/api/auth/register", payload);
}

export function loginApi(payload) {
  return http.post("/api/auth/login", payload);
}

export function getProfileApi() {
  return http.get("/api/auth/profile");
}

export function updateProfileApi(payload) {
  return http.put("/api/auth/profile", payload);
}

export function uploadAvatarApi(file) {
  const formData = new FormData();
  formData.append("Image", file);
  return http.post("/api/auth/avatar", formData, {
    headers: { "Content-Type": "multipart/form-data" }
  });
}

export function getMyArticlesApi(page = 1, pageSize = 10) {
  return http.get("/api/auth/articles", { params: { page, pageSize } });
}

export function getAdminDashboardApi() {
  return http.get("/api/auth/admin/dashboard");
}

export function uploadAdminImageApi(file) {
  const formData = new FormData();
  formData.append("Image", file);
  return http.post("/api/auth/admin/image", formData, {
    headers: { "Content-Type": "multipart/form-data" }
  });
}
