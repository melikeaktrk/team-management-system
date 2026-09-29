using TeamTaskManager.DTO.Task;

namespace TeamTaskManager.DTO.Project;

// Proje rapor kartlarının toplamlar, ilerleme, üye dağılımı ve aktivite verileri.
public class ProjectReportResponse
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public decimal CompletionPercentage { get; set; }
    public IReadOnlyList<MemberTaskSummary> MemberTaskDistribution { get; set; } = Array.Empty<MemberTaskSummary>();
    public IReadOnlyList<TaskActivityResponse> RecentActivities { get; set; } = Array.Empty<TaskActivityResponse>();
}

// Rapor içindeki bir üyeye ait açık ve tamamlanmış görev toplamları.
public class MemberTaskSummary
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int OpenTasks { get; set; }
    public int CompletedTasks { get; set; }
}
