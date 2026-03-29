import http from "./http";

export function getCommentsApi(articleId) {
  return http.get("/api/Comments", { params: { articleId } });
}

export function publishCommentApi(articleId, payload) {
  return http.post("/api/Comments", payload, { params: { articleId } });
}

export function deleteCommentApi(commentId) {
  return http.delete(`/api/Comments/${commentId}`);
}
