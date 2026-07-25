using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Helper;
using System.Data;
using CuteBlogSystem.AI.Planner;

namespace CuteBlogSystem.Service
{
    public class AgentMessageService
    {
        private readonly AgentMessageRepository _messageRepository;
        private readonly AgentConversationRepository _conversationRepository;
        private readonly ILogger<AgentMessageService> _logger;

        
        public AgentMessageService(
            AgentMessageRepository messageRepository, 
            AgentConversationRepository conversationRepository,
            ILogger<AgentMessageService> logger)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        // 用户输入消息到 Agent 接收前的处理逻辑
        public async Task<ApiResponse> DealUserMessageAsync(AgentUserMessage userMessage)
        {
            ApiResponse response;

            // 确保 Conversation 存在，如果不存在则创建新的 Conversation
            var conversation = await _conversationRepository.GetBySessionIdAsync(userMessage.SessionId);

            if (conversation != null)   // 会话存在
            {
                if (conversation.UserId != userMessage.UserId)
                {
                    return new ApiResponse(false, "当前会话不存在或无权访问！", code: ResponseCode.NotFound);
                }

                if (userMessage.IsEvaluation && conversation.Status != AgentConversationStatus.Evaluation)
                {
                    return new ApiResponse(false, "评估请求不能写入普通会话！", code: ResponseCode.BadRequest);
                }

                if (!userMessage.IsEvaluation && conversation.Status == AgentConversationStatus.Evaluation)
                {
                    return new ApiResponse(false, "普通请求不能写入评估会话！", code: ResponseCode.BadRequest);
                }

                if (conversation.Status != AgentConversationStatus.Active && conversation.Status != AgentConversationStatus.Evaluation)
                {
                    switch (conversation.Status)
                    {
                        case AgentConversationStatus.Archived:
                            return new ApiResponse(false, "当前会话已被归档！恢复前无法进行对话！", code: ResponseCode.BadRequest);

                        case AgentConversationStatus.Deleted:
                            return new ApiResponse(false, "当前会话已被删除！", code: ResponseCode.BadRequest);

                        default:
                            throw new InvalidOperationException($"未知的会话状态：{conversation.Status}");
                    }
                }
            }


            if (conversation == null || userMessage.SessionId == string.Empty)
            {
                conversation = new AgentConversation
                {
                    SessionId = IDCreator.CreateSnowflakeID().ToString(),
                    UserId = userMessage.UserId,
                    Title = GenerateTitle(userMessage.Content),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ModelUsed = "deepseek-v4-pro", // 默认模型
                    Status = userMessage.IsEvaluation ? AgentConversationStatus.Evaluation : AgentConversationStatus.Active
                };
                userMessage.SessionId = conversation.SessionId; // 将新创建的 SessionId 赋值回用户消息 DTO
                bool addConversationResult = await _conversationRepository.AddConversationAsync(conversation);
                if (addConversationResult == false)
                {
                    response = new ApiResponse(false, "会话创建失败！", code: ResponseCode.InternalError);
                    return response;
                }
            }

            // 将DTO转为用户消息实体
            var agentMessage = new AgentMessage
            {
                SessionId = conversation.SessionId,
                Role = AgentMessageRole.User,
                Content = userMessage.Content,
                TokenCount = AiChatHelper.CountTokens(userMessage.Content),
                CreatedAt = DateTime.UtcNow
            };

            // 保存用户消息到数据库
            bool addMessageResult = await _messageRepository.AddMessageAsync(agentMessage); 

            if(addMessageResult == false)
            {
                response = new ApiResponse(false, "用户消息创建失败！", code:ResponseCode.InternalError);
                return response;
            }

            response = new ApiResponse
            (
                true,
                "用户消息预处理成功！",
                agentMessage,
                ResponseCode.Success
            );
            return response;
        }

        // Agent 输出消息处理逻辑
        public async Task<ApiResponse> SaveAgentMessageAsync(string sessionId, string content)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return new ApiResponse(false, "SessionId 不能为空！", code: ResponseCode.BadRequest);
            }

            // 校验对话是否存在
            var conversation = await _conversationRepository.GetBySessionIdAsync(sessionId);
            if (conversation == null)
            {
                return new ApiResponse(false, "会话不存在！", code: ResponseCode.NotFound);
            }

            // 校验内容是否存在
            if (string.IsNullOrEmpty(content))
            {
                return new ApiResponse(false, "Agent 消息不能为空！", code: ResponseCode.NotFound);
            }

            // 创建 Agent 消息实体
            var agentMessage = new AgentMessage
            {
                SessionId = sessionId,
                Role = AgentMessageRole.Assistant,
                Content = content,
                TokenCount = AiChatHelper.CountTokens(content),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _messageRepository.AddMessageAsync(agentMessage);
            if (result == false)
            {
                return new ApiResponse(false, "Agent 消息创建失败！", code: ResponseCode.InternalError);
            }

            return new ApiResponse(true, "Agent 消息创建成功！", agentMessage, ResponseCode.Success);
        }

