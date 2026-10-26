using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Activity;
using Xdb.Api.Application.Admin;
using Xdb.Api.Application.Assets;
using Xdb.Api.Application.BulkUploads;
using Xdb.Api.Application.Assets.Queries;
using Xdb.Api.Application.Assets.Writes;
using Xdb.Api.Application.Charts.Commands;
using Xdb.Api.Application.Charts.Queries;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Files;
using Xdb.Api.Application.Metadata.Commands;
using Xdb.Api.Application.Reports;
using Xdb.Api.Application.Reports.Commands;
using Xdb.Api.Application.Reports.Export;
using Xdb.Api.Application.Reports.Queries;
using Xdb.Api.Application.Roles.Commands;
using Xdb.Api.Application.Views.Commands;
using Xdb.Api.Application.Roles.Queries;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Api.Application.GeneratedObjects.GenTables;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Metadata.Queries.AssetTypes;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Application.Metadata.Queries.PickLists;
using Xdb.Api.Application.Metadata.Queries.Properties;
using Xdb.Api.Application.Metadata.Queries.Values;
using Xdb.Api.Application.Assets.Commands.CreateAsset;
using Xdb.Api.Application.Assets.Commands.CreateAssets;
using Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;
using Xdb.Api.Application.PickLists.Commands.CreatePickList;
using Xdb.Api.Application.Properties.Commands.CreateProperty;
using Xdb.Api.Application.PropertyGroups.Commands.CreatePropertyGroup;
using Xdb.Api.Application.Views.Commands.CreateView;
using Xdb.Api.Application.Reports.Queries.GetReportClient;
using Xdb.Api.Application.Reports.Queries.GetReportData;
using Xdb.Api.Application.Reports.Queries.GetReportExecutionStats;
using Xdb.Api.Application.Setup.Commands.InitializeDatabase;
using Xdb.Api.Application.Users.Commands;
using Xdb.Api.Application.Users.Queries;
using Xdb.Api.Application.Views;
using Xdb.Api.Application.Views.Queries;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.DependencyInjection;

public static class ServiceRegistration
{
    /// <summary>
    /// Handlers are registered explicitly, one line each, grouped by area.
    /// Validators are discovered by assembly scan.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddSingleton<BackgroundWorkQueue>();
        services.AddSingleton<IBackgroundWorkQueue>(sp => sp.GetRequiredService<BackgroundWorkQueue>());
        services.AddHostedService<BackgroundWorkService>();

        services.AddScoped<IReportProcedures, ReportProcedures>();
        services.AddScoped<IGeneratedObjectProcedures, GeneratedObjectProcedures>();
        services.AddScoped<IMetadataProcedures, MetadataProcedures>();
        services.AddScoped<IViewProcedures, ViewProcedures>();
        services.AddScoped<IAssetProcedures, AssetProcedures>();
        services.AddScoped<IFilterProcedures, FilterProcedures>();
        services.AddScoped<IUiProcedures, UiProcedures>();
        services.AddScoped<IChartProcedures, ChartProcedures>();
        services.AddScoped<IDashboardProcedures, DashboardProcedures>();
        services.AddScoped<IRoleProcedures, RoleProcedures>();
        services.AddScoped<IReportStore, ReportStore>();

        services.AddOptions<GeneratedObjectsOptions>().BindConfiguration(GeneratedObjectsOptions.SectionName);
        services.AddOptions<FirstAdminOptions>().BindConfiguration(FirstAdminOptions.SectionName);
        services.AddOptions<LegacyEnvironmentOptions>().BindConfiguration(LegacyEnvironmentOptions.SectionName);

