using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.AI.Tools;
using CuteBlogSystem.DTO;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO.AgentAction;
using CuteBlogSystem.DTO.Blog;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Util;
using Microsoft.Extensions.AI;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CuteBlogSystem.Service
{
    // 负责按计划（AgentPlan）逐步执行操作，并收集每步结果
    public class AgentPlanExecutorService
    {
        private readonly ArticleService _articleService;
        private readonly CategoryService _categoryService;
        private readonly UserService _userService;
        private readonly IChatClient _chatClient;
        private readonly ILogger<AgentPlanExecutorService> _logger;
        private readonly AIShieldService _aiShieldService;
        private readonly AgentParameterRiskService _paramRiskService;
        private readonly AgentConversationMemoryRepository _memoryRepository;

        public AgentPlanExecutorService(
            ArticleService articleService,
            CategoryService categoryService,
            UserService userService,
            IChatClient chatClient,
            ILogger<AgentPlanExecutorService> logger,
            AIShieldService aiShieldService,
            AgentParameterRiskService paramRiskService,
            AgentConversationMemoryRepository memoryRepository)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _userService = userService;
            _chatClient = chatClient;
            _logger = logger;
            _aiShieldService = aiShieldService;
            _paramRiskService = paramRiskService;
            _memoryRepository = memoryRepository;
        }

        // 执行传入的计划，依次处理每个步骤，遇失败则提前返回(计划无补救，直接返回失败结果)
        public async Task<AgentPlanExecutionResult> ExecuteAsync(AgentPlan plan, int userId, string sessionId)
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

                    case AgentActionRegistry.GetMyArticles:
                        stepResult = await ExecuteGetMyArticlesAsync(step, userId);
                        break;

                    case AgentActionRegistry.UpdateArticleTitle:
                        stepResult = await ExecuteUpdateArticleTitleAsync(step, userId, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.GenerateContentRevision:
                        stepResult = await ExecuteGenerateContentRevisionAsync(step, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.UpdateArticleContent:
                        stepResult = await ExecuteUpdateArticleContentAsync(step, userId, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.DeleteArticle:
                        stepResult = await ExecuteDeleteArticleAsync(step, userId, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.ExplainFailureWithSuggestions:
                        stepResult = await ExecuteExplainFailureWithSuggestionsAsync(step, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.AnswerQuestionFromContent:
                        stepResult = await ExecuteAnswerQuestionFromContentAsync(step, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.SelectArticleFromList:
                        stepResult = await ExecuteSelectArticleFromListAsync(step, executionResult.StepResults, sessionId);
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
            AgentPlanExecutionResult failedExecutionResult,
            int userId)
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

                    case AgentActionRegistry.SearchArticlesByCategory:
                        stepResult = await ExecuteSearchArticlesByCategoryAsync(step);
                        break;

                    case AgentActionRegistry.GetArticleContentById:
                        stepResult = await ExecuteGetArticleContentByIdAsync(step, executionResult.StepResults);
                        break;

                    case AgentActionRegistry.GetMyArticles:
                        stepResult = await ExecuteGetMyArticlesAsync(step, userId);
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
            var input = ParseSearchArticlesByCategoryInput(step.Parameters);

            _logger.LogInformation(
                "执行 SearchArticlesByCategory，分类：{CategoryName}，数量：{Top}，排序：{SortBy}",
                input.CategoryName,
                input.Top,
                input.SortBy);

            var response = await _articleService.GetArticlesByCategoryNameAsync(
                input.CategoryName,
                input.SortBy);

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

            // 将服务层返回的data（GetArticleListDTO列表）转化为Action的Output DTO
            if (response.Data is List<GetArticleListDTO> articleLists)
            {
                var items = articleLists.Select(article => new ArticleSearchResultItem(article)).ToList();

                int totalCount = items.Count;

                // 按照要求对文章列表进行排序
                switch (input.SortBy)
                {
                    case ArticleSortBy.Latest:
                        items = items.OrderByDescending(a => a.Id).ToList();
                        break;

                    case ArticleSortBy.MostLiked:
                        items = items.OrderByDescending(a => a.LikeCount).ToList();
                        break;

                    case ArticleSortBy.MostViewed:
                        items = items.OrderByDescending(a => a.ViewCount).ToList();
                        break;
                }

                // 取出前 top 条
                items = items.Take(input.Top).ToList();

                var output = new SearchArticlesByCategoryOutput
                {
                    CategoryName = input.CategoryName,
                    Articles = items,
                    SortBy = input.SortBy,
                    TotalCount = totalCount
                };

                var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.SearchArticlesByCategory).ToList();

                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = true,
                    Message = "文章查询成功",
                    Data = output, // 保存原始结果数据，供后续步骤提取ID
                    MemoryFacts = memoryFacts
                };
            }
            else
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "文章查询失败",
                    Data = null
                };
            }
            
        }

        // 从上一步结果中提取文章 ID，然后获取文章正文内容
        private async Task<AgentStepExecutionResult> ExecuteGetArticleContentByIdAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            var articleId = GetIntParam(step.Parameters, "articleId");

            if (articleId <= 0)
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

            if (response.Data is not DisplayArticleDTO article)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "文章正文获取失败：返回数据格式非法"
                };
            }

            var output = new GetArticleContentByIdOutput(articleId, article);
            var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.GetArticleContentById).ToList();

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章正文获取成功",
                Data = output,
                MemoryFacts = memoryFacts
            };
        }

        // 执行 SummarizeContent 动作：支持直接传入正文或从前置步骤引用，使用 AI 生成文章摘要，并附带记忆事实
        private async Task<AgentStepExecutionResult> ExecuteSummarizeContentAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            // 构造输入对象，统一使用 input 保存动作参数
            var input = new SummarizeContentInput
            {
                Content = GetStringParam(step.Parameters, "content", string.Empty),
                ContentFromStep = GetIntParam(step.Parameters, "contentFromStep")
            };

            AgentMemoryFact? sourceArticleFact = null;

            // 未直接提供正文时，从前置步骤中获取正文
            if (string.IsNullOrWhiteSpace(input.Content))
            {
                var previousResult = previousResults
                    .FirstOrDefault(result => result.StepNumber == input.ContentFromStep);

                // 如果找不到对应的步骤结果，返回失败
                if (previousResult == null || previousResult.Data == null)
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = $"找不到第 {input.ContentFromStep} 步的正文结果"
                    };
                }
                else
                {
                    // 从上游执行结果获取记忆事实
                    if (previousResult.MemoryFacts != null)
                    {
                        sourceArticleFact = previousResult.MemoryFacts
                            .LastOrDefault(f =>
                                f.ArticleId.HasValue &&
                                !string.IsNullOrWhiteSpace(f.ArticleTitle) &&
                                (f.Type == ArticleMemoryType.ArticleSelected ||
                                 f.Type == ArticleMemoryType.ArticleUpdated ||
                                 f.Type == ArticleMemoryType.ArticleMentioned));
                    }

                    // 从前置步骤的数据中提取文本内容
                    input.Content = ExtractContentText(previousResult.Data);
                }
            }

            // 未指定有效前置步骤编号时，将其统一设置为 null
            input.ContentFromStep = input.ContentFromStep > 0
                ? input.ContentFromStep
                : null;

            // 构造 AI 对话消息
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

                new(ChatRole.User, "以下是文章内容：\n" + input.Content)
            };

            _logger.LogInformation("执行 SummarizeContent，正文来源步骤：{StepNumber}", input.ContentFromStep);

            // 调用 AI 生成摘要，限制输出 Token 数
            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.SummaryMaxOutputTokens
                }
            );

            // 提取助手回复文本
            var summary = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? string.Empty;

            // 构造输出 DTO，包含摘要和长度信息
            var output = new SummarizeContentOutput
            {
                Summary = summary,
                SummaryLength = summary.Length,
                OriginalContentLength = input.Content.Length,
                SourceArticleId = sourceArticleFact?.ArticleId,
                SourceArticleTitle = sourceArticleFact?.ArticleTitle,
                SourceCategoryName = sourceArticleFact?.CategoryName
            };

            // 构造当前Action的记忆事实
            var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.SummarizeContent).ToList();

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章总结成功",
                Data = output, // 将输出对象存入 Data，包含摘要和长度信息
                MemoryFacts = memoryFacts
            };
        }

        // 执行 CompareContents 动作：对两篇文章进行 AI 对比分析，支持指定比较重点方向
        private async Task<AgentStepExecutionResult> ExecuteCompareContentsAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            // 构造输入对象，统一保存动作参数
            var input = new CompareContentsInput
            {
                ContentFromStepA = GetIntParam(step.Parameters, "contentFromStepA"),
                ContentFromStepB = GetIntParam(step.Parameters, "contentFromStepB"),
                CompareFocus = GetStringParam(
                    step.Parameters,
                    "compareFocus",
                    string.Empty)
            };

            // 从之前步骤结果中查找第一篇文章的正文数据
            var resultA = previousResults.FirstOrDefault(
                result => result.StepNumber == input.ContentFromStepA);

            // 若第一篇文章正文缺失，返回失败
            if (resultA == null || resultA.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {input.ContentFromStepA} 步的正文内容"
                };
            }

            // 从之前步骤结果中查找第二篇文章的正文数据
            var resultB = previousResults.FirstOrDefault(
                result => result.StepNumber == input.ContentFromStepB);

            // 若第二篇文章正文缺失，返回失败
            if (resultB == null || resultB.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {input.ContentFromStepB} 步的正文内容"
                };
            }

            // 从上游执行结果中获取记忆事实
            var sourceArticleFactA = ExtractArticleFactFromStepResult(resultA);
            var sourceArticleFactB = ExtractArticleFactFromStepResult(resultB);

            // 从步骤数据中提取纯文本内容
            input.ContentA = ExtractContentText(resultA.Data);
            input.ContentB = ExtractContentText(resultB.Data);

            // 未指定比较重点时统一设置为 null
            input.CompareFocus = string.IsNullOrWhiteSpace(input.CompareFocus)
                ? null
                : input.CompareFocus;

            // 根据是否指定比较重点，生成 AI 提示中的聚焦方向文本
            string focusText = string.IsNullOrWhiteSpace(input.CompareFocus)
                ? "用户没有指定特别比较方向，请从主题、内容重点、相同点、不同点和适用读者角度进行比较。"
                : $"用户希望重点比较：{input.CompareFocus}";

            // 构造 AI 对比分析对话消息
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                $"""
                你是一个博客文章对比分析助手。
                请根据两篇文章的真实内容进行对比分析。

                要求：
                1. 不要编造文章中没有的信息。
                2. 先分别概括两篇文章的主题。
                3. 再对比两篇文章的内容侧重点。
                4. 再说明它们的相同点和不同点。
                5. 最后给出简短评价。
                6. 如果两篇文章其实是同一篇文章，要明确说明它们是同一篇文章，不要强行制造差异。

                比较重点方向：
                - {focusText}
                """),

                new(ChatRole.User,
                $"""
                第一篇文章内容如下：

                {input.ContentA}

                第二篇文章内容如下：

                {input.ContentB}
                """)
            };

            _logger.LogInformation(
                "执行 CompareContents，正文来源步骤：{StepA} 和 {StepB}",
                input.ContentFromStepA,
                input.ContentFromStepB);

            // 调用 AI 生成对比结果，限制输出 Token 数
            var response = await _chatClient.GetResponseAsync
            (
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.CompareMaxOutputTokens
                }
            );

            // 提取助手的回复文本作为对比结果
            var compareResult = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? string.Empty;

            // 构造输出 DTO，包含对比结果和长度信息
            var output = new CompareContentsOutput
            {
                Comparison = compareResult,
                ContentALength = input.ContentA.Length,
                ContentBLength = input.ContentB.Length,
                ComparisonLength = compareResult.Length,
                CompareFocus = input.CompareFocus,

                ArticleAId = sourceArticleFactA?.ArticleId,
                ArticleATitle = sourceArticleFactA?.ArticleTitle,
                ArticleACategoryName = sourceArticleFactA?.CategoryName,

                ArticleBId = sourceArticleFactB?.ArticleId,
                ArticleBTitle = sourceArticleFactB?.ArticleTitle,
                ArticleBCategoryName = sourceArticleFactB?.CategoryName
            };

            // 获取Action执行的记忆事实
            var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.CompareContents).ToList();

            // 返回成功结果，Data 为输出对象
            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章对比分析成功",
                Data = output,
                MemoryFacts = memoryFacts
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

            if (response.Data is List<GetCategoryDTO> dtoList)
            {
                var output = new GetAllCategoriesOutput(dtoList);
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = true,
                    Message = "分类获取成功",
                    Data = output
                };
            }
            else
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = true,
                    Message = "在将查询出的分类列表转为数据传输对象时失败！",
                    Data = null
                };
            }
        }

        // 获取当前用户的所有文章列表，返回文章简要信息
        private async Task<AgentStepExecutionResult> ExecuteGetMyArticlesAsync(AgentPlanStep step, int userId)
        {
            var sortByText = GetStringParam(step.Parameters, "sortBy", "Latest");
            var input = new GetMyArticlesInput
            {
                UserId = userId,
                Top = GetIntParam(step.Parameters, "top", 10),
                SortBy = AiHelper.NormalizeSortBy(sortByText)
            };

            _logger.LogInformation(
                "执行 GetMyArticles，用户ID：{UserId}，数量：{Top}，排序：{SortBy}",
                input.UserId,
                input.Top,
                input.SortBy);

            var response = await _userService.GetMyArticlesAsync(userId);

            if (!response.Success || response.Data is not List<GetArticleListDTO> articles)
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

            var sortedArticles = input.SortBy switch
            {
                ArticleSortBy.MostLiked => articles.OrderByDescending(a => a.LikeCount),
                ArticleSortBy.MostViewed => articles.OrderByDescending(a => a.ViewCount),
                _ => articles.OrderByDescending(a => a.CreatedAt)
            };

            var items = sortedArticles
                .Take(input.Top)
                .Select(article => new ArticleSearchResultItem(article))
                .ToList();

            var output = new GetMyArticlesOutput
            {
                Articles = items,
                TotalCount = articles.Count
            };

            var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.GetMyArticles).ToList();

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "获取用户文章列表成功",
                Data = output,
                MemoryFacts = memoryFacts
            };
        }

        // 执行 UpdateArticleTitle 动作：根据文章 ID 和新标题更新文章，支持通过前置步骤引用文章 ID
        private async Task<AgentStepExecutionResult> ExecuteUpdateArticleTitleAsync(AgentPlanStep step, int userId,
            List<AgentStepExecutionResult> previousResults)
        {
            // 构造输入对象，从参数字典中读取直接值和引用步骤编号
            var input = new UpdateArticleTitleInput
            {
                ArticleId = GetIntParam(step.Parameters, "articleId"),
                ArticleIdFromStep = GetIntParam(step.Parameters, "articleIdFromStep"),
                NewTitle = GetStringParam(step.Parameters, "newTitle")
            };

            // 如果直接提供的 ArticleId 无效，且存在有效的引用步骤编号，尝试从前置步骤提取文章 ID
            if (input.ArticleId <= 0 && input.ArticleIdFromStep > 0)
            {
                var sourceResult = previousResults.FirstOrDefault(r => r.StepNumber == input.ArticleIdFromStep && r.Success);
                if (sourceResult?.Data != null)
                {
                    input.ArticleId = ExtractFirstArticleId(sourceResult.Data);
                }
            }

            // 记录操作日志
            _logger.LogInformation(
                "执行 UpdateArticleTitle，用户ID：{UserId}，文章ID：{ArticleId}，新标题：{NewTitle}",
                userId,
                input.ArticleId,
                input.NewTitle);

            // 调用业务服务执行标题更新
            var response = await _articleService.UpdateArticleTitleAsync(input.ArticleId, input.NewTitle, userId);

            // 服务层返回失败，透传错误信息
            if (!response.Success)
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

            // 若服务层返回的数据是预期 DTO，则构造输出对象
            if (response.Data is UpdateArticleTitleDTO dto)
            {
                var output = new UpdateArticleTitleOutput(dto);
                var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.UpdateArticleTitle).ToList();
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = true,
                    Message = response.Message,
                    Data = output,
                    MemoryFacts = memoryFacts
                };
            }

            // 数据转换失败，返回错误
            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = false,
                Message = "将服务层输出数据转化为Action DTO失败！",
                Data = null
            };
        }

        // 根据原文和修改指令，使用 AI 生成修订后的内容
        private async Task<AgentStepExecutionResult> ExecuteGenerateContentRevisionAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            var input = new GenerateContentRevisionInput
            {
                OriginalContent = GetStringParam(step.Parameters, "originalContent"),
                ContentFromStep = GetIntParam(step.Parameters, "contentFromStep"),
                Instruction = GetStringParam(step.Parameters, "instruction", "请优化文章表达，使其更清晰、流畅。")
            };

            AgentMemoryFact? sourceArticleFact = null;

            // 优先从参数中直接获取原文内容
            if (string.IsNullOrWhiteSpace(input.OriginalContent))
            {
                // 若没有直接提供，则尝试从指定的上一步结果中提取
                var previousResult = previousResults.FirstOrDefault(r => r.StepNumber == input.ContentFromStep);
                if (previousResult == null || previousResult.Data == null)
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = "无法获取待修改的原文内容"
                    };
                }
                input.OriginalContent = ExtractContentText(previousResult.Data);
                sourceArticleFact = ExtractArticleFactFromStepResult(previousResult);
            }

            // 检测修改指令是否具有风险
            var instructionRiskResult = _paramRiskService.ValidateContentRevisionInstruction(
                input.Instruction,
                step.StepNumber);

            if (!instructionRiskResult.IsSafe)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "修订指令未通过安全检查：" + string.Join("；", instructionRiskResult.Errors)
                };
            }

            
            // 构造 AI 对话消息：系统提示指定编辑角色，用户消息包含原文和指令
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一位专业文章编辑。根据用户指令修改文章内容。
                要求：
                1. 保留原文核心观点和事实。
                2. 仅输出修改后的完整文章，不要附加任何解释、前言或后记。
                3. 严格遵循用户修改指令。
                4. 输出格式必须为 Markdown 纯文本，并确保所有 Markdown 语法正确，能够被标准渲染器正常渲染。
                """),
                new(ChatRole.User,
                $"""
                原文：
                {input.OriginalContent}

                修改指令：
                {input.Instruction}
                """)
            };

            // 调用 AI 生成修订内容，适当调大输出 Token 限制
            var response = await _chatClient.GetResponseAsync(messages,
                new ChatOptions { MaxOutputTokens = AgentTokenBudget.ContentPolishMaxOutputTokens });

            // 提取助手回复的修订文本
            var revised = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? string.Empty;

            // 若未生成有效修订，返回失败
            if (string.IsNullOrWhiteSpace(revised))
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "AI 未能生成修订内容"
                };

            // 构造输出数据集
            var output = new GenerateContentRevisionOutput
            {
                Instruction = input.Instruction,
                OriginalContentLength = input.OriginalContent.Length,
                RevisedContent = revised,
                RevisedContentLength = revised.Length,
                SourceArticleId = sourceArticleFact?.ArticleId,
                SourceArticleTitle = sourceArticleFact?.ArticleTitle,
                SourceCategoryName = sourceArticleFact?.CategoryName
            };

            // 根据当前执行输出构造记忆事实
            var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.GenerateContentRevision).ToList();

            // 返回成功结果，将修订内容存入 Data
            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章修订内容生成成功",
                Data = output,
                MemoryFacts = memoryFacts
            };
        }

        // 根据文章 ID 和新的内容更新文章
        private async Task<AgentStepExecutionResult> ExecuteUpdateArticleContentAsync(
            AgentPlanStep step,
            int userId,
            List<AgentStepExecutionResult> previousResults)
        {
            var input = new UpdateArticleContentInput
            {
                // 获取文章 ID：优先从参数直接读取，若无效则尝试从前置步骤提取
                ArticleId = GetIntParam(step.Parameters, "articleId"), 
                ArticleIdFromStep = GetIntParam(step.Parameters, "articleIdFromStep"),

                // 获取新内容：优先从参数直接读取，若无效则尝试从前置步骤提取
                NewContent = GetStringParam(step.Parameters, "newContent"),     
                NewContentFromStep = GetIntParam(step.Parameters, "newContentFromStep")
            };
            
            if (input.ArticleId <= 0)
            {
                var fromStep = GetIntParam(step.Parameters, "articleIdFromStep");
                if (fromStep > 0)
                {
                    var prev = previousResults.FirstOrDefault(r => r.StepNumber == fromStep);
                    if (prev?.Data != null)
                    {
                        input.ArticleId = ExtractFirstArticleId(prev.Data);
                    }    
                }
            }
            if (input.ArticleId <= 0)
            {
                return FailResult(step, "无法确定要修改的文章ID");
            }


            if (string.IsNullOrWhiteSpace(input.NewContent))
            {
                var fromStep = GetIntParam(step.Parameters, "newContentFromStep");
                if (fromStep > 0)
                {
                    var prev = previousResults.FirstOrDefault(r => r.StepNumber == fromStep && r.Success);
                    if (prev?.Data != null)
                    {
                        input.NewContent = ExtractContentText(prev.Data);
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(input.NewContent))
                return FailResult(step, "没有可写入的新文章内容");

            // 对计划进行执行时校验，检测拿到的新内容是否具有风险性
            var contentRiskResult = _paramRiskService.ValidateResolvedArticleContent(input.NewContent, step.StepNumber);

            if (!contentRiskResult.IsSafe)
            {
                return FailResult(
                    step,
                    "生成的新文章内容未通过安全检查：" + string.Join("；", contentRiskResult.Errors));
            }

            // 调用业务服务执行更新操作
            var response = await _articleService.UpdateArticleContentAsync(input.ArticleId, input.NewContent, userId);

            if (response != null && response.Success)
            {
                if (response.Data is UpdateArticleInformation dto)
                {
                    var output = new UpdateArticleContentOutput(dto);
                    var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.UpdateArticleContent).ToList();

                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = response.Success,
                        Message = response.Message,
                        Data = output,
                        MemoryFacts = memoryFacts
                    };
                }
                else
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = "在将执行结果转化为数据传输集合时出错！",
                        Data = null
                    };
                }
            }
            else
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = response?.Message ?? "在将执行更新操作时出错！",
                    Data = null
                };
            }
            
        }

        // 删除指定文章
        private async Task<AgentStepExecutionResult> ExecuteDeleteArticleAsync(
            AgentPlanStep step,
            int userId,
            List<AgentStepExecutionResult> previousResults)
        {
            var input = new DeleteArticleInput
            {
                ArticleId = GetIntParam(step.Parameters, "articleId")
            };


            if (input.ArticleId <= 0)
            {
                input.ArticleIdFromStep = GetIntParam(step.Parameters, "articleIdFromStep");
                if (input.ArticleIdFromStep > 0)
                {
                    var previousResult = previousResults
                        .FirstOrDefault(r => r.StepNumber == input.ArticleIdFromStep);

                    if (previousResult?.Data != null)
                    {
                        input.ArticleId = ExtractFirstArticleId(previousResult.Data);
                    }
                }
            }

            // 若仍无法获取有效的文章 ID，返回失败
            if (input.ArticleId <= 0)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "无法确定要删除的文章 ID，请提供 articleId 或有效的 articleIdFromStep。"
                };
            }

            // 记录删除操作日志（敏感操作，需记录）
            _logger.LogWarning(
                "执行 DeleteArticle，用户ID：{UserId}，准备删除文章ID：{ArticleId}",
                userId,
                input.ArticleId);

            // 调用 Service 执行删除（需要在 ArticleService 中实现该方法）
            var response = await _articleService.DeleteArticleAsync(input.ArticleId, userId);

            if (response.Data is DeleteArticleInformation dto)
            {
                var output = new DeleteArticleOutput(dto);
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = response.Success,
                    Message = response.Message,
                    Data = output // 返回被删除文章的简要信息（如标题），用于最终回答
                };
            }
            else
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "在将执行结果转化为数据传输对象时发生错误！",
                    Data = null
                };
            }

        }

        // 根据失败步骤的结果和当前可用分类，使用 AI 生成失败原因分析和恢复建议
        private async Task<AgentStepExecutionResult> ExecuteExplainFailureWithSuggestionsAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            var input = new ExplainFailureWithSuggestionsInput
            {
                FailureFromStep = GetIntParam(step.Parameters, "failureFromStep"),
                CategoriesFromStep = GetIntParam(step.Parameters, "categoriesFromStep"),
                RequestedCategoryName = GetStringParam(step.Parameters, "requestedCategoryName")
            };

            var failureResult = previousResults
                .FirstOrDefault(r => r.StepNumber == input.FailureFromStep);

            var categoriesResult = previousResults
                .FirstOrDefault(r => r.StepNumber == input.CategoriesFromStep);

            if (failureResult == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {input.FailureFromStep} 步的失败结果"
                };
            }

            if (categoriesResult == null || categoriesResult.Data == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到第 {input.CategoriesFromStep} 步的分类结果"
                };
            }

            var requestedTarget =
                string.IsNullOrWhiteSpace(input.RequestedCategoryName)
                ? "（未指定）"
                : input.RequestedCategoryName;

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
                5. 如果“用户请求的分类名称”不在当前可用分类中，必须明确说明该分类不存在，而不是说该分类下没有文章。
                6. 如果用户请求的分类名称在当前可用分类中，但查询结果为空，才说明该分类下暂无文章。
                7. 语气自然、简洁。
                """),

                new(ChatRole.User,
                $"""
                用户请求的分类名称：
                {requestedTarget}

                失败步骤信息：
                {failureText}

                当前可用分类：
                {categoriesText}
                """)
            };

            _logger.LogInformation(
                "执行 ExplainFailureWithSuggestions，失败来源步骤：{FailureStep}，分类来源步骤：{CategoriesStep}",
                input.FailureFromStep,
                input.CategoriesFromStep);

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

            var output = new ExplainFailureWithSuggestionsOutput
            {
                Answer = answer,
                FailureFromStep = input.FailureFromStep,
                RequestedTarget = requestedTarget,
                UsedContextTypes = new List<string> { "Categories" },
                FailureSummary = failureResult.Message
            };

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "失败恢复建议生成成功",
                Data = output
            };
        }

        // 重载版本：从两个不同来源的步骤结果中获取数据，生成更准确的失败分析和恢复建议
        // 执行 ExplainFailureWithSuggestions 动作：根据原始失败步骤和补救阶段收集的上下文，生成用户友好的失败解释和建议
        private async Task<AgentStepExecutionResult> ExecuteExplainFailureWithSuggestionsAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> recoveryResults,
            List<AgentStepExecutionResult> failedOriginalResults)
        {
            var input = new ExplainFailureWithSuggestionsInput
            {
                FailureFromStep = GetIntParam(step.Parameters, "failureFromStep"),
                CategoriesFromStep = GetIntParam(step.Parameters, "categoriesFromStep"),
                ArticlesFromStep = GetIntParam(step.Parameters, "articlesFromStep"),
                SearchResultsFromStep = GetIntParam(step.Parameters, "searchResultsFromStep"),
                ContentFromStep = GetIntParam(step.Parameters, "contentFromStep"),
                RequestedCategoryName = GetStringParam(step.Parameters, "requestedCategoryName")
            };

            // 从原始失败结果中查找对应的步骤结果
            var failureResult = failedOriginalResults
                .FirstOrDefault(r => r.StepNumber == input.FailureFromStep);

            // 如果找不到原始失败步骤，返回失败
            if (failureResult == null)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = $"找不到原始失败流程中的第 {input.FailureFromStep} 步结果"
                };
            }

            var requestedTarget =
                string.IsNullOrWhiteSpace(input.RequestedCategoryName)
                ? "（未指定）"
                : input.RequestedCategoryName;

            // ---- 收集可选的补救上下文 ----
            var contextParts = new List<string>();

            // 将各步骤数据添加到上下文列表（若存在）
            AddRecoveryContext(contextParts, recoveryResults, input.CategoriesFromStep, "当前可用分类");
            AddRecoveryContext(contextParts, recoveryResults, input.ArticlesFromStep, "当前用户文章列表");
            AddRecoveryContext(contextParts, recoveryResults, input.SearchResultsFromStep, "补救查询结果");
            AddRecoveryContext(contextParts, recoveryResults, input.ContentFromStep, "文章正文内容");

            // 如果没有收集到任何上下文，占位提示
            var recoveryContext = contextParts.Count == 0
                ? "无额外补救上下文。"
                : string.Join("\n\n", contextParts);

            // ---- 将失败步骤的结果转为文本，供 AI 理解 ----
            var failureText = ConvertObjectToText(failureResult);

            // ---- 构造 AI 对话消息 ----
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                """
                你是一个博客系统 Agent 执行恢复助手。
                用户的原始任务没有成功完成，现在你需要根据失败原因和提供的上下文信息，给用户一个清晰、友好的说明。

                要求：
                1. 不要编造不存在的文章或数据。
                2. 明确说明原任务为什么没完成。
                3. 根据提供的上下文信息（可能包含可用分类、用户文章列表、补救查询结果、文章内容等），给出针对性的建议。
                4. 如果提供了可用分类，并且用户请求的分类不在其中，必须明确说明该分类不存在，而不是说该分类下没有文章。
                5. 如果用户请求的分类在可用分类中，但查询结果为空，则说明该分类下暂无文章。
                6. 语气自然、简洁。
                """),

                new(ChatRole.User,
                $"""
                用户请求的目标（分类名称）：
                {requestedTarget}

                原始失败步骤信息：
                {failureText}

                补救阶段收集的上下文：
                {recoveryContext}
                """)
            };

            // 记录详细日志，便于追踪上下文来源
            _logger.LogInformation(
                "执行 ExplainFailureWithSuggestions，原始失败步骤：{FailureStep}，上下文来源：Categories={CatStep}, Articles={ArtStep}, Search={SearchStep}, Content={ContentStep}",
                input.FailureFromStep,
                input.CategoriesFromStep,
                input.ArticlesFromStep,
                input.SearchResultsFromStep,
                input.ContentFromStep);

            // 调用 AI 生成建议，限制输出 Token 数
            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = AgentTokenBudget.RecoverySuggestionMaxOutputTokens
                });

            // 提取助手的回复文本
            var answer = response.Messages
                .Where(m => m.Role == ChatRole.Assistant)
                .Select(m => m.Text)
                .FirstOrDefault() ?? "任务执行失败，请稍后重试。";

            var usedContextTypes = new List<string>();

            if (input.CategoriesFromStep > 0) usedContextTypes.Add("Categories");
            if (input.ArticlesFromStep > 0) usedContextTypes.Add("MyArticles");
            if (input.SearchResultsFromStep > 0) usedContextTypes.Add("SearchResults");
            if (input.ContentFromStep > 0) usedContextTypes.Add("Content");

            var output = new ExplainFailureWithSuggestionsOutput
            {
                Answer = answer,
                FailureFromStep = input.FailureFromStep,
                RequestedTarget = requestedTarget,
                UsedContextTypes = usedContextTypes,
                FailureSummary = failureResult.Message
            };

            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "失败恢复建议生成成功",
                Data = output
            };
        }

        // 执行 AnswerQuestionFromContent 动作：根据用户问题从文章正文中提取答案，支持直接传入正文或从前置步骤引用
        private async Task<AgentStepExecutionResult> ExecuteAnswerQuestionFromContentAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults)
        {
            // 构造输入对象，统一保存动作参数
            var input = new AnswerQuestionFromContentInput
            {
                Content = GetStringParam(step.Parameters, "content", string.Empty),
                ContentFromStep = GetIntParam(step.Parameters, "contentFromStep"),
                Question = GetStringParam(step.Parameters, "question")
            };

            AgentMemoryFact? sourceArticleFact = null;

            // 未直接提供正文时，从前置步骤中获取正文
            if (string.IsNullOrWhiteSpace(input.Content))
            {
                var previousResult = previousResults.FirstOrDefault(
                    result => result.StepNumber == input.ContentFromStep);

                if (previousResult == null || previousResult.Data == null)
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = $"找不到第 {input.ContentFromStep} 步的文章正文"
                    };
                }

                // 从上游执行结果获取记忆事实
                if (previousResult.MemoryFacts != null)
                {
                    sourceArticleFact = previousResult.MemoryFacts
                        .LastOrDefault(f =>
                            f.ArticleId.HasValue &&
                            !string.IsNullOrWhiteSpace(f.ArticleTitle) &&
                            (f.Type == ArticleMemoryType.ArticleSelected ||
                             f.Type == ArticleMemoryType.ArticleUpdated ||
                             f.Type == ArticleMemoryType.ArticleMentioned));
                }

                // 从前置步骤的数据中提取文本内容
                input.Content = ExtractContentText(previousResult.Data);
            }

            // 校验正文不能为空
            if (string.IsNullOrWhiteSpace(input.Content))
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "文章正文不能为空"
                };
            }

            // 校验问题不能为空
            if (string.IsNullOrWhiteSpace(input.Question))
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "文章问题不能为空"
                };
            }

            // 未指定有效前置步骤编号时，将其统一设置为 null
            input.ContentFromStep = input.ContentFromStep > 0
                ? input.ContentFromStep
                : null;

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
                    {input.Content}

                    用户问题：
                    {input.Question}
                    """)
            };

            _logger.LogInformation(
                "执行 AnswerQuestionFromContent，正文来源步骤：{StepNumber}，问题：{Question}",
                input.ContentFromStep,
                input.Question);

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

            // 构造输出对象，包含问题和答案及相关长度信息
            var output = new AnswerQuestionFromContentOutput
            {
                Question = input.Question,
                Answer = answer,
                ContentLength = input.Content?.Length ?? 0,
                AnswerLength = answer.Length,
                SourceArticleId = sourceArticleFact?.ArticleId,
                SourceArticleTitle = sourceArticleFact?.ArticleTitle,
                SourceCategoryName = sourceArticleFact?.CategoryName
            };

            var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.AnswerQuestionFromContent).ToList();

            // 返回成功结果，将输出对象存入 Data
            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = true,
                Message = "文章问题回答成功",
                Data = output,
                MemoryFacts = memoryFacts
            };
        }

        // 执行 SelectArticleFromList 动作：根据匹配模式从文章列表中选出一篇
        private async Task<AgentStepExecutionResult> ExecuteSelectArticleFromListAsync(
            AgentPlanStep step,
            List<AgentStepExecutionResult> previousResults,
            string sessionId)
        {
            // 解析参数：匹配模式、列表来源步骤、索引或标题
            var matchTypeText = GetStringParam(step.Parameters, "matchType");
            System.Enum.TryParse<ArticleSelectionMatchMode>(
                matchTypeText,
                ignoreCase: true,
                out var matchType);

            var input = new SelectArticleFromListInput
            {
                ListFromStep = GetIntParam(step.Parameters, "listFromStep"),
                MatchType = matchType,
                Index = GetIntParam(step.Parameters, "index", -1),
                Selection = GetStringParam(step.Parameters, "selection")
            };

            List<RecentMentionedArticleItem>? articles = null;

            // 首先尝试从计划中的前置步骤获取文章列表
            if (input.ListFromStep > 0)
            {
                var previousResult = previousResults.FirstOrDefault(r => r.Success && r.StepNumber == input.ListFromStep);

                if (previousResult?.Data != null)
                {
                    // 通过 IArticleListOutput 接口判断 Action Output 能返回文章列表
                    if (previousResult.Data is IArticleListOutput articleListOutput)
                    {
                        articles = articleListOutput.Articles
                            .Select(a => new RecentMentionedArticleItem
                            {
                                ArticleId = a.Id,
                                Title = a.Title,
                                CategoryName = a.CategoryName
                            })
                            .ToList();
                    }
                }
            }

            // 如果从计划中获取不到，尝试从会话记忆中恢复最近提及的文章列表
            if (articles == null || articles.Count == 0)
            {
                var memory = await _memoryRepository.GetByConversationIdAsync(sessionId);

                if (memory != null && memory.RecentMentionedArticlesJson != null)
                {
                    try
                    {
                        var memoryArticles = JsonSerializer.Deserialize<List<RecentMentionedArticleItem>>(
                            memory.RecentMentionedArticlesJson);
                        if (memoryArticles != null && memoryArticles.Count > 0)
                        {
                            articles = memoryArticles;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "反序列化最近提及文章JSON失败，SessionId: {SessionId}", sessionId);
                    }
                }
            }

            // 仍无列表则失败
            if (articles == null || articles.Count == 0)
            {
                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = false,
                    Message = "当前上下文缺少文章列表，请先执行‘搜索文章’步骤后再尝试选择。"
                };
            }

            // 按标题匹配
            if (input.MatchType == ArticleSelectionMatchMode.ByTitle)
            {
                if (string.IsNullOrWhiteSpace(input.Selection))
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = "未提供要搜索的文章标题。"
                    };
                }

                // 模糊匹配（不区分大小写，包含子串）
                var matchedArticles = articles
                    .Where(a => a.Title?.IndexOf(input.Selection, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                if (matchedArticles.Count == 0)
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = $"未在文章列表中找到标题包含“{input.Selection}”的文章。"
                    };
                }
                else if (matchedArticles.Count == 1)
                {
                    var matchedArticle = matchedArticles.First();

                    var output = new SelectArticleFromListOutput
                    {
                        ArticleId = matchedArticle.ArticleId,
                        Title = matchedArticle.Title,
                        CategoryName = matchedArticle.CategoryName,
                        MatchMode = ArticleSelectionMatchMode.ByTitle,
                        Selection = input.Selection,
                    };

                    var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.SelectArticleFromList).ToList();

                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = true,
                        Message = $"成功选择到文章：{output.Title}",
                        Data = output,
                        MemoryFacts = memoryFacts
                    };
                }
                else
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = $"关键词“{input.Selection}”匹配到 {matchedArticles.Count} 篇文章，无法自动选择。请使用更具体的标题重试。"
                    };
                }
            }

            // 按索引匹配
            if (input.MatchType == ArticleSelectionMatchMode.ByIndex)
            {
                // 校验索引是否有效（>0）
                if (!input.Index.HasValue || input.Index.Value <= 0)
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = "未提供有效的文章序号，请指定 index 参数（从 1 开始）。"
                    };
                }

                // 转为从0开始的索引
                int targetIndex = input.Index.Value - 1;

                if (targetIndex < 0 || targetIndex >= articles.Count)
                {
                    return new AgentStepExecutionResult
                    {
                        StepNumber = step.StepNumber,
                        Action = step.Action,
                        Success = false,
                        Message = $"文章序号 {input.Index.Value} 超出范围，当前列表共有 {articles.Count} 篇文章。"
                    };
                }

                var matchedArticle = articles[targetIndex];

                var output = new SelectArticleFromListOutput
                {
                    ArticleId = matchedArticle.ArticleId,
                    Title = matchedArticle.Title,
                    CategoryName = matchedArticle.CategoryName,
                    MatchMode = ArticleSelectionMatchMode.ByIndex,
                    Selection = input.Index.Value.ToString(),
                };

                var memoryFacts = output.GetMemoryFacts(AgentActionRegistry.SelectArticleFromList).ToList();

                return new AgentStepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    Action = step.Action,
                    Success = true,
                    Data = output,
                    Message = $"已选择第 {input.Index.Value} 篇文章：“{matchedArticle.Title}”。",
                    MemoryFacts = memoryFacts
                };
            }

            // 匹配模式不支持
            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = false,
                Message = $"不支持的匹配模式：{input.MatchType}。"
            };
        }

        //  ====================       以下是辅助方法       ====================

        // 快速生成失败结果
        private AgentStepExecutionResult FailResult(AgentPlanStep step, string message)
        {
            return new AgentStepExecutionResult
            {
                StepNumber = step.StepNumber,
                Action = step.Action,
                Success = false,
                Message = message
            };
        }

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
            var lastSuccessStep = stepResults
                .LastOrDefault(result => result.Success && result.Data != null);

            // 如果没有任何成功的步骤包含数据，返回默认提示
            if (lastSuccessStep == null)
            {
                return "计划执行完成，但没有生成最终结果。";
            }

            // 这些 Action 的输出本身已经是自然语言，无需再让 AI 二次加工
            var naturalLanguageActions = new[]
            {
                AgentActionRegistry.SummarizeContent,
                AgentActionRegistry.CompareContents,
                AgentActionRegistry.ExplainFailureWithSuggestions,
                AgentActionRegistry.AnswerQuestionFromContent,
                AgentActionRegistry.GenerateContentRevision
            };

            // 如果执行结果是单一成功步骤且行为为自然语言类型，直接将其 Data 转为文本返回
            var successfulDataSteps = stepResults
                .Where(result => result.Success && result.Data != null)
                .ToList();

            if (successfulDataSteps.Count == 1)
            {
                if (naturalLanguageActions.Contains(lastSuccessStep.Action))
                {
                    return ConvertObjectToText(lastSuccessStep.Data!);
                }
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
                    7. 如果工具结果中同时包含文章查询结果和文章总结结果，回答时必须先说明查询到的文章标题、分类、点赞量等信息，再给出总结内容。
                    8. 不要只返回最后一步总结结果，必须融合前面步骤中与用户目标相关的信息。
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
        private static string BuildFinalAnswerContext(List<AgentStepExecutionResult> stepResults)
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
                            执行步骤：
                            - Action：{result.Action}
                            - Message：{result.Message}
                            - Data：
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
            return AiChatHelper.GetString(parameters, key, defaultValue);
        }

        // 安全地从参数字典中获取整数值（兼容 JsonElement 与字符串数字）
        private static int GetIntParam(
            Dictionary<string, object> parameters,
            string key,
            int defaultValue = 0)
        {
            return AiChatHelper.GetInt(parameters, key, defaultValue);
        }

        // 从对象文本中提取第一个出现的文章ID，匹配中文提示或JSON属性
        private static int ExtractFirstArticleId(object data)
        {
            if (data is IAgentArticleReferenceOutput articleReference)
            {
                return articleReference.GetPrimaryArticleId() ?? 0;
            }

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

            if (data is IUserReadableOutput readableOutput)
            {
                return readableOutput.ToUserReadableText();
            }

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        // 从补救执行结果中提取指定步骤的数据，并添加到上下文列表中（用于构建最终回答）
        private static void AddRecoveryContext(
            List<string> contextParts,
            List<AgentStepExecutionResult> recoveryResults,
            int fromStep,
            string title)
        {
            // 步骤编号无效则直接返回
            if (fromStep <= 0)
            {
                return;
            }

            // 查找对应步骤的执行结果
            var result = recoveryResults.FirstOrDefault(r => r.StepNumber == fromStep);
            if (result?.Data == null)
            {
                return;
            }

            // 将步骤数据转换为文本并添加到上下文列表，带标题
            contextParts.Add($"""
             【{title}】
             {ConvertObjectToText(result.Data)}
             """);
        }

        // 从参数字典中解析 SearchArticlesByCategory 动作的输入参数
        private static SearchArticlesByCategoryInput ParseSearchArticlesByCategoryInput(Dictionary<string, object> parameters)
        {
            // 获取排序方式原始值，默认为 "Latest"
            var sortByText = AiChatHelper.GetString(parameters, "sortBy", "Latest");

            return new SearchArticlesByCategoryInput
            {
                // 获取分类名称，默认为空字符串
                CategoryName = AiChatHelper.GetString(parameters, "categoryName", string.Empty),

                // 获取返回数量，默认为 5
                Top = AiChatHelper.GetInt(parameters, "top", 5),

                // 将排序方式标准化为系统内部允许的值（Latest/MostLiked/MostViewed）
                SortBy = AiHelper.NormalizeSortBy(sortByText)
            };
        }

        // 从 DTO 中单独取出文章内容，避免大量信息的序列化
        private static string ExtractContentText(object data)
        {
            if (data is IAgentContentOutput contentOutput)
            {
                return contentOutput.GetContentText();
            }

            return ConvertObjectToText(data);
        }

        // 从步骤执行结果中提取最后一条有效的文章记忆事实（去重、过滤未知类型）
        private AgentMemoryFact? ExtractArticleFactFromStepResult(AgentStepExecutionResult? result)
        {
            // 若结果或记忆事实列表为空，直接返回 null
            return result?.MemoryFacts?
                .LastOrDefault(f =>
                    f.ArticleId.HasValue &&              // 必须有文章 ID
                    !string.IsNullOrWhiteSpace(f.ArticleTitle) && // 必须有标题
                    f.Type != ArticleMemoryType.Unknown); // 排除未知类型
        }

    }
}
