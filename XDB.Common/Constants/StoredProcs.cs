
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Enumerations;

namespace XDB.Common.Constants
{

    public static class StoredProcs
    {

        public static string Created = string.Format("[{0}.{1}].[CO_Created]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string CreatedBy = string.Format("[{0}.{1}].[CO_CreatedBy]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string LastModified = string.Format("[{0}.{1}].[CO_LastModified]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string LastModifiedBy = string.Format("[{0}.{1}].[CO_LastModifiedBy]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string DisplayValue = string.Format("[{0}.{1}].[CO_DisplayValue]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Id = string.Format("[{0}.{1}].[CO_Id]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string IsValidId = string.Format("[{0}.{1}].[CO_IsValidId]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Name = string.Format("[{0}.{1}].[CO_Name]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Description = string.Format("[{0}.{1}].[CO_Description]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Dictionary = string.Format("[{0}.{1}].[CO_Dictionary]", Config.DbSchemaPrefix, EDBSchema.Core);

        public static string spSys_Report_Exec_Stats = "spSys_Report_Execution_Stats";
        public static string spSys_StoredProcActivity = "spSys_StoredProcActivity_Save";
        public static string spSys_SqlLog_Insert = "spSys_SqlLog_Insert";
        public static string spSys_ReportLog_Save = "spSys_ReportLog_Save";

        public static string Asset_Delete = string.Format("[{0}.{1}].[Asset_Delete]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Asset_Get = string.Format("[{0}.{1}].[Asset_Get]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Asset_GetAssetTypeId = string.Format("[{0}.{1}].[Asset_GetAssetTypeId]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Asset_GetId = string.Format("[{0}.{1}].[Asset_GetId]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Asset_GetInstanceOfId = string.Format("[{0}.{1}].[Asset_Get_InstanceOfId]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Asset_Kill = "spr_Asset_Kill";
        public static string AssetId_IsValid = "spr_AssetId_IsValid";
        public static string AssetIds_Get = "spr_AssetIds_Get";
        public static string Asset_Save = string.Format("[{0}.{1}].[Asset_Save]", Config.DbSchemaPrefix, EDBSchema.Core);

        public static string spAsset_Get_Name = "spr_Asset_Get_Name";
        public static string spAssetInstances_Get = "spr_AssetInstances_Get";
        public static string spAssets_GetByInstanceOfId = "spr_Assets_GetByInstanceOfId";
        public static string spAssets_GetFromPropertyValue = "spr_Assets_GetFromPropertyValue";
        public static string spAssets_GetInPropertyValue = "spr_Assets_GetInPropertyValue";

        public static string spAsset_ChangeAssetType = "spr_Assets_ChangeAssetType";

        public static string AssetRelation_Delete = string.Format("[{0}.{1}].[AssetRelation_Delete]", Config.DbSchemaPrefix, EDBSchema.Data);

        public static string AssetType_Get = string.Format("[{0}.{1}].[AssetType_Get]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string AssetType_GetAllDescendants = "spr_AssetType_GetAllDescendants";
        public static string AssetType_GetChildDictionary = "spr_AssetType_GetChildDictionary";
        public static string AssetType_Delete = "spr_AssetType_Delete";
        public static string AssetType_HasAssets = string.Format("[{0}.{1}].[AssetType_HasAssets]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string ObjectType_Save = string.Format("[{0}.{1}].[xObjectTypeSave]", Config.DbSchemaPrefix, EDBSchema.Core);

        public static string AssetTypePropertyRelation_Get = "spr_AssetTypePropertyRelation_Get";
        public static string AssetTypePropertyRelation_Save = "spr_AssetTypePropertyRelation_Save";
        public static string AssetTypePropertyRelations_Get = "spr_AssetTypePropertyRelationList_GetByAssetTypeId";
        public static string AssetTypePropertyRelations_GetByAssetTypeIdAndPropertyIds = "spr_AssetTypePropertyRelationList_GetByAssetTypeIdAndPropertyIds";

        public static string AssetTypes_GetDictionary = "spr_AssetTypes_GetDictionary";
        public static string AssetTypes_GetParentDictionary = "spr_AssetTypes_GetParentDictionary";
        public static string AssetTypes_ByMember = "spr_AssetTypes_ByMember_Get"; // checked

        public static string Document_Delete = "spr_Document_Delete";
        public static string Document_Get = "spr_Document_Get";
        public static string Document_Save = "spr_Document_Save";

        public static string GeneratedTableNames_Get = string.Format("[{0}.{1}].[TableNamesGet]", Config.DbSchemaPrefix, EDBSchema.Gen);

        public static string Image_Delete = "spr_Image_Delete";
        public static string Image_Get = "spr_Image_Get";
        public static string Image_Save = "spr_Image_Save";

        public static string User_Get = string.Format("[{0}.{1}].[xUserGet]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Member_Save = "spr_Member_Save";
        public static string MemberDictionary = "spr_Members_GetDictionary";
        public static string MemberDictionary_InRole = "spr_Members_GetDictionary_InRole";
        public static string MemberDictionary_NotInRole = "spr_Members_GetDictionary_NotInRole";

        public static string Member_Delete = string.Format("[{0}.{1}].[Member_Delete]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string spMemberId_IsValid = "spr_MemberId_IsValid";

        public static string Picklist_Get = string.Format("[{0}.{1}].[PickList_Get]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Picklist_Save = string.Format("[{0}.{1}].[PickList_Save]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Picklist_Delete = string.Format("[{0}.{1}].[PickList_Delete]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Picklists_GetDictionary = string.Format("[{0}.{1}].[Picklists_GetDictionary]", Config.DbSchemaPrefix, EDBSchema.Core);

        public static string PicklistValue_Get = string.Format("[{0}.{1}].[PicklistValue_Get]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string PicklistValue_Save = string.Format("[{0}.{1}].[PickListValue_Save]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string PicklistValue_Delete = string.Format("[{0}.{1}].[PickListValue_Delete]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string PicklistValues_Get = string.Format("[{0}.{1}].[PicklistValues_Get]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string PicklistValues_GetDictionary = string.Format("[{0}.{1}].[PicklistValues_GetDictionary]", Config.DbSchemaPrefix, EDBSchema.Core);

        public static string PickListValues_DeleteByPickListId = string.Format("[{0}.{1}].[PickListValues_DeleteByPickListId]", Config.DbSchemaPrefix, EDBSchema.Core);

        public static string Properties_GetDictionary = string.Format("[{0}.{1}].[Properties_GetDictionary]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Properties_GetObjectDictionary = string.Format("[{0}.{1}].[Properties_GetObjectDictionary]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Property_Delete = string.Format("[{0}.{1}].[xPropertyDelete]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Property_Get = string.Format("[{0}.{1}].[xPropertyGet]", Config.DbSchemaPrefix, EDBSchema.Core);
        public static string Property_Save = string.Format("[{0}.{1}].[xPropertySave]", Config.DbSchemaPrefix, EDBSchema.Core);

        public static string PropertyValue_Save = "spr_PropertyValue_Save";
        public static string PropertyValue_Delete = "spr_PropertyValue_Delete";
        public static string PropertyValue_DeleteForAsset = "spr_PropertyValue_DeleteForAsset";
        public static string PropertyValue_Get = "spr_PropertyValue_Get";

        public static string PropertyValueSubmittal_Get = "spr_PropertyValueSubmittal_Get";
        public static string PropertyValueSubmittal_GetAssetInfo = "spr_PropertyValueSubmittal_GetAssetInfo";
        public static string PropertyValueSubmittal_GetSummary = "spr_PropertyValueSubmittal_GetSummary";
        public static string PropertyValueSubmittal_Save = "spr_PropertyValueSubmittal_Save";

        public static string spReport_Delete = "spr_Report_Delete";
        public static string spReport_Get = "spr_Report_Get";
        public static string spReport_Kill = "spr_Report_Kill";
        public static string Report_GetOrderByColumnName = "spr_Report_GetOrderByColumnName";
        public static string Report_Save = "spr_Report_Save";
        public static string Report_Cache = "spr_Report_Cache";
        public static string Reports_Get_Dictionary_For_Member = "spr_ReportList_GetByMemberId"; // checked
        public static string Reports_Get_Dictionary = "spr_Reports_GetDictionary";
        public static string spReportFilters_Get = "spr_ReportFilters_Get";

        public static string ReportProperty_Insert = "spr_ReportProperty_Insert";

        public static string Role_ContainsUser = string.Format("[{0}.{1}].[xRoleContainsUser]", Config.DbSchemaPrefix, EDBSchema.Sec);
        public static string Role_Delete = "spr_Role_Delete"; // checked
        public static string Role_Get = "spr_Role_Get";
        public static string Role_Kill = "spr_Role_Kill";
        public static string Role_Save = "spr_Role_Save";
        public static string Role_Get_MemberIds = "spr_Role_Get_MemberIds"; // checked
        public static string Role_HasPermission = "spr_Role_HasPermission"; // checked
        public static string RoleId_IsValid = "spr_Role_IsValidId"; // checked
        public static string RoleAssetType_Save = "spr_RoleAssetType_Save";
        public static string RoleFilter_Save = "RolesAssetTypesFilters";
        public static string RoleMember_Save = "spr_RoleMember_Save"; // checked
        public static string RolePermission_Save = "spr_RoleAssetTypePermission_Save";
        public static string RoleReport_Save = "spr_RoleReport_Save"; // checked
        public static string Roles_Get_By_MemberId = "spr_Roles_GetByMemberId";
        public static string Roles_Get_By_ViewId = "spr_Roles_GetDictionaryByViewId";
        public static string Roles_Get_Editable = "spr_Roles_GetEditable";
        public static string Roles_Get_Dictionary_By_MemberId = "spr_Roles_GetDictionaryByMemberId";

    }

}