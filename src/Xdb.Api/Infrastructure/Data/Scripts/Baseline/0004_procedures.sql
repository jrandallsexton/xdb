-- XDB baseline (generated; do not edit by hand).
-- Definitions copied verbatim from sys.sql_modules, each with the ANSI_NULLS / QUOTED_IDENTIFIER settings it was created with.
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spSys_ReportLog_Close]
	@Id					uniqueidentifier,
	@ReportId			uniqueidentifier,
	@MemberId			uniqueidentifier,
	@Completed			smalldatetime = null,
	@SqlText			varchar(MAX) = null,
	@RecordsReturned	int = -1,
	@WasCached			bit = 0
AS

UPDATE [dbo].[ReportLogs] SET
	[ReportId] = @ReportId,
	[MemberId] = @MemberId,
	[Completed] = SYSDATETIME(),
	[SqlText] = @SqlText,
	[RecordsReturned] = @RecordsReturned,
	[WasCached] = @WasCached
WHERE [dbo].[ReportLogs].[Id] = @Id
GO
EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'NOT USED', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'PROCEDURE', @level1name = N'spSys_ReportLog_Close';
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spSys_ReportLog_Open]
	@Id					uniqueidentifier,
	@ReportId			uniqueidentifier,
	@MemberId			uniqueidentifier,
	@Completed			time = null,
	@SqlText			varchar(MAX) = null,
	@RecordsReturned	int,
	@WasCached			bit = 0
AS

	BEGIN TRANSACTION

		-- Mark previous log entries for this report as deleted
		-- This is important because the Client UI displays the statistics to
		--	the user upon completion.
		-- The only way of determining the statistics is to use the stats
		--	from the most current record
		UPDATE [ReportLogs]
		SET [Deleted] = GetDate()
		WHERE
			[ReportId] = @ReportId AND
			[Deleted] IS NULL

		-- Rollback the transaction if there were any errors
		IF @@ERROR <> 0
		 BEGIN
			-- Rollback the transaction
			ROLLBACK

			-- Raise an error and return
			RAISERROR ('Error in marking report log entry as deleted.', 16, 1)
			RETURN
		 END
	 		
		INSERT INTO [dbo].[ReportLogs] (
			[Id],
			[ReportId],
			[MemberId],
			[Started],
			[Completed],
			[SqlText],
			[RecordsReturned],
			[WasCached])
		VALUES (
			@Id,
			@ReportId,
			@MemberId,
			SYSDATETIME(),
			@Completed,
			@SqlText,
			@RecordsReturned,
			@WasCached)
			
		-- Rollback the transaction if there were any errors
		IF @@ERROR <> 0
		 BEGIN
			-- Rollback the transaction
			ROLLBACK

			-- Raise an error and return
			RAISERROR ('Error in inserting report log entry', 16, 1)
			RETURN
		 END
	 
	COMMIT
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spSys_ReportLog_Save]
	@Id					uniqueidentifier,
	@ReportId			uniqueidentifier,
	@MemberId			uniqueidentifier,
	@Completed			smalldatetime = null,
	@SqlText			varchar(MAX) = null,
	@RecordsReturned	int,
	@WasCached			bit = 0
AS

IF EXISTS (SELECT * FROM [dbo].[ReportLogs] WHERE [Id] = @Id)

	EXEC [dbo].[spSys_ReportLog_Close] 
		@Id, @ReportId, @MemberId, @Completed, @SqlText, @RecordsReturned, @WasCached

ELSE

	EXEC [dbo].[spSys_ReportLog_Open]
		@Id, @ReportId, @MemberId, @Completed, @SqlText, @RecordsReturned, @WasCached
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spSys_Report_Execution_Stats]
	@ReportId uniqueidentifier,
	@UserId  uniqueidentifier
AS

--time diff portion taken from
--Time Difference in SQL Server 2008
--http://ask.sqlservercentral.com/questions/22256/time-difference-in-sql-server-2008

SELECT
	[RecordsReturned]
	,[WasCached]
	,convert(time, dateadd(MILLISECOND, datediff(MILLISECOND, completed, [started]), 0))
	,convert(time, dateadd(MILLISECOND, datediff(MILLISECOND, [started], completed), 0))
FROM [ReportLogs]
WHERE 
	([ReportId] = @ReportId)
	AND ([MemberId] = @UserId )
	AND ([Deleted] IS NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spSys_SqlLog_Insert]
	@InvokingClass varchar(50),
	@InvokingMethod varchar(50),
	@InvokingLineNo int,
	@InvokedClass varchar(50),
	@InvokedMethod varchar(50),
	@InvokedLineNo int,
	@SqlText text = null,
	@ParamList text = null,
	@StackTrace varchar(MAX) = null
AS

INSERT INTO [SqlLogs] (
	[InvokingClass],
	[InvokingMethod],
	[InvokingLineNo],
	[InvokedClass],
	[InvokedMethod],
	[InvokedLineNo],
	[SqlText],
	[ParamList],
	[StackTrace])
VALUES (
	@InvokingClass, @InvokingMethod, @InvokingLineNo, 
	@InvokedClass, @InvokedMethod, @InvokedLineNo, @SqlText, @ParamList, @StackTrace)

SELECT CAST(scope_identity() AS int)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spSys_StoredProcActivity_Save]
	@SpName	varchar(255)
AS

IF EXISTS (SELECT * FROM [dbo].[StoredProcActivity] WHERE [SpName] = @SpName)

	UPDATE [dbo].[StoredProcActivity] SET [Hits] = [Hits] + 1 WHERE [SpName] = @SpName

ELSE

	INSERT INTO [dbo].[StoredProcActivity] ([SpName], [Hits]) VALUES (@SpName, 1)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelationList_DeleteByAssetRelationTypeId
--*	Deletes a AssetAssetRelationList based on the foreign key of AssetRelationTypeId
--*	Generated by B.A.B.O.O.M.
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelationList_DeleteByAssetRelationTypeId]
	@AssetRelationTypeId	tinyint
AS

	UPDATE [AssetRelations]
	SET [Deleted] = GetDate()
	WHERE [AssetRelationTypeId] = @AssetRelationTypeId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelationList_DeleteByFromAssetId
--*	Deletes a AssetAssetRelationList based on the foreign key of FromAssetId
--*	Generated by B.A.B.O.O.M.
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelationList_DeleteByFromAssetId]
	@FromAssetId	uniqueidentifier
AS

		UPDATE [AssetRelations]
		SET [Deleted] = GetDate()
		WHERE [FromAssetId] = @FromAssetId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelationList_DeleteByToAssetId
--*	Deletes a AssetAssetRelationList based on the foreign key of ToAssetId
--*	Generated by B.A.B.O.O.M.
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelationList_DeleteByToAssetId]
	@ToAssetId	uniqueidentifier
AS

		UPDATE [AssetRelations]
		SET [Deleted] = GetDate()
		WHERE [ToAssetId] = @ToAssetId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelationList_Get
--*	Retrieves a AssetAssetRelationList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelationList_Get]
AS

SELECT
	[Id] AS [Id],
	[FromAssetId] AS [FromAssetId],
	[ToAssetId] AS [ToAssetId],
	[AssetRelationTypeId] AS [AssetRelationTypeId],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy]
FROM [dbo].[AssetRelations] WITH (NoLock)
	WHERE [dbo].[AssetRelations].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelationList_GetByAssetRelationTypeId
--*	Retrieves a AssetAssetRelationList based on the foreign key of AssetRelationTypeId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelationList_GetByAssetRelationTypeId]
	@AssetRelationTypeId	tinyint
AS

SELECT
	[Id] AS [Id],
	[FromAssetId] AS [FromAssetId],
	[ToAssetId] AS [ToAssetId],
	[AssetRelationTypeId] AS [AssetRelationTypeId],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[AssetRelations] WITH (NoLock)
WHERE
	[dbo].[AssetRelations].[AssetRelationTypeId] = @AssetRelationTypeId
		AND
	[dbo].[AssetRelations].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelationList_GetByFromAssetId
--*	Retrieves a AssetAssetRelationList based on the foreign key of FromAssetId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelationList_GetByFromAssetId]
	@FromAssetId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[FromAssetId] AS [FromAssetId],
	[ToAssetId] AS [ToAssetId],
	[AssetRelationTypeId] AS [AssetRelationTypeId],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[AssetRelations] WITH (NoLock)
WHERE
	[dbo].[AssetRelations].[FromAssetId] = @FromAssetId
		AND
	[dbo].[AssetRelations].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelationList_GetByToAssetId
--*	Retrieves a AssetAssetRelationList based on the foreign key of ToAssetId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelationList_GetByToAssetId]
	@ToAssetId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[FromAssetId] AS [FromAssetId],
	[ToAssetId] AS [ToAssetId],
	[AssetRelationTypeId] AS [AssetRelationTypeId],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[AssetRelations] WITH (NoLock)
WHERE
	[dbo].[AssetRelations].[ToAssetId] = @ToAssetId
		AND
	[dbo].[AssetRelations].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelation_Delete
--*	Deletes the record for a/an AssetAssetRelation.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_AssetAssetRelation_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[AssetRelations] 
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelation_Get
--*	Retrieves a AssetAssetRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelation_Get]
	@Id	uniqueidentifier
AS

SELECT
	[FromAssetId] AS [FromAssetId],
	[ToAssetId] AS [ToAssetId],
	[AssetRelationTypeId] AS [AssetRelationTypeId],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[AssetRelations] WITH (NoLock)
WHERE
	[Id] = @Id
		AND
	[dbo].[AssetRelations].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelation_Insert
--*	Inserts a new record for a new AssetAssetRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelation_Insert]
	@Id						uniqueidentifier,
	@FromAssetId			uniqueidentifier,
	@ToAssetId				uniqueidentifier,
	@AssetRelationTypeId	tinyint,
	@Created				smalldatetime,
	@Approved				smalldatetime = null,
	@CreatedBy				uniqueidentifier,
	@ApprovedBy				uniqueidentifier = null,
	@DeletedBy				uniqueidentifier = null
AS

INSERT INTO [dbo].[AssetRelations] (
	[Id],
	[FromAssetId],
	[ToAssetId],
	[AssetRelationTypeId],
	[Created],
	[Approved],
	[CreatedBy],
	[ApprovedBy],
	[DeletedBy])
VALUES (
	@Id,
	@FromAssetId,
	@ToAssetId,
	@AssetRelationTypeId,
	@Created,
	@Approved,
	@CreatedBy,
	@ApprovedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelation_Save
--*	Saves an instance of AssetAssetRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelation_Save]
	@Id						uniqueidentifier,
	@FromAssetId			uniqueidentifier,
	@ToAssetId				uniqueidentifier,
	@AssetRelationTypeId	tinyint,
	@Created				smalldatetime,
	@Approved				smalldatetime = null,
	@CreatedBy				uniqueidentifier,
	@ApprovedBy				uniqueidentifier = null,
	@DeletedBy				uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[AssetRelations] WHERE [ToAssetId] = @ToAssetId AND [ToAssetId] = @ToAssetId)

	EXEC [dbo].[spr_AssetAssetRelation_Update] @Id, @FromAssetId, @ToAssetId, @AssetRelationTypeId, @Created, @Approved, @CreatedBy, @ApprovedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_AssetAssetRelation_Insert] @Id, @FromAssetId, @ToAssetId, @AssetRelationTypeId, @Created, @Approved, @CreatedBy, @ApprovedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetAssetRelation_Update
--*	Updates an existing record for a/an AssetAssetRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetAssetRelation_Update]
	@Id						uniqueidentifier,
	@FromAssetId			uniqueidentifier,
	@ToAssetId				uniqueidentifier,
	@AssetRelationTypeId	tinyint,
	@Created				smalldatetime,
	@Approved				smalldatetime = null,
	@CreatedBy				uniqueidentifier,
	@ApprovedBy				uniqueidentifier = null,
	@DeletedBy				uniqueidentifier = null
AS

UPDATE [dbo].[AssetRelations] SET
	[FromAssetId] = @FromAssetId,
	[ToAssetId] = @ToAssetId,
	[AssetRelationTypeId] = @AssetRelationTypeId,
	[Created] = @Created,
	[Approved] = @Approved,
	[CreatedBy] = @CreatedBy,
	[ApprovedBy] = @ApprovedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[AssetRelations].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetHits_GetForUser]
	@MemberId	uniqueidentifier
AS

SELECT TOP(10)
	COUNT(SUB.AssetId) AS [Hits]
	,A.[Id]
	,ISNULL(A.[DisplayValue], A.[Name]) AS [Asset]
	,A.[AssetTypeId]
	,CASE
		WHEN A.[InstanceOfId] IS NULL THEN (SELECT [DefaultViewId] FROM [AssetTypes] WHERE [Id] = A.[AssetTypeId])
		ELSE (SELECT [DefaultInstanceViewId] FROM [AssetTypes] WHERE [Id] = A.[AssetTypeId])
		END
	AS [DefaultViewId]
	,[dbo].[GetLastAssetHit](A.[Id], @MemberId) AS [LastHit]
FROM ( 
	SELECT TOP(50) AH.AssetId
	FROM [AssetHits] AH WITH (NoLock)
	WHERE AH.[MemberId] = @MemberId
	ORDER BY AH.[Created] DESC) SUB
INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = SUB.[AssetId]
GROUP BY A.[Id] , A.[DisplayValue], A.[Name], A.[AssetTypeId], A.[InstanceOfId]
ORDER BY [LastHit], DefaultViewId DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetHits_GetForUserByAssetIds]
	@AssetIds	varchar(MAX),
	@MemberId	uniqueidentifier
AS

select
	COUNT(*) as hitCount,
	[AssetId],
	MAX(created) as lastvisit
FROM [AssetHits] WITH (NoLock)
WHERE
	[AssetId] IN (SELECT [Value] FROM dbo.Split(@AssetIds, ',')) AND
	[MemberId] = @MemberId
GROUP BY [AssetId]
--SELECT TOP(12)
--	COUNT(SUB.AssetId) AS [Hits]
--	, A.Id
--	, ISNULL(A.[DisplayValue], A.[Name]) AS [Asset]
--	, CASE
--		WHEN A.[InstanceOfId] IS NULL THEN (SELECT [DefaultViewId] FROM [AssetTypes] WHERE [Id] = A.[AssetTypeId])
--		ELSE (SELECT [DefaultInstanceViewId] FROM [AssetTypes] WHERE [Id] = A.[AssetTypeId])
--		END AS [DefaultViewId]
--	,[dbo].[GetLastAssetHit](A.[Id], @MemberId) AS [LastHit]
--FROM ( 
--	SELECT TOP(50) AH.AssetId
--	FROM [AssetHits] AH
--	WHERE AH.[MemberId] = @MemberId
--	ORDER BY AH.[Created] DESC) SUB
--INNER JOIN [Assets] A ON A.[Id] = SUB.[AssetId]
--GROUP BY A.Id , A.[DisplayValue], A.[Name], A.[AssetTypeId], A.[InstanceOfId]
--ORDER BY [LastHit], DefaultViewId DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Thursday, Sep 29, 2011 12:01
--*	spr_AssetHits_get
--*	Retrieves the number of page visits by asset type
--* ==================================================
--* Date			Modifier	Action
--* 29 Sep 2011		JRS			Initial Version
--* 02 Jun 2013		JRS			Split to handle software abbreviations

CREATE PROCEDURE [dbo].[spr_AssetHits_get]
	@AssetTypeId	uniqueidentifier
AS

IF @AssetTypeId = '473F1298-8A9F-4C3D-8AA5-358A27D0735D'
	SELECT
		A.[Id],
		IsNull(dbo.GetPropertyValue(A.Id, 'BAEF42A2-C83D-4B49-B83B-320B6299ED65'), A.[Name]) AS [Name],
		COUNT(*) AS [HitCount]
	FROM [Assets] A WITH (NoLock)
		INNER JOIN [AssetHits] AH WITH (NoLock) ON AH.[AssetId] = A.[Id]
	WHERE
		A.[AssetTypeId] = @AssetTypeId
		AND A.[Deleted] IS NULL
	GROUP BY A.[Name], A.[Id]
	ORDER BY [HitCount] DESC
ELSE
	SELECT
		A.[Id],
		A.[Name],
		COUNT(*) AS [HitCount]
	FROM [Assets] A
	INNER JOIN [AssetHits] AH ON AH.[AssetId] = A.[Id]
	WHERE
		A.[AssetTypeId] = @AssetTypeId AND
		A.[Deleted] IS NULL
	GROUP BY A.[Name], A.[Id]
	ORDER BY [HitCount] DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetId_IsValid]
	@Id	uniqueidentifier
AS

SELECT COUNT(*) FROM [Assets] WITH (NoLock)
WHERE [Id] = @Id AND [Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetIds_Get]
	@AssetTypeId	uniqueidentifier,
	@IsInstance		bit
AS

SELECT
	[Id] FROM [Assets] WITH (NoLock)
WHERE
	[AssetTypeId] = @AssetTypeId AND
	([Deleted] IS NULL) AND
	([Approved] IS NOT NULL) AND
	([IsInstance] = @IsInstance)
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetIds_GetByInstanceOfIds]
	@AssetIds	varchar(max)
AS

SELECT DISTINCT
	[InstanceOfId]
FROM [Assets] A WITH (NoLock)
WHERE
	A.[Id] IN (SELECT [Value] FROM dbo.Split(@AssetIds, ',')) AND
	(A.[Deleted] IS NULL) AND
	(A.[Approved] IS NOT NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetInstances_Get]
	@AssetTypeId uniqueidentifier
AS

SELECT
	[Id],
	IsNull([DisplayValue], [Name]) AS [Name]
FROM [Assets] WITH (NoLock)
WHERE
	[AssetTypeId] = @AssetTypeId AND
	[Deleted] IS NULL AND
	[Approved] IS NOT NULL AND
	[IsInstance] = 1
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetInstances_GetByAssetTypeIds]
	@AssetTypeIds varchar(max)
AS

SELECT
	[Id],
	IsNull([DisplayValue], [Name]) AS [Name]
FROM [Assets] WITH (NoLock)
WHERE
	[AssetTypeId] IN (SELECT [Value] FROM dbo.Split(@AssetTypeIds, ',')) AND
	[Deleted] IS NULL AND
	[Approved] IS NOT NULL AND
	[IsInstance] = 1
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetInstances_GetByParentId]
	@AssetTypeId uniqueidentifier
AS

--declare @ttbl table([key] varchar(36), [value] varchar(max))

 --Using a Parent AssetTypeId
--insert @ttbl
	SELECT
		A.[Id],
		IsNull(A.[DisplayValue], A.[Name]) AS [Name]	
	FROM [Assets] a WITH (NoLock)
	 INNER JOIN [dbo].[AssetTypeChildren](@AssetTypeId) B on B.Id = A.AssetTypeId 
	WHERE A.[Deleted] IS NULL AND
		A.[Approved] IS NOT NULL AND
		A.[IsInstance] = 1
	ORDER BY [Name]

--select '[' + STUFF((
--        select 
--            ',{"key":"' + [key]
--            --+ ',"name":"' + name + '"'
--            + '","value":"' + [value]
--            +'"}'
--        from @ttbl t1
--        for xml path(''), type
--    ).value('.', 'varchar(max)'), 1, 1, '') + ']'
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetTypePropertyRelationList_GetByAssetTypeId
--*	Retrieves a AssetTypePropertyRelationList based on the foreign key of AssetTypeId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetTypePropertyRelationList_GetByAssetTypeId]
	@AssetTypeId	uniqueidentifier
AS

SELECT
	ATP.[Id] AS [Id],
	ATP.[AssetTypeId] AS [AssetTypeId],
	ATP.[PropertyId] AS [PropertyId],
	ATP.[IsInstance] AS [IsInstance],
	ATP.[IsInherited] AS [IsInherited],
	ATP.[IsInheritedValue] AS [IsInheritedValue],
	ATP.[Created] AS [Created],
	ATP.[CreatedBy] AS [CreatedBy]
FROM [dbo].[AssetTypesProperties] ATP WITH (NoLock)
INNER JOIN [dbo].[Properties] P ON P.[Id] = ATP.[PropertyId]
WHERE
	ATP.[AssetTypeId] = @AssetTypeId
		AND
	ATP.[Deleted] IS NULL
ORDER BY P.[DisplayValue]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetTypePropertyRelationList_GetByAssetTypeIdAndPropertyIds]
	@AssetTypeId	uniqueidentifier,
	@PropertyIds	varchar(max)
AS

SELECT
	ATP.[Id] AS [Id],
	ATP.[AssetTypeId] AS [AssetTypeId],
	ATP.[PropertyId] AS [PropertyId],
	ATP.[IsInstance] AS [IsInstance],
	ATP.[IsInherited] AS [IsInherited],
	ATP.[IsInheritedValue] AS [IsInheritedValue],
	ATP.[Created] AS [Created],
	ATP.[CreatedBy] AS [CreatedBy]
FROM [dbo].[AssetTypesProperties] ATP WITH (NoLock)
INNER JOIN [dbo].[Properties] P ON P.[Id] = ATP.[PropertyId]
WHERE
	ATP.[AssetTypeId] = @AssetTypeId AND
	ATP.[PropertyId] IN (SELECT [Value] FROM dbo.Split(@PropertyIds, ',')) AND
	ATP.[Deleted] IS NULL AND
	P.[Deleted] IS NULL
ORDER BY P.[DisplayValue]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetTypePropertyRelationList_GetByAssetTypeIdsAndPropertyId]
	@AssetTypeIds	varchar(max),
	@PropertyId		uniqueidentifier
AS

SELECT
	ATP.[Id] AS [Id],
	ATP.[AssetTypeId] AS [AssetTypeId],
	ATP.[PropertyId] AS [PropertyId],
	ATP.[IsInstance] AS [IsInstance],
	ATP.[IsInherited] AS [IsInherited],
	ATP.[IsInheritedValue] AS [IsInheritedValue],
	ATP.[Created] AS [Created],
	ATP.[CreatedBy] AS [CreatedBy]
FROM [dbo].[AssetTypesProperties] ATP WITH (NoLock)
INNER JOIN [dbo].[Properties] P ON P.[Id] = ATP.[PropertyId]
WHERE
	ATP.[AssetTypeId] IN (SELECT [Value] FROM dbo.Split(@AssetTypeIds, ',')) AND
	ATP.[Deleted] IS NULL AND
	ATP.[PropertyId] = @PropertyId AND
	P.[Deleted] IS NULL
ORDER BY P.[DisplayValue]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetTypePropertyRelation_Delete
--*	Deletes the record for a/an AssetTypePropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================
--* 28 Jul 2009	JRS	Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_AssetTypePropertyRelation_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[AssetTypesProperties]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetTypePropertyRelation_Get
--*	Retrieves a AssetTypePropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetTypePropertyRelation_Get]
	@Id uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[AssetTypeId] AS [AssetTypeId],
	[PropertyId] AS [PropertyId],
	[IsInstance] AS [IsInstance],
	[IsInherited] AS [IsInherited],
	[IsInheritedValue] AS [IsInheritedValue],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[Deleted] AS [Deleted],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[AssetTypesProperties] WITH (NoLock)
WHERE 
	[dbo].[AssetTypesProperties].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetTypePropertyRelation_Insert
--*	Inserts a new record for a new AssetTypePropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetTypePropertyRelation_Insert]
	@Id					uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@IsInstance			bit,
	@IsInherited		bit,
	@IsInheritedValue	bit,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[AssetTypesProperties] (
	[Id],
	[AssetTypeId],
	[PropertyId],
	[IsInstance],
	[IsInherited],
	[IsInheritedValue],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@AssetTypeId,
	@PropertyId,
	@IsInstance,
	@IsInherited,
	@IsInheritedValue,
	@Created,
	@CreatedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetTypePropertyRelation_Save
--*	Saves an instance of AssetTypePropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetTypePropertyRelation_Save]
	@Id					uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@IsInstance			bit,
	@IsInherited		bit,
	@IsInheritedValue	bit,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

--IF EXISTS (SELECT * FROM [dbo].[AssetTypesProperties] WHERE [AssetTypeId] = @AssetTypeId AND [PropertyId] = @PropertyId)
IF EXISTS (SELECT * FROM [dbo].[AssetTypesProperties] WHERE [Id] = @Id)

	EXEC [dbo].[spr_AssetTypePropertyRelation_Update] @Id, @AssetTypeId, @PropertyId, @IsInstance, @IsInherited, @IsInheritedValue, @Created, @CreatedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_AssetTypePropertyRelation_Insert] @Id, @AssetTypeId, @PropertyId, @IsInstance, @IsInherited, @IsInheritedValue, @Created, @CreatedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetTypePropertyRelation_Update
--*	Updates an existing record for a/an AssetTypePropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetTypePropertyRelation_Update]
	@Id					uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@IsInstance			bit,
	@IsInherited		bit,
	@IsInheritedValue	bit,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

UPDATE [dbo].[AssetTypesProperties] SET
	[AssetTypeId] = @AssetTypeId,
	[PropertyId] = @PropertyId,
	[IsInstance] = @IsInstance,
	[IsInherited] = @IsInherited,
	[IsInheritedValue] = @IsInheritedValue,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[AssetTypesProperties].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetType_Delete
--*	Deletes the record for a/an AssetType.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_AssetType_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[AssetTypes] 
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetType_Get
--*	Retrieves an instance of AssetType based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added [CreatedBy] to select list

CREATE PROCEDURE [dbo].[spr_AssetType_Get]
	@Id	uniqueidentifier
AS

SELECT
	[Name] AS [Name],
	[Pluralization] AS [Pluralization],
	[DefaultViewId] AS [DefaultViewId],
	[DefaultInstanceViewId] AS [DefaultInstanceViewId],
	[DefinitionLabel] AS [DefinitionLabel],
	[InstanceLabel] AS [InstanceLabel],
	[ImageId] AS [ImageId],
	[AllowAssets] AS [AllowAssets],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy]
FROM [dbo].[AssetTypes] WITH (NoLock)
WHERE
	[dbo].[AssetTypes].[Id] = @Id
		AND
	[dbo].[AssetTypes].[Deleted] IS NULL
		AND
	[dbo].[AssetTypes].[DeletedBy] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetType_GetAllDescendants]
	@Id	uniqueidentifier
AS

WITH AssetType_CTE AS (

	SELECT AT0.[Name] AS [Parent], ATR.[ParentAssetTypeId], AT1.[Name] AS [Child], ATR.[ChildAssetTypeId]
	FROM [AssetTypeRelations] ATR 
	INNER JOIN [AssetTypes] AT0 ON AT0.[Id] = ATR.[ParentAssetTypeId] 
	INNER JOIN [AssetTypes] AT1 ON AT1.[Id] = ATR.[ChildAssetTypeId] 
	WHERE ATR.[Deleted] IS NULL AND AT0.[Deleted] IS NULL AND AT1.[Deleted] IS NULL 

	UNION ALL

	SELECT AT2.[Name] AS [Parent], ATR2.[ParentAssetTypeId], AT3.[Name] AS [Child], CTE.[ChildAssetTypeId]
	FROM [AssetTypeRelations] ATR2
	INNER JOIN AssetType_CTE cte on ATR2.[ChildAssetTypeId] = cte.ParentAssetTypeId
	INNER JOIN [AssetTypes] AT2 ON AT2.[Id] = cte.[ParentAssetTypeId] 
	INNER JOIN [AssetTypes] AT3 ON AT3.[Id] = cte.[ChildAssetTypeId] 
	WHERE ATR2.[Deleted] IS NULL AND AT2.[Deleted] IS NULL AND AT3.[Deleted] IS NULL 
)

