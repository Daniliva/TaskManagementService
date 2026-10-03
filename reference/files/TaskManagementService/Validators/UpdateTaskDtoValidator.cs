using FluentValidation;
using TaskManagementService.DTOs;

namespace TaskManagementService.Validators;

/// <summary>Same rules as for creating a task: the title is required (whitespace only counts as empty) and at most 100 characters, the description at most 1000.</summary>
public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}
