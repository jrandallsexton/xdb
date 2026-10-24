using System;

using FluentAssertions;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Properties.Commands.CreateProperty;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Properties.Commands.CreateProperty;

public class CreatePropertyCommandValidatorTests
{
    private readonly CreatePropertyCommandValidator _validator = new();

    private static CreatePropertyCommand Valid(DataType type = DataType.String) =>
        new() { ActorId = Guid.NewGuid(), Name = "City", DataType = type };

    [Fact]
    public void PlainStringProperty_IsValid()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingActorOrName_Fails()
    {
        var result = _validator.Validate(Valid() with { ActorId = Guid.Empty, Name = "" });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyCommand.ActorId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyCommand.Name));
    }

    [Fact]
    public void EmptyFixedId_Fails()
    {
        _validator.Validate(Valid() with { Id = Guid.Empty }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.Id));
    }

    [Fact]
    public void NameLongerThanTheColumn_Fails()
    {
        _validator.Validate(Valid() with { Name = new string('x', CreatePropertyCommandValidator.NameMaxLength + 1) }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.Name));
    }

    [Theory]
    [InlineData(DataType.SystemAssetType)]
    [InlineData(DataType.SystemDescription)]
    [InlineData(DataType.SystemInstanceOf)]
    [InlineData(DataType.SystemAssetName)]
    [InlineData(DataType.SystemInstanceOfDescription)]
    public void SystemDataType_Fails(DataType type)
    {
        _validator.Validate(Valid(type)).Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.DataType));
    }

    [Fact]
    public void UndefinedDataType_Fails()
    {
        _validator.Validate(Valid((DataType)200)).Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyCommand.DataType));
    }

    [Fact]
    public void PickListProperty_NeedsAPickList()
    {
        _validator.Validate(Valid(DataType.PickList)).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.PickListId));
        _validator.Validate(Valid(DataType.PickList) with { PickListId = Guid.NewGuid() }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void NonPickListProperty_TakesNoPickList()
    {
        _validator.Validate(Valid() with { PickListId = Guid.NewGuid() }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.PickListId));
    }

    [Fact]
    public void AssetProperty_NeedsTheTargetTypeAndLevel()
    {
        var result = _validator.Validate(Valid(DataType.Asset));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyCommand.AssetTypeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyCommand.AssetTypeIsInstance));
        _validator.Validate(Valid(DataType.Asset) with { AssetTypeId = Guid.NewGuid(), AssetTypeIsInstance = true }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UserProperty_CannotTargetAnotherType()
    {
        _validator.Validate(Valid(DataType.User) with { AssetTypeId = Guid.NewGuid() }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.AssetTypeId));
        _validator.Validate(Valid(DataType.User) with { AssetTypeId = SystemAssetTypeIds.User }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Role_IsOnlyForUserProperties()
    {
        _validator.Validate(Valid() with { RoleId = Guid.NewGuid() }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.RoleId));
        _validator.Validate(Valid(DataType.User) with { RoleId = Guid.NewGuid() }).IsValid.Should().BeTrue();
    }
}