SELECT [ChildAssetTypeId] FROM AssetType_CTE
WHERE [ParentAssetTypeId] = @Id
ORDER BY [Child];
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================

--* ==================================================

CREATE PROCEDURE [dbo].[spr_AssetType_HasAssets]
	@AssetTypeId	uniqueidentifier,
	@IsInstance	bit
AS

SELECT
	COUNT([Id])
FROM [Assets] WITH (NoLock)
WHERE
	([AssetTypeId] = @AssetTypeId)
		AND
	([IsInstance] = @IsInstance)
		AND
	([Deleted] IS NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetType_Insert
--*	Inserts a new record for a new AssetType.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @CreatedBy

CREATE PROCEDURE [dbo].[spr_AssetType_Insert]
	@Id						uniqueidentifier,
	@Name					varchar(100),
	@Pluralization			varchar(100) = null,
	@DefaultViewId			uniqueidentifier = null,
	@DefaultInstanceViewId	uniqueidentifier = null,
	@DefinitionLabel		varchar(50) = null,
	@InstanceLabel			varchar(50) = null,
	@ImageId				uniqueidentifier = null,
	@AllowAssets			bit,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier
AS

INSERT INTO [dbo].[AssetTypes] (
	[Id],
	[Name],
	[Pluralization],
	[DefaultViewId],
	[DefaultInstanceViewId],
	[DefinitionLabel],
	[InstanceLabel],
	[ImageId],
	[AllowAssets],
	[Created],
	[CreatedBy])
VALUES (
	@Id,
	@Name,
	@Pluralization,
	@DefaultViewId,
	@DefaultInstanceViewId,
	@DefinitionLabel,
	@InstanceLabel,
	@ImageId,
	@AllowAssets,
	@Created,
	@CreatedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO



CREATE PROCEDURE [dbo].[spr_AssetType_Kill]
	@AssetTypeId	uniqueidentifier
AS

delete from AssetTypeRelations where ChildAssetTypeId = @AssetTypeId
delete from AssetTypesProperties where AssetTypeId = @AssetTypeId
delete from RolesAssetTypes where AssetTypeId = @AssetTypeId
delete from RolesAssetTypesFilters where AssetTypeId = @AssetTypeId
delete from RolesAssetTypesPermissions where AssetTypeId = @AssetTypeId

delete from AssetTypes where Id  = @AssetTypeId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetType_Save
--*	Saves an instance of AssetType.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @CreatedBy

CREATE PROCEDURE [dbo].[spr_AssetType_Save]
	@Id						uniqueidentifier,
	@Name					varchar(100),
	@Pluralization			varchar(100) = null,
	@DefaultViewId			uniqueidentifier = null,
	@DefaultInstanceViewId	uniqueidentifier = null,
	@DefinitionLabel		varchar(50) = null,
	@InstanceLabel			varchar(50) = null,
	@ImageId				uniqueidentifier = null,
	@AllowAssets			bit,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier,
	@Deleted				smalldatetime = null,
	@DeletedBy				uniqueidentifier = null
AS


IF EXISTS (SELECT * FROM [dbo].[AssetTypes] WHERE [Id] = @Id)

	EXEC [dbo].[spr_AssetType_Update] @Id, @Name, @Pluralization, @DefaultViewId, @DefaultInstanceViewId, @DefinitionLabel, @InstanceLabel, @ImageId, @AllowAssets, @Created, @CreatedBy, @Deleted, @DeletedBy 

ELSE

	EXEC [dbo].[spr_AssetType_Insert] @Id, @Name, @Pluralization, @DefaultViewId, @DefaultInstanceViewId, @DefinitionLabel, @InstanceLabel, @ImageId, @AllowAssets, @Created, @CreatedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_AssetType_Update
--*	Updates an existing record for a/an AssetType.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @CreatedBy

CREATE PROCEDURE [dbo].[spr_AssetType_Update]
	@Id						uniqueidentifier,
	@Name					varchar(100),
	@Pluralization			varchar(100) = null,
	@DefaultViewId			uniqueidentifier = null,
	@DefaultInstanceViewId	uniqueidentifier = null,
	@DefinitionLabel		varchar(50) = null,
	@InstanceLabel			varchar(50) = null,
	@ImageId				uniqueidentifier = null,
	@AllowAssets			bit,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier,
	@Deleted				smalldatetime = null,
	@DeletedBy				uniqueidentifier = null
AS

UPDATE [dbo].[AssetTypes] SET
	[Name] = @Name,
	[Pluralization] = @Pluralization,
	[DefaultViewId] = @DefaultViewId,
	[DefaultInstanceViewId] = @DefaultInstanceViewId,
	[DefinitionLabel] = @DefinitionLabel,
	[InstanceLabel] = @InstanceLabel,
	[ImageId] = @ImageId,
	[AllowAssets] = @AllowAssets,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[AssetTypes].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetTypes_ByMember_Get]
	@MemberId	uniqueidentifier
AS

SELECT DISTINCT
	AT.Id,
	V.[IsInstance]
FROM [RoleMembers] RM
	INNER JOIN [RolesViews] RV ON RV.[RoleId] = [RM].[RoleId]
	INNER JOIN [Roles] R ON R.[Id] = RM.[RoleId]
	INNER JOIN [RoleAssetTypes] RAT ON RAT.[RoleId] = R.[Id]
	INNER JOIN [AssetTypes] AT ON AT.Id = RAT.[AssetTypeId]
	INNER JOIN [Views] V ON V.[Id] = RV.[ViewId]
WHERE
	R.[Deleted] is null
	and
	R.[IsActive] = 1
	and
	AT.[Deleted] is null
	and
	RM.[MemberId] = @MemberId
--ORDER BY AT.[Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_AssetTypes_GetDictionary]
	@IncludeUnused	bit -- whether or not 'placeholder' AssetTypes and AssetTypes w/o assets should be returned
AS

IF (@IncludeUnused = 0)
	SELECT DISTINCT
		AT.[Id], AT.[Name]
	FROM [AssetTypes] AT WITH (NoLock)
		INNER JOIN [Assets] A ON A.[AssetTypeId] = AT.[Id] 
	WHERE
		(AT.[Deleted] IS NULL)
		AND (AT.[AllowAssets] = 1)
	ORDER BY AT.[Name] 
ELSE
	SELECT [Id], [Name] FROM [AssetTypes] WITH (NoLock) WHERE [Deleted] IS NULL ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Asset_Delete
--*	Deletes the record for a/an Asset.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_Asset_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[Assets] 
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy 
WHERE [Id] = @Id

DELETE FROM [AssetClients] WHERE [AssetId] = @Id

-- TODO: This asset might be cached in a picklist somewhere
-- TODO: If so, we would need to delete those items from ClientCache
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Asset_Get
--*	Retrieves an instance of Asset based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Asset_Get]
	@Id	uniqueidentifier
AS

SELECT
	[AssetTypeId] AS [AssetTypeId],
	[Name] AS [Name],
	[DisplayValue] AS [DisplayValue],
	[Description] AS [Description],
	[IsInstance] AS [IsInstance],
	[InstanceOfId] AS [InstanceOfId],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[LastModified] AS [LastModified],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[LastModifiedBy] AS [LastModifiedBy],
	[Deleted] AS [Deleted],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[Assets] WITH (NoLock)
WHERE
	[dbo].[Assets].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_GetAssetTypeId]
	@Id	uniqueidentifier
AS

SELECT [AssetTypeId] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_GetDescription]
	@Id	uniqueidentifier
AS

SELECT [Description] FROM [Assets] WITH (NoLock)
WHERE
	[Id] = @Id AND
	[Deleted] IS NULL AND
	[DeletedBy] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_GetId]
	@Name			varchar(MAX),
	@AssetTypeId	uniqueidentifier
AS

SELECT
	[Id]
FROM [Assets] WITH (NoLock)
WHERE
	(
		([Name] = @Name) OR ([DisplayValue] = @Name)
	)
	AND [AssetTypeId] = @AssetTypeId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_Get_Created]
	@Id	uniqueidentifier
AS

SELECT [Created] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_Get_CreatedBy]
	@Id	uniqueidentifier
AS

SELECT [CreatedBy] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_Get_DisplayValue]
	@Id	uniqueidentifier
AS

SELECT [DisplayValue] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_Get_InstanceOfId]
	@Id	uniqueidentifier
AS

SELECT [InstanceOfId] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_Get_Name]
	@Id	uniqueidentifier
AS

SELECT [Name] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Asset_Insert
--*	Inserts a new record for a new Asset.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Asset_Insert]
	@Id					uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@Name				varchar(255),
	@DisplayValue		varchar(100) = null,
	@Description		varchar(MAX) = null,
	@IsInstance			bit = 0,
	@InstanceOfId		uniqueidentifier = null,
	@Created			smalldatetime,
	@Approved			smalldatetime = null,
	@LastModified		smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@ApprovedBy			uniqueidentifier = null,
	@LastModifiedBy		uniqueidentifier = null,
	@DeletedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[Assets] (
	[Id],
	[AssetTypeId],
	[Name],
	[DisplayValue],
	[Description],
	[IsInstance],
	[InstanceOfId],
	[Created],
	[Approved],
	[LastModified],
	[CreatedBy],
	[ApprovedBy],
	[LastModifiedBy],
	[DeletedBy])
VALUES (
	@Id,
	@AssetTypeId,
	@Name,
	@DisplayValue,
	@Description,
	@IsInstance,
	@InstanceOfId,
	@Created,
	@Approved,
	@LastModified,
	@CreatedBy,
	@ApprovedBy,
	@LastModifiedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_IsInstance]
	@Id	uniqueidentifier
AS

	SELECT [IsInstance]
	FROM [Assets] WITH (NoLock)
	WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Asset_Kill]
	@AssetId	uniqueidentifier
AS

DELETE FROM [PropertyValues] WHERE [AssetId] = @AssetId
DELETE FROM [AssetRelations] WHERE [FromAssetId] = @AssetId
DELETE FROM [AssetRelations] WHERE [ToAssetId] = @AssetId
DELETE FROM [Assets] WHERE [Id] = @AssetId
DELETE FROM [AssetClients] WHERE [AssetId] = @AssetId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Asset_Save
--*	Saves an instance of Asset.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Asset_Save]
	@Id					uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@Name				varchar(255),
	@DisplayValue		varchar(100) = null,
	@Description		varchar(MAX) = null,
	@IsInstance			bit = 0,
	@InstanceOfId		uniqueidentifier = null,
	@Created			smalldatetime,
	@Approved			smalldatetime = null,
	@LastModified		smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@ApprovedBy			uniqueidentifier = null,
	@LastModifiedBy		uniqueidentifier = null,
	@DeletedBy			uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[Assets] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Asset_Update] @Id, @AssetTypeId, @Name, @DisplayValue, @Description, @IsInstance, @InstanceOfId, @Created, @Approved, @LastModified, @CreatedBy, @ApprovedBy, @LastModifiedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_Asset_Insert] @Id, @AssetTypeId, @Name, @DisplayValue, @Description, @IsInstance, @InstanceOfId, @Created, @Approved, @LastModified, @CreatedBy, @ApprovedBy, @LastModifiedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Asset_Update
--*	Updates an existing record for a/an Asset.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Asset_Update]
	@Id					uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@Name				varchar(255),
	@DisplayValue		varchar(100) = null,
	@Description		varchar(MAX) = null,
	@IsInstance			bit = 0,
	@InstanceOfId		uniqueidentifier = null,
	@Created			smalldatetime,
	@Approved			smalldatetime = null,
	@LastModified		smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@ApprovedBy			uniqueidentifier = null,
	@LastModifiedBy		uniqueidentifier = null,
	@DeletedBy			uniqueidentifier = null
AS

UPDATE [dbo].[Assets] SET
	[AssetTypeId] = @AssetTypeId,
	[Name] = @Name,
	[DisplayValue] = @DisplayValue,
	[Description] = @Description,
	[IsInstance] = @IsInstance,
	[InstanceOfId] = @InstanceOfId,
	[Created] = @Created,
	[Approved] = @Approved,
	[LastModified] = @LastModified,
	[CreatedBy] = @CreatedBy,
	[ApprovedBy] = @ApprovedBy,
	[LastModifiedBy] = @LastModifiedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[Assets].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Assets_ChangeAssetType]
	@NewAssetTypeId uniqueidentifier,
	@InstanceOfId uniqueidentifier
AS

UPDATE [Assets]
SET [AssetTypeId] = @NewAssetTypeId
WHERE
	[InstanceOfId] = @InstanceOfId AND
	[AssetTypeId] != @NewAssetTypeId AND
	[IsInstance] = 1
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Assets_GetByInstanceOfId]
	@InstanceOfId uniqueidentifier
AS

	SELECT
		[Id],
		IsNull([DisplayValue], [Name]) AS [Name]
	FROM [Assets] WITH (NoLock)
	WHERE 
		([InstanceOfId] = @InstanceOfId) AND 
		([Deleted] IS NULL) AND
		([Approved] IS NOT NULL) AND
		([IsInstance] = 1)
	ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Assets_GetDictionaryByNames]
	@AssetNames		varchar(MAX),
	@AssetTypeIds	varchar(MAX),
	@IsInstance		bit
AS

SELECT
	A.[Id],
	ISNULL(A.[DisplayValue], A.[Name]) AS [Name]
FROM [Assets] A WITH (NoLock)
WHERE
	(
		A.[Name] IN (SELECT [Value] FROM dbo.SplitAsString(@AssetNames, ',')) OR
		A.[DisplayValue] IN (SELECT [Value] FROM dbo.SplitAsString(@AssetNames, ','))
	)
	AND A.[AssetTypeId] IN (SELECT [Value] FROM dbo.Split(@AssetTypeIds, ','))
	AND A.[IsInstance] = @IsInstance
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Assets_GetFromPropertyValue]
	@PropertyId uniqueidentifier
AS

SELECT DISTINCT
	A.[Id],
	IsNull(A.[DisplayValue], A.[Name]) AS [Name]
FROM [PropertyValues] PV WITH (NoLock)
	INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = cast([PV].[Value] as varchar(36))
WHERE
	PV.[Value] IS NOT NULL AND PV.[Value] <> '' AND
	PV.[PropertyId] = @PropertyId AND
	PV.[Deleted] IS NULL AND
	PV.[Approved] IS NOT NULL AND
	PV.[Rejected] IS NULL AND
	A.[Deleted] IS NULL AND
	A.[Approved] IS NOT NULL 
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Assets_GetInPropertyValue]
	@PropertyId		uniqueidentifier,
	@AssetTypeId	uniqueidentifier
AS
-- want a list of all companies who appear as a software author for a contract
-- want a list of all Assets who appear as a propertyValue for an assetType
SELECT DISTINCT 
	A2.[Id],
	A2.[Name]
FROM [Assets] A WITH (NoLock)
	INNER JOIN [PropertyValues] PV WITH (NoLock) ON PV.[AssetId] = A.[Id]
	INNER JOIN [Assets] A2 WITH (NoLock) ON A2.[Id] = CAST(PV.[Value] AS uniqueidentifier)
WHERE PV.[PropertyId] = @PropertyId
	AND PV.[Deleted] IS NULL
	AND PV.[Approved] IS NOT NULL
	AND A.[Deleted] IS NULL
	AND A2.[Deleted] IS NULL
	AND PV.[Value] IS NOT NULL AND PV.[Value] != ''
	and A2.[AssetTypeId] = @AssetTypeId
ORDER BY A2.[Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Bookmark_Delete]
	@UserId		uniqueidentifier,
	@AssetId	uniqueidentifier = null,
	@ReportId	uniqueidentifier = null,
	@ViewId		uniqueidentifier = null
AS

IF (@AssetId IS NULL AND @ViewId IS NULL)
	DELETE FROM [MemberBookmarks] WHERE ([MemberId] = @UserId) AND ([ReportId] = @ReportId)
ELSE
	DELETE FROM [MemberBookmarks] WHERE ([AssetId] = @AssetId) AND ([ViewId] = @ViewId)
	
-- Mark the user as having been updated so web clients will get changes
UPDATE [Members] SET [LastModified] = GETDATE(), [LastModifiedBy] = @UserId WHERE [Id] = @UserId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Bookmark_ToggleHome]
	@Id		uniqueidentifier
AS

DECLARE @ShowOnHome int
DECLARE @MemberId uniqueidentifier

SELECT @ShowOnHome = [ShowOnHome] FROM [MemberBookmarks] WHERE [Id] = @Id
SELECT @MemberId = [MemberId] FROM [MemberBookmarks] WHERE [Id] = @Id

IF @ShowOnHome = 0
	UPDATE [MemberBookmarks] SET [ShowOnHome] = 1 WHERE [Id] = @Id
ELSE
	UPDATE [MemberBookmarks] SET [ShowOnHome] = 0 WHERE [Id] = @Id

-- Mark the user as having been updated so web clients will get changes
UPDATE [Members] SET [LastModified] = GETDATE(), [LastModifiedBy] = [Id] WHERE [Id] = @MemberId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Bookmarks_Get]
	@MemberId	uniqueidentifier
AS

SELECT
	MB.[Id] AS [Id]
	,AT.[Id] AS [AssetTypeId]
	,AT.[Pluralization] AS [AssetType]
	,V.[Id] AS [ViewId]
	,A.Id AS [AssetId]
	,IsNull(A.[DisplayValue], A.[Name]) + IsNull(dbo.GetBookmarkCaption(A.Id), '') AS [Asset]
	,'00000000-0000-0000-0000-000000000000' AS [ReportId]
	,MB.[ShowOnHome] AS [ShowOnHome]
	,null AS [Report]
FROM [MemberBookmarks] MB WITH (NoLock)
	INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = MB.[AssetId]
	INNER JOIN [AssetTypes] AT WITH (NoLock) ON AT.[Id] = A.[AssetTypeId]
	INNER JOIN [Views] V WITH (NoLock) ON V.[Id] = MB.[ViewId]
WHERE
	(MB.[MemberId] = @MemberId)
	AND ([A].[Deleted] IS NULL)
	AND ([V].[Deleted] IS NULL)

UNION --join the bookmarked reports in

SELECT
	MB.[Id] AS [Id]
	,'00000000-0000-0000-0000-000000000000' AS [AssetTypeId]
	,'Reports' AS [AssetType]
	,'00000000-0000-0000-0000-000000000000' AS [ViewId]
	,'00000000-0000-0000-0000-000000000000' AS [AssetId]
	,null AS [Asset]
	,R.[Id] AS [ReportId]
	,MB.[ShowOnHome] AS [ShowOnHome]
	,R.[ReportName] AS [Report]
FROM [MemberBookmarks] MB WITH (NoLock)
	INNER JOIN [Reports] R WITH (NoLock) ON R.[Id] = MB.[ReportId]
WHERE
	(MB.[MemberId] = @MemberId)
	AND (R.[Deleted] IS NULL)

ORDER BY [AssetType], [Asset], [Report]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Bookmarks_GetShortcuts]
	@MemberId	uniqueidentifier
AS

SELECT
	AT.[Id] AS [AssetTypeId],
	AT.[Pluralization] AS [AssetType],
	V.[Id] AS [ViewId],
	A.Id AS [AssetId],
	IsNull(A.[DisplayValue], A.[Name]) + IsNull(dbo.GetBookmarkCaption(A.Id), '') AS [Asset],
	'00000000-0000-0000-0000-000000000000' AS [ReportId],
	MB.[ShowOnHome] AS [ShowOnHome],
	null AS [Report]
FROM [MemberBookmarks] MB WITH (NoLock)
	INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = MB.[AssetId]
	INNER JOIN [AssetTypes] AT WITH (NoLock) ON AT.[Id] = A.[AssetTypeId]
	INNER JOIN [Views] V WITH (NoLock) ON V.[Id] = MB.[ViewId]
WHERE MB.[MemberId] = @MemberId AND MB.[ShowOnHome] = 1

UNION --join the bookmarked reports in

SELECT
	'00000000-0000-0000-0000-000000000000' AS [AssetTypeId],
	'Reports' AS [AssetType],
	'00000000-0000-0000-0000-000000000000' AS [ViewId],
	'00000000-0000-0000-0000-000000000000' AS [AssetId],
	null AS [Asset],
	R.[Id] AS [ReportId],
	MB.[ShowOnHome] AS [ShowOnHome],
	R.[ReportName] AS [Report]
FROM [MemberBookmarks] MB WITH (NoLock)
	INNER JOIN [Reports] R WITH (NoLock) ON R.[Id] = MB.[ReportId]
WHERE
	MB.[MemberId] = @MemberId AND MB.[ShowOnHome] = 1
	AND R.[Deleted] IS NULL

ORDER BY [AssetType], [Asset], [Report]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	spr_BulkUpdateLog_Save
--* ==================================================

CREATE PROCEDURE [dbo].[spr_BulkUpdateLog_Save]
	@BulkUploadId	uniqueidentifier,
	@Category		varchar(MAX),
	@AssetId		uniqueidentifier = null,
	@Message		varchar(MAX),
	@Order			int
AS

INSERT INTO [BulkUploadLogs] ([BulkUploadId], [Category], [AssetId], [Message], [Created], [Order])
VALUES (@BulkUploadId, @Category, @AssetId, @Message, GetDate(), @Order)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_BulkUploadLogsLimited_GetAsc]
	@BulkUploadId	uniqueidentifier,
	@StartPos int
AS

SELECT BUL.[Id], BUL.[Category], BUL.[AssetId], A.[Name], A.[AssetTypeId], A.[IsInstance], BUL.[Message], BUL.[Order]
FROM [BulkUploadLogs] BUL WITH (NoLock)
LEFT JOIN [Assets] A WITH (NoLock) ON A.[Id] = BUL.[AssetId]
WHERE BUL.[BulkUploadId] = @BulkUploadId
AND BUL.[Order] > @StartPos
ORDER BY BUL.[Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_BulkUploadLogsLimited_GetDesc]
	@BulkUploadId	uniqueidentifier,
	@StartPos int
AS

SELECT BUL.[Id], BUL.[Category], BUL.[AssetId], A.[Name], A.[AssetTypeId], A.[IsInstance], BUL.[Message], BUL.[Order]
FROM [BulkUploadLogs] BUL WITH (NoLock)
LEFT JOIN [Assets] A WITH (NoLock) ON A.[Id] = BUL.[AssetId]
WHERE BUL.[BulkUploadId] = @BulkUploadId
AND BUL.[Order] > @StartPos
ORDER BY BUL.[Order] DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_BulkUploadLogs_GetAsc]
	@BulkUploadId	uniqueidentifier
AS

SELECT BUL.[Id], BUL.[Category], BUL.[AssetId], A.[Name], A.[AssetTypeId], A.[IsInstance], BUL.[Message], BUL.[Order]
FROM [BulkUploadLogs] BUL WITH (NoLock)
LEFT JOIN [Assets] A WITH (NoLock) ON A.[Id] = BUL.[AssetId]
WHERE BUL.[BulkUploadId] = @BulkUploadId
ORDER BY BUL.[Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_BulkUploadLogs_GetDesc]
	@BulkUploadId	uniqueidentifier
AS

SELECT BUL.[Id], BUL.[Category], BUL.[AssetId], A.[Name], A.[AssetTypeId], A.[IsInstance], BUL.[Message], BUL.[Order]
FROM [BulkUploadLogs] BUL WITH (NoLock)
LEFT JOIN [Assets] A WITH (NoLock) ON A.[Id] = BUL.[AssetId]
WHERE BUL.[BulkUploadId] = @BulkUploadId
ORDER BY BUL.[Order] DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_BulkUploadTemplatePropertyIds_Get]
	@Id	uniqueidentifier
AS

SELECT [PropertyId]
FROM [BulkUploadTemplatesProperties] WITH (NoLock)
WHERE [BulkUploadTemplateId] = @Id
ORDER BY [Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_BulkUploads_Get]
	@MemberId	uniqueidentifier = null
AS

IF (@MemberId IS NULL)
	BEGIN
		SELECT
			BU.[Id],
			D.[Name],
			M.[LName],
			M.[FName],
			M.[UserId],
			BU.[Completed],
			BU.[RecordCount],
			BU.[Created]
		FROM [BulkUploads] BU WITH (NoLock)
			INNER JOIN [Documents] D WITH (NoLock) ON D.[Id] = BU.[FileId]
			INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = BU.[CreatedBy]
		ORDER BY BU.[Created] DESC	
	END
ELSE
	BEGIN
		SELECT
			BU.[Id],
			D.[Name],
			M.[LName],
			M.[FName],
			M.[UserId],
			BU.[Completed],
			BU.[RecordCount],
			BU.[Created]
		FROM [BulkUploads] BU WITH (NoLock)
			INNER JOIN [Documents] D WITH (NoLock) ON D.[Id] = BU.[FileId]
			INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = BU.[CreatedBy]
		WHERE M.[Id] = @MemberId
		ORDER BY BU.[Created] DESC	
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ChartData_Get]
	@ChartId		uniqueidentifier,
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN
		select TOP (@MaxRecords)
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC, [Item]
	END
ELSE
	BEGIN
		select
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Item]
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ChartData_Get_Asset]
	@ChartId		uniqueidentifier,
	@PropertyId		uniqueidentifier,
	@AssetTypeId	uniqueidentifier,
	@IsInstance		bit,
	@Values			varchar(max),
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN

		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select TOP (@MaxRecords)
                @ChartId,
                pv.Value,
                dbo.GetAssetValueById(pv.Value),
                a.Id,
                a.Name
            from Assets a WITH (NoLock)
                inner join PropertyValues pv on pv.AssetId = a.Id
            where
                a.AssetTypeId = @AssetTypeId
                and pv.PropertyId = @PropertyId
                and pv.Deleted is null
                and a.IsInstance = @IsInstance
                and a.Deleted is null

		select TOP (@MaxRecords)
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC
		--select TOP (@MaxRecords)
		--	dbo.GetAssetValueById(pv.Value) as [Key],
		--	COUNT(a.Id) AS [Value]
		--from Assets a
		--	inner join PropertyValues pv on pv.AssetId = a.Id
		--where
		--	a.AssetTypeId = @AssetTypeId
		--	and a.id IN (SELECT [Value] FROM dbo.Split(@Values, ','))
		--	and pv.PropertyId = @PropertyId 
		--	and pv.Deleted is null
		--	and a.IsInstance = @IsInstance
		--	and a.Deleted is null
		--group by pv.Value
		--ORDER BY [Value] DESC
	END
ELSE
	BEGIN

		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select
                @ChartId,
                pv.Value,
                dbo.GetAssetValueById(pv.Value),
                a.Id,
                a.Name
            from Assets a WITH (NoLock)
                inner join PropertyValues pv on pv.AssetId = a.Id
            where
                a.AssetTypeId = @AssetTypeId
                and pv.PropertyId = @PropertyId
                and pv.Deleted is null
                and a.IsInstance = @IsInstance
                and a.Deleted is null

		select
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Item]
               		
		--select
		--	dbo.GetAssetValueById(pv.Value) as [Key],
		--	COUNT(a.Id) AS [Value]
		--from Assets a
		--	inner join PropertyValues pv on pv.AssetId = a.Id
		--where
		--	a.AssetTypeId = @AssetTypeId
		--	and a.id IN (SELECT [Value] FROM dbo.Split(@Values, ','))
		--	and pv.PropertyId = @PropertyId 
		--	and pv.Deleted is null
		--	and a.IsInstance = @IsInstance
		--	and a.Deleted is null
		--group by pv.Value
		--ORDER BY [Key]

	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ChartData_Get_AssetInherited]
	@ChartId		uniqueidentifier,
	@PropertyId		uniqueidentifier,
	@AssetTypeId	uniqueidentifier,
	@IsInstance		bit,
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select TOP (@MaxRecords)
                @ChartId,
                pv.Value,
				--dbo.GetAssetValueById(pv.Value) as [Key],
				dbo.GetPropertyValueLookup(a2.Id, @PropertyId) AS [Key],
                a2.Id,
                a2.Name
			from Assets a
				inner join Assets A2 on A2.Id = a.InstanceOfId
				inner join PropertyValues pv on pv.AssetId = a2.Id
			where
				a.AssetTypeId = @AssetTypeId
				and pv.PropertyId = @PropertyId 
				and pv.Deleted is null
				and pv.Value <> ''
				and pv.Value IS NOT NULL
				and a.IsInstance = @IsInstance
				and a.Deleted is null

		select TOP (@MaxRecords)
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC
	END