        // Metadata (asset types, properties, picklists, lookups)
        services.AddScoped<MetadataRules>();
        services.AddScoped<MetadataLoader>();
        services.AddScoped<IGetCachedPropertiesQueryHandler, GetCachedPropertiesQueryHandler>();
        services.AddScoped<IGetPotentialValuesQueryHandler, GetPotentialValuesQueryHandler>();
        services.AddScoped<IGetDictionaryValuesQueryHandler, GetDictionaryValuesQueryHandler>();
        services.AddScoped<IGetAssetTypeQueryHandler, GetAssetTypeQueryHandler>();
        services.AddScoped<IGetAssetTypeParentQueryHandler, GetAssetTypeParentQueryHandler>();
        services.AddScoped<IGetAssetTypeParentIdQueryHandler, GetAssetTypeParentIdQueryHandler>();
        services.AddScoped<IGetAssetTypeNameQueryHandler, GetAssetTypeNameQueryHandler>();
        services.AddScoped<IGetAssetTypeReportingLabelQueryHandler, GetAssetTypeReportingLabelQueryHandler>();
        services.AddScoped<IAssetTypeHasAssetsQueryHandler, AssetTypeHasAssetsQueryHandler>();
        services.AddScoped<IGetAssetTypeDefaultViewIdQueryHandler, GetAssetTypeDefaultViewIdQueryHandler>();
        services.AddScoped<IGetAssetTypeDescendantsQueryHandler, GetAssetTypeDescendantsQueryHandler>();
        services.AddScoped<IGetAssetTypesAvailableForCreationQueryHandler, GetAssetTypesAvailableForCreationQueryHandler>();
        services.AddScoped<IGetAssetTypeByProcessStepQueryHandler, GetAssetTypeByProcessStepQueryHandler>();
        services.AddScoped<IGetAssetTypesByProcessStepQueryHandler, GetAssetTypesByProcessStepQueryHandler>();
        services.AddScoped<IGetAssetTypesDictionaryQueryHandler, GetAssetTypesDictionaryQueryHandler>();
        services.AddScoped<IGetRootAssetTypesQueryHandler, GetRootAssetTypesQueryHandler>();
        services.AddScoped<IGetAssetTypeTreeQueryHandler, GetAssetTypeTreeQueryHandler>();
        services.AddScoped<IGetDefaultViewsQueryHandler, GetDefaultViewsQueryHandler>();
        services.AddScoped<IGetAssetTypeIdOfAssetQueryHandler, GetAssetTypeIdOfAssetQueryHandler>();
        services.AddScoped<IGetAssetTypePropertiesQueryHandler, GetAssetTypePropertiesQueryHandler>();
        services.AddScoped<IGetPropertiesForCustomFieldQueryHandler, GetPropertiesForCustomFieldQueryHandler>();
        services.AddScoped<IGetNonFreeformPropertiesQueryHandler, GetNonFreeformPropertiesQueryHandler>();
        services.AddScoped<IGetPickListPropertiesQueryHandler, GetPickListPropertiesQueryHandler>();
        services.AddScoped<IGetPropertiesDictionaryQueryHandler, GetPropertiesDictionaryQueryHandler>();
        services.AddScoped<IGetPropertyQueryHandler, GetPropertyQueryHandler>();
        services.AddScoped<IGetPropertyNameQueryHandler, GetPropertyNameQueryHandler>();
        services.AddScoped<IGetViewPropertyIdsQueryHandler, GetViewPropertyIdsQueryHandler>();
        services.AddScoped<IGetPickListQueryHandler, GetPickListQueryHandler>();
        services.AddScoped<IGetPickListValuesQueryHandler, GetPickListValuesQueryHandler>();
        services.AddScoped<IGetPickListValueDictionaryQueryHandler, GetPickListValueDictionaryQueryHandler>();
        services.AddScoped<IGetPickListNameQueryHandler, GetPickListNameQueryHandler>();
        services.AddScoped<IGetPickListsDictionaryQueryHandler, GetPickListsDictionaryQueryHandler>();
        services.AddScoped<IGetPickListValueTextQueryHandler, GetPickListValueTextQueryHandler>();
        services.AddScoped<IFindPickListValueQueryHandler, FindPickListValueQueryHandler>();
        services.AddScoped<IGetPropertyDataTypeQueryHandler, GetPropertyDataTypeQueryHandler>();
        services.AddScoped<IGetOperatorsQueryHandler, GetOperatorsQueryHandler>();
        services.AddScoped<IGetPermissionsQueryHandler, GetPermissionsQueryHandler>();
        services.AddScoped<IGetCurrencySymbolsQueryHandler, GetCurrencySymbolsQueryHandler>();
        services.AddScoped<IGetCurrencyValueQueryHandler, GetCurrencyValueQueryHandler>();
        services.AddScoped<IGetSystemImageDictionaryQueryHandler, GetSystemImageDictionaryQueryHandler>();
        services.AddScoped<IGetSystemImagesQueryHandler, GetSystemImagesQueryHandler>();
        services.AddScoped<IGenerateGuidQueryHandler, GenerateGuidQueryHandler>();

