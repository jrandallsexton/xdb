using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;

using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Core.Common;

namespace Xdb.Api.Application.PickLists.Commands.CreatePickList;

public interface ICreatePickListCommandHandler
{
    /// <summary>The new list's id (Created); Conflict when the list id or a value id is taken; Validation for a bad command.</summary>
    Task<Result<Guid>> ExecuteAsync(CreatePickListCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Replaces the create half of the legacy PickList_Save (PickListSaver with spr_PickList_Save and
/// spr_PickListValue_Save, plain upserts with no side effects). Unlike the original, the list and its values are
/// written together, names and values must be unique, and values are ordered by position. Everything is created
/// approved by its creator, as every legacy list and value was.
/// </summary>
public sealed class CreatePickListCommandHandler(
    XdbDataContext db,
    IValidator<CreatePickListCommand> validator,
    IDateTimeProvider clock) : ICreatePickListCommandHandler
{
    public async Task<Result<Guid>> ExecuteAsync(CreatePickListCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return new Failure<Guid>(Guid.Empty, ResultStatus.Validation, validation.Errors);

        var errors = new List<ValidationFailure>();
        if (!await db.Members.AsNoTracking().AnyAsync(m => m.Id == command.ActorId && m.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreatePickListCommand.ActorId), "The actor is not a live member."));
        // Case-insensitive: the column's collation decides the comparison.
        if (await db.PickLists.AsNoTracking().AnyAsync(l => l.Name == command.Name && l.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreatePickListCommand.Name), $"A pick list named '{command.Name}' already exists."));
        if (errors.Count > 0)
            return new Failure<Guid>(Guid.Empty, ResultStatus.Validation, errors);

        var id = command.Id ?? Guid.NewGuid();
        if (await db.PickLists.AsNoTracking().AnyAsync(l => l.Id == id, cancellationToken))
            return Conflict(nameof(CreatePickListCommand.Id), $"Pick list {id} already exists.");

        var valueIds = command.Values.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToList();
        if (valueIds.Count > 0 && await db.PickListValues.AsNoTracking().AnyAsync(v => valueIds.Contains(v.Id), cancellationToken))
            return Conflict(nameof(CreatePickListCommand.Values), "A pick list value id already exists.");

        var now = clock.UtcNow();
        var pickList = new PickList
        {
            Id = id,
            Name = command.Name,
            DisplayValue = command.DisplayValue,
            Description = command.Description,
            AllowNewValues = command.AllowNewValues,
            IsMemberList = false,
            Created = now,
            CreatedBy = command.ActorId,
            Approved = now,
            ApprovedBy = command.ActorId
        };
        for (var order = 0; order < command.Values.Count; order++)
        {
            var value = command.Values[order];
            pickList.PickListValues.Add(new PickListValue
            {
                Id = value.Id ?? Guid.NewGuid(),
                PickListId = id,
                Value = value.Value,
                DisplayValue = value.DisplayValue,
                Order = order,
                Created = now,
                CreatedBy = command.ActorId,
                Approved = now,
                ApprovedBy = command.ActorId
            });
        }

        db.PickLists.Add(pickList);
        await db.SaveChangesAsync(cancellationToken);

        return new Success<Guid>(id, ResultStatus.Created);
    }

    private static Failure<Guid> Conflict(string property, string message) =>
        new(Guid.Empty, ResultStatus.Conflict, [new ValidationFailure(property, message)]);
}
