using TaskManagementService.DTOs;
using TaskManagementService.Models;
using TaskManagementService.Validators;

namespace TaskManagementService.Tests;

public class ValidatorTests
{
    [Theory]
    [InlineData("Task", "", true)]
    [InlineData("T", "d", true)]
    [InlineData("", "d", false)]
    [InlineData("   ", "d", false)]
    public void Create_title_required(string title, string description, bool valid) =>
        Assert.Equal(valid, new CreateTaskDtoValidator().Validate(new CreateTaskDto { Title = title, Description = description }).IsValid);

    [Theory]
    [InlineData(100, 1000, true)]
    [InlineData(101, 0, false)]
    [InlineData(1, 1001, false)]
    public void Create_length_limits(int titleLength, int descriptionLength, bool valid) =>
        Assert.Equal(valid, new CreateTaskDtoValidator().Validate(new CreateTaskDto { Title = new string('a', titleLength), Description = new string('b', descriptionLength) }).IsValid);

    [Theory]
    [InlineData("New", "", true)]
    [InlineData("", "", false)]
    [InlineData("  ", "x", false)]
    [InlineData("a", "b", true)]
    public void Update_task_rules_match_create(string title, string description, bool valid) =>
        Assert.Equal(valid, new UpdateTaskDtoValidator().Validate(new UpdateTaskDto { Title = title, Description = description }).IsValid);

    [Fact]
    public void Update_task_length_limits()
    {
        var sut = new UpdateTaskDtoValidator();

        Assert.False(sut.Validate(new UpdateTaskDto { Title = new string('a', 101) }).IsValid);
        Assert.False(sut.Validate(new UpdateTaskDto { Title = "a", Description = new string('b', 1001) }).IsValid);
    }

    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 20, false)]
    [InlineData(-1, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void Query_paging_limits(int page, int pageSize, bool valid) =>
        Assert.Equal(valid, new TaskQueryValidator().Validate(new TaskQuery { Page = page, PageSize = pageSize }).IsValid);

    [Fact]
    public void Query_search_is_limited_and_status_must_be_defined()
    {
        var sut = new TaskQueryValidator();

        Assert.True(sut.Validate(new TaskQuery { Search = new string('x', 100) }).IsValid);
        Assert.False(sut.Validate(new TaskQuery { Search = new string('x', 101) }).IsValid);
        Assert.False(sut.Validate(new TaskQuery { Status = (Models.TaskStatus)42 }).IsValid);
        Assert.True(sut.Validate(new TaskQuery { Status = null }).IsValid);
    }

    [Fact]
    public void Query_error_messages_are_readable()
    {
        var result = new TaskQueryValidator().Validate(new TaskQuery { Page = 0, PageSize = 500 });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Page must be at least 1.");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "PageSize must be between 1 and 100.");
    }
}
