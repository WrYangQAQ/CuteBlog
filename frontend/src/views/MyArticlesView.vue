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
    message.value = err?.payload?.message || err.message || "鍔犺浇澶辫触";
  }
}

function editArticle(id) {
  router.push(`/articles/${id}/edit`);
}

async function deleteArticle(id) {
  if (!confirm("纭畾鍒犻櫎杩欑瘒鏂囩珷鍚楋紵")) return;
  try {
    await deleteArticleApi(id);
    message.value = "鍒犻櫎鎴愬姛";
    await loadMine();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鍒犻櫎澶辫触";
  }
}

async function toggleTop(id) {
  try {
    await toggleArticleTopApi(id);
    await loadFlags();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "缃《鎿嶄綔澶辫触";
  }
}

async function toggleRecommend(id) {
  try {
    await toggleArticleRecommendApi(id);
    await loadFlags();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鎺ㄨ崘鎿嶄綔澶辫触";
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
    <h2>鎴戠殑鏂囩珷绠＄悊</h2>

    <table class="cute-table">
      <thead>
        <tr>
          <th>鏍囬</th>
          <th>鍒嗙被</th>
          <th>鐐硅禐</th>
          <th>娴忚</th>
          <th>鍒涘缓鏃堕棿</th>
          <th>鎿嶄綔</th>
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
            <button class="btn ghost" @click="editArticle(row.id)">缂栬緫</button>
            <button class="btn danger" @click="deleteArticle(row.id)">鍒犻櫎</button>
            <button v-if="isAdmin" class="btn ghost" @click="toggleTop(row.id)">
              {{ topSet.has(row.id) ? "鍙栨秷缃《" : "璁句负缃《" }}
            </button>
            <button v-if="isAdmin" class="btn ghost" @click="toggleRecommend(row.id)">
              {{ recommendSet.has(row.id) ? "鍙栨秷鎺ㄨ崘" : "璁句负鎺ㄨ崘" }}
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