ELSE
	BEGIN
	
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select
                @ChartId,
                pv.Value,
				--dbo.GetAssetValueById(pv.Value) as [Key],
				dbo.GetPropertyValueLookup(a2.Id, @PropertyId) AS [Key],
                a2.Id,
                a2.Name
			from Assets a
				inner join Assets A2 on A2.Id = a.InstanceOfId
				inner join PropertyValues pv on pv.AssetId = a2.Id
			where
				a.AssetTypeId = @AssetTypeId
				and pv.PropertyId = @PropertyId 
				and pv.Deleted is null
				and pv.Value <> ''
				and pv.Value IS NOT NULL
				and a.IsInstance = @IsInstance
				and a.Deleted is null

		select
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Item]
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ChartData_Get_AssetInherited_Filtered]
	@ChartId		uniqueidentifier,
	@PropertyId		uniqueidentifier,
	@AssetIds		VARCHAR(MAX),
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select TOP (@MaxRecords)
                @ChartId,
                pv.Value,
				dbo.GetPropertyValueLookup(a2.Id, @PropertyId) AS [Key],
                a2.Id,
                a2.Name
			from Assets a
				inner join Assets A2 on A2.Id = a.InstanceOfId
				inner join PropertyValues pv on pv.AssetId = a2.Id
			where
				a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
				and pv.PropertyId = @PropertyId 
				and pv.Deleted is null
				and pv.Value <> ''
				and pv.Value IS NOT NULL

		select TOP (@MaxRecords)
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC
	END
ELSE
	BEGIN
	
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select
                @ChartId,
                pv.Value,
				dbo.GetPropertyValueLookup(a2.Id, @PropertyId) AS [Key],
                a2.Id,
                a2.Name
			from Assets a
				inner join Assets A2 on A2.Id = a.InstanceOfId
				inner join PropertyValues pv on pv.AssetId = a2.Id
			where
				a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
				and pv.PropertyId = @PropertyId 
				and pv.Deleted is null
				and pv.Value <> ''
				and pv.Value IS NOT NULL

		select
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Item]
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_ChartData_Get_Asset_Filtered]
	@ChartId		uniqueidentifier,
	@PropertyId		uniqueidentifier,
	@AssetIds		VARCHAR(MAX),
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select TOP (@MaxRecords)
				@ChartId,
				pv.Value,
				dbo.GetAssetValueById(pv.Value) as [Key],
				pv.Value,
				COUNT(a.Id)
			from Assets a
				inner join PropertyValues pv on pv.AssetId = a.Id
			where
				a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
				and pv.PropertyId = @PropertyId 
				and pv.Deleted is null
				and pv.Value is not null
			group by pv.Value

		select TOP (@MaxRecords)
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC

	END
ELSE
	BEGIN
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select
				@ChartId,
				pv.Value,
				dbo.GetAssetValueById(pv.Value) as [Key],
				pv.Value,
				COUNT(a.Id)
			from Assets a
				inner join PropertyValues pv on pv.AssetId = a.Id
			where
				a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
				and pv.PropertyId = @PropertyId 
				and pv.Deleted is null
				and pv.Value is not null
			group by pv.Value

		select
			[Item],
			--COUNT(ValueId) AS [Value]
			[Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		--group by [Item]
		ORDER BY [Item]
		--select
		--	dbo.GetAssetValueById(pv.Value) as [Key],
		--	COUNT(a.Id) AS [Value]
		--from Assets a
		--	inner join PropertyValues pv on pv.AssetId = a.Id
		--where
		--	a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
		--	and pv.PropertyId = @PropertyId 
		--	and pv.Deleted is null
		--	and pv.Value is not null
		--group by pv.Value
		--ORDER BY [Key]
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_ChartData_Get_InstanceOf]
	@ChartId		uniqueidentifier,
	@AssetTypeId	uniqueidentifier,
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select TOP (@MaxRecords)
                @ChartId,
                a.InstanceOfId,
				dbo.GetAssetValueById(a2.Id) as [Key],
                a.Id,
                a.Name
			from Assets a
				inner join Assets A2 on A2.Id = a.InstanceOfId
			where
				a.AssetTypeId = @AssetTypeId
				and a.IsInstance = 1
				and a.Deleted is null

		select TOP (@MaxRecords)
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC
	END
ELSE
	BEGIN
	
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select
                @ChartId,
                a.InstanceOfId,
				dbo.GetAssetValueById(a2.Id) as [Key],
                a.Id,
                a.Name
			from Assets a
				inner join Assets A2 on A2.Id = a.InstanceOfId
			where
				a.AssetTypeId = @AssetTypeId
				and a.IsInstance = 1
				and a.Deleted is null

		select
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Item]
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ChartData_Get_InstanceOf_Filtered]
	@ChartId		uniqueidentifier,
	@AssetIds		VARCHAR(MAX),
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select TOP (@MaxRecords)
                @ChartId,
                a.InstanceOfId,
				dbo.GetAssetValueById(a2.Id) as [Key],
                a.Id,
                a.Name
			from Assets a
				inner join Assets A2 on A2.Id = a.InstanceOfId
			where
				a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
				and a.IsInstance = 1
				and a.Deleted is null

		select TOP (@MaxRecords)
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC
	END
ELSE
	BEGIN
	
		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select
                @ChartId,
                a.InstanceOfId,
				dbo.GetAssetValueById(a2.Id) as [Key],
                a.Id,
                a.Name
			from Assets a
				inner join Assets A2 on A2.Id = a.InstanceOfId
			where
				a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
				and a.IsInstance = 1
				and a.Deleted is null

		select
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ChartData_Get_PropVal]
	@ChartId		uniqueidentifier,
	@PropertyId		uniqueidentifier,
	@AssetTypeId	uniqueidentifier,
	@IsInstance		bit,
	@Values			varchar(max),
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN

		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select TOP (@MaxRecords)
				@ChartId,
				pv.Value,
				dbo.GetPickListValueById(pv.Value) as [Key],
				a.[Id] AS [Value],
				a.[Name]
            from Assets a
				inner join PropertyValues pv on pv.AssetId = a.Id
            where
				a.AssetTypeId = @AssetTypeId
				and pv.Value IN (SELECT [Value] FROM dbo.Split(@Values, ','))
				and pv.PropertyId = @PropertyId
				and pv.Deleted is null
				and a.IsInstance = @IsInstance
				and a.Deleted is null
            ORDER BY [Key]
           
		select TOP (@MaxRecords)
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC

		--select TOP (@MaxRecords)
		--	dbo.GetPickListValueById(pv.Value) as [Key],
		--	COUNT(a.Id) AS [Value]
		--from Assets a
		--	inner join PropertyValues pv on pv.AssetId = a.Id
		--where
		--	a.AssetTypeId = @AssetTypeId 
		--	and pv.Value IN (SELECT [Value] FROM dbo.Split(@Values, ','))
		--	and pv.PropertyId = @PropertyId 
		--	and pv.Deleted is null
		--	and a.IsInstance = @IsInstance
		--	and a.Deleted is null
		--group by pv.Value
		--ORDER BY [Value] DESC
	END
ELSE
	BEGIN

		insert into [ChartData] ([ChartId], [ItemId], [Item], [ValueId], [Value])
			select
				@ChartId,
				pv.Value,
				dbo.GetPickListValueById(pv.Value) as [Key],
				a.[Id] AS [Value],
				a.[Name]
            from Assets a
				inner join PropertyValues pv on pv.AssetId = a.Id
            where
				a.AssetTypeId = @AssetTypeId
				and pv.Value IN (SELECT [Value] FROM dbo.Split(@Values, ','))
				and pv.PropertyId = @PropertyId
				and pv.Deleted is null
				and a.IsInstance = @IsInstance
				and a.Deleted is null
            ORDER BY [Key]

		select
			[Item],
			COUNT(ValueId) AS [Value]
		from [ChartData] CD
		where CD.[ChartId] = @ChartId		
		group by [Item]
		ORDER BY [Value] DESC
		
		--select
		--	dbo.GetPickListValueById(pv.Value) as [Key],
		--	COUNT(a.Id) AS [Value]
		--from Assets a
		--	inner join PropertyValues pv on pv.AssetId = a.Id
		--where
		--	a.AssetTypeId = @AssetTypeId 
		--	and pv.Value IN (SELECT [Value] FROM dbo.Split(@Values, ','))
		--	and pv.PropertyId = @PropertyId 
		--	and pv.Deleted is null
		--	and a.IsInstance = @IsInstance
		--	and a.Deleted is null
		--group by pv.Value
		--ORDER BY [Key]
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ChartData_Get_PropVal_Filtered]
	@PropertyId		uniqueidentifier,	
	@AssetIds		VARCHAR(MAX),
	@MaxRecords		int
AS

IF (@MaxRecords > 0)
	BEGIN
		select TOP (@MaxRecords)
			dbo.GetPickListValueById(pv.Value) as [Key],
			COUNT(a.Id) AS [Value]
		from Assets a
			inner join PropertyValues pv on pv.AssetId = a.Id
		where
			a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
			and pv.PropertyId = @PropertyId 
			and pv.Value is not null
			and pv.Deleted is null
		group by pv.Value
		ORDER BY [Value] DESC
	END
ELSE
	BEGIN
		select
			dbo.GetPickListValueById(pv.Value) as [Key],
			COUNT(a.Id) AS [Value]
		from Assets a
			inner join PropertyValues pv on pv.AssetId = a.Id
		where
			a.id IN (SELECT [Value] FROM dbo.Split(@AssetIds, ','))
			and pv.PropertyId = @PropertyId
			and pv.Value is not null
			and pv.Deleted is null
		group by pv.Value
		ORDER BY [Key]
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ChartProperties_Get]
	@Id		uniqueidentifier
AS

	SELECT CP.[Id],
		CP.[PropertyId],
		CP.[SubPropertyId],
		CP.[Order],
		CP.[IsGrouping],
		CP.[CustomReportFieldTypeId],
		CP.[TimeElapsedTypeId],
		CP.[TimeElapsedFormatId],
		CP.[TimeElapsedSpecificDate],
		CP.[Label],
		ISNULL(P.[DisplayValue], P.[Name]) AS [Property],
		ISNULL(P2.[DisplayValue], P2.[Name]) AS [SubProperty],
		CP.[Logic]
	FROM [ChartsProperties] CP WITH (NoLock)
		LEFT JOIN [Properties] P WITH (NoLock) ON P.[Id] = CP.[PropertyId]
		LEFT JOIN [Properties] P2 WITH (NoLock) ON P2.[Id] = CP.[SubPropertyId]
	WHERE
		CP.[ChartId] = @Id AND
		P.[Deleted] IS NULL
	ORDER BY CP.[Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_ChartProperty_Insert]
	@Id							uniqueidentifier,
	@ChartId					uniqueidentifier,
	@PropertyId					uniqueidentifier = null,
	@SubPropertyId				uniqueidentifier = null,
	@Order						int,
	@IsGrouping					bit,
	@CustomReportFieldTypeId	int,
	@TimeElapsedTypeId			int,
	@TimeElapsedFormatId		int,
	@TimeElapsedSpecificDate	smalldatetime = null,
	@Label						varchar(50) = null,
	@Logic						varchar(100) = null	
AS

INSERT INTO [dbo].[ChartsProperties] (
	[Id],
	[ChartId],
	[PropertyId],
	[SubPropertyId],
	[Order],
	[IsGrouping],
	[CustomReportFieldTypeId],
	[TimeElapsedTypeId],
	[TimeElapsedFormatId],
	[TimeElapsedSpecificDate],
	[Label],
	[Logic])
VALUES (
	@Id,
	@ChartId,
	@PropertyId,
	@SubPropertyId,
	@Order,
	@IsGrouping,
	@CustomReportFieldTypeId,
	@TimeElapsedTypeId,
	@TimeElapsedFormatId,
	@TimeElapsedSpecificDate,
	@Label,
	@Logic)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Chart_Get]
	@Id	uniqueidentifier
AS

SELECT 
	[Name],
	[PaletteName],
	[AppearanceName],
	[Title],
	[Subtitle],
	[ChartTitle],
	[LegendText],
	[LegendVisible],
	[SeriesName],
	[ViewType],
	[AssetTypeId],
	[IsInstance],
	[IsPublic],
	[RecordCount],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy],
	dbo.AssetType_GetReportingLabel([AssetTypeId], [IsInstance]) AS [ReportingLabel],
	[dbo].[AssetType_GetName] ([AssetTypeId]) AS [AssetType],
	[GroupSql]
FROM [Charts] WITH (NoLock)
WHERE [Id] = @Id
exec spr_ChartProperties_Get @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	spr_Chart_Insert
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Chart_Insert]
	@Id					uniqueidentifier,
	@Name				varchar(50),
	@PaletteName		varchar(50) = null,
	@AppearanceName		varchar(50) = null,
	@Title				varchar(100) = null,
	@Subtitle			varchar(100) = null,
	@ChartTitle			varchar(50) = null,
	@LegendText			varchar(50) = null,
	@LegendVisible		bit,
	@SeriesName			varchar(50),
	@ViewType			varchar(50) = null,
	@AssetTypeId		uniqueidentifier,
	@IsInstance			bit,	
	@IsPublic			bit,
	@RecordCount		int,
	@FilterExpression	varchar(50) = null,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null,
	@GroupSql			varchar(MAX) = null,
	@FullSql			varchar(MAX) = null
AS

INSERT INTO [dbo].[Charts] (
	[Id],
	[Name],
	[PaletteName],
	[AppearanceName],	
	[Title],
	[Subtitle],
	[ChartTitle],
	[LegendText],
	[LegendVisible],
	[SeriesName],
	[ViewType],
	[AssetTypeId],
	[IsInstance],	
	[IsPublic],
	[RecordCount],
	[FilterExpression],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy],
	[GroupSql],
	[FullSql])
VALUES (
	@Id,
	@Name,
	@PaletteName,
	@AppearanceName,	
	@Title,
	@Subtitle,
	@ChartTitle,
	@LegendText,
	@LegendVisible,
	@SeriesName,
	@ViewType,
	@AssetTypeId,
	@IsInstance,	
	@IsPublic,
	@RecordCount,
	@FilterExpression,
	@Created,
	@CreatedBy,
	@Deleted,
	@DeletedBy,
	@GroupSql,
	@FullSql)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	spr_Chart_Update
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Chart_Save]
	@Id					uniqueidentifier,
	@Name				varchar(50),
	@PaletteName		varchar(50) = null,
	@AppearanceName		varchar(50) = null,
	@Title				varchar(100) = null,
	@Subtitle			varchar(100) = null,
	@ChartTitle			varchar(50) = null,
	@LegendText			varchar(50) = null,
	@LegendVisible		bit,
	@SeriesName			varchar(50),
	@ViewType			varchar(50) = null,
	@AssetTypeId		uniqueidentifier,
	@IsInstance			bit,	
	@IsPublic			bit,
	@RecordCount		int,
	@FilterExpression	varchar(50) = null,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null,
	@GroupSql			varchar(MAX) = null,
	@FullSql			varchar(MAX) = null
AS

IF EXISTS (SELECT * FROM [dbo].[Charts] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Chart_Update] @Id, @Name, @PaletteName, @AppearanceName, @Title, @Subtitle, @ChartTitle, @LegendText, @LegendVisible,
	@SeriesName, @ViewType, @AssetTypeId, @IsInstance, @IsPublic, @RecordCount, @FilterExpression, @Created, @CreatedBy, @Deleted, @DeletedBy, @GroupSql, @FullSql

ELSE

	EXEC [dbo].[spr_Chart_Insert] @Id, @Name, @PaletteName, @AppearanceName, @Title, @Subtitle, @ChartTitle, @LegendText, @LegendVisible,
	@SeriesName, @ViewType, @AssetTypeId, @IsInstance, @IsPublic, @RecordCount, @FilterExpression, @Created, @CreatedBy, @Deleted, @DeletedBy, @GroupSql, @FullSql
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	spr_Chart_Update
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Chart_Update]
	@Id					uniqueidentifier,
	@Name				varchar(50),
	@PaletteName		varchar(50) = null,
	@AppearanceName		varchar(50) = null,
	@Title				varchar(100) = null,
	@Subtitle			varchar(100) = null,
	@ChartTitle			varchar(50) = null,
	@LegendText			varchar(50) = null,
	@LegendVisible		bit,
	@SeriesName			varchar(50),
	@ViewType			varchar(50) = null,
	@AssetTypeId		uniqueidentifier,
	@IsInstance			bit,	
	@IsPublic			bit,
	@RecordCount		int,
	@FilterExpression	varchar(50) = null,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null,
	@GroupSql			varchar(MAX) = null,
	@FullSql			varchar(MAX) = null
AS

UPDATE [dbo].[Charts] SET
	[Name] = @Name,
	[PaletteName] = @PaletteName,
	[AppearanceName] = @AppearanceName,	
	[Title] = @Title,
	[Subtitle] = @Subtitle,
	[ChartTitle] = @ChartTitle,
	[LegendText] = @LegendText,
	[LegendVisible] = @LegendVisible,
	[SeriesName] = @SeriesName,
	[ViewType] = @ViewType,
	[AssetTypeId] = @AssetTypeId,
	[IsInstance] = @IsInstance,	
	[IsPublic] = @IsPublic,
	[RecordCount] = @RecordCount,
	[FilterExpression] = @FilterExpression,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy,
	[GroupSql] = @GroupSql,
	[FullSql] = @FullSql,
	[LastModified] = GETDATE()
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelationList_DeleteByDocumentId
--*	Deletes a DocumentTagRelationList based on the foreign key of DocumentId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* 28 Jul 2009		JRS		Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_DocumentTagRelationList_DeleteByDocumentId]
	@DocumentId	uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [DocumentsTags]
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [DocumentId] = @DocumentId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelationList_DeleteByTagId
--*	Deletes a DocumentTagRelationList based on the foreign key of TagId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* 28 Jul 2009		JRS		Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_DocumentTagRelationList_DeleteByTagId]
	@TagId		uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [DocumentsTags]
SET
	[Deleted] = GetDate(),
	[DeletedBy]	= @DeletedBy
WHERE [TagId] = @TagId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelationList_Get
--*	Retrieves a DocumentTagRelationList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_DocumentTagRelationList_Get]
AS

SELECT
	[Id] AS [Id],
	[DocumentId] AS [DocumentId],
	[TagId] AS [TagId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[DocumentsTags] WITH (NoLock)
	WHERE [dbo].[DocumentsTags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelationList_GetByDocumentId
--*	Retrieves a DocumentTagRelationList based on the foreign key of DocumentId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_DocumentTagRelationList_GetByDocumentId]
	@DocumentId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[DocumentId] AS [DocumentId],
	[TagId] AS [TagId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[DocumentsTags] WITH (NoLock)
WHERE
	[dbo].[DocumentsTags].[DocumentId] = @DocumentId
		AND
	[dbo].[DocumentsTags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelationList_GetByTagId
--*	Retrieves a DocumentTagRelationList based on the foreign key of TagId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_DocumentTagRelationList_GetByTagId]
	@TagId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[DocumentId] AS [DocumentId],
	[TagId] AS [TagId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[DocumentsTags] WITH (NoLock)
WHERE
	[dbo].[DocumentsTags].[TagId] = @TagId
		AND
	[dbo].[DocumentsTags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelation_Delete
--*	Deletes the record for a/an DocumentTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_DocumentTagRelation_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[DocumentsTags]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelation_Get
--*	Retrieves a DocumentTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_DocumentTagRelation_Get]
	@Id uniqueidentifier
AS

SELECT
	[DocumentId] AS [DocumentId],
	[TagId] AS [TagId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[DocumentsTags] WITH (NoLock)
	WHERE
		[dbo].[DocumentsTags].[Id] = @Id
			AND
		[dbo].[DocumentsTags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelation_Insert
--*	Inserts a new record for a new DocumentTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_DocumentTagRelation_Insert]
	@Id		uniqueidentifier,
	@DocumentId		uniqueidentifier,
	@TagId		uniqueidentifier,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[DocumentsTags] (
	[Id],
	[DocumentId],
	[TagId],
	[Created],
	[CreatedBy],
	[DeletedBy])
VALUES (
	@Id,
	@DocumentId,
	@TagId,
	@Created,
	@CreatedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelation_Save
--*	Saves an instance of DocumentTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_DocumentTagRelation_Save]
	@Id			uniqueidentifier,
	@DocumentId	uniqueidentifier,
	@TagId		uniqueidentifier,
	@Created	smalldatetime,
	@CreatedBy	uniqueidentifier,
	@DeletedBy	uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[DocumentsTags] WHERE [DocumentId] = @DocumentId AND [TagId] = @TagId)

	EXEC [dbo].[spr_DocumentTagRelation_Update] @Id, @DocumentId, @TagId, @Created, @CreatedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_DocumentTagRelation_Insert] @Id, @DocumentId, @TagId, @Created, @CreatedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_DocumentTagRelation_Update
--*	Updates an existing record for a/an DocumentTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_DocumentTagRelation_Update]
	@Id		uniqueidentifier,
	@DocumentId		uniqueidentifier,
	@TagId		uniqueidentifier,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[DocumentsTags] SET
	[DocumentId] = @DocumentId,
	[TagId] = @TagId,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[DocumentsTags].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Document_Delete
--*	Deletes the record for a/an Document.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_Document_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[Documents]
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Document_Get
--*	Retrieves an instance of Document based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Document_Get]
	@Id	uniqueidentifier
AS

SELECT
	[DocumentTypeId] AS [DocumentTypeId],
	[Name] AS [Name],
	[Title] AS [Title],
	[Description] AS [Description],
	[Data] AS [Data],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[Documents] WITH (NoLock)
WHERE
	[dbo].[Documents].[Id] = @Id
		AND
	[dbo].[Documents].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Document_Insert
--*	Inserts a new record for a new Document.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Document_Insert]
	@Id				uniqueidentifier,
	@DocumentTypeId	int,
	@Name			varchar(50),
	@Title			varchar(255) = null,
	@Description	varchar(MAX) = null,
	@Data			varbinary(MAX) = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[Documents] (
	[Id],
	[DocumentTypeId],
	[Name],
	[Title],
	[Description],
	[Data],
	[Created],
	[Approved],
	[CreatedBy],
	[ApprovedBy],
	[DeletedBy])
VALUES (
	@Id,
	@DocumentTypeId,
	@Name,
	@Title,
	@Description,
	@Data,
	@Created,
	@Approved,
	@CreatedBy,
	@ApprovedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Document_Save
--*	Saves an instance of Document.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Document_Save]
	@Id				uniqueidentifier,
	@DocumentTypeId	int,
	@Name			varchar(50),
	@Title			varchar(255) = null,
	@Description	varchar(MAX) = null,
	@Data			varbinary(MAX) = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS


IF EXISTS (SELECT * FROM [dbo].[Documents] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Document_Update] @Id, @DocumentTypeId, @Name, @Title, @Description, @Data, @Created, @Approved, @CreatedBy, @ApprovedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_Document_Insert] @Id, @DocumentTypeId, @Name, @Title, @Description, @Data, @Created, @Approved, @CreatedBy, @ApprovedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Document_Update
--*	Updates an existing record for a/an Document.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Document_Update]
	@Id				uniqueidentifier,
	@DocumentTypeId	int,
	@Name			varchar(50),
	@Title			varchar(255) = null,
	@Description	varchar(MAX) = null,
	@Data			varbinary(MAX) = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[Documents] SET
	[DocumentTypeId] = @DocumentTypeId,
	[Name] = @Name,
	[Title] = @Title,
	[Description] = @Description,
	[Data] = @Data,
	[Created] = @Created,
	[Approved] = @Approved,
	[CreatedBy] = @CreatedBy,
	[ApprovedBy] = @ApprovedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[Documents].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelationList_DeleteByImageId
--*	Deletes a ImageTagRelationList based on the foreign key of ImageId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* 29 Jul 2009		JRS		Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_ImageTagRelationList_DeleteByImageId]
	@ImageId	uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [ImagesTags]
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [ImageId] = @ImageId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelationList_DeleteByTagId
--*	Deletes a ImageTagRelationList based on the foreign key of TagId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* 29 Jul 2009		JRS		Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_ImageTagRelationList_DeleteByTagId]
	@TagId		uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [ImagesTags]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [TagId] = @TagId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelationList_Get
--*	Retrieves a ImageTagRelationList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ImageTagRelationList_Get]
AS

SELECT
	[Id] AS [Id],
	[ImageId] AS [ImageId],
	[TagId] AS [TagId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[ImagesTags] WITH (NoLock)
	WHERE [dbo].[ImagesTags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelationList_GetByImageId
--*	Retrieves a ImageTagRelationList based on the foreign key of ImageId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ImageTagRelationList_GetByImageId]
	@ImageId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[ImageId] AS [ImageId],
	[TagId] AS [TagId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[ImagesTags] WITH (NoLock)
WHERE
	[dbo].[ImagesTags].[ImageId] = @ImageId
		AND
	[dbo].[ImagesTags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelationList_GetByTagId
--*	Retrieves a ImageTagRelationList based on the foreign key of TagId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ImageTagRelationList_GetByTagId]
	@TagId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[ImageId] AS [ImageId],
	[TagId] AS [TagId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[ImagesTags] WITH (NoLock)
WHERE
	[dbo].[ImagesTags].[TagId] = @TagId
		AND
	[dbo].[ImagesTags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelation_Delete
--*	Deletes the record for a/an ImageTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================
--* 29 Jul 2009		JRS		Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_ImageTagRelation_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[ImagesTags]
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelation_Get
--*	Retrieves a ImageTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ImageTagRelation_Get]
	@Id uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[ImageId] AS [ImageId],
	[TagId] AS [TagId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[ImagesTags] WITH (NoLock)
WHERE 
	[dbo].[ImagesTags].[Id] = @Id
		AND
	[dbo].[ImagesTags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelation_Insert
--*	Inserts a new record for a new ImageTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ImageTagRelation_Insert]
	@Id		uniqueidentifier,
	@ImageId		uniqueidentifier,
	@TagId		uniqueidentifier,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[ImagesTags] (
	[Id],
	[ImageId],
	[TagId],
	[Created],
	[CreatedBy],
	[DeletedBy])
VALUES (
	@Id,
	@ImageId,
	@TagId,
	@Created,
	@CreatedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelation_Save
--*	Saves an instance of ImageTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ImageTagRelation_Save]
	@Id				uniqueidentifier,
	@ImageId		uniqueidentifier,
	@TagId			uniqueidentifier,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[ImagesTags] WHERE [ImageId] = @ImageId AND [TagId] = @TagId)

	EXEC [dbo].[spr_ImageTagRelation_Update] @Id, @ImageId, @TagId, @Created, @CreatedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_ImageTagRelation_Insert] @Id, @ImageId, @TagId, @Created, @CreatedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ImageTagRelation_Update
--*	Updates an existing record for a/an ImageTagRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ImageTagRelation_Update]
	@Id		uniqueidentifier,
	@ImageId		uniqueidentifier,
	@TagId		uniqueidentifier,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[ImagesTags] SET
	[ImageId] = @ImageId,
	[TagId] = @TagId,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[ImagesTags].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Image_Delete
--*	Deletes the record for a/an Image.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_Image_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[Images] 
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Image_Get
--*	Retrieves an instance of Image based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Image_Get]
	@Id	uniqueidentifier
