import { defineStore } from "pinia";
import { getProfileApi, loginApi } from "../api/auth";

function parseTokenPayload(token) {
  try {
    const payload = token.split(".")[1];
    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    const json = decodeURIComponent(
      atob(base64)
        .split("")
        .map((c) => `%${`00${c.charCodeAt(0).toString(16)}`.slice(-2)}`)
        .join("")
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}

function getRoleFromPayload(payload) {
  if (!payload) return "";
  return (
    payload.role ||
    payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
    ""
  );
}

export const useAuthStore = defineStore("auth", {
  state: () => ({
    token: localStorage.getItem("token") || "",
    profile: null,
    role: localStorage.getItem("role") || ""
  }),
  getters: {
    isLoggedIn: (state) => Boolean(state.token),
    isAdmin: (state) => state.role === "Admin"
  },
  actions: {
    restoreFromToken() {
      if (!this.token) return;
      const payload = parseTokenPayload(this.token);
      this.role = getRoleFromPayload(payload);
      localStorage.setItem("role", this.role);
    },
    async login(form) {
      const res = await loginApi(form);
      const token = res.data;
      this.token = token;
      localStorage.setItem("token", token);
      this.restoreFromToken();
      return res;
    },
    logout() {
      this.token = "";
      this.profile = null;
      this.role = "";
      localStorage.removeItem("token");
      localStorage.removeItem("role");
    },
    async fetchProfile() {
      if (!this.token) return null;
      const res = await getProfileApi();
      this.profile = res.data;
      return res.data;
    }
  }
});