        // Views, property groups, processes, help
        services.AddScoped<ViewLoader>();
        services.AddScoped<IGetProcessQueryHandler, GetProcessQueryHandler>();
        services.AddScoped<IGetProcessStepLabelQueryHandler, GetProcessStepLabelQueryHandler>();
        services.AddScoped<IGetProcessStepFlagQueryHandler, GetProcessStepFlagQueryHandler>();
        services.AddScoped<IGetProcessStepFiltersQueryHandler, GetProcessStepFiltersQueryHandler>();
        services.AddScoped<IGetFilterValuesQueryHandler, GetFilterValuesQueryHandler>();
        services.AddScoped<IGetNextFilterValuesQueryHandler, GetNextFilterValuesQueryHandler>();
        services.AddScoped<IGetProcessStepViewQueryHandler, GetProcessStepViewQueryHandler>();
        services.AddScoped<IGetHelpSectionQueryHandler, GetHelpSectionQueryHandler>();
        services.AddScoped<IGetHelpSectionsQueryHandler, GetHelpSectionsQueryHandler>();
        services.AddScoped<IGetPropertyGroupQueryHandler, GetPropertyGroupQueryHandler>();
        services.AddScoped<IGetPropertyGroupViewsQueryHandler, GetPropertyGroupViewsQueryHandler>();
        services.AddScoped<IGetViewsDictionaryQueryHandler, GetViewsDictionaryQueryHandler>();
        services.AddScoped<IGetMemberFiltersQueryHandler, GetMemberFiltersQueryHandler>();
        services.AddScoped<IGetUserViewsQueryHandler, GetUserViewsQueryHandler>();
        services.AddScoped<IGetUserViewGroupsQueryHandler, GetUserViewGroupsQueryHandler>();
        services.AddScoped<IGetViewRolesQueryHandler, GetViewRolesQueryHandler>();
        services.AddScoped<IGetMemberRolesQueryHandler, GetMemberRolesQueryHandler>();
        services.AddScoped<IGetPropertyGroupsQueryHandler, GetPropertyGroupsQueryHandler>();
        services.AddScoped<IGetPropertyGroupsForPropertyQueryHandler, GetPropertyGroupsForPropertyQueryHandler>();
        services.AddScoped<IGetAssetTypePropertyGroupsQueryHandler, GetAssetTypePropertyGroupsQueryHandler>();
        services.AddScoped<IGetProcessesQueryHandler, GetProcessesQueryHandler>();
        services.AddScoped<IGetAssetTypeProcessesQueryHandler, GetAssetTypeProcessesQueryHandler>();
        services.AddScoped<IGetHelpSectionsDictionaryQueryHandler, GetHelpSectionsDictionaryQueryHandler>();
        services.AddScoped<IGetViewClientQueryHandler, GetViewClientQueryHandler>();
        services.AddScoped<IViewClientModifiedQueryHandler, ViewClientModifiedQueryHandler>();
        services.AddScoped<IGetViewQueryHandler, GetViewQueryHandler>();
        services.AddScoped<IGetViewsByAssetTypeQueryHandler, GetViewsByAssetTypeQueryHandler>();
        services.AddScoped<IGetViewFilterDisplayQueryHandler, GetViewFilterDisplayQueryHandler>();
        services.AddScoped<IGetSearchFilterDisplayQueryHandler, GetSearchFilterDisplayQueryHandler>();
        services.AddScoped<IViewMatchesAssetTypeQueryHandler, ViewMatchesAssetTypeQueryHandler>();
        services.AddScoped<IGetRoleViewsQueryHandler, GetRoleViewsQueryHandler>();

