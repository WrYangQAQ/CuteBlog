using CuteBlogSystem.AI.Planner;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CuteBlogSystem.Service
{
    // 负责按计划（AgentPlan）逐步执行操作，并收集每步结果
    public class AgentPlanExecutorService
    {
        private readonly ArticleService _articleService;
        private readonly CategoryService _categoryService;
        private readonly IChatClient _chatClient;
        private readonly ILogger<AgentPlanExecutorService> _logger;
        private readonly AIShieldService _aiShieldService;

        public AgentPlanExecutorService(
            ArticleService articleService,
            CategoryService categoryService,
            IChatClient chatClient,
            ILogger<AgentPlanExecutorService> logger,
            AIShieldService aiShieldService)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _chatClient = chatClient;
            _logger = logger;
            _aiShieldService = aiShieldService;
        }

        // 执行传入的计划，依次处理每个步骤，遇失败则提前返回(计划无补救，直接返回失败结果)
        public async Task<AgentPlanExecutionResult> ExecuteAsync(AgentPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var executionResult = new AgentPlanExecutionResult
            {
                Goal = plan.Goal
            };

            // 按 StepNumber 升序执行，保证步骤顺序
            foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
            {
                _logger.LogInformation(
                    "开始执行计划步骤：Step {StepNumber}, Action = {Action}",
                    step.StepNumber,
                    step.Action);

                AgentStepExecutionResult stepResult;

                // 在执行具体工具动作前，先调用 AIShield 检测工具调用是否安全
                var toolCheck = await CheckToolCallBeforeStepAsync(step);
                if (!toolCheck.Success)
                {
                    executionResult.StepResults.Add(toolCheck);
                    executionResult.FinalAnswer = $"执行计划失败：{toolCheck.Message}";
                    return executionResult;
                }

                // 根据动作名称分发到具体处理方法
                switch (step.Action)
                {
                    case AgentActionRegistry.SearchArticlesByCategory:
                        stepResult = await ExecuteSearchArticlesByCategoryAsync(step);
                        break;

                    case AgentActionRegistry.GetArticleContentById:
                        stepResult = await ExecuteGetArticleContentByIdAsync(step, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.SummarizeContent:
                        stepResult = await ExecuteSummarizeContentAsync(step, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.CompareContents:
                        stepResult = await ExecuteCompareContentsAsync(step, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.GetAllCategories:
                        stepResult = await ExecuteGetAllCategoriesAsync(step);
                        break;

                    case AgentActionRegistry.ExplainFailureWithSuggestions:
                        stepResult = await ExecuteExplainFailureWithSuggestionsAsync(step, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.AnswerQuestionFromContent:
                        stepResult = await ExecuteAnswerQuestionFromContentAsync(step, executionResult.StepResults);
                        break;

                    default:
                        stepResult = new AgentStepExecutionResult
                        {
                            StepNumber = step.StepNumber,
                            Action = step.Action,
                            Success = false,
                            Message = $"不支持的 Action：{step.Action}"
                        };
                        break;
                }

                executionResult.StepResults.Add(stepResult);

                // 如果某一步执行失败，终止整个计划，并设置最终错误答案
                if (!stepResult.Success)
                {
                    _logger.LogWarning(
                        "计划步骤执行失败：Step {StepNumber}, Action = {Action}, Message = {Message}",
                        step.StepNumber,
                        step.Action,
                        stepResult.Message);

                    executionResult.FinalAnswer = $"执行计划失败：{stepResult.Message}";
                    return executionResult;
                }
            }

            // 所有步骤成功，从最后一步成功结果中提取最终答案
            executionResult.FinalAnswer = await GenerateFinalAnswerAsync(executionResult.Goal, executionResult.StepResults);

            return executionResult;
        }

        // 执行补救计划，参数包含原失败执行结果，允许从中提取信息进行更准确的恢复分析和建议生成
        public async Task<AgentPlanExecutionResult> ExecuteRecoveryAsync(
            AgentPlan recoveryPlan,
            AgentPlanExecutionResult failedExecutionResult)
        {
            if (recoveryPlan == null)
            {
                throw new ArgumentNullException(nameof(recoveryPlan));
            }

            if (failedExecutionResult == null)
            {
                throw new ArgumentNullException(nameof(failedExecutionResult));
            }

            var executionResult = new AgentPlanExecutionResult
            {
                Goal = recoveryPlan.Goal
            };

            foreach (var step in recoveryPlan.Steps.OrderBy(s => s.StepNumber))
            {
                _logger.LogInformation(
                    "开始执行补救计划步骤：Step {StepNumber}, Action = {Action}",
                    step.StepNumber,
                    step.Action);

                AgentStepExecutionResult stepResult;

                // 在执行补救计划工具动作前，也需要经过 AIShield 检测
                var toolCheck = await CheckToolCallBeforeStepAsync(step);
                if (!toolCheck.Success)
                {
                    executionResult.StepResults.Add(toolCheck);
                    executionResult.FinalAnswer = $"补救计划执行失败：{toolCheck.Message}";
                    return executionResult;
                }

                switch (step.Action)
                {
                    case AgentActionRegistry.GetAllCategories:
                        stepResult = await ExecuteGetAllCategoriesAsync(step);
                        break;

                    case AgentActionRegistry.ExplainFailureWithSuggestions:
                        stepResult = await ExecuteExplainFailureWithSuggestionsAsync(
                            step,
                            executionResult.StepResults,
                            failedExecutionResult.StepResults);
                        break;

                    default:
                        stepResult = new AgentStepExecutionResult
                        {
                            StepNumber = step.StepNumber,
                            Action = step.Action,
                            Success = false,
                            Message = $"补救计划不支持的 Action：{step.Action}"
                        };
                        break;
                }

                executionResult.StepResults.Add(stepResult);

                if (!stepResult.Success)
                {
                    executionResult.FinalAnswer = $"补救计划执行失败：{stepResult.Message}";
                    return executionResult;
                }
            }

            executionResult.FinalAnswer = await GenerateFinalAnswerAsync(executionResult.Goal, executionResult.StepResults);

            return executionResult;
        }

        // ====================       以下是具体步骤的执行方法       ====================

        // 按分类查询文章列表，返回前 top 条
        private async Task<AgentStepExecutionResult> ExecuteSearchArticlesByCategoryAsync(AgentPlanStep step)
        {
            var categoryName = GetStringParam(step.Parameters, "categoryName");
            var top = GetIntParam(step.Parameters, "top", 5);
            var sortBy = GetStringParam(step.Parameters, "sortBy", "Latest");

            _logger.LogInformation(
                "执行 SearchArticlesByCategory，分类：{CategoryName}，数量：{Top}，排序：{SortBy}",
                categoryName,
                top,
                sortBy);

            var response = await _articleService.GetArticlesByCategoryNameAsync(categoryName, top, sortBy);

            if (!response.Success || response.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = response.Message
                };
            }

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章查询成功",
                Data = response.Data // 保存原始结果数据，供后续步骤提取ID
            };
        }

        // 从上一步结果中提取文章ID，然后获取文章正文内容
        private async Task<AgentStepExecutionResult> ExecuteGetArticleContentByIdAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            var articleId = GetIntParam(step.Parameters, "articleId");

            if(articleId <= 0)
            {
                // 参数指定文章ID来自哪个步骤的结果
                var fromStepNumber = GetIntParam(step.Parameters, "articleIdFromStep");

                var previousResult = previousResults
                    .FirstOrDefault(r => r.StepNumber == fromStepNumber);

                if (previousResult == null || previousResult.Data == null)
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = $"找不到第 {fromStepNumber} 步的结果，无法获取文章ID"
                    };
                }

                // 从文本中提取第一个文章ID（可能为JSON或特定格式）
                articleId = ExtractFirstArticleId(previousResult.Data);

                if (articleId <= 0)
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = $"无法从第 {fromStepNumber} 步结果中提取文章ID"
                    };
                }
            }

            _logger.LogInformation(
                "执行 GetArticleContentById，文章ID：{ArticleId}",
                articleId);

            var response = await _articleService.GetArticleContentByIdAsync(articleId);

            if (!response.Success || response.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = response.Message
                };
            }

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章正文获取成功",
                Data = response.Data
            };
        }

        // 使用AI对上一步获取的正文内容进行总结
        private async Task<AgentStepExecutionResult> ExecuteSummarizeContentAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            var fromStepNumber = GetIntParam(step.Parameters, "contentFromStep");

            var previousResult = previousResults
                .FirstOrDefault(r => r.StepNumber == fromStepNumber);

            if (previousResult == null || previousResult.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {fromStepNumber} 步的正文结果"
                };
            }

            // 将数据统一转换为文本供AI处理
            var contentText = ConvertObjectToText(previousResult.Data);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客文章总结助手。
                请根据用户博客文章内容进行总结。
                要求：
                1. 不要编造文章中没有的信息。
                2. 先概括文章主题。
                3. 再列出文章主要内容。
                4. 最后给出简短评价。
                """),

                new(ChatRole.User, contentText)
            };

            _logger.LogInformation("执行 SummarizeContent，正文来源步骤：{StepNumber}", fromStepNumber);

            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.SummaryMaxOutputTokens
                }
            );

            // 获取助手回复文本
            var summary = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? string.Empty;

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章总结成功",
                Data = summary // 将AI总结文本作为步骤数据
            };
        }

        // 对两篇文章内容进行比较，输出异同点
        private async Task<AgentStepExecutionResult> ExecuteCompareContentsAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            var contentFromStepA = GetIntParam(step.Parameters, "contentFromStepA");
            var contentFromStepB = GetIntParam(step.Parameters, "contentFromStepB");

            var resultA = previousResults
                .FirstOrDefault(r => r.StepNumber == contentFromStepA);

            var resultB = previousResults
                .FirstOrDefault(r => r.StepNumber == contentFromStepB);

            if (resultA == null || resultA.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {contentFromStepA} 步的正文内容"
                };
            }

            if (resultB == null || resultB.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {contentFromStepB} 步的正文内容"
                };
            }

            var contentA = ConvertObjectToText(resultA.Data);
            var contentB = ConvertObjectToText(resultB.Data);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客文章对比分析助手。
                请根据两篇文章的真实内容进行对比分析。

                要求：
                1. 不要编造文章中没有的信息。
                2. 先分别概括两篇文章的主题。
                3. 再对比两篇文章的内容侧重点。
                4. 再说明它们的相同点和不同点。
                5. 最后给出简短评价。
                6. 如果两篇文章其实是同一篇文章，要明确说明它们是同一篇文章，不要强行制造差异。
                """),

                new(ChatRole.User,
                $"""
                第一篇文章内容如下：

                {contentA}

                第二篇文章内容如下：

                {contentB}
                """)
            };

            _logger.LogInformation(
                "执行 CompareContents，正文来源步骤：{StepA} 和 {StepB}",
                contentFromStepA,
                contentFromStepB);

            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.CompareMaxOutputTokens
                }
            );

            var compareResult = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? string.Empty;

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章对比成功",
                Data = compareResult
            };
        }

        // 获取系统中所有文章分类，返回分类列表
        private async Task<AgentStepExecutionResult> ExecuteGetAllCategoriesAsync(AgentPlanStep step)
        {
            _logger.LogInformation("执行 GetAllCategories");

            var response = await _categoryService.GetAllCategoriesAsync();

            if (!response.Success || response.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = response.Message,
                    Data = null
                };
            }

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "分类获取成功",
                Data = response.Data
            };
        }

        // 根据失败步骤的结果和当前可用分类，使用 AI 生成失败原因分析和恢复建议
        private async Task<AgentStepExecutionResult> ExecuteExplainFailureWithSuggestionsAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            var failureFromStep = GetIntParam(step.Parameters, "failureFromStep");
            var categoriesFromStep = GetIntParam(step.Parameters, "categoriesFromStep");

            var failureResult = previousResults
                .FirstOrDefault(r => r.StepNumber == failureFromStep);

            var categoriesResult = previousResults
                .FirstOrDefault(r => r.StepNumber == categoriesFromStep);

            if (failureResult == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {failureFromStep} 步的失败结果"
                };
            }

            if (categoriesResult == null || categoriesResult.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {categoriesFromStep} 步的分类结果"
                };
            }

            var failureText = ConvertObjectToText(failureResult);
            var categoriesText = ConvertObjectToText(categoriesResult.Data);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客系统 Agent 执行恢复助手。
                用户的原始任务没有成功完成，现在你需要根据失败原因和当前可用分类，给用户一个清晰、友好的说明。

                要求：
                1. 不要编造不存在的文章。
                2. 明确说明原任务为什么没完成。
                3. 告诉用户当前可用的文章分类。
                4. 建议用户换一个已有分类继续查询。
                5. 语气自然、简洁。
                """),

                new(ChatRole.User,
                $"""
                失败步骤信息：
                {failureText}

                当前可用分类：
                {categoriesText}
                """)
            };

            _logger.LogInformation(
                "执行 ExplainFailureWithSuggestions，失败来源步骤：{FailureStep}，分类来源步骤：{CategoriesStep}",
                failureFromStep,
                categoriesFromStep);

            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.RecoverySuggestionMaxOutputTokens
                }
            );

            var answer = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? "任务执行失败，请换一个分类后重试。";

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "失败恢复建议生成成功",
                Data = answer
            };
        }

        // 重载版本：从两个不同来源的步骤结果中获取数据，生成更准确的失败分析和恢复建议
        private async Task<AgentStepExecutionResult> ExecuteExplainFailureWithSuggestionsAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> recoveryResults,
            List<AgentStepExecutionResult> failedOriginalResults)
        {
            var failureFromStep = GetIntParam(step.Parameters, "failureFromStep");
            var categoriesFromStep = GetIntParam(step.Parameters, "categoriesFromStep");

            var failureResult = failedOriginalResults
                .FirstOrDefault(r => r.StepNumber == failureFromStep);

            var categoriesResult = recoveryResults
                .FirstOrDefault(r => r.StepNumber == categoriesFromStep);

            if (failureResult == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到原始失败流程中的第 {failureFromStep} 步结果"
                };
            }

            if (categoriesResult == null || categoriesResult.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到补救计划中的第 {categoriesFromStep} 步分类结果"
                };
            }

            var failureText = ConvertObjectToText(failureResult);
            var categoriesText = ConvertObjectToText(categoriesResult.Data);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客系统 Agent 执行恢复助手。
                用户的原始任务没有成功完成，现在你需要根据失败原因和当前可用分类，给用户一个清晰、友好的说明。

                要求：
                1. 不要编造不存在的文章。
                2. 明确说明原任务为什么没完成。
                3. 告诉用户当前可用的文章分类。
                4. 建议用户换一个已有分类继续查询。
                5. 语气自然、简洁。
                """),

                new(ChatRole.User,
                $"""
                原始失败步骤信息：
                {failureText}

                当前可用分类：
                {categoriesText}
                """)
            };

            _logger.LogInformation(
                "执行 ExplainFailureWithSuggestions，原始失败步骤：{FailureStep}，分类来源步骤：{CategoriesStep}",
                failureFromStep,
                categoriesFromStep);

            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.RecoverySuggestionMaxOutputTokens
                }
            );

            var answer = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? "任务执行失败，请换一个分类后重试。";

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "失败恢复建议生成成功",
                Data = answer
            };
        }

        // 根据用户具体问题，从指定步骤的文章正文中提取答案
        private async Task<AgentStepExecutionResult> ExecuteAnswerQuestionFromContentAsync(
                AgentPlanStep step,
                List<AgentStepExecutionResult> previousResults)
        {
            // 从步骤参数中获取文章正文所在步骤编号
            var contentFromStep = GetIntParam(step.Parameters, "contentFromStep");

            // 获取用户要问的具体问题
            var question = GetStringParam(step.Parameters, "question");

            // 查找之前步骤中对应的正文结果
            var contentResult = previousResults.FirstOrDefault(
                result => result.StepNumber == contentFromStep);

            // 如果找不到正文数据，返回失败
            if (contentResult?.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {contentFromStep} 步的文章正文"
                };
            }

            // 校验问题不能为空
            if (string.IsNullOrWhiteSpace(question))
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "文章问题不能为空"
                };
            }

            // 将正文数据统一转换为纯文本（兼容各种对象类型）
            var contentText = ConvertObjectToText(contentResult.Data);

            // 构造 AI 对话消息：系统指令限定只根据文章内容回答问题
            var messages = new List<ChatMessage>
            {
                new(
                    ChatRole.System,
                    """
                    你是博客文章内容问答助手。

                    请只根据提供的文章内容回答用户的具体问题。

                    要求：
                    1. 针对用户的问题直接回答，不要擅自总结整篇文章。
                    2. 只能使用文章中存在的信息，不得编造。
                    3. 如果文章没有提到相关内容，要明确说明文章中未提及。
                    4. 回答应简洁、准确，可以引用必要的代码或要点。
                    5. 不要输出JSON，也不要描述内部执行过程。
                    """),

                new(
                    ChatRole.User,
                    $"""
                    文章内容：
                    {contentText}

                    用户问题：
                    {question}
                    """)
            };

            _logger.LogInformation(
                "执行 AnswerQuestionFromContent，正文来源步骤：{StepNumber}，问题：{Question}",
                contentFromStep,
                question);

            // 调用 AI 获取答案，限制输出 Token 数
            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens =
                        AgentTokenBudget.ContentQuestionAnswerMaxOutputTokens
                });

            // 提取助手的回答文本
            var answer = response.Messages
                .Where(message => message.Role == ChatRole.Assistant)
                .Select(message => message.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

            // 如果未生成有效答案，返回失败
            if (string.IsNullOrWhiteSpace(answer))
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "未能生成文章问题的回答"
                };
            }

            // 返回成功结果，将答案存入 Data
            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章问题回答成功",
                Data = answer
            };
        }

        //  ====================       以下是辅助方法       ====================

        // 在执行计划步骤前调用 AIShield 检测工具名称和参数
        private async Task<AgentStepExecutionResult> CheckToolCallBeforeStepAsync(AgentPlanStep step)
        {
            // 将 AgentPlanStep 的 Action 作为工具名称，Parameters 作为工具参数传给 AIShield
            var checkResult = await _aiShieldService.CheckToolCallAsync(step.Action, step.Parameters);

            // 检测通过时返回成功结果，调用方继续执行真实步骤
            if (!checkResult.ShouldBlock())
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = true,
                    Message = "AIShield 工具调用检测通过"
                };
            }

            // 检测不通过时返回失败步骤，调用方停止执行计划
            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = false,
                Message = $"AIShield 工具调用检测拦截：{checkResult.Reason}"
            };
        }

        // 从所有成功步骤中取最后一条有效数据作为最终答案
        // 根据执行结果生成最终回答，如果最后一步已经是自然语言则直接复用，否则调用 AI 整理
        private async Task<string> GenerateFinalAnswerAsync(
            string goal,
            List<AgentStepExecutionResult> stepResults)
        {
            // 从所有成功的步骤中，取最后一个包含有效数据的步骤作为最终结果的来源
            var lastSuccess = stepResults
                .LastOrDefault(result => result.Success && result.Data != null);

            // 如果没有任何成功的步骤包含数据，返回默认提示
            if (lastSuccess == null)
            {
                return "计划执行完成，但没有生成最终结果。";
            }

            // 这些 Action 的输出本身已经是自然语言，无需再让 AI 二次加工
            var naturalLanguageActions = new[]
            {
                AgentActionRegistry.SummarizeContent,
                AgentActionRegistry.CompareContents,
                AgentActionRegistry.ExplainFailureWithSuggestions,
                AgentActionRegistry.AnswerQuestionFromContent
            };

            // 如果最后一步是自然语言类型，直接将其 Data 转为文本返回
            if (naturalLanguageActions.Contains(lastSuccess.Action))
            {
                return ConvertObjectToText(lastSuccess.Data!);
            }

            // 构建包含所有步骤上下文的文本，供 AI 理解执行过程
            var executionContext = BuildFinalAnswerContext(stepResults);

            // 构造系统提示和用户消息，要求 AI 根据目标整理最终回答
            var messages = new List<ChatMessage>
            {
                new(
                    ChatRole.System,
                    """
                    你是博客系统 Agent 的最终回答生成器。

                    请将工具执行结果整理为简洁、自然、面向用户的中文回答。

                    要求：
                    1. 只能使用工具结果中已有的信息，不得编造。
                    2. 不要输出 JSON、Unicode 转义或代码块。
                    3. 不要提及步骤编号、Action、工具调用等内部实现。
                    4. 查询文章时，应说明文章标题、分类、点赞量等相关信息。
                    5. 如果只有一篇文章，使用自然语言直接介绍。
                    6. 回答必须与用户目标一致。
                    """),

                new(
                    ChatRole.User,
                    $"""
                    用户目标：
                    {goal}

                    工具执行结果：
                    {executionContext}
                    """)
            };

            // 调用 AI 生成最终回答，并指定输出 Token 上限
            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.FinalAnswerMaxOutputTokens
                });

            // 提取助手的回复文本，若为空则返回降级提示
            return response.Messages
                .Where(message => message.Role == ChatRole.Assistant)
                .Select(message => message.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))
                ?? "任务已执行完成，但暂时无法整理最终回答。";
        }

        // 将执行结果构建为供 AI 最终回答生成的上下文文本
        private static string BuildFinalAnswerContext(
            List<AgentStepExecutionResult> stepResults)
        {
            // 单步数据文本的最大字符数，防止上下文过长
            const int maxDataCharsPerStep = 4000;

            // 只保留成功且有数据的步骤，将每个步骤的数据转为文本片段
            var sections = stepResults
                .Where(result => result.Success && result.Data != null)
                .Select(result =>
                {
                    var dataText = ConvertObjectToText(result.Data!);

                    // 若数据过长则截断并添加提示
                    if (dataText.Length > maxDataCharsPerStep)
                    {
                        dataText = dataText[..maxDataCharsPerStep] + "\n...(内容已截断)";
                    }

                    return $"""
                    执行结果：
                    {dataText}
                    """;
                });

            // 将所有步骤的文本片段用空行分隔，形成完整上下文
            return string.Join("\n\n", sections);
        }

        // 安全地从参数字典中获取字符串值（兼容 JsonElement 类型）
        private static string GetStringParam(
            Dictionary<string, object> parameters,
            string key,
            string defaultValue = "")
        {
            if (!parameters.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            // 如果参数是反序列化后的 JsonElement，需要特殊处理
            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind == JsonValueKind.String
                    ? jsonElement.GetString() ?? defaultValue
                    : jsonElement.ToString();
            }

            return value.ToString() ?? defaultValue;
        }

        // 安全地从参数字典中获取整数值（兼容 JsonElement 与字符串数字）
        private static int GetIntParam(
            Dictionary<string, object> parameters,
            string key,
            int defaultValue = 0)
        {
            if (!parameters.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number &&
                    jsonElement.TryGetInt32(out var number))
                {
                    return number;
                }

                if (jsonElement.ValueKind == JsonValueKind.String &&
                    int.TryParse(jsonElement.GetString(), out var stringNumber))
                {
                    return stringNumber;
                }

                return defaultValue;
            }

            return int.TryParse(value.ToString(), out var result)
                ? result
                : defaultValue;
        }

        // 从对象文本中提取第一个出现的文章ID，匹配中文提示或JSON属性
        private static int ExtractFirstArticleId(object data)
        {
            var text = ConvertObjectToText(data);

            var patterns = new[]
            {
                @"文章ID[:：]\s*(\d+)",   // 匹配“文章ID：123” 或 “文章ID: 123”
                @"""id""\s*:\s*(\d+)",   // 匹配JSON中的 "id": 123
                @"""Id""\s*:\s*(\d+)"    // 匹配JSON中的 "Id": 123
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);

                if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
                {
                    return id;
                }
            }

            return 0; // 未找到则返回0表示无效ID
        }

        // 将任意对象转换为字符串表示：字符串直接返回，否则序列化为JSON
        private static string ConvertObjectToText(object data)
        {
            if (data is string text)
            {
                return text;
            }

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
