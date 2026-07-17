<script setup>
import { computed, onMounted, ref } from "vue";
import { getAdminDashboardApi } from "../api/auth";
import { formatDate } from "../utils/asset";
import { Eye, FileText, FolderOpen, MessageCircle, Tags } from "lucide-vue-next";

const stats = ref(null);
const loading = ref(false);
const message = ref("");

const summaryCards = computed(() => {
  const data = stats.value || {};

  return [
    {
      label: "文章总数",
      value: data.totalArticles ?? 0,
      icon: FileText,
    },
    {
      label: "评论总数",
      value: data.totalComments ?? 0,
      icon: MessageCircle,
    },
    {
      label: "分类总数",
      value: data.totalCategories ?? 0,
      icon: FolderOpen,
    },
    {
      label: "标签总数",
      value: data.totalTags ?? 0,
      icon: Tags,
    },
  ];
});

const trendChart = computed(() => {
  const values = stats.value?.articlesLast7Days || [];
  const max = Math.max(...values, 1);
  const width = 700;
  const height = 240;
  const padding = { top: 28, right: 28, bottom: 44, left: 42 };
  const plotWidth = width - padding.left - padding.right;
  const plotHeight = height - padding.top - padding.bottom;
  const today = new Date();

  const points = values.map((count, index) => {
    const date = new Date(today);
    date.setDate(today.getDate() - (values.length - 1 - index));

    const x = padding.left + (values.length === 1 ? plotWidth / 2 : (plotWidth / (values.length - 1)) * index);
    const y = padding.top + (1 - count / max) * plotHeight;
    const label = `${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;

    return {
      x,
      y,
      count,
      label,
    };
  });

  const linePath = points.map((point, index) => `${index === 0 ? "M" : "L"} ${point.x} ${point.y}`).join(" ");
  const areaPath = points.length
    ? `${linePath} L ${points[points.length - 1].x} ${height - padding.bottom} L ${points[0].x} ${height - padding.bottom} Z`
    : "";
  const gridLines = Array.from({ length: 5 }, (_, index) => {
    const ratio = index / 4;
    const y = padding.top + plotHeight * ratio;
    const value = Math.round(max * (1 - ratio));

    return { y, value };
  });

  return {
    width,
    height,
    points,
    linePath,
    areaPath,
    gridLines,
  };
});

async function loadStats() {
  loading.value = true;
  message.value = "";

  try {
    const res = await getAdminDashboardApi();
    stats.value = res.data;
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载统计数据失败";
  } finally {
    loading.value = false;
  }
}

onMounted(loadStats);
</script>

<template>
  <section class="admin-dashboard">
    <header class="admin-dashboard__header">
      <div>
        <p>Administrator</p>
        <h1>管理员仪表盘</h1>
      </div>
      <button class="btn ghost" type="button" :disabled="loading" @click="loadStats">
        {{ loading ? "刷新中..." : "刷新数据" }}
      </button>
    </header>

    <p v-if="message" class="error">{{ message }}</p>

    <section class="dashboard-card summary-panel">
      <article v-for="card in summaryCards" :key="card.label" class="summary-card">
        <div class="summary-card__icon">
          <component :is="card.icon" :size="22" />
        </div>
        <span>{{ card.label }}</span>
        <strong>{{ card.value }}</strong>
      </article>
    </section>

    <section class="dashboard-grid">
      <article class="dashboard-card trend-card">
        <div class="dashboard-card__head">
          <h2>近 7 天发文趋势</h2>
          <span>按天统计新增文章</span>
        </div>

        <div v-if="trendChart.points.length" class="line-chart">
          <svg :viewBox="`0 0 ${trendChart.width} ${trendChart.height}`" role="img" aria-label="近 7 天发文趋势折线图">
            <g class="line-chart__grid">
              <g v-for="line in trendChart.gridLines" :key="`grid-${line.y}`">
                <line x1="42" x2="672" :y1="line.y" :y2="line.y" />
                <text x="18" :y="line.y + 4">{{ line.value }}</text>
              </g>
            </g>

            <path class="line-chart__area" :d="trendChart.areaPath" />
            <path class="line-chart__line" :d="trendChart.linePath" />

            <g v-for="point in trendChart.points" :key="point.label" class="line-chart__point">
              <circle :cx="point.x" :cy="point.y" r="5" />
              <text class="line-chart__value" :x="point.x" :y="point.y - 12">{{ point.count }}</text>
              <text class="line-chart__date" :x="point.x" y="224">{{ point.label }}</text>
            </g>
          </svg>
        </div>
        <p v-else class="hint">暂无趋势数据</p>
      </article>

      <article class="dashboard-card top-card">
        <div class="dashboard-card__head">
          <h2>阅读量 Top 5</h2>
          <span>浏览量最高的文章</span>
        </div>

        <div v-if="stats?.top5ArticlesByViews?.length" class="top-list">
          <div v-for="(article, index) in stats.top5ArticlesByViews" :key="article.id" class="top-item">
            <b>{{ index + 1 }}</b>
            <div>
              <strong>{{ article.title }}</strong>
              <span>{{ formatDate(article.createdAt) }}</span>
            </div>
            <em>
              <Eye :size="15" />
              {{ article.viewCount }}
            </em>
          </div>
        </div>
        <p v-else class="hint">暂无阅读排行数据</p>
      </article>
    </section>
  </section>
</template>

<style scoped>
.admin-dashboard {
  display: grid;
  gap: 16px;
}

.admin-dashboard__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  padding: 20px 22px;
  border: 1px solid var(--line);
  border-radius: 18px;
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.96), rgba(232, 244, 255, 0.92));
  box-shadow: 0 10px 24px rgba(66, 120, 188, 0.1);
}

.admin-dashboard__header p {
  margin: 0 0 4px;
  color: var(--muted);
  font-weight: 700;
}

.admin-dashboard__header h1 {
  margin: 0;
  color: var(--ink);
  font-size: 2rem;
}

.dashboard-card {
  border: 1px solid var(--line);
  border-radius: 18px;
  background: var(--card);
  box-shadow: 0 10px 24px rgba(66, 120, 188, 0.1);
  padding: 18px;
}

.summary-panel {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.summary-card {
  min-height: 128px;
  display: grid;
  align-content: space-between;
  gap: 12px;
  padding: 18px;
  border: 1px solid #c8def8;
  border-radius: 14px;
  background: linear-gradient(135deg, #f9fcff, #eaf4ff);
}

.summary-card__icon {
  width: 42px;
  height: 42px;
  display: grid;
  place-items: center;
  border-radius: 12px;
  color: #2f77dc;
  background: #e2f0ff;
}

.summary-card span {
  color: var(--muted);
  font-weight: 700;
}

.summary-card strong {
  color: var(--ink);
  font-size: 2.1rem;
  line-height: 1;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.1fr) minmax(360px, 0.9fr);
  gap: 16px;
}

.dashboard-card__head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 18px;
}

.dashboard-card__head h2 {
  margin: 0;
  color: var(--ink);
}

.dashboard-card__head span {
  color: var(--muted);
  font-weight: 700;
}

.line-chart {
  min-height: 260px;
  overflow: hidden;
  border: 1px solid #d6e6fb;
  border-radius: 16px;
  background: linear-gradient(180deg, #fbfdff, #f4f9ff);
  padding: 10px 12px;
}

.line-chart svg {
  display: block;
  width: 100%;
  height: 260px;
}

.line-chart__grid line {
  stroke: #dce9fb;
  stroke-dasharray: 5 7;
}

.line-chart__grid text {
  fill: #7890b5;
  font-size: 12px;
  font-weight: 700;
  text-anchor: middle;
}

.line-chart__area {
  fill: rgba(67, 128, 213, 0.12);
}

.line-chart__line {
  fill: none;
  stroke: #2f77dc;
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-width: 4;
  filter: drop-shadow(0 8px 12px rgba(47, 119, 220, 0.2));
}

.line-chart__point circle {
  fill: #fff;
  stroke: #2f77dc;
  stroke-width: 4;
}

.line-chart__value {
  fill: var(--ink);
  font-size: 15px;
  font-weight: 800;
  text-anchor: middle;
}

.line-chart__date {
  fill: var(--muted);
  font-size: 13px;
  font-weight: 800;
  text-anchor: middle;
}

.top-list {
  display: grid;
  gap: 10px;
}

.top-item {
  display: grid;
  grid-template-columns: 34px minmax(0, 1fr) auto;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border: 1px solid #d6e6fb;
  border-radius: 14px;
  background: #f8fbff;
}

.top-item b {
  width: 30px;
  height: 30px;
  display: grid;
  place-items: center;
  border-radius: 10px;
  color: #2f77dc;
  background: #e2f0ff;
}

.top-item strong {
  display: block;
  overflow: hidden;
  color: var(--ink);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.top-item span {
  color: var(--muted);
  font-size: 0.9rem;
}

.top-item em {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  color: #41628d;
  font-style: normal;
  font-weight: 800;
}

@media (max-width: 980px) {
  .summary-panel,
  .dashboard-grid {
    grid-template-columns: 1fr;
  }

  .admin-dashboard__header {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