AS

SELECT
	[ImageTypeId] AS [ImageTypeId],
	[Order] AS [Order],
	[Name] AS [Name],
	[Description] AS [Description],
	[ImageData] AS [ImageData],
	[Width] AS [Width],
	[Height] AS [Height],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[Images] WITH (NoLock)
WHERE
	[dbo].[Images].[Id] = @Id
		AND
	[dbo].[Images].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Image_Insert
--*	Inserts a new record for a new Image.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Image_Insert]
	@Id				uniqueidentifier,
	@ImageTypeId	tinyint,
	@Order			tinyint,
	@Name			varchar(50),
	@Description	varchar(MAX) = null,
	@IsSystem		bit,
	@ImageData		varbinary(MAX) = null,
	@Width			int = null,
	@Height			int = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[Images] (
	[Id],
	[ImageTypeId],
	[Order],
	[Name],
	[Description],
	[IsSystem],
	[ImageData],
	[Width],
	[Height],
	[Created],
	[Approved],
	[CreatedBy],
	[ApprovedBy],
	[DeletedBy])
VALUES (
	@Id,
	@ImageTypeId,
	@Order,
	@Name,
	@Description,
	@IsSystem,
	@ImageData,
	@Width,
	@Height,
	@Created,
	@Approved,
	@CreatedBy,
	@ApprovedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Image_Save
--*	Saves an instance of Image.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Image_Save]
	@Id				uniqueidentifier,
	@ImageTypeId	tinyint,
	@Order			tinyint,
	@Name			varchar(50),
	@Description	varchar(MAX) = null,
	@IsSystem		bit,
	@ImageData		varbinary(MAX) = null,
	@Width			int = null,
	@Height			int = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS


IF EXISTS (SELECT * FROM [dbo].[Images] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Image_Update] @Id, @ImageTypeId, @Order, @Name, @Description, @IsSystem, @ImageData, @Width, @Height, @Created, @Approved, @CreatedBy, @ApprovedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_Image_Insert] @Id, @ImageTypeId, @Order, @Name, @Description, @IsSystem, @ImageData, @Width, @Height, @Created, @Approved, @CreatedBy, @ApprovedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Image_Update
--*	Updates an existing record for a/an Image.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Image_Update]
	@Id				uniqueidentifier,
	@ImageTypeId	tinyint,
	@Order			tinyint,
	@Name			varchar(50),
	@Description	varchar(MAX) = null,
	@ImageData		varbinary(MAX) = null,
	@IsSystem		bit,
	@Width			int = null,
	@Height			int = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[Images] SET
	[ImageTypeId] = @ImageTypeId,
	[Order] = @Order,
	[Name] = @Name,
	[Description] = @Description,
	[IsSystem] = @IsSystem,
	[ImageData] = @ImageData,
	[Width] = @Width,
	[Height] = @Height,
	[Created] = @Created,
	[Approved] = @Approved,
	[CreatedBy] = @CreatedBy,
	[ApprovedBy] = @ApprovedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[Images].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO





CREATE PROCEDURE [dbo].[spr_MapData_Get]

AS

select
	A.[LocId],
	A.[Location],
	dbo.GetPropertyValue(A.[LocId], '210E2FDC-802F-47C6-83A4-8E2A4F721624') AS [LatLon],
	--dbo.GetPropertyValue(A.[LocId], '6c0ee5d7-abaa-4226-afcb-046d375f109e') as [Lat],
	--dbo.GetPropertyValue(A.[LocId], '4900587a-1917-4a75-a7fa-84c0287aef96') as [Lon],
	A.[AssetTypeId],
	A.[AssetType],
	COUNT(A.[Asset]) AS [AssetCount]
from (
	select at.id as [AssetTypeId], at.Pluralization AS [AssetType], a1.Id, a1.Name AS [Asset], a2.id AS [LocId], a2.Name AS [Location]
	from PropertyValues pv
		inner join Assets a1 on a1.id = pv.AssetId 
		inner join Assets a2 on a2.Id = CAST(pv.value as uniqueidentifier)
		inner join AssetTypes at on at.Id = a1.AssetTypeId 
	where pv.PropertyId = '7fb9b5f2-734b-4e5f-9cac-104781f35328'
		and pv.Deleted is null and pv.Value != '' and pv.Value is not null
		and a2.Deleted is null
		and a1.AssetTypeId is not null
		and a2.AssetTypeId is not null) A
GROUP BY A.[LocId], A.[Location], A.[AssetTypeId], A.[AssetType]
ORDER BY A.[Location], A.[AssetType]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_MemberId_IsValid]
	@Id	uniqueidentifier
AS

SELECT COUNT(*)
FROM [Members] WITH (NoLock)
WHERE
	[Id] = @Id AND
	[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_MemberReport_Delete]
	@Id	uniqueidentifier
AS

DELETE FROM [MemberReportsProperties] WHERE [MemberReportId] = @Id
DELETE FROM [MemberReports] WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_MemberReport_Insert]
	@Id					uniqueidentifier,
	@ReportName			varchar(MAX),
	@AssetTypeId		uniqueidentifier,
	@FilterExpression	varchar(MAX) = null,
	@IsInstance			bit,
	@IsPublic			bit,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier
AS

INSERT INTO [dbo].[MemberReports] (
	[Id],
	[ReportName],
	[AssetTypeId],
	[IsInstance],
	[IsPublic],
	[FilterExpression],
	[Created],
	[CreatedBy])
VALUES (
	@Id,
	@ReportName,
	@AssetTypeId,
	@IsInstance,
	@IsPublic,
	@FilterExpression,
	@Created,
	@CreatedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_MemberReport_Save]
	@Id					uniqueidentifier,
	@ReportName			varchar(MAX),
	@AssetTypeId		uniqueidentifier,
	@FilterExpression	varchar(MAX) = null,
	@IsInstance			bit,
	@IsPublic			bit,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier
AS


IF EXISTS (SELECT * FROM [dbo].[MemberReports] WHERE [Id] = @Id)

	EXEC [dbo].[spr_MemberReport_Update] @Id, @ReportName, @FilterExpression, @IsPublic

ELSE

	EXEC [dbo].[spr_MemberReport_Insert] @Id, @ReportName, @AssetTypeId, @FilterExpression, @IsInstance, @IsPublic, @Created, @CreatedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_MemberReport_Update]
	@Id					uniqueidentifier,
	@ReportName			varchar(MAX),
	@FilterExpression	varchar(MAX) = null,
	@IsPublic			bit
AS

UPDATE [dbo].[MemberReports] SET
	[ReportName] = @ReportName,
	[FilterExpression] = @FilterExpression,
	[IsPublic] = @IsPublic
WHERE [dbo].[MemberReports].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--* ==================================================

CREATE PROCEDURE [dbo].[spr_MemberView_Get]
	@Id	uniqueidentifier
AS

SELECT MV.[MemberId], MV.[ViewId], V.[AssetTypeId], V.[IsInstance], MV.[Name], MV.[FilterExpression], MV.[FilterString]
FROM [MemberViews] MV
INNER JOIN [Views] V ON V.[Id] = MV.[ViewId]
WHERE MV.[Id] = @Id

SELECT [PropertyId], [OperatorId], [Value], [Order]  FROM [dbo].[MemberViewFilters]
WHERE [MemberViewId] = @Id
ORDER BY [Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--* ==================================================

CREATE PROCEDURE [dbo].[spr_MemberView_Save]
	@Id			uniqueidentifier,
	@MemberId	uniqueidentifier,
	@ViewId		uniqueidentifier,
	@Name		varchar(100),
	@FilterExp	varchar(max),
	@FilterString varchar(MAX) = null
AS

IF EXISTS (SELECT * FROM [dbo].[MemberViews] WHERE [Id] = @Id)

	BEGIN
		UPDATE [MemberViews]
		SET [Name] = @Name, [FilterExpression] = @FilterExp, [FilterString] = @FilterString
		WHERE [Id] = @Id 
	END

ELSE

	BEGIN
		INSERT INTO [dbo].[MemberViews] ([Id], [MemberId], [ViewId], [Name], [FilterExpression], [FilterString])
		VALUES (@Id, @MemberId, @ViewId, @Name, @FilterExp, @FilterString)
	END
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_MemberVisits_Get]
	@MemberId	uniqueidentifier
AS

SELECT DISTINCT [Created], [BrowserName], [BrowserVersion]
FROM [MemberVisits] WITH (NoLock)
WHERE [MemberId] = @MemberId
ORDER BY [Created] DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Member_Delete
--*	Deletes the record for a/an Member.
--*	Generated by BizObjGenerator
--* ==================================================
--* 29 Jul 2009		JRS		Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_Member_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[Members] 
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Member_Get
--*	Retrieves an instance of Member based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Member_Get]
	@Id	uniqueidentifier
AS

SELECT
	[UserId] AS [UserId]
	,[LName] AS [LName]
	,[FName] AS [FName]
	,[MName] AS [MName]
	,[Created] AS [Created]
	,[CreatedBy] AS [CreatedBy]
	,[LastModified] AS [LastModified]
	,[Deleted] AS [Deleted]
	,[DeletedBy] AS [DeletedBy]
	,dbo.MemberIsAdmin(@Id) AS [IsAdmin]
	,[AllowBeta] AS [AllowBeta]
FROM [dbo].[Members] WITH (NoLock)
WHERE
	[dbo].[Members].[Id] = @Id

--INSERT INTO [MemberVisits] ([MemberId]) VALUES (@Id)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Member_Insert
--*	Inserts a new record for a new Member.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Member_Insert]
	@Id				uniqueidentifier,
	@UserId			varchar(50),
	@LName			varchar(50) = null,
	@FName			varchar(50) = null,
	@MName			varchar(50) = null,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[Members] (
	[Id],
	[UserId],
	[LName],
	[FName],
	[MName],
	[Created],
	[CreatedBy],
	[DeletedBy])
VALUES (
	@Id,
	@UserId,
	@LName,
	@FName,
	@MName,
	@Created,
	@CreatedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Member_Save
--*	Saves an instance of Member.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Member_Save]
	@Id				uniqueidentifier,
	@UserId			varchar(50),
	@LName			varchar(50) = null,
	@FName			varchar(50) = null,
	@MName			varchar(50) = null,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS


IF EXISTS (SELECT * FROM [dbo].[Members] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Member_Update] @Id, @UserId, @LName, @FName, @MName, @Created, @CreatedBy, @DeletedBy

ELSE
	DELETE FROM [ClientCache] WHERE [Key] = 'ba357eed-63de-434f-9882-a9ea27a60a7b'
	EXEC [dbo].[spr_Member_Insert] @Id, @UserId, @LName, @FName, @MName, @Created, @CreatedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Member_Update
--*	Updates an existing record for a/an Member.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Member_Update]
	@Id				uniqueidentifier,
	@UserId			varchar(50),
	@LName			varchar(50) = null,
	@FName			varchar(50) = null,
	@MName			varchar(50) = null,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[Members] SET
	[UserId] = @UserId,
	[LName] = @LName,
	[FName] = @FName,
	[MName] = @MName,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[DeletedBy] = @DeletedBy,
	[LastModified] = GETDATE()
WHERE [dbo].[Members].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Members_GetDictionary]
AS

SELECT
	M.[Id] AS [Key],
	A.[DisplayValue] AS [Value]
FROM [Members] M WITH (NoLock)
	INNER JOIN [Assets] A ON A.[Id] = M.[Id] 
WHERE (M.[Deleted] IS NULL)
ORDER BY [Value]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Members_GetDictionary_InRole]
	@RoleId	uniqueidentifier
AS

SELECT
	M.[Id] AS [Key],
	A.[DisplayValue] AS [Value]
FROM [RoleMembers] RM WITH (NoLock)
	INNER JOIN [Roles] R WITH (NoLock) ON R.[Id] = RM.[RoleId]
	INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = RM.[MemberId]
	INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = M.[Id]
WHERE
	(RM.[Deleted] IS NULL) AND
	(M.[Deleted] IS NULL) AND
	(RM.[RoleId] = @RoleId) AND
	(A.[Deleted] IS NULL)
ORDER BY [Value]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Members_GetDictionary_NotInRole]
	@RoleId	uniqueidentifier
AS

SELECT
	M.[Id] AS [Key],
	A.[DisplayValue] AS [Value]
FROM [Assets] A WITH (NoLock)
INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = A.[Id]
WHERE M.[Id] NOT IN (
	SELECT DISTINCT RM.[MemberId]
	FROM [RoleMembers] RM
	WHERE RM.[RoleId] = @RoleId
	)
	AND (A.[Deleted] IS NULL)
ORDER BY [Value]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	spr_PickListDependency_Insert
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickListDependency_Insert]
	@Id						uniqueidentifier,
	@ParentPickListId		uniqueidentifier,
	@ParentPickListValueId	uniqueidentifier,
	@ChildPickListId		uniqueidentifier,
	@ChildPickListValueId	uniqueidentifier,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier,
	@Deleted				smalldatetime = null,
	@DeletedBy				uniqueidentifier = null
AS

INSERT INTO [dbo].[PickListDependencies] (
	[Id],
	[ParentPickListId],
	[ParentPickListValueId],
	[ChildPickListId],
	[ChildPickListValueId],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@ParentPickListId,
	@ParentPickListValueId,
	@ChildPickListId,
	@ChildPickListValueId,
	@Created,
	@CreatedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	spr_PickListDependency_Save
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickListDependency_Save]
	@Id						uniqueidentifier,
	@ParentPickListId		uniqueidentifier,
	@ParentPickListValueId	uniqueidentifier,
	@ChildPickListId		uniqueidentifier,
	@ChildPickListValueId	uniqueidentifier,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier,
	@Deleted				smalldatetime = null,
	@DeletedBy				uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[PickListDependencies] WHERE [Id] = @Id)

	EXEC [dbo].[spr_PickListDependency_Update] @Id, @ParentPickListId, @ParentPickListValueId, @ChildPickListId, @ChildPickListValueId, @Created, @CreatedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_PickListDependency_Insert] @Id, @ParentPickListId, @ParentPickListValueId, @ChildPickListId, @ChildPickListValueId, @Created, @CreatedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	spr_PickListPropertyRelation_Update
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickListDependency_Update]
	@Id						uniqueidentifier,
	@ParentPickListId		uniqueidentifier,
	@ParentPickListValueId	uniqueidentifier,
	@ChildPickListId		uniqueidentifier,
	@ChildPickListValueId	uniqueidentifier,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier,
	@Deleted				smalldatetime = null,
	@DeletedBy				uniqueidentifier = null
AS

UPDATE [dbo].[PickListDependencies] SET
	[ParentPickListId] = @ParentPickListId,
	[ParentPickListValueId] = @ParentPickListValueId,
	[ChildPickListId] = @ChildPickListId,
	[ChildPickListValueId] = @ChildPickListValueId,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[PickListDependencies].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickListValueList_DeleteByPickListId
--*	Deletes a PickListValueList based on the foreign key of PickListId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* 28 Jul 2009	JRS	Added @DeletedBY

CREATE PROCEDURE [dbo].[spr_PickListValueList_DeleteByPickListId]
	@PickListId	uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [PickListValues]
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [PickListId] = PickListId
AND [Deleted] IS NULL
AND [DeletedBy] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickListValueList_GetByPickListId
--*	Retrieves a PickListValueList based on the foreign key of PickListId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickListValueList_GetByPickListId]
	@PickListId	uniqueidentifier
AS

SELECT
	[Id],
	[Value],
	[DisplayValue],
	[BGColor],
	[Order],
	[Created],
	[Approved],
	[LastModified],
	[CreatedBy],
	[ApprovedBy],
	[LastModifiedBy],
	[Deleted],
	[DeletedBy]
FROM [dbo].[PickListValues] WITH (NoLock)
WHERE
	[dbo].[PickListValues].[PickListId] = @PickListId
		AND
	[dbo].[PickListValues].[Deleted] IS NULL
ORDER BY [Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickListValue_Delete
--*	Deletes the record for a/an PickListValue.
--*	Generated by BizObjGenerator
--* ==================================================
--* 29 Jul 2009		JRS		Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PickListValue_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[PickListValues] 
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickListValue_Get
--*	Retrieves an instance of PickListValue based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickListValue_Get]
	@Id	uniqueidentifier
AS

SELECT
	[PickListId] AS [PickListId],
	[Value] AS [Value],
	[DisplayValue] AS [DisplayValue],
	[BGColor] AS [BGColor],
	[Order] AS [Order],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[LastModified] AS [LastModified],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[LastModifiedBy] AS [LastModifiedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[PickListValues] WITH (NoLock)
WHERE
	[dbo].[PickListValues].[Id] = @Id
		AND
	[dbo].[PickListValues].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickListValue_Insert
--*	Inserts a new record for a new PickListValue.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickListValue_Insert]
	@Id			uniqueidentifier,
	@PickListId		uniqueidentifier,
	@Value			varchar(255),
	@DisplayValue	varchar(255) = null,
	@BGColor		varchar(7) = null,
	@Order			int,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@LastModified	smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@LastModifiedBy	uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null,
	@Deleted		smalldatetime = null
AS

INSERT INTO [dbo].[PickListValues] (
	[Id],
	[PickListId],
	[Value],
	[DisplayValue],
	[BGColor],
	[Order],
	[Created],
	[Approved],
	[LastModified],
	[CreatedBy],
	[ApprovedBy],
	[LastModifiedBy],
	[DeletedBy],
	[Deleted])
VALUES (
	@Id,
	@PickListId,
	@Value,
	@DisplayValue,
	@BGColor,
	@Order,
	@Created,
	@Approved,
	@LastModified,
	@CreatedBy,
	@ApprovedBy,
	@LastModifiedBy,
	@DeletedBy,
	@Deleted)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickListValue_Save
--*	Saves an instance of PickListValue.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickListValue_Save]
	@Id				uniqueidentifier,
	@PickListId		uniqueidentifier,
	@Value			varchar(255),
	@DisplayValue	varchar(255) = null,
	@BGColor		varchar(7) = null,
	@Order			int,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@LastModified	smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@LastModifiedBy	uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null,
	@Deleted		smalldatetime = null
AS


IF EXISTS (SELECT * FROM [dbo].[PickListValues] WHERE [Id] = @Id)

	EXEC [dbo].[spr_PickListValue_Update] @Id, @PickListId, @Value, @DisplayValue, @BGColor, @Order, @Created, @Approved, @LastModified, @CreatedBy, @ApprovedBy, @LastModifiedBy, @DeletedBy, @Deleted

ELSE

	EXEC [dbo].[spr_PickListValue_Insert] @Id, @PickListId, @Value, @DisplayValue, @BGColor, @Order, @Created, @Approved, @LastModified, @CreatedBy, @ApprovedBy, @LastModifiedBy, @DeletedBy, @Deleted
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickListValue_Update
--*	Updates an existing record for a/an PickListValue.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickListValue_Update]
	@Id				uniqueidentifier,
	@PickListId		uniqueidentifier,
	@Value			varchar(255),
	@DisplayValue	varchar(255) = null,
	@BGColor		varchar(7) = null,
	@Order			int,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@LastModified	smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@LastModifiedBy	uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null,
	@Deleted		smalldatetime = null
AS

UPDATE [dbo].[PickListValues] SET
	[PickListId] = @PickListId,
	[Value] = @Value,
	[DisplayValue] = @DisplayValue,
	[BGColor] = @BGColor,
	[Order] = @Order,
	[Created] = @Created,
	[Approved] = @Approved,
	[LastModified] = @LastModified,
	[CreatedBy] = @CreatedBy,
	[ApprovedBy] = @ApprovedBy,
	[LastModifiedBy] = @LastModifiedBy,
	[DeletedBy] = @DeletedBy,
	[Deleted] = @Deleted	
WHERE [dbo].[PickListValues].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickList_Delete
--*	Deletes the record for a/an PickList.
--*	Generated by BizObjGenerator
--* ==================================================
--* 29 Jul 2009		JRS		Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PickList_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[PickLists] 
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickList_Get
--*	Retrieves an instance of PickList based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickList_Get]
	@Id	uniqueidentifier
AS

SELECT
	[Name],
	[DisplayValue],
	[Description],
	[IsMemberList],
	[AllowNewValues],
	[ServiceUrl],
	[ServiceMethod],
	[ServiceUsername],
	[ServicePassword],
	[Created],
	[CreatedBy],
	[Approved],
	[ApprovedBy],
	[LastModified],
	[LastModifiedBy],
	[Deleted],
	[DeletedBy]
FROM [dbo].[PickLists] WITH (NoLock)
WHERE [dbo].[PickLists].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickList_Insert
--*	Inserts a new record for a new PickList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickList_Insert]
	@Id		uniqueidentifier,
	@Name		varchar(100),
	@DisplayValue		varchar(100) = null,
	@Description		varchar(MAX) = null,
	@IsMemberList		bit,
	@ServiceUrl		varchar(MAX) = null,
	@ServiceMethod		varchar(255) = null,
	@ServiceUsername		varchar(50) = null,
	@ServicePassword		varchar(50) = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@LastModified		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@LastModifiedBy		uniqueidentifier = null,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[PickLists] (
	[Id],
	[Name],
	[DisplayValue],
	[Description],
	[IsMemberList],
	[ServiceUrl],
	[ServiceMethod],
	[ServiceUsername],
	[ServicePassword],
	[Created],
	[Approved],
	[LastModified],
	[CreatedBy],
	[ApprovedBy],
	[LastModifiedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@Name,
	@DisplayValue,
	@Description,
	@IsMemberList,
	@ServiceUrl,
	@ServiceMethod,
	@ServiceUsername,
	@ServicePassword,
	@Created,
	@Approved,
	@LastModified,
	@CreatedBy,
	@ApprovedBy,
	@LastModifiedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickList_Save
--*	Saves an instance of PickList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickList_Save]
	@Id		uniqueidentifier,
	@Name		varchar(100),
	@DisplayValue		varchar(100) = null,
	@Description		varchar(MAX) = null,
	@IsMemberList		bit,
	@ServiceUrl		varchar(MAX) = null,
	@ServiceMethod		varchar(255) = null,
	@ServiceUsername		varchar(50) = null,
	@ServicePassword		varchar(50) = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@LastModified		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@LastModifiedBy		uniqueidentifier = null,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS


IF EXISTS (SELECT * FROM [dbo].[PickLists] WHERE [Id] = @Id)

	EXEC [dbo].[spr_PickList_Update] @Id, @Name, @DisplayValue, @Description, @IsMemberList, @ServiceUrl, @ServiceMethod, @ServiceUsername, @ServicePassword, @Created, @Approved, @LastModified, @CreatedBy, @ApprovedBy, @LastModifiedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_PickList_Insert] @Id, @Name, @DisplayValue, @Description, @IsMemberList, @ServiceUrl, @ServiceMethod, @ServiceUsername, @ServicePassword, @Created, @Approved, @LastModified, @CreatedBy, @ApprovedBy, @LastModifiedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PickList_Update
--*	Updates an existing record for a/an PickList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PickList_Update]
	@Id		uniqueidentifier,
	@Name		varchar(100),
	@DisplayValue		varchar(100) = null,
	@Description		varchar(MAX) = null,
	@IsMemberList		bit,
	@ServiceUrl		varchar(MAX) = null,
	@ServiceMethod		varchar(255) = null,
	@ServiceUsername		varchar(50) = null,
	@ServicePassword		varchar(50) = null,
	@Created		smalldatetime,
	@Approved		smalldatetime = null,
	@LastModified		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@ApprovedBy		uniqueidentifier = null,
	@LastModifiedBy		uniqueidentifier = null,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[PickLists] SET
	[Name] = @Name,
	[DisplayValue] = @DisplayValue,
	[Description] = @Description,
	[IsMemberList] = @IsMemberList,
	[ServiceUrl] = @ServiceUrl,
	[ServiceMethod] = @ServiceMethod,
	[ServiceUsername] = @ServiceUsername,
	[ServicePassword] = @ServicePassword,
	[Created] = @Created,
	[Approved] = @Approved,
	[LastModified] = @LastModified,
	[CreatedBy] = @CreatedBy,
	[ApprovedBy] = @ApprovedBy,
	[LastModifiedBy] = @LastModifiedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[PickLists].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================

--* ==================================================

CREATE PROCEDURE [dbo].[spr_Processes_GetBy_AssetTypeIds]
	@AssetTypeIds	varchar(max),
	@IsInstance		bit
AS

SELECT
	P.[Id]
	,IsNull(P.[DisplayValue], P.[Name])
FROM [Processes] P WITH (NoLock)
	INNER JOIN [ProcessSteps] PS WITH (NoLock) ON PS.[ProcessId] = P.[Id]
	INNER JOIN [Views] V WITH (NoLock) ON V.[Id] = PS.[ViewId]
WHERE
	V.[AssetTypeId] IN (SELECT [Value] FROM dbo.Split(@AssetTypeIds, ','))
	AND (V.[Deleted] IS NULL)
	AND (P.[Deleted] IS NULL)
	AND (V.[IsInstance] = @IsInstance)
ORDER BY P.[Name]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Properties_GetByListOfPropertyIds]
	@PropertyIds	varchar(max)
AS

SELECT
	[Id],
	[DataTypeId],
	[Precision],
	[PickListId],
	[RoleId],
	[IsOrdered],
	[AllowMultiValue],
	[Name],
	[DisplayValue],
	[Description],
	[AssetTypeId],
	[AssetTypeIsInstance],
	[Created],
	[Approved],
	[LastModified],
	[CreatedBy],
	[ApprovedBy],
	[LastModifiedBy],
	[Deleted],
	[DeletedBy],
	[IsSystem],
	[SystemTypeId]
FROM [dbo].[Properties] WITH (NoLock)
WHERE
	[dbo].[Properties].[Id] IN (SELECT [Value] FROM dbo.Split(@PropertyIds, ','))
ORDER BY [DisplayValue]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_PropertyGroupList_GetByViewId]
	@ViewId uniqueidentifier
AS

SELECT
	PG.[Id],
	PG.[Name],
	PG.[DisplayValue],
	PG.[AssetTypeId],
	PG.[IsInstance],
	PG.[Created],
	PG.[LastModified],
	PG.[CreatedBy],
	PG.[LastModifiedBy]
FROM [PropertyGroups] PG WITH (NoLock)	
INNER JOIN [dbo].[ViewsPropertyGroups] VPG ON  VPG.[PropertyGroupId] = PG.[Id]
WHERE
	VPG.[ViewId] = @ViewId AND
	VPG.[Deleted] IS NULL AND
	PG.[Deleted] IS NULL
ORDER BY VPG.[Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelationList_DeleteByPropertyGroupId
--*	Deletes a PropertyGroupPropertyRelationList based on the foreign key of PropertyGroupId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelationList_DeleteByPropertyGroupId]
	@PropertyGroupId	uniqueidentifier,
	@DeletedBy			uniqueidentifier
AS

