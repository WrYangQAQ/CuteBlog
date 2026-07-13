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

const trendItems = computed(() => {
  const values = stats.value?.articlesLast7Days || [];
  const max = Math.max(...values, 1);

  return values.map((count, index) => ({
    label: `D${index + 1}`,
    count,
    height: `${Math.max(12, Math.round((count / max) * 120))}px`,
  }));
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

        <div v-if="trendItems.length" class="trend-chart">
          <div v-for="item in trendItems" :key="item.label" class="trend-item">
            <span>{{ item.count }}</span>
            <i :style="{ height: item.height }"></i>
            <small>{{ item.label }}</small>
          </div>
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

.trend-chart {
  min-height: 210px;
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  align-items: end;
  gap: 14px;
  padding: 14px 4px 0;
}

.trend-item {
  display: grid;
  justify-items: center;
  gap: 8px;
}

.trend-item span {
  color: var(--ink);
  font-weight: 800;
}

.trend-item i {
  width: 100%;
  max-width: 42px;
  min-height: 12px;
  border-radius: 12px 12px 5px 5px;
  background: linear-gradient(180deg, #6ea8ff, #2f77dc);
  box-shadow: 0 8px 18px rgba(47, 119, 220, 0.22);
}

.trend-item small {
  color: var(--muted);
  font-weight: 700;
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
