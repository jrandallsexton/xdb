using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Microsoft.EntityFrameworkCore;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>
/// Shared plumbing for gateways that reproduce the original data layer (SqlBaseDal): its timeouts, its scalar
/// helpers' semantics and its dictionary readers. SQL text is built line by line with CRLF, as AppendLine did.
/// </summary>
public abstract class LegacyGatewayBase(XdbDataContext db)
{
    /// <summary>The original's timeouts: 3000 s for procedures, 30000 s for inline readers.</summary>
    protected const int ProcedureTimeout = 3000;
    protected const int InlineTimeout = 30000;

    protected const string NewLine = "\r\n";

    protected static readonly Guid NullStringKey = Guid.Empty;
    protected static readonly Guid EmptyStringKey = new("99999999-9999-9999-9999-999999999999");

    protected static string Lines(params string[] lines) => string.Join(NewLine, lines) + NewLine;

    protected static void Line(StringBuilder sb, string text) => sb.Append(text).Append(NewLine);

    protected async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);
        return connection;
    }

    protected static CommandDefinition Procedure(string name, object? parameters, CancellationToken cancellationToken) =>
        new(name, parameters, commandType: CommandType.StoredProcedure, commandTimeout: ProcedureTimeout, cancellationToken: cancellationToken);

    protected static CommandDefinition Inline(string sql, object? parameters, CancellationToken cancellationToken) =>
        new(sql, parameters, commandType: CommandType.Text, commandTimeout: InlineTimeout, cancellationToken: cancellationToken);

    /// <summary>ExecuteScalarStringInLine: trimmed; "" for no row or NULL.</summary>
    protected async Task<string> ScalarStringAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Inline(sql, parameters, cancellationToken));
        return value is null or DBNull ? string.Empty : value.ToString()!.Trim();
    }

    /// <summary>ExecuteScalarGuidInLine: Guid.Empty for no row or NULL.</summary>
    protected async Task<Guid> ScalarGuidAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Inline(sql, parameters, cancellationToken));
        var text = value is null or DBNull ? string.Empty : value.ToString()!;
        return string.IsNullOrEmpty(text) ? Guid.Empty : new Guid(text.Trim());
    }

    /// <summary>ExecuteScalarInLine: -1 for NULL; no row throws (the original dereferenced null).</summary>
    protected async Task<int> ScalarIntAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Inline(sql, parameters, cancellationToken))
            ?? throw new NullReferenceException("Scalar query returned no row.");
        return int.TryParse(value.ToString(), out var result) ? result : -1;
    }

    /// <summary>A COUNT(*) procedure compared with 1, as the original's IsValidId-style helpers did.</summary>
    protected async Task<bool> CountIsOneAsync(CommandDefinition command) =>
        await (await OpenAsync(command.CancellationToken)).ExecuteScalarAsync<int>(command) == 1;

    /// <summary>
    /// The original's guarded reader (<c>if (reader == null || !reader.HasRows) return</c>): rows mapped as Dapper
    /// would, but nothing when the first result set has no rows. A runtime error the server raises after the column
    /// metadata (e.g. SQL 8169 from a plan-dependent conversion) leaves HasRows false and is discarded when the reader
    /// closes, so the call returns empty instead of failing, as in the original (measured on both SqlClients).
    /// </summary>
    protected async Task<List<T>> QueryGuardedAsync<T>(CommandDefinition command)
    {
        var rows = new List<T>();
        await using var reader = await (await OpenAsync(command.CancellationToken)).ExecuteReaderAsync(command);
        if (!reader.HasRows)
            return rows;
        var parse = reader.GetRowParser<T>();
        while (await reader.ReadAsync(command.CancellationToken))
            rows.Add(parse(reader));
        return rows;
    }

    /// <summary>Reads like the original's guarded GetDictionary-style readers: nothing when the first result set has no rows (see <see cref="QueryGuardedAsync{T}"/>).</summary>
    /// <param name="skipNulls">Skip rows where either column is NULL; otherwise a NULL value fails as GetString did.</param>
    /// <param name="skipNullKeysOnly">Skip rows with a NULL key only (a NULL value still fails).</param>
    /// <param name="unique">Fail on a repeated key, as Dictionary.Add did.</param>
    protected async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GuidPairsAsync(CommandDefinition command, bool skipNulls,
        CancellationToken cancellationToken, bool skipNullKeysOnly = false, bool unique = false)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(command);
        if (!reader.HasRows)
            return result;
        while (await reader.ReadAsync(cancellationToken))
        {
            if ((skipNulls || skipNullKeysOnly) && reader.IsDBNull(0))
                continue;
            if (skipNulls && reader.IsDBNull(1))
                continue;
            var key = reader.GetGuid(0);
            if (unique && !seen.Add(key))
                throw new ArgumentException($"An item with the same key has already been added: {key:D}");
            result.Add(new KeyValuePair<Guid, string>(key, reader.GetString(1)));
        }
        return result;
    }

    /// <summary>
    /// SqlBaseDal.GetDictionary: a NULL key becomes the empty Guid with "NULL"; a key that is not a Guid becomes
    /// 99999999-... with its value (or "[ Empty String ]" when NULL). <paramref name="firstWins"/> is the inline
    /// overload (repeats skipped); otherwise a repeated key fails (the procedure overload's unguarded Add).
    /// </summary>
    protected async Task<IReadOnlyList<KeyValuePair<Guid, string>>> LegacyDictionaryAsync(CommandDefinition command, bool firstWins, CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(command);
        if (!reader.HasRows)
            return result;
        while (await reader.ReadAsync(cancellationToken))
        {
            Guid key;
            Func<string> value;
            if (reader.IsDBNull(0))
            {
                key = NullStringKey;
                value = () => "NULL";
            }
            else if (Guid.TryParse(reader.GetValue(0).ToString(), out key))
            {
                value = () => reader.GetString(1);
            }
            else
            {
                key = EmptyStringKey;
                value = () => reader.IsDBNull(1) ? "[ Empty String ]" : reader.GetString(1);
            }

            if (!seen.Add(key))
            {
                if (firstWins)
                    continue;
                throw new ArgumentException($"An item with the same key has already been added: {key:D}");
            }
            result.Add(new KeyValuePair<Guid, string>(key, value()));
        }
        return result;
    }

    protected static DbString AnsiList(IEnumerable<Guid> ids) =>
        new() { Value = string.Join(",", ids.Select(id => id.ToString("D"))), IsAnsi = true, Length = -1 };
}
