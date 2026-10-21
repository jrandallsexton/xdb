-- XDB baseline (generated; do not edit by hand).
-- Tables with primary keys, unique constraints, defaults, indexes and statistics. Foreign keys are in 0002.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[AgentReports](
	[Id] [uniqueidentifier] NOT NULL,
	[AssetId] [uniqueidentifier] NULL,
	[Created] [smalldatetime] NOT NULL,
	[Report] [varbinary](max) NOT NULL,
	[ReportVersion] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Processed] [smalldatetime] NULL,
 CONSTRAINT [PK_AgentReports] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[AgentReports] ADD  CONSTRAINT [DF_AgentReports_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[AgentReports] ADD  CONSTRAINT [DF_AgentReports_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[AssetClients](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AssetId] [uniqueidentifier] NOT NULL,
	[ViewId] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[Data] [varbinary](max) NOT NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_AssetClients] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[AssetClients] ADD  CONSTRAINT [DF__AssetClie__Creat__7E208022]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AssetHits](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AssetId] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_AssetHits] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_MemberIds] ON [dbo].[AssetHits]
(
	[MemberId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AssetHits] ADD  CONSTRAINT [DF_AssetHits_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[AssetRelationTypes](
	[Id] [tinyint] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AssetRelationTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_Unique_AssetRelationTypes] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AssetRelations](
	[Id] [uniqueidentifier] NOT NULL,
	[FromAssetId] [uniqueidentifier] NOT NULL,
	[ToAssetId] [uniqueidentifier] NOT NULL,
	[AssetRelationTypeId] [tinyint] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AssetRelations_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AssetRelations] ADD  CONSTRAINT [DF_AssetRelations_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[AssetRelations] ADD  CONSTRAINT [DF_AssetRelations_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AssetTypeRelations](
	[Id] [uniqueidentifier] NOT NULL,
	[ParentAssetTypeId] [uniqueidentifier] NOT NULL,
	[ChildAssetTypeId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AssetTypeRelations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_ParentChild] UNIQUE NONCLUSTERED 
(
	[ParentAssetTypeId] ASC,
	[ChildAssetTypeId] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AssetTypeRelations] ADD  CONSTRAINT [DF_AssetTypeRelations_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[AssetTypeRelations] ADD  CONSTRAINT [DF_AssetTypeRelations_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[AssetTypes](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Pluralization] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DefaultViewId] [uniqueidentifier] NULL,
	[DefaultInstanceViewId] [uniqueidentifier] NULL,
	[ImageId] [uniqueidentifier] NULL,
	[DefinitionLabel] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InstanceLabel] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[LastModified] [smalldatetime] NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[AllowAssets] [bit] NOT NULL,
	[DefinitionLabelPlural] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InstanceLabelPlural] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsSystem] [bit] NOT NULL,
 CONSTRAINT [PK_AssetTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_Unique_AssetTypes] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[AssetTypes] ADD  CONSTRAINT [DF_AssetTypes_Created]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[AssetTypes] ADD  CONSTRAINT [DF_AssetTypes_AllowAssets]  DEFAULT ((1)) FOR [AllowAssets]
GO
ALTER TABLE [dbo].[AssetTypes] ADD  CONSTRAINT [DF_AssetTypes_IsSystem]  DEFAULT ((0)) FOR [IsSystem]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AssetTypesProperties](
	[Id] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[IsInstance] [bit] NOT NULL,
	[IsInherited] [bit] NOT NULL,
	[IsInheritedValue] [bit] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AssetTypesProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AssetTypesProperties] ADD  CONSTRAINT [DF_AssetTypesProperties_IsInstance]  DEFAULT ((0)) FOR [IsInstance]
GO
ALTER TABLE [dbo].[AssetTypesProperties] ADD  CONSTRAINT [DF_AssetTypesProperties_IsInherited]  DEFAULT ((0)) FOR [IsInherited]
GO
ALTER TABLE [dbo].[AssetTypesProperties] ADD  CONSTRAINT [DF_AssetTypesProperties_IsInheritedValue]  DEFAULT ((0)) FOR [IsInheritedValue]
GO
ALTER TABLE [dbo].[AssetTypesProperties] ADD  CONSTRAINT [DF_AssetTypesProperties_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Assets](
	[Id] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[Name] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[LastModified] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsInstance] [bit] NOT NULL,
	[InstanceOfId] [uniqueidentifier] NULL,
	[DatastoreId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_Assets] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
