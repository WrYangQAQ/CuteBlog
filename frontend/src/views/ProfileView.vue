<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import { getMyArticlesApi, updateProfileApi, uploadAvatarApi } from "../api/auth";
import { getArticlesApi } from "../api/articles";
import ArticleCard from "../components/ArticleCard.vue";
import { formatDate, toAbsoluteAsset } from "../utils/asset";

const router = useRouter();
const authStore = useAuthStore();
const loading = ref(false);
const message = ref("");
const myPage = ref(1);
const myPageSize = 6;
const myArticles = ref([]);
const totalCount = ref(0);
const allArticles = ref([]);
const isEditingProfile = ref(false);

const profileForm = reactive({
  nickName: "",
  bio: ""
});

const likedArticlesRich = computed(() => {
  const liked = authStore.profile?.articlesLike || [];
  const map = new Map((allArticles.value || []).map((i) => [Number(i.id), i]));
  return liked.map((item) => map.get(Number(item.id)) || item);
});

function fillFormFromProfile() {
  profileForm.nickName = authStore.profile?.nickName || "";
  profileForm.bio = authStore.profile?.bio || "";
}

async function loadProfile() {
  loading.value = true;
  message.value = "";
  try {
    await authStore.fetchProfile();
    fillFormFromProfile();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "获取资料失败";
  } finally {
    loading.value = false;
  }
}

async function loadAllArticles() {
  try {
    const res = await getArticlesApi();
    allArticles.value = res.data || [];
  } catch {
    allArticles.value = [];
  }
}

async function loadMyArticles() {
  try {
    const res = await getMyArticlesApi(myPage.value, myPageSize);
    myArticles.value = res.data?.items || [];
    totalCount.value = res.data?.totalCount || 0;
  } catch {
    myArticles.value = [];
    totalCount.value = 0;
  }
}

function startEditProfile() {
  fillFormFromProfile();
  isEditingProfile.value = true;
}

function cancelEditProfile() {
  fillFormFromProfile();
  isEditingProfile.value = false;
}

async function saveProfile() {
  try {
    await updateProfileApi(profileForm);
    message.value = "资料更新成功";
    isEditingProfile.value = false;
    await loadProfile();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "更新失败";
  }
}

async function uploadAvatar(event) {
  const file = event.target.files?.[0];
  if (!file) return;
  try {
    await uploadAvatarApi(file);
    message.value = "头像上传成功";
    await loadProfile();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "头像上传失败";
  }
}

async function toPage(offset) {
  const next = myPage.value + offset;
  const max = Math.max(1, Math.ceil(totalCount.value / myPageSize));
  if (next < 1 || next > max) return;
  myPage.value = next;
  await loadMyArticles();
}

function goDetail(id) {
  router.push(`/articles/${id}`);
}

onMounted(async () => {
  await Promise.all([loadProfile(), loadMyArticles(), loadAllArticles()]);
});
</script>

<template>
  <section class="stack">
    <div class="panel panel-soft">
      <h2>个人资料</h2>
      <p v-if="loading" class="hint">加载中...</p>
      <p v-if="message" :class="message.includes('成功') ? 'ok' : 'error'">{{ message }}</p>

      <div v-if="authStore.profile" class="profile-top">
        <img class="avatar" :src="toAbsoluteAsset(authStore.profile.avatarUrl)" alt="avatar" />
        <label class="btn ghost upload">
          上传头像
          <input type="file" accept="image/*" @change="uploadAvatar" hidden />
        </label>
      </div>

      <div v-if="!isEditingProfile" class="profile-display">
        <p><strong>昵称：</strong>{{ authStore.profile?.nickName || "未设置" }}</p>
        <p><strong>简介：</strong>{{ authStore.profile?.bio || "这个人很神秘，什么都没写。" }}</p>
        <button class="btn solid" @click="startEditProfile">修改个人资料</button>
      </div>

      <form v-else class="form-grid" @submit.prevent="saveProfile">
        <label>
          昵称
          <input v-model.trim="profileForm.nickName" maxlength="20" />
        </label>
        <label>
          简介
          <textarea v-model="profileForm.bio" maxlength="200" />
        </label>
        <div class="action-row">
          <button class="btn solid">保存资料</button>
          <button class="btn ghost" type="button" @click="cancelEditProfile">取消</button>
        </div>
      </form>
    </div>

    <div class="panel panel-dashed">
      <h2>我点赞的文章</h2>
      <div v-if="likedArticlesRich.length" class="card-grid">
        <div
          v-for="a in likedArticlesRich"
          :key="`like-${a.id}`"
          class="clickable"
          @click="goDetail(a.id)"
        >
          <ArticleCard :article="a" />
        </div>
      </div>
      <p v-else class="hint">还没有点赞过文章</p>
    </div>

    <div class="panel panel-cut">
      <h2>我发布的文章</h2>
      <div v-if="myArticles.length" class="card-grid">
        <div
          v-for="a in myArticles"
          :key="`mine-${a.id}`"
          class="clickable"
          @click="goDetail(a.id)"
        >
          <ArticleCard :article="a" />
        </div>
      </div>
      <p v-else class="hint">暂无文章</p>
      <div class="pager">
        <button class="btn ghost" @click="toPage(-1)">上一页</button>
        <span>{{ myPage }} / {{ Math.max(1, Math.ceil(totalCount / myPageSize)) }}</span>
        <button class="btn ghost" @click="toPage(1)">下一页</button>
      </div>
    </div>
  </section>
</template>
