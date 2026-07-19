using CuteBlogSystem.DTO.Agent;

namespace CuteBlogSystem.DTO.AgentAction
{
    // 提供向用户展示的可读文本输出
    public interface IUserReadableOutput
    {
        // 生成对用户友好的可读文本摘要
        string ToUserReadableText();
    }

    // 提供内容文本输出（用于AI处理或后续步骤）
    public interface IAgentContentOutput
    {
        // 获取内容的纯文本形式
        string GetContentText();
    }

    // 提供主要涉及的文章ID引用（用于跨步骤记忆和引用）
    public interface IAgentArticleReferenceOutput
    {
        // 获取主要涉及的文章ID，若无则返回null
        int? GetPrimaryArticleId();
    }

    // 提供执行动作后产生的记忆事实
    public interface IAgentMemoryFactProvider
    {
        // 根据来源动作名称生成记忆事实列表
        IEnumerable<AgentMemoryFact> GetMemoryFacts(string sourceAction);
    }

    // 提供文章列表输出（用于从上一步结果中选择文章）
    public interface IArticleListOutput
    {
        // 执行结果携带的文章列表
        List<ArticleSearchResultItem> Articles { get; }
    }
}
