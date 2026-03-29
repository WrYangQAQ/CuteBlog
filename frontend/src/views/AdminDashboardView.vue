<script setup>
import { onMounted, ref } from "vue";
import { getAdminDashboardApi } from "../api/auth";
import { formatDate } from "../utils/asset";

const stats = ref(null);
const message = ref("");

async function loadStats() {
  try {
    const res = await getAdminDashboardApi();
    stats.value = res.data;
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载统计数据失败";
  }
}

onMounted(loadStats);
</script>

<template>
  <section class="stack">
    <div class="panel">
      <h2>管理员仪表盘</h2>
      <p v-if="message" class="error">{{ message }}</p>

      <div v-if="stats" class="stats-grid">
        <div class="stat-box">
          <h3>文章总数</h3>
          <p>{{ stats.totalArticles }}</p>
        </div>
        <div class="stat-box">
          <h3>评论总数</h3>
          <p>{{ stats.totalComments }}</p>
        </div>
        <div class="stat-box">
          <h3>分类总数</h3>
          <p>{{ stats.totalCategories }}</p>
        </div>
        <div class="stat-box">
          <h3>标签总数</h3>
          <p>{{ stats.totalTags }}</p>
        </div>
      </div>
    </div>

    <div v-if="stats" class="panel">
      <h2>近7天发文趋势</h2>
      <div class="bar-wrap">
        <div
          v-for="(count, idx) in stats.articlesLast7Days"
          :key="`day-${idx}`"
          class="bar-item"
          :style="{ '--height': `${20 + count * 18}px` }"
        >
          <span>{{ count }}</span>
          <i></i>
          <small>D{{ idx + 1 }}</small>
        </div>
      </div>
    </div>

    <div v-if="stats" class="panel">
      <h2>阅读量 Top5</h2>
      <div class="list-grid">
        <div v-for="a in stats.top5ArticlesByViews" :key="`top5-${a.id}`" class="line-card">
          <strong>{{ a.title }}</strong>
          <span>👀 {{ a.viewCount }}</span>
          <span>{{ formatDate(a.createdAt) }}</span>
        </div>
      </div>
    </div>
  </section>
</template>
