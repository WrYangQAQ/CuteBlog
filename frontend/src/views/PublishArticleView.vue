<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from "vue";
import { onBeforeRouteLeave, useRouter } from "vue-router";
import DOMPurify from "dompurify";
import RichTextEditor from "../components/RichTextEditor.vue";
import { publishArticleApi, uploadArticleCoverApi } from "../api/articles";
import { getCategoriesApi, getTagsByCategoryApi } from "../api/taxonomy";
import { showError, showSuccess } from "../stores/feedback";
import { toAbsoluteAsset } from "../utils/asset";
import bannerArticle from "../assets/images/banner-article.png";
import decorationShark from "../assets/images/decoration-shark.png";

const router = useRouter();
const loading = ref(false);
const uploadingCover = ref(false);
const loadingTags = ref(false);
const coverUploadSuccess = ref(false);
const allowLeave = ref(false);
const categories = ref([]);
const tags = ref([]);

const form = reactive({
  title: "",
  summary: "",
  content: "",
  categoryId: "",
  selectedTagIds: [],
  coverUrl: ""
});

function plainTextFromHtml(html) {
  const text = DOMPurify.sanitize(html || "", { ALLOWED_TAGS: [] });
  return text.replace(/\s+/g, " ").trim();
}

const stats = computed(() => ({
  words: plainTextFromHtml(form.content).length,
  minutes: Math.max(1, Math.ceil(plainTextFromHtml(form.content).length / 350)),
  paragraphs: plainTextFromHtml(form.content).split(/\s+/).filter(Boolean).length ? Math.max(1, (form.content.match(/<p|<h[1-6]|<li/g) || []).length) : 0,
  tags: form.selectedTagIds.length
}));

const isBusy = computed(() => loading.value || uploadingCover.value || loadingTags.value);
const coverPreviewUrl = computed(() => (form.coverUrl ? toAbsoluteAsset(form.coverUrl) : ""));
const hasContent = computed(() => Boolean(plainTextFromHtml(form.content)));
const hasDraftContent = computed(() => {
  return Boolean(
    form.title.trim() ||
      form.summary.trim() ||
      plainTextFromHtml(form.content) ||
      form.categoryId ||
      form.selectedTagIds.length ||
      form.coverUrl
  );
});

function confirmLeave() {
  if (allowLeave.value || !hasDraftContent.value) return true;
  return window.confirm("本网站不保存内容，确定要离开吗？");
}

function handleBeforeUnload(event) {
  if (allowLeave.value || !hasDraftContent.value) return;
  event.preventDefault();
  event.returnValue = "";
}

async function loadMeta() {
  try {
    const res = await getCategoriesApi();
    categories.value = res.data || [];
  } catch {
    categories.value = [];
  }
}

async function loadTagsByCategory(categoryId) {
  if (!categoryId) {
    tags.value = [];
    form.selectedTagIds = [];
    return;
  }

  loadingTags.value = true;
  try {
    const res = await getTagsByCategoryApi(categoryId);
    tags.value = res.data || [];

    const validTagIds = new Set(tags.value.map((t) => Number(t.id)));
    form.selectedTagIds = form.selectedTagIds.filter((id) => validTagIds.has(Number(id)));
  } catch {
    tags.value = [];
    form.selectedTagIds = [];
  } finally {
    loadingTags.value = false;
  }
}

watch(
  () => form.categoryId,
  async (newCategoryId) => {
    await loadTagsByCategory(newCategoryId ? Number(newCategoryId) : 0);
  }
);

function toggleTag(tagId) {
  if (!form.categoryId) return;
  const id = Number(tagId);

  if (form.selectedTagIds.includes(id)) {
    form.selectedTagIds = form.selectedTagIds.filter((i) => i !== id);
  } else {
    form.selectedTagIds = [...form.selectedTagIds, id];
  }
}

async function uploadCover(event) {
  const file = event.target.files?.[0];
  if (!file) return;

  uploadingCover.value = true;
  coverUploadSuccess.value = false;

  try {
    const res = await uploadArticleCoverApi(file);
    form.coverUrl = res.data;
    coverUploadSuccess.value = true;
    showSuccess("封面上传成功");
  } catch {
  } finally {
    uploadingCover.value = false;
    event.target.value = "";
  }
}

