using Microsoft.EntityFrameworkCore;
using CuteBlogSystem.Entity;

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

            base.OnModelCreating(modelBuilder);
        }
    }
}