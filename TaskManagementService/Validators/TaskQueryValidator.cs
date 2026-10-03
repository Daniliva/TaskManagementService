using FluentValidation;
using TaskManagementService.Models;

namespace TaskManagementService.Validators;

/// <summary>
/// TODO (spec): Page at least 1 ("Page must be at least 1."), PageSize 1..TaskQuery.MaxPageSize ("PageSize must be between 1 and 100."),
/// Search at most 100 characters, Status (when given) a defined enum value ("Invalid status value.").
/// </summary>
public class TaskQueryValidator : AbstractValidator<TaskQuery>
{
}