CREATE NONCLUSTERED INDEX [_dta_index_Assets_5_1365579903__K2_K14_1_3_4_15] ON [dbo].[Assets]
(
	[AssetTypeId] ASC,
	[IsInstance] ASC
)
INCLUDE([DisplayValue],[Id],[InstanceOfId],[Name]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_AssetNames] ON [dbo].[Assets]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_AssetTypeIds] ON [dbo].[Assets]
(
	[AssetTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Assets] ADD  CONSTRAINT [DF_Assets_Created]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[Assets] ADD  CONSTRAINT [DF_Assets_IsInstance]  DEFAULT ((0)) FOR [IsInstance]
GO
CREATE STATISTICS [_dta_stat_1365579903_14_2] ON [dbo].[Assets]([IsInstance], [AssetTypeId])
WITH AUTO_DROP = OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Bugs](
	[Id] [uniqueidentifier] NOT NULL,
	[Area] [int] NOT NULL,
	[Criticality] [int] NOT NULL,
	[Title] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Bug] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_MembersIssues] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Bugs] ADD  CONSTRAINT [DF_Bugs_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Bugs] ADD  CONSTRAINT [DF_MembersIssues_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[BulkUploadLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BulkUploadId] [uniqueidentifier] NOT NULL,
	[Category] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[AssetId] [uniqueidentifier] NULL,
	[Message] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Order] [int] NOT NULL,
 CONSTRAINT [PK_BulkUploadLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[BulkUploadLogs] ADD  CONSTRAINT [DF_BulkUploadLogs_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[BulkUploadTemplates](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_BulkUploadTemplates] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BulkUploadTemplatesProperties](
	[Id] [uniqueidentifier] NOT NULL,
	[BulkUploadTemplateId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[Order] [int] NOT NULL,
 CONSTRAINT [PK_BulkUploadTemplatesProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BulkUploads](
	[Id] [uniqueidentifier] NOT NULL,
	[FileId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Completed] [smalldatetime] NULL,
	[RecordCount] [int] NULL,
	[AssetTypeId] [uniqueidentifier] NULL,
	[IsInstance] [bit] NULL,
	[ImportCount] [int] NULL,
	[UpdateCount] [int] NULL,
 CONSTRAINT [PK_BulkUploads] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Charts](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PaletteName] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AppearanceName] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ChartTitle] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[LegendText] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[LegendVisible] [bit] NOT NULL,
	[SeriesName] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ViewType] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[ChartImage] [varbinary](max) NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Title] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Subtitle] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsPublic] [bit] NOT NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsInstance] [bit] NULL,
	[RecordCount] [int] NULL,
	[FilterExpression] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Charts] ADD [GroupSql] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
