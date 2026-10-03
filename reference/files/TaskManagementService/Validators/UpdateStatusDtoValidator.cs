using FluentValidation;
using TaskManagementService.DTOs;

namespace TaskManagementService.Validators;

public class UpdateStatusDtoValidator : AbstractValidator<UpdateStatusDto>
{
    public UpdateStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required.")
            .Must(s => s is null || Enum.IsDefined(s.Value)).WithMessage("Invalid status value.");
    }
}
