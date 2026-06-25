import http from "./http";

export function askAgentApi({ content, sessionId, userId, debug = false }) {
  return http.post(`/api/AiAgent/planner-ask?debug=${debug}`, {
    sessionId,
    userId,
    content,
    role: 2
  }, {
    allowBusinessFailure: true
  });
}

export function getAgentConversationsApi() {
  return http.get("/api/AiAgent/conversations");
}

export function getArchivedAgentConversationsApi() {
  return http.get("/api/AiAgent/conversations/archived");
}

export function getAgentMessagesApi(sessionId) {
  return http.get(`/api/AiAgent/conversations/${encodeURIComponent(sessionId)}/messages`);
}

export function archiveAgentConversationApi(sessionId) {
  return http.patch(`/api/AiAgent/conversations/${encodeURIComponent(sessionId)}/archive`);
}

export function restoreAgentConversationApi(sessionId) {
  return http.patch(`/api/AiAgent/conversations/${encodeURIComponent(sessionId)}/restore`);
}

export function deleteAgentConversationApi(sessionId) {
  return http.delete(`/api/AiAgent/conversations/${encodeURIComponent(sessionId)}`);
}

export function confirmAgentPlanApi({ sessionId, confirmationId, debug = false }) {
  return http.post(`/api/AiAgent/confirm?debug=${debug}`, {
    sessionId,
    confirmationId
  }, {
    allowBusinessFailure: true
  });
}

export function cancelAgentConfirmationApi({ sessionId, confirmationId }) {
  return http.post("/api/AiAgent/cancel-confirmation", {
    sessionId,
    confirmationId
  }, {
    allowBusinessFailure: true
  });
}

export function getRecentAgentWorkflowLogsApi(count = 20) {
  return http.get("/api/AiAgent/workflow-logs/recent", {
    params: { count }
  });
}

export function getAgentWorkflowLogDetailApi(id) {
  return http.get(`/api/AiAgent/logs/${encodeURIComponent(id)}`);
}
