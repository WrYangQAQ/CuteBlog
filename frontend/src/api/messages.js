import http from "./http";

export function getMessagesApi() {
  return http.get("/api/Messages");
}

export function publishMessageApi(payload) {
  return http.post("/api/Messages", payload);
}

export function deleteMessageApi(commentId) {
  return http.delete(`/api/Messages/${commentId}`);
}