        // 更新会话时间
        public async Task<ApiResponse> TouchConversationAsync(string sessionId)
        {
            var conversation = await _conversationRepository.GetBySessionIdAsync(sessionId);
            if (conversation == null)
            {
                return new ApiResponse(false, "会话不存在", code: ResponseCode.NotFound);
            }

            conversation.UpdatedAt = DateTime.UtcNow;

            var result = await _conversationRepository.UpdateAsync(conversation);

            if(result == true)
            {
                return new ApiResponse(true, "会话更新时间成功", code: ResponseCode.Success);
            }
            else
            {
                return new ApiResponse(false, "会话更新时间失败", code: ResponseCode.InternalError);
            }
        }

        // 根据 userId 获取用户的所有对话(Active 状态)
        public async Task<ApiResponse> GetConversationsByUserIdAsync(int userId)
        {
            try
            {
                var conversations = await _conversationRepository.GetConversationsByUserIdAsync(userId);
                if (conversations == null)
                {
                    return new ApiResponse(false, "获取用户对话列表失败！", code: ResponseCode.NotFound);
                }

                // 将实体对象映射为DTO
                var result = conversations.Select(conversation => 
                    new AgentConversationListDto(conversation)).ToList();

                return new ApiResponse(true, "获取用户对话列表成功！", result, ResponseCode.Success);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取用户对话列表时发生异常");
                return new ApiResponse(false, $"获取用户对话列表时发生异常!", code: ResponseCode.InternalError);
            }

        }

        // 根据 sessionId 获取对话消息列表
        public async Task<ApiResponse> GetMessagesBySessionIdAsync(string sessionId, int userId)
        {
            try
            {
                // 校验会话是否属于该用户
                var conversation = await _conversationRepository.GetBySessionIdAsync(sessionId);
                if(conversation == null || conversation.UserId != userId)
                {
                    return new ApiResponse(false, "当前会话不存在或无权访问！", code: ResponseCode.NotFound);
                }

                if (conversation.Status == AgentConversationStatus.Evaluation)
                {
                    return new ApiResponse(false, "评估会话不能通过普通会话接口查看！", code: ResponseCode.BadRequest);
                }

                var messages = await _messageRepository.GetBySessionIdAsync(sessionId);
                if (messages == null)
                {
                    return new ApiResponse(false, "获取对话消息列表失败！", code: ResponseCode.NotFound);
                }

                // 将实体对象映射为DTO
                var result = messages.Select(message => 
                    new AgentMessageListDto(message)).ToList();

                return new ApiResponse(true, "获取对话消息列表成功！", result, ResponseCode.Success);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取用户对话消息列表时发生异常");
                return new ApiResponse(false, $"获取对话消息列表时发生异常!", code: ResponseCode.InternalError);
            }
        }

