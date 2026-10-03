using FluentValidation;
using TaskManagementService.Models;

namespace TaskManagementService.Validators;

/// <summary>Page at least 1, page size 1..100, search text at most 100 characters, status a defined value.</summary>
public class TaskQueryValidator : AbstractValidator<TaskQuery>
{
    public TaskQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, TaskQuery.MaxPageSize).WithMessage("PageSize must be between 1 and 100.");
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Search cannot exceed 100 characters.");
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue).WithMessage("Invalid status value.");
    }
}
