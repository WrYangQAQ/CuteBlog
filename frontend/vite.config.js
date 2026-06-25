import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  base: "/app/",
  plugins: [vue()],
  server: {
    host: "0.0.0.0",
    port: 5174,
    proxy: {
      "/api": {
        target: "http://127.0.0.1:5125",
        changeOrigin: true,
        secure: false
      },
      "/Picture": {
        target: "http://127.0.0.1:5125",
        changeOrigin: true,
        secure: false
      }
    }
  }
});