ALTER TABLE [dbo].[Charts] ADD [LastModified] [smalldatetime] NULL
ALTER TABLE [dbo].[Charts] ADD [LastExecuted] [smalldatetime] NULL
SET ANSI_PADDING ON
ALTER TABLE [dbo].[Charts] ADD [FullSql] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
 CONSTRAINT [PK_Charts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Charts] ADD  CONSTRAINT [DF_Charts_Created]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[Charts] ADD  CONSTRAINT [DF_Charts_IsPublic]  DEFAULT ((0)) FOR [IsPublic]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ChartsFilters](
	[Id] [uniqueidentifier] NOT NULL,
	[ChartId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[Value] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Order] [int] NOT NULL,
	[OperatorId] [int] NULL,
 CONSTRAINT [PK_ChartsFilters] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[ChartsFilters] ADD  CONSTRAINT [DF_ChartsFilters_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ChartsProperties](
	[Id] [uniqueidentifier] NOT NULL,
	[ChartId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NULL,
	[SubPropertyId] [uniqueidentifier] NULL,
	[Order] [int] NOT NULL,
	[Label] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Logic] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsGrouping] [bit] NOT NULL,
	[CustomReportFieldTypeId] [int] NULL,
	[TimeElapsedTypeId] [int] NULL,
	[TimeElapsedFormatId] [int] NULL,
	[TimeElapsedSpecificDate] [smalldatetime] NULL,
 CONSTRAINT [PK_ChartsProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[ChartsProperties] ADD  CONSTRAINT [DF_ChartsProperties_IsGrouping]  DEFAULT ((0)) FOR [IsGrouping]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ClientCache](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Key] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Value] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_ClientCache] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[ClientCache] ADD  CONSTRAINT [DF_ClientCache_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[CurrencySymbols](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Value] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Display] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_CurrencySymbols] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CurrencyValues](
	[Id] [uniqueidentifier] NOT NULL,
	[CurrencySymbolId] [uniqueidentifier] NOT NULL,
	[Value] [decimal](18, 2) NULL,
 CONSTRAINT [PK_CurrencyValues] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[DashboardItems](
	[Id] [uniqueidentifier] NOT NULL,
	[DashboardId] [uniqueidentifier] NOT NULL,
	[Index] [int] NOT NULL,
	[ItemTypeId] [tinyint] NOT NULL,
	[ChartId] [uniqueidentifier] NOT NULL,
	[Title] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Subtitle] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
 CONSTRAINT [PK_DashboardItems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[DashboardItems] ADD  CONSTRAINT [DF_DashboardItems_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[DashboardItems] ADD  CONSTRAINT [DF_DashboardItems_Index]  DEFAULT ((0)) FOR [Index]
GO
ALTER TABLE [dbo].[DashboardItems] ADD  CONSTRAINT [DF_DashboardItems_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Dashboards](
	[Id] [uniqueidentifier] NOT NULL,
	[Caption] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Index] [int] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
 CONSTRAINT [PK_Dashboards] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Dashboards] ADD  CONSTRAINT [DF_Dashboards_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Dashboards] ADD  CONSTRAINT [DF_Dashboards_Index]  DEFAULT ((0)) FOR [Index]
GO
ALTER TABLE [dbo].[Dashboards] ADD  CONSTRAINT [DF_Dashboards_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[DataTypes](
	[Id] [tinyint] NOT NULL,
	[Value] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
 CONSTRAINT [PK_DataTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_Unique_DataTypes] UNIQUE NONCLUSTERED 
(
	[Value] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[DataTypes] ADD  CONSTRAINT [DF_DataTypes_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[DateParts](
	[Id] [uniqueidentifier] NOT NULL,
	[Value] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SqlDatePart] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Order] [int] NULL,
 CONSTRAINT [PK_DateParts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[DateParts] ADD  CONSTRAINT [DF_DateParts_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[DebugLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Category] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Message] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_DebugLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[DocumentTypes](
	[Id] [int] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
 CONSTRAINT [PK_DocumentTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_Unique_DocTypes] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Documents](
	[Id] [uniqueidentifier] NOT NULL,
	[DocumentTypeId] [int] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Title] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Data] [varbinary](max) NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Documents] ADD  CONSTRAINT [DF_Documents_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Documents] ADD  CONSTRAINT [DF_Documents_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Errors](
	[Id] [uniqueidentifier] NOT NULL,
	[Message] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Exception] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MemberId] [uniqueidentifier] NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_Errors] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Errors] ADD  CONSTRAINT [DF_Errors_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Errors] ADD  CONSTRAINT [DF_Errors_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[HelpContent](
	[Id] [uniqueidentifier] NOT NULL,
	[Title] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ImageId] [uniqueidentifier] NOT NULL,
	[Order] [int] NOT NULL,
	[Content] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_HelpContent] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ImageTypes](
	[Id] [tinyint] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
 CONSTRAINT [PK_ImageTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_Unique_ImageTypes] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Images](
	[Id] [uniqueidentifier] NOT NULL,
	[ImageTypeId] [tinyint] NOT NULL,
	[Order] [tinyint] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ImageData] [varbinary](max) NULL,
	[Width] [int] NULL,
	[Height] [int] NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsSystem] [bit] NULL,
 CONSTRAINT [PK_AssetImages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Images] ADD  CONSTRAINT [DF_AssetImages_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Images] ADD  CONSTRAINT [DF_AssetImages_Created]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[Images] ADD  CONSTRAINT [DF_Images_IsSystem]  DEFAULT ((0)) FOR [IsSystem]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Mappings](
	[Id] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[Key] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Value] [uniqueidentifier] NULL,
 CONSTRAINT [PK_SoftwareMappings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Mappings] ADD  CONSTRAINT [DF_SoftwareMappings_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MemberBookmarks](
	[Id] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[ViewId] [uniqueidentifier] NULL,
	[AssetId] [uniqueidentifier] NULL,
	[ReportId] [uniqueidentifier] NULL,
	[ShowOnHome] [bit] NOT NULL,
 CONSTRAINT [PK_MemberBookmarks] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[MemberBookmarks] ADD  CONSTRAINT [DF_MemberBookmarks_ShowOnHome]  DEFAULT ((0)) FOR [ShowOnHome]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[MemberOptions](
	[Id] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[OptionTypeId] [uniqueidentifier] NOT NULL,
	[OptionValue] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_MemberOptions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[MemberSettings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[Key] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Value] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_MemberSettings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[MemberSettings] ADD  CONSTRAINT [DF_MemberSettings_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[MemberViewFilters](
	[Id] [uniqueidentifier] NOT NULL,
	[MemberViewId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[OperatorId] [int] NOT NULL,
	[Value] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Order] [int] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[MemberViews](
	[Id] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[ViewId] [uniqueidentifier] NOT NULL,
	[Name] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FilterExpression] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING OFF
ALTER TABLE [dbo].[MemberViews] ADD [FilterString] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
 CONSTRAINT [PK_MemberViews] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[MemberViews] ADD  CONSTRAINT [DF_MemberViews_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[MemberVisits](
	[Id] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[BrowserName] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BrowserVersion] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IpAddress] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY]
SET ANSI_PADDING OFF
ALTER TABLE [dbo].[MemberVisits] ADD [UserAgent] [varchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
 CONSTRAINT [PK_MemberVisits] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[MemberVisits] ADD  CONSTRAINT [DF_MemberVisits_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[MemberVisits] ADD  CONSTRAINT [DF_MemberVisits_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Members](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[LName] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FName] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MName] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[LastModified] [smalldatetime] NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[AllowBeta] [bit] NULL,
 CONSTRAINT [PK_RoleMembers_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_Unique_Members] UNIQUE NONCLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Members] ADD  CONSTRAINT [DF_Members_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[MimeTypes](
	[Id] [uniqueidentifier] NOT NULL,
	[FileExtension] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MimeType] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_MimeTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[MimeTypes] ADD  CONSTRAINT [DF_MimeTypes_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Operators](
	[Id] [uniqueidentifier] NOT NULL,
	[Operator] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Order] [tinyint] NOT NULL,
 CONSTRAINT [PK_Operators] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Permissions](
	[Id] [uniqueidentifier] NOT NULL,
	[Value] [int] NOT NULL,
	[Name] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NULL,
	[AssetRequestType] [tinyint] NULL,
 CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Permissions] ADD  CONSTRAINT [DF_Permissions_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Permissions] ADD  CONSTRAINT [DF_Permissions_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PickListDependencies](
	[Id] [uniqueidentifier] NOT NULL,
	[ParentPickListId] [uniqueidentifier] NOT NULL,
	[ParentPickListValueId] [uniqueidentifier] NOT NULL,
	[ChildPickListId] [uniqueidentifier] NOT NULL,
	[ChildPickListValueId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_PickListDependencies] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[PickListDependencies] ADD  CONSTRAINT [DF_PickListDependencies_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PickListValues](
	[Id] [uniqueidentifier] NOT NULL,
	[PickListId] [uniqueidentifier] NOT NULL,
	[Value] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BGColor] [varchar](7) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Order] [int] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[LastModified] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_PickListValues] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
CREATE NONCLUSTERED INDEX [IX_PickListId] ON [dbo].[PickListValues]
(
	[PickListId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[PickListValues] ADD  CONSTRAINT [DF_PickListValues_uiId]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[PickListValues] ADD  CONSTRAINT [DF_PickListValues_dtCreated]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PickLists](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsMemberList] [bit] NOT NULL,
	[AllowNewValues] [bit] NOT NULL,
	[ServiceUrl] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ServiceMethod] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ServiceUsername] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ServicePassword] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[LastModified] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_PickLists] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[PickLists] ADD  CONSTRAINT [DF_PickLists_uiId]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[PickLists] ADD  CONSTRAINT [DF_PickLists_IsMemberlist]  DEFAULT ((0)) FOR [IsMemberList]
