<script setup>
import { onMounted, reactive, ref } from "vue";
import { useAuthStore } from "../stores/auth";
import {
  getMyArticlesApi,
  updateProfileApi,
  uploadAvatarApi
} from "../api/auth";
import { formatDate, toAbsoluteAsset } from "../utils/asset";

const authStore = useAuthStore();
const loading = ref(false);
const message = ref("");
const myPage = ref(1);
const myPageSize = 5;
const myArticles = ref([]);
const totalCount = ref(0);

const profileForm = reactive({
  nickName: "",
  bio: ""
});

async function loadProfile() {
  loading.value = true;
  message.value = "";
  try {
    const profile = await authStore.fetchProfile();
    profileForm.nickName = profile?.nickName || "";
    profileForm.bio = profile?.bio || "";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "获取资料失败";
  } finally {
    loading.value = false;
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

async function saveProfile() {
  try {
    await updateProfileApi(profileForm);
    message.value = "资料更新成功";
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

onMounted(async () => {
  await loadProfile();
  await loadMyArticles();
});
</script>

<template>
  <section class="stack">
    <div class="panel">
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

      <form class="form-grid" @submit.prevent="saveProfile">
        <label>
          昵称
          <input v-model.trim="profileForm.nickName" maxlength="20" />
        </label>
        <label>
          简介
          <textarea v-model="profileForm.bio" maxlength="200" />
        </label>
        <button class="btn solid">保存资料</button>
      </form>
    </div>

    <div class="panel">
      <h2>我点赞的文章</h2>
      <div v-if="authStore.profile?.articlesLike?.length" class="list-grid">
        <div v-for="a in authStore.profile.articlesLike" :key="`like-${a.id}`" class="line-card">
          <strong>{{ a.title }}</strong>
          <span>{{ formatDate(a.createdAt) }}</span>
        </div>
      </div>
      <p v-else class="hint">还没有点赞过文章</p>
    </div>

    <div class="panel">
      <h2>我发布的文章</h2>
      <div v-if="myArticles.length" class="list-grid">
        <div v-for="a in myArticles" :key="`mine-${a.id}`" class="line-card">
          <strong>{{ a.title }}</strong>
          <span>{{ formatDate(a.createdAt) }}</span>
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
