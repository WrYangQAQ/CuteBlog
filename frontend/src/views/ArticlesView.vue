<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { getArticlesApi, getArticlesByCategoryApi, searchArticlesApi } from "../api/articles";
import { getCategoriesApi } from "../api/taxonomy";
import { formatDate, toAbsoluteAsset } from "../utils/asset";
import bannerArticle from "../assets/images/banner-article.png";
import decorationShark from "../assets/images/decoration-shark.png";
import { BookOpenText, Eye, Heart } from "lucide-vue-next";

const router = useRouter();
const loading = ref(false);
const message = ref("");
const articles = ref([]);
const allArticles = ref([]);
const categories = ref([]);
const selectedCategoryId = ref(0);
const searchForm = reactive({ keyword: "", articleTag: [] });

const categoryTabs = computed(() => {
  const map = new Map();
  (allArticles.value || []).forEach((a) => {
    const key = a.categoryName || "未分类";
    map.set(key, (map.get(key) || 0) + 1);
  });
  const categoryItems = (categories.value || []).map((item) => ({
    id: item.id,
    name: item.name,
    count: map.get(item.name) || 0
  }));
  return [
    { id: 0, name: "全部文章", count: allArticles.value.length },
    ...categoryItems
  ];
});

const selectedCategoryName = computed(() => {
  return categoryTabs.value.find((tab) => tab.id === selectedCategoryId.value)?.name || "";
});

const hotArticles = computed(() => {
  return [...(articles.value || [])]
    .sort((a, b) => (b.viewCount || 0) - (a.viewCount || 0))
    .slice(0, 5);
});

const hotTags = computed(() => {
  const map = new Map();
  (articles.value || []).forEach((a) => {
    (a.tagNames || []).forEach((t) => map.set(t, (map.get(t) || 0) + 1));
  });
  return [...map.entries()]
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 12);
});

async function loadData() {
  loading.value = true;
  message.value = "";
  try {
    const [articleRes, categoryRes] = await Promise.all([getArticlesApi(), getCategoriesApi()]);
    allArticles.value = articleRes.data || [];
    articles.value = allArticles.value;
    categories.value = categoryRes.data || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载失败";
  } finally {
    loading.value = false;
  }
}

async function search() {
  loading.value = true;
  message.value = "";
  try {
    const res = await searchArticlesApi({
      ...searchForm,
      category: selectedCategoryId.value ? selectedCategoryName.value : ""
    });
    articles.value = res.data || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "搜索失败";
  } finally {
    loading.value = false;
  }
}

async function selectCategory(tab) {
  selectedCategoryId.value = tab.id;
  message.value = "";

  if (!tab.id) {
    articles.value = allArticles.value;
    return;
  }

  loading.value = true;
  try {
    const res = await getArticlesByCategoryApi(tab.id);
    articles.value = res.data || [];
  } catch (err) {
    articles.value = [];
    message.value = err?.payload?.message || err.message || "分类文章加载失败";
  } finally {
    loading.value = false;
  }
}

function goDetail(id) {
  router.push(`/articles/${id}`);
}

onMounted(loadData);
</script>

<template>
  <section class="page-stack">
    <header class="sea-hero mini" :style="{ backgroundImage: `url(${bannerArticle})` }">
      <div class="hero-copy">
        <h1>文章 <BookOpenText :size="28" class="title-icon" /></h1>
        <p class="hero-sub">记录编程学习、技术探索与生活思考</p>
      </div>
      <img class="hero-avatar" :src="decorationShark" alt="article decoration" />
    </header>

    <div class="content-grid">
      <section class="panel">
        <div class="tabs-row">
          <button
            v-for="tab in categoryTabs"
            :key="tab.id"
            class="tab-pill"
            :class="{ active: selectedCategoryId === tab.id }"
            @click="selectCategory(tab)"
          >
            {{ tab.name }} <small>{{ tab.count }}</small>
          </button>
        </div>

        <div class="article-line-list compact">
          <article v-for="item in articles" :key="item.id" class="article-line" @click="goDetail(item.id)">
            <img :src="toAbsoluteAsset(item.coverUrl)" alt="cover" />
            <div class="line-body">
              <h3>{{ item.title }}</h3>
              <p>{{ item.summary || "暂无摘要" }}</p>
              <div class="tags">
                <span v-for="tag in (item.tagNames || []).slice(0, 4)" :key="tag" class="tag">{{ tag }}</span>
              </div>
              <div class="meta">
                <span>{{ formatDate(item.createdAt) }}</span>
                <span><Eye :size="15" class="meta-icon" /> {{ item.viewCount }}</span>
                <span><Heart :size="15" class="meta-icon" /> {{ item.likeCount }}</span>
              </div>
            </div>
          </article>
        </div>
        <p v-if="loading" class="hint">加载中...</p>
        <p v-else-if="!articles.length" class="hint">暂无文章</p>
        <p v-if="message" class="error">{{ message }}</p>
      </section>

      <aside class="right-column">
        <section class="panel side-panel">
          <div class="search-row">
            <input v-model.trim="searchForm.keyword" placeholder="搜索文章..." @keyup.enter="search" />
            <button class="btn solid" @click="search">搜索</button>
          </div>
        </section>

        <section class="panel side-panel">
          <h2>热门文章</h2>
          <ul class="rank-list">
            <li v-for="item in hotArticles" :key="item.id" @click="goDetail(item.id)">
              <span>{{ item.title }}</span>
              <b>{{ item.viewCount }}</b>
            </li>
          </ul>
        </section>

        <section class="panel side-panel">
          <h2>热门标签</h2>
          <div class="tags cloud">
            <span v-for="tag in hotTags" :key="tag.name" class="tag">{{ tag.name }}</span>
          </div>
        </section>
      </aside>
    </div>
  </section>
</template>





