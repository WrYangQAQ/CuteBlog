<script setup>
import { reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { registerApi } from "../api/auth";
import registerBackground from "../assets/images/register-background.png";
import logoShark from "../assets/images/logo-shark.png";
import copyrightLogo from "../assets/images/copyright-logo.png";
import { Eye, EyeOff, Lock, Mail, UserRound } from "lucide-vue-next";

const router = useRouter();
const loading = ref(false);
const message = ref("");
const showPassword = ref(false);

const form = reactive({
  username: "",
  email: "",
  password: "",
  nickName: ""
});

async function submit() {
  loading.value = true;
  message.value = "";
  try {
    await registerApi(form);
    message.value = "注册成功，正在跳转登录页...";
    setTimeout(() => router.push("/login"), 700);
  } catch (err) {
    message.value = err?.payload?.message || err.message || "注册失败";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <section class="login-scene-wrap">
    <div class="login-scene" :style="{ backgroundImage: `url(${registerBackground})` }">
      <div class="login-welcome">
        <div class="login-brand">
          <img :src="logoShark" alt="Sharky logo" />
          <div>
            <h1>Sharky</h1>
            <p>Personal Blog</p>
          </div>
        </div>
        <h2>欢迎注册</h2>
        <p>创建账号，开启你的编程学习之旅</p>
      </div>

      <div class="auth-card login-panel">
        <h2>创建账号</h2>
        <p class="hint">已有账号？<a @click.prevent="$router.push('/login')">立即登录</a></p>

        <form class="form-grid" @submit.prevent="submit">
          <label>
            用户名
            <div class="input-wrap">
              <UserRound :size="18" class="input-icon" />
              <input v-model.trim="form.username" minlength="3" maxlength="20" required placeholder="请输入用户名" />
            </div>
          </label>

          <label>
            邮箱
            <div class="input-wrap">
              <Mail :size="18" class="input-icon" />
              <input v-model.trim="form.email" type="email" required placeholder="请输入邮箱" />
            </div>
          </label>

          <label>
            密码
            <div class="input-wrap">
              <Lock :size="18" class="input-icon" />
              <input
                v-model="form.password"
                :type="showPassword ? 'text' : 'password'"
                minlength="10"
                required
                placeholder="请输入密码"
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

          <label>
            昵称（可选）
            <div class="input-wrap">
              <UserRound :size="18" class="input-icon" />
              <input v-model.trim="form.nickName" maxlength="20" placeholder="请输入昵称" />
            </div>
          </label>

          <button class="btn solid" :disabled="loading">{{ loading ? "注册中..." : "注册" }}</button>
        </form>
      </div>

      <div class="login-copyright">
        <span>©️2026 Sharky Blog. All rights reserved.</span>
        <img :src="copyrightLogo" alt="copyright logo" />
      </div>
    </div>
  </section>
</template>
