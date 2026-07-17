namespace CuteBlogSystem.DTO.AgentAction
{
    public interface IUserReadableOutput
    {
        // 返回可供用户直接阅读的 string 文本
        string ToUserReadableText();
    }

    public interface IAgentContentOutput
    {
        string GetContentText();
    }

    public interface IAgentArticleReferenceOutput
    {
        int? GetPrimaryArticleId();
    }
}
