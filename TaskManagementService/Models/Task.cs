namespace TaskManagementService.Models;

public class Task
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TaskStatus Status { get; set; } = TaskStatus.Backlog;

    /// <summary>When the task was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the task was last changed: edited or moved to another status (UTC). Equals CreatedAt until then.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>One step of a task's life.</summary>
/// <param name="From">The status before.</param>
/// <param name="To">The status after.</param>
/// <param name="At">When it happened (UTC).</param>
public sealed record TaskHistoryEntry(TaskStatus From, TaskStatus To, DateTimeOffset At);

/// <summary>Filter, search and paging for the task list.</summary>
public sealed class TaskQuery
{
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    public TaskStatus? Status { get; set; }

    /// <summary>Case-insensitive text that must appear in the title or the description.</summary>
    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => this.PageSize <= 0 ? 0 : (int)Math.Ceiling(this.TotalCount / (double)this.PageSize);
}
