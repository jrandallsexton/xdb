-- XDB baseline (generated; do not edit by hand).
-- Definitions copied verbatim from sys.sql_modules, each with the ANSI_NULLS / QUOTED_IDENTIFIER settings it was created with.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- =============================================

CREATE FUNCTION [dbo].[AssetTypeChildren] 
(	
	@Id	uniqueidentifier
)
RETURNS @Tbl_IDs 
	TABLE (
		Id	varchar(36)
	) As 
BEGIN
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

		Insert Into @Tbl_IDs (Id)	
		SELECT [ChildAssetTypeId] FROM AssetType_CTE
		WHERE [ParentAssetTypeId] = @Id
		ORDER BY [Child]

    Return
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[AssetType_GetName] 
(
	@AssetTypeId varchar(36)
)
RETURNS varchar(50)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(50)	

	If ((@AssetTypeId IS NULL) OR (@AssetTypeId = ''))
		BEGIN
			RETURN NULL
		END
	ELSE
		SELECT @RETURN_VAL = AT.[Name]
		FROM [AssetTypes] AT WITH (NoLock)
		WHERE AT.[Id] = @AssetTypeId

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[AssetType_GetReportingLabel] 
(
	@AssetTypeId varchar(36),
	@IsInstance bit
)
RETURNS varchar(50)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(50)	

	If ((@AssetTypeId IS NULL) OR (@AssetTypeId = '') OR (@IsInstance IS NULL))
		BEGIN
			RETURN NULL
		END
	ELSE
		IF @IsInstance = 0
			BEGIN
				SELECT @RETURN_VAL = AT.[DefinitionLabel]
				FROM [AssetTypes] AT WITH (NoLock)
				WHERE AT.[Id] = @AssetTypeId
			END
		ELSE
			BEGIN
				SELECT @RETURN_VAL = AT.[InstanceLabel]
				FROM [AssetTypes] AT WITH (NoLock)
				WHERE AT.[Id] = @AssetTypeId
			END

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE function [dbo].[F_AGE_YYYY_MM_DD]
	(
	@START_DATE		datetime,
	@END_DATE		datetime
	)
returns  varchar(10) 
as
/*
Function: F_AGE_YYYY_MM_DD

This function calculates age in years, months and days
from @START_DATE through @END_DATE and
returns the age in format YYYY MM DD.

Years is the number of full years between @START_DATE and @END_DATE.

Months is the number of full months since the last full year anniversary.

Days is the number of days since the last full month anniversary.
http://www.sqlteam.com/forums/topic.asp?TOPIC_ID=64762
*/
begin

declare @AGE varchar(10)

declare @AGE_IN_YEARS	int
declare @AGE_IN_MONTHS	int
declare @AGE_IN_DAYS	int

-- Return null if @START_DATE > @END_DATE
if @START_DATE > @END_DATE begin return @AGE end

select
	@AGE_IN_YEARS = AGE_IN_YEARS,
	@AGE_IN_MONTHS = AGE_IN_MONTHS,
	@AGE_IN_DAYS =
		datediff(dd,
		dateadd(mm,AGE_IN_MONTHS,
		dateadd(yy,AGE_IN_YEARS,START_DATE))
		,END_DATE)
from
(
select
	AGE_IN_MONTHS =
	case
	when AnniversaryThisMonth <= END_DATE
	then datediff(mm,dateadd(yy,AGE_IN_YEARS,START_DATE),END_DATE)
	else datediff(mm,dateadd(yy,AGE_IN_YEARS,START_DATE),END_DATE)-1
	end,
	*
from
(
select
	AGE_IN_YEARS =
	case
	when AnniversaryThisYear <= END_DATE
	then datediff(yy,START_DATE,END_DATE)
	else datediff(yy,START_DATE,END_DATE)-1
	end,
	*
from
(
select
	AnniversaryThisYear =
	dateadd(yy,datediff(yy,START_DATE,END_DATE),START_DATE),
	AnniversaryThisMonth =
	dateadd(mm,datediff(mm,START_DATE,END_DATE),START_DATE),
	*
from
(
select	START_DATE	= dateadd(dd,datediff(dd,0,@START_DATE),0),
	END_DATE	= dateadd(dd,datediff(dd,0,@END_DATE),0)
) aaaa
) aaa
) aa
) a

