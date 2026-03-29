<script setup>
import { onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { publishArticleApi, uploadArticleCoverApi } from "../api/articles";
import { getCategoriesApi } from "../api/taxonomy";

const router = useRouter();
const loading = ref(false);
const message = ref("");
const categories = ref([]);

const form = reactive({
  title: "",
  summary: "",
  content: "",
  categoryId: "",
  tagInput: "",
  coverUrl: ""
});

async function loadCategories() {
  try {
    const res = await getCategoriesApi();
    categories.value = res.data || [];
  } catch {
    categories.value = [];
  }
}

async function uploadCover(event) {
  const file = event.target.files?.[0];
  if (!file) return;
  try {
    const res = await uploadArticleCoverApi(file);
    form.coverUrl = res.data;
    message.value = "封面上传成功";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "封面上传失败";
  }
}

async function submit() {
  loading.value = true;
  message.value = "";
  try {
    const payload = {
      title: form.title,
      summary: form.summary,
      content: form.content,
      categoryId: Number(form.categoryId),
      tagNames: form.tagInput
        .split(",")
        .map((i) => i.trim())
        .filter(Boolean),
      coverUrl: form.coverUrl
    };
    await publishArticleApi(payload);
    message.value = "发布成功";
    setTimeout(() => router.push("/my-articles"), 700);
  } catch (err) {
    message.value = err?.payload?.message || err.message || "发布失败";
  } finally {
    loading.value = false;
  }
}

onMounted(loadCategories);
</script>

<template>
  <section class="panel">
    <h2>发布文章</h2>
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
        正文（支持 Markdown）
        <textarea v-model="form.content" class="large" required />
      </label>
      <label>
        分类
        <select v-model="form.categoryId" required>
          <option value="">请选择分类</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </label>
      <label>
        标签（英文逗号分隔）
        <input v-model="form.tagInput" placeholder="Vue, CSharp, 生活随笔" />
      </label>
      <label>
        封面上传
        <input type="file" accept="image/*" @change="uploadCover" />
      </label>
      <input v-model="form.coverUrl" placeholder="封面 URL（上传后自动填充）" required />
      <button class="btn solid" :disabled="loading">{{ loading ? "发布中..." : "发布" }}</button>
    </form>
  </section>
</template>
