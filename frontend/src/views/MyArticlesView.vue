<script setup>
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import {
  deleteArticleApi,
  getRecommendedArticlesApi,
  getToppedArticlesApi,
  toggleArticleRecommendApi,
  toggleArticleTopApi
} from "../api/articles";
import { getMyArticlesApi } from "../api/auth";
import { formatDate } from "../utils/asset";

const router = useRouter();
const authStore = useAuthStore();

const page = ref(1);
const pageSize = 10;
const totalCount = ref(0);
const rows = ref([]);
const message = ref("");

const isAdmin = computed(() => authStore.isAdmin);
const topSet = ref(new Set());
const recommendSet = ref(new Set());

async function loadFlags() {
  const [topRes, recRes] = await Promise.all([
    getToppedArticlesApi().catch(() => ({ data: [] })),
    getRecommendedArticlesApi().catch(() => ({ data: [] }))
  ]);
  topSet.value = new Set((topRes.data || []).map((a) => a.id));
  recommendSet.value = new Set((recRes.data || []).map((a) => a.id));
}

async function loadMine() {
  try {
    const res = await getMyArticlesApi(page.value, pageSize);
    rows.value = res.data?.items || [];
    totalCount.value = res.data?.totalCount || 0;
    if (isAdmin.value) await loadFlags();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载失败";
  }
}

function editArticle(id) {
  router.push(`/articles/${id}/edit`);
}

async function deleteArticle(id) {
  if (!confirm("确定删除这篇文章吗？")) return;
  try {
    await deleteArticleApi(id);
    message.value = "删除成功";
    await loadMine();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "删除失败";
  }
}

async function toggleTop(id) {
  try {
    await toggleArticleTopApi(id);
    await loadFlags();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "置顶操作失败";
  }
}

async function toggleRecommend(id) {
  try {
    await toggleArticleRecommendApi(id);
    await loadFlags();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "推荐操作失败";
  }
}

async function toPage(delta) {
  const next = page.value + delta;
  const max = Math.max(1, Math.ceil(totalCount.value / pageSize));
  if (next < 1 || next > max) return;
  page.value = next;
  await loadMine();
}

onMounted(loadMine);
</script>

<template>
  <section class="panel">
    <h2>我的文章管理</h2>
    <p v-if="message" :class="message.includes('成功') ? 'ok' : 'error'">{{ message }}</p>

    <table class="cute-table">
      <thead>
        <tr>
          <th>标题</th>
          <th>分类</th>
          <th>点赞</th>
          <th>浏览</th>
          <th>创建时间</th>
          <th>操作</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="row.id">
          <td>{{ row.title }}</td>
          <td>{{ row.categoryName }}</td>
          <td>{{ row.likeCount }}</td>
          <td>{{ row.viewCount }}</td>
          <td>{{ formatDate(row.createdAt) }}</td>
          <td class="table-actions">
            <button class="btn ghost" @click="editArticle(row.id)">编辑</button>
            <button class="btn danger" @click="deleteArticle(row.id)">删除</button>
            <button v-if="isAdmin" class="btn ghost" @click="toggleTop(row.id)">
              {{ topSet.has(row.id) ? "取消置顶" : "设为置顶" }}
            </button>
            <button v-if="isAdmin" class="btn ghost" @click="toggleRecommend(row.id)">
              {{ recommendSet.has(row.id) ? "取消推荐" : "设为推荐" }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <div class="pager">
      <button class="btn ghost" @click="toPage(-1)">上一页</button>
      <span>{{ page }} / {{ Math.max(1, Math.ceil(totalCount / pageSize)) }}</span>
      <button class="btn ghost" @click="toPage(1)">下一页</button>
    </div>
  </section>
</template>
