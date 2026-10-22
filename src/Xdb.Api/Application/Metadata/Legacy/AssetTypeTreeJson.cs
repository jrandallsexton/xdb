using System.Collections.Generic;
using System.Text;

using Xdb.Api.Application.Metadata.Queries.AssetTypes;

namespace Xdb.Api.Application.Metadata.Legacy;

/// <summary>
/// AssetTypes_GetForTreeView's hand-built text, line for line: CRLF line endings, two spaces in the metadata id,
/// names not escaped, an "id" line on roots only.
/// </summary>
public static class AssetTypeTreeJson
{
    private const string NewLine = "\r\n";

    public static string Build(IReadOnlyList<AssetTypeTreeNode> roots)
    {
        var sb = new StringBuilder();
        Line(sb, "[");
        for (var i = 0; i < roots.Count; i++)
        {
            var root = roots[i];
            Line(sb, "{");
            Line(sb, "\"data\" : \"" + root.Name + "\",");
            Line(sb, "\"id\" : \"" + root.Id.ToString("D") + "\",");
            Line(sb, "\"children\" : [");
            Children(sb, root.Children);
            Line(sb, "],");
            Line(sb, "\"metadata\" : { \"id\" :  \"" + root.Id.ToString("D") + "\" }");
            Line(sb, i == roots.Count - 1 ? "}" : "},");
        }
        Line(sb, "]");
        return sb.ToString();
    }

    private static void Children(StringBuilder sb, IReadOnlyList<AssetTypeTreeNode> children)
    {
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            Line(sb, "{");
            Line(sb, "\"data\" : \"" + child.Name + "\",");
            Line(sb, "\"children\" : [");
            Children(sb, child.Children);
            Line(sb, "],");
            Line(sb, "\"metadata\" : { \"id\" :  \"" + child.Id.ToString("D") + "\" }");
            Line(sb, i == children.Count - 1 ? "}" : "},");
        }
    }

    private static void Line(StringBuilder sb, string text) => sb.Append(text).Append(NewLine);
}
