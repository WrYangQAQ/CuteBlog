const API_ORIGIN = import.meta.env.VITE_API_ORIGIN || "https://localhost:7181";

export function toAbsoluteAsset(url) {
  if (!url) return "";
  if (url.startsWith("http://") || url.startsWith("https://")) return url;
  return `${API_ORIGIN}${url}`;
}

export function formatDate(dateText) {
  if (!dateText) return "-";

  const hasZone = /Z$|[+-]\d{2}:\d{2}$/.test(dateText);
  const normalized = hasZone ? dateText : `${dateText}Z`; // 当作 UTC
  const date = new Date(normalized);
  const local = new Date(
    date.toLocaleString("en-US", {
      timeZone: "Asia/Shanghai"
    })
  );
  const year = local.getFullYear();
  const month = local.getMonth() + 1;
  const day = local.getDate();
  return `${year}年${month}月${day}日`;
}
