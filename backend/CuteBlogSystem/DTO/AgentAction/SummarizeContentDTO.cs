using CuteBlogSystem.AI.Planner;
using CuteBlogSystem.Util;

namespace CuteBlogSystem.DTO.AgentAction
{
    public class SummarizeContentInput
    {
        // 直接传入的正文内容
        public string? Content { get; set; }

        // 从前面某一步结果中提取正文内容
        public int? ContentFromStep { get; set; }
    }

    public class SummarizeContentOutput : IUserReadableOutput
    {
        // 文章摘要
        public string Summary { get; set; } = string.Empty;

        // 原始文章内容长度
        public int OriginalContentLength { get; set; }

        // 文章总结后的摘要长度
        public int SummaryLength { get; set; }

        // 返回可供用户阅读的 String 文本
        public string ToUserReadableText()
        {
            return Summary;
        }
    }
}
