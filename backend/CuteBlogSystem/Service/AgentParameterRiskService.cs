using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Helper;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    /// <summary>
    /// 参数内容风险检测服务（语义安检）
    /// 职责：检测参数值是否在语义上危险、异常、像误操作。
    /// 不负责：结构是否完整（由 PlanValidator 负责）、权限是否允许（由 PermissionService 负责）。
    /// </summary>
    public class AgentParameterRiskService
    {
        // 语义上的阈值（非硬限制，仅供风险嗅探）
        private const int SuspiciouslyShortContentLength = 20;   // 低于此长度极像误覆盖
        private const int RiskLargePageSize = 100;               // 超过此数量视为批量拉取风险
        private const int RiskLargeTopCount = 50;                // 查询前N条超过此值视为风险

        private readonly IChatClient _chatClient;
        private readonly ILogger<AgentParameterRiskService> _logger;

        public AgentParameterRiskService(
            IChatClient chatClient,
            ILogger<AgentParameterRiskService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        // 危险内容修订指令列表
        private static readonly string[] DangerousContentRevisionInstructions =
        {
            "清空",
            "改成空",
            "改为空",
            "变为空",
            "空字符串",
            "删除全文",
            "删除所有内容",
            "删掉全部",
            "覆盖为空",
            "置空",
            "null"
        };

        // 主验证入口
        public AgentParameterRiskResult Validate(AgentPlan plan)
        {
            var errors = new List<string>(); // 这里 errors 实际代表“风险项列表”

            if (plan?.Steps == null)
            {
                errors.Add("计划为空，无法进行参数风险校验。");
                return AgentParameterRiskResult.Unsafe(errors);
            }

            foreach (var step in plan.Steps)
            {
                step.Parameters ??= new Dictionary<string, object>();

                // ---- 风险检测分发 ----
                switch (step.Action)
                {
                    case AgentActionRegistry.UpdateArticleTitle:
                        DetectUpdateTitleRisks(step, errors);
                        break;

                    case AgentActionRegistry.UpdateArticleContent:
                        DetectUpdateContentRisks(step, errors);
                        break;

                    case AgentActionRegistry.SearchArticlesByCategory:
                        DetectSearchPaginationRisks(step, errors);
                        break;

                    case AgentActionRegistry.GetMyArticles:
                        DetectGetMyArticlesPaginationRisks(step, errors);
                        break;

                    case AgentActionRegistry.DeleteArticle:
                        DetectDeleteRisks(step, errors);
                        break;

                    case AgentActionRegistry.GenerateContentRevision:
                        DetectGenerateContentRevisionRisks(step, errors);
                        break;

                    case AgentActionRegistry.SearchArticlesByKeyword:
                    case AgentActionRegistry.SearchArticlesByTag:
                        DetectSearchPaginationRisks(step, errors);
                        break;

                    case AgentActionRegistry.CreateArticle:
                        DetectCreateArticleRisks(step, errors);
                        break;

                    default:
                        // 其他Action不进行风险检测
                        break;
                }
            }

            return errors.Count == 0
                ? AgentParameterRiskResult.Safe()
                : AgentParameterRiskResult.Unsafe(errors);
        }


        // ==================== 执行时内容检测器 ====================

        // 验证更新内容是否具有风险性
        public AgentParameterRiskResult ValidateResolvedArticleContent(string? newContent, int stepNumber)
        {
            var errors = new List<string>();

            DetectArticleContentTextRisks(newContent, stepNumber, errors);

            return errors.Count == 0
                ? AgentParameterRiskResult.Safe()
                : AgentParameterRiskResult.Unsafe(errors);
        }

        // 验证执行时修改建议是否具有风险性内容
        public AgentParameterRiskResult ValidateContentRevisionInstruction(string? instruction, int stepNumber)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(instruction))
            {
                return AgentParameterRiskResult.Safe();
            }

            if (ContainsDangerousContentRevisionInstruction(instruction))
            {
                errors.Add($"Step {stepNumber} 的内容修订指令包含清空/删除正文风险：{instruction}");
            }

            return errors.Count == 0
                ? AgentParameterRiskResult.Safe()
                : AgentParameterRiskResult.Unsafe(errors);
        }

        // 验证执行时AI创建文章的正文是否具有风险性内容
        public async Task<AgentParameterRiskResult> ValidateCreateArticleContent(string? title, string? summary, string? content)
        {
            // 所有字段都为空时视为安全
            if (string.IsNullOrWhiteSpace(title)
                && string.IsNullOrWhiteSpace(summary)
                && string.IsNullOrWhiteSpace(content))
            {
                return AgentParameterRiskResult.Safe();
            }

            try
            {
                // 构建待检测的完整文本（包含标题、摘要、正文）
                var fullText = BuildFullContent(title, summary, content);

                // 构建 AI 检测提示
                var messages = new List<ChatMessage>
                {
                    new ChatMessage(
                        ChatRole.System,
                        @"你是一个文章内容安全检测器。请检测用户提供的文章标题、摘要、正文三个部分是否包含以下不安全内容：
                        1. 政治敏感内容（如危害国家安全、分裂国家、颠覆政权等）
                        2. 生命危险内容（如自残、自杀、暴力伤害、恐怖活动等）
                        3. 情色色情内容（如色情描写、淫秽信息、低俗内容等）
                        4. 其他违法违规内容（如赌博、毒品、诈骗等）

                        请综合评估三个部分，只要任意一部分存在风险，整篇文章即判定为不安全。
                        必须返回 JSON 格式，格式如下：
                        {
                          ""passed"": true,   // true 表示安全，false 表示不安全
                          ""reason"": ""如果不安全，说明具体原因；如果安全则返回空字符串""
                        }
                        只返回 JSON，不要有其他解释。"),
                    new ChatMessage(ChatRole.User, fullText)
                };

                // 调用 AI 模型（可加超时控制）
                var response = await _chatClient.GetResponseAsync(messages);
                var raw = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text ?? string.Empty;

                // 提取 JSON
                var json = ExtractJson(raw);

                // 反序列化
                var result = JsonSerializer.Deserialize<SafetyCheckResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 检测未通过
                if (result != null && !result.Passed)
                {
                    var errors = new List<string> { result.Reason ?? "内容检测未通过（未提供具体原因）" };
                    return AgentParameterRiskResult.Unsafe(errors);
                }

                // 通过
                return AgentParameterRiskResult.Safe();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证文章内容安全性时发生异常，Title: {Title}, Summary: {Summary}, Content: {Content}",
                    title, summary, content);
                var errors = new List<string> { "内容安全检测服务异常，请稍后重试" };
                return AgentParameterRiskResult.Unsafe(errors);
            }
        }

        // ==================== 私有检测器 ====================

        // 检测修改标题的风险：标题包含换行 → 像粘贴了正文，标题极短或无意义（如"."） → 可能误清空标题，标题含敏感指令
        private static void DetectUpdateTitleRisks(AgentPlanStep step, List<string> errors)
        {
            var newTitle = GetStringParam(step.Parameters, "newTitle");

            if (string.IsNullOrWhiteSpace(newTitle))
            {
                errors.Add($"Step {step.StepNumber} 新标题为空，疑似误清空标题。");
                return;
            }

            // 风险1：标题里带换行，大概率是把整篇文章粘进去了
            if (newTitle.Contains('\n') || newTitle.Contains('\r'))
            {
                errors.Add($"Step {step.StepNumber} 新标题包含换行符，疑似误将正文填入标题字段。");
            }

            // 风险2：标题虽然不为空，但去掉标点/空格后只剩极少数无意义字符
            var cleaned = new string(newTitle.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray());
            if (cleaned.Length <= 2)
            {
                errors.Add($"Step {step.StepNumber} 新标题语义上过短或无意义（'{newTitle.Trim()}'），疑似误操作。");
            }

            // 风险3：标题嵌入敏感指令（比如想删除内容却填到了标题里）
            if (ContainsSensitiveInstruction(newTitle))
            {
                errors.Add($"Step {step.StepNumber} 新标题包含高危意图关键词。");
            }
        }

        /// <summary>
        /// 检测修改内容的风险：
        /// 1. 内容异常短（像"略"、"删除"、"空"）
        /// 2. 内容为空（直接清空）
        /// 3. 内容仅包含无意义标点或极短字符
        /// 4. 内容含高危敏感指令
        /// 5. 如果依赖 newContentFromStep，先给提示（不拦截）
        /// </summary>
        private static void DetectUpdateContentRisks(AgentPlanStep step, List<string> errors)
        {
            // 若内容来自上一步（间接引用），暂时不在此处硬拦截，但记录风险提示
            if (step.Parameters.ContainsKey("newContentFromStep"))
            {
                // 内容来自前置步骤，执行前无法判断真实内容。
                // 这里不拦截，后续应在 ExecuteUpdateArticleContentAsync 中拿到真实 newContent 后做写入前风险检查。
            }

            // 如果有直接内容，立刻做语义嗅探
            var newContent = GetStringParam(step.Parameters, "newContent");
            if (string.IsNullOrWhiteSpace(newContent))
            {
                // 如果参数里既没有 newContent 也没有 newContentFromStep，或者内容为空字符串
                if (!step.Parameters.ContainsKey("newContentFromStep"))
                {
                    DetectArticleContentTextRisks(newContent, step.StepNumber, errors);
                }
                return;
            }

            DetectArticleContentTextRisks(newContent, step.StepNumber, errors);
        }

        // 检测查询分类时的分页风险：top 过大可能导致性能问题或返回过多数据
        private static void DetectSearchPaginationRisks(AgentPlanStep step, List<string> errors)
        {
            var top = GetIntParam(step.Parameters, "top", 5);
            if (top > RiskLargeTopCount)
            {
                errors.Add($"Step {step.StepNumber} 请求返回 {top} 篇文章，数量较大，可能不是用户真实意图（如误将 pageSize 填为 top）。");
            }
        }

        // 检测“我的文章列表”分页风险：top 过大可能拖垮性能或非用户本意
        private static void DetectGetMyArticlesPaginationRisks(AgentPlanStep step, List<string> errors)
        {
            var top = GetIntParam(step.Parameters, "top", 10);
            if (top > RiskLargeTopCount)
            {
                errors.Add($"Step {step.StepNumber} 请求返回 {top} 篇自己的文章，数量较大，可能不是用户真实意图。");
            }
        }

        // 检测删除操作的风险：重点检查是否可能误删全量（如有批量删除标志），以及 ID 是否异常
        private static void DetectDeleteRisks(AgentPlanStep step, List<string> errors)
        {
            // 当前 DeleteArticle 只支持单 ID，但如果后续扩展了 batchDelete 或条件删除，这里兜底
            var articleId = GetIntParam(step.Parameters, "articleId", 0);
            var fromStep = GetIntParam(step.Parameters, "articleIdFromStep", 0);

            if (articleId == 0 && fromStep == 0)
            {
                errors.Add($"Step {step.StepNumber} 删除操作未指定任何文章ID，存在异常。");
            }

            // 若未来出现 deleteAll = true 之类参数，在此嗅探
            if (step.Parameters.ContainsKey("deleteAll") && step.Parameters["deleteAll"] is true)
            {
                errors.Add($"Step {step.StepNumber} 检测到批量删除标志（deleteAll），属于高危操作。");
            }
        }

        // 检测文章正文内容的风险：空内容、过短、占位符、无意义字符、敏感指令
        private static void DetectArticleContentTextRisks(string? newContent, int stepNumber, List<string> errors)
        {
            // 内容为空或空白 → 清空风险
            if (string.IsNullOrWhiteSpace(newContent))
            {
                errors.Add($"Step {stepNumber} 新正文为空，存在清空文章的风险。");
                return;
            }

            var trimmed = newContent.Trim();

            // 内容过短 → 可能误清空或简略占位符
            if (trimmed.Length < SuspiciouslyShortContentLength)
            {
                errors.Add($"Step {stepNumber} 新正文长度仅 {trimmed.Length} 字符，极短，疑似误清空或简略占位符。");
            }

            // 内容为明确的危险占位符（如“删除”、“空”等）→ 恶意清空嫌疑
            var dangerousPlaceholders = new[] { "略", "删除", "空", "无", "null", "删除全文", "清空" };
            if (dangerousPlaceholders.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Step {stepNumber} 新正文内容为 '{trimmed}'，疑似恶意清空或占位符误操作。");
            }

            // 仅包含标点和少量字母数字 → 无意义乱填
            var lettersAndDigits = new string(trimmed.Where(c => char.IsLetterOrDigit(c)).ToArray());
            if (lettersAndDigits.Length <= 3 && trimmed.Length > 5)
            {
                errors.Add($"Step {stepNumber} 新正文仅包含标点符号或无意义字符，疑似乱填。");
            }

            // 包含敏感指令关键词 → 高危意图
            if (ContainsSensitiveInstruction(newContent))
            {
                errors.Add($"Step {stepNumber} 新正文包含高危意图关键词。");
            }
        }

        // 检测文章修改建议内容风险：敏感字符，空内容
        private static void DetectGenerateContentRevisionRisks(AgentPlanStep step, List<string> errors)
        {
            var instruction = GetStringParam(step.Parameters, "instruction");

            if (ContainsDangerousContentRevisionInstruction(instruction))
            {
                errors.Add($"Step {step.StepNumber} 的内容修订指令包含清空/删除正文风险：{instruction}");
            }
        }

        // 检测发布文章的风险：标题、正文、摘要、封面路径都必须看起来像有效发布参数
        private static void DetectCreateArticleRisks(AgentPlanStep step, List<string> errors)
        {
            var title = GetStringParam(step.Parameters, "title");
            var content = GetStringParam(step.Parameters, "content");
            var summary = GetStringParam(step.Parameters, "summary");
            var description = GetStringParam(step.Parameters, "description");
            var coverUrl = GetStringParam(step.Parameters, "coverUrl");

            if (!string.IsNullOrWhiteSpace(title))
            {
                if (title.Contains('\n') || title.Contains('\r'))
                {
                    errors.Add($"Step {step.StepNumber} 发布文章标题包含换行符，疑似误将正文填入标题字段。");
                }

                if (ContainsSensitiveInstruction(title))
                {
                    errors.Add($"Step {step.StepNumber} 发布文章标题包含高危意图关键词。");
                }
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                DetectArticleContentTextRisks(content, step.StepNumber, errors);
            }

            if (!string.IsNullOrWhiteSpace(summary) && ContainsSensitiveInstruction(summary))
            {
                errors.Add($"Step {step.StepNumber} 发布文章摘要包含高危意图关键词。");
            }

            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(description))
            {
                errors.Add($"Step {step.StepNumber} 发布文章缺少正文且没有生成方向，存在无效发布风险。");
            }

            if (!string.IsNullOrWhiteSpace(coverUrl) &&
                (coverUrl.Contains("..") || coverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Step {step.StepNumber} 发布文章封面路径异常。");
            }
        }

        // ==================== 通用工具方法 ====================

        private static string GetStringParam(Dictionary<string, object> parameters, string key)
        {
            return AiChatHelper.GetString(parameters, key, string.Empty);
        }

        private static int GetIntParam(Dictionary<string, object> parameters, string key, int defaultValue = 0)
        {
            return AiChatHelper.GetInt(parameters, key, defaultValue);
        }

        // 检测是否有扩展后的敏感指令词库（涵盖越权、破坏性、绕过类意图）中的词语
        private static bool ContainsSensitiveInstruction(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var keywords = new[]
            {
                // 越权/安全类
                "管理员密码", "admin password", "泄露密码", "绕过权限", "提权",
                // 破坏/清空类
                "删除所有", "清空所有", "删除全部", "清空全文", "覆盖全部", "批量删除",
                // 系统指令类
                "DROP TABLE", "TRUNCATE", "DELETE FROM", "UPDATE ALL"
            };

            return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        // 检测内容修订指令是否包含危险操作关键词（如“删除所有”、“清空全文”等），用于安全拦截
        private static bool ContainsDangerousContentRevisionInstruction(string? instruction)
        {
            if (string.IsNullOrWhiteSpace(instruction))
            {
                return false;
            }

            // 归一化：去除所有空白字符，提高匹配准确度（防止通过加空格绕过检测）
            var normalized = instruction
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "");

            // 检查归一化后的指令是否包含危险关键词（不区分大小写）
            return DangerousContentRevisionInstructions.Any(k =>
                normalized.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        // 从可能包含 Markdown 或多余文本的字符串中提取纯 JSON 对象
        private static string ExtractJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var trimmed = text.Trim();
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                return trimmed.Substring(start, end - start + 1);

            return trimmed;
        }

        // AI 返回的检测结果结构
        private class SafetyCheckResult
        {
            public bool Passed { get; set; }
            public string? Reason { get; set; }
        }

        // 将标题、摘要、正文拼接为完整的检测文本
        private static string BuildFullContent(string? title, string? summary, string? content)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(title))
                parts.Add($"【标题】{title}");

            if (!string.IsNullOrWhiteSpace(summary))
                parts.Add($"【摘要】{summary}");

            if (!string.IsNullOrWhiteSpace(content))
                parts.Add($"【正文】{content}");

            return string.Join("\n\n", parts);
        }
    }
}
