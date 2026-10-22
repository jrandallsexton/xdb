using System;
using System.Text.Json;

using FluentAssertions;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Roles;
using Xdb.Api.Application.Roles.Legacy;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Roles;

public class RoleTests
{
    private static readonly Guid RoleId = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Caller = new("cccccccc-0000-0000-0000-000000000003");

    [Fact]
    public void Batch_NewMemberAndNewReport_UseTheCallerAndTheOriginalDateText()
    {
        var role = new RoleContract
        {
            Id = RoleId,
            Members = [new RoleMemberContract { Id = Id(1), MemberId = Id(2), IsNew = true }],
            Reports = [new RoleReportContract { Id = Id(3), ReportId = Id(4), Created = new DateTime(2025, 10, 21, 10, 44, 37), CreatedBy = Id(5), IsNew = true, IsDirty = true }]
        };

        RoleSaveBatch.Build(role, Caller).Should().Be(
            $"INSERT INTO [dbo].[RoleMembers] ([Id], [RoleId], [MemberId], [Created], [CreatedBy]) VALUES ('{Id(1)}', '{RoleId}', '{Id(2)}', GetDate(), '{Caller}')\r\n"
            + $"INSERT INTO [RoleReports] ([Id], [RoleId], [ReportId], [Created], [CreatedBy]) VALUES ('{Id(3)}', '{RoleId}', '{Id(4)}', '10/21/2025 10:44:37 AM', '{Id(5)}')\r\n");
    }

    [Fact]
    public void Batch_KeepsTheOriginalBugs()
    {
        // A removed report: "GetDate" without parentheses (a compile error for the whole batch).
        RoleSaveBatch.Build(new RoleContract { Id = RoleId, Reports = [new RoleReportContract { Id = Id(1), IsDeleted = true, IsDirty = true }] }, Caller)
            .Should().Contain("SET [Deleted] = GetDate, [DeletedBy]");

        // An existing dirty asset type: a two-item format with one argument.
        FluentActions.Invoking(() => RoleSaveBatch.Build(new RoleContract { Id = RoleId, AssetTypes = [new RoleAssetTypeContract { Id = Id(1), IsDirty = true }] }, Caller))
            .Should().Throw<FormatException>();

        // A new inherited asset type without InheritFrom.
        FluentActions.Invoking(() => RoleSaveBatch.Build(new RoleContract
            {
                Id = RoleId, AssetTypes = [new RoleAssetTypeContract { Id = Id(1), IsDirty = true, IsNew = true, IsInherited = true }]
            }, Caller))
            .Should().Throw<InvalidOperationException>();

        // An inherited asset type's permissions: the DELETE misses its closing parenthesis.
        RoleSaveBatch.Build(new RoleContract
            {
                Id = RoleId,
                AssetTypes = [new RoleAssetTypeContract { Id = Id(1), IsDirty = true, IsDeleted = true, IsInherited = true, Permissions = [new RolePermissionContract { Id = Id(2) }] }]
            }, Caller)
            .Should().Contain($"WHERE [Id] IN (SELECT [Id] FROM [RoleFilters] WHERE [RolePermissionsId] = '{Id(2)}'\r\n");

        // Null lists fail while building.
        FluentActions.Invoking(() => RoleSaveBatch.Build(new RoleContract { Id = RoleId, Members = null }, Caller)).Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ViewRelations_IsDirtyFollowsThePostedMemberOrder_AndRepeatedViewsKeepTheFirst()
    {
        const string date = "\\/Date(1761148800000-0400)\\/";
        var alphabetical = $$"""[{"Created":"{{date}}","CreatedBy":"{{Id(9)}}","Deleted":null,"DeletedBy":null,"Id":"{{Id(1)}}","IsAutoApprove":false,"IsDirty":false,"IsNew":false,"RoleId":"{{RoleId}}","ViewId":"{{Id(2)}}"}]""";
        var uiOrder = $$"""[{"Id":"{{Id(1)}}","RoleId":"{{RoleId}}","ViewId":"{{Id(2)}}","IsAutoApprove":false,"Created":"{{date}}","CreatedBy":"{{Id(9)}}","IsNew":true,"Deleted":null,"DeletedBy":null,"IsDirty":false},{"Id":"{{Id(3)}}","RoleId":"{{RoleId}}","ViewId":"{{Id(2)}}","IsAutoApprove":true,"Created":"{{date}}","CreatedBy":"{{Id(9)}}","IsNew":true,"Deleted":null,"DeletedBy":null,"IsDirty":true}]""";

        RoleBodies.ViewRelations(Parse(alphabetical))!.Should().ContainSingle().Which.IsDirty.Should().BeTrue();
        var ui = RoleBodies.ViewRelations(Parse(uiOrder))!;
        ui.Should().ContainSingle();
        ui[0].Id.Should().Be(Id(1));
        ui[0].IsDirty.Should().BeFalse();

        RoleBodies.ViewRelations(Parse("{}"))!.Should().BeEmpty();
        FluentActions.Invoking(() => RoleBodies.ViewRelations(Parse("[null]"))).Should().Throw<LegacyBodyException>();
        FluentActions.Invoking(() => RoleBodies.ViewRelations(Parse("\"x\""))).Should().Throw<LegacyBodyException>();
    }

    [Fact]
    public void Dates_WithAnOffsetAreLocal_WithoutOneKeepTheUtcWallTime()
    {
        RoleBodies.Date(Parse("\"\\/Date(1761148800000)\\/\"")).Should().Be(new DateTime(2025, 10, 22, 16, 0, 0, DateTimeKind.Utc));
        RoleBodies.Date(Parse("\"\\/Date(1761148800000+0000)\\/\"")).Kind.Should().Be(DateTimeKind.Local);
        FluentActions.Invoking(() => RoleBodies.Date(Parse("\"2025-10-22\""))).Should().Throw<LegacyBodyException>();
    }

    private static Guid Id(int n) => new($"{n:D8}-0000-0000-0000-000000000000");

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
