using TaskManagementService.DTOs;
using TaskManagementService.Models;
using TaskManagementService.Repositories;
using TaskManagementService.Services;
using TaskManagementService.Validators;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace TaskManagementService.Tests;

public class TaskRepositoryTests
{
    private readonly InMemoryTaskRepository _repo = new();

    [Fact]
    public async Task CreateAsync_ShouldAssignIdAndDefaultStatus()
    {
        var task = new Models.Task { Title = "Test", Description = "Desc" };
        var result = await _repo.CreateAsync(task);

        Assert.Equal(1, result.Id);
        Assert.Equal(Models.TaskStatus.Backlog, result.Status);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTasks()
    {
        await _repo.CreateAsync(new Models.Task { Title = "T1" });
        await _repo.CreateAsync(new Models.Task { Title = "T2" });

        var tasks = await _repo.GetAllAsync();
        Assert.Equal(2, tasks.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnTask()
    {
        var created = await _repo.CreateAsync(new Models.Task { Title = "Find me" });
        var found = await _repo.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, found?.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        var found = await _repo.GetByIdAsync(999);
        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_ShouldUpdate()
    {
        var task = await _repo.CreateAsync(new Models.Task { Title = "Test" });
        await _repo.UpdateStatusAsync(task.Id, Models.TaskStatus.InWork);

        var updated = await _repo.GetByIdAsync(task.Id);
        Assert.Equal(Models.TaskStatus.InWork, updated?.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ShouldThrow()
    {
        var task = await _repo.CreateAsync(new Models.Task { Title = "Test", Description = "" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repo.UpdateStatusAsync(task.Id, Models.TaskStatus.Done));

        Assert.Equal(
            "Invalid status transition from Backlog to Done.",
            ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistingTask_ShouldThrowKeyNotFound()
    {
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repo.UpdateStatusAsync(999, Models.TaskStatus.InWork));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_SameStatus_ShouldBeInvalid()
    {
        var task = await _repo.CreateAsync(new Models.Task { Title = "Test" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repo.UpdateStatusAsync(task.Id, Models.TaskStatus.Backlog));

        Assert.Contains("transition", ex.Message);
    }
}