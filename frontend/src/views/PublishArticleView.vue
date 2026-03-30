<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { publishArticleApi, uploadArticleCoverApi } from "../api/articles";
import { getCategoriesApi, getTagsApi } from "../api/taxonomy";

const router = useRouter();
const loading = ref(false);
const uploadingCover = ref(false);
const message = ref("");
const categories = ref([]);
const tags = ref([]);
const coverFileName = ref("未选择文件");
const showAllTags = ref(false);
const defaultVisibleCount = 8;
const tagKeyword = ref("");

const form = reactive({
  title: "",
  summary: "",
  content: "",
  categoryId: "",
  selectedTagNames: [],
  coverUrl: ""
});

const isBusy = computed(() => loading.value || uploadingCover.value);
const visibleTags = computed(() => {
  const keyword = tagKeyword.value.trim().toLowerCase();
  const all = (tags.value || []).filter((t) =>
    keyword ? t.name.toLowerCase().includes(keyword) : true
  );
  const selected = form.selectedTagNames || [];
  const selectedSet = new Set(selected);
  const selectedTags = all.filter((t) => selectedSet.has(t.name));
  const unselectedTags = all.filter((t) => !selectedSet.has(t.name));
  const ordered = [...selectedTags, ...unselectedTags];
  if (showAllTags.value) return ordered;
  return ordered.slice(0, defaultVisibleCount);
});

function toggleTag(name) {
  if (form.selectedTagNames.includes(name)) {
    form.selectedTagNames = form.selectedTagNames.filter((i) => i !== name);
  } else {
    form.selectedTagNames = [...form.selectedTagNames, name];
  }
}

async function loadMeta() {
  try {
    const [cateRes, tagRes] = await Promise.all([
      getCategoriesApi(),
      getTagsApi().catch(() => ({ data: [] }))
    ]);
    categories.value = cateRes.data || [];
    tags.value = tagRes.data || [];
  } catch {
    categories.value = [];
    tags.value = [];
  }
}

async function uploadCover(event) {
  const file = event.target.files?.[0];
  if (!file) return;
  coverFileName.value = file.name;
  uploadingCover.value = true;
  message.value = "";
  try {
    const res = await uploadArticleCoverApi(file);
    form.coverUrl = res.data;
    message.value = "封面上传成功";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "封面上传失败";
  } finally {
    uploadingCover.value = false;
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
      tagNames: form.selectedTagNames,
      coverUrl: form.coverUrl
    };
    await publishArticleApi(payload);
    message.value = "发布成功";
    setTimeout(() => router.push("/profile"), 700);
  } catch (err) {
    message.value = err?.payload?.message || err.message || "发布失败";
  } finally {
    loading.value = false;
  }
}

onMounted(loadMeta);
</script>

<template>
  <section class="panel publish-page">
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
        选择标签
        <input v-model.trim="tagKeyword" placeholder="搜索标签..." />
        <div class="tag-chip-grid">
          <button
            v-for="tag in visibleTags"
            :key="tag.id"
            type="button"
            class="tag-chip"
            :class="{ active: form.selectedTagNames.includes(tag.name) }"
            @click="toggleTag(tag.name)"
          >
            #{{ tag.name }}
          </button>
        </div>
        <div class="tag-tools" v-if="visibleTags.length > defaultVisibleCount || tags.length > defaultVisibleCount">
          <button class="btn ghost mini" type="button" @click="showAllTags = !showAllTags">
            {{ showAllTags ? "收起标签" : "展开更多标签" }}
          </button>
        </div>
        <div class="selected-tags" v-if="form.selectedTagNames.length">
          <span class="hint">已选：</span>
          <span class="tag selected" v-for="name in form.selectedTagNames" :key="name">#{{ name }}</span>
          <button class="btn ghost mini" type="button" @click="form.selectedTagNames = []">
            清空
          </button>
        </div>
      </label>

      <label>
        封面上传
        <div class="file-row">
          <label class="btn ghost file-btn">
            选择图片
            <input type="file" accept="image/*" hidden @change="uploadCover" />
          </label>
          <span class="hint">{{ coverFileName }}</span>
        </div>
      </label>

      <input v-model="form.coverUrl" placeholder="封面 URL（上传后自动填充）" required />
      <button class="btn solid" :disabled="isBusy">{{ loading ? "发布中..." : "发布" }}</button>
    </form>

    <div v-if="uploadingCover" class="loading-mask">
      <div class="loader-card">
        <div class="loader"></div>
        <p>封面上传中...</p>
      </div>
    </div>
  </section>
</template>
