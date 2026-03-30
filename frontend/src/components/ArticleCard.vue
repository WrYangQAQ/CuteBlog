<script setup>
import { computed } from "vue";
import { formatDate, toAbsoluteAsset } from "../utils/asset";

const props = defineProps({
  article: {
    type: Object,
    required: true
  }
});

const cover = computed(() => toAbsoluteAsset(props.article.coverUrl));
</script>

<template>
  <article class="article-card">
    <img class="cover" :src="cover" alt="cover" />
    <div class="content">
      <h3>{{ article.title }}</h3>
      <p class="summary"><span class="summary-prefix">摘要：</span>{{ article.summary }}</p>
      <div class="meta">
        <span>{{ article.categoryName }}</span>
        <span>👀 {{ article.viewCount }}</span>
        <span>❤ {{ article.likeCount }}</span>
        <span>{{ formatDate(article.createdAt) }}</span>
      </div>
      <div class="tags">
        <span v-for="tag in article.tagNames || []" :key="tag" class="tag">#{{ tag }}</span>
      </div>
    </div>
  </article>
</template>
