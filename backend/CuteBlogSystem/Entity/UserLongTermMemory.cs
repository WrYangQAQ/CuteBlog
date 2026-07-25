using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Enum;
using CuteBlogSystem.Helper;
using System;
using System.Runtime.CompilerServices;

namespace CuteBlogSystem.Entity
{
    // Agent跨会话长期记忆表
    public class UserLongTermMemory
    {
        // ========== 第一组：身份标识与用户隔离 ==========
        public Guid MemoryId { get; set; }
        public int UserId { get; set; }

        // ========== 第二组：记忆内容与逻辑分类 ==========
        public MemoryTypeConstants MemoryType { get; set; } = MemoryTypeConstants.Unknown;
        public MemoryGroupConstants MemoryGroup { get; set; } = MemoryGroupConstants.Unknown;
        public string? MemoryKey { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }

        // ========== 第三组：来源与上下文溯源 ==========
        public SourceTypeConstants SourceType { get; set; } = SourceTypeConstants.Unknown;
        public string? SourceSessionId { get; set; }
        public long? SourceMessageId { get; set; }
        public string? SourceAction { get; set; }

        // ========== 第四组：可信度、重要性与固定状态 ==========
        public decimal Confidence { get; set; } = 0.5m;
        public decimal Importance { get; set; } = 0.5m;
        public bool IsPinned { get; set; }

        // ========== 第五组：访问统计与有效性反馈 ==========
        public int AccessCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? LastConfirmedAt { get; set; }
        public DateTime? LastDecayAt { get; set; }

        // ========== 第六组：生命周期与状态管理 ==========
        public MemoryStatus Status { get; set; } = MemoryStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // ========== 第七组：版本替换与并发控制 ==========
        public int RevisionNo { get; set; } = 1;
        public Guid? SupersedesMemoryId { get; set; }
        public byte[]? RowVersion { get; set; }

        // ========== 导航属性 ==========
        public virtual UserLongTermMemory? SupersedesMemory { get; set; }

        public virtual User? User { get; set; }

        // ========== 构造函数 ==========
        public UserLongTermMemory() { }

        public UserLongTermMemory(int userId, UpdateLongTermMemoryDto dto, string contentHash)
        {
            var now = DateTime.UtcNow;
            UserId = userId;
            MemoryType = dto.MemoryType;
            MemoryGroup = dto.MemoryGroup;
            MemoryKey = dto.MemoryKey;
            Content = dto.Content;
            ContentHash = contentHash;
            MetadataJson = dto.MetadataJson;
            SourceType = dto.SourceType;
            SourceSessionId = dto.SourceSessionId;
            SourceMessageId = dto.SourceMessageId;
            SourceAction = dto.SourceAction;
            Confidence = dto.Confidence;
            Importance = dto.Importance;
            IsPinned = dto.IsPinned;
            Status = MemoryStatus.Active;
            CreatedAt = now;
            UpdatedAt = now;
            LastConfirmedAt = now;
            ExpiresAt = dto.ExpiresAt;
        }
    }
}