GO
ALTER TABLE [dbo].[PickLists] ADD  CONSTRAINT [DF_PickLists_AllowNewValues]  DEFAULT ((0)) FOR [AllowNewValues]
GO
ALTER TABLE [dbo].[PickLists] ADD  CONSTRAINT [DF_PickLists_dtCreated]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProcessStepFilters](
	[Id] [uniqueidentifier] NOT NULL,
	[ProcessStepId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[Order] [int] NOT NULL,
 CONSTRAINT [PK_ProcessStepFilters] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ProcessSteps](
	[Id] [uniqueidentifier] NOT NULL,
	[ProcessId] [uniqueidentifier] NOT NULL,
	[Order] [int] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ViewId] [uniqueidentifier] NULL,
	[IsInstance] [bit] NOT NULL,
	[IsCreation] [bit] NOT NULL,
	[ShowAssetTypeSelector] [bit] NOT NULL,
	[AssetNameLabel] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsEdit] [bit] NOT NULL,
 CONSTRAINT [PK_ProcessSteps] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[ProcessSteps] ADD  CONSTRAINT [DF_ProcessSteps_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[ProcessSteps] ADD  CONSTRAINT [DF_ProcessSteps_IsInstance]  DEFAULT ((0)) FOR [IsInstance]
GO
ALTER TABLE [dbo].[ProcessSteps] ADD  CONSTRAINT [DF_ProcessSteps_IsCreation]  DEFAULT ((0)) FOR [IsCreation]
GO
ALTER TABLE [dbo].[ProcessSteps] ADD  CONSTRAINT [DF_ProcessSteps_ShowAssetTypeSelector]  DEFAULT ((0)) FOR [ShowAssetTypeSelector]
GO
ALTER TABLE [dbo].[ProcessSteps] ADD  CONSTRAINT [DF_ProcessSteps_IsEdit]  DEFAULT ((0)) FOR [IsEdit]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Processes](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[DisplayValue] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AssetTypeId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_Processes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Processes] ADD  CONSTRAINT [DF_Processes_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Processes] ADD  CONSTRAINT [DF_Processes_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Properties](
	[Id] [uniqueidentifier] NOT NULL,
	[DataTypeId] [tinyint] NOT NULL,
	[Precision] [tinyint] NULL,
	[PickListId] [uniqueidentifier] NULL,
	[RoleId] [uniqueidentifier] NULL,
	[IsOrdered] [bit] NOT NULL,
	[AllowMultiValue] [bit] NOT NULL,
	[Name] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AssetTypeId] [uniqueidentifier] NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[LastModified] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[AssetTypeIsInstance] [bit] NULL,
	[IsSystem] [bit] NOT NULL,
	[SystemTypeId] [int] NULL,
	[Singular] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Plural] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Properties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
CREATE NONCLUSTERED INDEX [IX_DataTypeIds] ON [dbo].[Properties]
(
	[DataTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Properties] ADD  CONSTRAINT [DF_Properties_uiId]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Properties] ADD  CONSTRAINT [DF_Properties_IsOrdered]  DEFAULT ((0)) FOR [IsOrdered]