async function submit() {
  if (!hasContent.value) {
    showError("请先填写文章内容", "参数错误");
    return;
  }

  if (!form.coverUrl) {
    showError("请先上传封面图片", "参数错误");
    return;
  }

  if (!form.categoryId) {
    showError("请先选择文章分类", "参数错误");
    return;
  }

  loading.value = true;
  try {
    await publishArticleApi({
      title: form.title,
      summary: form.summary,
      content: DOMPurify.sanitize(form.content),
      categoryId: Number(form.categoryId),
      tagIds: form.selectedTagIds,
      coverUrl: form.coverUrl
    });
    allowLeave.value = true;
    showSuccess("文章发布成功");
    setTimeout(() => router.push("/articles"), 600);
  } catch {
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  loadMeta();
  window.addEventListener("beforeunload", handleBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener("beforeunload", handleBeforeUnload);
});

onBeforeRouteLeave(() => {
  return confirmLeave();
});
</script>

<template>
  <section class="page-stack publish-page">
    <header class="sea-hero mini" :style="{ backgroundImage: `url(${bannerArticle})` }">
      <div class="hero-copy">
        <h1>发布文章</h1>
        <p class="hero-sub">分享你的知识与想法，让更多人看到你的精彩内容</p>
      </div>
      <img class="hero-avatar" :src="decorationShark" alt="publish decoration" />
    </header>

    <div class="content-grid">
      <section class="panel">
        <form class="form-grid" @submit.prevent="submit">
          <label>
            文章标题 *
            <input v-model.trim="form.title" maxlength="100" required placeholder="请输入文章标题" />
          </label>

          <label>
            文章摘要
            <textarea v-model="form.summary" maxlength="200" placeholder="可选，文章摘要" />
          </label>

          <div class="form-field">
            <span class="field-label">文章内容 *</span>
            <RichTextEditor v-model="form.content" placeholder="开始编写你的文章，选中文字后也可以继续调整格式..." />
          </div>

          <label>
            文章封面
            <div class="cover-upload">
              <label class="btn ghost">
                点击上传封面
                <input type="file" hidden accept="image/*" @change="uploadCover" />
              </label>
              <span v-if="coverUploadSuccess" class="ok">上传成功</span>
              <span v-else class="hint">请选择封面图片（必传）</span>

              <div v-if="coverPreviewUrl" class="cover-preview">
                <img :src="coverPreviewUrl" alt="封面预览" />
              </div>
            </div>
          </label>

          <label>
            文章分类
            <select v-model="form.categoryId" required>
              <option value="">请选择分类</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </label>

          <label>
            文章标签
            <div v-if="!form.categoryId" class="hint">请先选择分类，再选择标签</div>
            <div v-else-if="loadingTags" class="hint">正在加载该分类下标签...</div>
            <div v-else-if="tags.length === 0" class="hint">该分类下暂无标签</div>

            <div v-else class="tags selectable">
              <button
                v-for="tag in tags"
                :key="tag.id"
                type="button"
                class="tag"
                :class="{ selected: form.selectedTagIds.includes(Number(tag.id)) }"
                @click="toggleTag(tag.id)"
              >
                {{ tag.name }}
              </button>
            </div>
          </label>

          <div class="action-row between">
            <button type="button" class="btn ghost">存为草稿</button>
            <button class="btn solid" :disabled="isBusy">{{ loading ? "发布中..." : "发布文章" }}</button>
          </div>
        </form>
      </section>

      <aside class="right-column">
        <section class="panel side-panel">
          <h2>发布助手</h2>
          <ul class="rank-list plain">
            <li><span>字数统计</span><b>{{ stats.words }} 字</b></li>
            <li><span>阅读时间</span><b>{{ stats.minutes }} 分钟</b></li>
            <li><span>段落数量</span><b>{{ stats.paragraphs }} 段</b></li>
            <li><span>标签数量</span><b>{{ stats.tags }} 个</b></li>
          </ul>
        </section>

        <section class="panel side-panel">
          <h2>写作建议</h2>
          <ul class="tips-list">
            <li>一个好的标题能够吸引更多读者</li>
            <li>使用小标题让文章结构更清晰</li>
            <li>适当插图增强表达效果</li>
            <li>检查错别字和语法</li>
          </ul>
        </section>
      </aside>
    </div>

    <div v-if="uploadingCover" class="loading-mask">
      <div class="loader-card">
        <div class="loader"></div>
        <p>封面上传中...</p>
      </div>
    </div>
  </section>
</template>
