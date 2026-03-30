<script setup>
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import ArticleCard from "../components/ArticleCard.vue";
import { getRecommendedArticlesApi, getToppedArticlesApi } from "../api/articles";
import { toAbsoluteAsset } from "../utils/asset";

const router = useRouter();
const loading = ref(false);
const message = ref("");
const topped = ref([]);
const recommended = ref([]);

const decoImages = [
  "/Picture/Avatar/DefaultAvatar/DefaultAvatar_1.png",
  "/Picture/Avatar/DefaultAvatar/DefaultAvatar_2.png"
];

function goDetail(id) {
  router.push(`/articles/${id}`);
}

async function loadHomeData() {
  loading.value = true;
  message.value = "";
  try {
    const [topRes, recRes] = await Promise.all([
      getToppedArticlesApi(),
      getRecommendedArticlesApi()
    ]);
    topped.value = topRes.data || [];
    recommended.value = recRes.data || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载失败";
  } finally {
    loading.value = false;
  }
}

onMounted(loadHomeData);
</script>

<template>
  <section class="stack">
    <div class="panel hero-panel">
      <div class="hero-left">
        <p class="kicker">WELCOME</p>
        <h2>欢迎来到 CuteBlog 的萌系空间</h2>
        <p class="hero-text">
          今天也来记录点可爱的日常吧。点击上方“全部文章”探索内容，或者直接看看本周置顶与推荐。
        </p>
        <button class="btn solid" @click="$router.push('/articles')">进入全部文章</button>
      </div>
      <div class="hero-right">
        <img v-for="(img, idx) in decoImages" :key="idx" :src="toAbsoluteAsset(img)" alt="deco" />
      </div>
    </div>

    <div class="panel">
      <h2>置顶文章</h2>
      <p v-if="loading" class="hint">加载中...</p>
      <p v-if="message" class="error">{{ message }}</p>
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
  </section>
</template>
