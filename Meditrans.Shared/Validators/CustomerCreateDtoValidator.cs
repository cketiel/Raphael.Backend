using Meditrans.Shared.DTOs;
using FluentValidation;

namespace Meditrans.Shared.Validators
{
    public class CustomerCreateDtoValidator : AbstractValidator<CustomerCreateDto>
    {
        public CustomerCreateDtoValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Zip)
                .Matches(@"^\d{5}$").WithMessage("Invalid ZIP code format");

            RuleFor(x => x.Gender)
                .Must(g => new[] { "Male", "Female", "Other" }.Contains(g))
                .WithMessage("Invalid gender value");

            RuleFor(x => x.FundingSourceId)
                .GreaterThan(0).WithMessage("Invalid funding source");

            RuleFor(x => x.SpaceTypeId)
                .GreaterThan(0).WithMessage("Invalid space type");
        }
    }
}
