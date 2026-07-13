using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Util;

namespace CuteBlogSystem.Service
{
    public class AgentParameterPermissionService
    {
        private readonly ArticleService _articleService;

        public AgentParameterPermissionService(ArticleService articleService)
        {
            _articleService = articleService;
        }

        // 主校验入口
        public async Task<AgentParameterPermissionResult> ValidateAsync(AgentPlan plan, int userId)
        {
            var errors = new List<string>();

            foreach (var step in plan.Steps)
            {
                // 禁止 Agent 传入敏感身份参数
                if (ContainsForbiddenIdentityParameter(step))
                {
                    errors.Add($"步骤 {step.StepNumber} 包含禁止的身份参数。");
                    continue; // 跳过后续检查
                }

                // 对于涉及文章操作的步骤，检查文章的所有权
                if (step.Action == AgentActionRegistry.UpdateArticleContent || step.Action == AgentActionRegistry.UpdateArticleTitle)
                {
                    var articleId = GetIntParam(step.Parameters, "articleId");

                    // articleIdFromStep 暂时无法在执行前知道真实 ID，执行器里仍要靠 Service 层兜底。
                    if (articleId > 0)
                    {
                        var allowed = await _articleService.CheckArticleAuthorOrAdminAsync(articleId, userId);
                        if (!allowed)
                        {
                            errors.Add($"Step {step.StepNumber} 没有权限操作文章 ID：{articleId}。");
                        }
                    }
                }
            }

            // 根据错误列表构造结果，如果没有错误则返回成功，否则返回失败并附带错误信息
            return errors.Count == 0
                ? AgentParameterPermissionResult.Success()
                : AgentParameterPermissionResult.Fail(errors);
        }

        // ===========  以下为私有工具方法  ===========

        // 检查步骤中是否包含禁止的身份参数
        private static bool ContainsForbiddenIdentityParameter(AgentPlanStep step)
        {
            var forbiddenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "userId",
                "role",
                "isAdmin",
                "ownerId"
            };

            return step.Parameters.Keys.Any(forbiddenKeys.Contains);
        }

        // 从参数字典中安全获取整数值，若参数不存在或解析失败则返回 0
        private static int GetIntParam(Dictionary<string, object> parameters, string key)
        {
            return AiChatHelper.GetInt(parameters, key, 0);
        }
    }
}
