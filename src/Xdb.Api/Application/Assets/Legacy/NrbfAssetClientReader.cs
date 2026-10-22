using System;
using System.Collections.Generic;
using System.Formats.Nrbf;
using System.IO;

namespace Xdb.Api.Application.Assets.Legacy;

/// <summary>
/// Reads the original's cached asset clients (BinaryFormatter / MS-NRBF blobs in AssetClients) without loading types,
/// binding by backing-field name and ignoring assembly versions (rows come from more than one build).
/// </summary>
public static class NrbfAssetClientReader
{
    public static AssetClientContract Read(byte[] data)
    {
        var root = (ClassRecord)NrbfDecoder.Decode(new MemoryStream(data));
        var client = new AssetClientContract
        {
            Id = GuidOf(Raw(root, "Id")) ?? Guid.Empty,
            Name = Raw(root, "Name") as string,
            AssetTypeId = GuidOf(Raw(root, "AssetTypeId")) ?? Guid.Empty,
            IsInstance = Raw(root, "IsInstance") is true,
            InstanceOfId = GuidOf(Raw(root, "InstanceOfId")),
            Description = Raw(root, "Description") as string,
            LastModified = Raw(root, "lastModified") as string,
            AllowEdit = Raw(root, "AllowEdit") is true,
            AllowDelete = Raw(root, "AllowDelete") is true
        };
        foreach (var value in ListItems(Raw(root, "PropertyValues")))
        {
            client.PropertyValues.Add(new PropertyValueClientContract
            {
                Id = GuidOf(Raw(value, "Id")) ?? Guid.Empty,
                PropertyId = GuidOf(Raw(value, "PropertyId")) ?? Guid.Empty,
                Value = Raw(value, "Value") as string,
                DisplayValue = Raw(value, "DisplayValue") as string,
                DisplayValueHtml = Raw(value, "DisplayValueHtml") as string,
                Order = Raw(value, "Order") is int order ? order : null
            });
        }
        return client;
    }

    private static object? Raw(ClassRecord record, string name)
    {
        var field = $"<{name}>k__BackingField";
        if (record.HasMember(field))
            return record.GetRawValue(field);
        return record.HasMember(name) ? record.GetRawValue(name) : null;
    }

    private static Guid? GuidOf(object? value) => value is ClassRecord guid
        ? new Guid((int)guid.GetRawValue("_a")!, (short)guid.GetRawValue("_b")!, (short)guid.GetRawValue("_c")!,
            (byte)guid.GetRawValue("_d")!, (byte)guid.GetRawValue("_e")!, (byte)guid.GetRawValue("_f")!, (byte)guid.GetRawValue("_g")!,
            (byte)guid.GetRawValue("_h")!, (byte)guid.GetRawValue("_i")!, (byte)guid.GetRawValue("_j")!, (byte)guid.GetRawValue("_k")!)
        : null;

    private static IEnumerable<ClassRecord> ListItems(object? value)
    {
        if (value is not ClassRecord list || !list.HasMember("_items"))
            yield break;
        var size = list.HasMember("_size") && list.GetRawValue("_size") is int s ? s : 0;
        if (list.GetRawValue("_items") is not SZArrayRecord<SerializationRecord> items)
            yield break;
        var array = items.GetArray();
        for (var i = 0; i < size && i < array.Length; i++)
        {
            if (array[i] is ClassRecord item)
                yield return item;
        }
    }
}
