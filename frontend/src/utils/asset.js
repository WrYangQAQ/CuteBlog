const API_ORIGIN = import.meta.env.VITE_API_ORIGIN || "https://localhost:7181";

export function toAbsoluteAsset(url) {
  if (!url) return "";
  if (url.startsWith("http://") || url.startsWith("https://")) return url;
  return `${API_ORIGIN}${url}`;
}

export function formatDate(dateText) {
  if (!dateText) return "-";
  const date = new Date(dateText);
  return date.toLocaleString("zh-CN", { hour12: false });
}