UPDATE [PropertyGroupsProperties]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [PropertyGroupId] = @PropertyGroupId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelationList_DeleteByPropertyId
--*	Deletes a PropertyGroupPropertyRelationList based on the foreign key of PropertyId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelationList_DeleteByPropertyId]
	@PropertyId	uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [PropertyGroupsProperties]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [PropertyId] = @PropertyId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelationList_Get
--*	Retrieves a PropertyGroupPropertyRelationList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelationList_Get]
AS

SELECT
	[Id] AS [Id],
	[PropertyGroupId] AS [PropertyGroupId],
	[PropertyId] AS [PropertyId],
	[Order] AS [Order],
	[IsRequired] AS [IsRequired],
	[IsReadOnly] AS [IsReadOnly],
	[DefaultValue] AS [DefaultValue],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[PropertyGroupsProperties] WITH (NoLock)
	WHERE [dbo].[PropertyGroupsProperties].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelationList_GetByPropertyGroupId
--*	Retrieves a PropertyGroupPropertyRelationList based on the foreign key of PropertyGroupId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelationList_GetByPropertyGroupId]
	@PropertyGroupId	uniqueidentifier
AS

SELECT
	PGP.[Id] AS [Id],
	PGP.[PropertyGroupId] AS [PropertyGroupId],
	PGP.[PropertyId] AS [PropertyId],
	PGP.[Order] AS [Order],
	PGP.[IsRequired] AS [IsRequired],
	PGP.[IsReadOnly] AS [IsReadOnly],
	PGP.[DefaultValue] AS [DefaultValue],
	PGP.[Created] AS [Created],
	PGP.[CreatedBy] AS [CreatedBy],
	PGP.[DeletedBy] AS [DeletedBy]
FROM [dbo].[PropertyGroupsProperties] PGP WITH (NoLock)
INNER JOIN [dbo].[Properties] P ON P.[Id] = PGP.[PropertyId]
WHERE
	PGP.[PropertyGroupId] = @PropertyGroupId
		AND
	PGP.[Deleted] IS NULL
		AND
	P.[Deleted] IS NULL
		AND
	P.[DeletedBy] IS NULL
ORDER BY [Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelationList_GetByPropertyId
--*	Retrieves a PropertyGroupPropertyRelationList based on the foreign key of PropertyId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelationList_GetByPropertyId]
	@PropertyId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[PropertyGroupId] AS [PropertyGroupId],
	[PropertyId] AS [PropertyId],
	[Order] AS [Order],
	[IsRequired] AS [IsRequired],
	[IsReadOnly] AS [IsReadOnly],
	[DefaultValue] AS [DefaultValue],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[PropertyGroupsProperties] WITH (NoLock)
WHERE
	[dbo].[PropertyGroupsProperties].[PropertyId] = @PropertyId
		AND
	[dbo].[PropertyGroupsProperties].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelation_Delete
--*	Deletes the record for a/an PropertyGroupPropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelation_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[PropertyGroupsProperties] 
SET
	[Deleted] = GetDate() ,
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelation_Get
--*	Retrieves a PropertyGroupPropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelation_Get]
	@Id uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[PropertyGroupId] AS [PropertyGroupId],
	[PropertyId] AS [PropertyId],
	[Order] AS [Order],
	[IsRequired] AS [IsRequired],
	[IsReadOnly] AS [IsReadOnly],
	[DefaultValue] AS [DefaultValue],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[PropertyGroupsProperties] WITH (NoLock)
WHERE 
	[dbo].[PropertyGroupsProperties].[Id] = @Id
		AND
	[dbo].[PropertyGroupsProperties].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelation_Insert
--*	Inserts a new record for a new PropertyGroupPropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelation_Insert]
	@Id					uniqueidentifier,
	@PropertyGroupId	uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@Order				int,
	@IsRequired			bit,
	@IsReadOnly			bit,
	@DefaultValue		varchar(max) = null,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@DeletedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[PropertyGroupsProperties] (
	[Id],
	[PropertyGroupId],
	[PropertyId],
	[Order],
	[IsRequired],
	[IsReadOnly],
	[DefaultValue],
	[Created],
	[CreatedBy],
	[DeletedBy])
VALUES (
	@Id,
	@PropertyGroupId,
	@PropertyId,
	@Order,
	@IsRequired,
	@IsReadOnly,
	@DefaultValue,
	@Created,
	@CreatedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelation_Save
--*	Saves an instance of PropertyGroupPropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelation_Save]
	@Id					uniqueidentifier,
	@PropertyGroupId	uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@Order				int,
	@IsRequired			bit,
	@IsReadOnly			bit,
	@DefaultValue		varchar(max) = null,
	@Created			smalldatetime,
	@Deleted			smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@DeletedBy			uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[PropertyGroupsProperties] WHERE [Id] = @Id)

	EXEC [dbo].[spr_PropertyGroupPropertyRelation_Update] @Id, @PropertyGroupId, @PropertyId, @Order, @IsRequired, @IsReadOnly, @DefaultValue, @Created, @Deleted, @CreatedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_PropertyGroupPropertyRelation_Insert] @Id, @PropertyGroupId, @PropertyId, @Order, @IsRequired, @IsReadOnly, @DefaultValue, @Created, @CreatedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroupPropertyRelation_Update
--*	Updates an existing record for a/an PropertyGroupPropertyRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroupPropertyRelation_Update]
	@Id					uniqueidentifier,
	@PropertyGroupId	uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@Order				int,
	@IsRequired			bit,
	@IsReadOnly			bit,
	@DefaultValue		varchar(max) = null,
	@Created			smalldatetime,
	@Deleted			smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@DeletedBy			uniqueidentifier = null
AS

UPDATE [dbo].[PropertyGroupsProperties] SET
	[PropertyGroupId] = @PropertyGroupId,
	[PropertyId] = @PropertyId,
	[Order] = @Order,
	[IsRequired] = @IsRequired,
	[IsReadOnly] = @IsReadOnly,
	[DefaultValue] = @DefaultValue,
	[Created] = @Created,
	[Deleted] = @Deleted,
	[CreatedBy] = @CreatedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[PropertyGroupsProperties].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroup_Delete
--*	Deletes the record for a/an PropertyGroup.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PropertyGroup_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[PropertyGroups] 
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroup_Get
--*	Retrieves an instance of PropertyGroup based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroup_Get]
	@Id	uniqueidentifier
AS

SELECT
	[Name] AS [Name],
	[DisplayValue] AS [DisplayValue],
	[AssetTypeId] AS [AssetTypeId],
	[IsInstance] AS [IsInstance],
	[Created] AS [Created],
	[LastModified] AS [LastModified],
	[CreatedBy] AS [CreatedBy],
	[LastModifiedBy] AS [LastModifiedBy],
	[Deleted] AS [Deleted],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[PropertyGroups] WITH (NoLock)
WHERE
	[dbo].[PropertyGroups].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroup_Insert
--*	Inserts a new record for a new PropertyGroup.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroup_Insert]
	@Id				uniqueidentifier,
	@Name			varchar(100),
	@DisplayValue	varchar(100),
	@AssetTypeId	uniqueidentifier,
	@IsInstance		bit,
	@Created		smalldatetime,
	@LastModified	smalldatetime = null,
	@Deleted		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@LastModifiedBy	uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[PropertyGroups] (
	[Id],
	[Name],
	[DisplayValue],
	[AssetTypeId],
	[IsInstance],
	[Created],
	[LastModified],
	[Deleted],
	[CreatedBy],
	[LastModifiedBy],
	[DeletedBy])
VALUES (
	@Id,
	@Name,
	@DisplayValue,
	@AssetTypeId,
	@IsInstance,
	@Created,
	@LastModified,
	@Deleted,
	@CreatedBy,
	@LastModifiedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_PropertyGroup_Kill]
	@Id	uniqueidentifier
AS

DELETE FROM [PropertyGroupsProperties] WHERE [PropertyGroupId] = @Id 
DELETE FROM [ViewsPropertyGroups] WHERE [PropertyGroupId] = @Id 
DELETE FROM [PropertyGroups] WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroup_Save
--*	Saves an instance of PropertyGroup.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroup_Save]
	@Id				uniqueidentifier,
	@Name			varchar(100),
	@DisplayValue	varchar(100),
	@AssetTypeId	uniqueidentifier,
	@IsInstance		bit,
	@Created		smalldatetime,
	@LastModified	smalldatetime = null,
	@Deleted		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@LastModifiedBy	uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS


IF EXISTS (SELECT * FROM [dbo].[PropertyGroups] WHERE [Id] = @Id)

	EXEC [dbo].[spr_PropertyGroup_Update] @Id, @Name, @DisplayValue, @AssetTypeId, @IsInstance, @Created, @LastModified, @Deleted, @CreatedBy, @LastModifiedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_PropertyGroup_Insert] @Id, @Name, @DisplayValue, @AssetTypeId, @IsInstance, @Created, @LastModified, @Deleted, @CreatedBy, @LastModifiedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyGroup_Update
--*	Updates an existing record for a/an PropertyGroup.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyGroup_Update]
	@Id				uniqueidentifier,
	@Name			varchar(100),
	@DisplayValue	varchar(100),
	@AssetTypeId	uniqueidentifier,
	@IsInstance		bit,
	@Created		smalldatetime,
	@LastModified	smalldatetime = null,
	@Deleted		smalldatetime = null,
	@CreatedBy		uniqueidentifier,
	@LastModifiedBy	uniqueidentifier = null,
	@DeletedBy		uniqueidentifier = null
AS

-- Delete any ViewClients that are using this property group
DELETE FROM [ViewClients] WHERE [ViewId] IN ( 
	SELECT V.[Id]
	FROM [Views] v
	INNER JOIN [ViewsPropertyGroups] VPG on VPG.[ViewId] = V.[Id]
	WHERE VPG.[PropertyGroupId] = @Id
)

-- Mark any Views using this PropertyGroup as having been updated (because technically they have been updated indirectly)
UPDATE [Views]
SET [LastModified] = GETDATE(), [LastModifiedBy] = @CreatedBy
WHERE [Id] IN (
	SELECT V.[Id]
	FROM [Views] v
	INNER JOIN [ViewsPropertyGroups] VPG on VPG.[ViewId] = V.[Id]
	WHERE VPG.[PropertyGroupId] = @Id
)

UPDATE [dbo].[PropertyGroups] SET
	[Name] = @Name,
	[DisplayValue] = @DisplayValue,
	[AssetTypeId] = @AssetTypeId,
	[IsInstance] = @IsInstance,
	[Created] = @Created,
	[LastModified] = @LastModified,
	[Deleted] = @Deleted,
	[CreatedBy] = @CreatedBy,
	[LastModifiedBy] = @LastModifiedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[PropertyGroups].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_PropertyList_GetByAssetTypeId]
	@AssetTypeId	uniqueidentifier
AS

SELECT
	P.[Id],
	P.[DataTypeId],
	P.[Precision],
	P.[PickListId],
	P.[RoleId],
	P.[IsOrdered],
	P.[AllowMultiValue],
	P.[Name],
	P.[DisplayValue],
	P.[Description],
	P.[AssetTypeId],
	P.[AssetTypeIsInstance],
	P.[Created],
	P.[Approved],
	P.[LastModified],
	P.[CreatedBy],
	P.[ApprovedBy],
	P.[LastModifiedBy],
	P.[Deleted],
	P.[DeletedBy],
	P.[IsSystem],
	P.[SystemTypeId],
	ATP.[IsInherited],
	ATP.[IsInheritedValue]
FROM [dbo].[AssetTypesProperties] ATP WITH (NoLock)
INNER JOIN [dbo].[Properties] P WITH (NoLock) ON P.[Id] = ATP.[PropertyId]
WHERE
	(ATP.[AssetTypeId] = @AssetTypeId) AND
	(P.[Deleted] IS NULL) AND
	(ATP.[Deleted] IS NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_PropertyList_GetByPropertyGroupId]
	@PropertyGroupId	uniqueidentifier
AS

SELECT
	P.[Id],
	P.[DataTypeId],
	P.[Precision],
	P.[PickListId],
	P.[RoleId],
	P.[IsOrdered],
	P.[AllowMultiValue],
	P.[Name],
	P.[DisplayValue],
	P.[Description],
	P.[AssetTypeId],
	P.[AssetTypeIsInstance],
	P.[Created],
	P.[Approved],
	P.[LastModified],
	P.[CreatedBy],
	P.[ApprovedBy],
	P.[LastModifiedBy],
	P.[Deleted],
	P.[DeletedBy],
	P.[IsSystem],
	P.[SystemTypeId],
	PGP.IsRequired,
	PGP.IsReadOnly,
	PGP.DefaultValue
FROM [dbo].[PropertyGroupsProperties] PGP WITH (NoLock)
INNER JOIN [dbo].[Properties] P WITH (NoLock) ON P.[Id] = PGP.[PropertyId]
WHERE
	PGP.[PropertyGroupId] = @PropertyGroupId AND
	PGP.[Deleted] IS NULL AND
	P.[Deleted] IS NULL
ORDER BY PGP.[Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValueList_DeleteByAssetId
--*	Deletes a PropertyValueList based on the foreign key of AssetId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PropertyValueList_DeleteByAssetId]
	@AssetId		uniqueidentifier,
	@DeletedBy		uniqueidentifier
AS

UPDATE [PropertyValues]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE
	[AssetId] = @AssetId
		AND
	[Deleted] IS NULL
		AND
	[DeletedBy] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValueList_DeleteByPropertyId
--*	Deletes a PropertyValueList based on the foreign key of PropertyId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* Date			Modifier	Action
--* 03 Aug 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PropertyValueList_DeleteByPropertyId]
	@PropertyId	uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [PropertyValues]
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [PropertyId] = @PropertyId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Thursday, 14 January, 2010 03:10 PM
--*	spr_PropertyValueList_DeleteByValue
--*	Deletes a PropertyValueList based on the provided value
--* ==================================================
--* Date			Modifier	Action
--* 14 Jan 2010		JRS			Initial Version

CREATE PROCEDURE [dbo].[spr_PropertyValueList_DeleteByValue]
	@Value			varchar(MAX),
	@DeletedBy		uniqueidentifier
AS

UPDATE [PropertyValues]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE
	[Value] = @Value
		AND
	[Deleted] IS NULL
		AND
	[DeletedBy] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_PropertyValueList_Get]
	@Ids VARCHAR(MAX)
AS

SELECT
	PV.[Id] AS [Id],
	PV.[PropertyId] AS [PropertyId],
	PV.[AssetId] AS [AssetId],
	PV.[SubmittalGroupId] AS [SubmittalGroupId],
	PV.[Value] AS [Value],
	PV.[Order] AS [Order],
	PV.[Created] AS [Created],
	PV.[Approved] AS [Approved],
	PV.[Rejected] AS [Rejected],
	PV.[Deleted] AS [Deleted],
	PV.[CreatedBy] AS [CreatedBy],
	PV.[ApprovedBy] AS [ApprovedBy],
	PV.[RejectedBy] AS [RejectedBy],
	PV.[DeletedBy] AS [DeletedBy],
	dbo.GetPropertyValueLookup(PV.[AssetId], PV.[PropertyId]) AS [DisplayValue]
FROM [dbo].[PropertyValues] PV WITH (NoLock)
--INNER JOIN dbo.fnStringSplitter(@PropertyIds, ',') as eIds ON PV.[Id] = eIds.Id
WHERE PV.[Id] in (SELECT [Value] FROM dbo.Split(@Ids,','))
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValueList_GetByAssetId
--*	Retrieves a PropertyValueList based on the foreign key of AssetId
--*	Generated by BizObjGenerator
--* ==================================================
--* 05 Feb 2010		JRS		Added [Rejected] and [RejectedBy]

CREATE PROCEDURE [dbo].[spr_PropertyValueList_GetByAssetId]
	@AssetId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[PropertyId] AS [PropertyId],
	[AssetId] AS [AssetId],
	[SubmittalGroupId] AS [SubmittalGroupId],
	[Value] AS [Value],
	[Order] AS [Order],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[Rejected] AS [Rejected],
	[Deleted] AS [Deleted],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[RejectedBy] AS [RejectedBy],
	[DeletedBy] AS [DeletedBy],
	dbo.GetPropertyValueLookup([AssetId], [PropertyId]) AS [DisplayValue]
FROM [dbo].[PropertyValues] WITH (NoLock)
WHERE
	[dbo].[PropertyValues].[AssetId] = @AssetId
	--	AND
	--[dbo].[PropertyValues].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValueList_GetByPropertyId
--*	Retrieves a PropertyValueList based on the foreign key of PropertyId
--*	Generated by BizObjGenerator
--* ==================================================
--* 05 Feb 2010		JRS		Added [Rejected] and [RejectedBy]

CREATE PROCEDURE [dbo].[spr_PropertyValueList_GetByPropertyId]
	@PropertyId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[PropertyId] AS [PropertyId],
	[AssetId] AS [AssetId],
	[SubmittalGroupId] AS [SubmittalGroupId],
	[Value] AS [Value],
	[Order] AS [Order],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[Rejected] AS [Rejected],
	[Deleted] AS [Deleted],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[RejectedBy] AS [RejectedBy],
	[DeletedBy] AS [DeletedBy],
	NULL AS [DisplayValue]
FROM [dbo].[PropertyValues] WITH (NoLock)
WHERE
	[dbo].[PropertyValues].[PropertyId] = @PropertyId
		AND
	[dbo].[PropertyValues].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Friday, 05 Feb, 2010
--*	spr_PropertyValueList_GetBySubmittalGroupId
--*	Retrieves a PropertyValueList based on the foreign key of SubmittalGroupId
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueList_GetBySubmittalGroupId]
	@SubmittalGroupId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[PropertyId] AS [PropertyId],
	[AssetId] AS [AssetId],
	[SubmittalGroupId] AS [SubmittalGroupId],
	[Value] AS [Value],
	[Order] AS [Order],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[Rejected] AS [Rejected],
	[Deleted] AS [Deleted],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[RejectedBy] AS [RejectedBy],
	[DeletedBy] AS [DeletedBy],
	NULL AS [DisplayValue]
FROM [dbo].[PropertyValues] WITH (NoLock)
WHERE
	[dbo].[PropertyValues].[SubmittalGroupId] = @SubmittalGroupId
		AND
	[dbo].[PropertyValues].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_PropertyValueList_GetForAnAsset]
	@AssetId			uniqueidentifier,
	@PropertyIds		varchar(max)
AS

SELECT
	PV.[Id] AS [Id],
	PV.[PropertyId] AS [PropertyId],
	PV.[AssetId] AS [AssetId],
	PV.[SubmittalGroupId] AS [SubmittalGroupId],
	PV.[Value] AS [Value],
	PV.[Order] AS [Order],
	PV.[Created] AS [Created],
	PV.[Approved] AS [Approved],
	PV.[Rejected] AS [Rejected],
	PV.[Deleted] AS [Deleted],
	PV.[CreatedBy] AS [CreatedBy],
	PV.[ApprovedBy] AS [ApprovedBy],
	PV.[RejectedBy] AS [RejectedBy],
	PV.[DeletedBy] AS [DeletedBy],
	dbo.GetPropertyValueLookupByPropValId(PV.[Id], PV.[PropertyId]) AS [DisplayValue]
FROM [PropertyValues] PV
WHERE
	(PV.[AssetId] = @AssetId) AND
	(PV.[PropertyId] IN (SELECT [Value] FROm dbo.Split(@PropertyIds, ','))) AND
	(PV.[Deleted] IS NULL) AND
	(PV.[Approved] IS NOT NULL)
--ORDER BY PV.[Created]
ORDER BY [DisplayValue] DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_PropertyValueList_GetForAnAssetIncludeDeleted]
	@AssetId			uniqueidentifier,
	@PropertyIds		varchar(max)
AS

SELECT
	PV.[Id] AS [Id],
	PV.[PropertyId] AS [PropertyId],
	PV.[AssetId] AS [AssetId],
	PV.[SubmittalGroupId] AS [SubmittalGroupId],
	PV.[Value] AS [Value],
	PV.[Order] AS [Order],
	PV.[Created] AS [Created],
	PV.[Approved] AS [Approved],
	PV.[Rejected] AS [Rejected],
	PV.[Deleted] AS [Deleted],
	PV.[CreatedBy] AS [CreatedBy],
	PV.[ApprovedBy] AS [ApprovedBy],
	PV.[RejectedBy] AS [RejectedBy],
	PV.[DeletedBy] AS [DeletedBy],
	dbo.GetPropertyValueLookup(PV.[AssetId], PV.[PropertyId]) AS [DisplayValue]
FROM [PropertyValues] PV
WHERE
	(PV.[AssetId] = @AssetId) AND
	(PV.[PropertyId] IN (SELECT [Value] FROm dbo.Split(@PropertyIds, ','))) AND
	(PV.[Approved] IS NOT NULL)
ORDER BY PV.[Created]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*    Monday, July 20, 2009 12:18 PM
--*    spr_PropertyValueSubmittalList_Get
--*    Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittalList_Get]
AS

SELECT
	[Id] AS [Id],
    [AssetId] AS [AssetId],
    [Notes] AS [Notes],
    [Created] AS [Created],
    [Reviewed] AS [Reviewed],
    [CreatedBy] AS [CreatedBy],
    [ReviewedBy] AS [ReviewedBy]
FROM [dbo].[PropertyValueSubmittals] WITH (NoLock)
WHERE
    [dbo].[PropertyValueSubmittals].[Deleted] IS NULL
		AND
    [dbo].[PropertyValueSubmittals].[Reviewed] IS NULL
ORDER BY [Created] DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*    Monday, July 20, 2009 12:18 PM
--*    spr_PropertyValueSubmittalList_GetDisplay
--*    Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittalList_GetDisplay]
AS

SELECT DISTINCT
	PVS.[Id] AS [Id],
	PVS.AssetId AS [AssetId],
    A.[Name] AS [AssetName],
    PVS.[Notes] AS [Notes],
    PVS.[Created] AS [Created],
    PVS.[CreatedBy] AS [CreatedBy],    
    [dbo].[GetMemberValueById](PVS.[CreatedBy]) AS [CreatedByDisplay],
    PVS.[Reviewed] AS [Reviewed],
    PVS.[ReviewedBy] AS [ReviewedBy],
    [dbo].[GetMemberValueById](PVS.[ReviewedBy]) AS [ReviewedByDisplay]
FROM [dbo].[PropertyValueSubmittals] PVS 
INNER JOIN [Members] M ON M.[Id] = PVS.[CreatedBy]
INNER JOIN [PropertyValues] PV ON PV.[SubmittalGroupId] = PVS.[Id]
INNER JOIN [Assets] A ON A.[Id] = PVS.[AssetId]
WHERE
    PVS.[Deleted] IS NULL
		AND
    PVS.[Reviewed] IS NULL
ORDER BY PVS.[Created] DESC
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*    Monday, July 20, 2009 12:18 PM
--*    spr_PropertyValueSubmittal_Delete
--*    Deletes the record for a/an PropertyValueSubmittal.
--*    Generated by BizObjGenerator
--* ==================================================
--* Date            Modifier    Action
--* 29 Jul 2009        JRS            Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittal_Delete]
    @Id            uniqueidentifier,
    @DeletedBy    uniqueidentifier
AS

UPDATE [dbo].[PropertyValueSubmittals] 
SET 
    [Deleted] = GetDate(),
    [DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*    Monday, July 20, 2009 12:18 PM
--*    spr_PropertyValueSubmittal_Get
--*    Retrieves an instance of PropertyValueSubmittal based on the primary key of Id
--*    Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittal_Get]
    @Id    uniqueidentifier
AS

SELECT
	[Id]				AS	[Id],
    [AssetId]			AS	[AssetId],
    [Notes]				AS	[Notes],
    [Created]			AS	[Created],
    [Reviewed]			AS	[Reviewed],
    [Deleted]			AS	[Deleted],
    [CreatedBy]			AS	[CreatedBy],
    [ReviewedBy]		AS	[ReviewedBy],
    [DeletedBy]			AS	[DeletedBy]
FROM [dbo].[PropertyValueSubmittals] WITH (NoLock)
WHERE
    [dbo].[PropertyValueSubmittals].[Id] = @Id
        AND
    [dbo].[PropertyValueSubmittals].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittal_GetAssetInfo]
    @Id    uniqueidentifier
AS

SELECT
	A.[Id],
	ISNULL(A.[displayValue], A.[Name]) AS [Asset]
FROM [dbo].[PropertyValueSubmittals] PVS WITH (NoLock)
INNER JOIN [Assets] A ON A.[Id] = PVS.[AssetId] 
WHERE PVS.[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittal_GetSummary]
    @Id    uniqueidentifier
AS

SELECT
	ISNULL(P.DisplayValue, P.Name) AS [Prop],
	ISNULL(dbo.GetPreviousPropertyValueLookup(PV.[assetId], P.[Id]), '') AS [Previous],
	ISNULL(dbo.GetPropertyValueLookupByPropValId(PV.Id, PV.PropertyId), '') AS [New]
from PropertyValueSubmittals PVS
INNER JOIN [PropertyValues] PV ON PV.[SubmittalGroupId] = PVS.[Id] 
INNER JOIN [Properties] P ON P.[Id] = PV.[PropertyId]
where PVS.[Id] = @Id
ORDER BY [Prop]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*    Monday, July 20, 2009 12:18 PM
--*    spr_PropertyValueSubmittal_Get
--*    Retrieves an instance of PropertyValueSubmittal based on the primary key of Id
--*    Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittal_Get_IgnoreDeletedFlag]
    @Id    uniqueidentifier
AS

SELECT
	[Id]				AS	[Id],
    [AssetId]			AS	[AssetId],
    [Notes]				AS	[Notes],
    [Created]			AS	[Created],
    [Reviewed]			AS	[Reviewed],
    [Deleted]			AS	[Deleted],
    [CreatedBy]			AS	[CreatedBy],
    [ReviewedBy]		AS	[ReviewedBy],
    [DeletedBy]			AS	[DeletedBy]
FROM [dbo].[PropertyValueSubmittals] WITH (NoLock)
WHERE
    [dbo].[PropertyValueSubmittals].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*    Monday, July 20, 2009 12:18 PM
--*    spr_PropertyValueSubmittal_Insert
--*    Inserts a new record for a new PropertyValueSubmittal.
--*    Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittal_Insert]
    @Id                 uniqueidentifier,
    @AssetId			uniqueidentifier,
    @Notes				varchar(MAX) = null,
    @Created			smalldatetime,
    @Reviewed			smalldatetime = null,
    @Deleted			smalldatetime = null,
    @CreatedBy          uniqueidentifier,
    @ReviewedBy         uniqueidentifier = null,
    @DeletedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[PropertyValueSubmittals] (
    [Id],
    [AssetId],
    [Notes],
    [Created],
    [Reviewed],
    [Deleted],
    [CreatedBy],
    [ReviewedBy],
    [DeletedBy])
VALUES (
    @Id,
    @AssetId,
    @Notes,
    @Created,
    @Reviewed,
    @Deleted,
    @CreatedBy,
    @ReviewedBy,
    @DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValueSubmittal_Save
--*	Saves an instance of PropertyValueSubmittal.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittal_Save]
	@Id					uniqueidentifier,
	@AssetId			uniqueidentifier,
	@Notes				varchar(MAX) = null,
	@Created			smalldatetime,
	@Reviewed			smalldatetime = null,
	@Deleted			smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@ReviewedBy			uniqueidentifier = null,
	@DeletedBy			uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[PropertyValueSubmittals] WHERE [Id] = @Id)

	EXEC [dbo].[spr_PropertyValueSubmittal_Update] @Id, @AssetId, @Notes, @Created, @Reviewed, @Deleted, @CreatedBy, @ReviewedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_PropertyValueSubmittal_Insert] @Id, @AssetId, @Notes, @Created, @Reviewed, @Deleted, @CreatedBy, @ReviewedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValueSubmittal_Update
