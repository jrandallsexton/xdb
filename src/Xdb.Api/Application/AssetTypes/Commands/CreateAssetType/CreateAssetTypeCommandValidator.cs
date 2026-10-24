using System;
using System.Linq;

using FluentValidation;

namespace Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;

/// <summary>Shape rules; rules that need the database (live member, free name, parent, properties) are the handler's.</summary>
public sealed class CreateAssetTypeCommandValidator : AbstractValidator<CreateAssetTypeCommand>
{
    /// <summary>Lengths of AssetTypes.Name and AssetTypes.Pluralization.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Lengths of AssetTypes.DefinitionLabel and AssetTypes.InstanceLabel.</summary>
    public const int LabelMaxLength = 50;

    public CreateAssetTypeCommandValidator()
    {
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Id).NotEqual(Guid.Empty).When(x => x.Id.HasValue);

        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.Pluralization).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.DefinitionLabel).MaximumLength(LabelMaxLength);
        RuleFor(x => x.InstanceLabel).MaximumLength(LabelMaxLength);

        RuleFor(x => x.ParentId).NotEqual(Guid.Empty).When(x => x.ParentId.HasValue);
        RuleFor(x => x.ParentId).NotEqual(x => x.Id).When(x => x.ParentId.HasValue && x.Id.HasValue)
            .WithMessage("A type cannot be its own parent.");

        RuleFor(x => x.Properties).NotNull();
        RuleForEach(x => x.Properties).ChildRules(link => link.RuleFor(l => l.PropertyId).NotEmpty());
        RuleFor(x => x.Properties)
            .Must(links => links.Distinct().Count() == links.Count)
            .When(x => x.Properties is not null)
            .WithMessage("A property is linked at the same level more than once.");
        // Without an instance label the type has no instance level: no instance gen table, nothing to show them in.
        RuleFor(x => x.Properties)
            .Must(links => links.All(l => !l.IsInstance))
            .When(x => x.Properties is not null && string.IsNullOrEmpty(x.InstanceLabel))
            .WithMessage("Instance properties need an instance label.");
    }
}
