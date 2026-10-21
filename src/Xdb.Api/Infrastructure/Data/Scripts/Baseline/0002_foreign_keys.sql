-- XDB baseline (generated; do not edit by hand).
-- Foreign keys.
ALTER TABLE [dbo].[AssetHits]  WITH NOCHECK ADD  CONSTRAINT [FK_AssetHits_Assets] FOREIGN KEY([AssetId])
REFERENCES [dbo].[Assets] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[AssetHits] NOCHECK CONSTRAINT [FK_AssetHits_Assets]
GO
ALTER TABLE [dbo].[AssetRelations]  WITH NOCHECK ADD  CONSTRAINT [FK_AssetRelations_AssetRelationTypes] FOREIGN KEY([AssetRelationTypeId])
REFERENCES [dbo].[AssetRelationTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[AssetRelations] NOCHECK CONSTRAINT [FK_AssetRelations_AssetRelationTypes]
GO
ALTER TABLE [dbo].[AssetRelations]  WITH CHECK ADD  CONSTRAINT [FK_AssetRelations_Assets] FOREIGN KEY([FromAssetId])
REFERENCES [dbo].[Assets] ([Id])
GO
ALTER TABLE [dbo].[AssetRelations] CHECK CONSTRAINT [FK_AssetRelations_Assets]
GO
ALTER TABLE [dbo].[AssetRelations]  WITH CHECK ADD  CONSTRAINT [FK_AssetRelations_Assets1] FOREIGN KEY([ToAssetId])
REFERENCES [dbo].[Assets] ([Id])
GO
ALTER TABLE [dbo].[AssetRelations] CHECK CONSTRAINT [FK_AssetRelations_Assets1]
GO
ALTER TABLE [dbo].[AssetTypeRelations]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeRelations_AssetTypes] FOREIGN KEY([ParentAssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
GO
ALTER TABLE [dbo].[AssetTypeRelations] CHECK CONSTRAINT [FK_AssetTypeRelations_AssetTypes]
GO
ALTER TABLE [dbo].[AssetTypeRelations]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeRelations_AssetTypes1] FOREIGN KEY([ChildAssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
GO
ALTER TABLE [dbo].[AssetTypeRelations] CHECK CONSTRAINT [FK_AssetTypeRelations_AssetTypes1]
GO
ALTER TABLE [dbo].[AssetTypes]  WITH NOCHECK ADD  CONSTRAINT [FK_AssetTypes_Images] FOREIGN KEY([ImageId])
REFERENCES [dbo].[Images] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[AssetTypes] NOCHECK CONSTRAINT [FK_AssetTypes_Images]
GO
ALTER TABLE [dbo].[AssetTypes]  WITH NOCHECK ADD  CONSTRAINT [FK_AssetTypes_Views] FOREIGN KEY([DefaultViewId])
REFERENCES [dbo].[Views] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[AssetTypes] NOCHECK CONSTRAINT [FK_AssetTypes_Views]
GO
ALTER TABLE [dbo].[AssetTypes]  WITH NOCHECK ADD  CONSTRAINT [FK_AssetTypes_Views1] FOREIGN KEY([DefaultInstanceViewId])
REFERENCES [dbo].[Views] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[AssetTypes] NOCHECK CONSTRAINT [FK_AssetTypes_Views1]
GO
ALTER TABLE [dbo].[AssetTypesProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_AssetTypesProperties_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[AssetTypesProperties] NOCHECK CONSTRAINT [FK_AssetTypesProperties_AssetTypes]
GO
ALTER TABLE [dbo].[AssetTypesProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_AssetTypesProperties_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[AssetTypesProperties] NOCHECK CONSTRAINT [FK_AssetTypesProperties_Properties]
GO
ALTER TABLE [dbo].[Assets]  WITH CHECK ADD  CONSTRAINT [FK_Assets_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
GO
ALTER TABLE [dbo].[Assets] CHECK CONSTRAINT [FK_Assets_AssetTypes]
GO
ALTER TABLE [dbo].[Assets]  WITH NOCHECK ADD  CONSTRAINT [FK_Assets_Assets] FOREIGN KEY([InstanceOfId])
REFERENCES [dbo].[Assets] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Assets] NOCHECK CONSTRAINT [FK_Assets_Assets]
GO
ALTER TABLE [dbo].[BulkUploadLogs]  WITH NOCHECK ADD  CONSTRAINT [FK_BulkUploadLogs_Assets] FOREIGN KEY([AssetId])
REFERENCES [dbo].[Assets] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[BulkUploadLogs] NOCHECK CONSTRAINT [FK_BulkUploadLogs_Assets]
GO
ALTER TABLE [dbo].[BulkUploadLogs]  WITH NOCHECK ADD  CONSTRAINT [FK_BulkUploadLogs_BulkUploads] FOREIGN KEY([BulkUploadId])
REFERENCES [dbo].[BulkUploads] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[BulkUploadLogs] NOCHECK CONSTRAINT [FK_BulkUploadLogs_BulkUploads]
GO
ALTER TABLE [dbo].[BulkUploads]  WITH NOCHECK ADD  CONSTRAINT [FK_BulkUploads_Documents] FOREIGN KEY([FileId])
REFERENCES [dbo].[Documents] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[BulkUploads] NOCHECK CONSTRAINT [FK_BulkUploads_Documents]
GO
ALTER TABLE [dbo].[ChartsFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ChartsFilters_Charts] FOREIGN KEY([ChartId])
REFERENCES [dbo].[Charts] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ChartsFilters] NOCHECK CONSTRAINT [FK_ChartsFilters_Charts]
GO
ALTER TABLE [dbo].[ChartsFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ChartsFilters_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ChartsFilters] NOCHECK CONSTRAINT [FK_ChartsFilters_Properties]
GO
ALTER TABLE [dbo].[ChartsProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_ChartsProperties_Charts] FOREIGN KEY([ChartId])
REFERENCES [dbo].[Charts] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ChartsProperties] NOCHECK CONSTRAINT [FK_ChartsProperties_Charts]
GO
ALTER TABLE [dbo].[ChartsProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_ChartsProperties_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ChartsProperties] NOCHECK CONSTRAINT [FK_ChartsProperties_Properties]
GO
ALTER TABLE [dbo].[ChartsProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_ChartsProperties_Properties1] FOREIGN KEY([SubPropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ChartsProperties] NOCHECK CONSTRAINT [FK_ChartsProperties_Properties1]
GO
ALTER TABLE [dbo].[CurrencyValues]  WITH NOCHECK ADD  CONSTRAINT [FK_CurrencyValues_CurrencySymbols] FOREIGN KEY([CurrencySymbolId])
REFERENCES [dbo].[CurrencySymbols] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[CurrencyValues] NOCHECK CONSTRAINT [FK_CurrencyValues_CurrencySymbols]
GO
ALTER TABLE [dbo].[Documents]  WITH NOCHECK ADD  CONSTRAINT [FK_Documents_DocumentTypes] FOREIGN KEY([DocumentTypeId])
REFERENCES [dbo].[DocumentTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Documents] NOCHECK CONSTRAINT [FK_Documents_DocumentTypes]
GO
ALTER TABLE [dbo].[HelpContent]  WITH NOCHECK ADD  CONSTRAINT [FK_HelpContent_Images] FOREIGN KEY([ImageId])
REFERENCES [dbo].[Images] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[HelpContent] NOCHECK CONSTRAINT [FK_HelpContent_Images]
GO
ALTER TABLE [dbo].[Images]  WITH NOCHECK ADD  CONSTRAINT [FK_Images_ImageTypes] FOREIGN KEY([ImageTypeId])
REFERENCES [dbo].[ImageTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Images] NOCHECK CONSTRAINT [FK_Images_ImageTypes]
GO
ALTER TABLE [dbo].[MemberBookmarks]  WITH NOCHECK ADD  CONSTRAINT [FK_MemberBookmarks_Reports] FOREIGN KEY([ReportId])
REFERENCES [dbo].[Reports] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[MemberBookmarks] NOCHECK CONSTRAINT [FK_MemberBookmarks_Reports]
GO
ALTER TABLE [dbo].[MemberVisits]  WITH NOCHECK ADD  CONSTRAINT [FK_MemberVisits_Members] FOREIGN KEY([MemberId])
REFERENCES [dbo].[Members] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[MemberVisits] NOCHECK CONSTRAINT [FK_MemberVisits_Members]
GO
ALTER TABLE [dbo].[PickListDependencies]  WITH NOCHECK ADD  CONSTRAINT [FK_PickListDependencies_PickListValues] FOREIGN KEY([ParentPickListValueId])
REFERENCES [dbo].[PickListValues] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[PickListDependencies] NOCHECK CONSTRAINT [FK_PickListDependencies_PickListValues]
GO
ALTER TABLE [dbo].[PickListDependencies]  WITH NOCHECK ADD  CONSTRAINT [FK_PickListDependencies_PickListValues1] FOREIGN KEY([ChildPickListValueId])
REFERENCES [dbo].[PickListValues] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[PickListDependencies] NOCHECK CONSTRAINT [FK_PickListDependencies_PickListValues1]
GO
ALTER TABLE [dbo].[PickListDependencies]  WITH NOCHECK ADD  CONSTRAINT [FK_PickListDependencies_PickLists] FOREIGN KEY([ParentPickListId])
REFERENCES [dbo].[PickLists] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[PickListDependencies] NOCHECK CONSTRAINT [FK_PickListDependencies_PickLists]
GO
ALTER TABLE [dbo].[PickListDependencies]  WITH NOCHECK ADD  CONSTRAINT [FK_PickListDependencies_PickLists1] FOREIGN KEY([ChildPickListId])
REFERENCES [dbo].[PickLists] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[PickListDependencies] NOCHECK CONSTRAINT [FK_PickListDependencies_PickLists1]
GO
ALTER TABLE [dbo].[PickListValues]  WITH CHECK ADD  CONSTRAINT [FK_PickListValues_PickLists1] FOREIGN KEY([PickListId])
REFERENCES [dbo].[PickLists] ([Id])
GO
ALTER TABLE [dbo].[PickListValues] CHECK CONSTRAINT [FK_PickListValues_PickLists1]
GO
ALTER TABLE [dbo].[ProcessStepFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ProcessStepFilters_ProcessSteps] FOREIGN KEY([ProcessStepId])
REFERENCES [dbo].[ProcessSteps] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ProcessStepFilters] NOCHECK CONSTRAINT [FK_ProcessStepFilters_ProcessSteps]
GO
ALTER TABLE [dbo].[ProcessStepFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ProcessStepFilters_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ProcessStepFilters] NOCHECK CONSTRAINT [FK_ProcessStepFilters_Properties]
GO
ALTER TABLE [dbo].[ProcessSteps]  WITH NOCHECK ADD  CONSTRAINT [FK_ProcessSteps_Processes] FOREIGN KEY([ProcessId])
REFERENCES [dbo].[Processes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ProcessSteps] NOCHECK CONSTRAINT [FK_ProcessSteps_Processes]
GO
ALTER TABLE [dbo].[Properties]  WITH NOCHECK ADD  CONSTRAINT [FK_Properties_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Properties] NOCHECK CONSTRAINT [FK_Properties_AssetTypes]
GO
ALTER TABLE [dbo].[Properties]  WITH NOCHECK ADD  CONSTRAINT [FK_Properties_DataTypes] FOREIGN KEY([DataTypeId])
REFERENCES [dbo].[DataTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Properties] NOCHECK CONSTRAINT [FK_Properties_DataTypes]
GO
ALTER TABLE [dbo].[Properties]  WITH CHECK ADD  CONSTRAINT [FK_Properties_PickLists1] FOREIGN KEY([PickListId])
REFERENCES [dbo].[PickLists] ([Id])
GO
ALTER TABLE [dbo].[Properties] CHECK CONSTRAINT [FK_Properties_PickLists1]
GO
ALTER TABLE [dbo].[Properties]  WITH NOCHECK ADD  CONSTRAINT [FK_Properties_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Properties] NOCHECK CONSTRAINT [FK_Properties_Roles]
GO
ALTER TABLE [dbo].[Properties]  WITH NOCHECK ADD  CONSTRAINT [FK_Properties_SystemTypes] FOREIGN KEY([SystemTypeId])
REFERENCES [dbo].[SystemTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Properties] NOCHECK CONSTRAINT [FK_Properties_SystemTypes]
GO
ALTER TABLE [dbo].[PropertyGroups]  WITH NOCHECK ADD  CONSTRAINT [FK_PropertyGroups_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[PropertyGroups] NOCHECK CONSTRAINT [FK_PropertyGroups_AssetTypes]
GO
ALTER TABLE [dbo].[PropertyGroupsProperties]  WITH CHECK ADD  CONSTRAINT [FK_CategoriesProperties_Categories] FOREIGN KEY([PropertyGroupId])
REFERENCES [dbo].[PropertyGroups] ([Id])
GO
ALTER TABLE [dbo].[PropertyGroupsProperties] CHECK CONSTRAINT [FK_CategoriesProperties_Categories]
GO
ALTER TABLE [dbo].[PropertyGroupsProperties]  WITH CHECK ADD  CONSTRAINT [FK_CategoriesProperties_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
GO
ALTER TABLE [dbo].[PropertyGroupsProperties] CHECK CONSTRAINT [FK_CategoriesProperties_Properties]
GO
ALTER TABLE [dbo].[PropertyValueSubmittals]  WITH NOCHECK ADD  CONSTRAINT [FK_PropertyValueSubmittals_Assets] FOREIGN KEY([AssetId])
REFERENCES [dbo].[Assets] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[PropertyValueSubmittals] NOCHECK CONSTRAINT [FK_PropertyValueSubmittals_Assets]
GO
ALTER TABLE [dbo].[PropertyValueSubmittals]  WITH NOCHECK ADD  CONSTRAINT [FK_PropertyValueSubmittals_PickListValues] FOREIGN KEY([PickListValueId])
REFERENCES [dbo].[PickListValues] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[PropertyValueSubmittals] NOCHECK CONSTRAINT [FK_PropertyValueSubmittals_PickListValues]
GO
ALTER TABLE [dbo].[PropertyValues]  WITH NOCHECK ADD  CONSTRAINT [FK_PropertyValues_Assets] FOREIGN KEY([AssetId])
REFERENCES [dbo].[Assets] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[PropertyValues] NOCHECK CONSTRAINT [FK_PropertyValues_Assets]
GO
ALTER TABLE [dbo].[PropertyValues]  WITH CHECK ADD  CONSTRAINT [FK_PropertyValues_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
GO
ALTER TABLE [dbo].[PropertyValues] CHECK CONSTRAINT [FK_PropertyValues_Properties]
GO
ALTER TABLE [dbo].[ReportLogs]  WITH NOCHECK ADD  CONSTRAINT [FK_ReportLogs_Reports] FOREIGN KEY([ReportId])
REFERENCES [dbo].[Reports] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ReportLogs] NOCHECK CONSTRAINT [FK_ReportLogs_Reports]
GO
ALTER TABLE [dbo].[Reports]  WITH NOCHECK ADD  CONSTRAINT [FK_Reports_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Reports] NOCHECK CONSTRAINT [FK_Reports_AssetTypes]
GO
ALTER TABLE [dbo].[ReportsFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ReportsFilters_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ReportsFilters] NOCHECK CONSTRAINT [FK_ReportsFilters_Properties]
GO
ALTER TABLE [dbo].[ReportsFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ReportsFilters_Reports] FOREIGN KEY([ReportId])
REFERENCES [dbo].[Reports] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ReportsFilters] NOCHECK CONSTRAINT [FK_ReportsFilters_Reports]
GO
ALTER TABLE [dbo].[ReportsProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_ReportsProperties_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ReportsProperties] NOCHECK CONSTRAINT [FK_ReportsProperties_Properties]
GO
ALTER TABLE [dbo].[ReportsProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_ReportsProperties_Properties1] FOREIGN KEY([SubPropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ReportsProperties] NOCHECK CONSTRAINT [FK_ReportsProperties_Properties1]
GO
ALTER TABLE [dbo].[ReportsProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_ReportsProperties_Reports] FOREIGN KEY([ReportId])
REFERENCES [dbo].[Reports] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ReportsProperties] NOCHECK CONSTRAINT [FK_ReportsProperties_Reports]
GO
ALTER TABLE [dbo].[RolesAssetTypes]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypes_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypes] NOCHECK CONSTRAINT [FK_RolesAssetTypes_AssetTypes]
GO
ALTER TABLE [dbo].[RolesAssetTypes]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypes_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypes] NOCHECK CONSTRAINT [FK_RolesAssetTypes_Roles]
GO
ALTER TABLE [dbo].[RolesAssetTypesFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesFilters_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesFilters] NOCHECK CONSTRAINT [FK_RolesAssetTypesFilters_AssetTypes]
GO
ALTER TABLE [dbo].[RolesAssetTypesFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesFilters_Operators] FOREIGN KEY([OperatorId])
REFERENCES [dbo].[Operators] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesFilters] NOCHECK CONSTRAINT [FK_RolesAssetTypesFilters_Operators]
GO
ALTER TABLE [dbo].[RolesAssetTypesFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesFilters_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesFilters] NOCHECK CONSTRAINT [FK_RolesAssetTypesFilters_Properties]
GO
ALTER TABLE [dbo].[RolesAssetTypesFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesFilters_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesFilters] NOCHECK CONSTRAINT [FK_RolesAssetTypesFilters_Roles]
GO
ALTER TABLE [dbo].[RolesAssetTypesPermissions]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesPermissions_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesPermissions] NOCHECK CONSTRAINT [FK_RolesAssetTypesPermissions_AssetTypes]
GO
ALTER TABLE [dbo].[RolesAssetTypesPermissions]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesPermissions_Permissions] FOREIGN KEY([PermissionId])
REFERENCES [dbo].[Permissions] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesPermissions] NOCHECK CONSTRAINT [FK_RolesAssetTypesPermissions_Permissions]
GO
ALTER TABLE [dbo].[RolesAssetTypesPermissions]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesPermissions_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesPermissions] NOCHECK CONSTRAINT [FK_RolesAssetTypesPermissions_Roles]
GO
ALTER TABLE [dbo].[RolesAssetTypesProcesses]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesProcesses_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesProcesses] NOCHECK CONSTRAINT [FK_RolesAssetTypesProcesses_AssetTypes]
GO
ALTER TABLE [dbo].[RolesAssetTypesProcesses]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesProcesses_Processes] FOREIGN KEY([ProcessId])
REFERENCES [dbo].[Processes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesProcesses] NOCHECK CONSTRAINT [FK_RolesAssetTypesProcesses_Processes]
GO
ALTER TABLE [dbo].[RolesAssetTypesProcesses]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesAssetTypesProcesses_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesAssetTypesProcesses] NOCHECK CONSTRAINT [FK_RolesAssetTypesProcesses_Roles]
GO
ALTER TABLE [dbo].[RolesMembers]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesMembers_Members] FOREIGN KEY([MemberId])
REFERENCES [dbo].[Members] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesMembers] NOCHECK CONSTRAINT [FK_RolesMembers_Members]
GO
ALTER TABLE [dbo].[RolesMembers]  WITH CHECK ADD  CONSTRAINT [FK_RolesMembers_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
GO
ALTER TABLE [dbo].[RolesMembers] CHECK CONSTRAINT [FK_RolesMembers_Roles]
GO
ALTER TABLE [dbo].[RolesReports]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesReports_Reports] FOREIGN KEY([ReportId])
REFERENCES [dbo].[Reports] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesReports] NOCHECK CONSTRAINT [FK_RolesReports_Reports]
GO
ALTER TABLE [dbo].[RolesReports]  WITH NOCHECK ADD  CONSTRAINT [FK_RolesReports_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[RolesReports] NOCHECK CONSTRAINT [FK_RolesReports_Roles]
GO
ALTER TABLE [dbo].[RolesViews]  WITH CHECK ADD  CONSTRAINT [FK_RolesViews_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
GO
ALTER TABLE [dbo].[RolesViews] CHECK CONSTRAINT [FK_RolesViews_Roles]
GO
ALTER TABLE [dbo].[RolesViews]  WITH CHECK ADD  CONSTRAINT [FK_RolesViews_Views] FOREIGN KEY([ViewId])
REFERENCES [dbo].[Views] ([Id])
GO
ALTER TABLE [dbo].[RolesViews] CHECK CONSTRAINT [FK_RolesViews_Views]
GO
ALTER TABLE [dbo].[TriggersAssetTypes]  WITH NOCHECK ADD  CONSTRAINT [FK_TriggersAssetTypes_Triggers] FOREIGN KEY([TriggerId])
REFERENCES [dbo].[Triggers] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[TriggersAssetTypes] NOCHECK CONSTRAINT [FK_TriggersAssetTypes_Triggers]
GO
ALTER TABLE [dbo].[TriggersAssets]  WITH NOCHECK ADD  CONSTRAINT [FK_TriggersAssets_Triggers] FOREIGN KEY([TriggerId])
REFERENCES [dbo].[Triggers] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[TriggersAssets] NOCHECK CONSTRAINT [FK_TriggersAssets_Triggers]
GO
ALTER TABLE [dbo].[TriggersMembers]  WITH NOCHECK ADD  CONSTRAINT [FK_TriggersMembers_Triggers] FOREIGN KEY([TriggerId])
REFERENCES [dbo].[Triggers] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[TriggersMembers] NOCHECK CONSTRAINT [FK_TriggersMembers_Triggers]
GO
ALTER TABLE [dbo].[TriggersProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_TriggersProperties_Triggers] FOREIGN KEY([TriggerId])
REFERENCES [dbo].[Triggers] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[TriggersProperties] NOCHECK CONSTRAINT [FK_TriggersProperties_Triggers]
GO
ALTER TABLE [dbo].[TriggersRestrictions]  WITH NOCHECK ADD  CONSTRAINT [FK_TriggersRestrictions_Triggers] FOREIGN KEY([TriggerId])
REFERENCES [dbo].[Triggers] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[TriggersRestrictions] NOCHECK CONSTRAINT [FK_TriggersRestrictions_Triggers]
GO
ALTER TABLE [dbo].[TriggersRoles]  WITH NOCHECK ADD  CONSTRAINT [FK_TriggersRoles_Triggers] FOREIGN KEY([TriggerId])
REFERENCES [dbo].[Triggers] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[TriggersRoles] NOCHECK CONSTRAINT [FK_TriggersRoles_Triggers]
GO
ALTER TABLE [dbo].[TriggersTargetProperties]  WITH NOCHECK ADD  CONSTRAINT [FK_TriggersTargetProperties_Triggers] FOREIGN KEY([TriggerId])
REFERENCES [dbo].[Triggers] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[TriggersTargetProperties] NOCHECK CONSTRAINT [FK_TriggersTargetProperties_Triggers]
GO
ALTER TABLE [dbo].[ViewFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ViewFilters_Operators] FOREIGN KEY([OperatorId])
REFERENCES [dbo].[Operators] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ViewFilters] NOCHECK CONSTRAINT [FK_ViewFilters_Operators]
GO
ALTER TABLE [dbo].[ViewFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ViewFilters_Properties] FOREIGN KEY([PropertyId])
REFERENCES [dbo].[Properties] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ViewFilters] NOCHECK CONSTRAINT [FK_ViewFilters_Properties]
GO
ALTER TABLE [dbo].[ViewFilters]  WITH NOCHECK ADD  CONSTRAINT [FK_ViewFilters_Views] FOREIGN KEY([ViewId])
REFERENCES [dbo].[Views] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ViewFilters] NOCHECK CONSTRAINT [FK_ViewFilters_Views]
GO
ALTER TABLE [dbo].[Views]  WITH NOCHECK ADD  CONSTRAINT [FK_Views_AssetTypes] FOREIGN KEY([AssetTypeId])
REFERENCES [dbo].[AssetTypes] ([Id])
NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[Views] NOCHECK CONSTRAINT [FK_Views_AssetTypes]
GO
ALTER TABLE [dbo].[ViewsPropertyGroups]  WITH CHECK ADD  CONSTRAINT [FK_ViewsCategories_Categories] FOREIGN KEY([PropertyGroupId])
REFERENCES [dbo].[PropertyGroups] ([Id])
GO
ALTER TABLE [dbo].[ViewsPropertyGroups] CHECK CONSTRAINT [FK_ViewsCategories_Categories]
GO
ALTER TABLE [dbo].[ViewsPropertyGroups]  WITH CHECK ADD  CONSTRAINT [FK_ViewsCategories_Views] FOREIGN KEY([ViewId])
REFERENCES [dbo].[Views] ([Id])
GO
ALTER TABLE [dbo].[ViewsPropertyGroups] CHECK CONSTRAINT [FK_ViewsCategories_Views]
GO
