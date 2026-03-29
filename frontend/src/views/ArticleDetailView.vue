<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { marked } from "marked";
import {
  getArticleByIdApi,
  readArticleApi,
  toggleArticleLikeApi
} from "../api/articles";
import { getCommentsApi, publishCommentApi } from "../api/comments";
import { formatDate } from "../utils/asset";

const route = useRoute();
const articleId = computed(() => Number(route.params.id));

const loading = ref(false);
const message = ref("");
const article = ref(null);
const comments = ref([]);
const newComment = ref("");
const enterTime = ref(Date.now());

const htmlContent = computed(() => marked.parse(article.value?.content || ""));

async function loadDetail() {
  loading.value = true;
  message.value = "";
  try {
    const [articleRes, commentsRes] = await Promise.all([
      getArticleByIdApi(articleId.value),
      getCommentsApi(articleId.value).catch(() => ({ data: [] }))
    ]);
    article.value = articleRes.data;
    comments.value = commentsRes.data || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载文章失败";
  } finally {
    loading.value = false;
  }
}

async function publishComment() {
  if (!newComment.value.trim()) return;
  try {
    await publishCommentApi(articleId.value, {
      content: newComment.value,
      parentCommentId: null
    });
    newComment.value = "";
    const res = await getCommentsApi(articleId.value);
    comments.value = res.data || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "评论失败";
  }
}

async function likeArticle() {
  try {
    await toggleArticleLikeApi(articleId.value);
    message.value = "点赞状态已更新";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "点赞失败";
  }
}

async function reportReadDuration() {
  const seconds = Math.floor((Date.now() - enterTime.value) / 1000);
  if (seconds < 60) return;
  const stay = Math.min(3600, seconds);
  try {
    await readArticleApi(articleId.value, stay);
  } catch {
    // 忽略阅读上报异常，不影响用户页面体验
  }
}

onMounted(async () => {
  enterTime.value = Date.now();
  await loadDetail();
});

onBeforeUnmount(reportReadDuration);
</script>

<template>
  <section class="panel detail-page">
    <p v-if="loading" class="hint">加载中...</p>
    <p v-if="message" class="error">{{ message }}</p>

    <template v-if="article">
      <h2>{{ article.title }}</h2>
      <div class="meta">
        <span>作者：{{ article.authorName }}</span>
        <span>分类：{{ article.categoryName }}</span>
        <span>{{ formatDate(article.createdAt) }}</span>
      </div>
      <div class="tags">
        <span v-for="tag in article.tagNames || []" :key="tag" class="tag">#{{ tag }}</span>
      </div>
      <button class="btn solid" @click="likeArticle">点赞 / 取消点赞</button>

      <article class="markdown-body" v-html="htmlContent"></article>

      <section class="comment-section">
        <h3>评论区</h3>
        <div class="comment-form">
          <textarea v-model="newComment" maxlength="500" placeholder="写点什么吧..." />
          <button class="btn solid" @click="publishComment">发表评论</button>
        </div>
        <div class="comment-list">
          <div v-for="(c, idx) in comments" :key="`${idx}-${c.createdAt}`" class="comment-item">
            <div class="comment-head">
              <strong>{{ c.userName }}</strong>
              <span>{{ formatDate(c.createdAt) }}</span>
            </div>
            <p>{{ c.content }}</p>
          </div>
        </div>
      </section>
    </template>
  </section>
</template>
