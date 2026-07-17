<script setup>
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import { getArticlesApi, getRecommendedArticlesApi, getToppedArticlesApi } from "../api/articles";
import { formatDate, parseUtcDate, toAbsoluteAsset } from "../utils/asset";
import bannerHome from "../assets/images/banner-home.png";
import heroShark from "../assets/images/hero-shark.png";
import { PartyPopper, Eye, Heart } from "lucide-vue-next";

const router = useRouter();
const authStore = useAuthStore();
const loading = ref(false);
const message = ref("");
const topped = ref([]);
const allArticles = ref([]);
const profile = computed(() => authStore.profile || {});
const profileAvatar = computed(() => toAbsoluteAsset(profile.value.avatarUrl) || heroShark);
const profileName = computed(() => profile.value.nickName || profile.value.userName || "Sharky");
const profileBio = computed(() => profile.value.bio || "前后端学习中，喜欢可爱风格与实践项目。");

const latestArticles = computed(() => {
  return [...(allArticles.value || [])]
    .sort((a, b) => (parseUtcDate(b.createdAt)?.getTime() || 0) - (parseUtcDate(a.createdAt)?.getTime() || 0))
    .slice(0, 4);
});

const categoryStats = computed(() => {
  const map = new Map();
  (allArticles.value || []).forEach((item) => {
    const key = item.categoryName || "未分类";
    map.set(key, (map.get(key) || 0) + 1);
  });
  return [...map.entries()]
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 5);
});

const hotTags = computed(() => {
  const map = new Map();
  (allArticles.value || []).forEach((item) => {
    (item.tagNames || []).forEach((tag) => {
      map.set(tag, (map.get(tag) || 0) + 1);
    });
  });
  return [...map.entries()]
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 10);
});

function goDetail(id) {
  router.push(`/articles/${id}`);
}

async function loadHomeData() {
  loading.value = true;
  message.value = "";
  try {
    const [topRes, _recRes, allRes] = await Promise.all([
      getToppedArticlesApi(),
      getRecommendedArticlesApi(),
      getArticlesApi()
    ]);
    topped.value = topRes.data || [];
    allArticles.value = allRes.data || [];
    await authStore.fetchProfile().catch(() => null);
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载失败";
  } finally {
    loading.value = false;
  }
}

onMounted(loadHomeData);
</script>

<template>
  <section class="page-stack">
    <header class="sea-hero card home-hero" :style="{ backgroundImage: `url(${bannerHome})` }">
      <div class="hero-copy">
        <h1>嗨！欢迎来到<span>Sharky Blog</span>！ <PartyPopper :size="26" class="title-icon" /></h1>
        <p class="hero-sub">做爱分享的小鲨鱼！</p>
        <div class="hero-actions">
          <button class="btn solid" @click="$router.push('/articles')">查看文章</button>
          <button class="btn ghost" @click="$router.push('/profile')">关于我</button>
        </div>
      </div>
      <img class="hero-avatar" :src="heroShark" alt="hero shark" />
    </header>

    <p v-if="loading" class="hint">加载中...</p>

    <div class="content-grid">
      <section class="panel card">
        <div class="panel-head">
          <h2>最新文章</h2>
          <a class="more-link" @click.prevent="$router.push('/articles')">查看全部 →</a>
        </div>

        <div class="article-line-list">
          <article v-for="item in latestArticles" :key="item.id" class="article-line" @click="goDetail(item.id)">
            <img :src="toAbsoluteAsset(item.coverUrl)" alt="cover" />
            <div class="line-body">
              <p v-if="topped.some((t) => t.id === item.id)" class="pin-badge">置顶</p>
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
      </section>

      <aside class="right-column">
        <section class="panel side-panel card home-about-card">
          <h2>关于我</h2>
          <div class="about-mini">
            <img :src="profileAvatar" alt="avatar" />
            <h3>{{ profileName }}</h3>
            <p>{{ profileBio }}</p>
          </div>
        </section>

        <section class="panel side-panel card">
          <div class="panel-head">
            <h2>文章分类</h2>
            <a class="more-link" @click.prevent="$router.push('/categories')">查看全部 →</a>
          </div>
          <ul class="rank-list">
            <li v-for="item in categoryStats" :key="item.name">
              <span>{{ item.name }}</span>
              <b>{{ item.count }}</b>
            </li>
          </ul>
        </section>

        <section class="panel side-panel card">
          <h2>热门标签</h2>
          <div class="tags cloud">
            <span v-for="tag in hotTags" :key="tag.name" class="tag">{{ tag.name }}</span>
          </div>
        </section>
      </aside>
    </div>
  </section>
</template>


