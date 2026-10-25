using System;

using FluentAssertions;

using Xdb.Api.Application.PropertyGroups.Commands.CreatePropertyGroup;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.PropertyGroups.Commands.CreatePropertyGroup;

public class CreatePropertyGroupCommandValidatorTests
{
    private readonly CreatePropertyGroupCommandValidator _validator = new();

    private static CreatePropertyGroupCommand Valid() => new()
    {
        ActorId = Guid.NewGuid(),
        AssetTypeId = Guid.NewGuid(),
        IsInstance = true,
        Name = "Location",
        Properties = [new CreatePropertyGroupProperty(Guid.NewGuid(), IsRequired: true), new CreatePropertyGroupProperty(Guid.NewGuid(), IsReadOnly: true)]
    };

    [Fact]
    public void GroupWithProperties_IsValid()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyGroup_IsValid()
    {
        _validator.Validate(Valid() with { Properties = [] }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingActorTypeOrName_Fails()
    {
        var result = _validator.Validate(Valid() with { ActorId = Guid.Empty, AssetTypeId = Guid.Empty, Name = "" });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyGroupCommand.ActorId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyGroupCommand.AssetTypeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyGroupCommand.Name));
    }

    [Fact]
    public void NameOrDisplayValueLongerThanTheColumn_Fails()
    {
        var tooLong = new string('x', CreatePropertyGroupCommandValidator.NameMaxLength + 1);

        var result = _validator.Validate(Valid() with { Name = tooLong, DisplayValue = tooLong });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyGroupCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyGroupCommand.DisplayValue));
    }

    [Fact]
    public void EmptyId_Fails()
    {
        _validator.Validate(Valid() with { Id = Guid.Empty }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyGroupCommand.Id));
    }

    [Fact]
    public void EmptyPropertyId_Fails()
    {
        _validator.Validate(Valid() with { Properties = [new CreatePropertyGroupProperty(Guid.Empty)] }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Properties[0].PropertyId");
    }

    [Fact]
    public void RequiredAndReadOnly_Fails()
    {
        _validator.Validate(Valid() with { Properties = [new CreatePropertyGroupProperty(Guid.NewGuid(), IsRequired: true, IsReadOnly: true)] }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Properties[0].IsReadOnly");
    }

    [Fact]
    public void SamePropertyTwice_Fails()
    {
        var id = Guid.NewGuid();

        _validator.Validate(Valid() with { Properties = [new CreatePropertyGroupProperty(id), new CreatePropertyGroupProperty(id, IsRequired: true)] }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyGroupCommand.Properties));
    }
}
