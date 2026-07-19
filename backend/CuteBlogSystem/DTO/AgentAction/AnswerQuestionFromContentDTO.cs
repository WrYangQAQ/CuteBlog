using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;

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

    public class AnswerQuestionFromContentOutput : IUserReadableOutput, IAgentMemoryFactProvider
    {
        // 用户提出的问题
        public string Question { get; set; } = string.Empty;

        // 根据文章内容生成的回答
        public string Answer { get; set; } = string.Empty;

        // 被引用的文章内容长度（字符数）
        public int ContentLength { get; set; }

        // 回答的长度（字符数）
        public int AnswerLength { get; set; }

        // 被提问的文章 ID（若有）
        public int? SourceArticleId { get; set; }

        // 被提问的文章标题（若有）
        public string? SourceArticleTitle { get; set; }

        // 被提问的文章所属分类名称（若有）
        public string? SourceCategoryName { get; set; }

        // 返回可供用户阅读的文本（即回答本身）
        public string ToUserReadableText()
        {
            return Answer;
        }

        // 根据来源动作生成记忆事实，记录本次问答操作
        public IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction)
        {
            var facts = new List<AgentMemoryFact>
            {
                new AgentMemoryFact
                {
                    Type = ArticleMemoryType.ArticleAnswered,
                    ArticleId = SourceArticleId,
                    ArticleTitle = SourceArticleTitle,
                    CategoryName = SourceCategoryName,
                    SourceAction = sourceAction,
                    Summary = SourceArticleId.HasValue
                        ? $"用户围绕文章《{SourceArticleTitle}》提问：{Question}"
                        : $"用户围绕直接提供的内容提问：{Question}"
                }
            };

            return facts;
        }
    }
}