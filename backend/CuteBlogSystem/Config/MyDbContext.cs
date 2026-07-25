using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using Microsoft.EntityFrameworkCore;

namespace CuteBlogSystem.Config
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ArticleLike> ArticleLikes { get; set; }
        public DbSet<AgentWorkflowLog> AgentWorkflowLogs { get; set; }
        public DbSet<AgentConversationMemory> AgentConversationMemories { get; set; }
        public DbSet<AgentConversation> AgentConversations { get; set; }
        public DbSet<AgentMessage> AgentMessages { get; set; }
        public DbSet<AgentPendingConfirmation> AgentPendingConfirmations { get; set; }
        public DbSet<AgentTestCase> AgentTestCases { get; set; }
        public DbSet<AgentEvaluationResult> AgentEvaluationResults { get; set; }
        public DbSet<AgentEvaluationRun> AgentEvaluationRuns { get; set; }
        public DbSet<AgentEvaluationReportSnapshot> AgentEvaluationReportSnapshots { get; set; }
        public DbSet<UserLongTermMemory> UserLongTermMemories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置 ArticleTag 的联合主键
            modelBuilder.Entity<ArticleTag>()
                .HasKey(at => new { at.ArticleId, at.TagId });

            // 配置 ArticleLike 的联合主键
            modelBuilder.Entity<ArticleLike>()
                .HasKey(at => new { at.ArticleId, at.UserId });

            // ========== 配置 Comment 的外键，避免多重级联 ==========
            // Comment -> User
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);   // 或 NoAction

            // Comment -> Article
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Article)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.ArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment 自引用（ParentComment 关系）
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.InverseParentComment)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);   // 自引用通常禁止级联

            // 可选：配置其他可能存在级联关系的外键（如 Article 中的 UserId、CategoryId）
            // 虽然本例错误未涉及它们，但为避免后续类似问题，建议也进行统一配置
            modelBuilder.Entity<Article>()
                .HasOne(a => a.User)
                .WithMany(u => u.Articles)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Article>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Articles)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tag 与 ArticleTag 的关系（ArticleTag 是中间表，默认级联删除行为可接受）
            // 但如果你希望更精细控制，也可以添加：
            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Article)
                .WithMany(a => a.ArticleTags)
                .HasForeignKey(at => at.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);   // 删除文章时自动删除关联标签关系

            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(at => at.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== 配置 User 的唯一索引 ==========
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.UserName)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(u => u.Email)
                    .HasMaxLength(100)
                    .IsRequired()
                    .UseCollation("Chinese_PRC_CI_AS");

                entity.HasIndex(u => u.UserName)
                    .IsUnique();

                entity.HasIndex(u => u.Email)
                    .IsUnique();
            });

            // ========== 配置 ArticleLike 的外键，避免多重级联 ==========
            modelBuilder.Entity<ArticleLike>()
                .HasOne(al => al.User)
                .WithMany(u => u.ArticleLikes)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ArticleLike -> Article 的关系通常也需要配置为 Cascade，因为当文章被删除时，相关的点赞记录应该被自动删除，以保持数据一致性。
            modelBuilder.Entity<ArticleLike>()
                .HasOne(al => al.Article)
                .WithMany(a => a.ArticleLikes)
                .HasForeignKey(al => al.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            // 配置 Tag -> Category 的外键关系
            modelBuilder.Entity<Tag>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Tags)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);   // 删除分类时不删除标签，避免数据丢失

            // 配置 AgentConversationMemory 的 ConversationId 唯一索引，确保每个会话只有一条记忆记录
            modelBuilder.Entity<AgentConversationMemory>(entity =>
            {
                entity.Property(m => m.SessionId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(m => m.SessionId)
                    .IsUnique();

                entity.HasOne(m => m.Conversation)
                    .WithOne(c => c.Memory)
                    .HasForeignKey<AgentConversationMemory>(m => m.SessionId)
                    .OnDelete(DeleteBehavior.Cascade); // 配置级联删除，当会话被删除时，相关的记忆记录也被删除

                entity.Property(m => m.ConversationSummary).HasMaxLength(4000);

                entity.Property(m => m.RecentMentionedArticlesJson).HasMaxLength(4000);
            });

            // 配置 AgentConversation 的 SessionId 作为主键，并添加必要的属性配置
            modelBuilder.Entity<AgentConversation>(entity =>
            {
                entity.HasKey(c => c.SessionId);

                entity.Property(c => c.SessionId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(c => c.Title)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(c => c.ModelUsed)
                    .HasMaxLength(100);

                entity.Property(c => c.Status)
                    .HasConversion<string>()    // 将枚举转换为字符串存储
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasIndex(c => new { c.UserId, c.UpdatedAt });
            });

            // 配置 AgentMessage 的 MessageId 作为主键，并添加必要的属性配置
            modelBuilder.Entity<AgentMessage>(entity =>
            {
                entity.HasKey(m => m.MessageId);

                entity.Property(m => m.SessionId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(m => m.Role)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(m => m.Content)
                    .IsRequired();

                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(m => new { m.SessionId, m.CreatedAt });
            });

            // 配置 AgentPendingConfirmation 的 ID 作为主键，并添加索引，以及配置其他必要属性
            modelBuilder.Entity<AgentPendingConfirmation>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.ConfirmationId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.SessionId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.UserId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.UserMessage)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(x => x.PlanJson)
                    .IsRequired();

                entity.HasIndex(x => x.ConfirmationId)
                    .IsUnique();

                entity.HasIndex(x => new { x.SessionId, x.Status });
            });

            // 配置 AgentTestCase 测试用例表
            modelBuilder.Entity<AgentTestCase>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CaseName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.UserMessage)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(x => x.SessionId)
                    .HasMaxLength(100);

                entity.Property(x => x.ExpectedActionsJson)
                    .IsRequired();

                entity.Property(x => x.ExpectedAnswerSummary)
                    .HasMaxLength(4000);

                entity.Property(x => x.Category)
                    .HasMaxLength(100);

                entity.Property(x => x.Remark)
                    .HasMaxLength(1000);

                entity.Property(x => x.IsDeleted)
                    .IsRequired();

                entity.Property(x => x.ExpectedAnswerContainsJson)
                    .IsRequired();

                entity.HasIndex(x => x.IsEnabled);

                entity.HasIndex(x => x.Category);

                entity.HasIndex(x => x.IsDeleted);
            });

            // 配置 AgentEvaluationRun 评估运行批次表
            modelBuilder.Entity<AgentEvaluationRun>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.ModelUsed)
                    .HasMaxLength(100);

                entity.Property(x => x.Remark)
                    .HasMaxLength(1000);

                entity.HasIndex(x => x.StartedAt);

                entity.Property(x => x.PlannerPromptVersion)
                    .HasMaxLength(100);

                entity.Property(x => x.ActionRegistryVersion)
                    .HasMaxLength(100);

                entity.Property(x => x.EvaluationVersion)
                    .HasMaxLength(100);

                entity.Property(x => x.FinalAnswerPromptVersion)
                    .HasMaxLength(100);

                entity.HasOne(x => x.SourceRun)
                    .WithMany()
                    .HasForeignKey(x => x.SourceId)
                    .OnDelete(DeleteBehavior.Restrict); // 删除源批次时不删除当前批次

                entity.HasIndex(x => x.SourceId);
            });

            // 配置 AgentEvaluationResult 单条评估结果表
            modelBuilder.Entity<AgentEvaluationResult>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CaseName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ErrorsJson)
                    .IsRequired();

                entity.Property(x => x.Answer)
                    .IsRequired();

                entity.Property(x => x.ActualActionsJson)
                    .IsRequired();

                entity.Property(x => x.SemanticJudgeReason)
                    .HasMaxLength(4000);

                entity.Property(x => x.FailureType)
                    .HasConversion<int>()
                    .IsRequired();

                entity.HasOne(x => x.Run)
                    .WithMany()
                    .HasForeignKey(x => x.RunId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.TestCase)
                    .WithMany()
                    .HasForeignKey(x => x.TestCaseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.RunId);

                entity.HasIndex(x => x.TestCaseId);

                entity.HasIndex(x => x.CreatedAt);

                entity.HasIndex(x => x.Passed);

                entity.HasIndex(x => x.FailureType);

                entity.HasIndex(x => x.WorkflowLogId);

                entity.Property(x => x.TestCaseSnapshotJson)
                    .HasColumnType("nvarchar(max)")
                    .HasDefaultValue("{}")
                    .IsRequired();
            });

            // 配置 AgentEvaluationReportSnapshot 评估报告快照表
            modelBuilder.Entity<AgentEvaluationReportSnapshot>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.FileName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.MarkdownContent)
                    .IsRequired();

                entity.Property(x => x.PlannerPromptVersion)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.ActionRegistryVersion)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.EvaluationVersion)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.FinalAnswerPromptVersion)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasOne(x => x.Run)
                    .WithMany()
                    .HasForeignKey(x => x.RunId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.RunId).IsUnique();
                entity.HasIndex(x => x.CreatedAt);
                entity.HasIndex(x => x.IsDeleted);
            });

            // ========== 配置 UserLongTermMemory（Agent跨会话长期记忆表） ==========
            modelBuilder.Entity<UserLongTermMemory>(entity =>
            {
                entity.ToTable("UserLongTermMemories", table =>
                {
                    table.HasCheckConstraint(
                        "CK_UserLongTermMemories_Confidence",
                        "[Confidence] >= 0 AND [Confidence] <= 1");

                    table.HasCheckConstraint(
                        "CK_UserLongTermMemories_Importance",
                        "[Importance] >= 0 AND [Importance] <= 1");

                    table.HasCheckConstraint(
                        "CK_UserLongTermMemories_AccessCount",
                        "[AccessCount] >= 0");

                    table.HasCheckConstraint(
                        "CK_UserLongTermMemories_RevisionNo",
                        "[RevisionNo] >= 1");

                    table.HasCheckConstraint(
                        "CK_UserLongTermMemories_MetadataJson",
                        "[MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1");
                });

                // 主键：保留默认聚集索引
                entity.HasKey(e => e.MemoryId);

                entity.Property(e => e.MemoryId)
                    .HasDefaultValueSql("NEWSEQUENTIALID()");

                // 用户外键
                entity.Property(e => e.UserId)
                    .IsRequired();

                // 枚举按字符串保存
                entity.Property(e => e.MemoryType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.SourceType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                // 记忆内容
                entity.Property(e => e.MemoryGroup)
                    .HasConversion<byte>()
                    .HasColumnType("tinyint")
                    .HasDefaultValue(MemoryGroupConstants.Unknown)
                    .HasSentinel(MemoryGroupConstants.Unknown);

                entity.Property(e => e.MemoryKey)
                    .HasMaxLength(255);

                entity.Property(e => e.Content)
                    .IsRequired();

                entity.Property(e => e.ContentHash)
                    .HasMaxLength(64)
                    .IsFixedLength()
                    .IsUnicode(false)
                    .IsRequired();

                entity.Property(e => e.MetadataJson);

                // 来源追踪
                entity.Property(e => e.SourceSessionId)
                    .HasMaxLength(100);

                entity.Property(e => e.SourceAction)
                    .HasMaxLength(256);

                // 评分
                entity.Property(e => e.Confidence)
                    .HasPrecision(5, 4)
                    .HasDefaultValue(0.5000m);

                entity.Property(e => e.Importance)
                    .HasPrecision(5, 4)
                    .HasDefaultValue(0.5000m);

                entity.Property(e => e.IsPinned)
                    .HasDefaultValue(false);

                // 访问统计
                entity.Property(e => e.AccessCount)
                    .HasDefaultValue(0);

                // 状态按 TINYINT 保存
                entity.Property(e => e.Status)
                    .HasConversion<byte>()
                    .HasColumnType("tinyint")
                    .HasDefaultValue(MemoryStatus.Active)
                    .HasSentinel(MemoryStatus.Unknown);

                // 版本号
                entity.Property(e => e.RevisionNo)
                    .HasDefaultValue(1);

                // 乐观并发
                entity.Property(e => e.RowVersion)
                    .IsRowVersion()
                    .IsRequired();

                // 自关联版本链
                entity.HasOne(e => e.SupersedesMemory)
                    .WithMany()
                    .HasForeignKey(e => e.SupersedesMemoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 用户外键
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 同一用户下，当前有效 MemoryKey 唯一
                entity.HasIndex(e => new { e.UserId, e.MemoryKey, e.MemoryGroup, e.MemoryType })
                    .IsUnique()
                    .HasFilter($"[MemoryKey] IS NOT NULL AND [Status] = {(byte)MemoryStatus.Active}");

                // 用户有效记忆召回和重要性排序
                entity.HasIndex(e => new { e.UserId, e.Status, e.Importance })
                    .IsDescending(false, false, true);

                // 用户历史记忆查询
                entity.HasIndex(e => new
                {
                    e.UserId,
                    e.CreatedAt
                })
                    .IsDescending(false, true);

                // 全局衰减任务
                entity.HasIndex(e => new
                {
                    e.Status,
                    e.IsPinned,
                    e.LastDecayAt
                });

                // 用户级内容去重
                entity.HasIndex(e => new
                {
                    e.UserId,
                    e.ContentHash
                });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}