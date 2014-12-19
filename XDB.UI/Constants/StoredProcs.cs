using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.UI.Constants
{
    internal static class StoredProcs
    {

        internal static string AssetHits_Get = "spr_AssetHits_Get";
        internal static string AssetHits_GetForUser = "spr_AssetHits_GetForUser";

        internal static string Bookmark_Delete = string.Format("[{0}].[Bookmark_Delete]", EDBSchema.UI);
        internal static string Bookmark_ToggleHome = string.Format("[{0}].[Bookmark_ToggleHome]", EDBSchema.UI);

        internal static string CHART_GET = "spr_Chart_Get";
        internal static string CHART_SAVE = "spr_Chart_Save";

        internal static string CHART_DATA_GET = "spr_ChartData_Get";
        internal static string CHART_DATA_GET_ASSET = "spr_ChartData_Get_Asset";
        internal static string CHART_DATA_GET_ASSET_FILTERED = "spr_ChartData_Get_Asset_Filtered";
        internal static string CHART_DATA_GET_ASSET_INHERITED_VALUE = "spr_ChartData_Get_AssetInherited";
        internal static string CHART_DATA_GET_ASSET_INHERITED_VALUE_FILTERED = "spr_ChartData_Get_AssetInherited_Filtered";
        internal static string CHART_DATA_GET_INSTANCE_OF = "spr_ChartData_Get_InstanceOf";
        internal static string CHART_DATA_GET_INSTANCE_OF_FILTERED = "spr_ChartData_Get_InstanceOf_Filtered";
        internal static string CHART_DATA_GET_PROPVAL = "spr_ChartData_Get_PropVal";
        internal static string CHART_DATA_GET_PROPVAL_FILTERED = "spr_ChartData_Get_PropVal_Filtered";

        internal static string MEMBERVIEW_GET = "spr_MemberView_Get";
        internal static string MEMBERVIEW_SAVE = "spr_MemberView_Save";

        internal static string ROLE_PROCESS_RELATION_DELETE = "spr_RoleProcessRelation_Delete";
        internal static string ROLE_PROCESS_RELATION_SAVE = "spr_RoleProcessRelation_Save";

        internal static string ROLE_VIEW_RELATION_DELETE = "spr_RoleViewRelation_Delete";
        internal static string ROLE_VIEW_RELATION_GET = "spr_RoleViewRelation_Get";
        internal static string ROLE_VIEW_RELATION_SAVE = "spr_RoleViewRelation_Save";
        //internal static string ROLE_VIEW_RELATION_LIST_DELETE = "spr_RoleViewRelationList_Delete";
        internal static string ROLE_VIEW_RELATION_LIST_GET = "spr_RoleViewRelationList_Get";
        //internal static string ROLE_VIEW_RELATION_LIST_SAVE = "spr_RoleViewRelationList_Save";

        internal static string PROCESSES_GET_BY_ASSETTYPES = "spr_Processes_GetBy_AssetTypeIds";

        internal static string VIEW_DELETE = "spr_View_Delete";
        internal static string VIEW_GET = "spr_View_Get";
        internal static string VIEW_KILL = "spr_View_Kill";
        internal static string VIEW_SAVE = "spr_View_Save";

        internal static string VIEW_HAS_PERMISSION = "spr_View_Has_Permission";

        internal static string spViewPropertyGroupRelation_Get = "spr_ViewPropertyGroupRelation_Get";
        internal static string spViewPropertyGroupRelation_Save = "spr_ViewPropertyGroupRelation_Save";
        internal static string spViewPropertyGroupRelation_Delete = "spr_ViewPropertyGroupRelation_Delete";

        ////Collection SPs
        internal static string spViewPropertyGroupRelationList_Get = "spr_ViewPropertyGroupRelationList_Get";
        //internal static string spViewPropertyGroupRelationList_Save = "spr_ViewPropertyGroupRelationList_Save";
        //internal static string spViewPropertyGroupRelationList_Delete = "spr_ViewPropertyGroupRelationList_Delete";

        ////Collection by FK SPs
        internal static string spViewPropertyGroupRelationList_GetByViewId = "spr_ViewPropertyGroupRelationList_GetByViewId";
        internal static string spViewPropertyGroupRelationList_DeleteByViewId = "spr_ViewPropertyGroupRelationList_DeleteByViewId";

        ////Collection by FK SPs
        internal static string spViewPropertyGroupRelationList_GetByPropertyGroupId = "spr_ViewPropertyGroupRelationList_GetByPropertyGroupId";
        internal static string spViewPropertyGroupRelationList_DeleteByPropertyGroupId = "spr_ViewPropertyGroupRelationList_DeleteByPropertyGroupId";

    }
}
