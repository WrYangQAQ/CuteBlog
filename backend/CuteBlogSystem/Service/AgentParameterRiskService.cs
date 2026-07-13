using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Util;

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

                    default:
                        // 其他Action不进行风险检测
                        break;
                }
            }

            return errors.Count == 0
                ? AgentParameterRiskResult.Safe()
                : AgentParameterRiskResult.Unsafe(errors);
        }

        // ==================== 私有检测器 ====================

        /// <summary>
        /// 检测修改标题的风险：
        /// 1. 标题包含换行 → 像粘贴了正文
        /// 2. 标题极短或无意义（如"."） → 可能误清空标题
        /// 3. 标题含敏感指令
        /// </summary>
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
                    errors.Add($"Step {step.StepNumber} 新正文为空，存在直接清空文章的风险。");
                }
                return;
            }

            // 风险1：内容过短（疑似误覆盖）
            var trimmed = newContent.Trim();
            if (trimmed.Length < SuspiciouslyShortContentLength)
            {
                errors.Add($"Step {step.StepNumber} 新正文长度仅 {trimmed.Length} 字符，极短，疑似误清空或简略占位符。");
            }

            // 风险2：内容等于常见的“清空占位词”
            var dangerousPlaceholders = new[] { "略", "删除", "空", "无", "null", "删除全文", "清空" };
            if (dangerousPlaceholders.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Step {step.StepNumber} 新正文内容为 '{trimmed}'，疑似恶意清空或占位符误操作。");
            }

            // 风险3：去除标点和空格后几乎没有有效字符（全是符号）
            var lettersAndDigits = new string(trimmed.Where(c => char.IsLetterOrDigit(c)).ToArray());
            if (lettersAndDigits.Length <= 3 && trimmed.Length > 5) // 如 "....." 或 "！！！"
            {
                errors.Add($"Step {step.StepNumber} 新正文仅包含标点符号或无意义字符，疑似乱填。");
            }

            // 风险4：高危敏感关键词
            if (ContainsSensitiveInstruction(newContent))
            {
                errors.Add($"Step {step.StepNumber} 新正文包含高危意图关键词。");
            }
        }

        /// <summary>
        /// 检测查询分类时的分页风险：top 过大可能导致性能问题或返回过多数据
        /// </summary>
        private static void DetectSearchPaginationRisks(AgentPlanStep step, List<string> errors)
        {
            var top = GetIntParam(step.Parameters, "top", 5);
            if (top > RiskLargeTopCount)
            {
                errors.Add($"Step {step.StepNumber} 请求返回 {top} 篇文章，数量较大，可能不是用户真实意图（如误将 pageSize 填为 top）。");
            }
        }

        /// <summary>
        /// 检测“我的文章列表”分页风险：pageSize 过大可能拖垮性能或非用户本意
        /// </summary>
        private static void DetectGetMyArticlesPaginationRisks(AgentPlanStep step, List<string> errors)
        {
            var pageSize = GetIntParam(step.Parameters, "pageSize", 10);
            if (pageSize > RiskLargePageSize)
            {
                errors.Add($"Step {step.StepNumber} 请求每页 {pageSize} 条数据，数量异常大，疑似参数错位或意图拉取全量。");
            }
        }

        /// <summary>
        /// 检测删除操作的风险：重点检查是否可能误删全量（如有批量删除标志），以及 ID 是否异常
        /// </summary>
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

        // ==================== 通用工具方法 ====================

        private static string GetStringParam(Dictionary<string, object> parameters, string key)
        {
            return AiChatHelper.GetString(parameters, key, string.Empty);
        }

        private static int GetIntParam(Dictionary<string, object> parameters, string key, int defaultValue = 0)
        {
            return AiChatHelper.GetInt(parameters, key, defaultValue);
        }

        /// <summary>
        /// 扩展后的敏感指令词库（涵盖越权、破坏性、绕过类意图）
        /// </summary>
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
    }
}