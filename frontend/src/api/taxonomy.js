import http from "./http";

export function getCategoriesApi() {
  return http.get("/api/Categories");
}

export function createCategoryApi(payload) {
  return http.post("/api/Categories", payload);
}

export function updateCategoryApi(categoryId, payload) {
  return http.put(`/api/Categories/${categoryId}`, payload);
}

export function deleteCategoryApi(categoryId) {
  return http.delete(`/api/Categories/${categoryId}`);
}

export function getTagsApi() {
  return http.get("/api/Tags");
}

export function createTagApi(payload) {
  return http.post("/api/Tags", payload);
}

export function updateTagApi(tagId, payload) {
  return http.put(`/api/Tags/${tagId}`, payload);
}

export function deleteTagApi(tagId) {
  return http.delete(`/api/Tags/${tagId}`);
}
