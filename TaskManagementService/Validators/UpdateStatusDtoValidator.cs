using FluentValidation;
using TaskManagementService.DTOs;

namespace TaskManagementService.Validators;

public class UpdateStatusDtoValidator : AbstractValidator<UpdateStatusDto>
{
    public UpdateStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.");
    }
}