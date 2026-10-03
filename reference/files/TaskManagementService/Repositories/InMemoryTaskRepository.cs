using System.Collections.Concurrent;
using TaskManagementService.Domain;

namespace TaskManagementService.Repositories;

/// <summary>
/// Thread-safe in-memory store. Ids come from an atomic counter; every task has its own lock for read-modify-write;
/// callers only ever get copies, so changing a returned object never changes the store.
/// </summary>
public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<int, Entry> tasks = new();
    private readonly TimeProvider time;
    private int lastId;

    public InMemoryTaskRepository(TimeProvider? time = null)
    {
        this.time = time ?? TimeProvider.System;
    }

    public Task<Models.Task> CreateAsync(Models.Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var now = this.time.GetUtcNow();
        var entry = new Entry(new Models.Task
        {
            Id = Interlocked.Increment(ref this.lastId),
            Title = task.Title,
            Description = task.Description,
            Status = Models.TaskStatus.Backlog,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _ = this.tasks.TryAdd(entry.Task.Id, entry);
        return System.Threading.Tasks.Task.FromResult(Copy(entry.Task));
    }

    public System.Threading.Tasks.Task<IEnumerable<Models.Task>> GetAllAsync() =>
        System.Threading.Tasks.Task.FromResult<IEnumerable<Models.Task>>(this.Snapshot().ToList());

    public System.Threading.Tasks.Task<Models.PagedResult<Models.Task>> QueryAsync(Models.TaskQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var items = this.Snapshot();
        if (query.Status is { } status)
        {
            items = items.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var text = query.Search.Trim();
            items = items.Where(t => t.Title.Contains(text, StringComparison.OrdinalIgnoreCase) || t.Description.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        var all = items.ToList();
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, Models.TaskQuery.MaxPageSize);
        var pageItems = all.Skip((page - 1) * size).Take(size).ToList();
        return System.Threading.Tasks.Task.FromResult(new Models.PagedResult<Models.Task>(pageItems, page, size, all.Count));
    }

    public System.Threading.Tasks.Task<Models.Task?> GetByIdAsync(int id)
    {
        if (!this.tasks.TryGetValue(id, out var entry))
        {
            return System.Threading.Tasks.Task.FromResult<Models.Task?>(null);
        }

        lock (entry.Gate)
        {
            return System.Threading.Tasks.Task.FromResult<Models.Task?>(Copy(entry.Task));
        }
    }

    public System.Threading.Tasks.Task<Models.Task> UpdateAsync(int id, string title, string description)
    {
        var entry = this.Find(id);
        lock (entry.Gate)
        {
            entry.Task.Title = title;
            entry.Task.Description = description;
            entry.Task.UpdatedAt = this.time.GetUtcNow();
            return System.Threading.Tasks.Task.FromResult(Copy(entry.Task));
        }
    }

    public System.Threading.Tasks.Task UpdateStatusAsync(int id, Models.TaskStatus newStatus)
    {
        var entry = this.Find(id);
        lock (entry.Gate)
        {
            var current = entry.Task.Status;
            if (!TaskStatusRules.CanTransition(current, newStatus))
            {
                throw new InvalidOperationException(TaskStatusRules.DescribeRefusal(current, newStatus));
            }

            var now = this.time.GetUtcNow();
            entry.Task.Status = newStatus;
            entry.Task.UpdatedAt = now;
            entry.History.Add(new Models.TaskHistoryEntry(current, newStatus, now));
        }

        return System.Threading.Tasks.Task.CompletedTask;
    }

    public System.Threading.Tasks.Task<bool> DeleteAsync(int id) => System.Threading.Tasks.Task.FromResult(this.tasks.TryRemove(id, out _));

    public System.Threading.Tasks.Task<IReadOnlyList<Models.TaskHistoryEntry>> GetHistoryAsync(int id)
    {
        var entry = this.Find(id);
        lock (entry.Gate)
        {
            return System.Threading.Tasks.Task.FromResult<IReadOnlyList<Models.TaskHistoryEntry>>(entry.History.ToList());
        }
    }

    private static Models.Task Copy(Models.Task t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
    };

    private Entry Find(int id) =>
        this.tasks.TryGetValue(id, out var entry) ? entry : throw new KeyNotFoundException($"Task with ID {id} not found.");

    private IEnumerable<Models.Task> Snapshot() =>
        this.tasks.Values.OrderBy(e => e.Task.Id).Select(e =>
        {
            lock (e.Gate)
            {
                return Copy(e.Task);
            }
        });

    private sealed class Entry
    {
        public Entry(Models.Task task) => this.Task = task;

        public Models.Task Task { get; }

        public List<Models.TaskHistoryEntry> History { get; } = new();

        public object Gate { get; } = new();
    }
}
