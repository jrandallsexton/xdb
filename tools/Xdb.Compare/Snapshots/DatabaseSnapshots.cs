using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

namespace Xdb.Compare.Snapshots;

/// <summary>
/// SQL Server database snapshots, so the original and the port can be returned to the same data before every run.
/// Each database gets one snapshot named "{database}_xdbsnap".
/// </summary>
public sealed class DatabaseSnapshots(string server, string? snapshotDirectory)
{
    private const string Suffix = "_xdbsnap";

    public async Task CreateAsync(IEnumerable<string> databases, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var directory = snapshotDirectory ?? await ScalarAsync<string>(connection, "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(400))", cancellationToken);

        foreach (var database in databases)
        {
            await DropAsync(connection, database, cancellationToken);

            var files = new List<string>();
            await using (var command = new SqlCommand("SELECT name FROM sys.master_files WHERE database_id = DB_ID(@db) AND type = 0", connection))
            {
                command.Parameters.AddWithValue("@db", database);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    files.Add(reader.GetString(0));
            }
            if (files.Count == 0)
                throw new InvalidOperationException($"Database not found: {database}");

            var fileSpecs = files.Select(f =>
                $"(NAME = {Quote(f)}, FILENAME = N'{Path.Combine(directory, $"{database}{Suffix}_{f}.ss").Replace("'", "''")}')");
            await ExecuteAsync(connection, $"CREATE DATABASE {Quote(database + Suffix)} ON {string.Join(", ", fileSpecs)} AS SNAPSHOT OF {Quote(database)};", cancellationToken);
            Console.WriteLine($"snapshot created: {database}{Suffix}");
        }
    }

    public async Task RevertAsync(IEnumerable<string> databases, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        foreach (var database in databases)
        {
            var snapshot = database + Suffix;
            await ExecuteAsync(connection, $"ALTER DATABASE {Quote(database)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;", cancellationToken);
            try
            {
                await ExecuteAsync(connection, $"RESTORE DATABASE {Quote(database)} FROM DATABASE_SNAPSHOT = N'{snapshot.Replace("'", "''")}';", cancellationToken);
            }
            finally
            {
                await ExecuteAsync(connection, $"ALTER DATABASE {Quote(database)} SET MULTI_USER;", cancellationToken);
            }
            Console.WriteLine($"reverted: {database}");
        }
    }

    public async Task DropAsync(IEnumerable<string> databases, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        foreach (var database in databases)
            await DropAsync(connection, database, cancellationToken);
    }

    private static async Task DropAsync(SqlConnection connection, string database, CancellationToken cancellationToken)
    {
        var snapshot = database + Suffix;
        await ExecuteAsync(connection, $"IF DB_ID(N'{snapshot.Replace("'", "''")}') IS NOT NULL DROP DATABASE {Quote(snapshot)};", cancellationToken);
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            CommandTimeout = 600
        };
        var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 600 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<T> ScalarAsync<T>(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static string Quote(string name) => "[" + name.Replace("]", "]]") + "]";
}
