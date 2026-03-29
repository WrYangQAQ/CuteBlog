<script setup>
import { onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import ArticleCard from "../components/ArticleCard.vue";
import {
  getArticlesApi,
  getRecommendedArticlesApi,
  getToppedArticlesApi,
  searchArticlesApi
} from "../api/articles";
import { getCategoriesApi, getTagsApi } from "../api/taxonomy";

const router = useRouter();
const loading = ref(false);
const message = ref("");

const articles = ref([]);
const topped = ref([]);
const recommended = ref([]);
const categories = ref([]);
const tags = ref([]);

const searchForm = reactive({
  keyword: "",
  category: "",
  articleTag: []
});

async function loadAll() {
  loading.value = true;
  message.value = "";
  try {
    const [allRes, topRes, recRes, cateRes, tagRes] = await Promise.all([
      getArticlesApi(),
      getToppedArticlesApi(),
      getRecommendedArticlesApi(),
      getCategoriesApi().catch(() => ({ data: [] })),
      getTagsApi().catch(() => ({ data: [] }))
    ]);
    articles.value = allRes.data || [];
    topped.value = topRes.data || [];
    recommended.value = recRes.data || [];
    categories.value = cateRes.data || [];
    tags.value = tagRes.data || [];
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
    const res = await searchArticlesApi(searchForm);
    articles.value = res.data || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "搜索失败";
  } finally {
    loading.value = false;
  }
}

function resetSearch() {
  searchForm.keyword = "";
  searchForm.category = "";
  searchForm.articleTag = [];
  loadAll();
}

function goDetail(id) {
  router.push(`/articles/${id}`);
}

onMounted(loadAll);
</script>

<template>
  <section class="stack">
    <div class="panel">
      <h2>文章检索</h2>
      <div class="search-grid">
        <input v-model.trim="searchForm.keyword" placeholder="关键词（标题模糊搜索）" />
        <select v-model="searchForm.category">
          <option value="">全部分类</option>
          <option v-for="c in categories" :key="c.id" :value="c.name">{{ c.name }}</option>
        </select>
        <select v-model="searchForm.articleTag" multiple>
          <option v-for="tag in tags" :key="tag.id" :value="tag.name">{{ tag.name }}</option>
        </select>
        <div class="action-row">
          <button class="btn solid" @click="search" :disabled="loading">搜索</button>
          <button class="btn ghost" @click="resetSearch" :disabled="loading">重置</button>
        </div>
      </div>
      <p class="hint">标签多选：按住 Ctrl 或 Command 进行多选。</p>
    </div>

    <div class="panel">
      <h2>置顶文章</h2>
      <div class="card-grid">
        <div v-for="a in topped" :key="`top-${a.id}`" class="clickable" @click="goDetail(a.id)">
          <ArticleCard :article="a" />
        </div>
      </div>
    </div>

    <div class="panel">
      <h2>推荐文章</h2>
      <div class="card-grid">
        <div
          v-for="a in recommended"
          :key="`recommend-${a.id}`"
          class="clickable"
          @click="goDetail(a.id)"
        >
          <ArticleCard :article="a" />
        </div>
      </div>
    </div>

    <div class="panel">
      <h2>全部文章</h2>
      <p v-if="message" class="error">{{ message }}</p>
      <p v-if="loading" class="hint">加载中...</p>
      <div class="card-grid">
        <div v-for="a in articles" :key="a.id" class="clickable" @click="goDetail(a.id)">
          <ArticleCard :article="a" />
        </div>
      </div>
    </div>
  </section>
</template>
