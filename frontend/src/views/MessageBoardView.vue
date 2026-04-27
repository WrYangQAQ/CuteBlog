<script setup>
import { computed, onMounted, ref } from "vue";
import { getMessagesApi, publishMessageApi } from "../api/messages";
import { formatDate } from "../utils/asset";
import bannerHome from "../assets/images/banner-home.png";
import decorationShark from "../assets/images/decoration-shark.png";
import { MessageCircle } from "lucide-vue-next";

const loading = ref(false);
const message = ref("");
const comments = ref([]);
const content = ref("");

const topComments = computed(() => [...comments.value].sort((a, b) => (b.likeCount || 0) - (a.likeCount || 0)));

async function loadData() {
  loading.value = true;
  message.value = "";
  try {
    const res = await getMessagesApi();
    comments.value = res.data || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载失败";
  } finally {
    loading.value = false;
  }
}

async function submit() {
  if (!content.value.trim()) return;
  try {
    await publishMessageApi({ content: content.value, parentCommentId: null });
    content.value = "";
    await loadData();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "留言失败";
  }
}

onMounted(loadData);
</script>

<template>
  <section class="page-stack">
    <header class="sea-hero mini" :style="{ backgroundImage: `url(${bannerHome})` }">
      <div class="hero-copy">
        <h1>留言板 <MessageCircle :size="28" class="title-icon" /></h1>
        <p class="hero-sub">欢迎留下你的想法，我们一起交流成长</p>
      </div>
      <img class="hero-avatar" :src="decorationShark" alt="message decoration" />
    </header>

    <div class="content-grid">
      <section class="panel">
        <div class="panel-head"><h2>留言列表</h2><span>{{ comments.length }} 条</span></div>
        <div class="message-list">
          <article v-for="(item, idx) in comments" :key="idx" class="message-item">
            <header>
              <strong>{{ item.userName || '游客' }}</strong>
              <small>{{ formatDate(item.createdAt) }}</small>
            </header>
            <p>{{ item.content }}</p>
          </article>
        </div>

        <div class="panel inner" style="margin-top: 14px">
          <h3>留下你的留言</h3>
          <textarea v-model="content" maxlength="500" placeholder="说点什么吧..." />
          <div class="action-row" style="margin-top: 8px">
            <button class="btn solid" @click="submit">发送留言</button>
          </div>
        </div>

        <p v-if="loading" class="hint">加载中...</p>
      </section>

      <aside class="right-column">
        <section class="panel side-panel">
          <h2>热门留言 TOP 5</h2>
          <ul class="rank-list">
            <li v-for="(item, i) in topComments.slice(0, 5)" :key="i">
              <span>{{ item.userName || '游客' }}</span>
              <b>{{ item.likeCount || 0 }}</b>
            </li>
          </ul>
        </section>

        <section class="panel side-panel">
          <h2>留言须知</h2>
          <ul class="tips-list">
            <li>欢迎交流，分享你的想法和建议</li>
            <li>请勿发布广告、恶意信息</li>
            <li>尊重他人，文明留言</li>
          </ul>
        </section>
      </aside>
    </div>
  </section>
</template>