GO
ALTER TABLE [dbo].[Properties] ADD  CONSTRAINT [DF_Properties_AllowMultiValue]  DEFAULT ((0)) FOR [AllowMultiValue]
GO
ALTER TABLE [dbo].[Properties] ADD  CONSTRAINT [DF_Properties_dtCreated]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[Properties] ADD  CONSTRAINT [DF_Properties_IsSystem]  DEFAULT ((0)) FOR [IsSystem]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PropertyGroups](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[LastModified] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[IsInstance] [bit] NOT NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[PropertyGroups] ADD  CONSTRAINT [DF_Categories_uiId]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[PropertyGroups] ADD  CONSTRAINT [DF_Categories_dtCreated]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PropertyGroupsProperties](
	[Id] [uniqueidentifier] NOT NULL,
	[PropertyGroupId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[Order] [int] NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsReadOnly] [bit] NULL,
	[DefaultValue] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_PropertyGroupsProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[PropertyGroupsProperties] ADD  CONSTRAINT [DF_PropertyGroupsProperties_IsRequired]  DEFAULT ((0)) FOR [IsRequired]
GO
ALTER TABLE [dbo].[PropertyGroupsProperties] ADD  CONSTRAINT [DF_PropertyGroupsProperties_IsReadOnly]  DEFAULT ((0)) FOR [IsReadOnly]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PropertyValueSubmittals](
	[Id] [uniqueidentifier] NOT NULL,
	[AssetId] [uniqueidentifier] NULL,
	[PickListValueId] [uniqueidentifier] NULL,
	[Notes] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Reviewed] [smalldatetime] NULL,
	[ReviewedBy] [uniqueidentifier] NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[TriggerChecked] [smalldatetime] NULL,
	[Narrative] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_PropertyValueSubmittals] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PropertyValues](
	[Id] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[AssetId] [uniqueidentifier] NOT NULL,
	[SubmittalGroupId] [uniqueidentifier] NULL,
	[Value] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Order] [int] NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[Rejected] [smalldatetime] NULL,
	[RejectedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_PropertyValues_1] PRIMARY KEY NONCLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [_dta_index_PropertyValues_5_836914053__K2_K3_K9_5] ON [dbo].[PropertyValues]
