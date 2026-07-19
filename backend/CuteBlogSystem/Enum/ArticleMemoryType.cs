namespace CuteBlogSystem.Enum
{
    public enum ArticleMemoryType
    {
        Unknown = 0,

        // 明确选中某篇文章，允许更新 LastSelectedArticleId / Title
        ArticleSelected = 1,

        // 对某篇文章发生了修改
        ArticleUpdated = 2,

        // 查询/展示/列表结果中提到过某篇文章，但不代表用户选中了它
        ArticleMentioned = 3,

        // 用户针对某篇文章内容进行了一次问答
        ArticleAnswered = 4,

        // 用户总结了某篇文章或一段内容
        ArticleSummarized = 5
    }
}
