<script setup>
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { getArticlesApi } from "../api/articles";
import { formatDate } from "../utils/asset";
import bannerCategory from "../assets/images/banner-category.png";
import decorationShark from "../assets/images/decoration-shark.png";
import { Archive } from "lucide-vue-next";

const router = useRouter();
const loading = ref(false);
const message = ref("");
const articles = ref([]);

const grouped = computed(() => {
  const map = new Map();
  (articles.value || []).forEach((a) => {
    const date = new Date(a.createdAt);
    const year = date.getFullYear();
    const month = date.getMonth() + 1;
    if (!map.has(year)) map.set(year, new Map());
    const m = map.get(year);
    if (!m.has(month)) m.set(month, []);
    m.get(month).push(a);
  });

  return [...map.entries()]
    .sort((a, b) => b[0] - a[0])
    .map(([year, months]) => ({
      year,
      count: [...months.values()].reduce((acc, list) => acc + list.length, 0),
      months: [...months.entries()]
        .sort((a, b) => b[0] - a[0])
        .map(([month, list]) => ({ month, list }))
    }));
});

async function loadData() {
  loading.value = true;
  message.value = "";
  try {
    const res = await getArticlesApi();
    articles.value = res.data || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载失败";
  } finally {
    loading.value = false;
  }
}

function openArticle(id) {
  router.push(`/articles/${id}`);
}

onMounted(loadData);
</script>

<template>
  <section class="page-stack">
    <header class="sea-hero mini" :style="{ backgroundImage: `url(${bannerCategory})` }">
      <div class="hero-copy">
        <h1>归档 <Archive :size="28" class="title-icon" /></h1>
        <p class="hero-sub">回顾过去的点滴，记录成长足迹</p>
      </div>
      <img class="hero-avatar" :src="decorationShark" alt="archive decoration" />
    </header>

    <div class="content-grid">
      <section class="panel">
        <h2>全部文章 {{ articles.length }} 篇</h2>
        <div class="archive-list">
          <details v-for="year in grouped" :key="year.year" open>
            <summary>{{ year.year }} 年 <span>{{ year.count }} 篇文章</span></summary>
            <div class="archive-month" v-for="month in year.months" :key="month.month">
              <h4>{{ month.month }} 月（{{ month.list.length }}）</h4>
              <ul>
                <li v-for="item in month.list" :key="item.id" @click="openArticle(item.id)">
                  <span>{{ item.title }}</span>
                  <small>{{ formatDate(item.createdAt) }}</small>
                </li>
              </ul>
            </div>
          </details>
        </div>
        <p v-if="loading" class="hint">加载中...</p>
      </section>

      <aside class="right-column">
        <section class="panel side-panel">
          <h2>归档统计</h2>
          <ul class="rank-list plain">
            <li><span>全部文章</span><b>{{ articles.length }}</b></li>
            <li><span>最新更新</span><b>{{ articles[0] ? formatDate(articles[0].createdAt) : '-' }}</b></li>
            <li><span>归档年份</span><b>{{ grouped.length }}</b></li>
          </ul>
        </section>
      </aside>
    </div>
  </section>
</template>




