namespace TeamTaskManager.Entities;

public enum ProjectStatus
{
    Planning = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum TaskStatus
{
    New = 0,
    InProgress = 1,
    Waiting = 2,
    Completed = 3,
    Cancelled = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum MemberRole
{
    ProjectManager = 0,
    TeamMember = 1
}

public enum NotificationType
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Task = 4,
    Project = 5
}
