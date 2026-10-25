using System;

using FluentValidation;

using Xdb.Api.Application.Common.Metadata;

namespace Xdb.Api.Application.Properties.Commands.CreateProperty;

/// <summary>Shape rules; rules that need the database (live member, existing list, type and role) are the handler's.</summary>
public sealed class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    /// <summary>Lengths of Properties.Name and Properties.DisplayValue.</summary>
    public const int NameMaxLength = 100;

    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Id).NotEqual(Guid.Empty).When(x => x.Id.HasValue);

        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.DisplayValue).MaximumLength(NameMaxLength);

        RuleFor(x => x.DataType)
            .IsInEnum()
            .Must(t => !IsSystem(t)).WithMessage("System data types are reserved for system properties.");

        // One condition per rule: a When applies to every earlier validator in its chain.
        RuleFor(x => x.PickListId)
            .NotNull().When(x => x.DataType == DataType.PickList).WithMessage("A PickList property needs a pick list.");
        RuleFor(x => x.PickListId)
            .Null().When(x => x.DataType != DataType.PickList).WithMessage("Only a PickList property takes a pick list.");

        RuleFor(x => x.AssetTypeId)
            .NotNull().When(x => x.DataType == DataType.Asset).WithMessage("An Asset property needs the asset type it refers to.");
        RuleFor(x => x.AssetTypeIsInstance)
            .NotNull().When(x => x.DataType == DataType.Asset).WithMessage("An Asset property must say whether it refers to instances or definitions.");
        RuleFor(x => x.AssetTypeId)
            .Must(id => id is null || id == SystemAssetTypeIds.User).When(x => x.DataType == DataType.User)
            .WithMessage("A User property always refers to the User type.");
        RuleFor(x => x.AssetTypeId)
            .Null().When(x => x.DataType is not (DataType.Asset or DataType.User))
            .WithMessage("Only an Asset or User property refers to an asset type.");
        // The handler sets a User property's level itself.
        RuleFor(x => x.AssetTypeIsInstance)
            .Null().When(x => x.DataType != DataType.Asset)
            .WithMessage("Only an Asset property says whether it refers to instances or definitions.");

        RuleFor(x => x.RoleId)
            .Null().When(x => x.DataType != DataType.User).WithMessage("Only a User property can be limited to a role.");
    }

    private static bool IsSystem(DataType type) =>
        type is DataType.SystemAssetType or DataType.SystemDescription or DataType.SystemInstanceOf
            or DataType.SystemAssetName or DataType.SystemInstanceOfDescription;
}
