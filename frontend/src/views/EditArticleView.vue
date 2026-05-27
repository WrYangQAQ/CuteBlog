<script setup>
import { onMounted, reactive, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import DOMPurify from "dompurify";
import RichTextEditor from "../components/RichTextEditor.vue";
import { getArticleByIdApi, getArticlesApi, updateArticleApi } from "../api/articles";
import { getCategoriesApi, getTagsByCategoryApi } from "../api/taxonomy";
import { showSuccess } from "../stores/feedback";

const route = useRoute();
const router = useRouter();
const articleId = Number(route.params.id);

const categories = ref([]);
const tags = ref([]);
const message = ref("");
const loading = ref(false);
const loadingTags = ref(false);
const initialTagNames = ref([]);
const contentMode = ref("rich");

const form = reactive({
  title: "",
  summary: "",
  content: "",
  categoryId: "",
  tagIds: []
});

function looksLikeHtml(content) {
  const trimmed = (content || "").trim();
  if (!trimmed) return false;
  return /^<(p|h[1-6]|ul|ol|li|blockquote|pre|code|div|span|img|table|figure|hr|br)\b/i.test(trimmed);
}

async function loadTagsByCategory(categoryId) {
  if (!categoryId) {
    tags.value = [];
    form.tagIds = [];
    return;
  }

  loadingTags.value = true;
  try {
    const res = await getTagsByCategoryApi(Number(categoryId));
    tags.value = res.data || [];

    if (initialTagNames.value.length) {
      form.tagIds = initialTagNames.value
        .map((name) => tags.value.find((tag) => tag.name === name)?.id)
        .filter(Boolean)
        .map(Number);
      initialTagNames.value = [];
    } else {
      const validTagIds = new Set(tags.value.map((tag) => Number(tag.id)));
      form.tagIds = form.tagIds.filter((id) => validTagIds.has(Number(id)));
    }
  } catch {
    tags.value = [];
    form.tagIds = [];
  } finally {
    loadingTags.value = false;
  }
}

watch(
  () => form.categoryId,
  async (categoryId) => {
    await loadTagsByCategory(categoryId);
  }
);

async function loadAll() {
  loading.value = true;
  message.value = "";

  try {
    const [detailRes, listRes, categoryRes] = await Promise.all([
      getArticleByIdApi(articleId),
      getArticlesApi(),
      getCategoriesApi()
    ]);

    const detail = detailRes.data;
    const listItem = (listRes.data || []).find((item) => item.id === articleId);
    categories.value = categoryRes.data || [];

    form.title = detail?.title || "";
    form.content = detail?.content || "";
    contentMode.value = looksLikeHtml(form.content) ? "rich" : "markdown";
    form.summary = listItem?.summary || "";
    initialTagNames.value = detail?.tagNames || [];
    form.categoryId =
      categories.value.find((category) => category.name === detail?.categoryName)?.id?.toString() || "";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载失败";
  } finally {
    loading.value = false;
  }
}

function toggleTag(tagId) {
  const id = Number(tagId);
  if (form.tagIds.includes(id)) {
    form.tagIds = form.tagIds.filter((item) => item !== id);
  } else {
    form.tagIds = [...form.tagIds, id];
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
      content: contentMode.value === "rich" ? DOMPurify.sanitize(form.content) : form.content,
      categoryId: Number(form.categoryId),
      tagIds: form.tagIds.map(Number)
    });
    showSuccess("文章修改成功");
    setTimeout(() => router.push(`/articles/${articleId}`), 700);
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
    <p v-if="message" class="error">{{ message }}</p>

    <form class="form-grid" @submit.prevent="submit">
      <label>
        标题
        <input v-model.trim="form.title" required />
      </label>

      <label>
        摘要
        <textarea v-model="form.summary" required />
      </label>

      <div class="form-field">
        <span class="field-label">正文</span>
        <RichTextEditor v-if="contentMode === 'rich'" v-model="form.content" placeholder="修改文章正文..." />
        <textarea
          v-else
          v-model="form.content"
          class="large"
          required
          placeholder="修改 Markdown 正文..."
        />
        <p v-if="contentMode === 'markdown'" class="hint">当前是旧 Markdown 文章，保存时会保留 Markdown 格式。</p>
      </div>

      <label>
        分类
        <select v-model="form.categoryId" required>
          <option value="">请选择分类</option>
          <option v-for="category in categories" :key="category.id" :value="String(category.id)">
            {{ category.name }}
          </option>
        </select>
      </label>

      <label>
        标签
        <div v-if="!form.categoryId" class="hint">请先选择分类，再选择标签</div>
        <div v-else-if="loadingTags" class="hint">正在加载该分类下标签...</div>
        <div v-else-if="tags.length === 0" class="hint">该分类下暂无标签</div>
        <div v-else class="tags selectable">
          <button
            v-for="tag in tags"
            :key="tag.id"
            type="button"
            class="tag"
            :class="{ selected: form.tagIds.includes(Number(tag.id)) }"
            @click="toggleTag(tag.id)"
          >
            {{ tag.name }}
          </button>
        </div>
      </label>

      <div class="action-row between">
        <button type="button" class="btn ghost" @click="router.push(`/articles/${articleId}`)">取消</button>
        <button class="btn solid" :disabled="loading">{{ loading ? "保存中..." : "保存修改" }}</button>
      </div>
    </form>
  </section>
</template>
