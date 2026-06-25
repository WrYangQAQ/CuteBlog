using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Repository;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    public class AgentConversationMemoryService
    {
        private readonly AgentConversationMemoryRepository _memoryRepository;
        private readonly AgentMessageRepository _messageRepository;
        private readonly IChatClient _chatClient;

        public AgentConversationMemoryService(AgentConversationMemoryRepository memoryRepository,
            AgentMessageRepository messageRepository,
            IChatClient chatClient)
        {
            _memoryRepository = memoryRepository;
            _messageRepository = messageRepository;
            _chatClient = chatClient;

        }

        // 获取指定会话 ID 的记忆记录，如果不存在则创建一条新的记录
        public async Task<AgentConversationMemory> GetOrCreateAsync(string conversationId, string? firstUserMessage = null)
        {
            // 尝试从数据库查询已有的记忆记录
            var existing = await _memoryRepository.GetByConversationIdAsync(conversationId);
            if (existing != null)
            {
                return existing;   // 已存在，直接返回
            }

            var now = DateTime.UtcNow;   // 统一使用当前时间作为创建时间和更新时间

            var memory = new AgentConversationMemory
            {
                SessionId = conversationId,
                CreatedAt = now,
                UpdatedAt = now
            };

            // 将新记录添加到数据库并返回
            return await _memoryRepository.AddAsync(memory);
        }

        // 在 Agent 响应后更新对话记忆：记录最近一条用户消息、答案、选中的文章信息
        public async Task<bool> UpdateAfterResponseAsync(
            string conversationId,
            string userMessage,
            string answer,
            int? selectedArticleId,
            string? selectedArticleTitle)
        {
            // 确保存在该会话的记忆记录
            var memory = await GetOrCreateAsync(conversationId, userMessage);

            // 更新记忆字段
            memory.LastUserMessage = userMessage;
            memory.LastAnswer = answer;
            
            // 如果该轮进行搜索并搜索到了新文章才对记忆里存储的之前文章的id和title进行更新
            if (selectedArticleId.HasValue)
            {
                memory.LastSelectedArticleId = selectedArticleId;
                memory.LastSelectedArticleTitle = selectedArticleTitle;
            }

            memory.UpdatedAt = DateTime.UtcNow;   // 更新最后修改时间

            // 保存变更到数据库，返回是否成功
            return await _memoryRepository.UpdateAsync(memory);
        }

        // 在工作流完成后更新对话记忆：提取执行结果中用户可能选中的文章，并保存最近交互信息
        public async Task<bool> UpdateAfterWorkflowAsync(
            string conversationId,           // 会话标识符
            string userMessage,              // 用户发送的消息
            string answer,                   // Agent 返回的最终答案
            AgentPlanExecutionResult? executionResult)   // 计划执行结果（可能为 null）
        {
            // 从执行结果中尝试提取用户选中的文章信息（文章ID和标题）
            var selectedArticle = ExtractSelectedArticle(executionResult);

            // 调用底层更新方法，保存用户消息、答案以及选中的文章信息
            return await UpdateAfterResponseAsync(
                conversationId,
                userMessage,
                answer,
                selectedArticle.ArticleId,
                selectedArticle.Title);
        }

        // 从执行结果中提取用户最后可能选中或讨论的文章（取最后一个成功的搜索文章步骤的数据）
        private static SelectedArticleMemory ExtractSelectedArticle(
            AgentPlanExecutionResult? executionResult)
        {
            // 如果执行结果为空，直接返回空结果
            if (executionResult == null)
            {
                return new SelectedArticleMemory();
            }

            // 从所有成功的步骤中，找出 Action 为 SearchArticlesByCategory 且 Data 非空的步骤
            // 按步骤编号降序排序，取最后一个（即最近一次成功的搜索文章步骤）
            var searchStep = executionResult.StepResults
                .Where(s => s.Success && s.Action == AgentActionRegistry.SearchArticlesByCategory && s.Data != null)
                .OrderByDescending(s => s.StepNumber)
                .FirstOrDefault();

            // 如果没有找到符合条件的步骤，返回空结果
            if (searchStep?.Data == null)
            {
                return new SelectedArticleMemory();
            }

            // 从步骤数据的对象中解析出文章ID和标题
            return ExtractArticleFromData(searchStep.Data);
        }

        // 从执行步骤的 Data 对象中提取文章信息（ID 和标题），支持多种 JSON 结构
        private static SelectedArticleMemory ExtractArticleFromData(object data)
        {
            // 将任意对象序列化为 JSON 字符串，以便统一解析
            var json = JsonSerializer.Serialize(data);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // 如果根节点是数组且非空，取第一个元素作为文章对象
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                return ExtractArticleFromJsonElement(root[0]);
            }

            // 处理根节点为对象的情况
            if (root.ValueKind == JsonValueKind.Object)
            {
                // 尝试获取常见的包装属性 "data"
                if (root.TryGetProperty("data", out var dataProperty))
                {
                    // 如果 data 属性是数组且非空，取第一个元素
                    if (dataProperty.ValueKind == JsonValueKind.Array && dataProperty.GetArrayLength() > 0)
                    {
                        return ExtractArticleFromJsonElement(dataProperty[0]);
                    }

                    // 如果 data 属性是对象，直接使用
                    if (dataProperty.ValueKind == JsonValueKind.Object)
                    {
                        return ExtractArticleFromJsonElement(dataProperty);
                    }
                }

                // 没有 "data" 包装，直接使用根对象
                return ExtractArticleFromJsonElement(root);
            }

            // 无法识别的结构，返回空记忆
            return new SelectedArticleMemory();
        }

        // 从 JSON 元素中提取文章信息（ID 和标题），尝试多种常见的属性名
        private static SelectedArticleMemory ExtractArticleFromJsonElement(JsonElement element)
        {
            // 尝试从多个可能的属性名中获取文章 ID
            var articleId = TryGetIntProperty(element, "id")
                ?? TryGetIntProperty(element, "articleId")
                ?? TryGetIntProperty(element, "ArticleId")
                ?? TryGetIntProperty(element, "Id");

            // 尝试获取文章标题
            var title = TryGetStringProperty(element, "title")
                ?? TryGetStringProperty(element, "Title");

            return new SelectedArticleMemory
            {
                ArticleId = articleId,
                Title = title
            };
        }

        // 尝试从json串中获取指定名称的整型属性值（支持数字或数字字符串）
        private static int? TryGetIntProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
            {
                return number;
            }

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), out var stringNumber))
            {
                return stringNumber;
            }

            return null;
        }

        // 尝试从json串中获取指定名称的字符串属性值
        private static string? TryGetStringProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : property.ToString();
        }

        // 构建带记忆增强的用户消息：如果存在有效的对话记忆，则添加上下文信息帮助 AI 理解指代
        public async Task<string> BuildMemoryContextAsync(
            string userMessage,
            string? conversationId)
        {
            // 没有会话 ID 则无法获取记忆，直接返回原始消息
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return string.Empty;
            }

            // 获取该会话的记忆记录
            var memory = await _memoryRepository.GetByConversationIdAsync(conversationId);

            // 判断是否应该使用记忆
            if (memory == null)
            {
                return string.Empty;
            }

            var sections = new List<string>();

            if (!string.IsNullOrWhiteSpace(memory.ConversationSummary))
            {
                sections.Add($"""
                    较早对话摘要：
                    {memory.ConversationSummary}
                    """);
            }

            // 精确的文章记忆只在发生指代时使用。
            if (ShouldUseMemory(userMessage, memory))
            {
                sections.Add($"""
                    可用的结构化文章记忆：
                    - 上一次用户问题：{memory.LastUserMessage}
                    - 上一次回答：{memory.LastAnswer}
                    - 上一次选中的文章ID：{memory.LastSelectedArticleId}
                    - 上一次选中的文章标题：{memory.LastSelectedArticleTitle}

                    如果“它、这篇、那篇、刚才那篇”等表达指代该文章，
                    请优先使用上述文章ID，不要重新搜索。
                    """);
            }

            return string.Join("\n\n", sections);
        }

        // 判断是否应该使用记忆增强用户消息：如果用户消息中包含指代词且记忆中有有效的选中文章信息，则返回 true；否则返回 false
        private static bool ShouldUseMemory(
            string userMessage,
            AgentConversationMemory memory)
        {
            if (memory.LastSelectedArticleId == null)
            {
                return false;
            }

            var keywords = new[]
            {
                "它",
                "这篇",
                "那篇",
                "上一篇",
                "刚刚",
                "之前",
                "刚才",
                "上面",
                "这个",
                "该文章",
                "继续",
                "详细讲讲",
                "主要讲了什么"
            };

            return keywords.Any(userMessage.Contains);
        }

        // 尝试对指定会话的未总结消息进行摘要更新，如果满足触发条件则生成新摘要并保存
        public async Task<bool> TrySummarizeConversationAsync(string sessionId)
        {
            // 获取或创建会话记忆记录，用于存储摘要和状态
            var memory = await GetOrCreateAsync(sessionId);

            // 计算摘要消息ID下界
            var summaryBoundaryMessageId = GetLatestBoundaryMessageId(
                memory.LastSummarizedMessageId,
                memory.ContextResetMessageId);

            // 统计当前会话中尚未被纳入摘要的消息数量
            var unsummarizedCount =
                await _messageRepository.CountUnsummarizedMessagesAsync(
                    sessionId,
                    summaryBoundaryMessageId);

            // 如果未总结消息数量未达到触发阈值，则不执行摘要
            if (unsummarizedCount <
                AgentTokenBudget.ConversationSummaryTriggerMessageCount)
            {
                return false;
            }

            // 获取需要总结的消息列表：保留最近若干条消息（以维持上下文），
            // 并取一批较早的消息用于生成摘要。
            var messages =
                await _messageRepository.GetMessagesForSummaryAsync(
                    sessionId,
                    summaryBoundaryMessageId,
                    AgentTokenBudget.ConversationSummaryKeepRecentCount,
                    AgentTokenBudget.ConversationSummaryBatchSize);

            // 如果没有可总结的消息，直接返回
            if (messages.Count == 0)
            {
                return false;
            }

            // 将消息格式化为文本，每条消息以 "User:" 或 "Assistant:" 开头
            var messageText = string.Join(
                "\n",
                messages.Select(message =>
                {
                    var role = message.Role == AgentMessageRole.User
                        ? "User"
                        : "Assistant";

                    return $"{role}: {message.Content}";
                }));

            // 构造 AI 请求：包含现有摘要和新消息，要求生成新的合并摘要
            var chatMessages = new List<ChatMessage>
            {
                new(
                    ChatRole.System,
                    """
                    你是对话记忆摘要助手。

                    请把旧摘要和新增对话合并成一份简洁的中文摘要。

                    要求：
                    1. 保留用户目标、偏好、重要结论和已经选中的文章。
                    2. 保留明确的文章ID、标题等精确信息。
                    3. 忽略客套话、重复内容和临时错误提示。
                    4. 不要编造对话中不存在的信息。
                    5. 不要输出JSON、Markdown标题或解释。
                    6. 输出内容应当能够帮助后续Agent理解对话。
                    """),

                new(
                    ChatRole.User,
                    $"""
                    现有历史摘要：
                    {memory.ConversationSummary ?? "暂无"}

                    本次新增的较早对话：
                    {messageText}
                    """)
            };

            // 调用 AI 生成摘要，并限制输出 Token 数
            var response = await _chatClient.GetResponseAsync(
                chatMessages,
                new ChatOptions
                {
                    MaxOutputTokens =
                        AgentTokenBudget.ConversationSummaryMaxOutputTokens
                });

            // 提取助手的回复作为新摘要
            var summary = response.Messages
                .Where(message => message.Role == ChatRole.Assistant)
                .Select(message => message.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

            // 如果 AI 未返回有效摘要，视为失败
            if (string.IsNullOrWhiteSpace(summary))
            {
                return false;
            }

            // 截断摘要至数据库 nvarchar(4000) 字段长度，防止存储异常
            if (summary.Length > 4000)
            {
                summary = summary[..4000];
            }

            // 更新记忆记录：新摘要、最后总结消息 ID、更新时间
            memory.ConversationSummary = summary;
            memory.LastSummarizedMessageId = messages[^1].MessageId;
            memory.SummaryLastUpdate = DateTime.UtcNow;

            // 保存更新，返回是否成功
            return await _memoryRepository.UpdateAsync(memory);
        }

        // 在用户发送特定指令时对上下文进行重置
        public async Task<bool> ResetConversationContextAsync(
            string sessionId,
            long resetMessageId)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || resetMessageId <= 0)
            {
                return false;
            }

            var memory = await GetOrCreateAsync(sessionId);

            memory.LastUserMessage = string.Empty;
            memory.LastAnswer = string.Empty;

            memory.LastSelectedArticleId = null;
            memory.LastSelectedArticleTitle = null;

            memory.ConversationSummary = null;
            memory.LastSummarizedMessageId = null;
            memory.SummaryLastUpdate = null;

            // 该条会话相关消息不再参与上下文构建，但依然存储在数据库
            memory.ContextResetMessageId = resetMessageId;
            memory.ContextResetAt = DateTime.UtcNow;
            memory.UpdatedAt = DateTime.UtcNow;

            return await _memoryRepository.UpdateAsync(memory);
        }

        // 根据ID计算生成摘要的ID下界
        private static long? GetLatestBoundaryMessageId(
            long? lastSummarizedMessageId,
            long? contextResetMessageId)
        {
            if (!lastSummarizedMessageId.HasValue)
            {
                return contextResetMessageId;
            }

            if (!contextResetMessageId.HasValue) 
            {
                return lastSummarizedMessageId;
            }

            return Math.Max(lastSummarizedMessageId.Value, contextResetMessageId.Value);
        }
    }
}