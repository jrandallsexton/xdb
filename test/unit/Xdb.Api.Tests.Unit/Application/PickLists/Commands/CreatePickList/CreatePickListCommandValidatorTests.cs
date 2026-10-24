using System;

using FluentAssertions;

using Xdb.Api.Application.PickLists.Commands.CreatePickList;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.PickLists.Commands.CreatePickList;

public class CreatePickListCommandValidatorTests
{
    private readonly CreatePickListCommandValidator _validator = new();

    private static CreatePickListCommand Valid() => new()
    {
        ActorId = Guid.NewGuid(),
        Name = "Surface",
        Description = "Playing surface",
        Values = [new CreatePickListValue { Value = "Grass" }, new CreatePickListValue { Value = "Artificial" }]
    };

    [Fact]
    public void ListWithValues_IsValid()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListWithoutValues_IsValid()
    {
        _validator.Validate(Valid() with { Values = [] }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingActorNameOrDescription_Fails()
    {
        var result = _validator.Validate(Valid() with { ActorId = Guid.Empty, Name = "", Description = "" });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePickListCommand.ActorId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePickListCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePickListCommand.Description));
    }

    [Fact]
    public void EmptyFixedId_Fails()
    {
        _validator.Validate(Valid() with { Id = Guid.Empty }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePickListCommand.Id));
    }

    [Fact]
    public void NameLongerThanTheColumn_Fails()
    {
        _validator.Validate(Valid() with { Name = new string('x', CreatePickListCommandValidator.NameMaxLength + 1) }).Errors
            .Should().ContainSingle(e => e.PropertyName == nameof(CreatePickListCommand.Name));
    }

    [Fact]
    public void EmptyOrOverlongValue_Fails()
    {
        var result = _validator.Validate(Valid() with
        {
            Values =
            [
                new CreatePickListValue { Value = "" },
                new CreatePickListValue { Value = new string('x', CreatePickListCommandValidator.ValueMaxLength + 1) }
            ]
        });

        result.Errors.Should().Contain(e => e.PropertyName == "Values[0].Value");
        result.Errors.Should().Contain(e => e.PropertyName == "Values[1].Value");
    }

    [Fact]
    public void ValuesDifferingOnlyByCase_Fail()
    {
        _validator.Validate(Valid() with { Values = [new CreatePickListValue { Value = "Grass" }, new CreatePickListValue { Value = "grass" }] })
            .Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePickListCommand.Values));
    }

    [Fact]
    public void RepeatedValueId_Fails()
    {
        var id = Guid.NewGuid();

        _validator.Validate(Valid() with
        {
            Values = [new CreatePickListValue { Id = id, Value = "Grass" }, new CreatePickListValue { Id = id, Value = "Artificial" }]
        }).Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePickListCommand.Values));
    }

    [Fact]
    public void EmptyValueId_Fails()
    {
        _validator.Validate(Valid() with { Values = [new CreatePickListValue { Id = Guid.Empty, Value = "Grass" }] })
            .Errors.Should().ContainSingle(e => e.PropertyName == "Values[0].Id");
    }
}
