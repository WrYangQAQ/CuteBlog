<script setup>
import { reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import loginBackground from "../assets/images/login-background.png";
import logoShark from "../assets/images/logo-shark.png";
import copyrightLogo from "../assets/images/copyright-logo.png";
import { Eye, EyeOff, Lock, UserRound } from "lucide-vue-next";

const router = useRouter();
const authStore = useAuthStore();

const form = reactive({ usernameOrEmail: "", password: "" });
const loading = ref(false);
const message = ref("");
const showPassword = ref(false);

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
  <section class="login-scene-wrap">
    <div class="login-scene" :style="{ backgroundImage: `url(${loginBackground})` }">
      <div class="login-welcome">
        <div class="login-brand">
          <img :src="logoShark" alt="Sharky logo" />
          <div>
            <h1>Sharky</h1>
            <p>Personal Blog</p>
          </div>
        </div>
        <h2>欢迎登录</h2>
        <p>登录后继续探索 Sharky 的小世界</p>
      </div>

      <div class="auth-card login-panel">
        <h2>登录账号</h2>
        <p class="hint">还没有账号？<a @click.prevent="$router.push('/register')">立即注册</a></p>

        <form class="form-grid" @submit.prevent="submit">
          <label>
            邮箱 / 用户名
            <div class="input-wrap">
              <UserRound :size="18" class="input-icon" />
              <input
                v-model.trim="form.usernameOrEmail"
                required
                placeholder="请输入邮箱或用户名"
                autocomplete="username"
              />
            </div>
          </label>
          <label>
            密码
            <div class="input-wrap">
              <Lock :size="18" class="input-icon" />
              <input
                v-model="form.password"
                :type="showPassword ? 'text' : 'password'"
                required
                placeholder="请输入密码"
                autocomplete="current-password"
              />
              <button
                type="button"
                class="pass-toggle"
                :aria-label="showPassword ? '隐藏密码' : '显示密码'"
                @click="showPassword = !showPassword"
              >
                <EyeOff v-if="showPassword" :size="18" />
                <Eye v-else :size="18" />
              </button>
            </div>
          </label>
          <button class="btn solid" :disabled="loading">{{ loading ? "登录中..." : "登录" }}</button>
        </form>
      </div>

      <div class="login-copyright">
        <span>©️2026 Sharky Blog. All rights reserved.</span>
        <img :src="copyrightLogo" alt="copyright logo" />
      </div>
    </div>
  </section>
</template>
