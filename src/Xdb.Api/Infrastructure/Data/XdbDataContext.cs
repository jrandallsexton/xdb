using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Xdb.Api.Infrastructure.Data.Entities;

namespace Xdb.Api.Infrastructure.Data;

public class XdbDataContext : DbContext
{
    public XdbDataContext(DbContextOptions<XdbDataContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AgentReport> AgentReports { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<AssetClient> AssetClients { get; set; }

    public virtual DbSet<AssetHit> AssetHits { get; set; }

    public virtual DbSet<AssetRelation> AssetRelations { get; set; }

    public virtual DbSet<AssetRelationType> AssetRelationTypes { get; set; }

    public virtual DbSet<AssetType> AssetTypes { get; set; }

    public virtual DbSet<AssetTypeRelation> AssetTypeRelations { get; set; }

    public virtual DbSet<AssetTypesProperty> AssetTypesProperties { get; set; }

    public virtual DbSet<Bug> Bugs { get; set; }

    public virtual DbSet<BulkUpload> BulkUploads { get; set; }

    public virtual DbSet<BulkUploadLog> BulkUploadLogs { get; set; }

    public virtual DbSet<BulkUploadTemplate> BulkUploadTemplates { get; set; }

    public virtual DbSet<BulkUploadTemplatesProperty> BulkUploadTemplatesProperties { get; set; }

    public virtual DbSet<Chart> Charts { get; set; }

    public virtual DbSet<ChartsFilter> ChartsFilters { get; set; }

    public virtual DbSet<ChartsProperty> ChartsProperties { get; set; }

    public virtual DbSet<ClientCache> ClientCaches { get; set; }

    public virtual DbSet<CurrencySymbol> CurrencySymbols { get; set; }

    public virtual DbSet<CurrencyValue> CurrencyValues { get; set; }

    public virtual DbSet<Dashboard> Dashboards { get; set; }

    public virtual DbSet<DashboardItem> DashboardItems { get; set; }

    public virtual DbSet<DataType> DataTypes { get; set; }

    public virtual DbSet<DatePart> DateParts { get; set; }

    public virtual DbSet<DebugLog> DebugLogs { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<Error> Errors { get; set; }

    public virtual DbSet<HelpContent> HelpContents { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<ImageType> ImageTypes { get; set; }

    public virtual DbSet<LkOperator> LkOperators { get; set; }

    public virtual DbSet<LkPermission> LkPermissions { get; set; }

    public virtual DbSet<Mapping> Mappings { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MemberBookmark> MemberBookmarks { get; set; }

    public virtual DbSet<MemberOption> MemberOptions { get; set; }

    public virtual DbSet<MemberSetting> MemberSettings { get; set; }

    public virtual DbSet<MemberView> MemberViews { get; set; }

    public virtual DbSet<MemberViewFilter> MemberViewFilters { get; set; }

    public virtual DbSet<MemberVisit> MemberVisits { get; set; }

    public virtual DbSet<MimeType> MimeTypes { get; set; }

    public virtual DbSet<Operator> Operators { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PickList> PickLists { get; set; }

    public virtual DbSet<PickListDependency> PickListDependencies { get; set; }

    public virtual DbSet<PickListValue> PickListValues { get; set; }

    public virtual DbSet<Process> Processes { get; set; }

    public virtual DbSet<ProcessStep> ProcessSteps { get; set; }

    public virtual DbSet<ProcessStepFilter> ProcessStepFilters { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<PropertyGroup> PropertyGroups { get; set; }

    public virtual DbSet<PropertyGroupsProperty> PropertyGroupsProperties { get; set; }

    public virtual DbSet<PropertyValue> PropertyValues { get; set; }

    public virtual DbSet<PropertyValueSubmittal> PropertyValueSubmittals { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionGroup> QuestionGroups { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReportCache> ReportCaches { get; set; }

    public virtual DbSet<ReportLog> ReportLogs { get; set; }

    public virtual DbSet<ReportsFilter> ReportsFilters { get; set; }

    public virtual DbSet<ReportsProperty> ReportsProperties { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleAssetType> RoleAssetTypes { get; set; }

    public virtual DbSet<RoleFilter> RoleFilters { get; set; }

    public virtual DbSet<RoleMember> RoleMembers { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<RoleReport> RoleReports { get; set; }

    public virtual DbSet<RolesAssetType> RolesAssetTypes { get; set; }

    public virtual DbSet<RolesAssetTypesFilter> RolesAssetTypesFilters { get; set; }

    public virtual DbSet<RolesAssetTypesPermission> RolesAssetTypesPermissions { get; set; }

    public virtual DbSet<RolesAssetTypesProcess> RolesAssetTypesProcesses { get; set; }

    public virtual DbSet<RolesMember> RolesMembers { get; set; }

    public virtual DbSet<RolesReport> RolesReports { get; set; }

    public virtual DbSet<RolesView> RolesViews { get; set; }

    public virtual DbSet<Search> Searches { get; set; }

    public virtual DbSet<ServiceModule> ServiceModules { get; set; }

    public virtual DbSet<ServiceModuleConfig> ServiceModuleConfigs { get; set; }

    public virtual DbSet<ServiceModuleLog> ServiceModuleLogs { get; set; }

    public virtual DbSet<ServiceModuleLogGroup> ServiceModuleLogGroups { get; set; }

    public virtual DbSet<SqlLog> SqlLogs { get; set; }

    public virtual DbSet<StoredProcActivity> StoredProcActivities { get; set; }

    public virtual DbSet<Suggestion> Suggestions { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<SystemOption> SystemOptions { get; set; }

    public virtual DbSet<SystemType> SystemTypes { get; set; }

    public virtual DbSet<Trigger> Triggers { get; set; }

    public virtual DbSet<TriggerLog> TriggerLogs { get; set; }

    public virtual DbSet<TriggerLogsMember> TriggerLogsMembers { get; set; }

    public virtual DbSet<TriggersAsset> TriggersAssets { get; set; }

    public virtual DbSet<TriggersAssetType> TriggersAssetTypes { get; set; }

    public virtual DbSet<TriggersMember> TriggersMembers { get; set; }

    public virtual DbSet<TriggersProperty> TriggersProperties { get; set; }

    public virtual DbSet<TriggersRestriction> TriggersRestrictions { get; set; }

    public virtual DbSet<TriggersRole> TriggersRoles { get; set; }

    public virtual DbSet<TriggersTargetProperty> TriggersTargetProperties { get; set; }

    public virtual DbSet<Url> Urls { get; set; }

    public virtual DbSet<View> Views { get; set; }

    public virtual DbSet<ViewClient> ViewClients { get; set; }

    public virtual DbSet<ViewFilter> ViewFilters { get; set; }

    public virtual DbSet<ViewsPropertyGroup> ViewsPropertyGroups { get; set; }

    public virtual DbSet<ViewsTrigger> ViewsTriggers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each entity configures itself in a nested EntityConfiguration class.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(XdbDataContext).Assembly);
    }
}
