namespace CuteBlogSystem.DTO.AgentAction
{
    public class GenerateContentRevisionInput
    {
        // 直接传入的原文内容
        public string? OriginalContent { get; set; }

        // 从前面某一步结果中提取原文内容
        public int? ContentFromStep { get; set; }

        // 修改 / 润色 / 扩写 / 改写指令
        public string Instruction { get; set; } = string.Empty;
    }

    public class GenerateContentRevisionOutput : IUserReadableOutput, IAgentContentOutput
    {
        // 修改指令
        public string Instruction { get; set; } = string.Empty;

        // 原文长度
        public int OriginalContentLength { get; set; }

        // 修订后的完整正文
        public string RevisedContent { get; set; } = string.Empty;

        // 修订后正文长度
        public int RevisedContentLength { get; set; }

        public string ToUserReadableText()
        {
            return RevisedContent;
        }

        public string GetContentText()
        {
            return RevisedContent;
        }
    }
}