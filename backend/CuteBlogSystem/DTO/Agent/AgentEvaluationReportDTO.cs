namespace CuteBlogSystem.DTO.Agent
{
    public class AgentEvaluationReportDTO
    {
        // 测试批次ID
        public long RunId { get; set; }

        // 报告文件名
        public string FileName { get; set; } = string.Empty;

        // 报告内容（Markdown 格式）
        public string Markdown { get; set; } = string.Empty;
    }
}
