namespace CuteBlogSystem.DTO.AgentAction
{
    public class AnswerQuestionFromContentInput
    {
        // 直接传入的正文内容
        public string? Content { get; set; }

        // 从前面某一步结果中提取正文内容
        public int? ContentFromStep { get; set; }

        // 用户针对文章提出的问题
        public string Question { get; set; } = string.Empty;
    }

    public class AnswerQuestionFromContentOutput : IUserReadableOutput
    {
        // 用户问题
        public string Question { get; set; } = string.Empty;

        // 基于文章内容生成的回答
        public string Answer { get; set; } = string.Empty;

        // 被问答的正文长度
        public int ContentLength { get; set; }

        // 回答长度
        public int AnswerLength { get; set; }

        public string ToUserReadableText()
        {
            return Answer;
        }
    }
}