        // Assets
        services.AddScoped<PropertyValueLoader>();
        services.AddScoped<AssetDictionaries>();
        services.AddScoped<FilterEngine>();
        services.AddScoped<RelatedItems>();
        services.AddScoped<IGetRestrictedValuesQueryHandler, GetRestrictedValuesQueryHandler>();
        services.AddScoped<IGetPotentialParentsQueryHandler, GetPotentialParentsQueryHandler>();
        services.AddScoped<IGetRelatedItemsHtmlQueryHandler, GetRelatedItemsHtmlQueryHandler>();
        services.AddScoped<IGetRelationGraphQueryHandler, GetRelationGraphQueryHandler>();
        services.AddScoped<IGetAssetHitsQueryHandler, GetAssetHitsQueryHandler>();
        services.AddScoped<IGetRecentItemsQueryHandler, GetRecentItemsQueryHandler>();
        services.AddScoped<IGetViewAssetsQueryHandler, GetViewAssetsQueryHandler>();
        services.AddScoped<IGetDefaultViewForQueryHandler, GetDefaultViewForQueryHandler>();
        services.AddScoped<IGetChartFilterQueryHandler, GetChartFilterQueryHandler>();
        services.AddScoped<IMatchAssetsQueryHandler, MatchAssetsQueryHandler>();
        services.AddScoped<IMatchAssetsByValueQueryHandler, MatchAssetsByValueQueryHandler>();
        services.AddScoped<IMatchAssetsBySavedFilterQueryHandler, MatchAssetsBySavedFilterQueryHandler>();
        services.AddScoped<IGetPotentialValuesForAssetsQueryHandler, GetPotentialValuesForAssetsQueryHandler>();
        services.AddScoped<IGetPotentialValuesViaFilteringQueryHandler, GetPotentialValuesViaFilteringQueryHandler>();
        services.AddScoped<IGetMatchingAssetsQueryHandler, GetMatchingAssetsQueryHandler>();
        services.AddScoped<IGetAssetsByPropertyValuesQueryHandler, GetAssetsByPropertyValuesQueryHandler>();
        services.AddScoped<IGetAssetClientQueryHandler, GetAssetClientQueryHandler>();
        services.AddScoped<IGetViewValuesQueryHandler, GetViewValuesQueryHandler>();
        services.AddScoped<IGetHistoryQueryHandler, GetHistoryQueryHandler>();
        services.AddScoped<IGetAssetValueTextQueryHandler, GetAssetValueTextQueryHandler>();
        services.AddScoped<IGetAssetNameQueryHandler, GetAssetNameQueryHandler>();
        services.AddScoped<IGetAssetDescriptionQueryHandler, GetAssetDescriptionQueryHandler>();
        services.AddScoped<IGetAssetParentQueryHandler, GetAssetParentQueryHandler>();
        services.AddScoped<IAssetModifiedQueryHandler, AssetModifiedQueryHandler>();
        services.AddScoped<IAssetExistsQueryHandler, AssetExistsQueryHandler>();
        services.AddScoped<IFindAssetIdQueryHandler, FindAssetIdQueryHandler>();
        services.AddScoped<IGetValueDisplayQueryHandler, GetValueDisplayQueryHandler>();
        services.AddScoped<IGetAssetsQueryHandler, GetAssetsQueryHandler>();
        services.AddScoped<IGetAssetDefinitionsQueryHandler, GetAssetDefinitionsQueryHandler>();
        services.AddScoped<IGetAssetInstancesQueryHandler, GetAssetInstancesQueryHandler>();
        services.AddScoped<ICountAssetsQueryHandler, CountAssetsQueryHandler>();
        services.AddScoped<ISearchAssetsByNameQueryHandler, SearchAssetsByNameQueryHandler>();
        services.AddScoped<IGetAssetsFromPropertyValueQueryHandler, GetAssetsFromPropertyValueQueryHandler>();
        services.AddScoped<IGetAssetsInPropertyValueQueryHandler, GetAssetsInPropertyValueQueryHandler>();
        services.AddScoped<IGetAssetsGroupedQueryHandler, GetAssetsGroupedQueryHandler>();

