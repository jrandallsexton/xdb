using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Xdb.Api.Infrastructure.Data;

/// <summary>
/// Inserts entities with SqlBulkCopy, inside the context's current transaction: one round trip per table instead of one
/// INSERT per row. Table and column names come from the EF model. Constraints are checked, so foreign keys stay trusted
/// (a bulk copy without the check would mark them untrusted); the schema has no triggers.
/// </summary>
public static class EntityBulkCopy
{
    public static async Task WriteAsync<T>(XdbDataContext db, IReadOnlyCollection<T> rows, CancellationToken cancellationToken) where T : class
    {
        if (rows.Count == 0)
            return;

        var entityType = db.Model.FindEntityType(typeof(T)) ?? throw new InvalidOperationException($"{typeof(T).Name} is not an entity.");
        var tableName = entityType.GetTableName() ?? throw new InvalidOperationException($"{typeof(T).Name} is not mapped to a table.");
        var table = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
        var columns = entityType.GetProperties()
            .Where(p => p.PropertyInfo is not null && p.GetComputedColumnSql(table) is null)
            .Select(p => (Property: p, Name: p.GetColumnName(table)!, Converter: p.GetTypeMapping().Converter))
            .Where(c => c.Name is not null)
            .ToList();

        using var data = new DataTable();
        foreach (var (property, name, converter) in columns)
        {
            var type = converter?.ProviderClrType ?? property.ClrType;
            data.Columns.Add(name, Nullable.GetUnderlyingType(type) ?? type);
        }
        foreach (var row in rows)
        {
            var values = new object[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                var value = columns[i].Property.PropertyInfo!.GetValue(row);
                if (value is not null && columns[i].Converter is { } converter)
                    value = converter.ConvertToProvider(value);
                values[i] = value ?? DBNull.Value;
            }
            data.Rows.Add(values);
        }

        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = (SqlConnection)db.Database.GetDbConnection();
            var transaction = (SqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
            using var copy = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints, transaction)
            {
                DestinationTableName = $"[{table.Schema ?? "dbo"}].[{table.Name}]",
                BulkCopyTimeout = 0
            };
            foreach (var (_, name, _) in columns)
                copy.ColumnMappings.Add(name, name);
            await copy.WriteToServerAsync(data, cancellationToken);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
