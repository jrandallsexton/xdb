using System;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.Common.Identity;

/// <summary>
/// The original's LogicalException where the operation caught it (HandleLogicalException): the message is saved as an
/// Errors row and the response is 500 with its id in an ErrorId header. Elsewhere it is an ordinary fault.
/// </summary>
public sealed class LegacyLogicalException(string message) : Exception(message);

/// <summary>ErrorDal.Error_Save for HandleLogicalException: a new lowercase id, the message, the caller's member.</summary>
public sealed class LegacyErrorLog(IRoleProcedures roles, CurrentMember currentMember)
{
    public async Task<string> SaveAsync(string message, CancellationToken cancellationToken)
    {
        var errorId = Guid.NewGuid().ToString("D");
        await roles.SaveErrorAsync(errorId, message, await currentMember.GetIdAsync(cancellationToken), cancellationToken);
        return errorId;
    }
}
