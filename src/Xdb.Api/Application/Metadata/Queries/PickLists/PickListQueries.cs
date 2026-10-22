using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Metadata.Queries.PickLists;

public sealed record GetPickListQuery(Guid PickListId);

public interface IGetPickListQueryHandler
{
    Task<Result<PickListContract?>> ExecuteAsync(GetPickListQuery query, CancellationToken cancellationToken = default);
}

/// <summary>A picklist with its live values and dependencies, deleted lists included; null when absent.</summary>
public sealed class GetPickListQueryHandler(MetadataLoader loader) : IGetPickListQueryHandler
{
    public async Task<Result<PickListContract?>> ExecuteAsync(GetPickListQuery query, CancellationToken cancellationToken = default) =>
        new Success<PickListContract?>(await loader.PickListAsync(query.PickListId, cancellationToken));
}

public sealed record GetPickListValuesQuery(Guid PickListId);

public interface IGetPickListValuesQueryHandler
{
    Task<Result<List<PickListValueContract>>> ExecuteAsync(GetPickListValuesQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetPickListValuesQueryHandler(MetadataLoader loader) : IGetPickListValuesQueryHandler
{
    public async Task<Result<List<PickListValueContract>>> ExecuteAsync(GetPickListValuesQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<PickListValueContract>>(await loader.PickListValuesAsync(query.PickListId, cancellationToken));
}

public sealed record GetPickListValueDictionaryQuery(Guid PickListId);

public interface IGetPickListValueDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPickListValueDictionaryQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Live values as (id, DisplayValue or Value) by [Order].</summary>
public sealed class GetPickListValueDictionaryQueryHandler(IMetadataProcedures procedures) : IGetPickListValueDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPickListValueDictionaryQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetPickListValueDictionaryAsync(query.PickListId, cancellationToken));
}

public sealed record GetPickListNameQuery(Guid PickListId);

public interface IGetPickListNameQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetPickListNameQuery query, CancellationToken cancellationToken = default);
}

/// <summary>The list's Name (not its display value), trimmed; "" when absent.</summary>
public sealed class GetPickListNameQueryHandler(IMetadataProcedures procedures) : IGetPickListNameQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetPickListNameQuery query, CancellationToken cancellationToken = default) =>
        new Success<string>(await procedures.GetPickListNameAsync(query.PickListId, cancellationToken));
}

public sealed record GetPickListsDictionaryQuery(bool IncludeDeleted);

public interface IGetPickListsDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPickListsDictionaryQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetPickListsDictionaryQueryHandler(IMetadataProcedures procedures) : IGetPickListsDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPickListsDictionaryQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetPickListsDictionaryAsync(query.IncludeDeleted, cancellationToken));
}

public sealed record GetPickListValueTextQuery(Guid PickListId, Guid PickListValueId);

public interface IGetPickListValueTextQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetPickListValueTextQuery query, CancellationToken cancellationToken = default);
}

/// <summary>PickListValue_Get: the value's display text (DisplayValue, else Value); "" when not a live value of that list.</summary>
public sealed class GetPickListValueTextQueryHandler(MetadataLoader loader) : IGetPickListValueTextQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetPickListValueTextQuery query, CancellationToken cancellationToken = default)
    {
        // The original loaded the whole list (values and dependencies) for this.
        if (await loader.PickListAsync(query.PickListId, cancellationToken) is { } pickList)
        {
            foreach (var value in pickList.PickListValues)
            {
                if (value.Id == query.PickListValueId)
                    return new Success<string>(string.IsNullOrEmpty(value.DisplayValue) ? value.Value : value.DisplayValue);
            }
        }
        return new Success<string>(string.Empty);
    }
}

public sealed record FindPickListValueQuery(Guid PickListId, string? Value);

public interface IFindPickListValueQueryHandler
{
    Task<Result<Guid?>> ExecuteAsync(FindPickListValueQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// PickListValue_Exists: the first live value (by [Order]) whose display text matches, compared lower-cased under
/// en-US; the request side is also trimmed, the stored side is not. A missing value parameter fails.
/// </summary>
public sealed class FindPickListValueQueryHandler(MetadataLoader loader) : IFindPickListValueQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<Guid?>> ExecuteAsync(FindPickListValueQuery query, CancellationToken cancellationToken = default)
    {
        var wanted = (query.Value ?? throw new NullReferenceException("value is required")).ToLower(EnUs).Trim();
        if (await loader.PickListAsync(query.PickListId, cancellationToken) is { } pickList)
        {
            foreach (var value in pickList.PickListValues)
            {
                var text = string.IsNullOrEmpty(value.DisplayValue) ? value.Value : value.DisplayValue;
                if (text.ToLower(EnUs) == wanted)
                    return new Success<Guid?>(value.Id);
            }
        }
        return new Success<Guid?>(null);
    }
}
