using System;
using CuteBlogSystem.Enum;

namespace CuteBlogSystem.Entity
{
    public class User
    {
        public User()
        {
            var random = new Random();
            int index = random.Next(1, 3);
            AvatarUrl = $"/Picture/DefaultAvatar/DefaultAvatar_{index}.png";
        }

        public User(int id, string userName, string email, string passwordHash, string nickName, 
            string? bio, UserRole role, DateTime createdAt,
            string avatarUrl)
        {
            Id = id;
            UserName = userName;
            Email = email;
            PasswordHash = passwordHash;
            NickName = nickName;
            AvatarUrl = avatarUrl;
            Bio = bio;
            Role = role;
            CreatedAt = createdAt;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? NickName { get; set; }
        public string AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }

        // 导航属性
        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<ArticleLike> ArticleLikes { get; set; } = new List<ArticleLike>();

    }
}
