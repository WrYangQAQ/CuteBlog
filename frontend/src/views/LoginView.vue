<script setup>
import { reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";

const router = useRouter();
const authStore = useAuthStore();

const form = reactive({
  usernameOrEmail: "",
  password: ""
});

const loading = ref(false);
const message = ref("");

async function submit() {
  loading.value = true;
  message.value = "";
  try {
    await authStore.login(form);
    await authStore.fetchProfile();
    router.push("/");
  } catch (err) {
    message.value = err?.payload?.message || err.message || "登录失败";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <section class="panel auth-panel">
    <h2>欢迎回来</h2>
    <p class="subtitle">登录后就能进入你的萌系博客世界</p>

    <form class="form-grid" @submit.prevent="submit">
      <label>
        用户名或邮箱
        <input v-model.trim="form.usernameOrEmail" required />
      </label>
      <label>
        密码
        <input v-model="form.password" type="password" required />
      </label>
      <button class="btn solid" :disabled="loading">{{ loading ? "登录中..." : "登录" }}</button>
      <p v-if="message" class="error">{{ message }}</p>
    </form>
  </section>
</template>
