namespace CuteBlogSystem.DTO.Agent
{
    public class AgentParameterPermissionResult
    {
        public bool IsValid { get; set; }

        public List<string> Errors { get; set; } = new();

        public static AgentParameterPermissionResult Success()
        {
            return new AgentParameterPermissionResult { IsValid = true };
        }

        public static AgentParameterPermissionResult Fail(List<string> errors)
        {
            return new AgentParameterPermissionResult
            {
                IsValid = false,
                Errors = errors
            };
        }
    }

    public class AgentParameterRiskResult
    {
        public bool IsSafe { get; set; }

        public List<string> Errors { get; set; } = new();

        public static AgentParameterRiskResult Safe()
        {
            return new AgentParameterRiskResult { IsSafe = true };
        }

        public static AgentParameterRiskResult Unsafe(List<string> errors)
        {
            return new AgentParameterRiskResult
            {
                IsSafe = false,
                Errors = errors
            };
        }
    }
}
