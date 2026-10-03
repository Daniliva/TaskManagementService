using TaskManagementService.Models;

namespace TaskManagementService.Domain;

/// <summary>The one place that knows how a task may move through its life: Backlog → InWork → Testing → Done.</summary>
public static class TaskStatusRules
{
    /// <summary>
    /// True only for the three forward steps Backlog→InWork, InWork→Testing, Testing→Done.
    /// Everything else is false: staying in the same status, skipping a step, going back, leaving Done, or an undefined enum value.
    /// </summary>
    /// <param name="current">The current status.</param>
    /// <param name="next">The requested status.</param>
    /// <returns>Whether the move is allowed.</returns>
    public static bool CanTransition(Models.TaskStatus current, Models.TaskStatus next) =>
        (current, next) switch
        {
            (Models.TaskStatus.Backlog, Models.TaskStatus.InWork) => true,
            (Models.TaskStatus.InWork, Models.TaskStatus.Testing) => true,
            (Models.TaskStatus.Testing, Models.TaskStatus.Done) => true,
            _ => false,
        };

    /// <summary>The statuses reachable from <paramref name="current"/>: one element, or empty for Done and for undefined values.</summary>
    /// <param name="current">The current status.</param>
    /// <returns>The allowed next statuses.</returns>
    public static IReadOnlyList<Models.TaskStatus> AllowedNext(Models.TaskStatus current) =>
        Enum.GetValues<Models.TaskStatus>().Where(next => CanTransition(current, next)).ToList();

    /// <summary>The message used when a move is refused, e.g. "Invalid status transition from Backlog to Done.".</summary>
    /// <param name="current">The current status.</param>
    /// <param name="next">The requested status.</param>
    /// <returns>The message.</returns>
    public static string DescribeRefusal(Models.TaskStatus current, Models.TaskStatus next) =>
        $"Invalid status transition from {current} to {next}.";
}
