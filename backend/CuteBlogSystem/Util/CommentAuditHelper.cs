using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CuteBlogSystem.Util
{
    public static class CommentAuditHelper
    {
        /// <summary>
        /// 审核评论是否通过
        /// true = 通过
        /// false = 不通过
        /// </summary>
        public static bool IsCommentApproved(string comment)
        {
            // 1. 空值校验
            if (string.IsNullOrWhiteSpace(comment))
                return false;

            // 2. 去掉首尾空格，统一处理
            string content = comment.Trim();

            // 3. 长度限制（贴近实际应用）
            // 太短可能是无意义评论，太长可能是灌水或恶意内容
            if (content.Length < 2 || content.Length > 500)
                return false;

            // 4. 规范化文本
            // 转小写，方便英文关键词匹配
            string normalized = content.ToLowerInvariant();

            // 5. 过滤纯符号或几乎无有效内容
            int letterOrDigitCount = normalized.Count(char.IsLetterOrDigit);
            if (letterOrDigitCount < 2)
                return false;

            // 6. 过滤重复字符刷屏，例如：666666666、哈哈哈哈哈哈哈哈、aaaaaaa
            if (HasTooManyRepeatedCharacters(normalized))
                return false;

            // 7. 过滤过多特殊符号，避免乱码/刷屏/恶意内容
            if (HasTooManySpecialCharacters(normalized))
                return false;

            // 8. 敏感词 / 辱骂词（这里只是示例，实际项目可接数据库或配置文件）
            string[] bannedWords =
            {
            "傻逼", "脑残", "废物", "滚", "去死", "妈的", "操", "垃圾",
            "sb", "fuck", "shit", "bitch"
        };

            if (ContainsBannedWords(normalized, bannedWords))
                return false;

            // 9. 广告 / 引流关键词
            string[] adWords =
            {
            "加微信", "加v", "vx", "vx:", "微信", "qq", "企鹅",
            "私聊我", "联系我", "扫码", "代理", "兼职", "推广",
            "优惠券", "返利", "代刷", "刷单", "赚钱", "引流"
        };

            if (ContainsBannedWords(normalized, adWords))
                return false;

            // 10. 过滤手机号
            // 中国大陆手机号简单规则：1开头，11位
            if (Regex.IsMatch(normalized, @"\b1[3-9]\d{9}\b"))
                return false;

            // 11. 过滤QQ号（简单规则：5-12位数字）
            // 为避免误伤普通数字，需要搭配上下文判断
            if (Regex.IsMatch(normalized, @"(qq|q q|企鹅)[：:\s\-]?\d{5,12}"))
                return false;

            // 12. 过滤微信号（简单规则）
            if (Regex.IsMatch(normalized, @"(微信|vx|vx:|wx)[：:\s\-]?[a-zA-Z0-9_\-]{5,20}"))
                return false;

            // 13. 过滤URL链接
            if (Regex.IsMatch(normalized, @"(http://|https://|www\.)"))
                return false;

            // 14. 过滤邮箱
            if (Regex.IsMatch(normalized, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"))
                return false;

            // 15. 过滤“全是重复词”的灌水，例如“好好好好好好”“赞赞赞赞赞”
            if (HasRepeatedPattern(normalized))
                return false;

            // 16. 可以加入简单的正常评论判断
            // 例如至少包含一个中文、英文字母或数字
            if (!Regex.IsMatch(normalized, @"[\u4e00-\u9fa5a-z0-9]"))
                return false;

            return true;
        }

        /// <summary>
        /// 是否包含敏感词
        /// </summary>
        private static bool ContainsBannedWords(string content, string[] bannedWords)
        {
            foreach (string word in bannedWords)
            {
                if (content.Contains(word))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 是否存在连续重复字符过多
        /// 例如：aaaaaa、111111、哈哈哈哈哈
        /// </summary>
        private static bool HasTooManyRepeatedCharacters(string content)
        {
            return Regex.IsMatch(content, @"(.)\1{5,}");
        }

        /// <summary>
        /// 特殊符号占比过高则拦截
        /// </summary>
        private static bool HasTooManySpecialCharacters(string content)
        {
            int specialCount = content.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && !IsChinese(c));
            double ratio = (double)specialCount / content.Length;

            return ratio > 0.4;
        }

        /// <summary>
        /// 是否有明显重复模式
        /// 例如：哈哈哈哈、666666、赞赞赞赞
        /// </summary>
        private static bool HasRepeatedPattern(string content)
        {
            // 一个字符或两个字符的模式连续重复 4 次以上
            return Regex.IsMatch(content, @"^(.{1,2})\1{3,}$");
        }

        /// <summary>
        /// 判断是否为中文字符
        /// </summary>
        private static bool IsChinese(char c)
        {
            return c >= 0x4e00 && c <= 0x9fa5;
        }
    }
}