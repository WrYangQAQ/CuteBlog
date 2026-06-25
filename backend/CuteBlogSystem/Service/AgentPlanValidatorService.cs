using CuteBlogSystem.AI.Planner;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    // 验证 AgentPlan 结构的合法性，包括步骤编号、Action、参数和步骤间引用关系
    public class AgentPlanValidatorService
    {

        // 验证计划，返回包含错误列表的验证结果对象
        public AgentPlanValidationResult Validate(AgentPlan plan)
        {
            var errors = new List<string>();

            if (plan == null)
            {
                errors.Add("计划不能为空。");
                return AgentPlanValidationResult.Fail(errors);
            }

            if (string.IsNullOrWhiteSpace(plan.Goal))
            {
                errors.Add("计划目标 Goal 不能为空。");
            }

            if (plan.Steps == null || plan.Steps.Count == 0)
            {
                errors.Add("计划步骤 Steps 不能为空。");
                return AgentPlanValidationResult.Fail(errors);
            }

            // 步骤编号连续性及重复性检查
            ValidateStepNumbers(plan, errors);

            // 逐个步骤验证 Action、参数、引用关系
            foreach (var step in plan.Steps)
            {
                ValidateAction(step, errors);
                ValidateParameters(step, errors);
                ValidateStepReferences(step, plan, errors);
            }

            return errors.Count == 0
                ? AgentPlanValidationResult.Success()
                : AgentPlanValidationResult.Fail(errors);
        }

        // 检查步骤编号是否从1开始连续且无重复
        private static void ValidateStepNumbers(AgentPlan plan, List<string> errors)
        {
            var orderedSteps = plan.Steps.OrderBy(s => s.StepNumber).ToList();

            // 检查连续性：期望 StepNumber 依次为 1,2,3...
            for (int i = 0; i < orderedSteps.Count; i++)
            {
                var expectedStepNumber = i + 1;
                if (orderedSteps[i].StepNumber != expectedStepNumber)
                {
                    errors.Add($"步骤编号不连续：期望 Step {expectedStepNumber}，实际为 Step {orderedSteps[i].StepNumber}。");
                }
            }

            // 检查重复编号
            var duplicateSteps = plan.Steps
                .GroupBy(s => s.StepNumber)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicateStep in duplicateSteps)
            {
                errors.Add($"存在重复步骤编号：Step {duplicateStep}。");
            }
        }

        // 检查 Action 是否非空且在允许列表中
        private static void ValidateAction(AgentPlanStep step, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(step.Action))
            {
                errors.Add($"Step {step.StepNumber} 的 Action 不能为空。");
                return;
            }

            if (!AgentActionRegistry.AllowedActions.Contains(step.Action))
            {
                errors.Add($"Step {step.StepNumber} 使用了不允许的 Action：{step.Action}。");
            }
        }

        // 根据 Action 类型，验证其参数的正确性
        private static void ValidateParameters(AgentPlanStep step, List<string> errors)
        {
            switch (step.Action)
            {
                case AgentActionRegistry.SearchArticlesByCategory:
                    ValidateSearchArticlesByCategoryParameters(step, errors);
                    break;

                case AgentActionRegistry.GetArticleContentById:
                    RequireArticleIdOrReference(step, errors);
                    break;

                case AgentActionRegistry.SummarizeContent:
                    RequireIntParameter(step, "contentFromStep", errors);
                    break;

                case AgentActionRegistry.CompareContents:
                    RequireIntParameter(step, "contentFromStepA", errors);
                    RequireIntParameter(step, "contentFromStepB", errors);
                    break;

                case AgentActionRegistry.GetAllCategories:
                    // 无特定参数要求
                    break;

                case AgentActionRegistry.ExplainFailureWithSuggestions:
                    RequireIntParameter(step, "failureFromStep", errors);
                    RequireIntParameter(step, "categoriesFromStep", errors);
                    break;

                case AgentActionRegistry.AnswerQuestionFromContent:
                    RequireIntParameter(step, "contentFromStep", errors);
                    RequireStringParameter(step, "question", errors);
                    break;
            }
        }

        // SearchArticlesByCategory 的具体参数校验
        private static void ValidateSearchArticlesByCategoryParameters(
            AgentPlanStep step,
            List<string> errors)
        {
            var categoryName = GetStringParameter(step, "categoryName");
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                errors.Add($"Step {step.StepNumber} 缺少 categoryName 参数。");
            }

            var top = GetIntParameter(step, "top", 5);
            if (top <= 0)
            {
                errors.Add($"Step {step.StepNumber} 的 top 必须大于 0。");
            }
            if (top > 10)
            {
                errors.Add($"Step {step.StepNumber} 的 top 不能超过 10。");
            }

            var sortBy = GetStringParameter(step, "sortBy", "Latest");
            if (!AgentActionRegistry.AllowedSortTypes.Contains(sortBy))
            {
                errors.Add($"Step {step.StepNumber} 的 sortBy 不合法：{sortBy}。只能是 Latest、MostLiked、MostViewed。");
            }
        }

        // 检查那些需要引用前面步骤结果的动作，确保引用的步骤存在且编号小于当前步骤
        private static void ValidateStepReferences(
            AgentPlanStep step,
            AgentPlan plan,
            List<string> errors)
        {
            switch (step.Action)
            {
                case AgentActionRegistry.GetArticleContentById:
                    if (GetIntParameter(step, "articleIdFromStep") > 0)
                    {
                        ValidateReferenceToPreviousStep(step, plan, "articleIdFromStep", errors);
                    }
                    break;

                case AgentActionRegistry.SummarizeContent:
                    ValidateReferenceToPreviousStep(step, plan, "contentFromStep", errors);
                    break;

                case AgentActionRegistry.CompareContents:
                    ValidateReferenceToPreviousStep(step, plan, "contentFromStepA", errors);
                    ValidateReferenceToPreviousStep(step, plan, "contentFromStepB", errors);
                    break;

                case AgentActionRegistry.ExplainFailureWithSuggestions:
                    ValidateReferenceToPreviousStep(step, plan, "failureFromStep", errors);
                    ValidateReferenceToPreviousStep(step, plan, "categoriesFromStep", errors);
                    break;

                case AgentActionRegistry.AnswerQuestionFromContent:
                    ValidateReferenceToPreviousStep(step, plan, "contentFromStep", errors);
                    break;
            }
        }

        // 通用的前一步引用校验：参数值必须为正整数，小于当前步骤编号，且对应的步骤真实存在
        private static void ValidateReferenceToPreviousStep(
            AgentPlanStep step,
            AgentPlan plan,
            string parameterName,
            List<string> errors)
        {
            var referencedStepNumber = GetIntParameter(step, parameterName);
            if (referencedStepNumber <= 0)
            {
                errors.Add($"Step {step.StepNumber} 的 {parameterName} 必须是有效步骤编号。");
                return;
            }

            if (referencedStepNumber >= step.StepNumber)
            {
                errors.Add($"Step {step.StepNumber} 的 {parameterName} 只能引用之前的步骤，不能引用当前或未来步骤。");
                return;
            }

            bool exists = plan.Steps.Any(s => s.StepNumber == referencedStepNumber);
            if (!exists)
            {
                errors.Add($"Step {step.StepNumber} 引用了不存在的步骤：Step {referencedStepNumber}。");
            }
        }

        // 要求整型参数必须存在且大于0
        private static void RequireIntParameter(
            AgentPlanStep step,
            string parameterName,
            List<string> errors)
        {
            if (!step.Parameters.ContainsKey(parameterName))
            {
                errors.Add($"Step {step.StepNumber} 缺少参数：{parameterName}。");
                return;
            }

            var value = GetIntParameter(step, parameterName);
            if (value <= 0)
            {
                errors.Add($"Step {step.StepNumber} 的 {parameterName} 必须是大于 0 的整数。");
            }
        }

        // 要求字符串型参数必须存在
        private static void RequireStringParameter(
            AgentPlanStep step,
            string parameterName,
            List<string> errors)
        {
            if (!step.Parameters.ContainsKey(parameterName))
            {
                errors.Add(
                    $"Step {step.StepNumber} 缺少参数：{parameterName}。");
                return;
            }

            var value = GetStringParameter(step, parameterName);

            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(
                    $"Step {step.StepNumber} 的 {parameterName} 不能为空。");
                return;
            }
        }

        // 从参数字典中安全获取字符串值（兼容 JsonElement）
        private static string GetStringParameter(
            AgentPlanStep step,
            string key,
            string defaultValue = "")
        {
            if (!step.Parameters.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind == JsonValueKind.String
                    ? jsonElement.GetString() ?? defaultValue
                    : jsonElement.ToString();
            }

            return value.ToString() ?? defaultValue;
        }

        // 从参数字典中安全获取整数值（兼容 JsonElement 数字或字符串数字）
        private static int GetIntParameter(
            AgentPlanStep step,
            string key,
            int defaultValue = 0)
        {
            if (!step.Parameters.TryGetValue(key, out var value) || value == null)
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

        // 专门验证补救计划的合法性，除了基本结构和参数校验外，还要检查 ExplainFailureWithSuggestions 的特殊引用关系
        public AgentPlanValidationResult ValidateRecoveryPlan(
            AgentPlan recoveryPlan,
            AgentPlanExecutionResult failedExecutionResult)
        {
            var errors = new List<string>();

            if (recoveryPlan == null)
            {
                errors.Add("补救计划不能为空。");
                return AgentPlanValidationResult.Fail(errors);
            }

            if (recoveryPlan.Steps == null || recoveryPlan.Steps.Count == 0)
            {
                errors.Add("补救计划步骤不能为空。");
                return AgentPlanValidationResult.Fail(errors);
            }

            ValidateStepNumbers(recoveryPlan, errors);

            foreach (var step in recoveryPlan.Steps)
            {
                if (!AgentActionRegistry.AllowedRecoveryActions.Contains(step.Action))
                {
                    errors.Add($"补救计划中不允许使用 Action：{step.Action}。");
                    continue;
                }

                if (step.Action == AgentActionRegistry.ExplainFailureWithSuggestions)
                {
                    RequireIntParameter(step, "failureFromStep", errors);
                    RequireIntParameter(step, "categoriesFromStep", errors);

                    var failureFromStep = GetIntParameter(step, "failureFromStep");
                    var categoriesFromStep = GetIntParameter(step, "categoriesFromStep");

                    var originalFailureStepExists = failedExecutionResult.StepResults
                        .Any(r => r.StepNumber == failureFromStep && !r.Success);

                    if (!originalFailureStepExists)
                    {
                        errors.Add($"补救计划引用的原始失败步骤不存在或不是失败步骤：Step {failureFromStep}。");
                    }

                    if (categoriesFromStep >= step.StepNumber)
                    {
                        errors.Add($"Step {step.StepNumber} 的 categoriesFromStep 只能引用补救计划中之前的步骤。");
                    }

                    var categoriesStepExists = recoveryPlan.Steps
                        .Any(s => s.StepNumber == categoriesFromStep && s.Action == AgentActionRegistry.GetAllCategories);

                    if (!categoriesStepExists)
                    {
                        errors.Add($"补救计划中 categoriesFromStep 引用的步骤不存在或不是 GetAllCategories：Step {categoriesFromStep}。");
                    }
                }
            }

            return errors.Count == 0
                ? AgentPlanValidationResult.Success()
                : AgentPlanValidationResult.Fail(errors);
        }

        // 验证 GetArticleContentById 动作的参数，要求：必须提供 articleId 或 articleIdFromStep 中的一个有效值
        private static void RequireArticleIdOrReference(
            AgentPlanStep step,
            List<string> errors)
        {
            var articleId = GetIntParameter(step, "articleId");
            var articleIdFromStep = GetIntParameter(step, "articleIdFromStep");

            if (articleId <= 0 && articleIdFromStep <= 0)
            {
                errors.Add($"Step {step.StepNumber} 必须提供 articleId 或 articleIdFromStep。");
            }

            if (articleId > 0 && articleIdFromStep > 0)
            {
                errors.Add($"Step {step.StepNumber} 的 articleId 和 articleIdFromStep 只能二选一，不能同时提供。");
            }
        }
    }
}