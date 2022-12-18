using FluentValidation;

namespace ExternalSort;

internal class OptionsValidator:AbstractValidator<Options>
{
    public OptionsValidator()
    {
        RuleFor(it => it.Input)
            .NotEmpty();
        RuleFor(it => it.Output)
            .NotEmpty();
    }
}