<script setup>
import { computed, onMounted, ref } from "vue";
import { getArticlesApi } from "../api/articles";
import { getTagsApi } from "../api/taxonomy";
import bannerTag from "../assets/images/banner-tag.png";
import decorationShark from "../assets/images/decoration-shark.png";
import { Tag } from "lucide-vue-next";

const loading = ref(false);
const message = ref("");
const keyword = ref("");
const tags = ref([]);
const tagCountMap = ref(new Map());

const tagCards = computed(() => {
  const q = keyword.value.trim().toLowerCase();
  return (tags.value || [])
    .map((t) => ({
      id: t.id,
      name: t.name,
      count: tagCountMap.value.get(t.name) || 0
    }))
    .filter((t) => !q || t.name.toLowerCase().includes(q))
    .sort((a, b) => b.count - a.count || a.id - b.id);
});

async function loadData() {
  loading.value = true;
  message.value = "";

  const [tagRes, articleRes] = await Promise.allSettled([getTagsApi(), getArticlesApi()]);

  if (tagRes.status === "fulfilled") {
    tags.value = tagRes.value.data || [];
  } else {
    tags.value = [];
    message.value = tagRes.reason?.payload?.message || tagRes.reason?.message || "标签加载失败";
  }

  if (articleRes.status === "fulfilled") {
    const map = new Map();
    (articleRes.value.data || []).forEach((a) => {
      (a.tagNames || []).forEach((name) => {
        map.set(name, (map.get(name) || 0) + 1);
      });
    });
    tagCountMap.value = map;
  } else {
    tagCountMap.value = new Map();
  }

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
        <div class="panel-head"><h2>全部标签 {{ tagCards.length }} 个</h2></div>
        <div class="card-grid tags-board">
          <article v-for="tag in tagCards" :key="tag.id" class="tag-card">
            <img :src="bannerTag" alt="tag cover" />
            <h3>{{ tag.name }}</h3>
            <p>{{ tag.count }} 篇文章</p>
          </article>
        </div>
        <p v-if="loading" class="hint">加载中...</p>
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