(
	[PropertyId] ASC,
	[AssetId] ASC,
	[Deleted] ASC
)
INCLUDE([Value]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [_dta_index_PropertyValues_7_1989582126__K1_2_3_4_5_6_7_8_9_10_11_12_13_14] ON [dbo].[PropertyValues]
(
	[Id] ASC
)
INCLUDE([Approved],[ApprovedBy],[AssetId],[Created],[CreatedBy],[Deleted],[DeletedBy],[Order],[PropertyId],[Rejected],[RejectedBy],[SubmittalGroupId],[Value]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [_dta_index_PropertyValues_7_1989582126__K9_K4_1_2_3_5_6_7_8_10_11_12_13_14] ON [dbo].[PropertyValues]
(
	[Deleted] ASC,
	[SubmittalGroupId] ASC
)
INCLUDE([Approved],[ApprovedBy],[AssetId],[Created],[CreatedBy],[DeletedBy],[Id],[Order],[PropertyId],[Rejected],[RejectedBy],[Value]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_AssetIds] ON [dbo].[PropertyValues]
(
	[AssetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_PropertyIds] ON [dbo].[PropertyValues]
(
	[PropertyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_PropVals] ON [dbo].[PropertyValues]
(
	[PropertyId] ASC,
	[AssetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[PropertyValues] ADD  CONSTRAINT [DF_PropertyValues_Created]  DEFAULT (getdate()) FOR [Created]
GO
CREATE STATISTICS [_dta_stat_836914053_3_2_9] ON [dbo].[PropertyValues]([AssetId], [PropertyId], [Deleted])
WITH AUTO_DROP = OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[QuestionGroups](
	[Id] [uniqueidentifier] NOT NULL,
	[SurveyId] [uniqueidentifier] NOT NULL,
	[AssetId] [uniqueidentifier] NOT NULL,
	[Caption] [varchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Notified] [smalldatetime] NULL,
	[Closed] [smalldatetime] NULL,
 CONSTRAINT [PK_QuestionGroups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[QuestionGroups] ADD  CONSTRAINT [DF_QuestionGroups_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Questions](
	[Id] [uniqueidentifier] NOT NULL,
	[QuestionGroupId] [uniqueidentifier] NOT NULL,
	[Index] [int] NOT NULL,
	[AllowUnknown] [bit] NOT NULL,
	[DisableNext] [bit] NOT NULL,
	[DisableCondition] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[PropertyValueId] [uniqueidentifier] NULL,
	[Message] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ResponseText] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[Responded] [smalldatetime] NULL,
	[Response] [bit] NULL,
	[PropertyId] [uniqueidentifier] NULL,
	[UserProvidedValue] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[UserProvidedValueIsValid] [bit] NULL,
	[UserProvidedValueAssetId] [uniqueidentifier] NULL,
	[Processed] [smalldatetime] NULL,
 CONSTRAINT [PK_Questions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Questions] ADD  CONSTRAINT [DF_Questions_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Questions] ADD  CONSTRAINT [DF_Questions_Index]  DEFAULT ((0)) FOR [Index]
GO
ALTER TABLE [dbo].[Questions] ADD  CONSTRAINT [DF_Questions_DisableNext]  DEFAULT ((0)) FOR [DisableNext]
GO
ALTER TABLE [dbo].[Questions] ADD  CONSTRAINT [DF_Questions_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReportCache](
	[ReportId] [uniqueidentifier] NOT NULL,
	[DocumentId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[LastModified] [smalldatetime] NULL,
 CONSTRAINT [PK_ReportCache] PRIMARY KEY CLUSTERED 
(
	[ReportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ReportLogs](
	[Id] [uniqueidentifier] NOT NULL,
	[ReportId] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Started] [time](7) NOT NULL,
	[Completed] [time](7) NULL,
	[SqlText] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RecordsReturned] [int] NULL,
	[Deleted] [smalldatetime] NULL,
	[WasCached] [bit] NULL,
 CONSTRAINT [PK_ReportLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[ReportLogs] ADD  CONSTRAINT [DF_ReportLogs_Created]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[ReportLogs] ADD  CONSTRAINT [DF_ReportLogs_WasCached]  DEFAULT ((0)) FOR [WasCached]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Reports](
	[Id] [uniqueidentifier] NOT NULL,
	[ReportName] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[AssetTypeId] [uniqueidentifier] NULL,
	[FilterExpression] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsInstance] [bit] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[StoredProc] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Reports] ADD [FilterString] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
 CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Reports] ADD  CONSTRAINT [DF_Reports_IsInstance]  DEFAULT ((0)) FOR [IsInstance]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ReportsFilters](
	[Id] [uniqueidentifier] NOT NULL,
	[ReportId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[Value] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Order] [int] NOT NULL,
	[OperatorId] [int] NULL,
 CONSTRAINT [PK_ReportsFilters] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[ReportsFilters] ADD  CONSTRAINT [DF_ReportsFilters_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ReportsProperties](
	[Id] [uniqueidentifier] NOT NULL,
	[ReportId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NULL,
	[Order] [int] NOT NULL,
	[IsGrouping] [bit] NOT NULL,
	[SubPropertyId] [uniqueidentifier] NULL,
	[CustomReportFieldTypeId] [int] NULL,
	[TimeElapsedTypeId] [int] NULL,
	[TimeElapsedFormatId] [int] NULL,
	[TimeElapsedSpecificDate] [smalldatetime] NULL,
	[Label] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Logic] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_ReportsProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
CREATE NONCLUSTERED INDEX [IX_PropertyIds] ON [dbo].[ReportsProperties]
(
	[PropertyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_ReportIds] ON [dbo].[ReportsProperties]
(
	[ReportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ReportsProperties] ADD  CONSTRAINT [DF_ReportsProperties_IsGrouping]  DEFAULT ((0)) FOR [IsGrouping]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoleAssetTypes](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[IsInstance] [bit] NOT NULL,
	[IsInherited] [bit] NOT NULL,
	[InheritFrom] [uniqueidentifier] NULL,
 CONSTRAINT [PK_RoleAssetTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[RoleFilters](
	[Id] [uniqueidentifier] NOT NULL,
	[RolePermissionsId] [uniqueidentifier] NOT NULL,
	[Order] [int] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[OperatorId] [int] NOT NULL,
	[Value] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_RoleFilters] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoleMembers](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_RoleMembers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[RoleMembers] ADD  CONSTRAINT [DF_RoleMembers_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[RoleMembers] ADD  CONSTRAINT [DF_RoleMembers_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING OFF
GO
CREATE TABLE [dbo].[RolePermissions](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleAssetTypesId] [uniqueidentifier] NOT NULL,
	[PermissionId] [int] NOT NULL,
	[IsIncludesFilter] [bit] NOT NULL,
	[FilterLogic] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_RolePermissions_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoleReports](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[ReportId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_RoleReports] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[LastModified] [smalldatetime] NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsSystem] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[RoleLead] [uniqueidentifier] NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Roles] ADD  CONSTRAINT [DF_Roles_Created]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[Roles] ADD  CONSTRAINT [DF_Roles_IsSystem]  DEFAULT ((0)) FOR [IsSystem]
GO
ALTER TABLE [dbo].[Roles] ADD  CONSTRAINT [DF_Roles_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[RolesAssetTypes](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[InheritAccess] [bit] NOT NULL,
	[FilterLogic] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsInstance] [bit] NULL,
 CONSTRAINT [PK_RolesAssetTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[RolesAssetTypes] ADD  CONSTRAINT [DF_RolesAssetTypes_InheritAccess]  DEFAULT ((1)) FOR [InheritAccess]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[RolesAssetTypesFilters](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[IsIncludesFilter] [bit] NOT NULL,
	[Order] [int] NOT NULL,
	[LogicalOperatorID] [uniqueidentifier] NULL,
	[OperatorId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[Value] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_RolesFilters] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolesAssetTypesPermissions](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[PermissionId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_RolesAssetTypesPermissions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolesAssetTypesProcesses](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NULL,
	[ProcessId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_RolesProcesses] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolesMembers](
	[Id] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Approved] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[ApprovedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_RolesMembers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_Unique_Members_InRole] UNIQUE NONCLUSTERED 
(
	[MemberId] ASC,
	[RoleId] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_MemberIds] ON [dbo].[RolesMembers]
(
	[MemberId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_RoleIds] ON [dbo].[RolesMembers]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[RolesMembers] ADD  CONSTRAINT [DF_RolesMembers_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[RolesMembers] ADD  CONSTRAINT [DF_RolesMembers_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolesReports](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[ReportId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_RolesReports] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolesViews](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[ViewId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsAutoApprove] [bit] NOT NULL,
 CONSTRAINT [PK_RolesViews] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [uc_Unique_Roles_Views] UNIQUE NONCLUSTERED 
(
	[RoleId] ASC,
	[ViewId] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[RolesViews] ADD  CONSTRAINT [DF_RolesViews_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[RolesViews] ADD  CONSTRAINT [DF_RolesViews_Created]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[RolesViews] ADD  CONSTRAINT [DF_RolesViews_IsAutoApprove]  DEFAULT ((0)) FOR [IsAutoApprove]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Searches](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SearchPhrase] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_Searches] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Searches] ADD  CONSTRAINT [DF_Searches_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ServiceModuleConfigs](
	[Id] [uniqueidentifier] NOT NULL,
	[ServiceModuleId] [uniqueidentifier] NOT NULL,
	[Key] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Value] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
 CONSTRAINT [PK_ServiceModuleConfigs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServiceModuleLogGroups](
	[Id] [uniqueidentifier] NOT NULL,
	[ServiceModuleId] [uniqueidentifier] NOT NULL,
	[LogCount] [int] NULL,
	[Started] [smalldatetime] NULL,
	[Completed] [smalldatetime] NULL,
 CONSTRAINT [PK_ServiceModuleLogGroups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ServiceModuleLogs](
	[Id] [uniqueidentifier] NOT NULL,
	[ServiceModuleId] [uniqueidentifier] NOT NULL,
	[Category] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Message] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[LogGroupId] [uniqueidentifier] NULL,
	[Order] [int] NULL,
	[AssetId] [uniqueidentifier] NULL,
	[MemberId] [uniqueidentifier] NULL,
	[PropertyId] [uniqueidentifier] NULL,
	[PickListId] [uniqueidentifier] NULL,
	[PickListValueId] [uniqueidentifier] NULL,
	[Subcategory] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_ServiceModuleLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ServiceModules](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AssemblyName] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AssemblyNamespace] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ClassName] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AssemblyData] [varbinary](max) NULL,
	[AssemblyVersion] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsActive] [nchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Downloaded] [smalldatetime] NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[LastRan] [smalldatetime] NULL,
	[LastCompleted] [smalldatetime] NULL,
	[LastPing] [smalldatetime] NULL,
 CONSTRAINT [PK_ServiceModules] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SqlLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InvokingClass] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InvokingMethod] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InvokingLineNo] [int] NULL,
	[InvokedClass] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InvokedMethod] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InvokedLineNo] [int] NULL,
	[SqlText] [text] COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ParamList] [text] COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[TsStart] [time](7) NULL,
	[TsEnd] [time](7) NULL,
	[StackTrace] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_SqlLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[SqlLogs] ADD  CONSTRAINT [DF_SqlLogs_Created]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[SqlLogs] ADD  CONSTRAINT [DF_SqlLogs_TS]  DEFAULT (getdate()) FOR [TsStart]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[StoredProcActivity](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SpName] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Hits] [int] NOT NULL,
 CONSTRAINT [PK_StoredProcActivity] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Suggestions](
	[Id] [uniqueidentifier] NOT NULL,
	[Title] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Suggestion] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_Suggestions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Suggestions] ADD  CONSTRAINT [DF_Suggestions_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Suggestions] ADD  CONSTRAINT [DF_Suggestions_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Surveys](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_Surveys] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Surveys] ADD  CONSTRAINT [DF_Surveys_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SystemOptions](
	[LogDynamicSql] [bit] NULL,
	[LogStoredProcs] [bit] NULL,
	[ReportCache] [int] NULL
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SystemTypes](
	[Id] [int] NOT NULL,
	[SystemType] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_SystemTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[TriggerLogs](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NOT NULL,
	[AssetId] [uniqueidentifier] NOT NULL,
	[SubmittalId] [uniqueidentifier] NOT NULL,
	[PropertyValueId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[OldValue] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NewValue] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TriggerText] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Created] [smalldatetime] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[TriggerLogs] ADD  CONSTRAINT [DF_TriggerLogs_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[TriggerLogs] ADD  CONSTRAINT [DF_TriggerLogs_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TriggerLogsMembers](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerLogId] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Viewed] [smalldatetime] NULL,
	[Read] [smalldatetime] NULL,
	[Pinned] [smalldatetime] NULL,
 CONSTRAINT [PK_TriggerLogsMembers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[TriggerLogsMembers] ADD  CONSTRAINT [DF_TriggerLogsMembers_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[TriggerLogsMembers] ADD  CONSTRAINT [DF_TriggerLogsMembers_Created]  DEFAULT (getdate()) FOR [Created]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Triggers](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IsActive] [bit] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[TypeId] [int] NULL,
	[FilterExpression] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Triggers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Triggers] ADD  CONSTRAINT [DF_Triggers_IsActive]  DEFAULT ((0)) FOR [IsActive]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TriggersAssetTypes](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NOT NULL,
	[AssetTypeId] [uniqueidentifier] NOT NULL,
	[IncludesChildren] [bit] NOT NULL,
 CONSTRAINT [PK_TriggersAssetTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[TriggersAssetTypes] ADD  CONSTRAINT [DF_TriggersAssetTypes_IncludesChildren]  DEFAULT ((0)) FOR [IncludesChildren]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TriggersAssets](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NOT NULL,
	[AssetId] [uniqueidentifier] NOT NULL,
	[IncludesInstances] [bit] NOT NULL,
 CONSTRAINT [PK_TriggersAssets] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[TriggersAssets] ADD  CONSTRAINT [DF_TriggersAssets_IncludesInstances]  DEFAULT ((0)) FOR [IncludesInstances]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TriggersMembers](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NOT NULL,
	[MemberId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_TriggersMembers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[TriggersMembers] ADD  CONSTRAINT [DF_TriggersMembers_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[TriggersProperties](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[OperatorId] [uniqueidentifier] NULL,
	[Threshold] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Value] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_TriggersProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TriggersRestrictions](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[RestrictedPropertyId] [uniqueidentifier] NOT NULL,
	[SourcePropertyId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_TriggersRestrictions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TriggersRoles](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_TriggersRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[TriggersRoles] ADD  CONSTRAINT [DF_TriggersRoles_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[TriggersTargetProperties](
	[Id] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[NewValue] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_TriggersTargetProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[URLs](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[URL] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created] [smalldatetime] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_URLs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ViewClients](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ViewId] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[Data] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_ViewClients] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ViewFilters](
	[Id] [uniqueidentifier] NOT NULL,
	[ViewId] [uniqueidentifier] NOT NULL,
	[PropertyId] [uniqueidentifier] NOT NULL,
	[OperatorId] [uniqueidentifier] NOT NULL,
	[Value] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Order] [int] NOT NULL,
 CONSTRAINT [PK_ViewFilters] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Views](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[AssetTypeId] [uniqueidentifier] NULL,
	[DriverCaption] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NewItemCaption] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ConfirmationLabel] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ReportingLabel] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsReadOnly] [bit] NULL,
	[Created] [smalldatetime] NOT NULL,
	[LastModified] [smalldatetime] NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[LastModifiedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsInstance] [bit] NOT NULL,
	[IsStandAlone] [bit] NOT NULL,
	[IsCreation] [bit] NOT NULL,
	[FilterLogic] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IconId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_Views] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[Views] ADD  CONSTRAINT [DF_Views_uiId]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Views] ADD  CONSTRAINT [DF_Views_IsReadOnly]  DEFAULT ((0)) FOR [IsReadOnly]