select @AGE =
	right('0000'+convert(varchar(4),@AGE_IN_YEARS),4) + ' ' +
	right('00'+convert(varchar(4),@AGE_IN_MONTHS),2) + ' ' +
	right('00'+convert(varchar(4),@AGE_IN_DAYS),2)

return @AGE

end
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetAssetParentDescByInstanceId] 
(
	@InstanceId varchar(36)
)
RETURNS varchar(MAX)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(max)	
	DECLARE @TEMP_ID varchar(36)
	
	If (@InstanceId IS NULL)
		BEGIN
			SET @RETURN_VAL = null
		END
	ELSE
	IF (@InstanceId = '')
		BEGIN
			SET @RETURN_VAL = null
		END
	ELSE
		BEGIN			
			SELECT
				@TEMP_ID = A.[InstanceOfId]
			FROM [Assets] A WITH (NoLock)
				WHERE A.[Id] = @InstanceId
			SELECT
				@RETURN_VAL = [Description]
			FROM [Assets] WITH (NoLock)
				WHERE [Id] = @TEMP_ID
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetAssetParentIdByChildId] 
(
	@ChildAssetId varchar(36)
)
RETURNS uniqueidentifier
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL uniqueidentifier	

	If (@ChildAssetId IS NULL)
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE
	IF (@ChildAssetId = '')
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE
		BEGIN			
			SELECT
				@RETURN_VAL = A.[Id]
			FROM [AssetRelations] AR WITH (NoLock)
				INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = AR.[FromAssetId]
			WHERE AR.[ToAssetId] = @ChildAssetId
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetAssetParentNameByChildId] 
(
	@ChildAssetId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(MAX)	

	If (@ChildAssetId IS NULL)
		BEGIN
			SET @RETURN_VAL = ''
		END
	ELSE
	IF (@ChildAssetId = '')
		BEGIN
			SET @RETURN_VAL = ''
		END
	ELSE
		BEGIN			
			SELECT
				@RETURN_VAL = IsNull(A.[DisplayValue], A.[Name])
			FROM [AssetRelations] AR WITH (NoLock)
				INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = AR.[FromAssetId]
			WHERE AR.[ToAssetId] = @ChildAssetId
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[GetAssetType] 
(
	@AssetId varchar(36)
)
RETURNS varchar(100)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(100)	

	If ((@AssetId IS NULL) OR (@AssetId = ''))
		BEGIN
			RETURN NULL
		END
	ELSE
		BEGIN
			
			SELECT @RETURN_VAL = AT.[Name]
			FROM [AssetTypes] AT WITH (NoLock)
			INNER JOIN [Assets] A WITH (NoLock) ON A.[AssetTypeId] = AT.[Id] AND A.[Id] = @AssetId
			WHERE AT.[Id] = A.[AssetTypeId]
			
		END

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetAssetValueById] 
(
	@AssetId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(MAX)	

	If ((@AssetId IS NULL) OR (@AssetId = ''))
		BEGIN
			RETURN NULL
		END
	ELSE	
		BEGIN			
			SELECT
				@RETURN_VAL = IsNull([DisplayValue], [Name])
			FROM [Assets] WITH (NoLock)
			WHERE [Id] = @AssetId
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[GetBookmarkCaption] 
(
	@AssetId varchar(36)
)
RETURNS varchar(MAX)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(36)	
	DECLARE @TEMP_ID varchar(36)
	
	If ((@AssetId IS NULL) OR (@AssetId = ''))
		RETURN NULL
	ELSE
		BEGIN			
			SELECT
				@TEMP_ID = A.[InstanceOfId]
			FROM [Assets] A WITH (NoLock)
				WHERE A.[Id] = @AssetId
			IF (@TEMP_ID IS NULL)
				RETURN NULL
			ELSE
				SELECT
					@RETURN_VAL = [Description]
				FROM [Assets] WITH (NoLock)
				WHERE [Id] = @TEMP_ID
		END

	-- Return the result of the function
	RETURN ' (' + @RETURN_VAL + ')'

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetCurrencyValueById] 
(
	@Id varchar(36)
)
RETURNS nvarchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL nvarchar(MAX)	

	If (@Id IS NULL)
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE	
	IF (@Id = '')
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE
		BEGIN			
			SELECT
				@RETURN_VAL = cast(CS.[Value] as nvarchar) + ' ' + cast(CV.[Value] as nvarchar)
			FROM [CurrencyValues] CV WITH (NoLock)
			INNER JOIN [CurrencySymbols] CS WITH (NoLock)
				ON CS.[Id] = CV.[CurrencySymbolId]
			WHERE CV.[Id] = @Id
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[GetDocumentValueById] 
(
	@Id varchar(36)
)
RETURNS nvarchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL nvarchar(MAX)	

	If ((@Id IS NULL) OR (@Id = ''))
		BEGIN
			RETURN NULL
		END
	ELSE	
		BEGIN			
			SELECT
				@RETURN_VAL = IsNull(D.[Title], D.[Name])
			FROM [Documents] D WITH (NoLock)
			WHERE [Id] = @Id
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetLastAssetHit] 
(
	@AssetId varchar(36),
	@MemberId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL smalldatetime	

	If (@AssetId IS NULL)
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE	
	IF (@AssetId = '')
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE
		BEGIN
			SELECT @RETURN_VAL = MAX([Created])
			FROM [AssetHits] WITH (NoLock)
			WHERE [AssetId] = @AssetId AND [MemberId] = @MemberId
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetMemberFriendlyValueById] 
(
	@MemberId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @MEM_VAL varchar(MAX)	

	If (@MemberId IS NULL)
		BEGIN
			SET @MEM_VAL = null
		END
	ELSE
	IF (@MemberId = '')
		BEGIN
			SET @MEM_VAL = null
		END
	ELSE
		BEGIN			
			SELECT @MEM_VAL = 
				[Members].[fname] + ' ' + [Members].[lname] + ' (' + [Members].[userId] + ')' FROM [Members] WITH (NoLock)
				WHERE [Id] = @MemberId
		END

	-- Return the result of the function
	RETURN @MEM_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetMemberValueById] 
