using System.Text.Json;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Repository;

namespace CuteBlogSystem.Service
{
    // 负责将 Agent 工作流执行结果保存到数据库日志表
    public class AgentWorkflowLogService
    {
        private readonly AgentWorkflowLogRepository _agentWorkflowLogRepository;
        

        public AgentWorkflowLogService(AgentWorkflowLogRepository agentWorkflowLogRepository)
        {
            _agentWorkflowLogRepository = agentWorkflowLogRepository;
            
        }

        // 保存一次工作流执行的完整日志（包含调试信息）
        public async Task<ApiResponse> SaveAsync(
            string userMessage,
            AgentAskResponse response,
            DateTime startedAt,
            DateTime finishedAt)
        {
            var debug = response.Debug;

            // 构建日志实体对象
            var log = new AgentWorkflowLog
            {
                UserMessage = userMessage,
                Success = response.Success,
                Recovered = response.Recovered,
                Message = response.Message,
                Answer = response.Answer,
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                DurationMs = (long)(finishedAt - startedAt).TotalMilliseconds, // 计算耗时

                // 序列化调试信息中的各个对象，若为 null 则存 null
                PlanJson = SerializeOrNull(debug?.Plan),
                ExecutionResultJson = SerializeOrNull(debug?.ExecutionResult),
                FailureAnalysis = debug?.FailureAnalysis,
                RecoveryPlanJson = SerializeOrNull(debug?.RecoveryPlan),
                RecoveryExecutionResultJson = SerializeOrNull(debug?.RecoveryExecutionResult)
            };

            try
            {
                bool success = await _agentWorkflowLogRepository.AddLogAsync(log);

                if (success)
                {
                    var apiResponse = new ApiResponse
                    (
                        true,
                        "日志保存成功",
                        null,
                        ResponseCode.Success
                    );
                    return apiResponse;
                }
                else
                {
                    var apiResponse = new ApiResponse
                    (
                        false,
                        "日志保存失败，数据库操作未成功",
                        null,
                        ResponseCode.InternalError
                    );
                    return apiResponse;
                }
            }
            catch (Exception ex)
            {
                var apiResponse = new ApiResponse
                (
                    false,
                    $"日志保存失败: {ex.Message}",
                    code: ResponseCode.InternalError
                );
                return apiResponse;
            }
        }

        // 查询所有日志记录
        public async Task<ApiResponse> GetAllLogsAsync()
        {
            ApiResponse response;

            try
            {
                var logs = await _agentWorkflowLogRepository.GetAllLogsAsync();
                if(logs == null)
                {
                    response = new ApiResponse
                    (
                        false,
                        "没有找到日志记录",
                        code:ResponseCode.NotFound
                    );
                    return response;
                }
                response = new ApiResponse
                (
                    true,
                    "日志查询成功",
                    logs,
                    ResponseCode.Success
                );
                return response;
            }
            catch (Exception ex)
            {
                response = new ApiResponse
                (
                    false,
                    $"日志查询失败: {ex.Message}",
                    code:ResponseCode.InternalError
                );
                return response;
            }
        }

        // 查询指定时间范围内的日志记录
        public async Task<ApiResponse> GetLogsByTimeRangeAsync(DateTime? from, DateTime? to)
        {
            if(from == null || to == null)
            {
                return new ApiResponse
                (
                    false,
                    "无效的时间范围，请确保开始时间和结束时间正确",
                    code:ResponseCode.BadRequest
                );
            }

            if(from > to)
            {
                return new ApiResponse
                (
                    false,
                    "无效的时间范围，开始时间不能晚于结束时间",
                    code:ResponseCode.BadRequest
                );
            }

            ApiResponse response;
            try
            {
                var logs = await _agentWorkflowLogRepository.GetLogsByTimeRangeAsync(from.Value, to.Value);
                if(logs == null || logs.Count == 0)
                {
                    response = new ApiResponse
                    (
                        false,
                        "选择的时间范围内没有找到日志记录，请重新选择时间范围",
                        code:ResponseCode.NotFound
                    );
                    return response;
                }
                response = new ApiResponse
                (
                    true,
                    "日志查询成功",
                    logs,
                    ResponseCode.Success
                );
                return response;
            }
            catch (Exception ex)
            {
                response = new ApiResponse
                (
                    false,
                    $"日志查询失败: {ex.Message}",
                    code:ResponseCode.InternalError
                );
                return response;
            } 
        }

        // 根据id查询单条日志记录
        public async Task<ApiResponse> GetLogByIdAsync(int id)
        {
            ApiResponse response;
            try
            {
                var log = await _agentWorkflowLogRepository.GetLogByIdAsync(id);

                if (log == null)
                {
                    response = new ApiResponse
                    (
                        false,
                        "没有找到对应的日志记录，请检查ID是否正确",
                        code:ResponseCode.NotFound
                    );
                    return response;
                }

                // 将 log 实体转为 DTO
                var logDto = new AgentWorkflowLogDetailDTO(log);

                response = new ApiResponse
                (
                    true,
                    "日志查询成功",
                    logDto,
                    ResponseCode.Success
                );
                return response;
            }
            catch (Exception ex)
            {
                response = new ApiResponse
                (
                    false,
                    $"日志查询失败: {ex.Message}",
                    code:ResponseCode.InternalError
                );
                return response;
            }
        }

        // 查询最近 count 条日志记录
        public async Task<ApiResponse> GetRecentLogAsync(int count = 20)
        {
            count = Math.Clamp(count, 1, 100);   // 将取出日志的数量限制为 1~100 条

            try
            {
                var logs = await _agentWorkflowLogRepository.GetRecentAsync(count);   // 取出日志

                var data = logs.Select(log => new AgentWorkflowLogListItemDTO(log)).ToList();

                return new ApiResponse(true, "获取 Agent 执行日志成功", data, ResponseCode.Success);
            }
            catch (Exception ex) 
            {
                return new ApiResponse(false, "获取 Agent 执行日志失败", code: ResponseCode.InternalError);
            }
        }

        // 将对象序列化为紧凑 JSON（不缩进），若 value 为 null 则返回 null
        private static string? SerializeOrNull(object? value)
        {
            if (value == null)
            {
                return null;
            }

            return JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false   // 节省数据库存储空间
            });
        }

        

    }
}