--*	Updates an existing record for a/an PropertyValueSubmittal.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_PropertyValueSubmittal_Update]
	@Id					uniqueidentifier,
	@AssetId			uniqueidentifier,
	@Notes				varchar(MAX) = null,	
	@Created			smalldatetime,
	@Reviewed			smalldatetime = null,
	@Deleted			smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@ReviewedBy			uniqueidentifier = null,
	@DeletedBy			uniqueidentifier = null
AS

UPDATE [dbo].[PropertyValueSubmittals] SET
	[AssetId] = @AssetId,
	[Notes] = @Notes,
	[Created] = @Created,
	[Reviewed] = @Reviewed,
	[Deleted] = @Deleted,
	[CreatedBy] = @CreatedBy,
	[ReviewedBy] = @ReviewedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[PropertyValueSubmittals].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValue_Delete
--*	Deletes the record for a/an PropertyValue.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_PropertyValue_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[PropertyValues] 
SET 
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_PropertyValue_DeleteForAsset]
	@PropertyId	uniqueidentifier,
	@DeletedBy	uniqueidentifier,
	@AssetId	uniqueidentifier
AS

UPDATE [dbo].[PropertyValues] 
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE
	[AssetId] = @AssetId AND
	[PropertyId] = @PropertyId AND
	[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValue_Get
--*	Retrieves an instance of PropertyValue based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================
--* 10 Nov 2009		JRS		Added [SubmittalGroupId] to associate PropertyValue with a submittal group
--* 05 Feb 2010		JRS		Added [Rejected] and [RejectedBy]

CREATE PROCEDURE [dbo].[spr_PropertyValue_Get]
	@Id	uniqueidentifier
AS

SELECT
	[PropertyId]		AS	[PropertyId],
	[AssetId]			AS	[AssetId],
	[SubmittalGroupId]	AS	[SubmittalGroupId],
	[Value]				AS	[Value],
	[Order]				AS	[Order],
	[Created]			AS	[Created],
	[Approved]			AS	[Approved],
	[Rejected]			AS	[Rejected],
	[Deleted]			AS	[Deleted],
	[CreatedBy]			AS	[CreatedBy],
	[ApprovedBy]		AS	[ApprovedBy],
	[RejectedBy]		AS	[RejectedBy],
	[DeletedBy]			AS	[DeletedBy]
FROM [dbo].[PropertyValues] WITH (NoLock)
WHERE
	[dbo].[PropertyValues].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Thursday, July 23, 2009 12:18 PM
--*	spr_PropertyValue_Get_IgnoreDeletedFlag
--*	Retrieves an instance of PropertyValue based on the primary key of Id
--* ==================================================
--* 10 Nov 2009		JRS		Added [SubmittalGroupId] to associate PropertyValue with a submittal group
--* 05 Feb 2010		JRS		Added [Rejected] and [RejectedBy]

CREATE PROCEDURE [dbo].[spr_PropertyValue_Get_IgnoreDeletedFlag]
	@Id	uniqueidentifier
AS

SELECT
	[PropertyId]		AS	[PropertyId],
	[AssetId]			AS	[AssetId],
	[SubmittalGroupId]	AS	[SubmittalGroupId],
	[Value]				AS	[Value],
	[Order]				AS	[Order],
	[Created]			AS	[Created],
	[Approved]			AS	[Approved],
	[Rejected]			AS	[Rejected],
	[Deleted]			AS	[Deleted],
	[CreatedBy]			AS	[CreatedBy],
	[ApprovedBy]		AS	[ApprovedBy],
	[RejectedBy]		AS	[RejectedBy],
	[DeletedBy]			AS	[DeletedBy]
FROM [dbo].[PropertyValues] WITH (NoLock)
WHERE
	[dbo].[PropertyValues].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValue_Insert
--*	Inserts a new record for a new PropertyValue.
--*	Generated by BizObjGenerator
--* ==================================================
--* 05 Feb 2010		JRS		Added [Rejected] and [RejectedBy]

CREATE PROCEDURE [dbo].[spr_PropertyValue_Insert]
	@Id					uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@AssetId			uniqueidentifier,
	@SubmittalGroupId	uniqueidentifier = null,
	@Value				nvarchar(MAX) = null,
	@Order				int = null,
	@Created			smalldatetime,
	@Approved			smalldatetime = null,
	@Deleted			smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@ApprovedBy			uniqueidentifier = null,
	@DeletedBy			uniqueidentifier = null,
	@Rejected			smalldatetime = null,
	@RejectedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[PropertyValues] (
	[Id],
	[PropertyId],
	[AssetId],
	[SubmittalGroupId],
	[Value],
	[Order],
	[Created],
	[Approved],
	[Deleted],
	[CreatedBy],
	[ApprovedBy],
	[DeletedBy],
	[Rejected],
	[RejectedBy])
VALUES (
	@Id,
	@PropertyId,
	@AssetId,
	@SubmittalGroupId,
	@Value,
	@Order,
	@Created,
	@Approved,
	@Deleted,
	@CreatedBy,
	@ApprovedBy,
	@DeletedBy,
	@Rejected,
	@RejectedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValue_Save
--*	Saves an instance of PropertyValue.
--*	Generated by BizObjGenerator
--* ==================================================
--* 05 Feb 2010		JRS		Added [Rejected] and [RejectedBy]

CREATE PROCEDURE [dbo].[spr_PropertyValue_Save]
	@Id					uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@AssetId			uniqueidentifier,
	@SubmittalGroupId	uniqueidentifier = null,
	@Value				nvarchar(MAX) = null,
	@Order				int = null,
	@Created			smalldatetime,
	@Approved			smalldatetime = null,
	@Deleted			smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@ApprovedBy			uniqueidentifier = null,
	@DeletedBy			uniqueidentifier = null,
	@Rejected			smalldatetime = null,
	@RejectedBy			uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[PropertyValues] WHERE [Id] = @Id)

	EXEC [dbo].[spr_PropertyValue_Update] @Id, @PropertyId, @AssetId,
	@SubmittalGroupId, @Value, @Order, @Created, @Approved, @Deleted, @CreatedBy, @ApprovedBy, @DeletedBy, @Rejected, @RejectedBy

ELSE

	EXEC [dbo].[spr_PropertyValue_Insert] @Id, @PropertyId, @AssetId,
	@SubmittalGroupId, @Value, @Order, @Created, @Approved, @Deleted, @CreatedBy, @ApprovedBy, @DeletedBy, @Rejected, @RejectedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_PropertyValue_Update
--*	Updates an existing record for a/an PropertyValue.
--*	Generated by BizObjGenerator
--* ==================================================
--* 05 Feb 2010		JRS		Added [Rejected] and [RejectedBy]

CREATE PROCEDURE [dbo].[spr_PropertyValue_Update]
	@Id					uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@AssetId			uniqueidentifier,
	@SubmittalGroupId	uniqueidentifier = null,
	@Value				nvarchar(MAX) = null,
	@Order				int = null,
	@Created			smalldatetime,
	@Approved			smalldatetime = null,
	@Deleted			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@ApprovedBy			uniqueidentifier = null,
	@DeletedBy			uniqueidentifier = null,
	@Rejected			smalldatetime = null,
	@RejectedBy			uniqueidentifier = null
AS

UPDATE [dbo].[PropertyValues] SET
	[PropertyId] = @PropertyId,
	[AssetId] = @AssetId,
	[SubmittalGroupId] = @SubmittalGroupId,
	[Value] = @Value,
	[Order] = @Order,
	[Created] = @Created,
	[Approved] = @Approved,
	[Deleted] = @Deleted,
	[CreatedBy] = @CreatedBy,
	[ApprovedBy] = @ApprovedBy,
	[DeletedBy] = @DeletedBy,
	[Rejected] = @Rejected,
	[RejectedBy] = @RejectedBy
WHERE [dbo].[PropertyValues].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_PropertyValues_GetPotentialByAssetTypeIds]
	@AssetTypeIds varchar(max),
	@PropertyId	uniqueidentifier
AS

SELECT DISTINCT
	PV.[Value]
FROM [PropertyValues] PV WITH (NoLock)
INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = PV.[AssetId]
WHERE
	(A.[AssetTypeId] IN (SELECT [Value] FROM dbo.Split(@AssetTypeIds, ','))) AND
	(PV.[PropertyId] = @PropertyId) AND
	(PV.[Approved] IS NOT NULL) AND
	(PV.[Deleted] IS NULL) AND
	(PV.[Value] IS NOT NULL) AND
	(PV.[Value] != '') AND 
	(PV.[Value] != ' ') AND
	(A.[Deleted] IS NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Property_Delete
--*	Deletes the record for a/an Property.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_Property_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[Properties]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
	AND [IsSystem] = 0
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Property_Get
--*	Retrieves an instance of Property based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Property_Get]
	@Id	uniqueidentifier
AS

SELECT
	[DataTypeId] AS [DataTypeId],
	[Precision] AS [Precision],
	[PickListId] AS [PickListId],
	[RoleId] AS [RoleId],
	[IsOrdered] AS [IsOrdered],
	[AllowMultiValue] AS [AllowMultiValue],
	[Name] AS [Name],
	[DisplayValue] AS [DisplayValue],
	[Singular] AS [Singular],
	[Plural] AS [Plural],
	[Description] AS [Description],
	[AssetTypeId] AS [AssetTypeId],
	[AssetTypeIsInstance] AS [AssetTypeIsInstance],
	[Created] AS [Created],
	[Approved] AS [Approved],
	[LastModified] AS [LastModified],
	[CreatedBy] AS [CreatedBy],
	[ApprovedBy] AS [ApprovedBy],
	[LastModifiedBy] AS [LastModifiedBy],
	[Deleted] AS [Deleted],
	[DeletedBy] AS [DeletedBy],
	[IsSystem] AS [IsSystem],
	[SystemTypeId] AS [SystemTypeId]
FROM [dbo].[Properties] WITH (NoLock)
WHERE
	[dbo].[Properties].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Property_Insert
--*	Inserts a new record for a new Property.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Property_Insert]
	@Id						uniqueidentifier,
	@DataTypeId				tinyint,
	@Precision				tinyint = null,
	@PickListId				uniqueidentifier = null,
	@RoleId					uniqueidentifier = null,
	@AllowMultiValue		bit,
	@IsOrdered				bit,
	@Name					varchar(100),
	@DisplayValue			varchar(100) = null,
	@Description			varchar(MAX) = null,
	@AssetTypeId			uniqueidentifier = null,
	@AssetTypeIsInstance	bit = null,
	@Created				smalldatetime,
	@Approved				smalldatetime = null,
	@LastModified			smalldatetime = null,
	@Deleted				smalldatetime = null,
	@CreatedBy				uniqueidentifier,
	@ApprovedBy				uniqueidentifier = null,
	@LastModifiedBy			uniqueidentifier = null,
	@DeletedBy				uniqueidentifier = null,
	@IsSystem				bit,
	@SystemTypeId			int
AS

INSERT INTO [dbo].[Properties] (
	[Id],
	[DataTypeId],
	[Precision],
	[PickListId],
	[RoleId],
	[AllowMultiValue],
	[IsOrdered],
	[Name],
	[DisplayValue],
	[Description],
	[AssetTypeId],
	[AssetTypeIsInstance],
	[Created],
	[Approved],
	[LastModified],
	[Deleted],
	[CreatedBy],
	[ApprovedBy],
	[LastModifiedBy],
	[DeletedBy],
	[IsSystem],
	[SystemTypeId])
VALUES (
	@Id,
	@DataTypeId,
	@Precision,
	@PickListId,
	@RoleId,
	@AllowMultiValue,
	@IsOrdered,
	@Name,
	@DisplayValue,
	@Description,
	@AssetTypeId,
	@AssetTypeIsInstance,
	@Created,
	@Approved,
	@LastModified,
	@Deleted,
	@CreatedBy,
	@ApprovedBy,
	@LastModifiedBy,
	@DeletedBy,
	@IsSystem,
	@SystemTypeId)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Property_Save
--*	Saves an instance of Property.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Property_Save]
	@Id						uniqueidentifier,
	@DataTypeId				tinyint,
	@Precision				tinyint = null,
	@PickListId				uniqueidentifier = null,
	@RoleId					uniqueidentifier = null,
	@AllowMultiValue		bit,
	@IsOrdered				bit,
	@Name					varchar(100),
	@DisplayValue			varchar(100) = null,
	@Description			varchar(MAX) = null,
	@AssetTypeId			uniqueidentifier = null,
	@AssetTypeIsInstance	bit = null,
	@Created				smalldatetime,
	@Approved				smalldatetime = null,
	@LastModified			smalldatetime = null,
	@Deleted				smalldatetime = null,
	@CreatedBy				uniqueidentifier,
	@ApprovedBy				uniqueidentifier = null,
	@LastModifiedBy			uniqueidentifier = null,
	@DeletedBy				uniqueidentifier = null,
	@IsSystem				bit,
	@SystemTypeId			int
AS


IF EXISTS (SELECT * FROM [dbo].[Properties] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Property_Update] @Id, @DataTypeId, @Precision, @PickListId, @RoleId, @AllowMultiValue,
		@IsOrdered, @Name, @DisplayValue, @Description, @AssetTypeId, @AssetTypeIsInstance, @Created, 
		@Approved, @LastModified, @Deleted, @CreatedBy, @ApprovedBy, @LastModifiedBy, @DeletedBy, @IsSystem, @SystemTypeId

ELSE

	EXEC [dbo].[spr_Property_Insert] @Id, @DataTypeId, @Precision, @PickListId, @RoleId, @AllowMultiValue,
		@IsOrdered, @Name, @DisplayValue, @Description, @AssetTypeId, @AssetTypeIsInstance, @Created,
		@Approved, @LastModified, @Deleted, @CreatedBy, @ApprovedBy, @LastModifiedBy, @DeletedBy, @IsSystem, @SystemTypeId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Property_Update
--*	Updates an existing record for a/an Property.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Property_Update]
	@Id						uniqueidentifier,
	@DataTypeId				tinyint,
	@Precision				tinyint = null,
	@PickListId				uniqueidentifier = null,
	@RoleId					uniqueidentifier = null,
	@AllowMultiValue		bit,
	@IsOrdered				bit,
	@Name					varchar(100),
	@DisplayValue			varchar(100) = null,
	@Description			varchar(MAX) = null,
	@AssetTypeId			uniqueidentifier = null,
	@AssetTypeIsInstance	bit = null,
	@Created				smalldatetime,
	@Approved				smalldatetime = null,
	@LastModified			smalldatetime = null,
	@Deleted				smalldatetime = null,
	@CreatedBy				uniqueidentifier,
	@ApprovedBy				uniqueidentifier = null,
	@LastModifiedBy			uniqueidentifier = null,
	@DeletedBy				uniqueidentifier = null,
	@IsSystem				bit,
	@SystemTypeId			int
AS

UPDATE [dbo].[Properties] SET
	[DataTypeId] = @DataTypeId,
	[Precision] = @Precision,
	[PickListId] = @PickListId,
	[RoleId] = @RoleId,
	[AllowMultiValue] = @AllowMultiValue,
	[IsOrdered] = @IsOrdered,
	[Name] = @Name,
	[DisplayValue] = @DisplayValue,
	[Description] = @Description,
	[AssetTypeId] = @AssetTypeId,
	[AssetTypeIsInstance] = @AssetTypeIsInstance,
	[Created] = @Created,
	[Approved] = @Approved,
	[LastModified] = @LastModified,
	[Deleted] = @Deleted,
	[CreatedBy] = @CreatedBy,
	[ApprovedBy] = @ApprovedBy,
	[LastModifiedBy] = @LastModifiedBy,
	[DeletedBy] = @DeletedBy,
	[IsSystem] = @IsSystem,
	[SystemTypeId] = @SystemTypeId 
WHERE [dbo].[Properties].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_QuestionGroup_Insert]
	@Id					uniqueidentifier,
	@SurveyId			uniqueidentifier,
	@AssetId			uniqueidentifier,
	@Caption			varchar(200),
	@MemberId			uniqueidentifier,
	@Created			smalldatetime,
	@Closed				smalldatetime = null
AS

INSERT INTO [dbo].[QuestionGroups] (
	[Id],
	[SurveyId],
	[AssetId],
	[Caption],
	[MemberId],
	[Created],
	[Closed])
VALUES (@Id, @SurveyId, @AssetId, @Caption, @MemberId, @Created, @Closed )
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Question_Insert]
	@Id					uniqueidentifier,
	@QuestionGroupId	uniqueidentifier,
	@Index				int,
	@AllowUnknown		bit,
	@DisableNext		bit,
	@DisableCondition	varchar(50),
	@MemberId			uniqueidentifier,
	@PropertyValueId	uniqueidentifier,
	@Message			varchar(MAX),
	@Created			smalldatetime
AS

INSERT INTO [dbo].[Questions] (
	[Id],
	[QuestionGroupId],
	[Index],
	[AllowUnknown],
	[DisableNext],
	[DisableCondition],
	[MemberId],
	[PropertyValueId],
	[Message],
	[Created])
VALUES (
	@Id,
	@QuestionGroupId,
	@Index,
	@AllowUnknown,
	@DisableNext,
	@DisableCondition,
	@MemberId,
	@PropertyValueId,
	@Message,
	@Created )
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_ReportCache_View]

AS

select r.Id, r.ReportName, IsNull(rc.LastModified, rc.Created) , dbo.GetReportHitCount(r.Id) as [Hits]  
from Reports r WITH (NoLock)
left join ReportCache rc WITH (NoLock) on rc.ReportId = r.Id
order by r.ReportName
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ReportFilters_Get]
	@ReportId	uniqueidentifier
AS

SELECT
	RF.[Id]
	,RF.[PropertyId]
	,IsNull(P.[DisplayValue], P.[Name]) AS [Property]
	,RF.[OperatorId]
	,O.[Operator]
	,RF.[Value]
	,RF.[Order]
FROM [ReportsFilters] RF
	INNER JOIN [Properties] P ON P.[Id] = RF.[PropertyId]
	INNER JOIN [lkOperators] O ON O.[Id] = RF.[OperatorId]
WHERE RF.[ReportId] = @ReportId
ORDER BY RF.[Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ReportList_GetByMemberId]
	@MemberId	uniqueidentifier
AS

SELECT DISTINCT
	R.[Id],
	R.[ReportName]
FROM [Reports] R
INNER JOIN [RoleReports] RR ON RR.[ReportId] = R.[Id] 
INNER JOIN [Roles] RL ON RL.[Id] = RR.[RoleId] 
INNER JOIN [RoleMembers] RM ON RM.[RoleId] = RL.[Id]
WHERE
	R.[Deleted] IS NULL
		AND
	RR.[Deleted] IS NULL
		AND 
	RL.[Deleted] IS NULL
		AND
	RM.[Deleted] IS NULL
		AND
	RM.[MemberId] = @MemberId
ORDER BY R.[ReportName]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ReportProperties_Get]
	@Id		uniqueidentifier
AS

	SELECT RP.[Id],
		RP.[PropertyId],
		RP.[SubPropertyId],
		RP.[Order],
		RP.[IsGrouping],
		RP.[CustomReportFieldTypeId],
		RP.[TimeElapsedTypeId],
		RP.[TimeElapsedFormatId],
		RP.[TimeElapsedSpecificDate],
		RP.[Label],
		ISNULL(P.[DisplayValue], P.[Name]) AS [Property],
		ISNULL(P2.[DisplayValue], P2.[Name]) AS [SubProperty],
		RP.[Logic]
	FROM [ReportsProperties] RP WITH (NoLock)
		LEFT JOIN [Properties] P WITH (NoLock) ON P.[Id] = RP.[PropertyId]
		LEFT JOIN [Properties] P2 WITH (NoLock) ON P2.[Id] = RP.[SubPropertyId]
	WHERE
		RP.[ReportId] = @Id AND
		P.[Deleted] IS NULL
	ORDER BY RP.[Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ReportProperty_Insert]
	@Id							uniqueidentifier,
	@ReportId					uniqueidentifier,
	@PropertyId					uniqueidentifier = null,
	@SubPropertyId				uniqueidentifier = null,
	@Order						int,
	@IsGrouping					bit,
	@CustomReportFieldTypeId	int,
	@TimeElapsedTypeId			int,
	@TimeElapsedFormatId		int,
	@TimeElapsedSpecificDate	smalldatetime = null,
	@Label						varchar(50) = null,
	@Logic						varchar(100) = null	
AS

INSERT INTO [dbo].[ReportsProperties] (
	[Id],
	[ReportId],
	[PropertyId],
	[SubPropertyId],
	[Order],
	[IsGrouping],
	[CustomReportFieldTypeId],
	[TimeElapsedTypeId],
	[TimeElapsedFormatId],
	[TimeElapsedSpecificDate],
	[Label],
	[Logic])
VALUES (
	@Id,
	@ReportId,
	@PropertyId,
	@SubPropertyId,
	@Order,
	@IsGrouping,
	@CustomReportFieldTypeId,
	@TimeElapsedTypeId,
	@TimeElapsedFormatId,
	@TimeElapsedSpecificDate,
	@Label,
	@Logic)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Report_Cache]
	@ReportId			uniqueidentifier,
	@DocumentId			uniqueidentifier
AS

IF EXISTS (SELECT * FROM [dbo].[ReportCache] WHERE [ReportId] = @ReportId)

	UPDATE [ReportCache]
	SET [DocumentId] = @DocumentId, [LastModified] = GETDATE()
	WHERE [ReportId] = @ReportId

ELSE

	INSERT INTO [ReportCache] (
		[ReportId], [DocumentId], [Created]) VALUES (
		@ReportId, @DocumentId, GETDATE())
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_Report_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [Reports] SET
	[Deleted] = GETDATE(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Report_Get]
	@Id	uniqueidentifier
AS

SELECT 
	[ReportName],
	[AssetTypeId],
	[FilterExpression],
	[StoredProc],
	[IsInstance],
	dbo.AssetType_GetReportingLabel([AssetTypeId], [IsInstance]) AS [ReportingLabel],
	[Created],
	[CreatedBy]
FROM [Reports] WITH (NoLock)
WHERE [Id] = @Id
exec spr_ReportProperties_Get @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Report_GetOrderByColumnName]
	@Id	uniqueidentifier
AS

SELECT RP.[Label]
FROM [ReportsProperties] RP
INNER JOIN [Reports] R ON R.[Id] = RP.[ReportId]
WHERE RP.[Order] = 0 AND R.[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_Report_Insert]
	@Id					uniqueidentifier,
	@ReportName			varchar(MAX),
	@AssetTypeId		uniqueidentifier,
	@FilterExpression	varchar(MAX) = null,
	@StoredProc			varchar(100) = null,
	@IsInstance			bit,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@FilterString		varchar(MAX) = null
AS

INSERT INTO [dbo].[Reports] (
	[Id],
	[ReportName],
	[AssetTypeId],
	[IsInstance],
	[FilterExpression],
	[StoredProc],
	[Created],
	[CreatedBy],
	[FilterString])
VALUES (
	@Id,
	@ReportName,
	@AssetTypeId,
	@IsInstance,
	@FilterExpression,
	@StoredProc,
	@Created,
	@CreatedBy,
	@FilterString)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Report_Kill]
	@Id	uniqueidentifier
AS

delete from ReportsProperties where ReportId = @Id
delete from ReportsFilters where ReportId = @Id
delete from Reports where Id = @Id
delete from RolesReports where ReportId = @Id

update [Members] SET [LastModified] = GETDATE(), [LastModifiedBy] = '6CACA132-7827-4DE6-81E6-2B7D8DB939B9'
WHERE [Id] IN (
	SELECT DISTINCT RM.[MemberId] FROM [Roles] R
	INNER JOIN [RoleReports] RR ON RR.[RoleId] = R.[Id]
	INNER JOIN [RoleMembers] RM ON RM.[RoleId] = R.[Id]
	WHERE RR.ReportId = @Id
)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Report_Save]
	@Id					uniqueidentifier,
	@ReportName			varchar(MAX),
	@AssetTypeId		uniqueidentifier,
	@FilterExpression	varchar(MAX) = null,
	@StoredProc			varchar(100) = null,
	@IsInstance			bit,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null,
	@FilterString		varchar(MAX) = null
AS


IF EXISTS (SELECT * FROM [dbo].[Reports] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Report_Update] @Id, @ReportName, @FilterExpression, @StoredProc, @Deleted, @DeletedBy, @FilterString

ELSE

	EXEC [dbo].[spr_Report_Insert] @Id, @ReportName, @AssetTypeId, @FilterExpression, @StoredProc, @IsInstance, @Created, @CreatedBy, @FilterString
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Report_Update]
	@Id					uniqueidentifier,
	@ReportName			varchar(MAX),
	@FilterExpression	varchar(MAX) = null,
	@StoredProc			varchar(100) = null,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null,
	@FilterString		varchar(MAX) = null
AS

UPDATE [dbo].[Reports] SET
	[ReportName] = @ReportName,
	[FilterExpression] = @FilterExpression,
	[StoredProc] = @StoredProc,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy,
	[FilterString] = @FilterString
WHERE [dbo].[Reports].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Reports_GetDictionary]

AS

SELECT [Id], [ReportName] FROM [Reports] WITH (NoLock) WHERE [Deleted] IS NULL ORDER BY [ReportName]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_RoleProcessRelation_Insert]
	@Id				uniqueidentifier,
	@RoleId			uniqueidentifier,
	@AssetTypeId	uniqueidentifier,
	@ProcessId		uniqueidentifier,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[RolesAssetTypesProcesses] (
	[Id],
	[RoleId],
	[AssetTypeId],
	[ProcessId],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@RoleId,
	@AssetTypeId,
	@ProcessId,
	@Created,
	@CreatedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_RoleProcessRelation_Save]
	@Id				uniqueidentifier,
	@RoleId			uniqueidentifier,
	@AssetTypeId	uniqueidentifier,
	@ProcessId		uniqueidentifier,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[RolesAssetTypesProcesses] WHERE [Id] = @Id)

	EXEC [dbo].[spr_RoleProcessRelation_Update] @Id, @RoleId, @AssetTypeId, @ProcessId, @Created, @CreatedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_RoleProcessRelation_Insert] @Id, @RoleId, @AssetTypeId, @ProcessId, @Created, @CreatedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_RoleProcessRelation_Update]
	@Id				uniqueidentifier,
	@RoleId			uniqueidentifier,
	@AssetTypeId	uniqueidentifier,
	@ProcessId		uniqueidentifier,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[RolesAssetTypesProcesses] SET
	[RoleId] = @RoleId,
	[AssetTypeId] = @AssetTypeId,
	[ProcessId] = @ProcessId,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[RolesAssetTypesProcesses].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelationList_DeleteByRoleId
--*	Deletes a RoleViewRelationList based on the foreign key of RoleId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_RoleViewRelationList_DeleteByRoleId]
	@RoleId		uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [RolesViews]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [RoleId] = @RoleId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelationList_DeleteByViewId
--*	Deletes a RoleViewRelationList based on the foreign key of ViewId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_RoleViewRelationList_DeleteByViewId]
	@ViewId		uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [RolesViews]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [ViewId] = @ViewId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelationList_Get
--*	Retrieves a RoleViewRelationList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_RoleViewRelationList_Get]
AS

