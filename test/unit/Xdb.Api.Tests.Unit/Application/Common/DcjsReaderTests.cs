using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using FluentAssertions;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Views.Legacy;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Common;

/// <summary>Cases measured against the original serializer (spec M6-M, research FX labels in the names).</summary>
public class DcjsReaderTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 15, 30, 45, 123, DateTimeKind.Local);
    private static readonly DateTime Old = new(2020, 9, 13, 8, 26, 0, DateTimeKind.Local);
    private static readonly Guid Creator = new("44444444-4444-4444-4444-444444444444");

    private static readonly DcjsReader Reader = new(() => Now);

    /// <summary>A body as the admin pages post it: the GET output (alphabetical, every member), edited.</summary>
    private static JsonObject Body<T>(T contract)
    {
        using var stream = new MemoryStream();
        new DataContractJsonSerializer(typeof(T)).WriteObject(stream, contract);
        return JsonNode.Parse(Encoding.UTF8.GetString(stream.ToArray()))!.AsObject();
    }

    private static T Read<T>(JsonNode body) where T : class => Reader.Read<T>(JsonDocument.Parse(body.ToJsonString()).RootElement);

    private static PropertyContract Property(bool isReadOnly = true) => new()
    {
        Id = Guid.NewGuid(), Name = "P", Created = Old, CreatedBy = Creator, DataType = 2, LastModified = Old, LastModifiedBy = Creator, IsReadOnly = isReadOnly,
        Description = "D"
    };

    [Fact]
    public void AssetType_AlphabeticalBody_EndsDirty_B1()
    {
        var body = Body(new AssetTypeContract
        {
            Id = Guid.NewGuid(), Name = "N", Pluralization = "Ns", Created = Old, CreatedBy = Creator,
            PropertyMembers = [new AssetTypePropertyRelationContract { Id = Guid.NewGuid(), AssetTypeId = Guid.NewGuid(), PropertyId = Guid.NewGuid(), Created = Old, CreatedBy = Creator }],
            Properties = [Property()]
        });

        var type = Read<AssetTypeContract>(body);

        type.IsDirty.Should().BeTrue();
        type.IsNew.Should().BeFalse();
        type.PropertyMembers.Should().ContainSingle().Which.IsDirty.Should().BeTrue();
        type.Created.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void AssetType_IsDirtyPostedLast_StaysFalse_B5()
    {
        var body = Body(new AssetTypeContract { Id = Guid.NewGuid(), Name = "N", Created = Old, CreatedBy = Creator });
        body.Remove("IsDirty");
        body["IsDirty"] = false;

        Read<AssetTypeContract>(body).IsDirty.Should().BeFalse();
    }

    [Fact]
    public void AssetType_LongName_KeepsNinetyNineCharacters_B4()
    {
        var body = Body(new AssetTypeContract { Id = Guid.NewGuid(), Created = Old, CreatedBy = Creator });
        body["Name"] = new string('n', 150);

        Read<AssetTypeContract>(body).Name.Should().HaveLength(99);
    }

    [Fact]
    public void AssetType_NullPropertyMembers_IsAnEmptyList_B2_NullItemIsAdded_B11()
    {
        var body = Body(new AssetTypeContract { Id = Guid.NewGuid(), Created = Old, CreatedBy = Creator });
        body["PropertyMembers"] = null;
        Read<AssetTypeContract>(body).PropertyMembers.Should().BeEmpty();

        body["PropertyMembers"] = new JsonArray(null, JsonNode.Parse(Body(new AssetTypePropertyRelationContract { Created = Old }).ToJsonString()));
        Read<AssetTypeContract>(body).PropertyMembers.Should().HaveCount(2).And.Contain((AssetTypePropertyRelationContract)null!);
    }

    [Fact]
    public void Binding_MissingNestedMember_DuplicateMember_NullValueType_Fail_B9_B12_B10()
    {
        var nested = Body(new AssetTypeContract { Id = Guid.NewGuid(), Created = Old, CreatedBy = Creator, Properties = [Property()] });
        ((JsonObject)nested["Properties"]![0]!).Remove("UsesRole");
        FluentActions.Invoking(() => Read<AssetTypeContract>(nested)).Should().Throw<LegacyBodyException>();

        var duplicate = new StringBuilder(Body(new AssetTypePropertyRelationContract { Created = Old }).ToJsonString());
        duplicate.Insert(1, "\"Deleted\":null,");
        FluentActions.Invoking(() => Reader.Read<AssetTypePropertyRelationContract>(JsonDocument.Parse(duplicate.ToString()).RootElement))
            .Should().Throw<LegacyBodyException>();

        var nullDate = Body(new AssetTypeContract { Id = Guid.NewGuid(), Created = Old });
        nullDate["Created"] = null;
        FluentActions.Invoking(() => Read<AssetTypeContract>(nullDate)).Should().Throw<LegacyBodyException>();
    }

    [Fact]
    public void Property_ReadOnlyTrue_KeepsPostedLastModified_P2()
    {
        var property = Read<PropertyContract>(Body(Property(isReadOnly: true)));

        property.IsDirty.Should().BeTrue();
        property.LastModified.Should().Be(Old);
    }

    [Fact]
    public void Property_ReadOnlyFalse_StampsTheServerClock_P3()
    {
        var property = Read<PropertyContract>(Body(Property(isReadOnly: false)));

        property.IsDirty.Should().BeTrue();
        property.LastModified.Should().Be(Now);
    }

    [Fact]
    public void Property_IsDirtyTrue_KeepsPostedLastModified_Y_p3()
    {
        var body = Body(Property(isReadOnly: false));
        body["IsDirty"] = true;

        Read<PropertyContract>(body).LastModified.Should().Be(Old);
    }

    [Fact]
    public void Property_UsesRoleIgnored_PrecisionOverflowFails_P7_P9()
    {
        var body = Body(Property());
        body["UsesRole"] = true;
        Read<PropertyContract>(body).UsesRole.Should().BeFalse();

        body["Precision"] = 70000;
        FluentActions.Invoking(() => Read<PropertyContract>(body)).Should().Throw<LegacyBodyException>();
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void PickListValue_StampRules_Y_plv(bool isDirty, bool isNew, bool stamped)
    {
        var body = Body(new PickListValueContract
        {
            Id = Guid.NewGuid(), PickListId = Guid.NewGuid(), Value = "v", Created = Old, CreatedBy = Creator, LastModified = Old, IsDirty = isDirty, IsNew = isNew
        });

        var value = Read<PickListValueContract>(body);

        value.IsDirty.Should().BeTrue();
        value.LastModified.Should().Be(stamped ? Now : Old);
    }

    [Fact]
    public void PickList_Untouched_StampsListAndValues_L1()
    {
        var listId = Guid.NewGuid();
        var body = Body(new PickListContract
        {
            Id = listId, Name = "L", Description = "D", Created = Old, CreatedBy = Creator, LastModified = Old,
            PickListValues = [new PickListValueContract { Id = Guid.NewGuid(), PickListId = listId, Value = "v", Created = Old, CreatedBy = Creator, LastModified = Old }]
        });

        var list = Read<PickListContract>(body);

        list.IsDirty.Should().BeTrue();
        list.LastModified.Should().Be(Now);
        list.PickListValues.Single().LastModified.Should().Be(Now);
    }

    [Fact]
    public void PickList_NullValues_StayNull_L5()
    {
        var body = Body(new PickListContract { Id = Guid.NewGuid(), Created = Old, CreatedBy = Creator });
        body["PickListValues"] = null;

        Read<PickListContract>(body).PickListValues.Should().BeNull();
    }

    [Fact]
    public void PropertyGroup_MembersDedupByProperty_NullMemberFails_X12_X13()
    {
        var propertyId = Guid.NewGuid();
        var first = new PropertyGroupPropertyRelationContract { Id = new Guid("55555555-5555-5555-5555-555555555555"), PropertyId = propertyId, Created = Old };
        var second = new PropertyGroupPropertyRelationContract { Id = Guid.NewGuid(), PropertyId = propertyId, Created = Old };
        var body = Body(new PropertyGroupContract { Id = Guid.NewGuid(), Created = Old, CreatedBy = Creator, PropertyMembers = [first, second] });

        Read<PropertyGroupContract>(body).PropertyMembers.Should().ContainSingle().Which.Id.Should().Be(first.Id);

        body["PropertyMembers"]!.AsArray().Add(null);
        FluentActions.Invoking(() => Read<PropertyGroupContract>(body)).Should().Throw<LegacyBodyException>();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void View_StandAloneDecidesTheStamp_V1_V2(bool isStandAlone, bool stamped)
    {
        var view = Read<ViewContract>(Body(new ViewContract
        {
            Id = Guid.NewGuid(), AssetTypeId = Guid.NewGuid(), Created = Old, CreatedBy = Creator, LastModified = Old, IsStandAlone = isStandAlone
        }));

        view.IsDirty.Should().BeTrue();
        view.LastModified.Should().Be(stamped ? Now : Old);
    }

    [Fact]
    public void View_NullPropertyGroupMembers_Fails_Y_v1()
    {
        var body = Body(new ViewContract { Id = Guid.NewGuid(), Created = Old, CreatedBy = Creator });
        body["PropertyGroupMembers"] = null;

        FluentActions.Invoking(() => Read<ViewContract>(body)).Should().Throw<LegacyBodyException>();
    }
}
