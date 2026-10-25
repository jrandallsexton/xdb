using System;
using System.Linq;

using FluentValidation;

namespace Xdb.Api.Application.PropertyGroups.Commands.CreatePropertyGroup;

/// <summary>Shape rules; rules that need the database (live member, type, free name, properties) are the handler's.</summary>
public sealed class CreatePropertyGroupCommandValidator : AbstractValidator<CreatePropertyGroupCommand>
{
    /// <summary>Length of PropertyGroups.Name and PropertyGroups.DisplayValue.</summary>
    public const int NameMaxLength = 100;

    public CreatePropertyGroupCommandValidator()
    {
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Id).NotEqual(Guid.Empty).When(x => x.Id.HasValue);
        RuleFor(x => x.AssetTypeId).NotEmpty();

        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.DisplayValue).MaximumLength(NameMaxLength);

        RuleFor(x => x.Properties).NotNull();
        RuleForEach(x => x.Properties).ChildRules(member =>
        {
            member.RuleFor(m => m.PropertyId).NotEmpty();
            // Nothing could ever fill it in: groups carry no default values.
            member.RuleFor(m => m.IsReadOnly).Equal(false).When(m => m.IsRequired)
                .WithMessage("A property cannot be both required and read-only.");
        });
        RuleFor(x => x.Properties)
            .Must(members => members.Select(m => m.PropertyId).Distinct().Count() == members.Count)
            .When(x => x.Properties is not null)
            .WithMessage("A property appears in the group more than once.");
    }
}
