<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { getTagArticleCountApi, getTagsApi } from "../api/taxonomy";
import bannerTag from "../assets/images/banner-tag.png";
import decorationShark from "../assets/images/decoration-shark.png";
import { Tag } from "lucide-vue-next";

const loading = ref(false);
const message = ref("");
const keyword = ref("");
const tags = ref([]);
const tagCountMap = ref(new Map());
const page = ref(1);
const pageSize = 24;

const tagCards = computed(() => {
  const q = keyword.value.trim().toLowerCase();
  return (tags.value || [])
    .map((t) => ({
      id: t.id,
      name: t.name,
      count: tagCountMap.value.get(t.id) || 0
    }))
    .filter((t) => !q || t.name.toLowerCase().includes(q))
    .sort((a, b) => b.count - a.count || a.id - b.id);
});

const totalPages = computed(() => Math.max(1, Math.ceil(tagCards.value.length / pageSize)));

const pagedTags = computed(() => {
  const start = (page.value - 1) * pageSize;
  return tagCards.value.slice(start, start + pageSize);
});

const pageRangeText = computed(() => {
  if (!tagCards.value.length) return "0-0";
  const start = (page.value - 1) * pageSize + 1;
  const end = Math.min(page.value * pageSize, tagCards.value.length);
  return `${start}-${end}`;
});

watch(keyword, () => {
  page.value = 1;
});

watch(totalPages, (next) => {
  if (page.value > next) page.value = next;
});

function toPage(nextPage) {
  page.value = Math.min(totalPages.value, Math.max(1, nextPage));
}

async function loadData() {
  loading.value = true;
  message.value = "";

  const tagRes = await getTagsApi().catch((err) => {
    tags.value = [];
    message.value = err?.payload?.message || err?.message || "标签加载失败";
    return null;
  });

  if (!tagRes) {
    tagCountMap.value = new Map();
    loading.value = false;
    return;
  }

  tags.value = tagRes.data || [];

  const countResults = await Promise.allSettled(
    tags.value.map((tag) => getTagArticleCountApi(tag.id))
  );
  const map = new Map();
  countResults.forEach((result, index) => {
    const tagId = tags.value[index]?.id;
    if (!tagId) return;
    map.set(tagId, result.status === "fulfilled" ? Number(result.value.data || 0) : 0);
  });
  tagCountMap.value = map;

  loading.value = false;
}

onMounted(loadData);
</script>

<template>
  <section class="page-stack">
    <header class="sea-hero mini" :style="{ backgroundImage: `url(${bannerTag})` }">
      <div class="hero-copy">
        <h1>标签 <Tag :size="28" class="title-icon" /></h1>
        <p class="hero-sub">探索感兴趣的标签，发现更多精彩内容</p>
      </div>
      <img class="hero-avatar" :src="decorationShark" alt="tag decoration" />
    </header>

    <div class="content-grid">
      <section class="panel">
        <div class="panel-head">
          <h2>全部标签 {{ tagCards.length }} 个</h2>
          <span class="panel-count">第 {{ pageRangeText }} 个</span>
        </div>
        <div class="card-grid tags-board">
          <article v-for="tag in pagedTags" :key="tag.id" class="tag-card">
            <img :src="bannerTag" alt="tag cover" />
            <h3>{{ tag.name }}</h3>
            <p>{{ tag.count }} 篇文章</p>
          </article>
        </div>
        <p v-if="loading" class="hint">加载中...</p>
        <p v-else-if="!tagCards.length" class="hint">没有找到相关标签</p>
        <div v-if="tagCards.length > pageSize" class="pager">
          <button class="btn ghost" :disabled="page === 1" @click="toPage(1)">首页</button>
          <button class="btn ghost" :disabled="page === 1" @click="toPage(page - 1)">上一页</button>
          <span>{{ page }} / {{ totalPages }}</span>
          <button class="btn ghost" :disabled="page === totalPages" @click="toPage(page + 1)">下一页</button>
          <button class="btn ghost" :disabled="page === totalPages" @click="toPage(totalPages)">末页</button>
        </div>
      </section>

      <aside class="right-column">
        <section class="panel side-panel">
          <input v-model.trim="keyword" placeholder="搜索标签..." />
        </section>

        <section class="panel side-panel">
          <h2>热门标签 TOP 10</h2>
          <ul class="rank-list">
            <li v-for="(tag, i) in tagCards.slice(0, 10)" :key="tag.id">
              <span>{{ i + 1 }}. {{ tag.name }}</span><b>{{ tag.count }}</b>
            </li>
          </ul>
        </section>
      </aside>
    </div>
  </section>
</template>


