using FluentValidation;
using TaskManagementService.DTOs;

namespace TaskManagementService.Validators;

/// <summary>
/// TODO (spec): same rules and messages as CreateTaskDtoValidator: Title required (whitespace only counts as empty, "Title is required."),
/// at most 100 characters ("Title cannot exceed 100 characters."), Description at most 1000 ("Description cannot exceed 1000 characters.").
/// </summary>
public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
}
