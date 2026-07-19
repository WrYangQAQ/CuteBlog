<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { marked } from "marked";
import DOMPurify from "dompurify";
import { Archive, Bug, CircleArrowDown, CircleArrowUp, ClipboardCheck, Download, Eye, MoreHorizontal, Pencil, Play, Plus, RefreshCw, RotateCcw, Save, Trash2, X } from "lucide-vue-next";
import {
  archiveAgentConversationApi,
  compareAgentEvaluationRunsApi,
  askAgentApi,
  cancelAgentConfirmationApi,
  confirmAgentPlanApi,
  deleteAgentConversationApi,
  downloadAgentEvaluationReportApi,
  createAgentEvaluationTestCaseApi,
  deleteAgentEvaluationTestCaseApi,
  getAgentConversationsApi,
  getAgentEvaluationRunResultsApi,
  getAgentEvaluationReportApi,
  getAgentEvaluationReportSnapshotApi,
  getAgentEvaluationRegressionSummaryApi,
  getAgentEvaluationTestCasesApi,
  getAgentEvaluationWorkflowLogApi,
  getAgentWorkflowLogDetailApi,
  getRecentAgentWorkflowLogsApi,
  getAgentMessagesApi,
  getArchivedAgentConversationsApi,
  getRecentAgentEvaluationRunsApi,
  restoreAgentConversationApi,
  rerunAgentEvaluationRunApi,
  runAgentEvaluationApi,
  saveAgentEvaluationReportSnapshotApi,
  toggleAgentEvaluationTestCaseStatusApi,
  updateAgentEvaluationTestCaseApi
} from "../api/agent";
import { useAuthStore } from "../stores/auth";
import { showSuccess } from "../stores/feedback";
import { formatDateTime as formatChinaDateTime, getChinaDateParts, toAbsoluteAsset } from "../utils/asset";
import agentAvatar from "../assets/images/agent-avator.png";
import userAvatarFallback from "../assets/images/logo-shark.png";

const authStore = useAuthStore();
const input = ref("");
const loading = ref(false);
const historyLoading = ref(false);
const messageLoading = ref(false);
const sessionId = ref("");
const conversations = ref([]);
const messages = ref([createWelcomeMessage()]);
const dialogRef = ref(null);
const inputRef = ref(null);
const expandedUserMessageIndexes = ref(new Set());
const openConversationMenuId = ref("");
const conversationActionLoadingId = ref("");
const confirmationLoadingId = ref("");
const conversationView = ref("active");
const logPanelOpen = ref(false);
const workflowLogs = ref([]);
const selectedWorkflowLog = ref(null);
const workflowLogLoading = ref(false);
const workflowLogDetailLoading = ref(false);
const evaluationPanelOpen = ref(false);
const evaluationActiveTab = ref("cases");
const evaluationRuns = ref([]);
const evaluationResults = ref([]);
const evaluationTestCases = ref([]);
const selectedEvaluationRun = ref(null);
const selectedEvaluationCaseIds = ref([]);
const evaluationCaseStatus = ref(1);
const evaluationRunLoading = ref(false);
const evaluationResultLoading = ref(false);
const evaluationCaseLoading = ref(false);
const evaluationRunning = ref(false);
const evaluationCaseSaving = ref(false);
const evaluationEditingCaseId = ref(0);
const evaluationCaseFormOpen = ref(false);
const evaluationCaseForm = ref(createEvaluationCaseForm());
const evaluationCompareBaseRunId = ref("");
const evaluationCompareTargetRunId = ref("");
const evaluationCompareResult = ref(null);
const evaluationRegressionSummary = ref(null);
const evaluationCompareLoading = ref(false);
const evaluationReportLoading = ref(false);
const evaluationSnapshotLoading = ref(false);
const evaluationReportPreviewOpen = ref(false);
const evaluationReportPreview = ref({ fileName: "", markdown: "" });

const canSend = computed(
  () => conversationView.value === "active" && input.value.trim() && !loading.value
);
const groupedConversations = computed(() => groupConversations(conversations.value));
const userAvatar = computed(() => toAbsoluteAsset(authStore.profile?.avatarUrl) || userAvatarFallback);
const isAdmin = computed(() => localStorage.getItem("role") === "Admin");
const evaluationResultStats = computed(() => {
  const total = evaluationResults.value.length;
  const passed = evaluationResults.value.filter((item) => isEvaluationResultPassed(item)).length;
  const failed = Math.max(total - passed, 0);
  const failureMap = new Map();

  evaluationResults.value.forEach((item) => {
    if (isEvaluationResultPassed(item)) return;
    const key = getEvaluationFailureTypeKey(item);
    const label = getEvaluationFailureTypeLabel(item);
    const current = failureMap.get(key) || { key, label, count: 0 };
    current.count += 1;
    failureMap.set(key, current);
  });

  return {
    total,
    passed,
    failed,
    failureGroups: Array.from(failureMap.values()).sort((a, b) => b.count - a.count)
  };
});
const canCompareEvaluationRuns = computed(
  () => evaluationCompareBaseRunId.value && evaluationCompareTargetRunId.value && evaluationCompareBaseRunId.value !== evaluationCompareTargetRunId.value
);

function createWelcomeMessage() {
  return {
    role: "assistant",
    content: "你好，我是博客助手 Agent。你可以让我帮你润色文章、生成标签、推荐分类，或继续当前会话里的上下文任务。"
  };
}

function renderAssistantMarkdown(content) {
  return DOMPurify.sanitize(marked.parse(content || ""));
}

function shouldCollapseUserMessage(message) {
  if (message?.role !== "user") return false;
  const content = message.content || "";
  const hardLineCount = content.split(/\r?\n/).length;
  return hardLineCount > 7 || content.length > 260;
}

function isUserMessageExpanded(index) {
  return expandedUserMessageIndexes.value.has(index);
}

function toggleUserMessage(index) {
  const next = new Set(expandedUserMessageIndexes.value);
  if (next.has(index)) {
    next.delete(index);
  } else {
    next.add(index);
  }
  expandedUserMessageIndexes.value = next;
}

function adjustInputHeight() {
  nextTick(() => {
    const textarea = inputRef.value;
    if (!textarea) return;
    textarea.style.height = "auto";
    textarea.style.height = `${Math.min(textarea.scrollHeight, 180)}px`;
  });
}

function scrollDialogToBottom() {
  nextTick(() => {
    const dialog = dialogRef.value;
    if (!dialog) return;
    dialog.scrollTop = dialog.scrollHeight;
  });
}