        // 根据 sessionId 获取最近的 n 条消息记录，不能超出 token 上限
        public async Task<string> BuildRecentConversationContextAsync
        (
            string sessionId,
            int count = 10,
            int maxChars = AgentTokenBudget.RecentConversationMaxChars,
            long? beforeMessageId = null,
            long? afterMessageId = null
        )
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return string.Empty;
            }

            var messages = await _messageRepository.GetRecentBySessionIdAsync(sessionId, count, beforeMessageId, afterMessageId);

            if (messages == null || messages.Count == 0)
            {
                return string.Empty;
            }

            var orderedMessages = messages.OrderBy(m => m.CreatedAt).ToList();

            // 将消息列表转换为字符串列表，每条消息格式为 "User: xxx" 或 "Assistant: xxx"
            var lines = orderedMessages.Select(m =>
            {
                string role = "Assistant";
                if(m.Role == AgentMessageRole.User)
                {
                    role = "User";
                }
                
                var content = FormatMessageForContext(m.Role, m.Content);          // 对超出限定长度的助手消息进行截断处理
                return $"{role}: {content}";
            }).ToList();

            var selectedLines = new List<string>();
            var totalChars = 0;

            for ( int i = lines.Count - 1; i >= 0; i-- )
            {
                var line = lines[i];
                var lineLength = line.Length;
                if ( totalChars + lineLength > maxChars )
                {
                    break;
                }
                selectedLines.Insert(0, line); // 从前面插入，保持顺序
                totalChars += lineLength;
            }

            return string.Join("\n", selectedLines);       // 返回使用列表拼接好了的字符串
        }

        // 根据 sessionId 与 userId 删除对应的 Conversation 以及相关的 AgentMessage
        public async Task<ApiResponse> DeleteConversationAsync(string sessionId, int userId)
        {
            try
            {
                var conversation = await _conversationRepository.GetBySessionIdAsync(sessionId);
                if (conversation == null || conversation.UserId != userId)
                {
                    return new ApiResponse(false, "当前会话不存在或无权访问！", code: ResponseCode.NotFound);
                }

                var response = await _conversationRepository.DeleteBySessionIdAsync(sessionId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "删除用户对话时发生异常");
                return new ApiResponse(false, $"删除会话时发生异常!", code: ResponseCode.InternalError);
            }
        }

        // 根据 sessionId 对会话做逻辑归档处理
        public async Task<ApiResponse> ArchiveConversationAsync(string sessionId, int userId)
        {
            var conversation = await _conversationRepository.GetBySessionIdAsync(sessionId);

            if (conversation == null || conversation.UserId != userId)
            {
                return new ApiResponse(false, "当前会话不存在或无权访问！", code: ResponseCode.Unauthorized);
            }

            if (conversation.Status == AgentConversationStatus.Deleted)
            {
                return new ApiResponse(false, "已删除的会话无法进行归档！", code: ResponseCode.BadRequest);
            }

            if (conversation.Status == AgentConversationStatus.Archived)
            {
                return new ApiResponse(false, "该会话已经归档。", code: ResponseCode.BadRequest);
            }

            if (conversation.Status == AgentConversationStatus.Evaluation)
            {
                return new ApiResponse(false, "该会话为Agent评估用测试会话，不可归档!", code: ResponseCode.BadRequest);
            }

            var updateSucess = await _conversationRepository.UpdateStatusAsync(sessionId, AgentConversationStatus.Archived);

            if (updateSucess)
            {
                return new ApiResponse(true, "会话归档成功！", code: ResponseCode.Success);
            }
            else
            {
                return new ApiResponse(false, "会话归档失败！", code: ResponseCode.InternalError);
            }
        }

        // 根据 sessionId 对已被归档的会话做恢复
        public async Task<ApiResponse> RestoreConversationAsync(
            string sessionId,
            int userId)
        {
            var conversation =
                await _conversationRepository.GetBySessionIdAsync(sessionId);

            if (conversation == null || conversation.UserId != userId)
            {
                return new ApiResponse(
                    false,
                    "当前会话不存在或无权访问！",
                    code: ResponseCode.NotFound);
            }

            if (conversation.Status == AgentConversationStatus.Deleted)
            {
                return new ApiResponse(
                    false,
                    "已删除的会话不能恢复！",
                    code: ResponseCode.BadRequest);
            }

            if (conversation.Status == AgentConversationStatus.Active)
            {
                return new ApiResponse(
                    true,
                    "该会话已经处于活跃状态。",
                    code: ResponseCode.Success);
            }

            if (conversation.Status == AgentConversationStatus.Evaluation)
            {
                return new ApiResponse(
                    true,
                    "该会话为评估用会话，不在归档列表！",
                    code: ResponseCode.Success);
            }

            var result = await _conversationRepository.UpdateStatusAsync(
                sessionId,
                AgentConversationStatus.Active);

            return result
                ? new ApiResponse(true, "会话恢复成功！", code: ResponseCode.Success)
                : new ApiResponse(false, "会话恢复失败！", code: ResponseCode.InternalError);
        }

        // 根据 userId 获取已归档的会话列表
        public async Task<ApiResponse> GetArchivedConversationsAsync(int userId)
        {
            if (userId <= 0)
            {
                return new ApiResponse(false, "用户ID错误！", code: ResponseCode.Unauthorized);
            }

            var conversations = await _conversationRepository.GetArchivedConversationsByUserIdAsync(userId);

            var result = conversations.Select(conversation =>
                new AgentConversationListDto(conversation)).ToList();

            return new ApiResponse(
                true,
                "获取归档会话成功！",
                result,
                ResponseCode.Success);
        }

        // ===============    以下是工具方法    ===============

        // 根据用户消息生成对话标题：如果消息为空或仅包含空白字符，返回默认标题 "新对话"；否则取前20个字符作为标题，超过部分用省略号表示
        private static string GenerateTitle(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return "新对话";
            }

            var normalized = userMessage.Trim();

            return normalized.Length <= 20
                ? normalized
                : normalized[..20] + "...";
        }

        // 根据消息角色格式化消息内容：如果是用户消息，直接返回原内容；如果是助手消息且内容超过500字符，则截断并添加提示
        private static string FormatMessageForContext(AgentMessageRole role, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var normalized = content.Trim();   // 去掉首尾空白字符，减少不必要的字符数

            if (role == AgentMessageRole.User)
            {
                return normalized;
            }

            const int maxAssistantChars = 500;

            if (normalized.Length <= maxAssistantChars)
            {
                return normalized;
            }

            return normalized[..maxAssistantChars] + "\n...(助手上一条回答较长，已截断)";
        }
    }
}
