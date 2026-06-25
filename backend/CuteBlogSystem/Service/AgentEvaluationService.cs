using CuteBlogSystem.DTO;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Service
{
    public class AgentEvaluationService
    {
        private readonly AgentWorkflowService _workflowService;

        public AgentEvaluationService(AgentWorkflowService workflowService) 
        { 
            _workflowService = workflowService;
        }

        // 执行 Agent 评估测试用例：对比实际执行结果与预期
        public async Task<ApiResponse> RunAsync(AgentEvaluationRunRequest request, int userId)
        {
            // 构造 Agent 请求，若未传 SessionId 则生成一个临时的评估专用 ID
            var agentRequest = new AgentUserMessage
            {
                Content = request.UserMessage,
                SessionId = string.IsNullOrWhiteSpace(request.SessionId)
                    ? $"eval-{Guid.NewGuid():N}"
                    : request.SessionId,
                UserId = userId,
                Role = AgentMessageRole.User
            };

            // 执行工作流并开启 debug 模式，以便获取计划详情用于验证
            var response = await _workflowService.AskAsync(agentRequest, debug: true);

            // 从调试信息中提取实际执行的动作列表
            var actualActions = response.Debug?.Plan?.Steps?
                .Select(x => x.Action)
                .ToList() ?? new List<string>();

            // 收集所有不匹配的期望项
            var errors = new List<string>();

            // 检查每个期望的动作是否都在实际动作中出现
            foreach (var expectedAction in request.ExpectedActions)
            {
                if (!actualActions.Contains(expectedAction))
                {
                    errors.Add($"缺少预期 Action：{expectedAction}");
                }
            }

            // 检查确认状态是否符合预期
            if (response.RequiresConfirmation != request.ExpectRequiresConfirmation)
            {
                errors.Add($"RequiresConfirmation 不符合预期。预期：{request.ExpectRequiresConfirmation}，实际：{response.RequiresConfirmation}");
            }

            // 检查执行成功状态是否符合预期
            if (response.Success != request.ExpectSuccess)
            {
                errors.Add($"Success 不符合预期。预期：{request.ExpectSuccess}，实际：{response.Success}");
            }

            // 构建评估结果对象
            var result = new AgentEvaluationRunResultDTO
            {
                CaseName = request.CaseName,
                Passed = errors.Count == 0,
                Errors = errors,
                Answer = response.Answer,
                ActualActions = actualActions,
                ActualRequiresConfirmation = response.RequiresConfirmation,
                ActualSuccess = response.Success
            };

            // 返回评估结果
            return new ApiResponse(true, "Agent Evaluation 执行完成", result);
        }
    }
}
