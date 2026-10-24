using System;

using FluentAssertions;

using Xdb.Api.Application.Assets.Commands.CreateAsset;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Assets.Commands.CreateAsset;

public class CreateAssetCommandValidatorTests
{
    private readonly CreateAssetCommandValidator _validator = new();

    private static CreateAssetCommand Valid() => new()
    {
        ActorId = Guid.NewGuid(),
        AssetTypeId = Guid.NewGuid(),
        Name = "A.J. McClung Stadium",
        InstanceOfId = Guid.NewGuid(),
        Values = [new CreateAssetValue(Guid.NewGuid(), "Columbus")]
    };

    [Fact]
    public void InstanceWithValues_IsValid()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DefinitionWithoutValues_IsValid()
    {
        _validator.Validate(Valid() with { InstanceOfId = null, Values = [] }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingActorTypeOrName_Fails()
    {
        var result = _validator.Validate(Valid() with { ActorId = Guid.Empty, AssetTypeId = Guid.Empty, Name = "" });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetCommand.ActorId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetCommand.AssetTypeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetCommand.Name));
    }

    [Fact]
    public void NameLongerThanTheColumn_Fails()
    {
        _validator.Validate(Valid() with { Name = new string('x', CreateAssetCommandValidator.NameMaxLength + 1) }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetCommand.Name));
    }

    [Fact]
    public void InstanceOfItself_Fails()
    {
        var id = Guid.NewGuid();

        _validator.Validate(Valid() with { Id = id, InstanceOfId = id }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetCommand.InstanceOfId));
    }

    [Fact]
    public void EmptyValueOrPropertyId_Fails()
    {
        var result = _validator.Validate(Valid() with { Values = [new CreateAssetValue(Guid.Empty, "")] });

        result.Errors.Should().Contain(e => e.PropertyName == "Values[0].PropertyId");
        result.Errors.Should().Contain(e => e.PropertyName == "Values[0].Value");
    }
}