SELECT
	[Id] AS [Id],
	[RoleId] AS [RoleId],
	[ViewId] AS [ViewId],
	[IsAutoApprove] AS [IsAutoApprove],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[RolesViews] WITH (NoLock)
	WHERE [dbo].[RolesViews].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelationList_GetByRoleId
--*	Retrieves a RoleViewRelationList based on the foreign key of RoleId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_RoleViewRelationList_GetByRoleId]
	@RoleId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[RoleId] AS [RoleId],
	[ViewId] AS [ViewId],
	[IsAutoApprove] AS [IsAutoApprove],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[RolesViews] WITH (NoLock)
WHERE
	[dbo].[RolesViews].[RoleId] = @RoleId
		AND
	[dbo].[RolesViews].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelationList_GetByViewId
--*	Retrieves a RoleViewRelationList based on the foreign key of ViewId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_RoleViewRelationList_GetByViewId]
	@ViewId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[RoleId] AS [RoleId],
	[ViewId] AS [ViewId],
	[IsAutoApprove] AS [IsAutoApprove],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[RolesViews] WITH (NoLock)
WHERE
	[dbo].[RolesViews].[ViewId] = @ViewId
		AND
	[dbo].[RolesViews].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelation_Delete
--*	Deletes the record for a/an RoleViewRelation.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_RoleViewRelation_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[RolesViews]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelation_Get
--*	Retrieves a RoleViewRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_RoleViewRelation_Get]
	@Id	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[RoleId] AS [RoleId],
	[ViewId] AS [ViewId],
	[IsAutoApprove] AS [IsAutoApprove],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[RolesViews] WITH (NoLock)
	WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelation_Insert
--*	Inserts a new record for a new RoleViewRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_RoleViewRelation_Insert]
	@Id				uniqueidentifier,
	@RoleId			uniqueidentifier,
	@ViewId			uniqueidentifier,
	@IsAutoApprove	bit,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[RolesViews] (
	[Id],
	[RoleId],
	[ViewId],
	[IsAutoApprove],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@RoleId,
	@ViewId,
	@IsAutoApprove,
	@Created,
	@CreatedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelation_Save
--*	Saves an instance of RoleViewRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_RoleViewRelation_Save]
	@Id				uniqueidentifier,
	@RoleId			uniqueidentifier,
	@ViewId			uniqueidentifier,
	@IsAutoApprove	bit,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[RolesViews] WHERE [Id] = @Id)

	EXEC [dbo].[spr_RoleViewRelation_Update] @Id, @RoleId, @ViewId, @IsAutoApprove, @Created, @CreatedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_RoleViewRelation_Insert] @Id, @RoleId, @ViewId, @IsAutoApprove, @Created, @CreatedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_RoleViewRelation_Update
--*	Updates an existing record for a/an RoleViewRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_RoleViewRelation_Update]
	@Id				uniqueidentifier,
	@RoleId			uniqueidentifier,
	@ViewId			uniqueidentifier,
	@IsAutoApprove	bit,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[RolesViews] SET
	[RoleId] = @RoleId,
	[ViewId] = @ViewId,
	[IsAutoApprove] = @IsAutoApprove,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[RolesViews].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Role_ContainsMember]
	@RoleId	uniqueidentifier,
	@MemberId	uniqueidentifier
AS

SELECT
	COUNT(*)
FROM [RoleMembers] RM WITH (NoLock)
WHERE
	RM.[RoleId] = @RoleId AND
	RM.[MemberId] = @MemberId AND
	RM.[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Role_Delete
--*	Deletes the record for a/an Role.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_Role_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[Roles]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Role_Get
--*	Retrieves an instance of Role based on the primary key of Id
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Role_Get]
	@Id	uniqueidentifier
AS

SELECT
	[Name] AS [Name],
	[Description] AS [Description],
	[IsSystem] AS [IsSystem],
	[IsActive] AS [IsActive],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[LastModified] AS [LastModified],
	[LastModifiedBy] AS [LastModifiedBy],
	[Deleted] AS [Deleted],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[Roles] WITH (NoLock)
WHERE
	[dbo].[Roles].[Id] = @Id

-- Get the role members
SELECT RM.[Id], RM.[MemberId], dbo.GetMemberValueById(RM.[MemberId]) AS [Member], RM.[Created], RM.[CreatedBy]
FROM [RoleMembers] RM
INNER JOIN [Members] M ON M.[Id] = RM.[MemberId]
WHERE
	(RM.[RoleId] = @Id)
	AND (RM.[Deleted] IS NULL)
	AND (M.[Deleted] IS NULL)
ORDER BY M.[LName], M.[FName] 

-- Get the reports for the role
IF (@Id = '68F8BBAB-C026-4ED4-A875-164C48E8874D') -- Admin
	SELECT NEWID(), [Id], [ReportName], [Created], [CreatedBy] FROM [Reports] WHERE ([Deleted] IS NULL)
ELSE
	SELECT RR.[ID], RR.[ReportId], R.[ReportName], RR.[Created], RR.[CreatedBy]
	FROM [RoleReports] RR
	INNER JOIN [Reports] R ON R.[Id] = RR.[ReportId]
	WHERE
		(RR.[RoleId] = @Id)
		AND (RR.[Deleted] IS NULL)
		AND (R.[Deleted] IS NULL)

-- Get the assetTypes and associated permissions and filters for the role
SELECT
	RAT.[Id]
	, AT.[Id] AS [AssetTypeId]
	, RAT.[IsInstance] AS [DefIns]
	, RAT.[IsInherited] AS [Inherited]
	, RAT.InheritFrom AS [Inherited From]
	, RP.[Id] AS [RolePermissionId]
	, RP.[PermissionId] AS [PermissionId]
	, RP.[IsIncludesFilter] AS [IsIncludes]
	, RP.FilterLogic as [FilterExpression]
	, RF.[Id] AS [FilterId]
	, RF.[Order] AS [FilterIndex]
	, RF.[PropertyId] AS [PropertyId]
	, ISNULL(p.DisplayValue, P.Name) AS [Property]
	, RF.[OperatorId] AS [OpId]
	, OPS.Operator AS [Op]
	, RF.Value AS [FilterValue]
	, CASE (LEN(RF.Value)) WHEN 36 THEN [dbo].[GetValueById](RF.Value) ELSE RF.Value END AS [FilterValueDisplay]
FROM [Roles] R
INNER JOIN [RoleAssetTypes] RAT ON RAT.RoleId = R.[Id]
INNER JOIN [AssetTypes] AT ON AT.Id = RAT.AssetTypeId 
	LEFT JOIN [RoleAssetTypes] RAT2 on RAT2.Id = RAT.InheritFrom
LEFT JOIN [RolePermissions] RP ON RP.RoleAssetTypesId = RAT.Id
	LEFT JOIN [RoleFilters] RF ON RF.RolePermissionsId = RP.Id 
	LEFT JOIN [Properties] P ON P.Id = RF.PropertyId
	LEFT JOIN [lkOperators] OPS ON OPS.Id = RF.OperatorId
WHERE (R.[Id] = @Id)
ORDER BY [AT].[Name], [DefIns], RP.PermissionId, [FilterIndex]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Role_Get_MemberIds]
	@RoleId	uniqueidentifier
AS

SELECT
	[MemberId] AS [Id]
FROM [RoleMembers] RM WITH (NoLock)
	INNER JOIN [Roles] R WITH (NoLock) ON R.[Id] = RM.[RoleId]
	INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = RM.[MemberId]
WHERE
	(RM.[Deleted] IS NULL) AND
	(M.[Deleted] IS NULL) AND
	(RM.[RoleId] = @RoleId)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Role_HasPermission]
	@RoleId			uniqueidentifier,
	@AssetTypeId	uniqueidentifier,
	@IsInstance		bit,
	@PermissionId	int
AS

SELECT COUNT(*)
FROM [RoleAssetTypes] RATP WITH (NoLock)
	INNER JOIN [Roles] R WITH (NoLock) ON R.[Id] = RATP.[RoleId]
	INNER JOIN [RolePermissions] RP ON RP.RoleAssetTypesId = RATP.Id
WHERE
	(R.[Deleted] IS NULL)
	AND (R.[Id] = @RoleId)
	AND (RP.[PermissionId] = @PermissionId)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Role_Insert
--*	Inserts a new record for a new Role.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Role_Insert]
	@Id				uniqueidentifier,
	@Name			varchar(100),
	@Description	varchar(MAX) = null,
	@IsSystem		bit,
	@IsActive		bit,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@LastModified	smalldatetime = null,
	@LastModifiedBy	uniqueidentifier = null,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null,
	@RoleLead		uniqueidentifier = null
AS

INSERT INTO [dbo].[Roles] (
	[Id],
	[Name],
	[Description],
	[IsSystem],
	[IsActive],
	[Created],
	[CreatedBy],
	[LastModified],
	[LastModifiedBy],
	[Deleted],
	[DeletedBy],
	[RoleLead])
VALUES (
	@Id,
	@Name,
	@Description,
	@IsSystem,
	@IsActive,
	@Created,
	@CreatedBy,
	@LastModified,
	@LastModifiedBy,
	@Deleted,
	@DeletedBy,
	@RoleLead)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Role_IsValidId]
	@Id	uniqueidentifier
AS

SELECT COUNT(*)
FROM [Roles] WITH (NoLock)
WHERE ([Id] = @Id) AND ([Deleted] IS NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Role_Kill]
	@RoleId	uniqueidentifier
AS

DELETE FROM [RolesAssetTypes] WHERE [RoleId] = @RoleId
DELETE FROM [RolesAssetTypesFilters] WHERE [RoleId] = @RoleId 
DELETE FROM [RolesAssetTypesPermissions] WHERE [RoleId] = @RoleId
DELETE FROM [RolesMembers] WHERE [RoleId] = @RoleId 
DELETE FROM [RolesViews] WHERE [RoleId] = @RoleId
DELETE FROM [Roles] WHERE [Id] = @RoleId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Role_Save
--*	Saves an instance of Role.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Role_Save]
	@Id				uniqueidentifier,
	@Name			varchar(100),
	@Description	varchar(MAX) = null,
	@IsSystem		bit,
	@IsActive		bit,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@LastModified	smalldatetime = null,
	@LastModifiedBy	uniqueidentifier = null,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null,
	@RoleLead		uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[Roles] WHERE [Id] = @Id)
	BEGIN
		EXEC [dbo].[spr_Role_Update] @Id, @Name, @Description, @IsSystem, @IsActive, @Created, @CreatedBy, @LastModified, @LastModifiedBy, @Deleted, @DeletedBy, @RoleLead
	END
ELSE
	BEGIN
		EXEC [dbo].[spr_Role_Insert] @Id, @Name, @Description, @IsSystem, @IsActive, @Created, @CreatedBy, @LastModified, @LastModifiedBy, @Deleted, @DeletedBy, @RoleLead
	END
	
-- Mark any RoleMembers as having been updated to web clients will get permission changes
UPDATE [Members]
SET [LastModified] = GETDATE(), [LastModifiedBy] = [CreatedBy]
WHERE [Id] IN (
	SELECT RM.[MemberId]
	FROM [RolesMembers] RM
	WHERE RM.[RoleId] = @Id
)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Role_Update
--*	Updates an existing record for a/an Role.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Role_Update]
	@Id				uniqueidentifier,
	@Name			varchar(100),
	@Description	varchar(MAX) = null,
	@IsSystem		bit,
	@IsActive		bit,
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@LastModified	smalldatetime = null,
	@LastModifiedBy	uniqueidentifier = null,
	@Deleted		smalldatetime = null,
	@DeletedBy		uniqueidentifier = null,
	@RoleLead		uniqueidentifier = null
AS

UPDATE [dbo].[Roles] SET
	[Name] = @Name,
	[Description] = @Description,
	[IsSystem] = @IsSystem,
	[IsActive] = @IsActive,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[LastModified] = @LastModified,
	[LastModifiedBy] = @LastModifiedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy,
	[RoleLead] = @RoleLead 
WHERE [dbo].[Roles].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Roles_GetByMemberId]
	@MemberId	uniqueidentifier
AS

SELECT DISTINCT R.[Id]
FROM [RoleMembers] RM
INNER JOIN [Roles] R ON R.[Id] = RM.[RoleId]
WHERE
	(RM.[MemberId] = @MemberId)
	AND (RM.Deleted IS NULL)
	AND (R.[IsActive] = 1)
	AND (R.[Deleted] IS NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Roles_GetByUserAndView]
	@MemberId uniqueidentifier,
	@ViewId	uniqueidentifier
AS

SELECT DISTINCT R.[Id]
FROM [Roles] R WITH (NoLock)
INNER JOIN [RolesViews] RV WITH (NoLock) ON RV.[RoleId] = R.[Id]
INNER JOIN [RoleMembers] RM WITH (NoLock) ON RM.[RoleId] = R.[Id]
WHERE
	(RM.[MemberId] = @MemberId)
	AND (RV.[ViewId] = @ViewId)
	AND (R.[Deleted] IS NULL)
	AND (RV.[Deleted] IS NULL)
	AND (RM.[Deleted] IS NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Roles_GetByViewId]
	@ViewId	uniqueidentifier
AS

SELECT DISTINCT RV.[RoleId], R.[Name]
FROM [RolesViews] RV WITH (NoLock)
	INNER JOIN [Roles] R WITH (NoLock) ON R.[Id] = RV.[RoleId]
	INNER JOIN [Views] V WITH (NoLock) ON V.[Id] = RV.[ViewId]
WHERE
	([ViewId] = @ViewId)
	AND (RV.[Deleted] IS NULL)
	AND (R.[Deleted] IS NULL)
	AND (V.[Deleted] IS NULL)
ORDER BY R.[Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Roles_GetEditable]
	@MemberId	uniqueidentifier
AS

SELECT
	R.[Id]
	, R.[Name] 
	, AT.[Id] AS [AssetTypeId]
	, RAT.[IsInstance] AS [DefIns]
	, RP.[IsIncludesFilter] AS [IsIncludes]
FROM [Roles] R
INNER JOIN [RoleMembers] RM ON RM.[RoleId] = R.[Id]
INNER JOIN [RoleAssetTypes] RAT ON RAT.RoleId = R.[Id]
INNER JOIN [AssetTypes] AT ON AT.Id = RAT.AssetTypeId 
	LEFT JOIN [RoleAssetTypes] RAT2 on RAT2.Id = RAT.InheritFrom
LEFT JOIN [RolePermissions] RP ON RP.RoleAssetTypesId = RAT.Id
WHERE
	(RM.[MemberId] = @MemberId)
	AND (RAT.IsInherited = 0)
	AND (RP.PermissionId = 1)
	AND (RP.FilterLogic != '')
	AND (RP.FilterLogic IS NOT NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_SearchTag_Asset]
	@Phrase	varchar(MAX)
AS

SELECT
	A.[Id]
	,A.[Name] AS [Name]
	,A.[AssetTypeId] as [Type]
FROM [Assets] A WITH (NoLock)
INNER JOIN [AssetTypes] AT WITH (NoLock) ON AT.[Id] = A.[AssetTypeId]
WHERE
	(A.[AssetTypeId] != '6C5DE6FF-1F85-4016-84ED-2F78B7C10517')
		AND
	(A.[Name] LIKE @Phrase)
		AND
	(A.[Deleted] IS NULL)
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_SearchTag_User]
	@Phrase	varchar(MAX)
AS

select
	A.[Id],
	M.[FName] + ' ' + [M].[LName] AS [Name],
	A.[AssetTypeId]
FROM [Assets] A WITH (NoLock)
	INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = A.[Id]
WHERE
	(M.LName like @Phrase) or (M.FName like @Phrase) or (A.[DisplayValue] like @Phrase)
	AND (A.[Deleted] IS NULL)
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Search_Asset]
	@Phrase	varchar(MAX)
AS

SELECT
	A.[Id]
	,AT.[Name] + ' - ' + A.[Name] AS [Name]
	,A.[AssetTypeId]
	,COALESCE(dbo.GetAssetParentNameByInstanceId(A.[Id]), A.[Description]) AS [Description]
	,A.[IsInstance]	
FROM [Assets] A WITH (NoLock)
INNER JOIN [AssetTypes] AT WITH (NoLock) ON AT.[Id] = A.[AssetTypeId]
WHERE
	(A.[Name] LIKE @Phrase)
	AND (A.[Deleted] IS NULL)
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Search_PropVal_Text]
	@Phrase	varchar(MAX)
AS

SELECT TOP 100
	A.[Id]
	,AT.[Name] + ' - ' + IsNull(A.[DisplayValue], A.[Name]) + ' - ' + IsNull(P.[DisplayValue], P.[Name]) AS [Name]
	,A.[AssetTypeId]
	,PV.[Value]
	,A.[IsInstance]
FROM [PropertyValues] PV WITH (NoLock)
INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = PV.[AssetId]
INNER JOIN [AssetTypes] AT WITH (NoLock) ON AT.[Id] = A.[AssetTypeId]
INNER JOIN [Properties] P WITH (NoLock) ON P.[Id] = PV.[PropertyId]
WHERE
	PV.[Value] LIKE @Phrase
	AND (PV.[Deleted] IS NULL)
	AND (A.[Deleted] IS NULL)
	AND P.[DataTypeId] IN (0 ,1, 3, 5, 6, 7, 10, 20, 21)
	AND 
		((P.[IsSystem] = 1 AND P.[SystemTypeId] != 0)
			OR
		(P.[IsSystem] = 0))
	AND P.Id != 'ae73ceb5-4235-46c9-84ff-b67cf27dbe6d'
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Search_PropertyValue_Asset]
	@AssetIds	varchar(MAX),
	@MemberId	uniqueidentifier
AS

--set @MemberId = '5ff0b5c6-1dab-47ef-a1d7-86efb05911ab'
--set @AssetIds = '36C2465B-A4BA-4FCE-8552-00AF2B28F875,D934B5CA-423A-48A8-9CE9-068AAEC39BEA,5E289A76-E530-4E7C-B93A-0982DB3171BD,386F1F0D-36D0-4009-8241-09DE2D2F48D2,CA82EB75-1477-43C2-BC2C-0F7E4724BE10,AFAEE730-ABF7-4359-9FE6-0F8E14769619,F3F8BCD2-FA25-4322-906C-1364E0AE955D,0071CE31-50AA-4E58-80B9-1494398B12A7,D8ECB420-AFFB-478A-93C8-15000121660A,F829EA90-F5AE-4367-AF31-16A93E988A37,C69AE453-C60A-495D-810D-16E14EB4B457,FBC715DE-EEA6-48E2-896D-19FD007A7C02,32A1D3CC-B7A4-4153-909E-1C99E67A3940,D34EA492-D0ED-4039-8CCA-22A9506569B8,224E092B-2477-4A2F-9580-22EE9D70ED81,7DCD484E-217C-455A-B618-23104239725B,2E32148B-2B1F-43B7-9DE5-236DC2A77381'

SELECT TOP 100
	A.[Id],
	IsNull(A.[DisplayValue], A.[Name]) AS [AssetName],
	IsNull(P.[DisplayValue], P.[Name]) AS [Property],
	IsNull(A2.[DisplayValue], A2.[Name]) AS [AssetValue],
	A.[AssetTypeId],
	A.[Description],
	A.[IsInstance],
	A.[InstanceOfId]
FROM dbo.Split(@AssetIds, ',') S
INNER JOIN [PropertyValues] PV WITH (NoLock) ON PV.[Value] = S.[Value]
INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = PV.[AssetId]
INNER JOIN [Assets] A2 WITH (NoLock) ON A2.[Id] = S.[Value]
INNER JOIN [Properties] P WITH (NoLock) ON P.[Id] = PV.[PropertyId]
WHERE
	--(PV.[Value] = S.[Value]) AND
	(PV.[Deleted] IS NULL) AND
	(A.[Deleted] IS NULL) AND
	(P.[DataTypeId] <> 19 AND IsNull(P.[SystemTypeId], -1) <> 0) AND
	(PV.[Approved] IS NOT NULL)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_Search_User]
	@Phrase	varchar(MAX)
AS

select
	A.[Id],
	A.[DisplayValue] as [Name],
	A.[AssetTypeId],
	A.[Description],
	A.[IsInstance]
FROM [Assets] A WITH (NoLock)
	INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = A.[Id]
WHERE
	(M.LName like @Phrase) or (M.FName like @Phrase)
	AND (A.[Deleted] IS NULL)
ORDER BY [Name]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ServiceModule_Insert]
	@Id					uniqueidentifier,
	@Name				varchar(100),
	@Description		varchar(MAX),
	@AssemblyName		varchar(50) = null,
	@AssemblyNamespace	varchar(50) = null,
	@ClassName			varchar(50) = null,
	@AssemblyVersion	varchar(50) = null,
	@IsActive			bit,
	@Downloaded			smalldatetime = null,
	@Created			smalldatetime,
	@Deleted			smalldatetime = null,
	@LastRan			smalldatetime = null,
	@LastCompleted		smalldatetime = null
AS

                
INSERT INTO [dbo].[ServiceModules] (
	[Id],
	[Name],
	[Description],
	AssemblyName,
	AssemblyNamespace,
	ClassName,
	AssemblyVersion,
	IsActive,
	[Downloaded],
	[Created],
	[Deleted],
	[LastRan],
	[LastCompleted])
VALUES (
	@Id,
	@Name,
	@Description,
	@AssemblyName,
	@AssemblyNamespace,
	@ClassName,
	@AssemblyVersion,
	@IsActive,
	@Downloaded,
	@Created,
	@Deleted,
	@LastRan,
	@LastCompleted)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ServiceModule_Save]
	@Id					uniqueidentifier,
	@Name				varchar(100),
	@Description		varchar(MAX),
	@AssemblyName		varchar(50) = null,
	@AssemblyNamespace	varchar(50) = null,
	@ClassName			varchar(50) = null,
	@AssemblyVersion	varchar(50) = null,
	@IsActive			bit,
	@Downloaded			smalldatetime = null,
	@Created			smalldatetime,
	@Deleted			smalldatetime = null,
	@LastRan			smalldatetime = null,
	@LastCompleted		smalldatetime = null
AS

IF EXISTS (SELECT * FROM [dbo].[ServiceModules] WHERE [Id] = @Id)

	EXEC [dbo].[spr_ServiceModule_Update] @Id, @Name, @Description, @AssemblyName, @AssemblyNamespace, @ClassName, @AssemblyVersion, @IsActive, @Downloaded, @Created, @Deleted, @LastRan, @LastCompleted

ELSE

	EXEC [dbo].[spr_ServiceModule_Insert] @Id, @Name, @Description, @AssemblyName, @AssemblyNamespace, @ClassName, @AssemblyVersion, @IsActive, @Downloaded, @Created, @Deleted, @LastRan, @LastCompleted
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ServiceModule_Update]
	@Id					uniqueidentifier,
	@Name				varchar(100),
	@Description		varchar(MAX),
	@AssemblyName		varchar(50) = null,
	@AssemblyNamespace	varchar(50) = null,
	@ClassName			varchar(50) = null,
	@AssemblyVersion	varchar(50) = null,
	@IsActive			bit,
	@Downloaded			smalldatetime = null,
	@Created			smalldatetime,
	@Deleted			smalldatetime = null,
	@LastRan			smalldatetime = null,
	@LastCompleted		smalldatetime = null
AS


UPDATE [ServiceModules] SET
	[Name] = @Name,
	[Description] = @Description,
	[AssemblyName] = @AssemblyName,
	[AssemblyNamespace] = @AssemblyNamespace,
	[ClassName] = @ClassName,
	[AssemblyVersion] = @AssemblyVersion,
	[IsActive] = @IsActive,
	[Downloaded] = @Downloaded,
	[Created] = @Created,
	[Deleted] = @Deleted,
	[LastRan] = @LastRan,
	[LastCompleted] = @LastCompleted
WHERE [dbo].[ServiceModules].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_SynchTrigger_Get]
	@Id	uniqueidentifier
AS

SELECT
	[TriggerType] AS [TriggerType],
	[TargetId] AS [TargetId],
	[SubtargetId] AS [SubtargetId],
	[SubSubtargetId] AS [SubSubtargetId],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[ProcessingStarted] AS [ProcessingStarted],
	[ProcessingCompleted] AS [ProcessingCompleted]	
FROM [dbo].[SynchTriggers] WITH (NoLock)
WHERE
	[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_SynchTrigger_Insert]
	@Id						uniqueidentifier,
	@GroupId				uniqueidentifier,
	@TriggerType			int,
	@TargetId				uniqueidentifier,
	@SubtargetId			uniqueidentifier = null,
	@SubSubtargetId			uniqueidentifier = null,
	@Order					int,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier,
	@ProcessingStarted		smalldatetime = null,
	@ProcessingCompleted	smalldatetime = null
AS

INSERT INTO [dbo].[SynchTriggers] (
	[Id],
	[GroupId],
	[TriggerType],
	[TargetId],
	[SubtargetId],
	[SubSubtargetId],
	[Order],
	[Created],
	[CreatedBy],
	[ProcessingStarted],
	[ProcessingCompleted])
VALUES (
	@Id,
	@GroupId,
	@TriggerType,
	@TargetId,
	@SubtargetId,
	@SubSubtargetId,
	@Order,
	@Created,
	@CreatedBy,
	@ProcessingStarted,
	@ProcessingCompleted)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_SynchTrigger_Save]
	@Id						uniqueidentifier,
	@GroupId				uniqueidentifier,
	@TriggerType			int,
	@TargetId				uniqueidentifier,
	@SubtargetId			uniqueidentifier = null,
	@SubSubtargetId			uniqueidentifier = null,
	@Order					int,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier,
	@ProcessingStarted		smalldatetime = null,
	@ProcessingCompleted	smalldatetime = null
AS

IF EXISTS (SELECT * FROM [dbo].[Roles] WHERE [Id] = @Id)

	EXEC [dbo].[spr_SynchTrigger_Update] @Id, @GroupId, @TriggerType, @TargetId, @SubtargetId, @SubSubtargetId, @Order, @Created, @CreatedBy, @ProcessingStarted, @ProcessingCompleted

ELSE

	EXEC [dbo].[spr_SynchTrigger_Insert] @Id, @GroupId, @TriggerType, @TargetId, @SubtargetId, @SubSubtargetId, @Order, @Created, @CreatedBy, @ProcessingStarted, @ProcessingCompleted
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_SynchTrigger_Update]
	@Id						uniqueidentifier,
	@GroupId				uniqueidentifier,
	@TriggerType			int,
	@TargetId				uniqueidentifier,
	@SubtargetId			uniqueidentifier = null,
	@SubSubtargetId			uniqueidentifier = null,
	@Order					int,
	@Created				smalldatetime,
	@CreatedBy				uniqueidentifier,
	@ProcessingStarted		smalldatetime = null,
	@ProcessingCompleted	smalldatetime = null
AS

UPDATE [dbo].[SynchTriggers]
SET
	[GroupId] = @GroupId,
	[TriggerType] = @TriggerType,
	[TargetId] = @TargetId,
	[SubtargetId] = @SubtargetId,
	[SubSubtargetId] = @SubSubtargetId,
	[Order] = @Order,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[ProcessingStarted] = @ProcessingStarted,
	[ProcessingCompleted] = @ProcessingCompleted
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Tag_Delete
--*	Deletes the record for a/an Tag.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_Tag_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[Tags]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Tag_Get
--*	Retrieves an instance of Tag based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Tag_Get]
	@Id	uniqueidentifier
AS

SELECT
	[Value] AS [Value],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[Tags] WITH (NoLock)
WHERE
	[dbo].[Tags].[Id] = @Id
		AND
	[dbo].[Tags].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Tag_Insert
