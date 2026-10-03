using Microsoft.Extensions.Time.Testing;
using TaskManagementService.Models;
using TaskManagementService.Repositories;
using Task = System.Threading.Tasks.Task;
using TaskStatus = TaskManagementService.Models.TaskStatus;

namespace TaskManagementService.Tests;

public class TaskRepositoryExtendedTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly InMemoryTaskRepository repo;

    public TaskRepositoryExtendedTests() => this.repo = new InMemoryTaskRepository(this.time);

    private async Task<Models.Task> Add(string title, string description = "") => await this.repo.CreateAsync(new Models.Task { Title = title, Description = description });

    private async Task Move(int id, params TaskStatus[] steps)
    {
        foreach (var s in steps)
        {
            await this.repo.UpdateStatusAsync(id, s);
        }
    }

    // ---- timestamps, copies, ids -------------------------------------------------------------------------------

    [Fact]
    public async Task Create_sets_both_timestamps_to_now()
    {
        var task = await this.Add("t");

        Assert.Equal(this.time.GetUtcNow(), task.CreatedAt);
        Assert.Equal(task.CreatedAt, task.UpdatedAt);
    }

    [Fact]
    public async Task Create_ignores_a_status_and_id_chosen_by_the_caller()
    {
        var created = await this.repo.CreateAsync(new Models.Task { Id = 77, Title = "t", Status = TaskStatus.Done });

        Assert.Equal(1, created.Id);
        Assert.Equal(TaskStatus.Backlog, created.Status);
    }

    [Fact]
    public async Task Changing_a_returned_object_does_not_change_the_store()
    {
        var created = await this.Add("original");
        created.Title = "hacked";
        created.Status = TaskStatus.Done;

        var stored = await this.repo.GetByIdAsync(created.Id);

        Assert.Equal("original", stored!.Title);
        Assert.Equal(TaskStatus.Backlog, stored.Status);
    }

    [Fact]
    public async Task Changing_the_input_object_after_create_does_not_change_the_store()
    {
        var input = new Models.Task { Title = "kept" };
        var created = await this.repo.CreateAsync(input);
        input.Title = "changed";

        Assert.Equal("kept", (await this.repo.GetByIdAsync(created.Id))!.Title);
    }

    [Fact]
    public async Task Ids_are_not_reused_after_a_delete()
    {
        var first = await this.Add("a");
        await this.repo.DeleteAsync(first.Id);

        var second = await this.Add("b");

        Assert.Equal(2, second.Id);
    }

    [Fact]
    public async Task Create_rejects_null() => await Assert.ThrowsAsync<ArgumentNullException>(() => this.repo.CreateAsync(null!));

    // ---- update / delete ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Update_changes_text_and_UpdatedAt_but_not_status_or_CreatedAt()
    {
        var task = await this.Add("old", "old desc");
        await this.Move(task.Id, TaskStatus.InWork);
        this.time.Advance(TimeSpan.FromHours(1));

        var updated = await this.repo.UpdateAsync(task.Id, "new", "new desc");

        Assert.Equal("new", updated.Title);
        Assert.Equal("new desc", updated.Description);
        Assert.Equal(TaskStatus.InWork, updated.Status);
        Assert.Equal(task.CreatedAt, updated.CreatedAt);
        Assert.Equal(this.time.GetUtcNow(), updated.UpdatedAt);
    }

    [Fact]
    public async Task Update_of_a_missing_task_throws() => await Assert.ThrowsAsync<KeyNotFoundException>(() => this.repo.UpdateAsync(5, "t", "d"));

    [Fact]
    public async Task Delete_removes_the_task_and_reports_whether_it_existed()
    {
        var task = await this.Add("t");

        Assert.True(await this.repo.DeleteAsync(task.Id));
        Assert.False(await this.repo.DeleteAsync(task.Id));
        Assert.Null(await this.repo.GetByIdAsync(task.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => this.repo.GetHistoryAsync(task.Id));
    }

    // ---- history -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task History_lists_transitions_oldest_first_with_their_times()
    {
        var task = await this.Add("t");
        await this.repo.UpdateStatusAsync(task.Id, TaskStatus.InWork);
        this.time.Advance(TimeSpan.FromMinutes(5));
        await this.repo.UpdateStatusAsync(task.Id, TaskStatus.Testing);

        var history = await this.repo.GetHistoryAsync(task.Id);

        Assert.Equal(2, history.Count);
        Assert.Equal(new TaskHistoryEntry(TaskStatus.Backlog, TaskStatus.InWork, new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)), history[0]);
        Assert.Equal(new TaskHistoryEntry(TaskStatus.InWork, TaskStatus.Testing, new DateTimeOffset(2026, 10, 1, 12, 5, 0, TimeSpan.Zero)), history[1]);
    }

    [Fact]
    public async Task A_refused_transition_leaves_no_history_and_no_change()
    {
        var task = await this.Add("t");
        this.time.Advance(TimeSpan.FromHours(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => this.repo.UpdateStatusAsync(task.Id, TaskStatus.Done));

        Assert.Empty(await this.repo.GetHistoryAsync(task.Id));
        Assert.Equal(task.UpdatedAt, (await this.repo.GetByIdAsync(task.Id))!.UpdatedAt);
    }

    [Fact]
    public async Task History_of_a_missing_task_throws() => await Assert.ThrowsAsync<KeyNotFoundException>(() => this.repo.GetHistoryAsync(1));

    [Fact]
    public async Task Changing_the_returned_history_does_not_change_the_store()
    {
        var task = await this.Add("t");
        await this.Move(task.Id, TaskStatus.InWork);

        var history = (List<TaskHistoryEntry>)await this.repo.GetHistoryAsync(task.Id);
        history.Clear();

        Assert.Single(await this.repo.GetHistoryAsync(task.Id));
    }

    // ---- query -------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_is_ordered_by_id()
    {
        for (var i = 0; i < 5; i++)
        {
            await this.Add("t" + i);
        }

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, (await this.repo.GetAllAsync()).Select(t => t.Id));
    }

    [Fact]
    public async Task Query_filters_by_status()
    {
        var a = await this.Add("a");
        var b = await this.Add("b");
        await this.Move(b.Id, TaskStatus.InWork);

        var result = await this.repo.QueryAsync(new TaskQuery { Status = TaskStatus.InWork });

        Assert.Equal(new[] { b.Id }, result.Items.Select(t => t.Id));
        Assert.Equal(1, result.TotalCount);
        Assert.NotEqual(a.Id, result.Items[0].Id);
    }

    [Theory]
    [InlineData("MILK", 1)]
    [InlineData("shop", 1)]
    [InlineData("  milk  ", 1)]
    [InlineData("o", 3)]
    [InlineData("nothing like this", 0)]
    [InlineData("", 3)]
    [InlineData("   ", 3)]
    [InlineData(null, 3)]
    public async Task Query_searches_title_and_description_ignoring_case(string? search, int expected)
    {
        await this.Add("Buy milk", "at the shop");
        await this.Add("Write report", "for Monday");
        await this.Add("Call mom");

        var result = await this.repo.QueryAsync(new TaskQuery { Search = search });

        Assert.Equal(expected, result.TotalCount);
    }

    [Fact]
    public async Task Query_combines_status_and_search()
    {
        var a = await this.Add("alpha");
        var b = await this.Add("alpha two");
        await this.Add("beta");
        await this.Move(b.Id, TaskStatus.InWork);

        var result = await this.repo.QueryAsync(new TaskQuery { Status = TaskStatus.InWork, Search = "alpha" });

        Assert.Equal(new[] { b.Id }, result.Items.Select(t => t.Id));
        Assert.DoesNotContain(result.Items, t => t.Id == a.Id);
    }

    [Fact]
    public async Task Query_pages_and_reports_totals()
    {
        for (var i = 1; i <= 7; i++)
        {
            await this.Add("t" + i);
        }

        var second = await this.repo.QueryAsync(new TaskQuery { Page = 2, PageSize = 3 });
        var last = await this.repo.QueryAsync(new TaskQuery { Page = 3, PageSize = 3 });
        var beyond = await this.repo.QueryAsync(new TaskQuery { Page = 9, PageSize = 3 });

        Assert.Equal(new[] { 4, 5, 6 }, second.Items.Select(t => t.Id));
        Assert.Equal(7, second.TotalCount);
        Assert.Equal(3, second.TotalPages);
        Assert.Equal(new[] { 7 }, last.Items.Select(t => t.Id));
        Assert.Empty(beyond.Items);
        Assert.Equal(7, beyond.TotalCount);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    public async Task Page_below_one_counts_as_one(int page, int expected)
    {
        await this.Add("t");

        Assert.Equal(expected, (await this.repo.QueryAsync(new TaskQuery { Page = page })).Page);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(100000, 100)]
    public async Task Page_size_is_clamped(int requested, int expected) =>
        Assert.Equal(expected, (await this.repo.QueryAsync(new TaskQuery { PageSize = requested })).PageSize);

    [Fact]
    public async Task Query_of_an_empty_store_is_empty()
    {
        var result = await this.repo.QueryAsync(new TaskQuery());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task Query_rejects_null() => await Assert.ThrowsAsync<ArgumentNullException>(() => this.repo.QueryAsync(null!));

    // ---- thread safety -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Parallel_creates_get_unique_consecutive_ids()
    {
        var tasks = Enumerable.Range(0, 500).Select(i => Task.Run(() => this.repo.CreateAsync(new Models.Task { Title = "t" + i })));

        var created = await Task.WhenAll(tasks);

        Assert.Equal(500, created.Select(t => t.Id).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 500), (await this.repo.GetAllAsync()).Select(t => t.Id));
    }

    [Fact]
    public async Task Of_many_parallel_identical_transitions_exactly_one_wins()
    {
        var task = await this.Add("t");
        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(async () =>
        {
            try
            {
                await this.repo.UpdateStatusAsync(task.Id, TaskStatus.InWork);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        })));

        Assert.Equal(1, results.Count(r => r));
        Assert.Single(await this.repo.GetHistoryAsync(task.Id));
    }

    [Fact]
    public async Task Reads_during_writes_never_throw()
    {
        await this.Add("seed");
        var writer = Task.Run(async () =>
        {
            for (var i = 0; i < 300; i++)
            {
                var t = await this.Add("w" + i);
                await this.repo.UpdateAsync(t.Id, "x" + i, "y");
                if (i % 7 == 0)
                {
                    await this.repo.DeleteAsync(t.Id);
                }
            }
        });
        var reader = Task.Run(async () =>
        {
            for (var i = 0; i < 300; i++)
            {
                _ = await this.repo.QueryAsync(new TaskQuery { Search = "x", PageSize = 10 });
                _ = (await this.repo.GetAllAsync()).ToList();
            }
        });

        await Task.WhenAll(writer, reader);
    }

    [Fact]
    public async Task Parallel_updates_and_transitions_of_one_task_stay_consistent()
    {
        var task = await this.Add("t");
        var edits = Enumerable.Range(0, 100).Select(i => Task.Run(() => this.repo.UpdateAsync(task.Id, "title" + i, "d")));
        var move = Task.Run(() => this.Move(task.Id, TaskStatus.InWork, TaskStatus.Testing, TaskStatus.Done));

        await Task.WhenAll(edits.Append(move));

        var stored = await this.repo.GetByIdAsync(task.Id);
        Assert.Equal(TaskStatus.Done, stored!.Status);
        Assert.Equal(3, (await this.repo.GetHistoryAsync(task.Id)).Count);
    }
}
