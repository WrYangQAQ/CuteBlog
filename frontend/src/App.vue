<script setup>
import { computed, onMounted, ref } from "vue";
import { useAuthStore } from "./stores/auth";
import FeedbackPopup from "./components/FeedbackPopup.vue";
import {
  Home,
  FileText,
  LayoutGrid,
  Tag,
  PlusCircle,
  Archive,
  User,
  MessageCircle
} from "lucide-vue-next";
import logoShark from "./assets/images/logo-shark.png";

const authStore = useAuthStore();
const darkMode = ref(false);

onMounted(() => {
  authStore.restoreFromToken();
});

const isLoggedIn = computed(() => authStore.isLoggedIn);
const isAdmin = computed(() => authStore.isAdmin);

const menuItems = computed(() => [
  { to: "/", text: "首页", icon: Home },
  { to: "/articles", text: "文章", icon: FileText },
  { to: "/categories", text: "分类", icon: LayoutGrid },
  { to: "/tags", text: "标签", icon: Tag },
  { to: "/publish", text: "发布", icon: PlusCircle },
  { to: "/archive", text: "归档", icon: Archive },
  { to: "/profile", text: "关于我", icon: User },
  { to: "/messages", text: "留言板", icon: MessageCircle }
]);

function toggleTheme() {
  darkMode.value = !darkMode.value;
  document.body.classList.toggle("night-mode", darkMode.value);
}
</script>

<template>
  <div class="app-shell">
    <template v-if="isLoggedIn">
      <div class="shark-layout">
        <aside class="shark-sidebar card">
          <div class="sidebar-brand" @click="$router.push('/')">
            <img class="brand-logo" :src="logoShark" alt="Sharky logo" />
            <h1>Sharky</h1>
            <p>Personal Blog</p>
          </div>

          <nav class="sidebar-menu">
            <router-link v-for="item in menuItems" :key="item.to" :to="item.to" class="menu-item">
              <component :is="item.icon" :size="18" class="menu-lucide" />
              <span>{{ item.text }}</span>
            </router-link>
          </nav>

          <div class="sidebar-footer">
            <button class="night-btn" @click="toggleTheme">{{ darkMode ? "Light" : "Night" }}</button>
            <router-link v-if="isAdmin" to="/admin/dashboard" class="admin-link">管理员入口</router-link>
          </div>
        </aside>

        <section class="shark-main">
          <main class="page-container">
            <router-view />
          </main>
        </section>
      </div>
    </template>

    <template v-else>
      <main class="auth-root">
        <router-view />
      </main>
    </template>

    <FeedbackPopup />
  </div>
</template>
