<script setup>
import { reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { registerApi } from "../api/auth";

const router = useRouter();
const loading = ref(false);
const message = ref("");

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
    message.value = "注册成功，正在跳转登录...";
    setTimeout(() => router.push("/login"), 700);
  } catch (err) {
    message.value = err?.payload?.message || err.message || "注册失败";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <section class="panel auth-panel">
    <h2>创建账号</h2>
    <p class="subtitle">加入 CuteBlogSystem，开始记录吧</p>

    <form class="form-grid" @submit.prevent="submit">
      <label>
        用户名
        <input v-model.trim="form.username" minlength="3" maxlength="20" required />
      </label>
      <label>
        邮箱
        <input v-model.trim="form.email" type="email" required />
      </label>
      <label>
        密码（至少10位）
        <input v-model="form.password" type="password" minlength="10" required />
      </label>
      <label>
        昵称（可选）
        <input v-model.trim="form.nickName" maxlength="20" />
      </label>
      <button class="btn solid" :disabled="loading">{{ loading ? "提交中..." : "注册" }}</button>
      <p v-if="message" :class="message.includes('成功') ? 'ok' : 'error'">{{ message }}</p>
    </form>
  </section>
</template>
