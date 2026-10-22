using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>A member as MemberDal.Get loaded it for a save (MName is never read; the save writes it as NULL).</summary>
public sealed record MemberSaveRow(Guid Id, string UserId, string LName, string FName, DateTime Created, Guid CreatedBy, Guid? DeletedBy);

public sealed record DashboardRow(Guid Id, string? Caption, int Index, IReadOnlyList<DashboardItemRow> Items);

public sealed record DashboardItemRow(Guid Id, int Index, int ItemTypeId, Guid ChartId, string? Title, string? Subtitle);

/// <summary>The original's dashboard, member-save and mapping SQL (DashboardDal, MemberDal, Mapper).</summary>
public interface IDashboardProcedures
{
    /// <summary>The member's live dashboards by caption (GetDictionary(sql): guarded, first key wins).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDashboardsAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>DashboardDal.GetDashboard: the dashboard and its live items by index; null when absent.</summary>
    Task<DashboardRow?> GetDashboardAsync(Guid dashboardId, CancellationToken cancellationToken);
    /// <summary>DashboardDal.Save: insert or update the dashboard, then each item (insert, or update its index).</summary>
    Task SaveDashboardAsync(DashboardRow dashboard, Guid memberId, CancellationToken cancellationToken);

    /// <summary>spr_Member_Get as MemberDal.Get mapped it; null when absent (deleted members load).</summary>
    Task<MemberSaveRow?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>spr_MemberId_IsValid compared with 1.</summary>
    Task<bool> IsLiveMemberAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>spr_Member_Save, called as the original called it (whatever the procedure then does).</summary>
    Task SaveMemberAsync(MemberSaveRow member, CancellationToken cancellationToken);
    /// <summary>MemberLayer.MarkAsUpdated: LastModified now, LastModifiedBy the given member.</summary>
    Task MarkMemberUpdatedAsync(Guid memberId, Guid byMemberId, CancellationToken cancellationToken);

    /// <summary>Mapper.Unmapped: mappings without a value, by key (trimmed), guarded.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUnmappedAsync(CancellationToken cancellationToken);
    /// <summary>Mapper.SetMapping (true whatever the row count).</summary>
    Task SetMappingAsync(Guid id, Guid value, CancellationToken cancellationToken);
}