        // Metadata and view writes
        services.AddScoped<DcjsReader>(sp => new DcjsReader(() => sp.GetRequiredService<IDateTimeProvider>().UtcNow().ToLocalTime()));
        services.AddScoped<IMetadataWriteProcedures, MetadataWriteProcedures>();
        services.AddScoped<AssetTypeSaver>();
        services.AddScoped<PropertySaver>();
        services.AddScoped<PickListSaver>();
        services.AddScoped<PropertyGroupSaver>();
        services.AddScoped<ViewSaver>();
        services.AddScoped<IMetadataSaveCommandHandler, MetadataSaveCommandHandler>();
        services.AddScoped<IMetadataMigrateCommandHandler, MetadataMigrateCommandHandler>();
        services.AddScoped<IViewCommandHandler, ViewCommandHandler>();
        services.AddScoped<IViewCloneQueryHandler, ViewCloneQueryHandler>();
        services.AddScoped<IViewMigrateCommandHandler, ViewMigrateCommandHandler>();

        // Asset writes, submittals, bookmarks
        services.AddScoped<IAssetWriteProcedures, AssetWriteProcedures>();
        services.AddScoped<GenTableRefresher>();
        services.AddScoped<AssetWriter>();
        services.AddScoped<SubmittalEngine>();
        services.AddScoped<NarrativeWriter>();
        services.AddScoped<IAssetCommandHandler, AssetCommandHandler>();
        services.AddScoped<IBookmarkCommandHandler, BookmarkCommandHandler>();

        // Members, errors and feedback, alerts and questions
        services.AddScoped<IUserProcedures, UserProcedures>();
        services.AddScoped<MemberLoader>();
        services.AddScoped<IUserQueryHandler, UserQueryHandler>();
        services.AddScoped<IUserCommandHandler, UserCommandHandler>();

        // Submittal reads, triggers, activity feeds, search
        services.AddScoped<IActivityProcedures, ActivityProcedures>();
        services.AddScoped<SubmittalSummaryLoader>();
        services.AddScoped<IActivityQueryHandler, ActivityQueryHandler>();
        services.AddScoped<ISearchQueryHandler, SearchQueryHandler>();
        services.AddScoped<ITriggerCommandHandler, TriggerCommandHandler>();

        // Bulk uploads
        services.AddScoped<IBulkUploadProcedures, BulkUploadProcedures>();
        services.AddScoped<IBulkUploadQueryHandler, BulkUploadQueryHandler>();

        // Image and document streams and uploads
        services.AddScoped<IFileProcedures, FileProcedures>();
        services.AddSingleton<UploadProgress>();
        services.AddScoped<IFileQueryHandler, FileQueryHandler>();
        services.AddScoped<IUploadCommandHandler, UploadCommandHandler>();

        // Report export (xlsx)
        services.AddScoped<IReportExportHandler, ReportExportHandler>();

        // Administration
        services.AddScoped<IAdminProcedures, AdminProcedures>();
        services.AddScoped<IAdminQueryHandler, AdminQueryHandler>();
        services.AddScoped<IAdminCommandHandler, AdminCommandHandler>();

        // Report builder and administration
        services.AddScoped<ReportLoader>();
        services.AddScoped<ReportFilterString>();
        services.AddScoped<ReportSaver>();
        services.AddScoped<IGetReportQueryHandler, GetReportQueryHandler>();
        services.AddScoped<IGetReportFilterStringQueryHandler, GetReportFilterStringQueryHandler>();
        services.AddScoped<IGetReportGroupColumnsQueryHandler, GetReportGroupColumnsQueryHandler>();
        services.AddScoped<IGetUnusedReportPropertiesQueryHandler, GetUnusedReportPropertiesQueryHandler>();
        services.AddScoped<IGetReportsDictionaryQueryHandler, GetReportsDictionaryQueryHandler>();
        services.AddScoped<IGetReportCacheQueryHandler, GetReportCacheQueryHandler>();
        services.AddScoped<ICloneReportQueryHandler, CloneReportQueryHandler>();
        services.AddScoped<ISaveReportCommandHandler, SaveReportCommandHandler>();
        services.AddScoped<ICreateReportFromMemberFilterCommandHandler, CreateReportFromMemberFilterCommandHandler>();
        services.AddScoped<IDeleteReportCommandHandler, DeleteReportCommandHandler>();
        services.AddScoped<IKillReportCommandHandler, KillReportCommandHandler>();
        services.AddScoped<IMigrateReportCommandHandler, MigrateReportCommandHandler>();
        services.AddScoped<IReportBookmarkCommandHandler, ReportBookmarkCommandHandler>();

