using System;

using FluentValidation;

namespace Xdb.Api.Application.Assets.Commands.CreateAsset;

/// <summary>Shape rules; rules that need the database (type, links, value forms that are references) are the handler's.</summary>
public sealed class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommand>
{
    /// <summary>Length of Assets.Name.</summary>
    public const int NameMaxLength = 255;

    public CreateAssetCommandValidator()
    {
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Id).NotEqual(Guid.Empty).When(x => x.Id.HasValue);
        RuleFor(x => x.AssetTypeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.InstanceOfId).NotEqual(Guid.Empty).When(x => x.InstanceOfId.HasValue);
        RuleFor(x => x.InstanceOfId).NotEqual(x => x.Id).When(x => x.InstanceOfId.HasValue && x.Id.HasValue)
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
