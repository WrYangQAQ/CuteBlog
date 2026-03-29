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
    message.value = err?.payload?.message || err.message || "加载失败";
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
    message.value = "更新成功";
    setTimeout(() => router.push("/my-articles"), 700);
  } catch (err) {
    message.value = err?.payload?.message || err.message || "更新失败";
  } finally {
    loading.value = false;
  }
}

onMounted(loadAll);
</script>

<template>
  <section class="panel">
    <h2>编辑文章</h2>
    <p v-if="message" :class="message.includes('成功') ? 'ok' : 'error'">{{ message }}</p>

    <form class="form-grid" @submit.prevent="submit">
      <label>
        标题
        <input v-model.trim="form.title" required />
      </label>
      <label>
        摘要
        <textarea v-model="form.summary" required />
      </label>
      <label>
        正文
        <textarea v-model="form.content" class="large" required />
      </label>
      <label>
        分类
        <select v-model="form.categoryId" required>
          <option value="">请选择分类</option>
          <option v-for="c in categories" :key="c.id" :value="String(c.id)">{{ c.name }}</option>
        </select>
      </label>
      <label>
        标签（多选）
        <select v-model="form.tagIds" multiple>
          <option v-for="t in tags" :key="t.id" :value="t.id">{{ t.name }}</option>
        </select>
      </label>
      <button class="btn solid" :disabled="loading">{{ loading ? "保存中..." : "保存修改" }}</button>
    </form>
  </section>
</template>
