<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import { getMyArticlesApi, updateProfileApi, uploadAvatarApi } from "../api/auth";
import { getArticlesApi } from "../api/articles";
import { toAbsoluteAsset } from "../utils/asset";
import bannerHome from "../assets/images/banner-home.png";
import decorationShark from "../assets/images/decoration-shark.png";
import { User } from "lucide-vue-next";
import { showSuccess } from "../stores/feedback";

const router = useRouter();
const authStore = useAuthStore();
const message = ref("");
const isEditing = ref(false);
const allArticles = ref([]);
const myArticles = ref([]);

const profileForm = reactive({ nickName: "", bio: "" });

const profile = computed(() => authStore.profile || {});
const totalTags = computed(() => {
  const set = new Set();
  (allArticles.value || []).forEach((a) => (a.tagNames || []).forEach((t) => set.add(t)));
  return set.size;
});

function fillForm() {
  profileForm.nickName = profile.value.nickName || "";
  profileForm.bio = profile.value.bio || "";
}

async function loadData() {
  try {
    await authStore.fetchProfile();
    fillForm();
    const [allRes, mineRes] = await Promise.all([getArticlesApi(), getMyArticlesApi(1, 6)]);
    allArticles.value = allRes.data || [];
    myArticles.value = mineRes.data?.items || [];
  } catch (err) {
    message.value = err?.payload?.message || err.message || "加载失败";
  }
}

async function saveProfile() {
  try {
    await updateProfileApi(profileForm);
    isEditing.value = false;
    await authStore.fetchProfile();
    showSuccess("个人信息修改成功");
  } catch (err) {
    message.value = err?.payload?.message || err.message || "更新失败";
  }
}

async function uploadAvatar(event) {
  const file = event.target.files?.[0];
  if (!file) return;
  try {
    await uploadAvatarApi(file);
    await authStore.fetchProfile();
    showSuccess("头像上传成功");
  } catch (err) {
    message.value = err?.payload?.message || err.message || "头像上传失败";
  }
}

function goDetail(id) {
  router.push(`/articles/${id}`);
}

function logout() {
  authStore.logout();
  router.push("/login");
}

onMounted(loadData);
</script>

<template>
  <section class="page-stack">
    <header class="sea-hero mini" :style="{ backgroundImage: `url(${bannerHome})` }">
      <div class="hero-copy">
        <h1>关于我 <User :size="28" class="title-icon" /></h1>
        <p class="hero-sub">了解更多关于站长和这个博客的故事</p>
      </div>
      <img class="hero-avatar" :src="decorationShark" alt="profile decoration" />
    </header>

    <div class="content-grid">
      <section class="panel">
        <div class="profile-header">
          <img class="profile-avatar" :src="toAbsoluteAsset(profile.avatarUrl)" alt="avatar" />
          <div class="profile-intro">
            <h2>{{ profile.nickName || profile.userName || "Sharky" }}</h2>
            <p>{{ profile.bio || "热爱技术、喜欢分享、持续学习。" }}</p>
            <div class="action-row">
              <label class="btn ghost">
                上传头像
                <input hidden type="file" accept="image/*" @change="uploadAvatar" />
              </label>
              <button class="btn ghost" @click="isEditing = !isEditing">{{ isEditing ? '取消编辑' : '编辑资料' }}</button>
            </div>
          </div>
          <button class="btn danger profile-logout-btn" @click="logout">退出登录</button>
        </div>

        <form v-if="isEditing" class="form-grid" @submit.prevent="saveProfile">
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


        <div class="timeline-grid">
          <section class="panel inner">
            <h3>我的成长轨迹</h3>
            <ul class="tips-list">
              <li>2024 年：持续输出内容，打磨项目实战能力</li>
              <li>2023 年：开始技术分享，博客访问量持续增长</li>
              <li>2022 年：创建博客，记录学习历程</li>
            </ul>
          </section>
          <section class="panel inner">
            <h3>技术栈</h3>
            <ul class="rank-list plain">
              <li><span>Vue / JavaScript</span><b>85%</b></li>
              <li><span>C# / ASP.NET Core</span><b>80%</b></li>
              <li><span>SQL Server</span><b>75%</b></li>
              <li><span>UI / UX</span><b>65%</b></li>
            </ul>
          </section>
        </div>
      </section>

      <aside class="right-column">
        <section class="panel side-panel">
          <h2>博客数据</h2>
          <div class="mini-stats two-col">
            <div><strong>{{ allArticles.length }}</strong><span>文章总数</span></div>
            <div><strong>{{ totalTags }}</strong><span>标签数量</span></div>
            <div><strong>{{ myArticles.length }}</strong><span>我的文章</span></div>
            <div><strong>{{ profile.articlesLike?.length || 0 }}</strong><span>点赞文章</span></div>
          </div>
        </section>

        <section class="panel side-panel">
          <h2>我发布的文章</h2>
          <ul class="rank-list">
            <li v-for="item in myArticles" :key="item.id" @click="goDetail(item.id)">
              <span>{{ item.title }}</span>
              <b>→</b>
            </li>
          </ul>
        </section>
      </aside>
    </div>
  </section>
</template>