(
	@MemberId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @MEM_VAL varchar(MAX)	

	If (@MemberId IS NULL)
		BEGIN
			SET @MEM_VAL = null
		END
	ELSE
	IF (@MemberId = '')
		BEGIN
			SET @MEM_VAL = null
		END
	ELSE
		BEGIN			
			SELECT @MEM_VAL = 
				[Members].[lname] + ', ' + [Members].[fname] + ' (' + [Members].[userId] + ')' FROM [Members] WITH (NoLock)
				WHERE [Id] = @MemberId
		END

	-- Return the result of the function
	RETURN @MEM_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetPickListValueById] 
(
	@PickListValueId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	DECLARE @PLV_Val varchar(MAX)	

	If ((@PickListValueId IS NULL) OR (@PickListValueId = ''))
		BEGIN
			RETURN NULL
		END
	ELSE
		BEGIN

			SELECT @PLV_Val = IsNull([DisplayValue], [Value])
			FROM PickListValues WITH (NoLock)
			WHERE [Id] = @PickListValueId		
			
		END

	RETURN @PLV_Val

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[GetPropertyValue] 
(
	@AssetId varchar(36),
	@PropertyId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(MAX)	

	IF ((@AssetId IS NULL) OR (@AssetId = ''))
		BEGIN
			RETURN NULL
		END
	ELSE
		BEGIN			
			SELECT @RETURN_VAL = [Value]
			FROM [PropertyValues] PV WITH (NoLock)
			WHERE
				PV.[AssetId] = @AssetId AND
				PV.[PropertyId] = @PropertyId AND
				PV.[Deleted] IS NULL AND
				PV.[Approved] IS NOT NULL
		END

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[GetReportHitCount] 
(
	@ReportId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL int	

	If ((@ReportId IS NULL) OR (@ReportId = ''))
		RETURN NULL
	ELSE	
		BEGIN
			SELECT @RETURN_VAL = COUNT(*)
			FROM [ReportLogs] WITH (NoLock)
			WHERE [ReportId] = @ReportId
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetUrlValueById] 
(
	@UrlId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(MAX)	

	If (@UrlId IS NULL)
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE	
	IF (@UrlId = '')
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE
		BEGIN			
			SELECT @RETURN_VAL = [URL] FROM [URLs] WITH (NoLock) WHERE [Id] = @UrlId
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[GetValueById] 
(
	@Id	uniqueidentifier
)
RETURNS varchar(max)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(MAX)	

	SELECT @RETURN_VAL = IsNull([DisplayValue], [Name])
	FROM [Assets] A WHERE A.[Id] = @Id

	IF @RETURN_VAL IS NULL
		BEGIN
			SELECT @RETURN_VAL = IsNull([DisplayValue], [Value])
			FROM [PickListValues] PLV WHERE PLV.[Id] = @Id
		END
	ELSE
		RETURN @RETURN_VAL	
	
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================

-- =============================================
CREATE FUNCTION [dbo].[MemberIsAdmin] 
(
	@MemberId uniqueidentifier
)
RETURNS bit
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL bit	
	DECLARE @Count int

	If (@MemberId IS NULL)
		BEGIN
			SET @RETURN_VAL = 0
		END
	ELSE	
		BEGIN
			SELECT @Count = COUNT(*)
			FROM [RoleMembers] RM WITH (NoLock)
			WHERE
				(RM.[RoleId] = '68F8BBAB-C026-4ED4-A875-164C48E8874D')
				AND (RM.[MemberId] = @MemberId)
				AND (RM.[Deleted] IS NULL)
			
			IF @Count > 0
				SET @RETURN_VAL = 1
			ELSE
				SET @RETURN_VAL = 0
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[PropertyValue] 
(
	@AssetId	uniqueidentifier,
	@PropertyId uniqueidentifier
)
RETURNS varchar(max)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(MAX)	

	SELECT @RETURN_VAL = [Value]
	FROM [PropertyValues] PV WITH (NoLock)
	WHERE
		PV.[AssetId] = @AssetId AND
		PV.[PropertyId] = @PropertyId AND
		PV.[Deleted] IS NULL AND
		PV.[Approved] IS NOT NULL

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- http://www.sqlbook.com/SQL/Create-comma-delimited-list-27.aspx
CREATE FUNCTION [dbo].[PropertyValueMulti_Asset] 
(
	@AssetId	uniqueidentifier,	--Id of the asset whose property value we want
	@PropertyId uniqueidentifier	--Id of the property which is of DataType=Asset
)
RETURNS varchar(MAX)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(MAX)	
	SET @RETURN_VAL = NULL
	
	DECLARE @tempValue varchar(MAX)
	
	SELECT @RETURN_VAL = COALESCE(@RETURN_VAL + ', ', '') + A.[Name]
	FROM PropertyValues PV
	INNER JOIN [Assets] A ON A.[Id] = CAST(PV.[Value] AS varchar(36))
	where
		PV.AssetId = @AssetId
		and PV.PropertyId = @PropertyId
		and PV.[Deleted] IS NULL
		and PV.[Value] IS NOT NULL
		and PV.[Approved] IS NOT NULL
		and PV.[Value] <> ''
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[PropertyValue_Asset] 
(
	@AssetId	uniqueidentifier,	--Id of the asset whose property value we want
	@PropertyId uniqueidentifier	--Id of the property which is of DataType=Asset
)
RETURNS varchar(255)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(255)	
	SET @RETURN_VAL = NULL
	
	DECLARE @tempValue varchar(255)
	
	SELECT @tempValue = PV.[Value]
	FROM [PropertyValues] PV WITH (NoLock)			
	WHERE
		PV.[PropertyId] = @PropertyId AND
		PV.[AssetId] = @AssetId AND
		PV.[Deleted] IS NULL AND
		PV.[Approved] IS NOT NULL
	
	IF ((@tempValue IS NOT NULL) AND (LEN(@tempValue) = 36))
		BEGIN
			DECLARE @pvValueId uniqueidentifier
			SET @pvValueId = CAST(@tempValue AS uniqueidentifier)
			SELECT
				@RETURN_VAL = IsNull([DisplayValue], [Name])
			FROM [Assets] WITH (NoLock)
			WHERE [Id] = @pvValueId				
		END

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[PropertyValue_Currency] 
(
	@AssetId	uniqueidentifier,	--Id of the asset whose property value we want
	@PropertyId uniqueidentifier	--Id of the property which is of DataType=Asset
)
RETURNS varchar(50)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(50)	
	SET @RETURN_VAL = NULL
	
	DECLARE @tempValue varchar(36)
	
	SELECT @tempValue = PV.[Value]
	FROM [PropertyValues] PV WITH (NoLock)			
	WHERE
		PV.[PropertyId] = @PropertyId AND
		PV.[AssetId] = @AssetId AND
		PV.[Deleted] IS NULL AND
		PV.[Approved] IS NOT NULL
	
	IF ((@tempValue IS NOT NULL) AND (LEN(@tempValue) = 36))
	
		BEGIN
		
			DECLARE @pvValueId uniqueidentifier
			SET @pvValueId = CAST(@tempValue AS uniqueidentifier)
			
			SELECT
				@RETURN_VAL = cast(CS.[Value] as nvarchar) + ' ' + cast(CV.[Value] as nvarchar)
			FROM [CurrencyValues] CV WITH (NoLock)
			INNER JOIN [CurrencySymbols] CS WITH (NoLock)
				ON CS.[Id] = CV.[CurrencySymbolId]
			WHERE CV.[Id] = @pvValueId				
		END

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[PropertyValue_Member] 
(
	@AssetId	uniqueidentifier,	--Id of the asset whose property value we want
	@PropertyId uniqueidentifier	--Id of the property which is of DataType=Asset and AssetType=User
)
RETURNS varchar(255)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(255)	
	SET @RETURN_VAL = NULL
	
	DECLARE @tempValue varchar(255)
	
	SELECT @tempValue = PV.[Value]
	FROM [PropertyValues] PV WITH (NoLock)			
	WHERE
		PV.[PropertyId] = @PropertyId AND
		PV.[AssetId] = @AssetId AND
		PV.[Deleted] IS NULL AND
		PV.[Approved] IS NOT NULL
	
	IF ((@tempValue IS NOT NULL) AND (LEN(@tempValue) = 36))
		BEGIN
			DECLARE @pvValueId uniqueidentifier
			SET @pvValueId = CAST(@tempValue AS uniqueidentifier)
			SELECT @RETURN_VAL = 
				[Members].[lname] + ', ' + [Members].[fname] + ' (' + [Members].[userId] + ')'
			FROM [Members] WITH (NoLock)
			WHERE [Id] = @pvValueId				
		END

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[PropertyValue_PickList] 
(
	@AssetId	uniqueidentifier,	--Id of the asset whose property value we want
	@PropertyId uniqueidentifier	--Id of the property which is of DataType=PickList
)
RETURNS varchar(255)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(255)	
	SET @RETURN_VAL = NULL
	
	DECLARE @tempValue varchar(50)
	
	SELECT @tempValue = PV.[Value]
	FROM [PropertyValues] PV WITH (NoLock)			
	WHERE
		PV.[PropertyId] = @PropertyId AND
		PV.[AssetId] = @AssetId AND
		PV.[Deleted] IS NULL AND
		PV.[Approved] IS NOT NULL
	
	IF ((@tempValue IS NOT NULL) AND (LEN(@tempValue) = 36))
		BEGIN
			DECLARE @pvValueId uniqueidentifier
			SET @pvValueId = CAST(@tempValue AS uniqueidentifier)
			SELECT
				@RETURN_VAL = IsNull([DisplayValue], [Value])
			FROM [PickListValues] WITH (NoLock)
			WHERE [Id] = @pvValueId				
		END

	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Creating a User Defined Function in SQL for Comma-Delimited Searches
-- By Corey Aldebol
-- http://www.4guysfromrolla.com/webtech/031004-1.shtml

CREATE FUNCTION [dbo].[Split]
(
	@List nvarchar(max),
	@SplitOn nvarchar(5)
)  
	RETURNS @RtnValue table 
	(		
		Value varchar(36)
	) 
AS  
	BEGIN

	While (Charindex(@SplitOn,@List)>0)
		Begin
			Insert Into @RtnValue (value)
			Select 
				Value = ltrim(rtrim(Substring(@List,1,Charindex(@SplitOn,@List)-1)))
			    
			    
			Set @List = Substring(@List,Charindex(@SplitOn,@List)+len(@SplitOn),len(@List))
		End
    
	Insert Into @RtnValue (Value)
	
    Select Value = cast(ltrim(rtrim(@List)) as varchar(36))

    Return
    
	END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Creating a User Defined Function in SQL for Comma-Delimited Searches
-- By Corey Aldebol
-- http://www.4guysfromrolla.com/webtech/031004-1.shtml

CREATE FUNCTION [dbo].[SplitAsString]
(
	@List nvarchar(max),
	@SplitOn nvarchar(5)
)  
	RETURNS @RtnValue table 
	(		
		Value varchar(max)
	) 
AS  
	BEGIN

	While (Charindex(@SplitOn,@List)>0)
		Begin
			Insert Into @RtnValue (value)
			Select 
				Value = ltrim(rtrim(Substring(@List,1,Charindex(@SplitOn,@List)-1)))
			    
			    
			Set @List = Substring(@List,Charindex(@SplitOn,@List)+len(@SplitOn),len(@List))
		End
    
	Insert Into @RtnValue (Value)
	
    Select Value = cast(ltrim(rtrim(@List)) as varchar(36))

    Return
    
	END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetNumeric]
(@strAlphaNumeric VARCHAR(256))
RETURNS VARCHAR(256)
AS
BEGIN

	DECLARE @intAlpha INT
	SET @intAlpha = PATINDEX('%[^0-9.]%', @strAlphaNumeric)
		BEGIN
			WHILE @intAlpha > 0
				BEGIN
					SET @strAlphaNumeric = STUFF(@strAlphaNumeric, @intAlpha, 1, '' )
					SET @intAlpha = PATINDEX('%[^0-9.]%', @strAlphaNumeric )
				END
		END
	RETURN ISNULL(@strAlphaNumeric,0)
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetAssetParentNameByInstanceId] 
(
	@InstanceId varchar(36)
)
RETURNS varchar(MAX)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(36)	
	DECLARE @TEMP_ID varchar(36)
	
	If (@InstanceId IS NULL)
		BEGIN
			SET @RETURN_VAL = ''
		END
	ELSE
	IF (@InstanceId = '')
		BEGIN
			SET @RETURN_VAL = ''
		END
	ELSE
		BEGIN			
			SELECT @TEMP_ID = A.[InstanceOfId] FROM [Assets] A WITH (NoLock)
				WHERE A.[Id] = @InstanceId
			SELECT @RETURN_VAL = dbo.[GetAssetValueById] (@TEMP_ID)
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetPreviousPropertyValueLookup] 
(
	@AssetId varchar(36),
	@PropertyId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(MAX)	

	IF ((@AssetId IS NULL) OR (@AssetId = '') OR (@PropertyId IS NULL) OR (@PropertyId = ''))
		BEGIN
			RETURN NULL
		END
	ELSE
		BEGIN
						
			SELECT TOP 1
				@RETURN_VAL = [Value]
			FROM [PropertyValues] PV WITH (NoLock)
			WHERE
				PV.[AssetId] = @AssetId AND
				PV.[PropertyId] = @PropertyId AND
				PV.[Deleted] IS NOT NULL
			ORDER BY PV.[Created] DESC
			
			IF ((@RETURN_VAL IS NOT NULL) AND (@RETURN_VAL <> '') AND (@RETURN_VAL <> ' '))
				BEGIN
					DECLARE @DataType int
					SELECT @DataType = [DataTypeId] FROM [Properties] WITH (NoLock) WHERE [Id] = @PropertyId
					SET @RETURN_VAL =
					CASE @DataType
						WHEN 2 THEN dbo.[GetDocumentValueById] (@RETURN_VAL)
						WHEN 7 THEN dbo.[GetUrlValueById](@RETURN_VAL)
						WHEN 9 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 8 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 14 THEN dbo.[GetCurrencyValueById] (@RETURN_VAL)
						WHEN 23 THEN dbo.[GetPickListValueById] (@RETURN_VAL)
						ELSE @RETURN_VAL
					END
				END
			ELSE
				RETURN NULL
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetPropertyValueLookup] 
(
	@AssetId varchar(36),
	@PropertyId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(MAX)	

	IF ((@AssetId IS NULL) OR (@AssetId = '') OR (@PropertyId IS NULL) OR (@PropertyId = ''))
		BEGIN
			RETURN NULL
		END
	ELSE
		BEGIN
						
			SELECT
				@RETURN_VAL = [Value]
			FROM [PropertyValues] PV WITH (NoLock)
			WHERE
				PV.[AssetId] = @AssetId AND
				PV.[PropertyId] = @PropertyId AND
				PV.[Deleted] IS NULL AND
				PV.[Approved] IS NOT NULL
			
			IF ((@RETURN_VAL IS NOT NULL) AND (@RETURN_VAL <> '') AND (@RETURN_VAL <> ' '))
				BEGIN
					DECLARE @DataType int
					SELECT @DataType = [DataTypeId] FROM [Properties] WITH (NoLock) WHERE [Id] = @PropertyId
					SET @RETURN_VAL =
					CASE @DataType
						WHEN 2 THEN dbo.[GetDocumentValueById] (@RETURN_VAL)
						WHEN 7 THEN dbo.[GetUrlValueById](@RETURN_VAL)
						WHEN 9 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 8 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 11 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 12 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 13 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 14 THEN dbo.[GetCurrencyValueById] (@RETURN_VAL)
						WHEN 23 THEN dbo.[GetPickListValueById] (@RETURN_VAL)
						ELSE @RETURN_VAL
					END
				END
			ELSE
				RETURN NULL
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[GetPropertyValueLookupByPropValId] 
(
	@PropValId varchar(36),
	@PropertyId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(MAX)	

	IF ((@PropValId IS NULL) OR (@PropValId = '') OR (@PropertyId IS NULL) OR (@PropertyId = ''))
		BEGIN
			RETURN NULL
		END
	ELSE
		BEGIN
						
			SELECT
				@RETURN_VAL = [Value]
			FROM [PropertyValues] PV WITH (NoLock)
			WHERE PV.[Id] = @PropValId
			
			IF ((@RETURN_VAL IS NOT NULL) AND (@RETURN_VAL <> '') AND (@RETURN_VAL <> ' '))
				BEGIN
					DECLARE @DataType int
					SELECT @DataType = [DataTypeId] FROM [Properties] WITH (NoLock) WHERE [Id] = @PropertyId
					SET @RETURN_VAL =
					CASE @DataType
						WHEN 2 THEN dbo.[GetDocumentValueById] (@RETURN_VAL)
						WHEN 7 THEN dbo.[GetUrlValueById](@RETURN_VAL)
						WHEN 9 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 8 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 11 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 12 THEN dbo.[GetAssetValueById](@RETURN_VAL)
						WHEN 14 THEN dbo.[GetCurrencyValueById] (@RETURN_VAL)
						WHEN 23 THEN dbo.[GetPickListValueById] (@RETURN_VAL)
						ELSE @RETURN_VAL
					END
				END
			ELSE
				RETURN NULL
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[GetSystemValueLookup] 
(
	@AssetId varchar(36),
	@PropertyId varchar(36)
)
RETURNS varchar(max)
AS
BEGIN

	-- Declare the return variable here
	DECLARE @RETURN_VAL varchar(MAX)	

	If (@AssetId IS NULL)
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE
	
	IF (@AssetId = '')
		BEGIN
			SET @RETURN_VAL = NULL
		END
	ELSE
		BEGIN
						
			DECLARE @SystemTypeId int
			SELECT @SystemTypeId = [SystemTypeId] FROM [Properties] WITH (NoLock) WHERE [Id] = @PropertyId
			
			SELECT @RETURN_VAL =
				CASE @SystemTypeId
					WHEN 1 THEN dbo.[GetAssetType] (@AssetId)
					WHEN 2 THEN CAST([Created] as varchar(max))
					WHEN 3 THEN CAST([CreatedBy] as varchar(max))
					WHEN 4 THEN CAST([Deleted] as varchar(max))
					WHEN 5 THEN CAST([DeletedBy] AS varchar(max))
					WHEN 9 THEN CAST([LastModified] as varchar(max))
					WHEN 10 THEN CAST([LastModifiedBy] as varchar(max))
					ELSE NULL
				END	
			FROM [Assets] WITH (NoLock) WHERE [Id] = @AssetId							
			
		END

	-- Return the result of the function
	RETURN @RETURN_VAL

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
