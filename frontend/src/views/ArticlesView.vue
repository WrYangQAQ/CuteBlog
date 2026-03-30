<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import ArticleCard from "../components/ArticleCard.vue";
import { getArticlesApi, searchArticlesApi } from "../api/articles";
import { getCategoriesApi, getTagsApi } from "../api/taxonomy";

const router = useRouter();
const loading = ref(false);
const message = ref("");
const articles = ref([]);
const categories = ref([]);
const tags = ref([]);

const searchForm = reactive({
  keyword: "",
  category: "",
  articleTag: []
});
const showAllTags = ref(false);
const defaultVisibleCount = 8;
const tagKeyword = ref("");

const visibleTags = computed(() => {
  const keyword = tagKeyword.value.trim().toLowerCase();
  const all = (tags.value || []).filter((t) =>
    keyword ? t.name.toLowerCase().includes(keyword) : true
  );
  const selected = searchForm.articleTag || [];
  const selectedSet = new Set(selected);
  const selectedTags = all.filter((t) => selectedSet.has(t.name));
  const unselectedTags = all.filter((t) => !selectedSet.has(t.name));
  const ordered = [...selectedTags, ...unselectedTags];
  if (showAllTags.value) return ordered;
  return ordered.slice(0, defaultVisibleCount);
});

function toggleTag(name) {
  if (searchForm.articleTag.includes(name)) {
    searchForm.articleTag = searchForm.articleTag.filter((i) => i !== name);
  } else {
    searchForm.articleTag = [...searchForm.articleTag, name];
  }
}

async function loadPageData() {
  loading.value = true;
  message.value = "";
  try {
    const [allRes, cateRes, tagRes] = await Promise.all([
      getArticlesApi(),
      getCategoriesApi().catch(() => ({ data: [] })),
      getTagsApi().catch(() => ({ data: [] }))
    ]);
    articles.value = allRes.data || [];
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
  showAllTags.value = false;
  loadPageData();
}

function goDetail(id) {
  router.push(`/articles/${id}`);
}

onMounted(loadPageData);
</script>

<template>
  <section class="stack">
    <div class="panel compact-search-panel">
      <h2>全部文章与搜索</h2>
      <div class="compact-search-grid">
        <input v-model.trim="searchForm.keyword" placeholder="输入标题关键词" />
        <select v-model="searchForm.category">
          <option value="">全部分类</option>
          <option v-for="c in categories" :key="c.id" :value="c.name">{{ c.name }}</option>
        </select>
        <input v-model.trim="tagKeyword" placeholder="搜索标签..." />
        <div class="tag-chip-grid">
          <button
            v-for="tag in visibleTags"
            :key="tag.id"
            type="button"
            class="tag-chip"
            :class="{ active: searchForm.articleTag.includes(tag.name) }"
            @click="toggleTag(tag.name)"
          >
            #{{ tag.name }}
          </button>
        </div>
        <div class="tag-tools" v-if="visibleTags.length > defaultVisibleCount || tags.length > defaultVisibleCount">
          <button class="btn ghost mini" type="button" @click="showAllTags = !showAllTags">
            {{ showAllTags ? "收起标签" : "展开更多标签" }}
          </button>
        </div>
        <div class="selected-tags" v-if="searchForm.articleTag.length">
          <span class="hint">已选：</span>
          <span class="tag selected" v-for="name in searchForm.articleTag" :key="name">#{{ name }}</span>
          <button class="btn ghost mini" type="button" @click="searchForm.articleTag = []">
            清空
          </button>
        </div>
        <div class="action-row">
          <button class="btn solid" @click="search" :disabled="loading">搜索</button>
          <button class="btn ghost" @click="resetSearch" :disabled="loading">重置</button>
        </div>
      </div>
    </div>

    <div class="panel">
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
