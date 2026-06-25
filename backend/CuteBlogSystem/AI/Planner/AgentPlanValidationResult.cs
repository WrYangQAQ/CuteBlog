namespace CuteBlogSystem.AI.Planner
{
    public class AgentPlanValidationResult
    {
        // 表示计划是否通过验证，true 表示有效，false 表示存在错误
        public bool IsValid { get; set; }

        // 存储验证失败时的错误信息列表，每个字符串描述一个具体的验证问题
        public List<string> Errors { get; set; } = new();

        // 静态方法：返回一个验证成功的结果对象（IsValid = true，Errors 为空列表）
        public static AgentPlanValidationResult Success()
        {
            return new AgentPlanValidationResult
            {
                IsValid = true
            };
        }

        // 静态方法：返回一个验证失败的结果对象，并记录指定的错误列表
        public static AgentPlanValidationResult Fail(List<string> errors)
        {
            return new AgentPlanValidationResult
            {
                IsValid = false,
                Errors = errors
            };
        }
    }
}