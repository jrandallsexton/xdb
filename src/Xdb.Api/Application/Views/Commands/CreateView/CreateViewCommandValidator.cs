using System;
using System.Linq;

using FluentValidation;

namespace Xdb.Api.Application.Views.Commands.CreateView;

/// <summary>Shape rules; rules that need the database (live member, type, free name, groups, roles) are the handler's.</summary>
public sealed class CreateViewCommandValidator : AbstractValidator<CreateViewCommand>
{
    /// <summary>Length of Views.Name and Views.DisplayValue.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Length of Views.DriverCaption and Views.NewItemCaption.</summary>
    public const int CaptionMaxLength = 50;

    public CreateViewCommandValidator()
    {
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Id).NotEqual(Guid.Empty).When(x => x.Id.HasValue);
        RuleFor(x => x.AssetTypeId).NotEmpty();

        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.DisplayValue).MaximumLength(NameMaxLength);
        RuleFor(x => x.DriverCaption).MaximumLength(CaptionMaxLength);
        RuleFor(x => x.NewItemCaption).MaximumLength(CaptionMaxLength);

        RuleFor(x => x.PropertyGroupIds).NotNull();
        RuleForEach(x => x.PropertyGroupIds).NotEmpty();
        RuleFor(x => x.PropertyGroupIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .When(x => x.PropertyGroupIds is not null)
            .WithMessage("A property group appears in the view more than once.");

        RuleFor(x => x.RoleIds).NotNull();
        RuleForEach(x => x.RoleIds).NotEmpty();
        RuleFor(x => x.RoleIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .When(x => x.RoleIds is not null)
            .WithMessage("A role is listed more than once.");
    }
}
