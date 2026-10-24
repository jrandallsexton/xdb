using System;

using FluentAssertions;

using Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.AssetTypes.Commands.CreateAssetType;

public class CreateAssetTypeCommandValidatorTests
{
    private readonly CreateAssetTypeCommandValidator _validator = new();

    private static CreateAssetTypeCommand Valid() => new()
    {
        ActorId = Guid.NewGuid(),
        Name = "Venue",
        Pluralization = "Venues",
        DefinitionLabel = "Venue Kind",
        InstanceLabel = "Venue",
        Properties = [new CreateAssetTypeProperty(Guid.NewGuid(), IsInstance: true), new CreateAssetTypeProperty(Guid.NewGuid(), IsInstance: false)]
    };

    [Fact]
    public void TypeWithLinksAtBothLevels_IsValid()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingActorNameOrPluralization_Fails()
    {
        var result = _validator.Validate(Valid() with { ActorId = Guid.Empty, Name = "", Pluralization = "" });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetTypeCommand.ActorId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetTypeCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetTypeCommand.Pluralization));
    }

    [Fact]
    public void LongerThanTheColumns_Fails()
    {
        var result = _validator.Validate(Valid() with
        {
            Name = new string('x', CreateAssetTypeCommandValidator.NameMaxLength + 1),
            InstanceLabel = new string('x', CreateAssetTypeCommandValidator.LabelMaxLength + 1)
        });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetTypeCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAssetTypeCommand.InstanceLabel));
    }

    [Fact]
    public void OwnParent_Fails()
    {
        var id = Guid.NewGuid();

        _validator.Validate(Valid() with { Id = id, ParentId = id }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetTypeCommand.ParentId));
    }

    [Fact]
    public void SameLinkTwice_Fails()
    {
        var propertyId = Guid.NewGuid();

        _validator.Validate(Valid() with
        {
            Properties = [new CreateAssetTypeProperty(propertyId, true), new CreateAssetTypeProperty(propertyId, true)]
        }).Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetTypeCommand.Properties));
    }

    [Fact]
    public void SamePropertyAtBothLevels_IsValid()
    {
        var propertyId = Guid.NewGuid();

        _validator.Validate(Valid() with
        {
            Properties = [new CreateAssetTypeProperty(propertyId, true), new CreateAssetTypeProperty(propertyId, false)]
        }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void InstanceLinkWithoutAnInstanceLabel_Fails()
    {
        _validator.Validate(Valid() with { InstanceLabel = null }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetTypeCommand.Properties));
    }

    [Fact]
    public void EmptyPropertyId_Fails()
    {
        _validator.Validate(Valid() with { Properties = [new CreateAssetTypeProperty(Guid.Empty, false)] }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Properties[0].PropertyId");
    }
}
