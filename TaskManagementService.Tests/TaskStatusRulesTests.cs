using TaskManagementService.Domain;
using TaskStatus = TaskManagementService.Models.TaskStatus;

namespace TaskManagementService.Tests;

public class TaskStatusRulesTests
{
    public static IEnumerable<object[]> AllPairs() =>
        from a in Enum.GetValues<TaskStatus>()
        from b in Enum.GetValues<TaskStatus>()
        select new object[] { a, b };

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void Only_the_three_forward_steps_are_allowed(TaskStatus from, TaskStatus to)
    {
        var allowed = (from, to) is (TaskStatus.Backlog, TaskStatus.InWork) or (TaskStatus.InWork, TaskStatus.Testing) or (TaskStatus.Testing, TaskStatus.Done);

        Assert.Equal(allowed, TaskStatusRules.CanTransition(from, to));
    }

    [Theory]
    [InlineData((TaskStatus)99, TaskStatus.InWork)]
    [InlineData(TaskStatus.Backlog, (TaskStatus)99)]
    [InlineData((TaskStatus)(-1), (TaskStatus)(-1))]
    public void Undefined_values_are_never_allowed(TaskStatus from, TaskStatus to) => Assert.False(TaskStatusRules.CanTransition(from, to));

    [Theory]
    [InlineData(TaskStatus.Backlog, new[] { TaskStatus.InWork })]
    [InlineData(TaskStatus.InWork, new[] { TaskStatus.Testing })]
    [InlineData(TaskStatus.Testing, new[] { TaskStatus.Done })]
    [InlineData(TaskStatus.Done, new TaskStatus[0])]
    public void AllowedNext_lists_what_is_reachable(TaskStatus from, TaskStatus[] expected) => Assert.Equal(expected, TaskStatusRules.AllowedNext(from));

    [Fact]
    public void AllowedNext_of_an_undefined_status_is_empty() => Assert.Empty(TaskStatusRules.AllowedNext((TaskStatus)99));

    [Fact]
    public void Refusal_message_names_both_statuses() =>
        Assert.Equal("Invalid status transition from Backlog to Done.", TaskStatusRules.DescribeRefusal(TaskStatus.Backlog, TaskStatus.Done));

    [Fact]
    public void AllowedNext_and_CanTransition_agree()
    {
        foreach (var from in Enum.GetValues<TaskStatus>())
        {
            foreach (var to in Enum.GetValues<TaskStatus>())
            {
                Assert.Equal(TaskStatusRules.CanTransition(from, to), TaskStatusRules.AllowedNext(from).Contains(to));
            }
        }
    }
}
