using CuteBlogSystem.DTO.AIShield;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CuteBlogSystem.Service
{
    // 封装 AIShield 安全中间件调用，供 Agent 输入、输出和工具调用检测复用
    public class AIShieldService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIShieldService> _logger;

        // 反序列化时忽略大小写，兼容 allowed/Allowed 两种返回
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AIShieldService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AIShieldService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // 判断 AIShield 是否启用
        public bool IsEnabled => _configuration.GetValue("AIShield:Enabled", false);

        // AIShield 不可用时是否放行，默认 false 表示失败即阻断
        private bool FailOpenOnError => _configuration.GetValue("AIShield:FailOpenOnError", false);

        // 检测用户输入，应该在进入 Agent 工作流前调用
        public Task<AIShieldCheckResult> CheckInputAsync(string content, int userId)
        {
            // 将用户输入和匿名用户标识提交给 AIShield
            return CallAsync("/api/security/check-input", new
            {
                content,
                subjectHash = BuildSubjectHash(userId)
            });
        }

        // 检测 Agent 输出，应该在返回用户和保存 assistant 消息前调用
        public Task<AIShieldCheckResult> CheckOutputAsync(string content, int userId)
        {
            // 将模型最终输出和匿名用户标识提交给 AIShield
            return CallAsync("/api/security/check-output", new
            {
                content,
                subjectHash = BuildSubjectHash(userId)
            });
        }

        // 检测工具调用，应该在执行 AgentPlanStep 前调用
        public Task<AIShieldCheckResult> CheckToolCallAsync(
            string toolName,
            IDictionary<string, object> arguments)
        {
            // 将工具名称和参数提交给 AIShield
            return CallAsync("/api/security/check-tool-call", new
            {
                toolName,
                arguments
            });
        }

        // 调用 AIShield 指定检测接口并统一处理响应
        private async Task<AIShieldCheckResult> CallAsync(string path, object body)
        {
            // 未启用 AIShield 时直接放行，避免影响普通开发流程
            if (!IsEnabled)
            {
                return AIShieldCheckResult.Allow();
            }

            // 读取 Agent Key，AIShield 通过 X-API-Key 识别接入方
            var agentKey = _configuration["AIShield:AgentKey"];
            if (string.IsNullOrWhiteSpace(agentKey))
            {
                return AIShieldCheckResult.Block("AIShield 已被启用！但是 AgentKey 缺失，请在配置中提供有效的 AgentKey。");
            }

            try
            {
                // 构造 POST 请求，并使用相对路径拼接 HttpClient.BaseAddress
                using var request = new HttpRequestMessage(HttpMethod.Post, path)
                {
                    Content = JsonContent.Create(body)
                };

                // 添加 AIShield 接口鉴权头
                request.Headers.TryAddWithoutValidation("X-API-Key", agentKey);

                // 发送请求并读取原始响应文本，方便异常时记录
                using var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                // 非 2xx 响应按配置决定阻断或放行
                if (!response.IsSuccessStatusCode)
                {
                    return HandleFailure($"AIShield 请求失败：HTTP {(int)response.StatusCode}。{content}");
                }

                // 将 AIShield 响应转换为统一 DTO
                var result = JsonSerializer.Deserialize<AIShieldCheckResult>(content, _jsonOptions);
                return result ?? HandleFailure("AIShield 返回了空或无效的内容。");
            }
            catch (Exception ex)
            {
                // 网络异常、序列化异常等都走统一失败策略
                _logger.LogError(ex, "AIShield 调用失败， 路径： {Path}", path);
                return HandleFailure($"AIShield 调用失败； {ex.Message}");
            }
        }

        // 根据失败策略处理 AIShield 调用失败
        private AIShieldCheckResult HandleFailure(string reason)
        {
            // 如果配置为失败放行，则记录警告并允许继续执行
            if (FailOpenOnError)
            {
                _logger.LogWarning("AIShield failed open. Reason: {Reason}", reason);
                return AIShieldCheckResult.Allow();
            }

            // 默认安全策略：AIShield 不可用时阻断 Agent 流程
            return AIShieldCheckResult.Block(reason);
        }

        // 生成匿名用户标识，避免直接把真实 userId 发送给 AIShield
        private static string BuildSubjectHash(int userId)
        {
            // 用 SHA256 哈希 userId，满足审计关联但不暴露真实 ID
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"user:{userId}"));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
