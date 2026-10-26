using System;
using System.Linq;

using FluentValidation;

using Xdb.Api.Application.Assets.Commands.CreateAsset;

namespace Xdb.Api.Application.Assets.Commands.CreateAssets;

/// <summary>The command's shape; a bad item is rejected on its own (<see cref="CreateAssetsItemValidator"/>).</summary>
public sealed class CreateAssetsCommandValidator : AbstractValidator<CreateAssetsCommand>
{
    public CreateAssetsCommandValidator()
    {
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.AssetTypeId).NotEmpty();
        RuleFor(x => x.Assets).NotNull();
        RuleFor(x => x.Assets).Must(assets => assets.Select(a => a.Id).Distinct().Count() == assets.Count)
            .When(x => x.Assets is not null)
            .WithMessage("Each asset id appears once.");
    }
}

/// <summary>One item's shape; rules that need the database (links, references, the definition) are the handler's.</summary>
public sealed class CreateAssetsItemValidator : AbstractValidator<CreateAssetsItem>
{
    public CreateAssetsItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(CreateAssetCommandValidator.NameMaxLength);
        RuleFor(x => x.InstanceOfId).NotEqual(Guid.Empty).When(x => x.InstanceOfId.HasValue);
        RuleFor(x => x.InstanceOfId).NotEqual(x => x.Id).When(x => x.InstanceOfId.HasValue)
            .WithMessage("An asset cannot be an instance of itself.");

        RuleFor(x => x.Values).NotNull();
        // An absent value is an omitted property, not an empty string.
        RuleForEach(x => x.Values).ChildRules(value =>
        {
            value.RuleFor(v => v.PropertyId).NotEmpty();
            value.RuleFor(v => v.Value).NotEmpty();
        });
    }
}
