<script setup>
import { computed, onMounted, reactive, ref, watch } from "vue";
import { marked } from "marked";
import { useRouter } from "vue-router";
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
const categories = ref([]);
const tags = ref([]);

const form = reactive({
  title: "",
  summary: "",
  content: "",
  categoryId: "",
  selectedTagNames: [],
  coverUrl: ""
});

const stats = computed(() => ({
  words: (form.content || "").trim().length,
  minutes: Math.max(1, Math.ceil((form.content || "").trim().length / 350)),
  paragraphs: (form.content || "").split(/\n+/).filter(Boolean).length,
  tags: form.selectedTagNames.length
}));

const markdownPreviewHtml = computed(() => {
  if (!form.content?.trim()) {
    return '<p class="hint">在左侧输入 Markdown 或纯文本，这里会实时预览效果。</p>';
  }
  return marked.parse(form.content, {
    gfm: true,
    breaks: true
  });
});

const isBusy = computed(() => loading.value || uploadingCover.value || loadingTags.value);
const coverPreviewUrl = computed(() => (form.coverUrl ? toAbsoluteAsset(form.coverUrl) : ""));

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
    form.selectedTagNames = [];
    return;
  }

  loadingTags.value = true;
  try {
    const res = await getTagsByCategoryApi(categoryId);
    tags.value = res.data || [];

    const validTagNames = new Set(tags.value.map((t) => t.name));
    form.selectedTagNames = form.selectedTagNames.filter((name) => validTagNames.has(name));
  } catch {
    tags.value = [];
    form.selectedTagNames = [];
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

function toggleTag(name) {
  if (!form.categoryId) return;

  if (form.selectedTagNames.includes(name)) {
    form.selectedTagNames = form.selectedTagNames.filter((i) => i !== name);
  } else {
    form.selectedTagNames = [...form.selectedTagNames, name];
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
      content: form.content,
      categoryId: Number(form.categoryId),
      tagNames: form.selectedTagNames,
      coverUrl: form.coverUrl
    });
    showSuccess("文章发布成功");
    setTimeout(() => router.push("/articles"), 600);
  } catch {
  } finally {
    loading.value = false;
  }
}

onMounted(loadMeta);
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

          <label>
            文章内容 *
            <div class="md-editor-grid">
              <textarea
                v-model="form.content"
                class="large md-input"
                required
                placeholder="开始编写你的文章... 支持 Markdown 语法"
              />
              <div class="md-live-preview markdown-body" v-html="markdownPreviewHtml"></div>
            </div>
          </label>

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
                :class="{ selected: form.selectedTagNames.includes(tag.name) }"
                @click="toggleTag(tag.name)"
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
