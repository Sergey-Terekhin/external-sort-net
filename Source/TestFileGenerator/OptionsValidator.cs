using FluentValidation;

namespace ExternalSort;

internal class OptionsValidator:AbstractValidator<Options>
{
    public OptionsValidator()
    {
        RuleFor(it => it.Size)
            .GreaterThan(0);
        RuleFor(it => it.StringLength)
            .GreaterThan(0)
            .LessThanOrEqualTo(Constants.MaxStringLength);
        RuleFor(it => it.DuplicatesRatio)
            .GreaterThanOrEqualTo(0.0)
            .LessThanOrEqualTo(1.0);
        RuleFor(it => it.Output)
            .NotEmpty();
    }
}