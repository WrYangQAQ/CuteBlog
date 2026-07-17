import http from "./http";

const AGENT_REQUEST_TIMEOUT = 5 * 60 * 1000;

function agentRequestConfig(config = {}) {
  return {
    timeout: AGENT_REQUEST_TIMEOUT,
    ...config
  };
}

export function askAgentApi({ content, sessionId, userId, debug = false }) {
  return http.post(`/api/AiAgent/planner-ask?debug=${debug}`, {
    sessionId,
    userId,
    content,
    role: 2
  }, agentRequestConfig({
    allowBusinessFailure: true
  }));
}

export function getAgentConversationsApi() {
  return http.get("/api/AiAgent/conversations", agentRequestConfig());
}

export function getArchivedAgentConversationsApi() {
  return http.get("/api/AiAgent/conversations/archived", agentRequestConfig());
}

export function getAgentMessagesApi(sessionId) {
  return http.get(`/api/AiAgent/conversations/${encodeURIComponent(sessionId)}/messages`, agentRequestConfig());
}

export function archiveAgentConversationApi(sessionId) {
  return http.patch(`/api/AiAgent/conversations/${encodeURIComponent(sessionId)}/archive`, null, agentRequestConfig());
}

export function restoreAgentConversationApi(sessionId) {
  return http.patch(`/api/AiAgent/conversations/${encodeURIComponent(sessionId)}/restore`, null, agentRequestConfig());
}

export function deleteAgentConversationApi(sessionId) {
  return http.delete(`/api/AiAgent/conversations/${encodeURIComponent(sessionId)}`, agentRequestConfig());
}

export function confirmAgentPlanApi({ sessionId, confirmationId, debug = false }) {
  return http.post(`/api/AiAgent/confirm?debug=${debug}`, {
    sessionId,
    confirmationId
  }, agentRequestConfig({
    allowBusinessFailure: true
  }));
}

export function cancelAgentConfirmationApi({ sessionId, confirmationId }) {
  return http.post("/api/AiAgent/cancel-confirmation", {
    sessionId,
    confirmationId
  }, agentRequestConfig({
    allowBusinessFailure: true
  }));
}

export function getRecentAgentWorkflowLogsApi(count = 20) {
  return http.get("/api/AiAgent/workflow-logs/recent", agentRequestConfig({
    params: { count }
  }));
}

export function getAgentWorkflowLogDetailApi(id) {
  return http.get(`/api/AiAgent/logs/${encodeURIComponent(id)}`, agentRequestConfig());
}

export function runAgentEvaluationApi(caseIds = []) {
  return http.post("/api/AiAgent/evaluation/run", caseIds, agentRequestConfig());
}

export function rerunAgentEvaluationRunApi(runId) {
  return http.post(`/api/AiAgent/evaluation/runs/${encodeURIComponent(runId)}/rerun`, null, agentRequestConfig());
}

export function getRecentAgentEvaluationRunsApi(recentCount = 10) {
  return http.get("/api/AiAgent/evaluation/runs/recent", agentRequestConfig({
    params: { recentCount }
  }));
}

export function getAgentEvaluationRunResultsApi(runId) {
  return http.get(`/api/AiAgent/evaluation/runs/${encodeURIComponent(runId)}/results`, agentRequestConfig());
}

export function getAgentEvaluationReportApi(runId) {
  return http.get(`/api/AiAgent/evaluation/runs/${encodeURIComponent(runId)}/report`, agentRequestConfig());
}

export function saveAgentEvaluationReportSnapshotApi(runId) {
  return http.post(`/api/AiAgent/evaluation/runs/${encodeURIComponent(runId)}/report/snapshot`, null, agentRequestConfig());
}

export function getAgentEvaluationReportSnapshotApi(runId) {
  return http.get(`/api/AiAgent/evaluation/runs/${encodeURIComponent(runId)}/report/snapshot`, agentRequestConfig());
}

export function downloadAgentEvaluationReportApi(runId) {
  return http.get(`/api/AiAgent/evaluation/runs/${encodeURIComponent(runId)}/report`, agentRequestConfig({
    params: { download: true },
    responseType: "blob"
  }));
}

export function compareAgentEvaluationRunsApi(baseRunId, targetRunId) {
  return http.get("/api/AiAgent/evaluation/runs/compare", agentRequestConfig({
    params: { baseRunId, targetRunId }
  }));
}

export function getAgentEvaluationRegressionSummaryApi(baseRunId, targetRunId) {
  return http.get("/api/AiAgent/evaluation/runs/regression-summary", agentRequestConfig({
    params: { baseRunId, targetRunId }
  }));
}

export function getAgentEvaluationWorkflowLogApi(runId, caseId) {
  return http.get(`/api/AiAgent/evaluation/runs/${encodeURIComponent(runId)}/test-cases/${encodeURIComponent(caseId)}/workflow-log`, agentRequestConfig());
}

export function getAgentEvaluationTestCasesApi(status = 1) {
  return http.get("/api/AiAgent/evaluation/test-cases", agentRequestConfig({
    params: { status }
  }));
}

export function createAgentEvaluationTestCaseApi(payload) {
  return http.post("/api/AiAgent/evaluation/test-cases", payload, agentRequestConfig());
}

export function updateAgentEvaluationTestCaseApi(payload) {
  return http.put("/api/AiAgent/evaluation/test-cases", payload, agentRequestConfig());
}

export function deleteAgentEvaluationTestCaseApi(caseId) {
  return http.delete("/api/AiAgent/evaluation/test-cases", agentRequestConfig({
    params: { caseId }
  }));
}

export function toggleAgentEvaluationTestCaseStatusApi(caseId) {
  return http.patch("/api/AiAgent/evaluation/test-cases", null, agentRequestConfig({
    params: { caseId }
  }));
}
