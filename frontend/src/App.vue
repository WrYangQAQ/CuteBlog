<script setup>
import { computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "./stores/auth";

const authStore = useAuthStore();
const router = useRouter();

onMounted(() => {
  authStore.restoreFromToken();
});

const isLoggedIn = computed(() => authStore.isLoggedIn);
const isAdmin = computed(() => authStore.isAdmin);

const leftPetals = Array.from({ length: 10 }, (_, i) => ({
  id: `l-${i}`,
  left: `${5 + (i % 3) * 18}%`,
  delay: `${(i % 5) * 1.4}s`,
  duration: `${9 + (i % 4) * 1.7}s`
}));

const rightPetals = Array.from({ length: 10 }, (_, i) => ({
  id: `r-${i}`,
  right: `${4 + (i % 3) * 17}%`,
  delay: `${0.8 + (i % 5) * 1.3}s`,
  duration: `${10 + (i % 4) * 1.6}s`
}));

function logout() {
  authStore.logout();
  router.push("/login");
}
</script>

<template>
  <div class="app-shell">
    <div class="sakura-layer left">
      <span
        v-for="p in leftPetals"
        :key="p.id"
        class="petal"
        :style="{ left: p.left, animationDelay: p.delay, animationDuration: p.duration }"
      ></span>
    </div>
    <div class="sakura-layer right">
      <span
        v-for="p in rightPetals"
        :key="p.id"
        class="petal"
        :style="{ right: p.right, animationDelay: p.delay, animationDuration: p.duration }"
      ></span>
    </div>

    <header class="cute-header">
      <div class="brand" @click="$router.push('/')">
        <span class="brand-badge">萌</span>
        <h1>CuteBlog</h1>
      </div>
      <nav class="nav-links">
        <router-link v-if="isLoggedIn" to="/">首页</router-link>
        <router-link v-if="isLoggedIn" to="/articles">全部文章</router-link>
        <router-link v-if="isLoggedIn" to="/publish">发布文章</router-link>
        <router-link v-if="isLoggedIn" to="/profile">个人中心</router-link>
        <router-link v-if="isAdmin" to="/admin/dashboard">管理台</router-link>
      </nav>
      <div class="auth-actions">
        <router-link v-if="!isLoggedIn" class="btn ghost" to="/login">登录</router-link>
        <router-link v-if="!isLoggedIn" class="btn solid" to="/register">注册</router-link>
        <button v-if="isLoggedIn" class="btn danger" @click="logout">退出</button>
      </div>
    </header>

    <main class="page-container">
      <router-view />
    </main>
  </div>
</template>
