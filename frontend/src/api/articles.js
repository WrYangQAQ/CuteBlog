import http from "./http";

export function getArticlesApi() {
  return http.get("/api/Articles");
}

export function searchArticlesApi(search = {}) {
  const params = new URLSearchParams();
  if (search.keyword) params.append("Keyword", search.keyword);
  if (search.category) params.append("Category", search.category);
  if (Array.isArray(search.articleTag)) {
    search.articleTag
      .filter(Boolean)
      .forEach((tag) => params.append("ArticleTag", tag));
  }
  return http.get(`/api/Articles/search?${params.toString()}`);
}

export function getArticleByIdApi(articleId) {
  return http.get(`/api/Articles/${articleId}`);
}

export function readArticleApi(articleId, stayDuration) {
  return http.post(`/api/Articles/read/${articleId}`, stayDuration, {
    headers: { "Content-Type": "application/json" }
  });
}

export function uploadArticleCoverApi(file) {
  const formData = new FormData();
  formData.append("Image", file);
  return http.post("/api/Articles/cover", formData, {
    headers: { "Content-Type": "multipart/form-data" }
  });
}

export function publishArticleApi(payload) {
  return http.post("/api/Articles/publish", payload);
}

export function toggleArticleLikeApi(articleId) {
  return http.post(`/api/Articles/${articleId}/like`);
}

export function deleteArticleApi(articleId) {
  return http.delete(`/api/Articles/${articleId}`);
}

export function updateArticleApi(articleId, payload) {
  return http.put(`/api/Articles/${articleId}`, payload);
}

export function toggleArticleTopApi(articleId) {
  return http.post(`/api/Articles/${articleId}/top`);
}

export function toggleArticleRecommendApi(articleId) {
  return http.post(`/api/Articles/${articleId}/recommend`);
}

export function getToppedArticlesApi() {
  return http.get("/api/Articles/topped");
}

export function getRecommendedArticlesApi() {
  return http.get("/api/Articles/recommended");
}
