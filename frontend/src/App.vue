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

function logout() {
  authStore.logout();
  router.push("/login");
}
</script>

<template>
  <div class="app-shell">
    <header class="cute-header">
      <div class="brand" @click="$router.push('/')">
        <span class="brand-badge">萌</span>
        <h1>CuteBlog</h1>
      </div>
      <nav class="nav-links">
        <router-link v-if="isLoggedIn" to="/">首页</router-link>
        <router-link v-if="isLoggedIn" to="/publish">发布文章</router-link>
        <router-link v-if="isLoggedIn" to="/my-articles">我的文章</router-link>
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
