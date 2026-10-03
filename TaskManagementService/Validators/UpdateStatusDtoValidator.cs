using FluentValidation;
using TaskManagementService.DTOs;

namespace TaskManagementService.Validators;

/// <summary>TODO (spec): Status is required ("Status is required.") and must be a defined value ("Invalid status value."). Status is nullable on purpose.</summary>
public class UpdateStatusDtoValidator : AbstractValidator<UpdateStatusDto>
{
    public UpdateStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.");
    }
}
