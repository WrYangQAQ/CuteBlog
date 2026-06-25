namespace CuteBlogSystem.DTO.AIShield
{
    // AIShield 三类检测接口的统一响应结果
    public class AIShieldCheckResult
    {
        // 是否允许继续执行
        public bool Allowed { get; set; } = true;

        // 建议处理动作，例如 Allow、Warn、Block、Mask、NeedApproval
        public string Action { get; set; } = "Allow";

        // 风险等级，例如 None、Low、Medium、High、Critical
        public string RiskLevel { get; set; } = "None";

        // AIShield 处理后的内容，常用于输出脱敏或替换
        public string? ProcessedContent { get; set; }

        // 命中规则或阻断时给出的原因
        public string? Reason { get; set; }

        // 命中的安全规则编号列表
        public List<string> HitRules { get; set; } = new();

        // 构造一个默认放行结果
        public static AIShieldCheckResult Allow() => new();

        // 构造一个阻断结果，用于本地配置缺失或 AIShield 调用失败时兜底
        public static AIShieldCheckResult Block(string reason) => new()
        {
            Allowed = false,
            Action = "Block",
            RiskLevel = "High",
            Reason = reason
        };

        // 判断当前响应是否应该阻断后续 Agent 流程
        public bool ShouldBlock()
        {
            // allowed=false 或 action=Block 都视为必须阻断
            return !Allowed || string.Equals(Action, "Block", StringComparison.OrdinalIgnoreCase);
        }
    }
}
