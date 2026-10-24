using System;
using System.Linq;

using FluentValidation;

namespace Xdb.Api.Application.PickLists.Commands.CreatePickList;

/// <summary>Shape rules; rules that need the database (live member, unique name, free ids) are the handler's.</summary>
public sealed class CreatePickListCommandValidator : AbstractValidator<CreatePickListCommand>
{
    /// <summary>Lengths of PickLists.Name and PickLists.DisplayValue.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Lengths of PickListValues.Value and PickListValues.DisplayValue.</summary>
    public const int ValueMaxLength = 255;

    public CreatePickListCommandValidator()
    {
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Id).NotEqual(Guid.Empty).When(x => x.Id.HasValue);

        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.DisplayValue).MaximumLength(NameMaxLength);
        RuleFor(x => x.Description).NotEmpty();

        RuleFor(x => x.Values).NotNull();
        RuleForEach(x => x.Values).ChildRules(value =>
        {
            value.RuleFor(v => v.Id).NotEqual(Guid.Empty).When(v => v.Id.HasValue);
            value.RuleFor(v => v.Value).NotEmpty().MaximumLength(ValueMaxLength);
            value.RuleFor(v => v.DisplayValue).MaximumLength(ValueMaxLength);
        });

        // The columns compare case-insensitively, so "Grass" and "grass" are the same value.
        RuleFor(x => x.Values)
            .Must(values => values.Select(v => v.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() == values.Count)
            .When(x => x.Values is not null)
            .WithMessage("Pick list values must be distinct.");
        RuleFor(x => x.Values)
            .Must(values => values.Where(v => v.Id.HasValue).Select(v => v.Id).Distinct().Count() == values.Count(v => v.Id.HasValue))
            .When(x => x.Values is not null)
            .WithMessage("Pick list value ids must be distinct.");
    }
}