GO
ALTER TABLE [dbo].[Views] ADD  CONSTRAINT [DF_Views_dtCreated]  DEFAULT (getdate()) FOR [Created]
GO
ALTER TABLE [dbo].[Views] ADD  CONSTRAINT [DF_Views_IsInstanceView]  DEFAULT ((0)) FOR [IsInstance]
GO
ALTER TABLE [dbo].[Views] ADD  CONSTRAINT [DF_Views_IsStandAlone]  DEFAULT ((1)) FOR [IsStandAlone]
GO
ALTER TABLE [dbo].[Views] ADD  CONSTRAINT [DF_Views_IsCreation]  DEFAULT ((0)) FOR [IsCreation]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ViewsPropertyGroups](
	[Id] [uniqueidentifier] NOT NULL,
	[ViewId] [uniqueidentifier] NOT NULL,
	[PropertyGroupId] [uniqueidentifier] NOT NULL,
	[Order] [int] NOT NULL,
	[Created] [smalldatetime] NOT NULL,
	[Deleted] [smalldatetime] NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[DeletedBy] [uniqueidentifier] NULL,
 CONSTRAINT [PK_ViewsPropertyGroups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_ViewIds] ON [dbo].[ViewsPropertyGroups]
(
	[ViewId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ViewsPropertyGroups] ADD  CONSTRAINT [DF_ViewsPropertyGroups_Id]  DEFAULT (newid()) FOR [Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ViewsTriggers](
	[Id] [uniqueidentifier] NOT NULL,
	[ViewId] [uniqueidentifier] NOT NULL,
	[TriggerId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_ViewTriggers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_ViewIds] ON [dbo].[ViewsTriggers]
(
	[ViewId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[lkOperators](
	[Id] [int] NOT NULL,
	[Operator] [varchar](5) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_lkOperators] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[lkPermissions](
	[Id] [int] NOT NULL,
	[Name] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayValue] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [varchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AllowFiltering] [bit] NOT NULL,
 CONSTRAINT [PK_lkPermissions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