--*	Inserts a new record for a new Tag.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Tag_Insert]
	@Id		uniqueidentifier,
	@Value		varchar(50),
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

INSERT INTO [dbo].[Tags] (
	[Id],
	[Value],
	[Created],
	[CreatedBy],
	[DeletedBy])
VALUES (
	@Id,
	@Value,
	@Created,
	@CreatedBy,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Tag_Save
--*	Saves an instance of Tag.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Tag_Save]
	@Id		uniqueidentifier,
	@Value		varchar(50),
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS


IF EXISTS (SELECT * FROM [dbo].[Tags] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Tag_Update] @Id, @Value, @Created, @CreatedBy, @DeletedBy

ELSE

	EXEC [dbo].[spr_Tag_Insert] @Id, @Value, @Created, @CreatedBy, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Tag_Update
--*	Updates an existing record for a/an Tag.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Tag_Update]
	@Id		uniqueidentifier,
	@Value		varchar(50),
	@Created		smalldatetime,
	@CreatedBy		uniqueidentifier,
	@DeletedBy		uniqueidentifier = null
AS

UPDATE [dbo].[Tags] SET
	[Value] = @Value,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[Tags].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerAssetType_Insert
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerAssetType_Insert]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@IncludesChildren	bit
AS

INSERT INTO [dbo].[TriggersAssetTypes] (
	[Id],
	[TriggerId],
	[AssetTypeId],
	[IncludesChildren])
VALUES (
	@Id,
	@TriggerId,
	@AssetTypeId,
	@IncludesChildren)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	Saves an instance of a TriggerAssetType.
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerAssetType_Save]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@IncludesChildren	bit
AS


IF EXISTS (SELECT * FROM [dbo].[TriggersAssetTypes] WHERE [Id] = @Id)

	EXEC [dbo].[spr_TriggerAssetType_Update] @Id, @TriggerId, @AssetTypeId, @IncludesChildren

ELSE

	EXEC [dbo].[spr_TriggerAssetType_Insert] @Id, @TriggerId, @AssetTypeId, @IncludesChildren
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerAssetType_Update
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerAssetType_Update]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@AssetTypeId		uniqueidentifier,
	@IncludesChildren	bit
AS

UPDATE [dbo].[TriggersAssetTypes] SET
	[IncludesChildren] = @IncludesChildren	
WHERE [Id] = @Id
	AND [TriggerId] = @TriggerId
	AND [AssetTypeId] = @AssetTypeId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerAsset_Insert
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerAsset_Insert]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@AssetId			uniqueidentifier,
	@IncludesInstances	bit
AS

INSERT INTO [dbo].[TriggersAssets] (
	[Id],
	[TriggerId],
	[AssetId],
	[IncludesInstances])
VALUES (
	@Id,
	@TriggerId,
	@AssetId,
	@IncludesInstances)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	Saves an instance of a TriggerAsset.
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerAsset_Save]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@AssetId			uniqueidentifier,
	@IncludesInstances	bit
AS


IF EXISTS (SELECT * FROM [dbo].[TriggersAssets] WHERE [Id] = @Id)

	EXEC [dbo].[spr_TriggerAsset_Update] @Id, @TriggerId, @AssetId, @IncludesInstances

ELSE

	EXEC [dbo].[spr_TriggerAsset_Insert] @Id, @TriggerId, @AssetId, @IncludesInstances
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerAsset_Update
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerAsset_Update]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@AssetId			uniqueidentifier,
	@IncludesInstances	bit
AS

UPDATE [dbo].[TriggersAssets] SET
	[IncludesInstances] = @IncludesInstances
WHERE [Id] = @Id
	AND [TriggerId] = @TriggerId
	AND [AssetId] = @AssetId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerProperty_Insert
--*	Inserts a new record for a new Tag.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerProperty_Insert]
	@Id				uniqueidentifier,
	@TriggerId		uniqueidentifier,
	@PropertyId		uniqueidentifier,
	@Value			varchar(MAX) = null,
	@OperatorId		uniqueidentifier = null,
	@Threshold		varchar(MAX) = null
AS

INSERT INTO [dbo].[TriggersProperties] (
	[Id],
	[TriggerId],
	[PropertyId],
	[Value],
	[OperatorId],
	[Threshold])
VALUES (
	@Id,
	@TriggerId,
	@PropertyId,
	@Value,
	@OperatorId,
	@Threshold)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Tag_Save
--*	Saves an instance of a TriggerProperty.
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerProperty_Save]
	@Id				uniqueidentifier,
	@TriggerId		uniqueidentifier,
	@PropertyId		uniqueidentifier,
	@Value			varchar(MAX) = null,
	@OperatorId		uniqueidentifier = null,
	@Threshold		varchar(MAX) = null
AS


IF EXISTS (SELECT * FROM [dbo].[TriggersProperties] WHERE [Id] = @Id)

	EXEC [dbo].[spr_TriggerProperty_Update] @Id, @TriggerId, @PropertyId, @Value, @OperatorId, @Threshold

ELSE

	EXEC [dbo].[spr_TriggerProperty_Insert] @Id, @TriggerId, @PropertyId, @Value, @OperatorId, @Threshold
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerProperty_Update
--*	
--*	
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerProperty_Update]
	@Id				uniqueidentifier,
	@TriggerId		uniqueidentifier,
	@PropertyId		uniqueidentifier,
	@Value			varchar(MAX) = null,
	@OperatorId		uniqueidentifier = null,
	@Threshold		varchar(MAX) = null
AS

UPDATE [dbo].[TriggersProperties] SET
	[Value] = @Value,
	[OperatorId] = @OperatorId,
	[Threshold] = @Threshold	
WHERE [Id] = @Id
	AND [TriggerId] = @TriggerId
	AND [PropertyId] = @PropertyId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerTargetProperty_Insert
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerTargetProperty_Insert]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@NewValue			varchar(max)
AS

INSERT INTO [dbo].[TriggersTargetProperties] (
	[Id],
	[TriggerId],
	[PropertyId],
	[NewValue])
VALUES (
	@Id,
	@TriggerId,
	@PropertyId,
	@NewValue)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM

--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerTargetProperty_Save]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@NewValue			varchar(max)
AS


IF EXISTS (SELECT * FROM [dbo].[TriggersTargetProperties] WHERE [Id] = @Id)

	EXEC [dbo].[spr_TriggerTargetProperty_Update] @Id, @TriggerId, @PropertyId, @NewValue

ELSE

	EXEC [dbo].[spr_TriggerTargetProperty_Insert] @Id, @TriggerId, @PropertyId, @NewValue
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerAsset_Update
--* ==================================================

CREATE PROCEDURE [dbo].[spr_TriggerTargetProperty_Update]
	@Id					uniqueidentifier,
	@TriggerId			uniqueidentifier,
	@PropertyId			uniqueidentifier,
	@NewValue			varchar(max)
AS

UPDATE [dbo].[TriggersTargetProperties] SET
	[NewValue] = @NewValue
WHERE [Id] = @Id
	AND [TriggerId] = @TriggerId
	AND [PropertyId] = @PropertyId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_Trigger_Insert
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Trigger_Insert]
	@Id					uniqueidentifier,
	@Name				varchar(50),
	@Description		varchar(max),	
	@IsActive			bit,
	@TypeId				int,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[Triggers] (
	[Id],
	[Name],
	[Description],
	[IsActive],
	[TypeId],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@Name,
	@Description,
	@IsActive,
	@TypeId,
	@Created,
	@CreatedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM

--* ==================================================

CREATE PROCEDURE [dbo].[spr_Trigger_Save]
	@Id					uniqueidentifier,
	@Name				varchar(50),
	@Description		varchar(max),	
	@IsActive			bit,
	@TypeId				int,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS


IF EXISTS (SELECT * FROM [dbo].[Triggers] WHERE [Id] = @Id)

	EXEC [dbo].[spr_Trigger_Update] @Id, @Name, @Description, @IsActive, @TypeId, @Created, @CreatedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_Trigger_Insert] @Id, @Name, @Description, @IsActive, @TypeId, @Created, @CreatedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_TriggerAsset_Update
--* ==================================================

CREATE PROCEDURE [dbo].[spr_Trigger_Update]
	@Id					uniqueidentifier,
	@Name				varchar(50),
	@Description		varchar(max),	
	@IsActive			bit,
	@TypeId		int,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

UPDATE [dbo].[Triggers] SET
	[Name] = @Name,
	[Description] = @Description,
	[IsActive] = @IsActive,
	[TypeId] = @TypeId,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_URL_Insert]
	@Id					uniqueidentifier,
	@Name				varchar(50) = null,
	@Description		varchar(MAX) = null,
	@URL				varchar(MAX),
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[URLs] (
	[Id],
	[Name],
	[Description],
	[URL],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@Name,
	@Description,
	@URL,
	@Created,
	@CreatedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_URL_Save]
	@Id					uniqueidentifier,
	@Name				varchar(50) = null,
	@Description		varchar(MAX) = null,
	@URL				varchar(MAX),
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[URLs] WHERE [Id] = @Id)

	EXEC [dbo].[spr_URL_Update] @Id, @Name, @Description, @URL, @Created, @CreatedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_URL_Insert] @Id, @Name, @Description, @URL, @Created, @CreatedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_URL_Update]
	@Id					uniqueidentifier,
	@Name				varchar(50) = null,
	@Description		varchar(MAX) = null,
	@URL				varchar(MAX),
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

UPDATE [dbo].[URLs] SET
	[Name] = @Name,
	[Description] = @Description,
	[URL] = @URL,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[URLs].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_ViewDictionaryForReporting_GetByUserId]
	@UserId	uniqueidentifier
AS

SELECT DISTINCT
	V.[Id]
	,IsNull(V.[ReportingLabel], V.[DisplayValue]) AS [DisplayValue]
FROM [Members] M
INNER JOIN [RolesMembers] RM ON RM.[MemberId] = M.[Id]
INNER JOIN [Roles] R ON R.[Id] = RM.[RoleId]
INNER JOIN [RolesViews] RV ON RV.[RoleId] = R.[Id]
INNER JOIN [Views] V ON V.[Id] = RV.[ViewId]
WHERE RM.[MemberId] = @UserId
	AND RM.[Deleted] IS NULL AND RM.[DeletedBy] IS NULL
	AND V.[Deleted] IS NULL AND V.[DeletedBy] IS NULL
	AND R.[Deleted] IS NULL AND R.[DeletedBy] IS NULL
	AND RV.[Deleted] IS NULL AND RV.[DeletedBy] IS NULL
	and V.[IsStandAlone] = 1
ORDER BY [DisplayValue]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewDictionary_GetByUserId
--*	Retrieves an instance of View based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ViewDictionary_GetByUserId]
	@UserId	uniqueidentifier
AS

SELECT DISTINCT
	V.[Id]
	,V.[DisplayValue]
FROM [Members] M
	INNER JOIN [RoleMembers] RM ON RM.[MemberId] = M.[Id]
	INNER JOIN [Roles] R ON R.[Id] = RM.[RoleId]
	INNER JOIN [RolesViews] RV ON RV.[RoleId] = R.[Id]
	INNER JOIN [Views] V ON V.[Id] = RV.[ViewId]
WHERE
	(RM.[MemberId] = @UserId)
	AND (RM.[Deleted] IS NULL)
	AND (V.[Deleted] IS NULL)
	AND (R.[Deleted] IS NULL)
	AND (RV.[Deleted] IS NULL)
	and (V.[IsStandAlone] = 1)
	AND (R.IsActive = 1)
ORDER BY V.[DisplayValue]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewGroups_GetByUserId
--*	Retrieves a list of views available to the current user
--*		grouped by the asset type each view operates on
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ViewGroups_GetByUserId]
	@UserId	uniqueidentifier
AS

SELECT DISTINCT
	AT.[ID],
	AT.[Pluralization] 
FROM [Members] M
INNER JOIN [RolesMembers] RM ON RM.[MemberId] = M.[Id]
INNER JOIN [Roles] R ON R.[Id] = RM.[RoleId]
INNER JOIN [RolesViews] RV ON RV.[RoleId] = R.[Id]
INNER JOIN [Views] V ON V.[Id] = RV.[ViewId]
INNER JOIN [AssetTypes] AT ON AT.[Id] = V.[AssetTypeId] 
WHERE RM.[MemberId] = @UserId
	AND RM.[Deleted] IS NULL AND RM.[DeletedBy] IS NULL
	AND V.[Deleted] IS NULL AND V.[DeletedBy] IS NULL
	AND R.[Deleted] IS NULL AND R.[DeletedBy] IS NULL
	AND RV.[Deleted] IS NULL AND RV.[DeletedBy] IS NULL
	and V.[IsStandAlone] = 1
ORDER BY AT.[Pluralization]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelationList_DeleteByPropertyGroupId
--*	Deletes a ViewPropertyGroupRelationList based on the foreign key of PropertyGroupId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelationList_DeleteByPropertyGroupId]
	@PropertyGroupId	uniqueidentifier,
	@DeletedBy			uniqueidentifier
AS

UPDATE [ViewsPropertyGroups]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [PropertyGroupId] = @PropertyGroupId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelationList_DeleteByViewId
--*	Deletes a ViewPropertyGroupRelationList based on the foreign key of ViewId
--*	Generated by B.A.B.O.O.M.
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelationList_DeleteByViewId]
	@ViewId		uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [ViewsPropertyGroups]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [ViewId] = @ViewId
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelationList_Get
--*	Retrieves a ViewPropertyGroupRelationList.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelationList_Get]
AS

SELECT
	[Id] AS [Id],
	[ViewId] AS [ViewId],
	[PropertyGroupId] AS [PropertyGroupId],
	[Order] AS [Order],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[Deleted] AS [Deleted],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[ViewsPropertyGroups] WITH (NoLock)
	WHERE [dbo].[ViewsPropertyGroups].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelationList_GetByPropertyGroupId
--*	Retrieves a ViewPropertyGroupRelationList based on the foreign key of PropertyGroupId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelationList_GetByPropertyGroupId]
	@PropertyGroupId	uniqueidentifier
AS

SELECT
	[Id] AS [Id],
	[ViewId] AS [ViewId],
	[PropertyGroupId] AS [PropertyGroupId],
	[Order] AS [Order],
	[Created] AS [Created],
	[CreatedBy] AS [CreatedBy],
	[Deleted] AS [Deleted],
	[DeletedBy] AS [DeletedBy]
FROM [dbo].[ViewsPropertyGroups] WITH (NoLock)
WHERE
	[dbo].[ViewsPropertyGroups].[PropertyGroupId] = @PropertyGroupId
		AND
	[dbo].[ViewsPropertyGroups].[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelationList_GetByViewId
--*	Retrieves a ViewPropertyGroupRelationList based on the foreign key of ViewId
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelationList_GetByViewId]
	@ViewId	uniqueidentifier
AS

SELECT
	VPG.[Id] AS [Id],
	VPG.[ViewId] AS [ViewId],
	VPG.[PropertyGroupId] AS [PropertyGroupId],
	VPG.[Order] AS [Order],
	VPG.[Created] AS [Created],
	VPG.[CreatedBy] AS [CreatedBy]
FROM [dbo].[ViewsPropertyGroups] VPG WITH (NoLock)
INNER JOIN [dbo].[PropertyGroups] PG ON PG.[Id] = VPG.[PropertyGroupId]
WHERE
	VPG.[ViewId] = @ViewId
		AND
	VPG.[Deleted] IS NULL
		AND
	PG.[Deleted] IS NULL
		AND
	PG.[DeletedBy] IS NULL
ORDER BY VPG.[Order]
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelation_Delete
--*	Deletes the record for a/an ViewPropertyGroupRelation.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelation_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[ViewsPropertyGroups]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelation_Insert
--*	Inserts a new record for a new ViewPropertyGroupRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelation_Insert]
	@Id					uniqueidentifier,
	@ViewId				uniqueidentifier,
	@PropertyGroupId	uniqueidentifier,
	@Order				int,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime,
	@DeletedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[ViewsPropertyGroups] (
	[Id],
	[ViewId],
	[PropertyGroupId],
	[Order],
	[Created],
	[CreatedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@ViewId,
	@PropertyGroupId,
	@Order,
	@Created,
	@CreatedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelation_Save
--*	Saves an instance of ViewPropertyGroupRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelation_Save]
	@Id					uniqueidentifier,
	@ViewId				uniqueidentifier,
	@PropertyGroupId	uniqueidentifier,
	@Order				int,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

IF EXISTS (SELECT * FROM [dbo].[ViewsPropertyGroups] WHERE [Id] = @Id)

	EXEC [dbo].[spr_ViewPropertyGroupRelation_Update] @Id, @ViewId, @PropertyGroupId, @Order, @Created, @CreatedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_ViewPropertyGroupRelation_Insert] @Id, @ViewId, @PropertyGroupId, @Order, @Created, @CreatedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_ViewPropertyGroupRelation_Update
--*	Updates an existing record for a/an ViewPropertyGroupRelation.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_ViewPropertyGroupRelation_Update]
	@Id					uniqueidentifier,
	@ViewId				uniqueidentifier,
	@PropertyGroupId	uniqueidentifier,
	@Order				int,
	@Created			smalldatetime,
	@CreatedBy			uniqueidentifier,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

UPDATE [dbo].[ViewsPropertyGroups] SET
	[ViewId] = @ViewId,
	[PropertyGroupId] = @PropertyGroupId,
	[Order]	= @Order,
	[Created] = @Created,
	[CreatedBy] = @CreatedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[ViewsPropertyGroups].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_View_Delete
--*	Deletes the record for a/an View.
--*	Generated by BizObjGenerator
--* ==================================================
--* Date			Modifier	Action
--* 29 Jul 2009		JRS			Added @DeletedBy

CREATE PROCEDURE [dbo].[spr_View_Delete]
	@Id			uniqueidentifier,
	@DeletedBy	uniqueidentifier
AS

UPDATE [dbo].[Views]
SET
	[Deleted] = GetDate(),
	[DeletedBy] = @DeletedBy
WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_View_Get
--*	Retrieves an instance of View based on the primary key of Id
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_View_Get]
	@Id	uniqueidentifier
AS

SELECT
	[Name],
	[DisplayValue],
	[Description],
	[IsInstance],
	[AssetTypeId],
	[DriverCaption],
	[NewItemCaption],
	[ConfirmationLabel],
	[IsReadOnly],
	[IsStandAlone],
	[IsCreation],
	[FilterLogic],
	[Created],
	[LastModified],
	[Deleted],
	[CreatedBy],
	[LastModifiedBy],
	[DeletedBy]
FROM [dbo].[Views] WITH (NoLock)
WHERE
	[dbo].[Views].[Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[spr_View_GetDefaultProperties]
	@ViewId	uniqueidentifier
AS

select PGP.PropertyId, PGP.DefaultValue 
from [Views] V WITH (NoLock)
	INNER JOIN [ViewsPropertyGroups] VPG WITH (NoLock) ON VPG.[ViewId] = V.[Id]
	INNER JOIN [PropertyGroups] PG WITH (NoLock) ON PG.[Id] = VPG.[PropertyGroupId]
	INNER JOIN [PropertyGroupsProperties] PGP WITH (NoLock) ON PGP.[PropertyGroupId] = PG.[Id]
	INNER JOIN [Properties] P WITH (NoLock) ON P.[Id] = PGP.[PropertyId]
WHERE
	V.[Id] = @ViewId
	AND VPG.Deleted IS NULL
	AND PGP.Deleted IS NULL
	AND P.Deleted IS NULL
	AND PGP.DefaultValue IS NOT NULL
	AND PGP.DefaultValue != ''
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


--* ==================================================
--*	
--*	spr_View_GetPropertyIds
--*	
--*	Gets a distinct list of all Property Ids that are in a View
--* ==================================================

CREATE PROCEDURE [dbo].[spr_View_GetPropertyIds]
	@ViewId	uniqueidentifier
AS

SELECT DISTINCT
	P.[Id]
FROM [Views] V
	INNER JOIN [ViewsPropertyGroups] VPG ON VPG.[ViewId] = V.[Id]
	INNER JOIN [PropertyGroups] PG ON PG.[Id] = VPG.[PropertyGroupId]
	INNER JOIN [PropertyGroupsProperties] PGP ON PGP.[PropertyGroupId] = PG.[Id]
	INNER JOIN [Properties] P ON P.[Id] = PGP.[PropertyId]
WHERE
	V.[Id] = @ViewId AND
	VPG.[Deleted] IS NULL AND
	PG.[Deleted] IS NULL AND
	PGP.[Deleted] IS NULL AND
	P.[Deleted] IS NULL
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================

--* ==================================================

CREATE PROCEDURE [dbo].[spr_View_Has_Permission]
	@MemberId uniqueidentifier,
	@PermissionId int,
	@ViewId uniqueidentifier
AS

SELECT COUNT(R.[Id])
	--R.[Id]
	--, R.[Name] 
	--, AT.[Id] AS [AssetTypeId]
	--, RAT.[IsInstance] AS [DefIns]
FROM [Roles] R
INNER JOIN [RolesViews] RV ON RV.[RoleId] = R.[Id]
INNER JOIN [Views] V ON V.[Id] = RV.[ViewId]
INNER JOIN [RoleMembers] RM ON RM.[RoleId] = R.[Id]
INNER JOIN [RoleAssetTypes] RAT ON RAT.RoleId = R.[Id]
INNER JOIN [AssetTypes] AT ON AT.Id = RAT.AssetTypeId 
LEFT JOIN [RolePermissions] RP ON RP.RoleAssetTypesId = RAT.Id
WHERE
	(RM.[MemberId] = @MemberId)
	AND (RAT.[IsInherited] = 0)
	AND (RP.[PermissionId] = @PermissionId)
	AND (RAT.[IsInstance] = V.[IsInstance])
	AND (RV.[ViewId] = @ViewId)
	AND (AT.[Id] = V.[AssetTypeId])
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_View_Insert
--*	Inserts a new record for a new View.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_View_Insert]
	@Id					uniqueidentifier,
	@Name				varchar(100),
	@DisplayValue		varchar(100),
	@Description		varchar(MAX),
	@IsInstance			bit,
	@AssetTypeId		uniqueidentifier = null,
	@DriverCaption		varchar(50) = null,
	@NewItemCaption		varchar(50) = null,
	@ConfirmationLabel	varchar(50) = null,
	@IsReadOnly			bit,
	@IsStandAlone		bit,
	@IsCreation			bit,
	@FilterLogic		varchar(max) = null,
	@Created			smalldatetime,
	@LastModified		smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@LastModifiedBy		uniqueidentifier = null,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

INSERT INTO [dbo].[Views] (
	[Id],
	[Name],
	[DisplayValue],
	[Description],
	[IsInstance],
	[AssetTypeId],
	[DriverCaption],
	[NewItemCaption],
	[ConfirmationLabel],
	[IsReadOnly],
	[IsStandAlone],
	[IsCreation],
	[FilterLogic],
	[Created],
	[LastModified],
	[CreatedBy],
	[LastModifiedBy],
	[Deleted],
	[DeletedBy])
VALUES (
	@Id,
	@Name,
	@DisplayValue,
	@Description,
	@IsInstance,
	@AssetTypeId,
	@DriverCaption,
	@NewItemCaption,
	@ConfirmationLabel,
	@IsReadOnly,
	@IsStandAlone,
	@IsCreation,
	@FilterLogic,
	@Created,
	@LastModified,
	@CreatedBy,
	@LastModifiedBy,
	@Deleted,
	@DeletedBy)
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO


CREATE PROCEDURE [dbo].[spr_View_Kill]
	@Id	uniqueidentifier
AS

DELETE FROM [ViewFilters] WHERE [ViewId] = @Id
DELETE FROM [RolesViews] WHERE [ViewId] = @Id 
DELETE FROM [ViewsPropertyGroups] WHERE [ViewId] = @Id 
DELETE FROM [Views] WHERE [Id] = @Id
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_View_Save
--*	Saves an instance of View.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_View_Save]
	@Id					uniqueidentifier,
	@Name				varchar(100),
	@DisplayValue		varchar(100),
	@Description		varchar(MAX),
	@IsInstance			bit,
	@AssetTypeId		uniqueidentifier = null,
	@DriverCaption		varchar(50) = null,
	@NewItemCaption		varchar(50) = null,
	@ConfirmationLabel	varchar(50) = null,
	@IsReadOnly			bit,
	@IsStandAlone		bit,
	@IsCreation			bit,
	@FilterLogic		varchar(max) = null,
	@Created			smalldatetime,
	@LastModified		smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@LastModifiedBy		uniqueidentifier = null,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

DELETE FROM [ViewClients] WHERE [ViewId] = @Id

IF EXISTS (SELECT * FROM [dbo].[Views] WHERE [Id] = @Id)

	EXEC [dbo].[spr_View_Update] @Id, @Name, @DisplayValue, @Description, @IsInstance, @AssetTypeId, @DriverCaption, @NewItemCaption, @ConfirmationLabel, @IsReadOnly, @IsStandAlone, @IsCreation, @FilterLogic, @Created, @LastModified, @CreatedBy, @LastModifiedBy, @Deleted, @DeletedBy

ELSE

	EXEC [dbo].[spr_View_Insert] @Id, @Name, @DisplayValue, @Description, @IsInstance, @AssetTypeId, @DriverCaption, @NewItemCaption, @ConfirmationLabel, @IsReadOnly, @IsStandAlone, @IsCreation, @FilterLogic, @Created, @LastModified, @CreatedBy, @LastModifiedBy, @Deleted, @DeletedBy
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

--* ==================================================
--*	Monday, July 20, 2009 12:18 PM
--*	spr_View_Update
--*	Updates an existing record for a/an View.
--*	Generated by BizObjGenerator
--* ==================================================

CREATE PROCEDURE [dbo].[spr_View_Update]
	@Id					uniqueidentifier,
	@Name				varchar(100),
	@DisplayValue		varchar(100),
	@Description		varchar(MAX),
	@IsInstance			bit,
	@AssetTypeId		uniqueidentifier = null,
	@DriverCaption		varchar(50) = null,
	@NewItemCaption		varchar(50) = null,
	@ConfirmationLabel	varchar(50) = null,
	@IsReadOnly			bit,
	@IsStandAlone		bit,
	@IsCreation			bit,
	@FilterLogic		varchar(max) = null,
	@Created			smalldatetime,
	@LastModified		smalldatetime = null,
	@CreatedBy			uniqueidentifier,
	@LastModifiedBy		uniqueidentifier = null,
	@Deleted			smalldatetime = null,
	@DeletedBy			uniqueidentifier = null
AS

-- Delete any cached ViewClient objects
DELETE FROM [ViewClients] WHERE [ViewId] = @Id

UPDATE [dbo].[Views] SET
	[Name] = @Name,
	[DisplayValue] = @DisplayValue,
	[Description] = @Description,
	[IsInstance] = @IsInstance,
	[AssetTypeId] = @AssetTypeId,
	[DriverCaption] = @DriverCaption,
	[NewItemCaption] = @NewItemCaption,
	[ConfirmationLabel] = @ConfirmationLabel,
	[IsReadOnly] = @IsReadOnly,
	[IsStandAlone] = @IsStandAlone,
	[IsCreation] = @IsCreation,
	[FilterLogic] = @FilterLogic,
	[Created] = @Created,
	[LastModified] = @LastModified,
	[CreatedBy] = @CreatedBy,
	[LastModifiedBy] = @LastModifiedBy,
	[Deleted] = @Deleted,
	[DeletedBy] = @DeletedBy
WHERE [dbo].[Views].[Id] = @Id
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
