using System;

using FluentAssertions;

using Xdb.Api.Application.Views.Commands.CreateView;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Views.Commands.CreateView;

public class CreateViewCommandValidatorTests
{
    private readonly CreateViewCommandValidator _validator = new();

    private static CreateViewCommand Valid() => new()
    {
        ActorId = Guid.NewGuid(),
        AssetTypeId = Guid.NewGuid(),
        IsInstance = true,
        Name = "Venues",
        DriverCaption = "Venue",
        NewItemCaption = "Create Venue",
        PropertyGroupIds = [Guid.NewGuid(), Guid.NewGuid()],
        RoleIds = [Guid.NewGuid()]
    };

    [Fact]
    public void ViewWithGroupsAndRoles_IsValid()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void BareView_IsValid()
    {
        _validator.Validate(Valid() with { DriverCaption = null, NewItemCaption = null, PropertyGroupIds = [], RoleIds = [] })
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingActorTypeOrName_Fails()
    {
        var result = _validator.Validate(Valid() with { ActorId = Guid.Empty, AssetTypeId = Guid.Empty, Name = "" });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.ActorId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.AssetTypeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.Name));
    }

    [Fact]
    public void TextLongerThanItsColumn_Fails()
    {
        var name = new string('x', CreateViewCommandValidator.NameMaxLength + 1);
        var caption = new string('x', CreateViewCommandValidator.CaptionMaxLength + 1);

        var result = _validator.Validate(Valid() with { Name = name, DisplayValue = name, DriverCaption = caption, NewItemCaption = caption });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.DisplayValue));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.DriverCaption));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.NewItemCaption));
    }

    [Fact]
    public void EmptyIds_Fail()
    {
        var result = _validator.Validate(Valid() with { Id = Guid.Empty, PropertyGroupIds = [Guid.Empty], RoleIds = [Guid.Empty] });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.Id));
        result.Errors.Should().Contain(e => e.PropertyName == "PropertyGroupIds[0]");
        result.Errors.Should().Contain(e => e.PropertyName == "RoleIds[0]");
    }

    [Fact]
    public void RepeatedGroupOrRole_Fails()
    {
        var id = Guid.NewGuid();

        var result = _validator.Validate(Valid() with { PropertyGroupIds = [id, id], RoleIds = [id, id] });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.PropertyGroupIds));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateViewCommand.RoleIds));
    }
}
