<script setup>
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { getArticlesApi } from "../api/articles";
import { getCategoriesApi } from "../api/taxonomy";
import bannerCategory from "../assets/images/banner-category.png";
import decorationShark from "../assets/images/decoration-shark.png";
import { LayoutGrid, FileText } from "lucide-vue-next";

const router = useRouter();
const loading = ref(false);
const message = ref("");
const keyword = ref("");
const categories = ref([]);
const articleCountMap = ref(new Map());

const categoryCards = computed(() => {
  const q = keyword.value.trim();
  return (categories.value || [])
    .map((c) => ({
      id: c.id,
      name: c.name,
      description: c.description || "探索该分类下的精彩文章",
      count: articleCountMap.value.get(c.name) || 0
    }))
    .filter((item) => !q || item.name.includes(q))
    .sort((a, b) => b.count - a.count || a.id - b.id);
});

async function loadData() {
  loading.value = true;
  message.value = "";

  const [categoryRes, articleRes] = await Promise.allSettled([getCategoriesApi(), getArticlesApi()]);

  if (categoryRes.status === "fulfilled") {
    categories.value = categoryRes.value.data || [];
  } else {
    categories.value = [];
    message.value = categoryRes.reason?.payload?.message || categoryRes.reason?.message || "分类加载失败";
  }

  if (articleRes.status === "fulfilled") {
    const map = new Map();
    (articleRes.value.data || []).forEach((a) => {
      const name = a.categoryName || "未分类";
      map.set(name, (map.get(name) || 0) + 1);
    });
    articleCountMap.value = map;
  } else {
    articleCountMap.value = new Map();
  }

  loading.value = false;
}

onMounted(loadData);
</script>

<template>
  <section class="page-stack">
    <header class="sea-hero mini" :style="{ backgroundImage: `url(${bannerCategory})` }">
      <div class="hero-copy">
        <h1>分类 <LayoutGrid :size="28" class="title-icon" /></h1>
        <p class="hero-sub">探索不同主题的精彩内容</p>
      </div>
      <img class="hero-avatar" :src="decorationShark" alt="category decoration" />
    </header>

    <div class="content-grid">
      <section class="panel">
        <div class="panel-head">
          <h2>全部分类 {{ categoryCards.length }} 个</h2>
        </div>

        <div class="card-grid category-grid">
          <article v-for="item in categoryCards" :key="item.id" class="category-card" @click="router.push('/articles')">
            <img :src="bannerCategory" alt="category cover" />
            <h3>{{ item.name }}</h3>
            <p>{{ item.description }}</p>
            <div class="meta"><span><FileText :size="15" class="meta-icon" /> {{ item.count }} 篇文章</span><b>→</b></div>
          </article>
        </div>

        <p v-if="loading" class="hint">加载中...</p>
      </section>

      <aside class="right-column">
        <section class="panel side-panel">
          <input v-model.trim="keyword" placeholder="搜索分类..." />
        </section>

        <section class="panel side-panel">
          <h2>热门分类</h2>
          <ul class="rank-list">
            <li v-for="item in categoryCards.slice(0, 5)" :key="item.id">
              <span>{{ item.name }}</span><b>{{ item.count }}</b>
            </li>
          </ul>
        </section>
      </aside>
    </div>
  </section>
</template>