        // Roles
        services.AddScoped<LegacyErrorLog>();
        services.AddScoped<IGetRolesDictionaryQueryHandler, GetRolesDictionaryQueryHandler>();
        services.AddScoped<IGetRoleQueryHandler, GetRoleQueryHandler>();
        services.AddScoped<ICloneRoleQueryHandler, CloneRoleQueryHandler>();
        services.AddScoped<ICloneRoleViewRelationsQueryHandler, CloneRoleViewRelationsQueryHandler>();
        services.AddScoped<IGetRoleMembersQueryHandler, GetRoleMembersQueryHandler>();
        services.AddScoped<IGetRoleReportsQueryHandler, GetRoleReportsQueryHandler>();
        services.AddScoped<IGetRoleUserIdsQueryHandler, GetRoleUserIdsQueryHandler>();
        services.AddScoped<ISaveRoleCommandHandler, SaveRoleCommandHandler>();
        services.AddScoped<ISaveRoleViewRelationsCommandHandler, SaveRoleViewRelationsCommandHandler>();
        services.AddScoped<ISaveRoleProcessRelationsCommandHandler, SaveRoleProcessRelationsCommandHandler>();
        services.AddScoped<IKillRoleCommandHandler, KillRoleCommandHandler>();

        // Charts
        services.AddScoped<ICreateChartQueryHandler, CreateChartQueryHandler>();
        services.AddScoped<CurrentMember>();
        services.AddScoped<IGetDashboardsQueryHandler, GetDashboardsQueryHandler>();
        services.AddScoped<IGetUnmappedQueryHandler, GetUnmappedQueryHandler>();
        services.AddScoped<SaveDashboardCommandHandler>();
        services.AddScoped<ISaveDashboardCommandHandler>(sp => sp.GetRequiredService<SaveDashboardCommandHandler>());
        services.AddScoped<IAddDashboardItemCommandHandler, AddDashboardItemCommandHandler>();
        services.AddScoped<ISetMappingCommandHandler, SetMappingCommandHandler>();
        services.AddScoped<IRenderChartCommandHandler, RenderChartCommandHandler>();
        services.AddScoped<IGetDashboardChartQueryHandler, GetDashboardChartQueryHandler>();
        services.AddScoped<IGetChartSupportDataQueryHandler, GetChartSupportDataQueryHandler>();

        // Generated objects
        services.AddScoped<IGenTableGenerator, GenTableGenerator>();
        services.AddScoped<ViewInputLoader>();
        services.AddScoped<IReportViewGenerator, ReportViewGenerator>();
        services.AddScoped<IRoleViewGenerator, RoleViewGenerator>();
        services.AddScoped<IRegenerateAllCommandHandler, RegenerateAllCommandHandler>();

        // Setup
        services.AddScoped<IInitializeDatabaseCommandHandler, InitializeDatabaseCommandHandler>();

        // Assets
        services.AddScoped<ICreateAssetCommandHandler, CreateAssetCommandHandler>();
        services.AddScoped<ICreateAssetsCommandHandler, CreateAssetsCommandHandler>();

        // Asset types
        services.AddScoped<ICreateAssetTypeCommandHandler, CreateAssetTypeCommandHandler>();

        // Pick lists
        services.AddScoped<ICreatePickListCommandHandler, CreatePickListCommandHandler>();

        // Properties
        services.AddScoped<ICreatePropertyCommandHandler, CreatePropertyCommandHandler>();

        // Property groups
        services.AddScoped<ICreatePropertyGroupCommandHandler, CreatePropertyGroupCommandHandler>();

        // Views
        services.AddScoped<ICreateViewCommandHandler, CreateViewCommandHandler>();

        // Reports
        services.AddScoped<IGetReportClientQueryHandler, GetReportClientQueryHandler>();
        services.AddScoped<IGetReportDataQueryHandler, GetReportDataQueryHandler>();
        services.AddScoped<IGetReportExecutionStatsQueryHandler, GetReportExecutionStatsQueryHandler>();

        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}
