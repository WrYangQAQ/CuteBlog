<script setup>
import { onMounted, reactive, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getArticleByIdApi, getArticlesApi, updateArticleApi } from "../api/articles";
import { getCategoriesApi, getTagsApi } from "../api/taxonomy";

const route = useRoute();
const router = useRouter();
const articleId = Number(route.params.id);

const categories = ref([]);
const tags = ref([]);
const message = ref("");
const loading = ref(false);

const form = reactive({
  title: "",
  summary: "",
  content: "",
  categoryId: "",
  tagIds: []
});

async function loadAll() {
  loading.value = true;
  try {
    const [detailRes, listRes, categoryRes, tagRes] = await Promise.all([
      getArticleByIdApi(articleId),
      getArticlesApi(),
      getCategoriesApi(),
      getTagsApi()
    ]);

    const detail = detailRes.data;
    const listItem = (listRes.data || []).find((i) => i.id === articleId);
    categories.value = categoryRes.data || [];
    tags.value = tagRes.data || [];

    form.title = detail?.title || "";
    form.content = detail?.content || "";
    form.summary = listItem?.summary || "";
    form.categoryId =
      categories.value.find((c) => c.name === detail?.categoryName)?.id?.toString() || "";
    form.tagIds = (detail?.tagNames || [])
      .map((name) => tags.value.find((t) => t.name === name)?.id)
      .filter(Boolean);
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鍔犺浇澶辫触";
  } finally {
    loading.value = false;
  }
}

async function submit() {
  loading.value = true;
  message.value = "";
  try {
    await updateArticleApi(articleId, {
      articleId,
      title: form.title,
      summary: form.summary,
      content: form.content,
      categoryId: Number(form.categoryId),
      tagIds: form.tagIds.map(Number)
    });
    message.value = "鏇存柊鎴愬姛";
    setTimeout(() => router.push("/profile"), 700);
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鏇存柊澶辫触";
  } finally {
    loading.value = false;
  }
}

onMounted(loadAll);
</script>

<template>
  <section class="panel">
    <h2>缂栬緫鏂囩珷</h2>

    <form class="form-grid" @submit.prevent="submit">
      <label>
        鏍囬
        <input v-model.trim="form.title" required />
      </label>
      <label>
        鎽樿
        <textarea v-model="form.summary" required />
      </label>
      <label>
        姝ｆ枃
        <textarea v-model="form.content" class="large" required />
      </label>
      <label>
        鍒嗙被
        <select v-model="form.categoryId" required>
          <option value="">璇烽€夋嫨鍒嗙被</option>
          <option v-for="c in categories" :key="c.id" :value="String(c.id)">{{ c.name }}</option>
        </select>
      </label>
      <label>
        鏍囩锛堝閫夛級
        <select v-model="form.tagIds" multiple>
          <option v-for="t in tags" :key="t.id" :value="t.id">{{ t.name }}</option>
        </select>
      </label>
      <button class="btn solid" :disabled="loading">{{ loading ? "淇濆瓨涓?.." : "淇濆瓨淇敼" }}</button>
    </form>
  </section>
</template>
