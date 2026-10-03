namespace TaskManagementService.Repositories;

/// <summary>
/// TODO (spec): this is still the ORIGINAL implementation (plain List and "_nextId++", not thread-safe, hands out the stored objects).
/// Target behaviour — see InMemoryTaskRepository in the reference branch and TaskRepositoryExtendedTests:
/// ConcurrentDictionary + Interlocked ids + a lock per task, copies in and out, CreatedAt/UpdatedAt from the TimeProvider,
/// history of transitions, QueryAsync (filter, search, paging), UpdateAsync, DeleteAsync, GetHistoryAsync,
/// transition rules from TaskStatusRules (the private IsValidTransition below goes away).
/// </summary>
public class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<Models.Task> _tasks = new();
    private int _nextId = 1;

    public InMemoryTaskRepository(TimeProvider? time = null)
    {
    }

    public async Task<Models.Task> CreateAsync(Models.Task task)
    {
        task.Id = _nextId++;
        _tasks.Add(task);
        return await System.Threading.Tasks.Task.FromResult(task);
    }

    public async Task<IEnumerable<Models.Task>> GetAllAsync()
    {
        return await System.Threading.Tasks.Task.FromResult(_tasks.AsEnumerable());
    }

    public async Task<Models.Task?> GetByIdAsync(int id)
    {
        return await System.Threading.Tasks.Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));
    }

    public async Task UpdateStatusAsync(int id, Models.TaskStatus newStatus)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found.");
        }

        if (!IsValidTransition(task.Status, newStatus))
        {
            throw new InvalidOperationException($"Invalid status transition from {task.Status} to {newStatus}.");
        }

        task.Status = newStatus;
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public Task<Models.PagedResult<Models.Task>> QueryAsync(Models.TaskQuery query) => throw new NotImplementedException("TODO");

    public Task<Models.Task> UpdateAsync(int id, string title, string description) => throw new NotImplementedException("TODO");

    public Task<bool> DeleteAsync(int id) => throw new NotImplementedException("TODO");

    public Task<IReadOnlyList<Models.TaskHistoryEntry>> GetHistoryAsync(int id) => throw new NotImplementedException("TODO");

    private bool IsValidTransition(Models.TaskStatus current, Models.TaskStatus next)
    {
        return (current, next) switch
        {
            (Models.TaskStatus.Backlog, Models.TaskStatus.InWork) => true,
            (Models.TaskStatus.InWork, Models.TaskStatus.Testing) => true,
            (Models.TaskStatus.Testing, Models.TaskStatus.Done) => true,
            _ => false
        };
    }
}
