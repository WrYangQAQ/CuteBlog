import { reactive } from "vue";

const state = reactive({
  visible: false,
  type: "error",
  title: "提示",
  message: "",
  timer: null
});

function clearTimer() {
  if (state.timer) {
    clearTimeout(state.timer);
    state.timer = null;
  }
}

export function hideFeedback() {
  clearTimer();
  state.visible = false;
}

export function showFeedback({ type = "error", title = "提示", message = "", autoCloseMs = 0 } = {}) {
  clearTimer();
  state.type = type;
  state.title = title;
  state.message = message || "发生未知错误";
  state.visible = true;

  if (autoCloseMs > 0) {
    state.timer = setTimeout(() => {
      state.visible = false;
      state.timer = null;
    }, autoCloseMs);
  }
}

export function showError(message, title = "操作失败") {
  showFeedback({ type: "error", title, message, autoCloseMs: 0 });
}

export function showSuccess(message, title = "操作成功") {
  showFeedback({ type: "success", title, message, autoCloseMs: 1700 });
}

export function useFeedbackState() {
  return state;
}
