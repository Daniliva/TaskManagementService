namespace TaskManagementService.Repositories;

public interface ITaskRepository
{
    /// <summary>Stores a new task: next free id (starting at 1), status Backlog, CreatedAt = UpdatedAt = now. Returns a copy.</summary>
    Task<Models.Task> CreateAsync(Models.Task task);

    /// <summary>All tasks ordered by id (copies).</summary>
    Task<IEnumerable<Models.Task>> GetAllAsync();

    /// <summary>One page of tasks, ordered by id, after the filters of the query. Page below 1 counts as 1, PageSize is clamped to 1..100.</summary>
    Task<Models.PagedResult<Models.Task>> QueryAsync(Models.TaskQuery query);

    /// <summary>A copy of the task, or null.</summary>
    Task<Models.Task?> GetByIdAsync(int id);

    /// <summary>Changes title and description; sets UpdatedAt. KeyNotFoundException when the task does not exist.</summary>
    Task<Models.Task> UpdateAsync(int id, string title, string description);

    /// <summary>Moves the task one step. KeyNotFoundException ("... not found."), InvalidOperationException ("Invalid status transition from A to B.").</summary>
    Task UpdateStatusAsync(int id, Models.TaskStatus newStatus);

    /// <summary>Removes the task and its history. False when it did not exist. Ids are never reused.</summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>The transitions of the task, oldest first. KeyNotFoundException when the task does not exist.</summary>
    Task<IReadOnlyList<Models.TaskHistoryEntry>> GetHistoryAsync(int id);
}
