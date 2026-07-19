using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.DTO;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using System.Numerics;

namespace CuteBlogSystem.Service
{
    // 整合整个 Agent 工作流：规划、验证、修复、执行、失败分析、重新规划
    public class AgentWorkflowService
    {
        private readonly AiPlannerService _aiPlannerService;                            // 生成初始计划
        private readonly AgentPlanValidatorService _agentPlanValidator;                 // 校验计划合法性
        private readonly AgentPlanRepairService _agentPlanRepairService;                // 修复不合法的计划
        private readonly AgentPlanExecutorService _agentPlanExecutor;                   // 执行计划
        private readonly AgentExecutionFailureAnalyzerService _failureAnalyzer;         // 分析执行失败原因
        private readonly AgentReplannerService _agentReplannerService;                  // 生成补救计划
        private readonly ILogger<AgentWorkflowService> _logger;                         // 日志记录
        private readonly AgentWorkflowLogService _agentWorkflowLogService;              // 工作流日志服务
        private readonly AgentConversationMemoryService _memoryService;                 // 对话记忆服务
        private readonly AgentMessageService _agentMessageService;                      // 消息服务
        private readonly AIShieldService _aiShieldService;                              // AIShield 安全检测服务
        private readonly AgentIntentRouterService _routerService;                       // 路由解析
        private readonly AgentPendingConfirmationService _pendingConfirmationService;   // plan待确认记录服务
        private readonly AgentParameterPermissionService _paramPermissionService;       // 参数权限校验
        private readonly AgentParameterRiskService _paramRiskService;                   // 参数风险校验
        private static readonly TimeSpan WorkflowTimeout = TimeSpan.FromSeconds(90);    // 工作流总超时时间，防止长时间挂起
        private static readonly TimeSpan DirectChatTimeout = TimeSpan.FromSeconds(20);  // DirectChat总超时时间，防止长时间挂起
        private const int MaxPlanRepairAttempts = 3;                                    // 最大重试次数，防止无限循环
        private const int MaxRecoveryPlanAttempts = 1;                                  // 补救计划最大尝试次数，避免过度复杂化


        public AgentWorkflowService(
            AiPlannerService aiPlannerService,
            AgentPlanValidatorService agentPlanValidator,
            AgentPlanRepairService agentPlanRepairService,
            AgentPlanExecutorService agentPlanExecutor,
            AgentExecutionFailureAnalyzerService failureAnalyzer,
            AgentReplannerService agentReplannerService,
            ILogger<AgentWorkflowService> logger,
            AgentWorkflowLogService agentWorkflowLogService,
            AgentConversationMemoryService memoryService,
            AgentMessageService agentMessageService,
            AIShieldService aiShieldService,
            AgentIntentRouterService routerService,
            AgentPendingConfirmationService agentPendingConfirmationService,
            AgentParameterPermissionService paramPermissionService,
            AgentParameterRiskService paramRiskService)
        {
            _aiPlannerService = aiPlannerService;
            _agentPlanValidator = agentPlanValidator;
            _agentPlanRepairService = agentPlanRepairService;
            _agentPlanExecutor = agentPlanExecutor;
            _failureAnalyzer = failureAnalyzer;
            _agentReplannerService = agentReplannerService;
            _logger = logger;
            _agentWorkflowLogService = agentWorkflowLogService;
            _memoryService = memoryService;
            _agentMessageService = agentMessageService;
            _aiShieldService = aiShieldService;
            _routerService = routerService;
            _pendingConfirmationService = agentPendingConfirmationService;
            _paramPermissionService = paramPermissionService;
            _paramRiskService = paramRiskService;
        }

        // 处理用户消息的入口，返回最终响应（含调试信息可选）
        public async Task<AgentAskResponse> AskAsync(AgentUserMessage userMessage, int userId, bool debug = false)
        {
            // 验证用户身份
            if (userMessage.UserId <= 0)
            {
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = "用户身份无效",
                    Answer = "请先登录后再使用 Agent。"
                };
            }

