using System;
using System.Collections.Generic;

using FluentAssertions;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.GenTables;
using Xdb.Api.Infrastructure.Data.Procedures;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.GeneratedObjects;

public class GenPropertySetTests
{
    private readonly List<GenPropertyNameRow> _rows = [];
    private readonly Dictionary<Guid, PropertyMetaRow> _properties = [];

    private Guid Row(string name, SystemType? systemType = null)
    {
        var id = Guid.NewGuid();
        _rows.Add(new GenPropertyNameRow(id, name));
        _properties[id] = new PropertyMetaRow { Id = id, Name = name, IsSystem = systemType is not null, SystemTypeId = (int?)systemType };
        return id;
    }

    [Fact]
    public void SystemLabels_RenameTheirProperties_ThenAllAreSortedByName()
    {
        var name = Row("Asset Name", SystemType.AssetName);
        var instanceOf = Row("Instance Of", SystemType.InstanceOf);
        var description = Row("Instance of Description", SystemType.InstanceOfDescription);
        var zone = Row("Zone");
        var apple = Row("apple");

        var order = GenPropertySet.Order(_rows, _properties, assetNameLabel: "Server", instanceOfLabel: "Model");

        order.Should().Equal(apple, instanceOf, description, name, zone); // apple, Model, Model Description, Server, Zone
    }

    [Fact]
    public void DuplicateNames_KeepTheFirstRowMet()
    {
        Row("Asset Name", SystemType.AssetName);
        var first = Row("Created");
        Row("Created");

        GenPropertySet.Order(_rows, _properties, "", "").Should().ContainSingle(id => _properties[id].Name == "Created").Which.Should().Be(first);
    }

    [Fact]
    public void EmptyLabels_LeaveNamesUnchanged()
    {
        var name = Row("Asset Name", SystemType.AssetName);
        var b = Row("B");

        GenPropertySet.Order(_rows, _properties, "", "").Should().Equal(name, b);
    }

    [Fact]
    public void WithoutTheThreeSystemProperties_TheQueryOrderIsKept()
    {
        var z = Row("Z");
        var a = Row("A");
        Row("A");

        GenPropertySet.Order(_rows, _properties, "x", "y").Should().Equal(z, a, _rows[2].Id);
    }

    [Fact]
    public void Sorting_IsCaseSensitiveCultureOrder()
    {
        Row("Asset Name", SystemType.AssetName);
        var upper = Row("Beta");
        var lower = Row("beta");

        var order = GenPropertySet.Order(_rows, _properties, "", "");

        order.Should().ContainInOrder(lower, upper);
    }
}
