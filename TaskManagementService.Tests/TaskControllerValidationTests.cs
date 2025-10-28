using TaskManagementService.DTOs;
using TaskManagementService.Models;
using TaskManagementService.Repositories;
using TaskManagementService.Services;
using TaskManagementService.Validators;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace TaskManagementService.Tests;

public class TaskControllerValidationTests
{
    private readonly CreateTaskDtoValidator _createValidator = new();
    private readonly UpdateStatusDtoValidator _statusValidator = new();

    [Fact]
    public async Task CreateValidator_EmptyTitle_ShouldHaveError()
    {
        var dto = new CreateTaskDto { Title = "", Description = "Desc" };
        var result = await _createValidator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Title is required.");
    }

    [Fact]
    public async Task CreateValidator_LongTitle_ShouldHaveError()
    {
        var dto = new CreateTaskDto { Title = new string('A', 101), Description = "Desc" };
        var result = await _createValidator.ValidateAsync(dto);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public async Task UpdateStatusValidator_InvalidEnum_ShouldHaveError()
    {
        var dto = new UpdateStatusDto { Status = (Models.TaskStatus)999 };
        var result = await _statusValidator.ValidateAsync(dto);
        Assert.False(result.IsValid);
    }
}