            // 空消息直接返回失败响应
            if (string.IsNullOrWhiteSpace(userMessage.Content))
            {
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = "消息不能为空",
                    Answer = "请输入要咨询的问题。"
                };
            }

            // 判断用户消息是否过长，超过限制则直接返回失败响应
            if (AgentTokenBudget.IsUserMessageTooLong(userMessage.Content))
            {
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = "用户输入过长",
                    Answer = $"你的问题太长了，请控制在 {AgentTokenBudget.MaxUserMessageChars} 个字符以内后再试。"
                };
            }

            // 在保存用户消息和进入 Agent 前，先调用 AIShield 检测用户输入
            var inputCheck = await _aiShieldService.CheckInputAsync(userMessage.Content, userMessage.UserId);
            if (inputCheck.ShouldBlock())
            {
                // 输入命中安全规则时，直接返回安全提示，不再调用模型或执行工具
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = $"AIShield 输入检测拦截：{inputCheck.Reason}",
                    Answer = "该请求存在安全风险，已被系统拦截。"
                };
            }

            // 对消息进行预处理
            // 如果会话存在，使用该会话的SessionId；如果会话不存在，创建新的会话
            // 将用户消息保存到数据库中
            var dealMessageResponse = await _agentMessageService.DealUserMessageAsync(userMessage);
            if (!dealMessageResponse.Success)
            {
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = $"消息预处理失败：{dealMessageResponse.Message}",
                    Answer = "抱歉，处理你的消息时发生了问题，请稍后重试或联系管理员。"
                };
            }

            var savedUserMessage = dealMessageResponse.Data as AgentMessage;

            if (savedUserMessage == null)
            {
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = "用户消息保存失败！",
                    Answer = "您好，服务器好像出了一点小故障，我们暂时无法聊天，请过会再来吧。"
                };
            }

            var intentResult = await _routerService.RouteAsync(userMessage.Content);

            _logger.LogInformation(
                    "识别 Agent 意图：{Intent}，置信度：{Confidence}，原因：{Reason}",
                    intentResult.Intent,
                    intentResult.Confidence,
                    intentResult.Reason);

            var startedAt = DateTime.UtcNow;
            AgentAskResponse response;

            // 检测用户意图，执行清除上下文
            if (intentResult.Intent == AgentIntentType.ResetContext)
            {
                var resetSuccess = await _memoryService.ResetConversationContextAsync(
                    savedUserMessage.SessionId,
                    savedUserMessage.MessageId);

                if (!resetSuccess)
                {
                    response =  new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "重置上下文失败",
                        Answer = "暂时无法清除当前会话的记忆，请稍后重试。"
                    };
                }
                else
                {
                    response = new AgentAskResponse
                    {
                        Success = true,
                        Recovered = false,
                        Message = "重置上下文成功！",
                        Answer = "好的，之前的记忆已经作废啦！我们重新开始吧"
                    };
                }
            }

            // 检测用户意图，直接输出聊天，不使用Plan
            else if (intentResult.Intent == AgentIntentType.DirectChat)
            {
                try
                {
                    var result = await _routerService
                        .GenerateDirectChatResponseAsync(savedUserMessage.Content)
                        .WaitAsync(DirectChatTimeout);
                    response = new AgentAskResponse
                    {
                        Success = true,
                        Recovered = false,
                        Message = "直接对话响应成功",
                        Answer = result
                    };
                }
                catch(TimeoutException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "DirectChat 响应超时。用户消息：{Message}",
                        userMessage.Content);

                    response = new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "直接对话响应超时",
                        Answer = "抱歉，我暂时没有及时回应，请稍后再试。"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "DirectChat 生成回答时发生异常。用户消息：{Message}",
                        userMessage.Content);

                    response = new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "直接对话响应异常",
                        Answer = "抱歉，回复你的消息时出现了问题，请稍后再试。"
                    };
                }
            }

            // 当用户类型意图不合法时，输出固定无法回答回复
            else if (intentResult.Intent == AgentIntentType.Unsupported)
            {
                response = _routerService.GenerateUnsupportedResponse(intentResult);
            }

            else
            {
                try
                {

                    // 构建带有记忆增强的消息内容
                    var memoryAugmentedMessage = await _memoryService.BuildMemoryContextAsync
                    (
                        userMessage.Content,
                        userMessage.SessionId
                    );

                    var memory = await _memoryService.GetOrCreateAsync(userMessage.SessionId);

                    // 构建最近的对话上下文，供规划和执行使用，确保 Agent 能够基于最新的对话历史进行决策
                    var recentConversationContext = await _agentMessageService.BuildRecentConversationContextAsync
                    (
                        userMessage.SessionId,
                        count: 6,                   // 包含最近的 6 条消息（用户和 Assistant），可以根据实际情况调整数量
                        beforeMessageId: savedUserMessage?.MessageId,
                        afterMessageId: memory.ContextResetMessageId
                    );

                    // 构建工作流输入消息，包含用户问题、记忆增强内容和最近的对话上下文
                    var workflowMessage = BuildAgentInputMessage
                    (
                        userMessage.Content,
                        memoryAugmentedMessage,
                        recentConversationContext
                    );

                    
                    // 执行核心工作流（规划、校验、执行、失败处理等）
                    response = await ExecuteWorkflowAsync(workflowMessage, userMessage, userId).WaitAsync(WorkflowTimeout);
                }
                catch (TimeoutException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "AgentWorkflow 执行超时。用户问题：{Message}，超时时间：{TimeoutSeconds} 秒",
                        userMessage.Content,
                        WorkflowTimeout.TotalSeconds);

                    response = new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "Agent 执行超时",
                        Answer = "抱歉，这次任务执行时间过长，已自动停止等待。你可以尝试缩小问题范围后重试。",
                        Debug = new AgentDebugInfo
                        {
                            FailureAnalysis = ex.ToString()
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AgentWorkflow 执行过程中发生未处理异常。用户问题：{Message}", userMessage.Content);

                    response = new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "Agent 执行过程中发生异常",
                        Answer = "抱歉，Agent 在处理这个问题时发生了异常，请稍后重试或联系管理员。",
                        Debug = new AgentDebugInfo
                        {
                            FailureAnalysis = ex.ToString()
                        }
                    };
                }
            }

            // 对 Agent 执行返回的响应做处理，如安全检测，保存消息
            return await FinalizeAgentResponseAsync(
                response,
                userMessage.Content,
                userMessage.SessionId,
                userMessage.UserId,
                startedAt,
                debug);
        }

        // 在前端确认计划执行后，执行计划
        public async Task<AgentAskResponse> ConfirmAsync(
            string confirmationId,
            string sessionId,
            int userId,
            bool debug = false)
        {
            // 记录执行计划的开始时间
            var startedAt = DateTime.UtcNow;

            var userMessageForLog = $"确认执行待确认计划：{confirmationId}";

            AgentAskResponse response;

            // 根据 ConfirmationId 对待确认的 plan 做确认处理，同时拿到 plan 实体对象
            var confirmedPlan = await _pendingConfirmationService.ConfirmAsync(
                confirmationId, userId.ToString(), sessionId);

            // 如果返回对象为空，直接返回响应
            if (confirmedPlan == null)
            {
                response = new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = "确认请求无效或已过期",
                    Answer = "这个确认请求不存在、已经处理过或已经过期。请重新发起任务。"
                };

                return await FinalizeAgentResponseAsync
                (
                    response,
                    userMessageForLog,
                    sessionId,
                    userId,
                    startedAt,
                    debug
                );
            }
            else
            {
                if (confirmedPlan.UserId != userId.ToString())
                {
                    response = new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "确认请求不属于当前用户",
                        Answer = "这个确认请求不属于当前登录用户，无法执行。"
                    };

                    return await FinalizeAgentResponseAsync(
                        response,
                        userMessageForLog,
                        sessionId,
                        userId,
                        startedAt,
                        debug);
                }

                // 检测生成计划与用户的权限关系
                var permissionResult = await _paramPermissionService.ValidateAsync(confirmedPlan.Plan, userId);

                if (!permissionResult.IsValid)
                {
                    response = new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "确认计划参数权限校验失败",
                        Answer = "抱歉，这个操作涉及你无权访问或无权修改的资源。",
                        Debug = debug
                            ? new AgentDebugInfo
                            {
                                Plan = confirmedPlan.Plan,
                                ValidationErrors = permissionResult.Errors
                            }
                            : null
                    };

                    return await FinalizeAgentResponseAsync(
                        response,
                        confirmedPlan.UserMessage,
                        confirmedPlan.SessionId,
                        userId,
                        startedAt,
                        debug);
                }

                // 检测生成计划参数的风险
                var riskParamResult = _paramRiskService.Validate(confirmedPlan.Plan);

                _logger.LogInformation
                (
                    "计划参数风险校验结果：IsSafe={IsSafe}, Errors={Errors}",
                    riskParamResult.IsSafe,
                    string.Join("；", riskParamResult.Errors)
                );

                if (!riskParamResult.IsSafe)
                {
                    response = new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "确认计划参数风险校验失败",
                        Answer = "抱歉，这个操作的参数存在较高风险，我暂时不能直接执行。请你重新描述要修改的内容。",
                        Debug = debug
                            ? new AgentDebugInfo
                            {
                                Plan = confirmedPlan.Plan,
                                ValidationErrors = riskParamResult.Errors
                            }
                            : null
                    };

                    return await FinalizeAgentResponseAsync(
                        response,
                        confirmedPlan.UserMessage,
                        confirmedPlan.SessionId,
                        userId,
                        startedAt,
                        debug);
                }

                try
                {
                    // 执行批准后的计划并获取响应
                    response = await ExecuteApprovedPlanAsync(confirmedPlan.Plan, confirmedPlan.UserMessage, userId, sessionId);

                    // 对返回的响应做最后处理，包括安全检测、消息保存等等
                    return await FinalizeAgentResponseAsync
                    (
                        response,
                        confirmedPlan.UserMessage,
                        confirmedPlan.SessionId,
                        userId,
                        startedAt,
                        debug
                    );
                }
                catch (Exception ex)
                {
                    // 日志记录
                    _logger.LogError(ex,
                        "确认执行 Agent Plan 时发生异常。ConfirmationId：{ConfirmationId}",
                        confirmationId);

                    // 生成失败响应
                    response = new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "确认执行过程中发生异常",
                        Answer = "抱歉，确认执行这个操作时发生了异常，请稍后重试。",
                    };

                    if (debug)
                    {
                        response.Debug = new AgentDebugInfo
                        {
                            FailureAnalysis = ex.ToString()
                        };
                    }

                    // 对返回的响应做最后处理，包括安全检测、消息保存等等
                    return await FinalizeAgentResponseAsync
                    (
                        response,
                        confirmedPlan.UserMessage,
                        confirmedPlan.SessionId,
                        userId,
                        startedAt,
                        debug
                    );
                }
            }


        }

        // 执行完整的 Agent 工作流，返回包含成功状态、消息、答案和调试信息的响应对象
        private async Task<AgentAskResponse> ExecuteWorkflowAsync(string workflowMessage, AgentUserMessage userMessage, int userId)
        {
            _logger.LogInformation("AgentWorkflow 开始处理用户问题：{Message}", workflowMessage);

            // 生成初始计划
            var plan = await _aiPlannerService.CreatePlanAsync(workflowMessage);

            // 校验计划合法性
            var validationResult = _agentPlanValidator.Validate(plan);

            // 如果计划不合法，尝试自动修复
            var repairAttempts = 0;

            while (!validationResult.IsValid && repairAttempts < MaxPlanRepairAttempts)
            {
                repairAttempts++;

                _logger.LogWarning(
                    "Planner 生成的计划未通过校验，准备尝试第 {Attempt} 次修复，最多 {MaxAttempts} 次。错误：{Errors}",
                    repairAttempts,
                    MaxPlanRepairAttempts,
                    string.Join("；", validationResult.Errors));

                plan = await _agentPlanRepairService.RepairPlanAsync(
                    workflowMessage,
                    plan,
                    validationResult);

                validationResult = _agentPlanValidator.Validate(plan);
            }

            if (!validationResult.IsValid)
            {
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = $"Planner 生成的计划修复 {MaxPlanRepairAttempts} 次后仍未通过校验",
                    Answer = "抱歉，我暂时无法为这个请求生成安全可执行的计划。",
                    Debug = new AgentDebugInfo
                    {
                        Plan = plan,
                        ValidationErrors = validationResult.Errors
                    }
                };
            }

            // 检查计划中是否含有高风险动作
            var riskLevel = AgentActionRegistry.GetHighestRiskLevel(plan);   // 获取计划中风险最高的动作的风险等级

            if (riskLevel == AgentActionRiskLevel.Forbidden)
            {
                return CreateForbiddenActionResponse();
            }    // 当含有被禁止的动作时返回失败响应

            else
            {
                var permissionResult = await _paramPermissionService.ValidateAsync(plan, userId);
                if (!permissionResult.IsValid)
                {
                    return new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "计划参数权限校验失败",
                        Answer = "抱歉，这个操作涉及你无权访问或无权修改的资源。",
                        Debug = new AgentDebugInfo
                        {
                            Plan = plan,
                            ValidationErrors = permissionResult.Errors
                        }
                    };
                }

                var riskParamResult = _paramRiskService.Validate(plan);

                _logger.LogInformation
                (
                    "计划参数风险校验结果：IsSafe={IsSafe}, Errors={Errors}",
                    riskParamResult.IsSafe,
                    string.Join("；", riskParamResult.Errors)
                );

                if (!riskParamResult.IsSafe)
                {
                    return new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        Message = "计划参数风险校验失败",
                        Answer = "抱歉，这个操作的参数存在较高风险，我暂时不能直接执行。请你重新描述要修改的内容。",
                        Debug = new AgentDebugInfo
                        {
                            Plan = plan,
                            ValidationErrors = riskParamResult.Errors
                        }
                    };
                }
                
                if (riskLevel == AgentActionRiskLevel.RequireConfirmation)
                {
                    var confirmationId = await _pendingConfirmationService.CreateAsync(
                        userMessage.SessionId,
                        userMessage.UserId.ToString(),
                        userMessage.Content,
                        plan);

                    // 校验 confirmationId 是否为空
                    if (string.IsNullOrWhiteSpace(confirmationId))
                    {
                        return new AgentAskResponse
                        {
                            Success = false,
                            Recovered = false,
                            Message = "创建确认请求失败",
                            Answer = "这个操作需要确认，但系统暂时无法创建确认请求，请稍后重试。"
                        };
                    }

                    return new AgentAskResponse
                    {
                        Success = false,
                        Recovered = false,
                        RequiresConfirmation = true,
                        ConfirmationId = confirmationId,
                        ConfirmationSummary = BuildConfirmationSummary(plan),
                        Message = "该操作需要用户确认",
                        Answer = "这个操作需要你确认之后才能执行。"
                    };
                }   // 当最高风险动作需确认时，返回确认响应

                else
                {
                    return await ExecuteApprovedPlanAsync(plan, userMessage.Content, userId, userMessage.SessionId);
                }    // 正常执行plan

            }
        }

        // 生成补救计划的辅助方法，限制尝试次数，避免过度复杂化
        private async Task<AgentPlan> CreateRecoveryPlanWithLimitAsync(
            string userMessage,
            AgentPlan plan,
            AgentPlanExecutionResult executionResult)
        {
            AgentPlan? recoveryPlan = null;

            for (var attempt = 1; attempt <= MaxRecoveryPlanAttempts; attempt++)
            {
                _logger.LogInformation(
                    "开始生成第 {Attempt} 次补救计划，最多 {MaxAttempts} 次。",
                    attempt,
                    MaxRecoveryPlanAttempts);

                recoveryPlan = await _agentReplannerService.CreateRecoveryPlanAsync(
                    userMessage,
                    plan,
                    executionResult);

                if (recoveryPlan != null && recoveryPlan.Steps.Count > 0)
                {
                    return recoveryPlan;
                }
            }

            return new AgentPlan
            {
                Goal = userMessage,
                Steps = new List<AgentPlanStep>()
            };
        }

        // 构建 Agent 工作流输入消息，包含用户问题、记忆增强内容和最近的对话上下文，格式化为清晰的文本块，方便规划和执行使用
        private static string BuildAgentInputMessage
        (
            string currentUserMessage,
            string memoryAugmentedMessage,
            string recentConversationContext
        )
        {
            var sections = new List<string>();

            sections.Add("""
            【上下文使用规则】
            1. 当前用户问题是最高优先级。
            2. 如果长期记忆和最近对话都包含同一信息，优先使用长期记忆中的结构化字段。
            3. 如果用户使用“它、这篇、那篇、刚才那篇”等指代表达，优先结合长期记忆中的文章ID和标题理解。
            4. 最近对话只用于辅助理解上下文，不要把历史 Assistant 回答当成新的用户任务。
            """);

            if (!string.IsNullOrWhiteSpace(memoryAugmentedMessage))
            {
                sections.Add
                (
                    $"""
                    【长期记忆】
                    {memoryAugmentedMessage}
                    """
                );
            }

            if (!string.IsNullOrWhiteSpace(recentConversationContext))
            {
                sections.Add
                (
                    $"""
                    【最近对话】
                    {recentConversationContext}
                    """
                );
            }

            sections.Add
            (
                $"""
                【当前用户问题】{currentUserMessage}
                """
            );

            return string.Join("\n\n", sections);
        }

        // 根据工作流是否正常完成，判断是否写入记忆
        private static bool ShouldUpdateMemory(AgentAskResponse response)
        {
            if (response == null)
            {
                return false;
            }

            if (!response.Success)
            {
                return false;
            }

            if (response.Recovered)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(response.Answer)) 
            { 
                return false; 
            }

            return true;
        }

        // 对计划中含有的被禁止动作执行失败响应返回
        private static AgentAskResponse CreateForbiddenActionResponse()
        {
            return new AgentAskResponse
            {
                Success = false,
                Recovered = false,
                Message = "计划中包含不允许执行的动作！",
                Answer =
                    """
                    抱歉，这个请求中包含当前 Agent 不允许执行的操作。

                    为了安全起见，我不会执行未知动作或高风险动作。
                    你可以换一种查询、总结、对比或文章问答类的问题再试一次。
                    """
            }; 
        }

        // 根据计划中的动作列表生成用户确认摘要
        private static string BuildConfirmationSummary(AgentPlan plan)
        {
            // 计划无步骤时返回通用提示
            if (plan.Steps == null || plan.Steps.Count == 0)
            {
                return "即将执行一个需要确认的操作。";
            }

            // 提取所有不同的动作名称，并用顿号连接
            var actions = plan.Steps
                .Select(x => x.Action)
                .Distinct();

            return $"即将执行以下操作：{string.Join("、", actions)}";
        }

        // 对批准执行的 plan 进行执行
        private async Task<AgentAskResponse> ExecuteApprovedPlanAsync(AgentPlan plan, string userMessage, int userId, string sessionId)
        {
            // 执行计划并获取执行结果
            var executionResult = await _agentPlanExecutor.ExecuteAsync(plan, userId, sessionId);

            // 检查执行结果中是否存在失败步骤
            var hasFailedStep = executionResult.StepResults.Any(s => !s.Success);

            // 如果没有失败步骤返回成功响应
            if (!hasFailedStep)  
            {
                return new AgentAskResponse
                {
                    Success = true,
                    Recovered = false,
                    Message = "Agent 执行成功",
                    Answer = executionResult.FinalAnswer,
                    Debug = new AgentDebugInfo
                    {
                        Plan = plan,
                        ExecutionResult = executionResult
                    }
                };
            }

            if (IsNonRecoverableExecutionFailure(executionResult))
            {
                var failedMessage = executionResult.StepResults
                    .Where(r => !r.Success)
                    .Select(r => r.Message)
                    .FirstOrDefault() ?? "任务执行失败。";
                
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = "Agent 执行被安全策略拦截",
                    Answer = failedMessage,
                    Debug = new AgentDebugInfo
                    {
                        Plan = plan,
                        ExecutionResult = executionResult
                    }
                };
            }

            // 如果有失败步骤，进行分析，恢复，验证，做二次执行，如果依旧无法执行，给出建议
            var failureAnalysis = await _failureAnalyzer.AnalyzeFailureAsync(plan, executionResult); // 获取对失败步骤的分析结果

            var recoveryPlan = await CreateRecoveryPlanWithLimitAsync(userMessage, plan, executionResult); // 尝试对计划进行修复

            var recoverdPlanValidationResult = _agentPlanValidator.ValidateRecoveryPlan(recoveryPlan, executionResult); // 对修复后的计划执行验证

            // 如果恢复后的计划未通过验证，返回失败响应
            if (!recoverdPlanValidationResult.IsValid)
            {
                return new AgentAskResponse
                {
                    Success = false,
                    Recovered = false,
                    Message = "Agent 执行失败！且补救计划未通过校验！",
                    Answer = failureAnalysis,

                    // 创建调试日志
                    Debug = new AgentDebugInfo
                    {
                        Plan = plan,
                        ExecutionResult = executionResult,
                        FailureAnalysis = failureAnalysis,
                        RecoveryPlan = recoveryPlan,
                        RecoveryErrors = recoverdPlanValidationResult.Errors
                    }
                };
            }

            // 如果恢复后的计划通过验证，则继续执行恢复后的计划，并获取执行结果
            var recoveryExecutionResult = await _agentPlanExecutor.ExecuteRecoveryAsync(recoveryPlan, executionResult, userId);
            var recoverySucceeded = recoveryExecutionResult.StepResults.All(s => s.Success);

            // 根据恢复计划执行结果返回不同响应
            return new AgentAskResponse
            {
                Success = false,
                Recovered = recoverySucceeded,
                Message = recoverySucceeded
                    ? "Agent 原任务执行失败，但已生成补救建议"
                    : "Agent 原任务执行失败，补救计划也执行失败",
                Answer = recoveryExecutionResult.FinalAnswer,
                Debug = new AgentDebugInfo
                {
                    Plan = plan,
                    ExecutionResult = executionResult,
                    FailureAnalysis = failureAnalysis,
                    RecoveryPlan = recoveryPlan,
                    RecoveryExecutionResult = recoveryExecutionResult
                }
            };
        }

        // 对 Agent 执行结果进行最终处理：安全检测、日志保存、消息记录、记忆更新、会话压缩，并根据 debug 标志决定是否保留调试信息
        private async Task<AgentAskResponse> FinalizeAgentResponseAsync(
            AgentAskResponse response,
            string userMessage,
            string sessionId,
            int userId,
            DateTime startedAt,
            bool debug)
        {
            var finishedAt = DateTime.UtcNow;

            // AIShield 输出安全检测
            var outputCheck = await _aiShieldService.CheckOutputAsync(response.Answer, userId);

            // 检测到违规内容则拦截并修改响应
            if (outputCheck.ShouldBlock())
            {
                response.Success = false;
                response.Recovered = false;
                response.Message = $"AIShield 输出检测拦截：{outputCheck.Reason}";
                response.Answer = "模型输出存在安全风险，已被系统拦截。";
            }
            else if (!string.IsNullOrWhiteSpace(outputCheck.ProcessedContent))
            {
                // 如有处理后的安全内容则替换原回答
                response.Answer = outputCheck.ProcessedContent;
            }

            // 保存工作流日志
            try
            {
                var saveResponse = await _agentWorkflowLogService.SaveAsync(
                    userMessage,
                    response,
                    startedAt,
                    finishedAt);

                if (!saveResponse.Success)
                {
                    _logger.LogError("保存 AgentWorkflow 日志失败：{Message}", saveResponse.Message);
                }

                else if (saveResponse.Data is int workflowLogId)
                {
                    response.WorkflowLogId = workflowLogId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存 AgentWorkflow 日志时发生异常。用户问题：{Message}", userMessage);
            }

            // 保存助手消息并更新会话时间（需要 sessionId）
            if (!string.IsNullOrWhiteSpace(sessionId) && !response.RequiresConfirmation)
            {
                try
                {
                    // 保存助手消息
                    var saveAssistantResponse = await _agentMessageService.SaveAgentMessageAsync(
                        sessionId,
                        response.Answer);

                    if (!saveAssistantResponse.Success)
                    {
                        _logger.LogError(
                            "保存 Agent 消息失败：{Message}",
                            saveAssistantResponse.Message);
                    }

                    // 更新会话时间
                    var touchResponse = await _agentMessageService.TouchConversationAsync(sessionId);

                    if (!touchResponse.Success)
                    {
                        _logger.LogError(
                            "更新会话时间失败：{Message}",
                            touchResponse.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "保存 Assistant 消息或更新会话时间时发生异常。SessionId：{SessionId}",
                        sessionId);
                }
            }

            // 更新会话记忆（需要 sessionId 且响应成功或有恢复信息）
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                try
                {
                    // 优先取执行结果，若不存在则取恢复执行结果
                    var executionResult = response.Debug?.ExecutionResult
                        ?? response.Debug?.RecoveryExecutionResult;

                    if (ShouldUpdateMemory(response))
                    {
                        var updated = await _memoryService.UpdateConversationMemoryAfterWorkflowAsync(
                            sessionId,
                            userMessage,
                            response.Answer,
                            executionResult);

                        if (!updated)
                        {
                            _logger.LogWarning(
                                "更新 Agent 会话记忆失败。SessionId：{SessionId}",
                                sessionId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "更新 Agent 会话记忆时发生异常。SessionId：{SessionId}",
                        sessionId);
                }
            }

            // 尝试压缩会话历史摘要（需要 sessionId）
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                try
                {
                    var summarized = await _memoryService.TrySummarizeConversationAsync(sessionId);

                    if (summarized)
                    {
                        _logger.LogInformation(
                            "会话历史压缩成功！SessionId:{SessionId}",
                            sessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "会话历史压缩时发生异常。SessionId:{SessionId}",
                        sessionId);
                }
            }

            // 非调试模式下清空调试信息
            if (!debug)
            {
                response.Debug = null;
            }

            return response;
        }

        // 判断执行结果是否为不可恢复的失败（即无法通过重试或补救计划恢复的严重错误）
        private static bool IsNonRecoverableExecutionFailure(AgentPlanExecutionResult executionResult)
        {
            // 从所有失败的步骤中取第一条错误消息
            var failedMessage = executionResult.StepResults
                .Where(r => !r.Success)
                .Select(r => r.Message)
                .FirstOrDefault() ?? string.Empty;

            // 定义不可恢复失败的错误关键词列表（安全拦截、权限拒绝、非法操作等）
            var nonRecoverableKeywords = new[]
            {
                "未通过安全检查",
                "参数存在较高风险",
                "无权访问",
                "无权修改",
                "AIShield 工具调用检测拦截",
                "当前 Agent 不允许执行"
            };

            // 如果错误消息包含任一不可恢复关键词，返回 true
            return nonRecoverableKeywords.Any(keyword =>
                failedMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}
