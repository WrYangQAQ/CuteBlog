e<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { marked } from "marked";
import { getArticleByIdApi, readArticleApi, toggleArticleLikeApi } from "../api/articles";
import { getCommentsApi, publishCommentApi } from "../api/comments";
import { getProfileApi } from "../api/auth";
import { formatDate, toAbsoluteAsset } from "../utils/asset";

const route = useRoute();
const articleId = computed(() => Number(route.params.id));

const loading = ref(false);
const message = ref("");
const article = ref(null);
const comments = ref([]);
const newComment = ref("");
const enterTime = ref(Date.now());
const liked = ref(false);

const htmlContent = computed(() => marked.parse(article.value?.content || ""));

async function loadDetail() {
  loading.value = true;
  message.value = "";
  try {
    const [articleRes, commentsRes, profileRes] = await Promise.all([
      getArticleByIdApi(articleId.value),
      getCommentsApi(articleId.value).catch(() => ({ data: [] })),
      getProfileApi().catch(() => ({ data: null }))
    ]);
    article.value = articleRes.data;
    comments.value = commentsRes.data || [];
    liked.value = Boolean(
      profileRes?.data?.articlesLike?.some((item) => Number(item.id) === articleId.value)
    );
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
  const prev = liked.value;
  liked.value = !prev;
  try {
    await toggleArticleLikeApi(articleId.value);
  } catch {
    liked.value = prev;
  }
}

async function reportReadDuration() {
  const seconds = Math.floor((Date.now() - enterTime.value) / 1000);
  if (seconds < 60) return;
  const stay = Math.min(3600, seconds);
  try {
    await readArticleApi(articleId.value, stay);
  } catch {
    // ignore read report errors
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

      <button class="thumb-btn" :class="{ liked }" @click="likeArticle" aria-label="like">
        👍
      </button>

      <article class="markdown-body" v-html="htmlContent"></article>

      <section class="comment-section">
        <h3>评论区（{{ comments.length }}）</h3>
        <div class="comment-form">
          <textarea v-model="newComment" maxlength="500" placeholder="写点什么吧..." />
          <button class="btn solid" @click="publishComment">发表评论</button>
        </div>
        <div class="comment-list">
          <p v-if="!comments.length" class="hint">还没有评论，来抢沙发吧。</p>
          <div v-for="(c, idx) in comments" :key="`${idx}-${c.createdAt}`" class="comment-item">
            <div class="comment-main">
              <img class="comment-avatar" :src="toAbsoluteAsset(c.avatarUrl)" alt="avatar" />
              <div class="comment-body">
                <div class="comment-head">
                  <strong>{{ c.userName }}</strong>
                  <span>{{ formatDate(c.createdAt) }}</span>
                </div>
                <p>{{ c.content }}</p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </template>
  </section>
</template>