function getTokenPayload() {
  const token = localStorage.getItem("token");
  if (!token) return null;

  try {
    const payload = token.split(".")[1];
    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    const json = decodeURIComponent(
      atob(base64)
        .split("")
        .map((char) => `%${`00${char.charCodeAt(0).toString(16)}`.slice(-2)}`)
        .join("")
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}

function getCurrentUserId() {
  const payload = getTokenPayload();
  const rawId =
    payload?.nameid ||
    payload?.sub ||
    payload?.userId ||
    payload?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];

  const parsed = Number(rawId);
  return Number.isFinite(parsed) ? parsed : 0;
}

function formatAgentResponse(response) {
  if (typeof response === "string") return response;
  if (!response || typeof response !== "object") return "Agent 已响应，但没有返回可展示内容。";

  return (
    response.answer ||
    response.Answer ||
    response.finalAnswer ||
    response.result ||
    response.content ||
    response.message ||
    response.data?.answer ||
    response.data?.Answer ||
    response.data?.finalAnswer ||
    response.data?.result ||
    JSON.stringify(response, null, 2)
  );
}

function getResponseValue(response, camelKey, pascalKey) {
  return response?.[camelKey] ?? response?.[pascalKey] ?? response?.data?.[camelKey] ?? response?.data?.[pascalKey];
}

function createAssistantMessageFromResponse(response) {
  const requiresConfirmation = Boolean(
    getResponseValue(response, "requiresConfirmation", "RequiresConfirmation")
  );
  const confirmationId = getResponseValue(response, "confirmationId", "ConfirmationId") || "";

  return {
    role: "assistant",
    content: formatAgentResponse(response),
    requiresConfirmation: requiresConfirmation && Boolean(confirmationId),
    confirmationId,
    confirmationSummary: getResponseValue(response, "confirmationSummary", "ConfirmationSummary") || "",
    confirmationStatus: requiresConfirmation && confirmationId ? "pending" : "none"
  };
}
function formatConversationTitle(item) {
  return item.title || item.Title || "新对话";
}

function getConversationSessionId(item) {
  return item.sessionId || item.SessionId || "";
}

function getConversationUpdatedAt(item) {
  return item.updatedAt || item.UpdatedAt || item.createdAt || item.CreatedAt || "";
}

function getMessageRole(item) {
  const role = item.role ?? item.Role;
  if (role === 2 || role === "User" || role === "user") return "user";
  return "assistant";
}

function getMessageContent(item) {
  return item.content || item.Content || "";
}

function normalizeMessages(rows) {
  const list = (rows || [])
    .map((item) => ({
      role: getMessageRole(item),
      content: getMessageContent(item)
    }))
    .filter((item) => item.content.trim());

  return list.length ? list : [createWelcomeMessage()];
}

function isSameChinaDay(dateText, targetParts) {
  const parts = getChinaDateParts(dateText);
  return Boolean(parts && targetParts) &&
    parts.year === targetParts.year &&
    parts.month === targetParts.month &&
    parts.day === targetParts.day;
}

function groupConversations(rows) {
  const groups = [
    { group: "今天", items: [] },
    { group: "昨天", items: [] },
    { group: "7天内", items: [] },
    { group: "更早", items: [] }
  ];

  const now = new Date();
  const todayParts = getChinaDateParts(now);
  const yesterday = new Date(now);
  yesterday.setDate(now.getDate() - 1);
  const yesterdayParts = getChinaDateParts(yesterday);

  rows.forEach((item) => {
    const updatedAt = getConversationUpdatedAt(item);
    const updated = getChinaDateParts(updatedAt);
    const bucket =
      !updated
        ? groups[3]
        : isSameChinaDay(updatedAt, todayParts)
          ? groups[0]
          : isSameChinaDay(updatedAt, yesterdayParts)
            ? groups[1]
            : now.getTime() - new Date(`${updated.year}-${String(updated.month).padStart(2, "0")}-${String(updated.day).padStart(2, "0")}T00:00:00+08:00`).getTime() <= 7 * 24 * 60 * 60 * 1000
              ? groups[2]
              : groups[3];

    bucket.items.push(item);
  });

  return groups.filter((group) => group.items.length);
}

async function loadConversations() {
  historyLoading.value = true;
  try {
    const res = conversationView.value === "archived"
      ? await getArchivedAgentConversationsApi()
      : await getAgentConversationsApi();
    conversations.value = Array.isArray(res.data) ? res.data : [];
  } catch {
    conversations.value = [];
  } finally {
    historyLoading.value = false;
  }
}

async function loadMessages(nextSessionId) {
  if (!nextSessionId) {
    messages.value = [createWelcomeMessage()];
    return;
  }

  messageLoading.value = true;
  try {
    const res = await getAgentMessagesApi(nextSessionId);
    messages.value = normalizeMessages(res.data);
  } catch (err) {
    messages.value = [
      {
        role: "assistant",
        content: err?.payload?.message || err?.message || "会话消息加载失败，请稍后再试。"
      }
    ];
  } finally {
    messageLoading.value = false;
  }
}

function newConversation() {
  openConversationMenuId.value = "";
  conversationView.value = "active";
  sessionId.value = "";
  loadConversations();
  messages.value = [
    {
      role: "assistant",
      content: "新的会话已开启。告诉我你想处理的博客任务吧。"
    }
  ];
}

async function setConversationView(view) {
  if (conversationView.value === view) return;

  conversationView.value = view;
  openConversationMenuId.value = "";
  sessionId.value = "";
  messages.value = [
    {
      role: "assistant",
      content: view === "archived"
        ? "这里是已归档会话。你可以查看历史消息、恢复会话或将其删除。"
        : "已返回活跃会话列表。选择一个会话继续，或开启新对话。"
    }
  ];

  await loadConversations();
}

function toggleConversationMenu(item) {
  const itemSessionId = getConversationSessionId(item);
  openConversationMenuId.value =
    openConversationMenuId.value === itemSessionId ? "" : itemSessionId;
}

function closeConversationMenu() {
  openConversationMenuId.value = "";
}

function handleDocumentKeydown(event) {
  if (event.key === "Escape") {
    closeConversationMenu();
  }
}

async function switchConversation(item) {
  const nextSessionId = getConversationSessionId(item);
  if (!nextSessionId) return;

  sessionId.value = nextSessionId;
  await loadMessages(nextSessionId);
}

async function archiveConversation(item) {
  const itemSessionId = getConversationSessionId(item);
  if (!itemSessionId || conversationActionLoadingId.value) return;

  conversationActionLoadingId.value = itemSessionId;
  closeConversationMenu();

  try {
    const response = await archiveAgentConversationApi(itemSessionId);
    if (sessionId.value === itemSessionId) {
      newConversation();
    }
    await loadConversations();
    showSuccess(response.message || "会话已归档");
  } finally {
    conversationActionLoadingId.value = "";
  }
}

async function restoreConversation(item) {
  const itemSessionId = getConversationSessionId(item);
  if (!itemSessionId || conversationActionLoadingId.value) return;

  conversationActionLoadingId.value = itemSessionId;
  closeConversationMenu();

  try {
    const response = await restoreAgentConversationApi(itemSessionId);
    if (sessionId.value === itemSessionId) {
      sessionId.value = "";
      messages.value = [
        {
          role: "assistant",
          content: "该会话已恢复到活跃列表，可以切换到活跃会话继续交流。"
        }
      ];
    }
    await loadConversations();
    showSuccess(response.message || "会话已恢复");
  } finally {
    conversationActionLoadingId.value = "";
  }
}

async function deleteConversation(item) {
  const itemSessionId = getConversationSessionId(item);
  if (!itemSessionId || conversationActionLoadingId.value) return;

  const confirmed = window.confirm(
    `确定删除会话“${formatConversationTitle(item)}”吗？此操作无法恢复。`
  );
  if (!confirmed) return;

  conversationActionLoadingId.value = itemSessionId;
  closeConversationMenu();

  try {
    const response = await deleteAgentConversationApi(itemSessionId);
    if (sessionId.value === itemSessionId) {
      newConversation();
    }
    await loadConversations();
    showSuccess(response.message || "会话已删除");
  } finally {
    conversationActionLoadingId.value = "";
  }
}

async function syncLatestConversation() {
  await loadConversations();
  const latest = conversations.value[0];
  const latestSessionId = latest ? getConversationSessionId(latest) : "";
  if (latestSessionId) {
    sessionId.value = latestSessionId;
  }
}

function canHandleConfirmation(message) {
  return message.role === "assistant" &&
    message.requiresConfirmation &&
    message.confirmationStatus === "pending" &&
    message.confirmationId;
}

async function confirmAgentPlan(message) {
  if (loading.value || !canHandleConfirmation(message) || confirmationLoadingId.value) return;

  confirmationLoadingId.value = message.confirmationId;

  try {
    const response = await confirmAgentPlanApi({
      sessionId: sessionId.value,
      confirmationId: message.confirmationId
    });

    message.requiresConfirmation = false;
    message.confirmationStatus = response.success ? "confirmed" : "failed";

    messages.value.push(createAssistantMessageFromResponse(response));
    await syncLatestConversation();
  } catch (err) {
    message.confirmationStatus = "failed";
    messages.value.push({
      role: "assistant",
      content: err?.payload?.message || err?.message || "确认执行失败，请稍后再试。"
    });
  } finally {
    confirmationLoadingId.value = "";
  }
}

async function cancelAgentConfirmation(message) {
  if (loading.value || !canHandleConfirmation(message) || confirmationLoadingId.value) return;

  confirmationLoadingId.value = message.confirmationId;

  try {
    const response = await cancelAgentConfirmationApi({
      sessionId: sessionId.value,
      confirmationId: message.confirmationId
    });

    message.requiresConfirmation = false;
    message.confirmationStatus = response.success ? "cancelled" : "failed";
  } catch (err) {
    message.confirmationStatus = "failed";
    messages.value.push({
      role: "assistant",
      content: err?.payload?.message || err?.message || "取消确认失败，请稍后再试。"
    });
  } finally {
    confirmationLoadingId.value = "";
  }
}

function getLogValue(item, camelKey, pascalKey) {
  return item?.[camelKey] ?? item?.[pascalKey];
}

function getLogId(item) {
  return getLogValue(item, "id", "Id");
}

function getLogUserMessage(item) {
  return getLogValue(item, "userMessage", "UserMessage") || "";
}

function getLogMessage(item) {
  return getLogValue(item, "message", "Message") || "";
}

function getLogDuration(item) {
  return Number(getLogValue(item, "durationMs", "DurationMs") || 0);
}

function getLogStartedAt(item) {
  return getLogValue(item, "startedAt", "StartedAt") || "";
}

function isLogSuccess(item) {
  return Boolean(getLogValue(item, "success", "Success"));
}

function isLogRecovered(item) {
  return Boolean(getLogValue(item, "recovered", "Recovered"));
}

function formatDateTime(value) {
  return formatChinaDateTime(value);
}

function formatDuration(ms) {
  if (!Number.isFinite(ms) || ms <= 0) return "-";
  if (ms < 1000) return `${Math.round(ms)} ms`;
  return `${(ms / 1000).toFixed(2)} s`;
}

function getLogStatusText(item) {
  if (isLogSuccess(item)) return "成功";
  if (isLogRecovered(item)) return "已补救";
  return "失败";
}

function getLogStatusClass(item) {
  return {
    success: isLogSuccess(item),
    recovered: !isLogSuccess(item) && isLogRecovered(item),
    failed: !isLogSuccess(item) && !isLogRecovered(item)
  };
}

function formatJsonBlock(value) {
  if (!value) return "暂无数据";
  if (typeof value !== "string") return JSON.stringify(value, null, 2);

  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function getEvalValue(item, camelKey, pascalKey) {
  return item?.[camelKey] ?? item?.[pascalKey];
}

function getEvaluationRunId(item) {
  return getEvalValue(item, "id", "Id") || getEvalValue(item, "runId", "RunId");
}

function getEvaluationCaseId(item) {
  return getEvalValue(item, "id", "Id") || getEvalValue(item, "caseId", "CaseId");
}

function getEvaluationCaseName(item) {
  return getEvalValue(item, "caseName", "CaseName") || "未命名用例";
}

function getEvaluationCaseEnabled(item) {
  return Boolean(getEvalValue(item, "isEnabled", "IsEnabled"));
}

function getEvaluationCaseField(item, camelKey, pascalKey, fallback = "") {
  return getEvalValue(item, camelKey, pascalKey) ?? fallback;
}

function createEvaluationCaseForm() {
  return {
    caseName: "",
    userMessage: "",
    sessionId: "",
    expectedActionsText: "",
    expectSuccess: true,
    expectedAnswerContainsText: "",
    expectRequiresConfirmation: false,
    expectedAnswerSummary: "",
    enableSemanticJudge: true,
    semanticJudgeThreshold: 0.7,
    category: "基础回归",
    remark: ""
  };
}

function textToList(value) {
  return String(value || "")
    .split(/[\n,，]/)
    .map((item) => item.trim())
    .filter(Boolean);
}

function jsonListToText(value) {
  if (!value) return "";
  if (Array.isArray(value)) return value.join("\n");
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed.join("\n") : String(value);
  } catch {
    return String(value);
  }
}

function openNewEvaluationCaseForm() {
  evaluationEditingCaseId.value = 0;
  evaluationCaseForm.value = createEvaluationCaseForm();
  evaluationCaseFormOpen.value = true;
}

function resetEvaluationCaseForm() {
  evaluationEditingCaseId.value = 0;
  evaluationCaseForm.value = createEvaluationCaseForm();
  evaluationCaseFormOpen.value = false;
}

function editEvaluationCase(item) {
  evaluationEditingCaseId.value = getEvaluationCaseId(item);
  evaluationCaseForm.value = {
    caseName: getEvaluationCaseField(item, "caseName", "CaseName"),
    userMessage: getEvaluationCaseField(item, "userMessage", "UserMessage"),
    sessionId: getEvaluationCaseField(item, "sessionId", "SessionId"),
    expectedActionsText: jsonListToText(getEvaluationCaseField(item, "expectedActionsJson", "ExpectedActionsJson")),
    expectSuccess: Boolean(getEvaluationCaseField(item, "expectedSuccess", "ExpectedSuccess", true)),
    expectedAnswerContainsText: jsonListToText(getEvaluationCaseField(item, "expectedAnswerContainsJson", "ExpectedAnswerContainsJson")),
    expectRequiresConfirmation: Boolean(getEvaluationCaseField(item, "expectedRequiresConfirmation", "ExpectedRequiresConfirmation", false)),
    expectedAnswerSummary: getEvaluationCaseField(item, "expectedAnswerSummary", "ExpectedAnswerSummary"),
    enableSemanticJudge: Boolean(getEvaluationCaseField(item, "enableSemanticJudge", "EnableSemanticJudge", false)),
    semanticJudgeThreshold: Number(getEvaluationCaseField(item, "semanticJudgeThreshold", "SemanticJudgeThreshold", 0.7)),
    category: getEvaluationCaseField(item, "category", "Category"),
    remark: getEvaluationCaseField(item, "remark", "Remark")
  };
  evaluationCaseFormOpen.value = true;
}

function buildEvaluationCasePayload() {
  const form = evaluationCaseForm.value;
  return {
    id: evaluationEditingCaseId.value,
    caseName: form.caseName.trim(),
    userMessage: form.userMessage.trim(),
    sessionId: form.sessionId.trim() || null,
    expectedActions: textToList(form.expectedActionsText),
    expectedSuccess: Boolean(form.expectSuccess),
    expectedAnswerContains: textToList(form.expectedAnswerContainsText),
    expectedRequiresConfirmation: Boolean(form.expectRequiresConfirmation),
    expectedAnswerSummary: form.expectedAnswerSummary.trim(),
    enableSemanticJudge: Boolean(form.enableSemanticJudge),
    semanticJudgeThreshold: Number(form.semanticJudgeThreshold) || 0.7,
    category: form.category.trim(),
    remark: form.remark.trim()
  };
}

async function saveEvaluationCase() {
  if (evaluationCaseSaving.value) return;
  const payload = buildEvaluationCasePayload();
  if (!payload.caseName || !payload.userMessage) {
    window.alert("用例名称和用户消息不能为空。");
    return;
  }

  evaluationCaseSaving.value = true;
  try {
    const response = evaluationEditingCaseId.value
      ? await updateAgentEvaluationTestCaseApi(payload)
      : await createAgentEvaluationTestCaseApi(payload);
    showSuccess(response.message || (evaluationEditingCaseId.value ? "测试用例已更新" : "测试用例已添加"));
    resetEvaluationCaseForm();
    await loadEvaluationTestCases();
  } finally {
    evaluationCaseSaving.value = false;
  }
}

async function toggleEvaluationCaseStatus(item) {
  const caseId = getEvaluationCaseId(item);
  if (!caseId || evaluationCaseSaving.value) return;

  evaluationCaseSaving.value = true;
  try {
    const response = await toggleAgentEvaluationTestCaseStatusApi(caseId);
    showSuccess(response.message || "测试用例状态已更新");
    selectedEvaluationCaseIds.value = selectedEvaluationCaseIds.value.filter((id) => id !== caseId);
    await loadEvaluationTestCases();
  } finally {
    evaluationCaseSaving.value = false;
  }
}

async function deleteEvaluationCase(item) {
  const caseId = getEvaluationCaseId(item);
  if (!caseId || evaluationCaseSaving.value) return;
  if (!window.confirm(`确定删除测试用例“${getEvaluationCaseName(item)}”吗？`)) return;

  evaluationCaseSaving.value = true;
  try {
    const response = await deleteAgentEvaluationTestCaseApi(caseId);
    showSuccess(response.message || "测试用例已删除");
    selectedEvaluationCaseIds.value = selectedEvaluationCaseIds.value.filter((id) => id !== caseId);
    if (evaluationEditingCaseId.value === caseId) resetEvaluationCaseForm();
    await loadEvaluationTestCases();
  } finally {
    evaluationCaseSaving.value = false;
  }
}

function getEvaluationRunTime(item) {
  return getEvalValue(item, "finishedAt", "FinishedAt") || getEvalValue(item, "startedAt", "StartedAt") || "";
}

function getEvaluationPassedCount(item) {
  return Number(getEvalValue(item, "passedCount", "PassedCount") || 0);
}

function getEvaluationFailedCount(item) {
  return Number(getEvalValue(item, "failedCount", "FailedCount") || 0);
}

function getEvaluationTotalCount(item) {
  return Number(getEvalValue(item, "totalCount", "TotalCount") || 0);
}

function getEvaluationRunVersion(item, camelKey, pascalKey) {
  return getEvalValue(item, camelKey, pascalKey) || "未指定";
}

function getEvaluationRunSummary(run) {
  return `${getEvaluationPassedCount(run)}/${getEvaluationTotalCount(run)} 通过，${getEvaluationFailedCount(run)} 失败`;
}

function getEvaluationRunLabel(item) {
  return `#${getEvaluationRunId(item)} · ${getEvaluationRunSummary(item)} · ${formatDateTime(getEvaluationRunTime(item))}`;
}

function getEvaluationVersionItems(item) {
  if (!item) return [];
  return [
    { label: "计划器 Prompt", value: getEvaluationRunVersion(item, "plannerPromptVersion", "PlannerPromptVersion") },
    { label: "动作注册表", value: getEvaluationRunVersion(item, "actionRegistryVersion", "ActionRegistryVersion") },
    { label: "评估配置", value: getEvaluationRunVersion(item, "evaluationVersion", "EvaluationVersion") },
    { label: "最终回答 Prompt", value: getEvaluationRunVersion(item, "finalAnswerPromptVersion", "FinalAnswerPromptVersion") }
  ];
}

function getEvaluationResultCaseName(item) {
  return getEvalValue(item, "caseName", "CaseName") || "未命名结果";
}

function getEvaluationResultCaseId(item) {
  return getEvalValue(item, "testCaseId", "TestCaseId") || getEvalValue(item, "caseId", "CaseId");
}

function isEvaluationResultPassed(item) {
  return Boolean(getEvalValue(item, "passed", "Passed"));
}

function getEvaluationResultAnswer(item) {
  return getEvalValue(item, "answer", "Answer") || "暂无回答";
}

function getEvaluationResultErrors(item) {
  return formatJsonBlock(getEvalValue(item, "errorsJson", "ErrorsJson"));
}

function getEvaluationResultActions(item) {
  return formatJsonBlock(getEvalValue(item, "actualActionsJson", "ActualActionsJson"));
}

function getEvaluationSemanticScore(item) {
  const value = getEvalValue(item, "semanticScore", "SemanticScore");
  return value === null || value === undefined ? "-" : Number(value).toFixed(2);
}

function getEvaluationSemanticReason(item) {
  return getEvalValue(item, "semanticJudgeReason", "SemanticJudgeReason") || "暂无语义评估说明";
}

function getEvaluationFailureTypeRaw(item) {
  return getEvalValue(item, "failureType", "FailureType") ?? 0;
}

function getEvaluationFailureTypeKey(item) {
  const value = getEvaluationFailureTypeRaw(item);
  if (typeof value === "string") return value;
  const map = { 0: "None", 1: "RunTimeError", 2: "PlanActionMissing", 3: "SuccessMismatch", 4: "ConfirmationMismatch", 5: "KeywordMismatch", 6: "SemanticMismatch", 7: "ResultFormatError", 99: "Unknown" };
  return map[Number(value)] || "Unknown";
}

function getEvaluationFailureTypeLabel(itemOrGroup) {
  const key = itemOrGroup?.key || getEvaluationFailureTypeKey(itemOrGroup);
  const labels = { None: "无错误", RunTimeError: "运行异常", PlanActionMissing: "缺少 Action", SuccessMismatch: "成功状态不符", ConfirmationMismatch: "确认状态不符", KeywordMismatch: "关键词不符", SemanticMismatch: "语义不符", ResultFormatError: "结果格式错误", Unknown: "未知错误" };
  return labels[key] || "未知错误";
}

function getEvaluationFailureTypeClass(itemOrGroup) {
  const key = itemOrGroup?.key || getEvaluationFailureTypeKey(itemOrGroup);
  return `type-${String(key).replace(/([a-z])([A-Z])/g, "$1-$2").toLowerCase()}`;
}

function getEvaluationFailureAnalysis(item) {
  const key = getEvaluationFailureTypeKey(item);
  const fallback = { reason: "当前失败不属于常见固定类型，需要结合错误信息和执行日志进一步判断。", suggestion: "先查看错误信息、实际 Actions 和对应工作流日志，确认是 Planner、执行器、最终回答还是评估器的问题。" };
  const map = {
    RunTimeError: { reason: "评估执行过程中抛出了异常。", suggestion: "优先查看后端控制台异常和工作流日志。" },
    PlanActionMissing: { reason: "实际计划没有包含期望 Action。", suggestion: "检查 Planner Prompt、Action 描述和测试用例期望动作。" },
    SuccessMismatch: { reason: "Agent 返回的 Success 与期望不一致。", suggestion: "确认该场景应视为业务失败、安全拒绝还是友好补救成功。" },
    ConfirmationMismatch: { reason: "是否需要确认的判断不符合预期。", suggestion: "检查 Action 风险等级配置和确认分支。" },
    KeywordMismatch: { reason: "最终回答缺少配置的关键词。", suggestion: "检查关键词是否过窄，或最终回答 Prompt 是否遗漏必要信息。" },
    SemanticMismatch: { reason: "语义评估认为回答没有覆盖预期要点。", suggestion: "对比语义摘要、最终回答和评估理由。" },
    ResultFormatError: { reason: "评估流程没有拿到预期结果 DTO。", suggestion: "检查 ApiResponse.Data 类型以及字段大小写兼容。" },
    Unknown: fallback
  };
  return map[key] || fallback;
}

function ensureEvaluationCompareSelection(preferredTargetRun = selectedEvaluationRun.value) {
  const runs = evaluationRuns.value;
  if (runs.length < 2) return;
  const targetId = String(getEvaluationRunId(preferredTargetRun) || getEvaluationRunId(runs[0]) || "");
  const baseRun = runs.find((item) => String(getEvaluationRunId(item)) !== targetId) || runs[1];
  if (!evaluationCompareTargetRunId.value) evaluationCompareTargetRunId.value = targetId;
  if (!evaluationCompareBaseRunId.value || evaluationCompareBaseRunId.value === evaluationCompareTargetRunId.value) evaluationCompareBaseRunId.value = String(getEvaluationRunId(baseRun) || "");
}

function getEvaluationCompareCount(key) {
  return Number(getEvalValue(evaluationCompareResult.value, key, key.charAt(0).toUpperCase() + key.slice(1)) || 0);
}

function getEvaluationCompareCases() {
  const cases = getEvalValue(evaluationCompareResult.value, "cases", "Cases");
  return Array.isArray(cases) ? cases : [];
}

function getEvaluationCompareCaseValue(item, camelKey, pascalKey) {
  return getEvalValue(item, camelKey, pascalKey);
}

function getEvaluationCompareChangeType(item) {
  return getEvaluationCompareCaseValue(item, "changeType", "ChangeType") || "Unknown";
}

function getEvaluationCompareChangeLabel(itemOrType) {
  const type = typeof itemOrType === "string" ? itemOrType : getEvaluationCompareChangeType(itemOrType);
  const labels = { Fixed: "已修复", Regressed: "退化", StillPassed: "持续通过", StillFailed: "持续失败", NewCase: "新增用例", MissingCase: "缺失用例", Unknown: "未知变化" };
  return labels[type] || "未知变化";
}

function getEvaluationCompareChangeClass(itemOrType) {
  const type = typeof itemOrType === "string" ? itemOrType : getEvaluationCompareChangeType(itemOrType);
  return `change-${String(type).replace(/([a-z])([A-Z])/g, "$1-$2").toLowerCase()}`;
}

function formatNullablePass(value) {
  if (value === null || value === undefined) return "无记录";
  return value ? "通过" : "失败";
}

function formatNullableScore(value) {
  if (value === null || value === undefined || value === "") return "-";
  const numberValue = Number(value);
  return Number.isFinite(numberValue) ? numberValue.toFixed(2) : "-";
}

function getEvaluationCompareScoreDelta(item) {
  const baseScore = getEvaluationCompareCaseValue(item, "baseSemanticScore", "BaseSemanticScore");
  const targetScore = getEvaluationCompareCaseValue(item, "targetSemanticScore", "TargetSemanticScore");
  if (baseScore === null || baseScore === undefined || targetScore === null || targetScore === undefined) return "-";
  const delta = Number(targetScore) - Number(baseScore);
  if (!Number.isFinite(delta)) return "-";
  return `${delta >= 0 ? "+" : ""}${delta.toFixed(2)}`;
}

function getEvaluationCompareActions(item, side) {
  const key = side === "base" ? "baseActionsJson" : "targetActionsJson";
  const pascalKey = side === "base" ? "BaseActionsJson" : "TargetActionsJson";
  return formatJsonBlock(getEvaluationCompareCaseValue(item, key, pascalKey));
}
function getEvaluationRegressionValue(camelKey, pascalKey, fallback = "") {
  return getEvalValue(evaluationRegressionSummary.value, camelKey, pascalKey) ?? fallback;
}

function getEvaluationRegressionDecision() {
  return String(getEvaluationRegressionValue("decision", "Decision", "Warning"));
}

function getEvaluationRegressionDecisionLabel() {
  const labels = {
    Pass: "可接受",
    Warning: "需关注",
    Blocked: "阻断"
  };
  return labels[getEvaluationRegressionDecision()] || "需关注";
}

function getEvaluationRegressionDecisionClass() {
  return `decision-${getEvaluationRegressionDecision().replace(/([a-z])([A-Z])/g, "$1-$2").toLowerCase()}`;
}

function getEvaluationRegressionList(camelKey, pascalKey) {
  const value = getEvaluationRegressionValue(camelKey, pascalKey, []);
  return Array.isArray(value) ? value : [];
}

async function fetchEvaluationReportContent(runId) {
  const response = await getAgentEvaluationReportApi(runId);
  const report = response.data?.markdown ?? response.data?.Markdown ?? "";
  const fileName = response.data?.fileName ?? response.data?.FileName ?? `agent-evaluation-report-run-${runId}.md`;
  if (!report.trim()) throw new Error("Empty evaluation report");
  return { report, fileName };
}

async function fetchEvaluationReportSnapshotContent(runId) {
  const response = await getAgentEvaluationReportSnapshotApi(runId);
  const snapshot = response.data || {};
  const report = snapshot.markdownContent ?? snapshot.MarkdownContent ?? "";
  const fileName = snapshot.fileName ?? snapshot.FileName ?? `agent-evaluation-report-run-${runId}-snapshot.md`;
  if (!report.trim()) throw new Error("Empty evaluation report snapshot");
  return { report, fileName };
}

async function saveEvaluationReportSnapshot() {
  const runId = getEvaluationRunId(selectedEvaluationRun.value);
  if (!runId || evaluationSnapshotLoading.value) return;
  evaluationSnapshotLoading.value = true;
  try {
    const response = await saveAgentEvaluationReportSnapshotApi(runId);
    showSuccess(response.message || "评估报告快照已保存");
  } catch {
    window.alert("评估报告快照保存失败，请稍后重试。");
  } finally {
    evaluationSnapshotLoading.value = false;
  }
}

async function previewEvaluationReportSnapshot() {
  const runId = getEvaluationRunId(selectedEvaluationRun.value);
  if (!runId || evaluationSnapshotLoading.value) return;
  evaluationSnapshotLoading.value = true;
  try {
    const { report, fileName } = await fetchEvaluationReportSnapshotContent(runId);
    evaluationReportPreview.value = { fileName, markdown: report };
    evaluationReportPreviewOpen.value = true;
  } catch {
    window.alert("评估报告快照读取失败，请先保存快照或稍后重试。");
  } finally {
    evaluationSnapshotLoading.value = false;
  }
}

async function copyEvaluationReport() {
  const runId = getEvaluationRunId(selectedEvaluationRun.value);
  if (!runId || !evaluationResults.value.length || evaluationReportLoading.value) return;
  evaluationReportLoading.value = true;
  try {
    const { report, fileName } = await fetchEvaluationReportContent(runId);
    await navigator.clipboard.writeText(report);
    showSuccess(`${fileName} 已复制`);
  } catch {
    window.alert("评估报告复制失败，请稍后重试。");
  } finally {
    evaluationReportLoading.value = false;
  }
}

async function previewEvaluationReport() {
  const runId = getEvaluationRunId(selectedEvaluationRun.value);
  if (!runId || !evaluationResults.value.length || evaluationReportLoading.value) return;
  evaluationReportLoading.value = true;
  try {
    const { report, fileName } = await fetchEvaluationReportContent(runId);
    evaluationReportPreview.value = { fileName, markdown: report };
    evaluationReportPreviewOpen.value = true;
  } catch {
    window.alert("评估报告预览失败，请稍后重试。");
  } finally {
    evaluationReportLoading.value = false;
  }
}

function closeEvaluationReportPreview() {
  evaluationReportPreviewOpen.value = false;
}

async function copyEvaluationPreviewContent() {
  const markdown = evaluationReportPreview.value.markdown || "";
  if (!markdown.trim()) return;
  try {
    await navigator.clipboard.writeText(markdown);
    showSuccess(`${evaluationReportPreview.value.fileName || "评估报告"} 已复制`);
  } catch {
    window.alert("当前预览内容复制失败，请稍后重试。");
  }
}

function downloadEvaluationPreviewContent() {
  const markdown = evaluationReportPreview.value.markdown || "";
  if (!markdown.trim()) return;
  const fileName = evaluationReportPreview.value.fileName || "agent-evaluation-report.md";
  const blob = new Blob([markdown], { type: "text/markdown;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
  showSuccess(`${fileName} 已开始下载`);
}

function getDownloadFileName(response, fallback) {
  const disposition = response?.headers?.["content-disposition"] || response?.headers?.["Content-Disposition"] || "";
  const utf8Match = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) return decodeURIComponent(utf8Match[1]);
  const normalMatch = disposition.match(/filename="?([^";]+)"?/i);
  if (normalMatch?.[1]) return normalMatch[1];
  return fallback;
}

async function downloadEvaluationReport() {
  const runId = getEvaluationRunId(selectedEvaluationRun.value);
  if (!runId || !evaluationResults.value.length || evaluationReportLoading.value) return;
  evaluationReportLoading.value = true;
  try {
    const response = await downloadAgentEvaluationReportApi(runId);
    const blob = response.data instanceof Blob ? response.data : new Blob([response.data], { type: "text/markdown;charset=utf-8" });
    const fileName = getDownloadFileName(response, `agent-evaluation-report-run-${runId}.md`);
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    showSuccess(`${fileName} 已开始下载`);
  } catch {
    window.alert("评估报告下载失败，请稍后重试。");
  } finally {
    evaluationReportLoading.value = false;
  }
}

async function loadEvaluationRuns() {
  evaluationRunLoading.value = true;
  try {
    const response = await getRecentAgentEvaluationRunsApi(10);
    evaluationRuns.value = Array.isArray(response.data) ? response.data : [];
    ensureEvaluationCompareSelection();
    if (evaluationRuns.value.length && !selectedEvaluationRun.value) await selectEvaluationRun(evaluationRuns.value[0]);
  } catch {
    evaluationRuns.value = [];
  } finally {
    evaluationRunLoading.value = false;
  }
}

async function loadEvaluationTestCases() {
  evaluationCaseLoading.value = true;
  try {
    const response = await getAgentEvaluationTestCasesApi(evaluationCaseStatus.value);
    evaluationTestCases.value = Array.isArray(response.data) ? response.data : [];
    const availableIds = new Set(evaluationTestCases.value.map((item) => getEvaluationCaseId(item)));
    selectedEvaluationCaseIds.value = selectedEvaluationCaseIds.value.filter((id) => availableIds.has(id));
  } catch {
    evaluationTestCases.value = [];
  } finally {
    evaluationCaseLoading.value = false;
  }
}

async function selectEvaluationRun(item) {
  const runId = getEvaluationRunId(item);
  if (!runId || evaluationResultLoading.value) return;
  selectedEvaluationRun.value = item;
  evaluationCompareTargetRunId.value = String(runId);
  ensureEvaluationCompareSelection(item);
  evaluationResultLoading.value = true;
  try {
    const response = await getAgentEvaluationRunResultsApi(runId);
    evaluationResults.value = Array.isArray(response.data) ? response.data : [];
  } catch {
    evaluationResults.value = [];
  } finally {
    evaluationResultLoading.value = false;
  }
}

async function rerunEvaluationFromSnapshot() {
  const runId = getEvaluationRunId(selectedEvaluationRun.value);
  if (!runId || evaluationRunning.value) return;
  if (!window.confirm(`确定基于评估批次 #${runId} 的快照重新执行评估吗？`)) return;

  evaluationRunning.value = true;
  evaluationActiveTab.value = "runs";
  try {
    const response = await rerunAgentEvaluationRunApi(runId);
    showSuccess(response.message || "快照重跑完成");
    selectedEvaluationRun.value = null;
    evaluationResults.value = [];
    evaluationCompareResult.value = null;
    evaluationRegressionSummary.value = null;
    await loadEvaluationRuns();
  } finally {
    evaluationRunning.value = false;
  }
}

async function openEvaluationWorkflowLog(item) {
  const runId = getEvaluationRunId(selectedEvaluationRun.value);
  const caseId = getEvaluationResultCaseId(item);
  if (!runId || !caseId || workflowLogDetailLoading.value) return;
  evaluationPanelOpen.value = false;
  logPanelOpen.value = true;
  selectedWorkflowLog.value = null;
  workflowLogDetailLoading.value = true;
  try {
    const response = await getAgentEvaluationWorkflowLogApi(runId, caseId);
    selectedWorkflowLog.value = response.data || null;
    workflowLogs.value = selectedWorkflowLog.value ? [selectedWorkflowLog.value] : [];
  } catch {
    selectedWorkflowLog.value = null;
    workflowLogs.value = [];
  } finally {
    workflowLogDetailLoading.value = false;
  }
}

async function openEvaluationPanel() {
  evaluationPanelOpen.value = true;
  selectedEvaluationRun.value = null;
  evaluationResults.value = [];
  evaluationCompareResult.value = null;
  evaluationRegressionSummary.value = null;
  await Promise.all([loadEvaluationTestCases(), loadEvaluationRuns()]);
}

function closeEvaluationPanel() {
  evaluationPanelOpen.value = false;
}

async function refreshEvaluationPanel() {
  selectedEvaluationRun.value = null;
  evaluationResults.value = [];
  evaluationCompareResult.value = null;
  evaluationRegressionSummary.value = null;
  await Promise.all([loadEvaluationTestCases(), loadEvaluationRuns()]);
}

async function changeEvaluationCaseStatus(status) {
  if (evaluationCaseStatus.value === status) return;
  evaluationCaseStatus.value = status;
  await loadEvaluationTestCases();
}

function toggleEvaluationCase(caseId) {
  if (!caseId) return;
  const existing = selectedEvaluationCaseIds.value.includes(caseId);
  selectedEvaluationCaseIds.value = existing ? selectedEvaluationCaseIds.value.filter((id) => id !== caseId) : [...selectedEvaluationCaseIds.value, caseId];
}

async function runEvaluation(caseIds = []) {
  if (evaluationRunning.value) return;
  evaluationActiveTab.value = "runs";
  evaluationRunning.value = true;
  evaluationCompareResult.value = null;
  evaluationRegressionSummary.value = null;
  try {
    const response = await runAgentEvaluationApi(caseIds);
    const batch = response.data || {};
    evaluationResults.value = Array.isArray(batch.results) ? batch.results : [];
    showSuccess(response.message || "评估执行完成");
    selectedEvaluationRun.value = null;
    await loadEvaluationRuns();
  } finally {
    evaluationRunning.value = false;
  }
}

async function compareEvaluationRuns() {
  if (!canCompareEvaluationRuns.value || evaluationCompareLoading.value) return;
  evaluationCompareLoading.value = true;
  evaluationRegressionSummary.value = null;
  try {
    const [compareResponse, summaryResponse] = await Promise.all([
      compareAgentEvaluationRunsApi(evaluationCompareBaseRunId.value, evaluationCompareTargetRunId.value),
      getAgentEvaluationRegressionSummaryApi(evaluationCompareBaseRunId.value, evaluationCompareTargetRunId.value)
    ]);
    evaluationCompareResult.value = compareResponse.data || null;
    evaluationRegressionSummary.value = summaryResponse.data || null;
    showSuccess(summaryResponse.message || compareResponse.message || "评估批次对比完成");
  } finally {
    evaluationCompareLoading.value = false;
  }
}
function getDetailField(camelKey, pascalKey) {
  return selectedWorkflowLog.value?.[camelKey] ?? selectedWorkflowLog.value?.[pascalKey] ?? "";
}

async function loadRecentWorkflowLogs() {
  workflowLogLoading.value = true;

  try {
    const response = await getRecentAgentWorkflowLogsApi(20);
    workflowLogs.value = Array.isArray(response.data) ? response.data : [];

    if (workflowLogs.value.length && !selectedWorkflowLog.value) {
      await selectWorkflowLog(workflowLogs.value[0]);
    }
  } catch {
    workflowLogs.value = [];
  } finally {
    workflowLogLoading.value = false;
  }
}

async function selectWorkflowLog(item) {
  const id = getLogId(item);
  if (!id || workflowLogDetailLoading.value) return;

  workflowLogDetailLoading.value = true;

  try {
    const response = await getAgentWorkflowLogDetailApi(id);
    selectedWorkflowLog.value = response.data || null;
  } catch {
    selectedWorkflowLog.value = null;
  } finally {
    workflowLogDetailLoading.value = false;
  }
}

async function openWorkflowLogPanel() {
  logPanelOpen.value = true;
  selectedWorkflowLog.value = null;
  await loadRecentWorkflowLogs();
}

function closeWorkflowLogPanel() {
  logPanelOpen.value = false;
}

async function refreshWorkflowLogs() {
  selectedWorkflowLog.value = null;
  await loadRecentWorkflowLogs();
}
async function sendMessage() {
  const text = input.value.trim();
  if (conversationView.value !== "active" || !text || loading.value) return;

  const userId = getCurrentUserId();
  if (!userId) {
    messages.value.push({
      role: "assistant",
      content: "无法从登录令牌中解析用户身份，请重新登录后再试。"
    });
    return;
  }

  messages.value.push({ role: "user", content: text });
  input.value = "";
  adjustInputHeight();
  loading.value = true;

  try {
    const response = await askAgentApi({
      content: text,
      sessionId: sessionId.value,
      userId
    });
    messages.value.push(createAssistantMessageFromResponse(response));
    await syncLatestConversation();
  } catch (err) {
    messages.value.push({
      role: "assistant",
      content: err?.payload?.message || err?.message || "Agent 调用失败，请稍后再试。"
    });
  } finally {
    loading.value = false;
  }
}

watch(messages, scrollDialogToBottom, { deep: true });
watch(input, adjustInputHeight);

onMounted(() => {
  document.addEventListener("click", closeConversationMenu);
  document.addEventListener("keydown", handleDocumentKeydown);
  if (authStore.token && !authStore.profile) {
    authStore.fetchProfile();
  }
  loadConversations();
  scrollDialogToBottom();
  adjustInputHeight();
});

onBeforeUnmount(() => {
  document.removeEventListener("click", closeConversationMenu);
  document.removeEventListener("keydown", handleDocumentKeydown);
});
</script>

<template>
  <section class="agent-page">
    <main class="agent-main">
      <section class="agent-chat-panel">
        <div ref="dialogRef" class="agent-dialog">
          <article
            v-for="(message, index) in messages"
            :key="index"
            class="agent-bubble-row"
            :class="message.role"
          >
            <img
              v-if="message.role === 'assistant'"
              class="agent-avatar"
              :src="agentAvatar"
              alt="Sharky Agent"
            />
            <div class="agent-bubble">
              <div
                v-if="message.role === 'assistant'"
                class="markdown-body agent-markdown"
                v-html="renderAssistantMarkdown(message.content)"
              ></div>
              <div
                v-else
                class="agent-user-message"
                :class="{
                  collapsed: shouldCollapseUserMessage(message) && !isUserMessageExpanded(index)
                }"
              >
                <pre>{{ message.content }}</pre>
              </div>
              <button
                v-if="shouldCollapseUserMessage(message)"
                type="button"
                class="agent-message-toggle"
                :aria-label="isUserMessageExpanded(index) ? '收起用户消息' : '展开用户消息'"
                :title="isUserMessageExpanded(index) ? '收起' : '展开全文'"
                @click.stop="toggleUserMessage(index)"
              >
                <CircleArrowUp v-if="isUserMessageExpanded(index)" :size="24" />
                <CircleArrowDown v-else :size="24" />
              </button>
              <div
                v-if="message.confirmationStatus && message.confirmationStatus !== 'none'"
                class="agent-confirm-box"
              >
                <p v-if="message.confirmationSummary" class="agent-confirm-summary">
                  {{ message.confirmationSummary }}
                </p>
                <div v-if="message.confirmationStatus === 'pending'" class="agent-confirm-actions">
                  <button
                    type="button"
                    class="agent-confirm-btn primary"
                    :disabled="loading || confirmationLoadingId === message.confirmationId"
                    @click.stop="confirmAgentPlan(message)"
                  >
                    确认执行
                  </button>
                  <button
                    type="button"
                    class="agent-confirm-btn ghost"
                    :disabled="loading || confirmationLoadingId === message.confirmationId"
                    @click.stop="cancelAgentConfirmation(message)"
                  >
                    取消
                  </button>
                </div>
                <p v-else-if="message.confirmationStatus === 'confirmed'" class="agent-confirm-status">
                  已确认，正在执行结果已返回。
                </p>
                <p v-else-if="message.confirmationStatus === 'cancelled'" class="agent-confirm-status">
                  已取消该操作。
                </p>
                <p v-else class="agent-confirm-status warning">
                  确认请求已失效，请重新发起任务。
                </p>
              </div>
            </div>
            <img v-if="message.role === 'user'" class="agent-avatar user-avatar" :src="userAvatar" alt="user" />
          </article>

          <p v-if="messageLoading" class="agent-thinking">正在加载会话消息...</p>
          <p v-if="loading" class="agent-thinking">Sharky Agent 正在思考...</p>
        </div>

        <form class="agent-compose" @submit.prevent="sendMessage">
          <div class="agent-input-box">
            <textarea
              ref="inputRef"
              v-model="input"
              rows="1"
              :disabled="conversationView === 'archived'"
              :placeholder="conversationView === 'archived'
                ? '归档会话仅供查看，恢复后可继续对话'
                : '输入你的问题，或告诉我你想写的内容...'"
              @input="adjustInputHeight"
              @keydown.ctrl.enter.prevent="sendMessage"
            />
          </div>
          <div class="agent-compose-actions">
            <div class="agent-note-actions">
              <button v-if="isAdmin" type="button" @click="openEvaluationPanel">
                <ClipboardCheck :size="15" />
                评估中心
              </button>
              <button type="button" @click="openWorkflowLogPanel">
                <Bug :size="15" />
                执行日志
              </button>
              <button type="button" @click="newConversation">清空对话</button>
            </div>
            <button class="agent-send" :disabled="!canSend" aria-label="发送消息" title="发送">
              <CircleArrowUp :size="26" />
            </button>
          </div>
        </form>
      </section>
    </main>

    <aside class="agent-side">
      <section class="agent-history-panel">
        <button type="button" class="agent-new-chat" @click="newConversation">
          <Plus :size="16" />
          开启新对话
        </button>

        <div class="agent-history-tabs" aria-label="会话状态筛选">
          <button
            type="button"
            :class="{ active: conversationView === 'active' }"
            @click="setConversationView('active')"
          >
            活跃
          </button>
          <button
            type="button"
            :class="{ active: conversationView === 'archived' }"
            @click="setConversationView('archived')"
          >
            已归档
          </button>
        </div>

        <p v-if="historyLoading" class="agent-history-empty">正在加载会话...</p>
        <p v-else-if="!groupedConversations.length" class="agent-history-empty">暂无历史会话</p>

        <div v-for="group in groupedConversations" :key="group.group" class="agent-history-group">
          <h3>{{ group.group }}</h3>
          <div
            v-for="(item, index) in group.items"
            :key="getConversationSessionId(item) || index"
            class="agent-history-item"
            :class="{ active: getConversationSessionId(item) === sessionId }"
          >
            <button
              type="button"
              class="agent-history-link"
              @click="switchConversation(item)"
            >
              <span>{{ formatConversationTitle(item) }}</span>
            </button>

            <button
              type="button"
              class="agent-history-more"
              :aria-label="`管理会话：${formatConversationTitle(item)}`"
              :aria-expanded="openConversationMenuId === getConversationSessionId(item)"
              @click.stop="toggleConversationMenu(item)"
            >
              <MoreHorizontal :size="17" />
            </button>

            <div
              v-if="openConversationMenuId === getConversationSessionId(item)"
              class="agent-conversation-menu"
              @click.stop
            >
              <button
                v-if="conversationView === 'active'"
                type="button"
                :disabled="conversationActionLoadingId === getConversationSessionId(item)"
                @click="archiveConversation(item)"
              >
                <Archive :size="17" />
                <span>归档</span>
              </button>
              <button
                v-else
                type="button"
                :disabled="conversationActionLoadingId === getConversationSessionId(item)"
                @click="restoreConversation(item)"
              >
                <RotateCcw :size="17" />
                <span>恢复</span>
              </button>
              <button
                type="button"
                class="danger"
                :disabled="conversationActionLoadingId === getConversationSessionId(item)"
                @click="deleteConversation(item)"
              >
                <Trash2 :size="17" />
                <span>删除</span>
              </button>
            </div>
          </div>
        </div>
      </section>
    </aside>

    <div v-if="evaluationPanelOpen" class="agent-log-modal" @click.self="closeEvaluationPanel">
      <section class="agent-log-panel agent-eval-panel" aria-label="Agent 评估中心">
        <header class="agent-log-head">
          <div>
            <h2>Agent 评估中心</h2>
            <p>运行测试用例，查看评估批次、动作命中和语义判断结果。</p>
          </div>
          <div class="agent-log-head-actions">
            <button v-if="evaluationActiveTab === 'cases'" type="button" :disabled="evaluationRunning" @click="runEvaluation([])">
              <Play :size="16" />
              运行全部
            </button>
            <button v-if="evaluationActiveTab === 'cases'" type="button" :disabled="evaluationRunning || !selectedEvaluationCaseIds.length" @click="runEvaluation(selectedEvaluationCaseIds)">
              <Play :size="16" />
              运行选中
            </button>
            <button v-if="evaluationActiveTab === 'runs'" type="button" :disabled="!selectedEvaluationRun || evaluationRunning" @click="rerunEvaluationFromSnapshot">
              <RotateCcw :size="16" />
              {{ evaluationRunning ? '重跑中...' : '快照重跑' }}
            </button>
            <button v-if="evaluationActiveTab === 'runs'" type="button" :disabled="!selectedEvaluationRun || evaluationSnapshotLoading" @click="saveEvaluationReportSnapshot">
              <Save :size="16" />
              {{ evaluationSnapshotLoading ? '处理中...' : '保存快照' }}
            </button>
            <button v-if="evaluationActiveTab === 'runs'" type="button" :disabled="!selectedEvaluationRun || evaluationSnapshotLoading" @click="previewEvaluationReportSnapshot">
              <Eye :size="16" />
              查看快照
            </button>
            <button v-if="evaluationActiveTab === 'runs'" type="button" :disabled="!evaluationResults.length || evaluationReportLoading" @click="previewEvaluationReport">
              <Eye :size="16" />
              预览报告
            </button>
            <button v-if="evaluationActiveTab === 'runs'" type="button" :disabled="!evaluationResults.length || evaluationReportLoading" @click="copyEvaluationReport">
              <ClipboardCheck :size="16" />
              复制报告
            </button>
            <button v-if="evaluationActiveTab === 'runs'" type="button" :disabled="!evaluationResults.length || evaluationReportLoading" @click="downloadEvaluationReport">
              <Download :size="16" />
              下载报告
            </button>
            <button type="button" :disabled="evaluationRunLoading || evaluationCaseLoading" @click="refreshEvaluationPanel">
              <RefreshCw :size="16" />
              刷新
            </button>
            <button type="button" class="icon" aria-label="关闭评估中心" @click="closeEvaluationPanel">
              <X :size="18" />
            </button>
          </div>
        </header>

        <nav class="agent-eval-page-tabs" aria-label="评估中心页面切换">
          <button type="button" :class="{ active: evaluationActiveTab === 'cases' }" @click="evaluationActiveTab = 'cases'">测试用例</button>
          <button type="button" :class="{ active: evaluationActiveTab === 'runs' }" @click="evaluationActiveTab = 'runs'">评估批次</button>
        </nav>

        <div class="agent-eval-body" :class="`tab-${evaluationActiveTab}`">
          <section v-show="evaluationActiveTab === 'cases'" class="agent-eval-cases">
            <div class="agent-eval-section-head">
              <div>
                <h3>测试用例</h3>
                <p>选择用例后可运行选中项，也可以维护用例库。</p>
              </div>
              <div class="agent-eval-head-tools">
                <div class="agent-eval-tabs" aria-label="测试用例状态筛选">
                  <button type="button" :class="{ active: evaluationCaseStatus === 1 }" @click="changeEvaluationCaseStatus(1)">启用</button>
                  <button type="button" :class="{ active: evaluationCaseStatus === 0 }" @click="changeEvaluationCaseStatus(0)">禁用</button>
                  <button type="button" :class="{ active: evaluationCaseStatus === 2 }" @click="changeEvaluationCaseStatus(2)">全部</button>
                </div>
              </div>
            </div>

            <p v-if="evaluationCaseLoading" class="agent-log-empty">正在加载测试用例...</p>
            <div v-else class="agent-eval-case-grid">
              <button type="button" class="agent-eval-case agent-eval-add-card" @click="openNewEvaluationCaseForm">
                <Plus :size="28" />
              </button>
              <p v-if="!evaluationTestCases.length" class="agent-log-empty agent-eval-grid-empty">暂无测试用例</p>
              <article
                v-for="item in evaluationTestCases"
                :key="getEvaluationCaseId(item)"
                class="agent-eval-case"
                :class="{ selected: selectedEvaluationCaseIds.includes(getEvaluationCaseId(item)), disabled: !getEvaluationCaseEnabled(item) }"
              >
                <button type="button" class="agent-eval-case-pick" :disabled="!getEvaluationCaseEnabled(item)" @click="toggleEvaluationCase(getEvaluationCaseId(item))">
                  <span>{{ getEvaluationCaseName(item) }}</span>
                  <small>ID {{ getEvaluationCaseId(item) }} · {{ getEvaluationCaseEnabled(item) ? '启用' : '禁用' }}</small>
                </button>
                <div class="agent-eval-case-actions">
                  <button type="button" class="agent-tooltip-btn" aria-label="编辑" data-tooltip="编辑" @click="editEvaluationCase(item)">
                    <Pencil :size="14" />
                  </button>
                  <button type="button" @click="toggleEvaluationCaseStatus(item)">
                    {{ getEvaluationCaseEnabled(item) ? '禁用' : '启用' }}
                  </button>
                  <button type="button" class="danger agent-tooltip-btn" aria-label="删除" data-tooltip="删除" @click="deleteEvaluationCase(item)">
                    <Trash2 :size="14" />
                  </button>
                </div>
              </article>
            </div>
          </section>

          <div v-show="evaluationActiveTab === 'runs'" class="agent-eval-content">
            <aside class="agent-log-list agent-eval-run-list">
              <p v-if="evaluationRunLoading" class="agent-log-empty">正在加载评估批次...</p>
              <p v-else-if="!evaluationRuns.length" class="agent-log-empty">暂无评估批次</p>
              <button
                v-for="item in evaluationRuns"
                v-else
                :key="getEvaluationRunId(item)"
                type="button"
                class="agent-log-item"
                :class="{ active: getEvaluationRunId(item) === getEvaluationRunId(selectedEvaluationRun) }"
                @click="selectEvaluationRun(item)"
              >
                <span class="agent-log-item-title">评估批次 #{{ getEvaluationRunId(item) }}</span>
                <span class="agent-log-item-meta">
                  <span class="agent-log-status" :class="getEvaluationFailedCount(item) > 0 ? 'failed' : 'success'">
                    {{ getEvaluationFailedCount(item) > 0 ? '有失败' : '通过' }}
                  </span>
                  <span>{{ getEvaluationRunSummary(item) }}</span>
                </span>
                <span class="agent-log-item-time">{{ formatDateTime(getEvaluationRunTime(item)) }}</span>
                <span class="agent-log-item-time agent-eval-run-version">
                  评估版本：{{ getEvaluationRunVersion(item, "evaluationVersion", "EvaluationVersion") }}
                </span>
              </button>
            </aside>

            <main class="agent-log-detail agent-eval-result-detail">
              <p v-if="evaluationRunning" class="agent-log-empty">评估正在执行，可能需要等待 Agent 完成调用...</p>
              <p v-else-if="evaluationResultLoading" class="agent-log-empty">正在加载评估结果...</p>
              <p v-else-if="!evaluationResults.length" class="agent-log-empty">选择批次或运行评估后查看结果</p>
              <div v-else class="agent-eval-result-stack">
                <section class="agent-eval-compare-panel" aria-label="评估批次对比">
                  <div>
                    <strong>批次对比</strong>
                    <p>选择两个评估批次，观察修复、退化和持续失败情况。</p>
                  </div>
                  <div class="agent-eval-compare-controls">
                    <label>
                      <span>基准</span>
                      <select v-model="evaluationCompareBaseRunId">
                        <option value="">选择基准批次</option>
                        <option v-for="run in evaluationRuns" :key="`base-${getEvaluationRunId(run)}`" :value="String(getEvaluationRunId(run))">
                          {{ getEvaluationRunLabel(run) }}
                        </option>
                      </select>
                    </label>
                    <label>
                      <span>目标</span>
                      <select v-model="evaluationCompareTargetRunId">
                        <option value="">选择目标批次</option>
                        <option v-for="run in evaluationRuns" :key="`target-${getEvaluationRunId(run)}`" :value="String(getEvaluationRunId(run))">
                          {{ getEvaluationRunLabel(run) }}
                        </option>
                      </select>
                    </label>
                    <button type="button" :disabled="!canCompareEvaluationRuns || evaluationCompareLoading" @click="compareEvaluationRuns">
                      {{ evaluationCompareLoading ? '对比中...' : '对比批次' }}
                    </button>
                  </div>
                </section>

                <section
                  v-if="evaluationRegressionSummary"
                  class="agent-eval-regression-card"
                  :class="getEvaluationRegressionDecisionClass()"
                  aria-label="评估回归结论"
                >
                  <header>
                    <div>
                      <span>回归结论</span>
                      <h3>{{ getEvaluationRegressionValue("title", "Title", "暂无结论") }}</h3>
                    </div>
                    <strong>{{ getEvaluationRegressionDecisionLabel() }}</strong>
                  </header>
                  <p>{{ getEvaluationRegressionValue("summary", "Summary", "暂无回归结论摘要。") }}</p>
                  <div class="agent-eval-regression-lists">
                    <div v-if="getEvaluationRegressionList('highlights', 'Highlights').length">
                      <h4>正向变化</h4>
                      <ul>
                        <li v-for="item in getEvaluationRegressionList('highlights', 'Highlights')" :key="item">{{ item }}</li>
                      </ul>
                    </div>
                    <div v-if="getEvaluationRegressionList('risks', 'Risks').length">
                      <h4>风险</h4>
                      <ul>
                        <li v-for="item in getEvaluationRegressionList('risks', 'Risks')" :key="item">{{ item }}</li>
                      </ul>
                    </div>
                    <div v-if="getEvaluationRegressionList('nextActions', 'NextActions').length">
                      <h4>下一步</h4>
                      <ul>
                        <li v-for="item in getEvaluationRegressionList('nextActions', 'NextActions')" :key="item">{{ item }}</li>
                      </ul>
                    </div>
                  </div>
                </section>

                <section v-if="evaluationCompareResult" class="agent-eval-compare-summary" aria-label="批次对比结果">
                  <div class="agent-eval-compare-card fixed">
                    <span>已修复</span>
                    <strong>{{ getEvaluationCompareCount('fixedCount') }}</strong>
                  </div>
                  <div class="agent-eval-compare-card regressed">
                    <span>退化</span>
                    <strong>{{ getEvaluationCompareCount('regressedCount') }}</strong>
                  </div>
                  <div class="agent-eval-compare-card passed">
                    <span>持续通过</span>
                    <strong>{{ getEvaluationCompareCount('stillPassedCount') }}</strong>
                  </div>
                  <div class="agent-eval-compare-card failed">
                    <span>持续失败</span>
                    <strong>{{ getEvaluationCompareCount('stillFailedCount') }}</strong>
                  </div>
                  <div class="agent-eval-compare-card neutral">
                    <span>新增/缺失</span>
                    <strong>{{ getEvaluationCompareCount('newCaseCount') }}/{{ getEvaluationCompareCount('missingCaseCount') }}</strong>
                  </div>
                </section>

                <section v-if="evaluationCompareResult" class="agent-eval-compare-cases" aria-label="批次对比明细">
                  <article
                    v-for="item in getEvaluationCompareCases()"
                    :key="getEvaluationCompareCaseValue(item, 'testCaseId', 'TestCaseId')"
                    class="agent-eval-compare-case"
                    :class="getEvaluationCompareChangeClass(item)"
                  >
                    <header>
                      <div>
                        <h4>{{ getEvaluationCompareCaseValue(item, 'caseName', 'CaseName') }}</h4>
                        <p>ID {{ getEvaluationCompareCaseValue(item, 'testCaseId', 'TestCaseId') }}</p>
                      </div>
                      <span>{{ getEvaluationCompareChangeLabel(item) }}</span>
                    </header>
                    <div class="agent-eval-compare-case-grid">
                      <div>
                        <small>基准结果</small>
                        <strong>{{ formatNullablePass(getEvaluationCompareCaseValue(item, 'basePassed', 'BasePassed')) }}</strong>
                        <p>语义分：{{ formatNullableScore(getEvaluationCompareCaseValue(item, 'baseSemanticScore', 'BaseSemanticScore')) }}</p>
                      </div>
                      <div>
                        <small>目标结果</small>
                        <strong>{{ formatNullablePass(getEvaluationCompareCaseValue(item, 'targetPassed', 'TargetPassed')) }}</strong>
                        <p>语义分：{{ formatNullableScore(getEvaluationCompareCaseValue(item, 'targetSemanticScore', 'TargetSemanticScore')) }}</p>
                      </div>
                      <div>
                        <small>分数变化</small>
                        <strong>{{ getEvaluationCompareScoreDelta(item) }}</strong>
                        <p>{{ getEvaluationCompareCaseValue(item, 'targetFailureType', 'TargetFailureType') || '无失败类型' }}</p>
                      </div>
                    </div>
                    <details>
                      <summary>查看 Actions 对比</summary>
                      <div class="agent-eval-compare-actions">
                        <pre>{{ getEvaluationCompareActions(item, 'base') }}</pre>
                        <pre>{{ getEvaluationCompareActions(item, 'target') }}</pre>
                      </div>
                    </details>
                  </article>
                </section>

                <section class="agent-eval-version-strip" aria-label="评估版本信息">
                  <div v-for="version in getEvaluationVersionItems(selectedEvaluationRun)" :key="version.label">
                    <span>{{ version.label }}</span>
                    <strong :title="version.value">{{ version.value }}</strong>
                  </div>
                </section>

                <section class="agent-eval-stats" aria-label="评估结果统计">
                  <div>
                    <span>总数</span>
                    <strong>{{ evaluationResultStats.total }}</strong>
                  </div>
                  <div class="agent-eval-stat-passed">
                    <span>通过</span>
                    <strong>{{ evaluationResultStats.passed }}</strong>
                  </div>
                  <div class="agent-eval-stat-failed">
                    <span>失败</span>
                    <strong>{{ evaluationResultStats.failed }}</strong>
                  </div>
                  <div class="agent-eval-failure-summary">
                    <span>失败类型</span>
                    <p v-if="!evaluationResultStats.failureGroups.length">暂无失败</p>
                    <p v-else>
                      <span
                        v-for="group in evaluationResultStats.failureGroups"
                        :key="group.key"
                        class="agent-eval-failure-chip"
                        :class="getEvaluationFailureTypeClass(group)"
                      >
                        {{ group.label }} × {{ group.count }}
                      </span>
                    </p>
                  </div>
                </section>

                <div class="agent-eval-results">
                  <article
                    v-for="item in evaluationResults"
                    :key="getEvalValue(item, 'id', 'Id') || getEvaluationResultCaseName(item)"
                    class="agent-eval-result-card"
                    :class="{ passed: isEvaluationResultPassed(item), failed: !isEvaluationResultPassed(item) }"
                  >
                    <header>
                      <div>
                        <h3>{{ getEvaluationResultCaseName(item) }}</h3>
                        <p>语义分：{{ getEvaluationSemanticScore(item) }}</p>
                      </div>
                      <div class="agent-eval-result-badges">
                        <span
                          v-if="!isEvaluationResultPassed(item)"
                          class="agent-eval-failure-chip"
                          :class="getEvaluationFailureTypeClass(item)"
                        >
                          {{ getEvaluationFailureTypeLabel(item) }}
                        </span>
                        <button type="button" class="agent-eval-log-button" :disabled="workflowLogDetailLoading" @click="openEvaluationWorkflowLog(item)">
                          查看日志
                        </button>
                        <span class="agent-log-status" :class="isEvaluationResultPassed(item) ? 'success' : 'failed'">
                          {{ isEvaluationResultPassed(item) ? '通过' : '失败' }}
                        </span>
                      </div>
                    </header>

                    <section v-if="!isEvaluationResultPassed(item)" class="agent-eval-failure-analysis">
                      <h4>失败分析</h4>
                      <div>
                        <strong>可能原因</strong>
                        <p>{{ getEvaluationFailureAnalysis(item).reason }}</p>
                      </div>
                      <div>
                        <strong>建议检查</strong>
                        <p>{{ getEvaluationFailureAnalysis(item).suggestion }}</p>
                      </div>
                    </section>

                    <section>
                      <h4>最终回答</h4>
                      <p>{{ getEvaluationResultAnswer(item) }}</p>
                    </section>
                    <section>
                      <h4>实际 Actions</h4>
                      <pre>{{ getEvaluationResultActions(item) }}</pre>
                    </section>
                    <section>
                      <h4>错误信息</h4>
                      <pre>{{ getEvaluationResultErrors(item) }}</pre>
                    </section>
                    <section>
                      <h4>语义判断</h4>
                      <p>{{ getEvaluationSemanticReason(item) }}</p>
                    </section>
                  </article>
                </div>
              </div>
            </main>
          </div>
        </div>
      </section>

      <div v-if="evaluationReportPreviewOpen" class="agent-eval-form-backdrop" @click.self="closeEvaluationReportPreview">
        <section class="agent-eval-report-dialog" aria-label="评估报告预览">
          <header class="agent-eval-case-dialog-head">
            <div>
              <h3>评估报告预览</h3>
              <p>{{ evaluationReportPreview.fileName }}</p>
            </div>
            <div class="agent-eval-report-actions">
              <button type="button" :disabled="evaluationReportLoading" @click="copyEvaluationPreviewContent">
                <ClipboardCheck :size="15" />
                复制
              </button>
              <button type="button" :disabled="evaluationReportLoading" @click="downloadEvaluationPreviewContent">
                <Download :size="15" />
                下载
              </button>
              <button type="button" class="icon" aria-label="关闭报告预览" @click="closeEvaluationReportPreview">
                <X :size="18" />
              </button>
            </div>
          </header>
          <div class="agent-eval-report-preview markdown-body" v-html="renderAssistantMarkdown(evaluationReportPreview.markdown)"></div>
        </section>
      </div>
      <div v-if="evaluationCaseFormOpen" class="agent-eval-form-backdrop" @click.self="resetEvaluationCaseForm">
        <form class="agent-eval-case-dialog" @submit.prevent="saveEvaluationCase">
          <header class="agent-eval-case-dialog-head">
            <div>
              <h3>{{ evaluationEditingCaseId ? '编辑测试用例' : '新增测试用例' }}</h3>
              <p>配置用户输入、期望动作和语义评估规则。</p>
            </div>
            <button type="button" class="icon" aria-label="关闭测试用例表单" @click="resetEvaluationCaseForm">
              <X :size="18" />
            </button>
          </header>

          <div class="agent-eval-case-form">
            <div class="agent-eval-form-grid">
              <label>
                <span>用例名称</span>
                <input v-model="evaluationCaseForm.caseName" type="text" placeholder="Case 7：上下文指代" />
              </label>
              <label>
                <span>SessionId</span>
                <input v-model="evaluationCaseForm.sessionId" type="text" placeholder="留空则后端生成" />
              </label>
              <label>
                <span>分类</span>
                <input v-model="evaluationCaseForm.category" type="text" placeholder="基础回归" />
              </label>
              <label>
                <span>语义阈值</span>
                <input v-model.number="evaluationCaseForm.semanticJudgeThreshold" type="number" min="0" max="1" step="0.05" />
              </label>
            </div>

            <label class="agent-eval-form-wide">
              <span>用户消息</span>
              <textarea v-model="evaluationCaseForm.userMessage" rows="2" placeholder="输入评估时模拟发送给 Agent 的用户问题"></textarea>
            </label>

            <div class="agent-eval-form-grid two">
              <label>
                <span>期望 Actions</span>
                <textarea v-model="evaluationCaseForm.expectedActionsText" rows="3" placeholder="每行一个 Action，也支持逗号分隔"></textarea>
              </label>
              <label>
                <span>期望关键词</span>
                <textarea v-model="evaluationCaseForm.expectedAnswerContainsText" rows="3" placeholder="语义评估关闭时使用，每行一个"></textarea>
              </label>
            </div>

            <label class="agent-eval-form-wide">
              <span>语义摘要</span>
              <textarea v-model="evaluationCaseForm.expectedAnswerSummary" rows="2" placeholder="描述回答应该覆盖哪些要点"></textarea>
            </label>

            <label class="agent-eval-form-wide">
              <span>备注</span>
              <input v-model="evaluationCaseForm.remark" type="text" placeholder="用例来源、修改说明或注意事项" />
            </label>

            <div class="agent-eval-form-bottom">
              <label class="agent-eval-check">
                <input v-model="evaluationCaseForm.expectSuccess" type="checkbox" />
                <span>期望成功</span>
              </label>
              <label class="agent-eval-check">
                <input v-model="evaluationCaseForm.expectRequiresConfirmation" type="checkbox" />
                <span>需要确认</span>
              </label>
              <label class="agent-eval-check">
                <input v-model="evaluationCaseForm.enableSemanticJudge" type="checkbox" />
                <span>语义评估</span>
              </label>
              <div class="agent-eval-form-actions">
                <button type="button" class="ghost" :disabled="evaluationCaseSaving" @click="resetEvaluationCaseForm">
                  取消
                </button>
                <button type="submit" class="primary" :disabled="evaluationCaseSaving">
                  <Save :size="15" />
                  {{ evaluationEditingCaseId ? '保存修改' : '添加用例' }}
                </button>
              </div>
            </div>
          </div>
        </form>
      </div>
    </div>
    <div v-if="logPanelOpen" class="agent-log-modal" @click.self="closeWorkflowLogPanel">
      <section class="agent-log-panel" aria-label="Agent 执行日志">
        <header class="agent-log-head">
          <div>
            <h2>Agent 执行日志</h2>
            <p>查看最近工作流、计划、执行结果和补救信息。</p>
          </div>
          <div class="agent-log-head-actions">
            <button type="button" :disabled="workflowLogLoading" @click="refreshWorkflowLogs">
              <RefreshCw :size="16" />
              刷新
            </button>
            <button type="button" class="icon" aria-label="关闭日志面板" @click="closeWorkflowLogPanel">
              <X :size="18" />
            </button>
          </div>
        </header>

        <div class="agent-log-body">
          <aside class="agent-log-list">
            <p v-if="workflowLogLoading" class="agent-log-empty">正在加载执行日志...</p>
            <p v-else-if="!workflowLogs.length" class="agent-log-empty">暂无执行日志</p>
            <button
              v-for="item in workflowLogs"
              v-else
              :key="getLogId(item)"
              type="button"
              class="agent-log-item"
              :class="{ active: getLogId(item) === getLogId(selectedWorkflowLog) }"
              @click="selectWorkflowLog(item)"
            >
              <span class="agent-log-item-title">{{ getLogUserMessage(item) }}</span>
              <span class="agent-log-item-meta">
                <span class="agent-log-status" :class="getLogStatusClass(item)">
                  {{ getLogStatusText(item) }}
                </span>
                <span>{{ formatDuration(getLogDuration(item)) }}</span>
              </span>
              <span class="agent-log-item-time">{{ formatDateTime(getLogStartedAt(item)) }}</span>
            </button>
          </aside>

          <main class="agent-log-detail">
            <p v-if="workflowLogDetailLoading" class="agent-log-empty">正在加载详情...</p>
            <p v-else-if="!selectedWorkflowLog" class="agent-log-empty">选择一条日志查看详情</p>
            <template v-else>
              <div class="agent-log-summary">
                <div>
                  <span>状态</span>
                  <strong>{{ getLogStatusText(selectedWorkflowLog) }}</strong>
                </div>
                <div>
                  <span>耗时</span>
                  <strong>{{ formatDuration(getLogDuration(selectedWorkflowLog)) }}</strong>
                </div>
                <div>
                  <span>开始时间</span>
                  <strong>{{ formatDateTime(getLogStartedAt(selectedWorkflowLog)) }}</strong>
                </div>
              </div>

              <section class="agent-log-section">
                <h3>用户问题</h3>
                <p>{{ getDetailField("userMessage", "UserMessage") }}</p>
              </section>

              <section class="agent-log-section">
                <h3>最终回答</h3>
                <p>{{ getDetailField("answer", "Answer") || "暂无回答" }}</p>
              </section>

              <section class="agent-log-section">
                <h3>执行消息</h3>
                <p>{{ getDetailField("message", "Message") || "暂无消息" }}</p>
              </section>

              <section class="agent-log-section">
                <h3>PlanJson</h3>
                <pre>{{ formatJsonBlock(getDetailField("planJson", "PlanJson")) }}</pre>
              </section>

              <section class="agent-log-section">
                <h3>ExecutionResultJson</h3>
                <pre>{{ formatJsonBlock(getDetailField("executionResultJson", "ExecutionResultJson")) }}</pre>
              </section>

              <section class="agent-log-section">
                <h3>FailureAnalysis</h3>
                <pre>{{ getDetailField("failureAnalysis", "FailureAnalysis") || "暂无失败分析" }}</pre>
              </section>

              <section class="agent-log-section">
                <h3>RecoveryPlanJson</h3>
                <pre>{{ formatJsonBlock(getDetailField("recoveryPlanJson", "RecoveryPlanJson")) }}</pre>
              </section>

              <section class="agent-log-section">
                <h3>RecoveryExecutionResultJson</h3>
                <pre>{{ formatJsonBlock(getDetailField("recoveryExecutionResultJson", "RecoveryExecutionResultJson")) }}</pre>
              </section>
            </template>
          </main>
        </div>
      </section>
    </div>
  </section>
</template>












