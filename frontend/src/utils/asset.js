const API_ORIGIN = import.meta.env.VITE_API_ORIGIN || "";

export function toAbsoluteAsset(url) {
  if (!url) return "";
  if (url.startsWith("http://") || url.startsWith("https://")) return url;
  return `${API_ORIGIN}${url}`;
}

const CHINA_TIME_ZONE = "Asia/Shanghai";

export function parseUtcDate(dateText) {
  if (!dateText) return null;
  if (dateText instanceof Date) {
    return Number.isNaN(dateText.getTime()) ? null : dateText;
  }

  const text = String(dateText);
  const hasZone = /Z$|[+-]\d{2}:\d{2}$/.test(text);
  const normalized = hasZone ? text : `${text}Z`;
  const date = new Date(normalized);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function getChinaDateParts(dateText) {
  const date = parseUtcDate(dateText);
  if (!date) return null;

  const parts = new Intl.DateTimeFormat("zh-CN", {
    timeZone: CHINA_TIME_ZONE,
    year: "numeric",
    month: "numeric",
    day: "numeric"
  }).formatToParts(date);

  return {
    year: Number(parts.find((part) => part.type === "year")?.value),
    month: Number(parts.find((part) => part.type === "month")?.value),
    day: Number(parts.find((part) => part.type === "day")?.value)
  };
}

export function formatDate(dateText) {
  const parts = getChinaDateParts(dateText);
  if (!parts) return "-";

  const { year, month, day } = parts;
  return `${year}年${month}月${day}日`;
}

export function formatDateTime(dateText) {
  const date = parseUtcDate(dateText);
  if (!date) return dateText ? String(dateText) : "-";

  return new Intl.DateTimeFormat("zh-CN", {
    timeZone: CHINA_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
  }).format(date);
}
