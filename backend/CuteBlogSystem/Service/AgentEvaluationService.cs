using CuteBlogSystem.DTO;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Repository;
using CuteBlogSystem.Helper;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    public class AgentEvaluationService
    {
        // 获取 LLM 响应最长延迟时间
        private readonly TimeSpan SemanticJudgeTimeout = TimeSpan.FromSeconds(20);

        private readonly AgentWorkflowService _workflowService;
        private readonly ILogger<AgentEvaluationService> _logger;
        private readonly IChatClient _chatClient;
        private readonly IConfiguration _configuration;

        private readonly AgentTestCaseRepository _testCaseRepository;
        private readonly AgentEvaluationRunRepository _evaluationRunRepository;
        private readonly AgentEvaluationResultRepository _resultRepository;
        private readonly AgentConversationRepository _conversationRepository;
        private readonly AgentWorkflowLogRepository _workflowLogRepository;
        private readonly AgentEvaluationReportSnapshotRepository _snapshotRepository;

        public AgentEvaluationService(
            AgentWorkflowService workflowService,
            ILogger<AgentEvaluationService> logger,
            IChatClient chatClient,
            IConfiguration configuration,
            AgentTestCaseRepository testCaseRepository,
            AgentEvaluationRunRepository evaluationRunRepository,
            AgentEvaluationResultRepository resultRepository,
            AgentConversationRepository conversationRepository,
            AgentWorkflowLogRepository workflowLogRepository,
            AgentEvaluationReportSnapshotRepository snapshotRepository) 
        { 
            _workflowService = workflowService;
            _logger = logger;
            _chatClient = chatClient;
            _configuration = configuration;
            _testCaseRepository = testCaseRepository;
            _evaluationRunRepository = evaluationRunRepository;
            _resultRepository = resultRepository;
            _conversationRepository = conversationRepository;
            _workflowLogRepository = workflowLogRepository;
            _snapshotRepository = snapshotRepository;
        }

        // 根据前端选择测试用例进行Agent评估测试
        public async Task<ApiResponse> RunWithCaseAsync(int userId, List<int> caseIds)
        { 
            // 从数据库中读取用例
            var testCases = await _testCaseRepository.FindByIdAsync(caseIds);

            if (testCases == null)
            {
                return new ApiResponse(false, "测试用例查询过程出现错误！", code: ResponseCode.NotFound);
            }

            if (testCases.Count == 0)
            {
                return new ApiResponse(false, "当前没有可执行的启用评估用例！", code: ResponseCode.BadRequest);
            }

            // 将测试用例实体转为评估请求DTO
            var requests = new List<AgentEvaluationRunRequest>();
            foreach (var testCase in testCases)
            {
                var actions = ConvertStringJsonToList(testCase.ExpectedActionsJson);
                var keywords = ConvertStringJsonToList(testCase.ExpectedAnswerContainsJson);
                requests.Add(new AgentEvaluationRunRequest(testCase, actions, keywords));
            }

            var dtoResults = new List<AgentEvaluationRunResultDTO>();

            string remark = "手动评估测试执行";
            return await RunEvaluationAsync(userId, remark, requests);
        }

        // 根据前端选择评估批次快照记录进行Agent评估测试
        public async Task<ApiResponse> RunWithSnapshotAsync(int userId, long runId)
        {
            if (userId <= 0 || runId <= 0)
            {
                return new ApiResponse(false, "传入评估批次结果Id或用户Id不合法！", code: ResponseCode.BadRequest);
            }
            try
            {
                // 从数据库中读取评估批次记录
                var run = await _evaluationRunRepository.GetByIdAsync(runId);
                if (run == null)
                {
                    return new ApiResponse(false, "评估批次记录查询过程出现错误！", code: ResponseCode.NotFound);
                }

                // 从数据库中读取该批次下的所有评估结果记录
                var results = await _resultRepository.FindResultByRunId(runId);
                if (results == null || results.Count == 0)
                {
                    return new ApiResponse(false, "该批次下暂时没有评估测试结果记录！", code: ResponseCode.NotFound);
                }

                // 将评估结果记录转为评估请求DTO
                var requests = new List<AgentEvaluationRunRequest>();
                foreach (var result in results)
                {
                    var testCaseDto = JsonSerializer.Deserialize<AgentTestCaseSnapshotDto>(result.TestCaseSnapshotJson);
                    if (testCaseDto == null)
                    {
                        return new ApiResponse(false, $"评估批次快照记录反序列化失败！CaseId: {result.TestCaseId}", code: ResponseCode.InternalError);
                    }
                    var actions = ConvertStringJsonToList(testCaseDto.ExpectedActionsJson);
                    var keywords = ConvertStringJsonToList(testCaseDto.ExpectedAnswerContainsJson);
                    requests.Add(new AgentEvaluationRunRequest(testCaseDto, actions, keywords));
                }

                string remark = $"基于评估批次 {run.Id} 快照重新执行";
                return await RunEvaluationAsync(userId, remark, requests, run.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"在基于评估批次快照重新执行评估测试的过程中出现了异常！\n异常信息:{ex}");
                return new ApiResponse(false, "在基于评估批次快照重新执行评估测试的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 查询最近 count 条评估批次记录
        public async Task<ApiResponse> GetRecentCountRunAsync(int count)
        {
            if (count <= 0)
            {
                return new ApiResponse(false, "查询数量必须大于 0！", code: ResponseCode.BadRequest);
            }

            count = Math.Min(count, 100);

            ApiResponse response;
            try
            {
                var runs = await _evaluationRunRepository.GetRecentAsync(count);
                if (runs == null || runs.Count == 0)
                {
                    response = new ApiResponse(false, "数据库中暂时不存在评估测试批次记录，请在完成评估后再次查询。", code: ResponseCode.NotFound);
                }
                else
                {
                    response = new ApiResponse(true, "评估测试批次记录查询成功！", runs, ResponseCode.Success);
                }
                return response;
            }
            catch(Exception ex)
            {
                _logger.LogError($"评估测试批次记录查询过程中出现异常！\n异常信息:{ex}");
                response = new ApiResponse(false, "评估测试批次记录查询过程中出现异常！", code: ResponseCode.InternalError);
                return response;
            }
        }

        // 根据Run Id查询某一批次下的评估执行结果
        public async Task<ApiResponse> GetResultsByRunIdAsync(long runId)
        {
            ApiResponse response;
            try
            {
                var run = await _evaluationRunRepository.GetByIdAsync(runId);
                if (run == null)
                {
                    return new ApiResponse(false, "数据库中暂时不存在评估测试批次记录！", code: ResponseCode.NotFound);
                }
                var results = await _resultRepository.FindResultByRunId(runId);
                if (results == null || results.Count == 0)
                {
                    response = new ApiResponse(false, "评估未结束！该批次下还没有评估结果", code: ResponseCode.NotFound);
                }
                else
                {
                    response = new ApiResponse(true, "该批次下的评估测试结果记录查询成功！", results, ResponseCode.Success);
                    
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"评估测试结果查询过程中出现异常！\n异常信息:{ex}");
                response = new ApiResponse(false, "评估测试结果查询过程中出现异常！", code: ResponseCode.InternalError);
                return response;
            }
        }

        // 查询所有的测试用例
        public async Task<ApiResponse> GetTestCasesByStatusAsync(int status)
        {
            ApiResponse response;
            List<AgentTestCase> cases;
            try
            {
                switch (status)
                {
                    case 2: cases = await _testCaseRepository.GetAllCaseAsync(); break;
                    case 1: cases = await _testCaseRepository.GetEnabledCaseAsync(); break;
                    case 0: cases = await _testCaseRepository.GetDisabledCaseAsync(); break;
                    default: return new ApiResponse(false, "用例状态查询条件有误！", code: ResponseCode.BadRequest);
                }
                if (cases == null || cases.Count == 0)
                {
                    response = new ApiResponse(false, "数据库暂时未找到评估测试用例，请先添加", code: ResponseCode.NotFound);
                }
                else
                {
                    response = new ApiResponse(true, "评估测试用例查询成功！", cases, code: ResponseCode.Success);
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"评估测试用例查询中出现错误！\n错误信息：{ex}");
                return new ApiResponse(false, "评估测试用例查询中出现错误！", code: ResponseCode.InternalError);
            }
        }

        // 添加测试用例
        public async Task<ApiResponse> CreateTestCaseAsync(AgentTestCaseAddDto caseDto)
        {
            string expectedActionJson = ConvertListToJsonString(caseDto.ExpectedActions);
            string expectedContainsJson = ConvertListToJsonString(caseDto.ExpectedAnswerContains);
            AgentTestCase newCase = new AgentTestCase(caseDto, expectedActionJson, expectedContainsJson);

            if (newCase.SessionId == null) 
            {
                int totalCountEvaluationConversation = _conversationRepository.GetEvaluationConversationsCount();
                int newNumber = totalCountEvaluationConversation + 1;
                newCase.SessionId = $"eval-case-{newNumber:d3}";
            }

            try
            {
                bool success = await _testCaseRepository.AddTestCaseAsync(newCase);
                if (success)
                {
                    return new ApiResponse(success, "新的评估测试用例添加成功！", newCase, ResponseCode.Success);
                }
                else
                {
                    return new ApiResponse(false, "评估测试用例添加失败！", code: ResponseCode.InternalError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"评估测试用例添加过程中发生异常！\n异常信息：{ex}");
                return new ApiResponse(false, "评估测试用例添加过程中发生异常!", code: ResponseCode.InternalError);
            }
        }

        // 修改某一条测试用例
        public async Task<ApiResponse> UpdateTestCaseAsync(AgentTestCaseUpdateDto caseDto)
        {
            if (caseDto == null)
            {
                return new ApiResponse(false, "传入评估测试用例的Id有误！", code: ResponseCode.BadRequest);
            }

            string expectedActionJson = ConvertListToJsonString(caseDto.ExpectedActions);
            string expectedContainsJson = ConvertListToJsonString(caseDto.ExpectedAnswerContains);

            try
            {
                var updatedCase = await _testCaseRepository.UpdateTestCaseAsync(caseDto, expectedActionJson, expectedContainsJson);
                if (updatedCase == null)
                {
                    return new ApiResponse(false, "评估测试用例更新时出错了！", code: ResponseCode.BadRequest);
                }
                else
                {
                    return new ApiResponse(true, "评估测试用例更新成功！", updatedCase, ResponseCode.Success);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"更新评估测试用例的途中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "更新评估测试用例的途中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 修改某一条测试用例的启用状态
        public async Task<ApiResponse> UpdateTestCaseStatusAsync(int caseId)
        {
            if (caseId <= 0)
            {
                return new ApiResponse(false, "传入用例Id不合法！", code: ResponseCode.BadRequest);
            }

            try
            {
                var enabledCase = await _testCaseRepository.FindByIdAsync(caseId);
                if (enabledCase == null)
                {
                    return new ApiResponse(false, "传入ID有误，未能找到该条评估测试用例！", code: ResponseCode.NotFound);
                }
                else
                {
                    if (enabledCase.IsEnabled)
                    {
                        return await _testCaseRepository.DisableCaseAsync(caseId);
                    }
                    else
                    {
                        return await _testCaseRepository.EnableCaseAsync(caseId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"在更新评估测试用例的过程中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "在更新评估测试用例的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 删除某一条测试用例（逻辑删除）
        public async Task<ApiResponse> DeleteTestCaseAsync(int caseId)
        {
            if (caseId <= 0)
            {
                return new ApiResponse(false, "传入用例Id不合法！", code: ResponseCode.BadRequest);
            }

            try
            {
                return await _testCaseRepository.DeleteCaseAsync(caseId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"删除该条评估测试用例的过程中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "删除该条评估测试用例的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 根据测试用例评估测试结果Id搜索对应工作流日志
        public async Task<ApiResponse> GetWorkflowLogByCaseIdAsync(long runId, int caseId)
        {
            if (runId <= 0 || caseId <= 0)
            {
                return new ApiResponse(false, "传入评估批次结果Id或用例Id不合法！", code: ResponseCode.BadRequest);
            }

            try
            {
                var caseRunResult = await _resultRepository.FindResultByRunIdAndCaseIdAsync(runId, caseId);
                if (caseRunResult == null)
                {
                    return new ApiResponse(false, "未能找到对应的评估测试结果记录！", code: ResponseCode.NotFound);
                }
                else
                {
                    int? workflowLogId = caseRunResult.WorkflowLogId;
                    if (workflowLogId == null) 
                    {
                        return new ApiResponse(false, "该条评估测试结果无工作流日志，请确认该条评估测试结果是否执行成功！", code: ResponseCode.NotFound);
                    }
                    else
                    {
                        var workflowLog = await _workflowLogRepository.GetLogByIdAsync(workflowLogId.Value);
                        if (workflowLog == null)
                        {
                            return new ApiResponse(false, "未能找到对应的工作流日志记录！", code: ResponseCode.NotFound);
                        }
                        else
                        {
                            var resultDto = new AgentEvaluationWorkflowLogDTO(workflowLog);
                            return new ApiResponse(true, "对应的工作流日志查询成功！", resultDto, ResponseCode.Success);
                        }
                    }       
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"在查询评估测试结果记录的过程中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "在查询评估测试结果记录的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 根据测试评估批次结果生成相应markdown报告
        public async Task<ApiResponse> GetEvaluationReportAsync(long runId)
        {
            if (runId <= 0)
            {
                return new ApiResponse(false, "传入评估批次结果Id不合法！", code: ResponseCode.BadRequest);
            }
            try
            {
                var run = await _evaluationRunRepository.GetByIdAsync(runId);
                if (run == null)
                {
                    return new ApiResponse(false, "未能找到对应的评估测试批次记录！", code: ResponseCode.NotFound);
                }
                else
                {
                    var results = await _resultRepository.FindResultByRunId(runId);
                    if (results == null || results.Count == 0)
                    {
                        return new ApiResponse(false, "该批次下暂时没有评估测试结果记录！", code: ResponseCode.NotFound);
                    }
                    else
                    {
                        var report = GenerateReport(run, results);
                        return new ApiResponse(true, "评估测试报告生成成功！", report, ResponseCode.Success);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"在生成评估测试报告的过程中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "在生成评估测试报告的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 对不同评估批次做数据对比，查看评估结果变化趋势
        public async Task<ApiResponse> CompareEvaluationRunsAsync(long baseRunId, long targetRunId)
        {
            if (baseRunId <= 0 || targetRunId <= 0)
            {
                return new ApiResponse(false, "传入评估批次结果Id不合法！", code: ResponseCode.BadRequest);
            }

            try
            {
                // 根据传入ID搜索对应评估批次记录
                var baseRun = await _evaluationRunRepository.GetByIdAsync(baseRunId);
                var targetRun = await _evaluationRunRepository.GetByIdAsync(targetRunId);
                if (baseRun == null || targetRun == null)
                {
                    return new ApiResponse(false, "未能找到对应的评估测试批次记录！", code: ResponseCode.NotFound);
                }

                // 根据评估批次记录ID搜索对应评估结果
                var baseResults = await _resultRepository.FindResultByRunId(baseRunId);
                var targetResults = await _resultRepository.FindResultByRunId(targetRunId);

                if (baseResults == null || targetResults == null || baseResults.Count == 0 || targetResults.Count == 0)
                {
                    return new ApiResponse(false, "该批次下暂时没有评估测试结果记录！", code: ResponseCode.NotFound);
                }

                var baseMap = baseResults.ToDictionary(result => result.TestCaseId);
                var targetMap = targetResults.ToDictionary(result => result.TestCaseId);

                var allCaseIds = baseMap.Keys
                    .Union(targetMap.Keys)
                    .OrderBy(x => x)
                    .ToList();

                var compareCases = new List<AgentEvaluationCompareCaseDTO>();

                foreach (var caseId in allCaseIds) 
                {
                    baseMap.TryGetValue(caseId, out var baseResult);
                    targetMap.TryGetValue(caseId, out var targetResult);

                    var changeType = ResolveCompareChangeType(baseResult, targetResult);

                    // 构造对比结果DTO
                    var compareCaseDto = new AgentEvaluationCompareCaseDTO()
                    {
                        TestCaseId = caseId,
                        CaseName = targetResult?.CaseName ?? baseResult?.CaseName ?? $"Case {caseId}",
                        BasePassed = baseResult?.Passed,
                        TargetPassed = targetResult?.Passed,
                        BaseSemanticScore = baseResult?.SemanticScore,
                        TargetSemanticScore = targetResult?.SemanticScore,
                        ChangeType = changeType,
                        BaseFailureType = baseResult?.FailureType.ToString(),
                        TargetFailureType = targetResult?.FailureType.ToString(),
                        BaseAnswer = baseResult?.Answer,
                        TargetAnswer = targetResult?.Answer,
                        BaseActionsJson = baseResult?.ActualActionsJson ?? "[]",
                        TargetActionsJson = targetResult?.ActualActionsJson ?? "[]"
                    };

                    compareCases.Add(compareCaseDto);
                }

                // 统计数量，并构造最终对比结果DTO
                var dto = new AgentEvaluationCompareDTO
                {
                    BaseRunId = baseRunId,
                    TargetRunId = targetRunId,
                    BaseRun = new AgentEvaluationCompareRunDTO
                    {
                        RunId = baseRun.Id,
                        TotalCount = baseRun.TotalCount,
                        PassedCount = baseRun.PassedCount,
                        FailedCount = baseRun.FailedCount,
                        StartedAt = baseRun.StartedAt,
                        FinishedAt = baseRun.FinishedAt,
                        PlannerPromptVersion = baseRun.PlannerPromptVersion,
                        ActionRegistryVersion = baseRun.ActionRegistryVersion,
                        EvaluationVersion = baseRun.EvaluationVersion,
                        FinalAnswerPromptVersion = baseRun.FinalAnswerPromptVersion
                    },
                    TargetRun = new AgentEvaluationCompareRunDTO
                    {
                        RunId = targetRun.Id,
                        TotalCount = targetRun.TotalCount,
                        PassedCount = targetRun.PassedCount,
                        FailedCount = targetRun.FailedCount,
                        StartedAt = targetRun.StartedAt,
                        FinishedAt = targetRun.FinishedAt,
                        PlannerPromptVersion = targetRun.PlannerPromptVersion,
                        ActionRegistryVersion = targetRun.ActionRegistryVersion,
                        EvaluationVersion = targetRun.EvaluationVersion,
                        FinalAnswerPromptVersion = targetRun.FinalAnswerPromptVersion
                    },
                    Cases = compareCases,
                    FixedCount = compareCases.Count(x => x.ChangeType == "Fixed"),
                    RegressedCount = compareCases.Count(x => x.ChangeType == "Regressed"),
                    StillPassedCount = compareCases.Count(x => x.ChangeType == "StillPassed"),
                    StillFailedCount = compareCases.Count(x => x.ChangeType == "StillFailed"),
                    NewCaseCount = compareCases.Count(x => x.ChangeType == "NewCase"),
                    MissingCaseCount = compareCases.Count(x => x.ChangeType == "MissingCase")
                };

                return new ApiResponse(true, "评估批次对比查询成功！", dto, ResponseCode.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"在对比评估批次结果的过程中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "在对比评估批次结果的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 对不同评估批次做回归分析，生成评估摘要报告
        public async Task<ApiResponse> GetRegressionSummaryAsync(long baseRunId, long targetRunId)
        {
            if (baseRunId <= 0 || targetRunId <= 0)
            {
                return new ApiResponse(false, "传入评估批次结果Id不合法！", code: ResponseCode.BadRequest);
            }

            try
            {
                var compareResponse = await CompareEvaluationRunsAsync(baseRunId, targetRunId);
                if (!compareResponse.Success || compareResponse.Data is not AgentEvaluationCompareDTO compareDto)
                {
                    return new ApiResponse(false, "在对比评估批次结果的过程中出现了异常，无法生成回归分析报告！", code: ResponseCode.InternalError);
                }
                var summary = BuildRegressionSummary(compareDto);
                return new ApiResponse(true, "评估批次回归分析报告生成成功！", summary, ResponseCode.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"在生成评估批次回归分析报告的过程中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "在生成评估批次回归分析报告的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 对运行批次，根据批次Id保存快照到数据库
        public async Task<ApiResponse> SaveRunSnapshotAsync(long runId)
        {
            if (runId <= 0)
            {
                return new ApiResponse(false, "传入评估批次结果Id不合法！", code: ResponseCode.BadRequest);
            }
            try
            {
                var run = await _evaluationRunRepository.GetByIdAsync(runId);

                bool exist = await _snapshotRepository.ExistSnapshotByRunIdAsync(runId);
                if (exist) 
                {
                    return new ApiResponse(false, "该评估批次快照已存在，无法重复保存！", code: ResponseCode.Conflict);
                }

                if (run == null)
                {
                    return new ApiResponse(false, "未能找到对应的评估测试批次记录！", code: ResponseCode.NotFound);
                }
                var getReportResponse = await GetEvaluationReportAsync(runId);
                if (!getReportResponse.Success)
                {
                    return getReportResponse;
                }
                else
                {
                    var report = getReportResponse.Data as AgentEvaluationReportDTO;
                    if (report == null)
                    {
                        return new ApiResponse(false, "生成评估测试报告的过程中出现了异常，无法保存快照！", code: ResponseCode.InternalError);
                    }
                    else
                    {
                        if (run.PlannerPromptVersion == null || run.ActionRegistryVersion == null || run.EvaluationVersion == null || run.FinalAnswerPromptVersion == null)
                        {
                            return new ApiResponse(false, "评估测试批次的版本信息不完整，无法保存快照！", code: ResponseCode.InternalError);
                        }

                        // 构造快照实体
                        var snapshot = new AgentEvaluationReportSnapshot
                        {
                            RunId = runId,
                            FileName = report.FileName,
                            MarkdownContent = report.Markdown,
                            CreatedAt = DateTime.UtcNow,
                            PlannerPromptVersion = run.PlannerPromptVersion,
                            ActionRegistryVersion = run.ActionRegistryVersion,
                            EvaluationVersion = run.EvaluationVersion,
                            FinalAnswerPromptVersion = run.FinalAnswerPromptVersion,
                            IsDeleted = false
                        };

                        var savedSnapshot = await _snapshotRepository.AddSnapshotAsync(snapshot);

                        if (savedSnapshot != null)
                        {
                            return new ApiResponse(true, "评估批次快照保存成功！", savedSnapshot, ResponseCode.Success);
                        }
                        else
                        {
                            return new ApiResponse(false, "评估批次快照保存失败！", code: ResponseCode.InternalError);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"在保存评估批次快照的过程中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "在保存评估批次快照的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }

        // 根据运行批次Id获取对应快照信息
        public async Task<ApiResponse> GetSnapshotByRunIdAsync(long runId)
        {
            if (runId <= 0)
            {
                return new ApiResponse(false, "传入评估批次结果Id不合法！", code: ResponseCode.BadRequest);
            }
            try
            {
                bool exist = await _snapshotRepository.ExistSnapshotByRunIdAsync(runId);
                if (!exist)
                {
                    return new ApiResponse(false, "该评估批次快照不存在，请先保存快照！", code: ResponseCode.NotFound);
                }

                var snapshot = await _snapshotRepository.GetSnapshotByRunIdAsync(runId);
                if (snapshot == null)
                {
                    return new ApiResponse(false, "未能找到对应的评估测试批次快照记录！", code: ResponseCode.NotFound);
                }
                else
                {
                    return new ApiResponse(true, "评估批次快照查询成功！", snapshot, ResponseCode.Success);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"在查询评估批次快照的过程中出现了异常！\n异常信息：{ex}");
                return new ApiResponse(false, "在查询评估批次快照的过程中出现了异常！", code: ResponseCode.InternalError);
            }
        }


        // ============    以下是工具方法    =============

        // 执行 Agent 评估测试用例：对比实际执行结果与预期
        private async Task<ApiResponse> EvaluateRequestAsync(AgentEvaluationRunRequest request, int userId)
        {
            // 构造用户消息实体，若未传 SessionId 则生成一个临时的评估专用 ID
            var agentRequest = new AgentUserMessage
            {
                Content = request.UserMessage,
                SessionId = string.IsNullOrWhiteSpace(request.SessionId)
                    ? $"eval-{Guid.NewGuid():N}"
                    : request.SessionId,
                UserId = userId,
                Role = AgentMessageRole.User,
                IsEvaluation = true
            };

            // 执行工作流并开启 debug 模式，以便获取计划详情用于验证
            var response = await _workflowService.AskAsync(agentRequest, userId, debug: true);

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

            // 对执行成功的做语义检测
            AgentSemanticJudgeResult? semanticJudgeResult = null;

            if (request.EnabledSemanticJudge)
            {
                semanticJudgeResult = await JudgeAnswerAsync(request, response);

                if (semanticJudgeResult != null && !semanticJudgeResult.Passed)
                {
                    errors.Add($"语义评估未通过：{semanticJudgeResult.Reason}");
                }
            }
            else
            {
                foreach (var word in request.ExpectedAnswerContains)
                {
                    if (string.IsNullOrWhiteSpace(word))
                    {
                        continue;
                    }

                    if (!response.Answer.Contains(word, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"最终回答缺少预期内容：{word}");
                    }
                }
            }

            // 构建评估结果对象
            var result = new AgentEvaluationRunResultDTO
            {
                CaseName = request.CaseName,
                Passed = errors.Count == 0,
                WorkflowLogId = response.WorkflowLogId,
                Errors = errors,
                Answer = response.Answer,
                ActualActions = actualActions,
                ActualRequiresConfirmation = response.RequiresConfirmation,
                ActualSuccess = response.Success,
                SemanticScore = semanticJudgeResult?.Score,
                SemanticJudgePassed = semanticJudgeResult?.Passed,
                SemanticJudgeReason = semanticJudgeResult?.Reason,
                FailureType = ResolveFailureType(errors)
            };

            // 返回评估结果
            return new ApiResponse(true, "Agent Evaluation 执行完成", result, ResponseCode.Success);
        }

        // 判断模型最终回复内容是否偏离用户输入
        private async Task<AgentSemanticJudgeResult?> JudgeAnswerAsync(
            AgentEvaluationRunRequest request,
            AgentAskResponse response)
        {
            // 从请求中获取评估样本输入内容
            var userMessage = request.UserMessage;
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return new AgentSemanticJudgeResult
                {
                    Passed = false,
                    Reason = "用户输入为空，无法进行语义评估"
                };
            }

            // 从请求中获取评估样本预期回答要点
            var expectedSummary = request.ExpectedAnswerSummary;
            if (string.IsNullOrWhiteSpace(expectedSummary) && request.ExpectedAnswerContains.Count > 0)
            {
                expectedSummary = string.Join(", ", request.ExpectedAnswerContains);
            }

            if (string.IsNullOrWhiteSpace(expectedSummary))
            {
                return null;
            }

            // 从响应中获取模型最终输出内容
            var agentMessage = response.Answer;
            if (string.IsNullOrWhiteSpace(agentMessage))
            {
                return new AgentSemanticJudgeResult
                {
                    Passed = false,
                    Reason = "Agent 最终回答为空，无法进行语义评估"
                };
            }

            // 构造 AI Prompt
            var chatMessages = new List<ChatMessage>();
            chatMessages.Add
            (
                new ChatMessage
                (
                    ChatRole.System,
                    """
                    你是 Agent Evaluation 评估器。
                    请根据提供的“用户输入”、“预期回答要点”、“实际回答”，判断“实际回答”是否满足“预期回答要点”。
                    
                    【评估执行逻辑】（严格按此步骤思考后再输出分数）：
                    1.核对要点覆盖：仔细检查“预期回答要点”中的每一条，判断“实际回答”是否准确包含并回答了这些内容。
                    2.检查额外偏差（惩罚项）：检查“实际回答”中是否包含与问题无关的错误信息、幻觉内容，或者是否包含危险 / 违规言论。如有，需大幅降分。
                    3.你不能根据自己的常识或猜测判断事实是否正确。
                    4.如果预期答案要点中没有提供精确数值，不要因为实际回答中的数值而扣分。
                    5.你只能根据用户问题、预期答案要点、Agent 实际回答三者进行评估。
                    6.综合打分：根据上述核查结果打分。

                    【评分标准】
                    -1.00 分：完全满足所有预期要点，表述清晰准确，无幻觉或偏差。
                    -0.70 分：基本满足所有预期要点，只有极小细节遗漏，不影响核心理解。
                    -0.50 分：只回答了部分预期要点，遗漏了多个关键信息，或存在轻微逻辑偏差。
                    -0.25 分：几乎没回答到预期要点上，答非所问，或存在严重幻觉。
                    -0.00 分：回答完全错误，或包含危险、不安全的违规内容。

                    【判定通过规则】
                    当且仅当综合得分 >= 0.7 时，passed 为 true，否则为 false。
                    
                    【输出强制要求】
                    - 你必须只返回一个合法 JSON 对象。
                    - 不要返回 Markdown。
                    - 不要返回 ```json 代码块。
                    - 不要在 JSON 前后添加任何解释文字。
                    - score 必须是数字，例如 0.70。
                    - passed 必须是布尔值 true 或 false。
                    - reason 必须是字符串。

                    【正确输出示例】
                    {
                      "score": 0.70,
                      "passed": true,
                      "reason": "实际回答覆盖了核心要点，只有轻微措辞差异。"
                    }

                    【错误输出示例】
                    {
                      "score": 0.0到1.0之间保留2位小数的浮点数,
                      "passed": true或false,
                      "reason": "xxx"
                    }
                    """
                )
            );
            chatMessages.Add
            (
                new ChatMessage
                (
                    ChatRole.User,
                    $"""
                    用户输入：{userMessage}
                    预期回答要点：{expectedSummary}
                    实际回答：{agentMessage}
                    """
                )
            );

            string? rawText = null;
            string? json = null;

            try
            {
                var judgeResponse = await _chatClient    // 将写好的 AI Prompt 提交到 LLM，并等待返回响应
                    .GetResponseAsync(chatMessages)
                    .WaitAsync(SemanticJudgeTimeout);

                rawText = judgeResponse.Text;        // 从 LLM 响应中提取结果文本
                json = ExtractJsonObject(rawText);   // 从结果文本中提取构造json串

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new AgentSemanticJudgeResult
                    {
                        Passed = false,
                        Reason = "语义评估返回内容为空或未包含有效 JSON"
                    };
                }

                var result = JsonSerializer.Deserialize<AgentSemanticJudgeResult>(    // 将 json 串反序列化为结果类 DTO
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result == null)
                {
                    return new AgentSemanticJudgeResult
                    {
                        Passed = false,
                        Reason = "语义评估 JSON 反序列化结果为空"
                    };
                }

                result.Score = Math.Clamp(result.Score, 0, 1);
                result.Passed = result.Score >= request.SemanticJudgeThreshold;

                if (string.IsNullOrWhiteSpace(result.Reason))
                {
                    result.Reason = result.Passed ? "语义评估通过" : "语义相似度低于阈值";
                }

                return result;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "语义评估超时");
                return new AgentSemanticJudgeResult { Passed = false, Reason = "语义评估超时" };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "语义评估 JSON 解析失败");
                return new AgentSemanticJudgeResult { Passed = false, Reason = "语义评估 JSON 解析失败" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "语义评估执行异常");
                return new AgentSemanticJudgeResult { Passed = false, Reason = "语义评估执行异常" };
            }
        }

        // 从可能包含 Markdown 代码块或多余文本的字符串中提取纯 JSON 对象字符串
        private static string? ExtractJsonObject(string? text)
        {
            // 空字符串直接返回 null
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var value = text.Trim();

            // 如果包含 Markdown 代码块标记，则移除 ``` 开头和结尾
            if (value.StartsWith("```"))
            {
                // 跳过第一行（可能包含 "json" 标记）
                var firstNewLineIndex = value.IndexOf('\n');
                if (firstNewLineIndex >= 0)
                {
                    value = value[(firstNewLineIndex + 1)..];
                }

                // 移除结尾的 ```
                if (value.EndsWith("```"))
                {
                    value = value[..^3];
                }

                value = value.Trim();
            }

            // 定位第一个 '{' 和最后一个 '}' 的位置
            var startIndex = value.IndexOf('{');
            var endIndex = value.LastIndexOf('}');

            // 如果未找到有效的 JSON 边界，返回 null
            if (startIndex < 0 || endIndex < startIndex)
            {
                return null;
            }

            // 截取并返回 JSON 对象字符串
            return value[startIndex..(endIndex + 1)];
        }

        // 将测试用例的 Json 由 string json 反序列化为 List<string>
        private List<string> ConvertStringJsonToList(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "反序列化字符串列表 JSON 时出错，JSON: {Json}", json);
                return new List<string>();
            }
        }

        // 将测试用例DTO的 list 列表由 List<string> 序列化为 Json
        private string ConvertListToJsonString(List<string> list)
        {
            try
            {
                return JsonSerializer.Serialize(list);
            }
            catch (Exception ex) // 一般不会发生，但为了完整性
            {
                _logger.LogError(ex, "序列化字符串列表为 JSON 时出错");
                return "[]";
            }
        }

        // 将测试结果的信息添加到评估记录结果中
        private void FinalAdd(AgentEvaluationRunResultDTO dto, AgentEvaluationResult result)
        {
            if (result == null)
            {
                return;
            }

            if (dto == null)
            {
                return;
            }

            result.Passed = dto.Passed;
            result.ErrorsJson = JsonSerializer.Serialize<List<string>>(dto.Errors);
            result.Answer = dto.Answer;
            result.WorkflowLogId = dto.WorkflowLogId;
            result.ActualActionsJson = JsonSerializer.Serialize<List<string>>(dto.ActualActions);
            result.ActualSuccess = dto.ActualSuccess;
            result.ActualRequiresConfirmation = dto.ActualRequiresConfirmation;
            result.SemanticScore = dto.SemanticScore;
            result.SemanticJudgeReason = dto.SemanticJudgeReason;
            result.SemanticJudgePassed = dto.SemanticJudgePassed;
            result.FailureType = dto.FailureType;
        }

        // 对 Error 列表的信息做解析，转为对应错误类型
        private AgentEvaluationFailureType ResolveFailureType(List<string> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return AgentEvaluationFailureType.None;
            }

            if (errors.Any(x => x.Contains("缺少预期 Action")))
            {
                return AgentEvaluationFailureType.PlanActionMissing;
            }

            if (errors.Any(x => x.Contains("RequiresConfirmation 不符合预期")))
            {
                return AgentEvaluationFailureType.ConfirmationMismatch;
            }

            if (errors.Any(x => x.Contains("Success 不符合预期")))
            {
                return AgentEvaluationFailureType.SuccessMismatch;
            }

            if (errors.Any(x => x.Contains("最终回答缺少预期内容")))
            {
                return AgentEvaluationFailureType.KeywordMismatch;
            }

            if (errors.Any(x => x.Contains("语义评估未通过")))
            {
                return AgentEvaluationFailureType.SemanticMismatch;
            }

            return AgentEvaluationFailureType.Unknown;
        }

        // 根据运行批次与运行结果生成 Markdown 格式的评估报告
        private AgentEvaluationReportDTO GenerateReport(AgentEvaluationRun run, List<AgentEvaluationResult> results)
        {
            if (run == null)
                throw new ArgumentNullException(nameof(run));

            results ??= new List<AgentEvaluationResult>();

            // ----- 统计数据 -----
            int total = results.Count;
            int passed = results.Count(r => r.Passed);
            int failed = total - passed;
            double passRate = total > 0 ? (double)passed / total * 100 : 0;

            var failureGroups = results
                .Where(r => !r.Passed && r.FailureType != AgentEvaluationFailureType.None)
                .GroupBy(r => r.FailureType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count);

            // ----- 构建 Markdown -----
            var sb = new StringBuilder();

            // 标题
            sb.AppendLine("# 评估报告");
            sb.AppendLine();

            // 批次信息
            sb.AppendLine("## 批次信息");
            sb.AppendLine($"- **批次ID**: {run.Id}");
            sb.AppendLine($"- **模型**: {run.ModelUsed ?? "未指定"}");
            sb.AppendLine($"- **开始时间**: {run.StartedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **结束时间**: {(run.FinishedAt.HasValue ? run.FinishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "进行中")}");
            sb.AppendLine($"- **总用例数**: {total}");
            sb.AppendLine($"- **通过数**: {passed}");
            sb.AppendLine($"- **失败数**: {failed}");
            sb.AppendLine($"- **通过率**: {passRate:F2}%");

            // ---- 新增版本信息 ----
            sb.AppendLine($"- **计划器提示版本**: {run.PlannerPromptVersion ?? "未指定"}");
            sb.AppendLine($"- **动作注册版本**: {run.ActionRegistryVersion ?? "未指定"}");
            sb.AppendLine($"- **评估版本**: {run.EvaluationVersion ?? "未指定"}");
            sb.AppendLine($"- **最终答案提示版本**: {run.FinalAnswerPromptVersion ?? "未指定"}");

            if (!string.IsNullOrEmpty(run.Remark))
                sb.AppendLine($"- **备注**: {run.Remark}");
            sb.AppendLine();

            // 失败类型统计
            sb.AppendLine("## 失败类型统计");
            if (failed > 0 && failureGroups.Any())
            {
                sb.AppendLine("| 失败类型 | 数量 | 占比 |");
                sb.AppendLine("|---------|------|------|");
                foreach (var g in failureGroups)
                {
                    double percent = (double)g.Count / failed * 100;
                    sb.AppendLine($"| {g.Type} | {g.Count} | {percent:F2}% |");
                }
            }
            else
            {
                sb.AppendLine("🎉 所有用例均通过，无失败类型。");
            }
            sb.AppendLine();

            // 详细结果列表
            sb.AppendLine("## 详细结果");
            sb.AppendLine("| 用例名称 | 是否通过 | 失败类型 | 语义得分 | 语义判断通过 | 失败详情 |");
            sb.AppendLine("|---------|---------|---------|---------|-------------|---------|");

            foreach (var result in results.OrderBy(r => r.CaseName))
            {
                string caseName = string.IsNullOrEmpty(result.CaseName)
                    ? $"Case {result.TestCaseId}"
                    : result.CaseName;
                string passedText = result.Passed ? "✅ 通过" : "❌ 失败";
                string failureType = result.Passed ? "-" : result.FailureType.ToString();
                string semanticScore = result.SemanticScore?.ToString("F2") ?? "-";
                string semanticPassed = result.SemanticJudgePassed.HasValue
                    ? (result.SemanticJudgePassed.Value ? "是" : "否")
                    : "-";

                // 失败详情：优先显示 ErrorsJson，其次显示语义评判理由，否则显示失败类型
                string details = "-";
                if (!result.Passed)
                {
                    if (!string.IsNullOrEmpty(result.ErrorsJson) && result.ErrorsJson != "[]")
                    {
                        details = Truncate(result.ErrorsJson, 100);
                    }
                    else if (!string.IsNullOrEmpty(result.SemanticJudgeReason))
                    {
                        details = Truncate(result.SemanticJudgeReason, 100);
                    }
                    else
                    {
                        details = failureType;
                    }
                }

                string escapedCaseName = EscapeMarkdownTableCell(caseName);
                string escapedDetails = EscapeMarkdownTableCell(details);

                sb.AppendLine($"| {escapedCaseName} | {passedText} | {failureType} | {semanticScore} | {semanticPassed} | {escapedDetails} |");
            }

            string markdown = sb.ToString();

            string fileName = $"EvaluationReport_Run{run.Id}_{run.StartedAt:yyyyMMddHHmmss}.md";

            return new AgentEvaluationReportDTO
            {
                RunId = run.Id,
                FileName = fileName,
                Markdown = markdown
            };
        }

        // 截断字符串
        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        // 对 Markdown 表格单元格内容进行转义处理
        private static string EscapeMarkdownTableCell(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "-";
            }

            return text
                .Replace("|", "\\|")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();
        }

        // 根据基准结果和目标结果判断测试用例的变化类型
        private string ResolveCompareChangeType(
            AgentEvaluationResult? baseResult,
            AgentEvaluationResult? targetResult)
        {
            // 基准不存在，目标存在 → 新增用例
            if (baseResult == null && targetResult != null)
            {
                return "NewCase";
            }

            // 基准存在，目标不存在 → 缺失用例
            if (baseResult != null && targetResult == null)
            {
                return "MissingCase";
            }

            // 两者都不存在（理论上不会发生），返回未知
            if (baseResult == null || targetResult == null)
            {
                return "Unknown";
            }

            // 基准失败 → 目标通过 → 修复
            if (!baseResult.Passed && targetResult.Passed)
            {
                return "Fixed";
            }

            // 基准通过 → 目标失败 → 退化
            if (baseResult.Passed && !targetResult.Passed)
            {
                return "Regressed";
            }

            // 两者均通过 → 持续通过
            if (baseResult.Passed && targetResult.Passed)
            {
                return "StillPassed";
            }

            // 两者均失败 → 持续失败
            return "StillFailed";
        }

        // 根据对比数据构建评估回归摘要，包含决策、标题、摘要、亮点、风险和建议操作
        private AgentEvaluationRegressionSummaryDTO BuildRegressionSummary(AgentEvaluationCompareDTO compare)
        {
            // 映射基础统计数据
            var dto = new AgentEvaluationRegressionSummaryDTO(compare);

            // 存在退化用例 → 阻断
            if (compare.RegressedCount > 0)
            {
                dto.Decision = EvluationDecisionResultType.Blocked;
                dto.Title = "本次评估存在退化，不建议合入";
                dto.Summary = $"目标批次相比基准批次出现 {compare.RegressedCount} 个退化用例，需要优先排查。";
                dto.Risks.Add("存在原本通过、现在失败的用例，说明本次改动破坏了已有能力。");
                dto.NextActions.Add("优先查看退化用例的 WorkflowLog，定位是 Planner、Action 执行还是最终回答问题。");
            }
            // 无退化，但仍有持续失败或缺失用例 → 警告
            else if (compare.StillFailedCount > 0 || compare.MissingCaseCount > 0)
            {
                dto.Decision = EvluationDecisionResultType.Warning;
                dto.Title = "本次评估无退化，但仍有遗留风险";
                dto.Summary = $"没有发现退化；修复 {compare.FixedCount} 个用例，仍有 {compare.StillFailedCount} 个持续失败用例。";
                dto.Risks.Add("仍有持续失败用例，说明 Agent 能力或评估用例还存在待处理问题。");

                if (compare.MissingCaseCount > 0)
                {
                    dto.Risks.Add($"目标批次缺失 {compare.MissingCaseCount} 个基准用例，可能导致对比不完整。");
                }

                dto.NextActions.Add("优先处理持续失败用例中重复出现的失败类型。");
                dto.NextActions.Add("如果持续失败是预期 TODO，可以保留，但需要在报告中标注。");
            }
            // 无退化、无持续失败、无缺失 → 通过
            else
            {
                dto.Decision = EvluationDecisionResultType.Pass;
                dto.Title = "本次评估通过，可以接受";
                dto.Summary = $"没有退化和持续失败；修复 {compare.FixedCount} 个用例，持续通过 {compare.StillPassedCount} 个用例。";
                dto.Highlights.Add("没有发现回归退化。");
                dto.Highlights.Add("目标批次整体表现不低于基准批次。");
                dto.NextActions.Add("可以把当前版本作为新的评估基线。");
            }

            // 补充修复数量的亮点
            if (compare.FixedCount > 0)
            {
                dto.Highlights.Add($"本次修复了 {compare.FixedCount} 个历史失败用例。");
            }

            // 补充新增用例的亮点
            if (compare.NewCaseCount > 0)
            {
                dto.Highlights.Add($"目标批次新增覆盖了 {compare.NewCaseCount} 个用例。");
            }

            // 从所有退化用例中取前5个添加到风险列表，便于快速定位
            var regressedCases = compare.Cases
                .Where(x => x.ChangeType == "Regressed")
                .Select(x => x.CaseName)
                .Take(5)
                .ToList();

            foreach (var caseName in regressedCases)
            {
                dto.Risks.Add($"退化用例：{caseName}");
            }

            return dto;
        }

        // 根据请求执行测试
        private async Task<ApiResponse> RunEvaluationAsync(
            int userId, string remark,
            List<AgentEvaluationRunRequest> requests,
            long? sourceId = null)
        {
            // 在进入评估逻辑前，创建一条评估批次记录，在评估结束后，更新剩余数据，将记录存入数据库
            string? modelProvider = _configuration["ModelProvider"];   // 从配置文件中读取模型提供方
            if (modelProvider == null)
            {
                return new ApiResponse(false, "读取评估使用模型过程中出现错误！", code: ResponseCode.InternalError);
            }

            var evaluationRun = new AgentEvaluationRun   // 创建一条评估批次记录
            {
                TotalCount = requests.Count,
                ModelUsed = modelProvider,
                StartedAt = DateTime.UtcNow,
                PlannerPromptVersion = AgentVersionConstants.PlannerPromptVersion,
                ActionRegistryVersion = AgentVersionConstants.ActionRegistryVersion,
                EvaluationVersion = AgentVersionConstants.EvaluationVersion,
                FinalAnswerPromptVersion = AgentVersionConstants.FinalAnswerPromptVersion
            };

            if (sourceId.HasValue)
            {
                evaluationRun.SourceId = sourceId.Value;
            }

            var preparedRun = await _evaluationRunRepository.AddAsync(evaluationRun);
            if (preparedRun == null)
            {
                return new ApiResponse(false, "保存评估批次记录的过程中出现错误！", code: ResponseCode.InternalError);
            }

            var testCaseRunResults = new List<AgentEvaluationResult>();
            var dtoResults = new List<AgentEvaluationRunResultDTO>();

            foreach (var request in requests)
            {
                // 初始化一条评估执行结果记录
                var runResult = new AgentEvaluationResult
                {
                    RunId = preparedRun.Id,
                    TestCaseId = request.CaseId,
                    CaseName = request.CaseName,
                    TestCaseSnapshotJson = request.TestCaseSnapshotJson,
                    CreatedAt = DateTime.UtcNow
                };
                AgentEvaluationRunResultDTO result;

                // 对单条测试用例进行执行并获取执行结果
                ApiResponse evaluateResponse;
                try
                {
                    evaluateResponse = await EvaluateRequestAsync(request, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"在评估测试执行过程中出现问题！\n异常信息：{ex}");

                    // 构建抛出异常情况下的结果信息
                    result = new AgentEvaluationRunResultDTO
                    {
                        CaseName = request.CaseName,
                        Passed = false,
                        Errors = new List<string> { "该测试案例在执行过程中出现了问题！" },
                        Answer = "该测试案例在执行过程中抛出异常，没有回复消息！",
                        ActualActions = new List<string>(),
                        ActualRequiresConfirmation = false,
                        ActualSuccess = false,
                        SemanticScore = 0,
                        SemanticJudgePassed = false,
                        SemanticJudgeReason = "该测试案例在执行过程中抛出异常，未进行语义判断处理！",
                        FailureType = AgentEvaluationFailureType.RunTimeError
                    };
                    evaluateResponse = new ApiResponse(false, "在评估测试执行过程中出现了问题！", result, code: ResponseCode.InternalError);
                }

                if (evaluateResponse.Data is AgentEvaluationRunResultDTO successResult)
                {
                    result = successResult;
                    dtoResults.Add(result);
                }
                else
                {
                    result = new AgentEvaluationRunResultDTO
                    {
                        CaseName = request.CaseName,
                        Passed = false,
                        Errors = new List<string> { evaluateResponse.Message },
                        FailureType = AgentEvaluationFailureType.ResultFormatError
                    };
                    dtoResults.Add(result);
                }

                // 对评估执行结果做信息添加
                FinalAdd(result, runResult);
                testCaseRunResults.Add(runResult);
            }

            // 构建结果信息，将结果信息返回
            var batchResult = new AgentEvaluationBatchResultDTO(dtoResults);

            // 批量保存评估结果记录到数据库中
            bool success = await _resultRepository.AddRangeAsync(testCaseRunResults);
            if (!success)
            {
                return new ApiResponse(false, "评估结果保存失败！", batchResult, ResponseCode.InternalError);
            }

            var finishedAt = DateTime.UtcNow;

            // 将评估结果信息添加到批次记录中，并对本条批次记录做入库保存
            var finishedRun = await _evaluationRunRepository
                .FinishAsync(preparedRun.Id,
                batchResult.Passed,
                batchResult.Failed,
                finishedAt,
                remark);

            if (finishedRun == false)
            {
                return new ApiResponse(false, "评估批次收尾更新失败！", batchResult, ResponseCode.InternalError);
            }

            return new ApiResponse(true, "所有测试用例执行完成", batchResult, ResponseCode.Success);
        }
    }
}
