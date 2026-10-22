-- Restores three report procedures (and the three functions they call) that the pre-baseline cleanup removed.
-- They are referenced by stored report metadata (Reports.StoredProc), not by code, so the reference check missed them.
-- Definitions copied verbatim with their original SET options. Idempotent: each object is dropped first if present.
IF OBJECT_ID(N'dbo.IsAGuid') IS NOT NULL DROP FUNCTION [dbo].[IsAGuid];
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[IsAGuid] (@Data varchar(max))

-- http://stackoverflow.com/questions/4649317/how-to-check-if-a-string-is-a-uniqueidentifier
RETURNS bit

WITH SCHEMABINDING AS

BEGIN

return case
when @Data like '[0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0‌​-9a-fA-F]-[0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F]-[0-9a-fA-F][0-9a-fA-F][0-9‌​a-fA-F][0-9a-fA-F]-[0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F]-[0-9a-fA-F][0-9a-‌​fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F‌​][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F]'
then 1 else 0 end END
GO
IF OBJECT_ID(N'dbo.F_AGE_DD_HH_MM') IS NOT NULL DROP FUNCTION [dbo].[F_AGE_DD_HH_MM];
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE function [dbo].[F_AGE_DD_HH_MM]
	(
	@Date1		datetime,
	@Date2		datetime
	)
returns  varchar(15) 
as

begin

declare @final varchar(15)

declare @Days decimal
declare @Hours decimal
declare @Minutes decimal
declare @Seconds decimal

set @Days = DATEDIFF(ss,@Date1,@Date2)/60/60/24 --Days
declare @RemainderDate as datetime = @Date2 - @Days

set @Hours = datediff(ss, @Date1, @RemainderDate)/60/60 --Hours
set @RemainderDate = @RemainderDate - (@Hours/24.0)

set @Minutes = datediff(ss, @Date1, @RemainderDate)/60 --Minutes
set @RemainderDate = @RemainderDate - (@Minutes/24.0/60)

set @Seconds = DATEDIFF(SS, @Date1, @RemainderDate)    
--set @Days as ElapsedDays, @Hours as ElapsedHours, @Minutes as ElapsedMinutes, @Seconds as ElapsedSeconds

select @final =
	right( '00' + convert(varchar(2),@Days), 2 ) + 'd ' +
	right( '00' + convert(varchar(2),@Hours), 2) + 'h ' +
	right( '00' + convert(varchar(2),@Minutes), 2) + 'm'
	
--select @final = @Days + 'd:' + @Hours + 'h:' + @Minutes + 'm'

return @final

end
GO
IF OBJECT_ID(N'dbo.GetUnknownValue') IS NOT NULL DROP FUNCTION [dbo].[GetUnknownValue];
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[GetUnknownValue] 
(
	@SomeUnknownValue varchar(max)
)
RETURNS varchar(max)
AS
BEGIN

	DECLARE @RETURN_VAL varchar(MAX)	

	DECLARE @IsGuid bit
	SELECT @IsGuid = dbo.IsAGuid(@SomeUnknownValue)

	IF @IsGuid = 0
		BEGIN
			RETURN @SomeUnknownValue
		END
	ELSE

		SELECT @RETURN_VAL = IsNull([DisplayValue], [Name])
		FROM [Assets] A WHERE A.[Id] = @SomeUnknownValue

		IF @RETURN_VAL IS NULL
			BEGIN
				SELECT @RETURN_VAL = IsNull([DisplayValue], [Value])
				FROM [PickListValues] PLV WHERE PLV.[Id] = @SomeUnknownValue
			END
		ELSE
			RETURN @RETURN_VAL	
		
		RETURN @RETURN_VAL

END
GO
IF OBJECT_ID(N'dbo.rpt_Questions') IS NOT NULL DROP PROCEDURE [dbo].[rpt_Questions];
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[rpt_Questions]

AS

SELECT rn, [AssetId], [Asset], [Survey], [Recipient], [Application Department], [Application Type], 
[Functional Sponsor], [Message], [Contacted], [Response], [Response Text], [Response Date] FROM ( 
SELECT row_number() over (ORDER BY [Asset] ASC, [Recipient] ASC, [Index] ASC)  rn, * FROM ( 

	select
		QG.[AssetId],
		QG.[Caption] as [Asset],
		S.[Name] AS [Survey],
		dbo.GetMemberValueById(M.Id) AS [Recipient],
		dbo.GetPropertyValueLookup(QG.[AssetId], '7547d3f1-84c8-4f96-af2d-8fded12da91c') AS [Application Department],
		dbo.GetPropertyValueLookup(QG.[AssetId], 'dfcf3646-c714-49f2-82bc-6e34b2174bbe') AS [Application Type],
		dbo.GetPropertyValueLookup(QG.[AssetId], 'ffe7d418-6234-4d6a-8ea8-93c8611b1103') AS [Functional Sponsor],
		Q.[Index],
		Q.[Message],
		Q.[Created] AS [Contacted],
		case Q.Response
			WHEN -1 THEN 'Unknown'
			WHEN 0 THEN 'No'
			WHEN 1 THEN 'Yes'
			WHEN 2 THEN 'N/A'
			ELSE NULL
		END AS [Response],
		dbo.[GetUnknownValue](Q.[ResponseText]) AS [Response Text],
		Q.[Responded] AS [Response Date]
		--[dbo].[F_AGE_DD_HH_MM](q.Created, IsNull(Q.Responded, GETDATE())) AS [Time Elapsed]
	from [Questions] Q
		INNER JOIN [QuestionGroups] QG ON QG.[Id] = Q.[QuestionGroupId]
		INNER JOIN [Members] M ON M.[Id] = Q.[MemberId]
		INNER JOIN [Surveys] S ON S.[Id] = QG.[SurveyId] 
	--WHERE [Response] IS NOT NULL
	
) AS someTable) AS anotherTable WHERE rn between 1 AND 1000000
GO
IF OBJECT_ID(N'dbo.rpt_Questions_Answered') IS NOT NULL DROP PROCEDURE [dbo].[rpt_Questions_Answered];
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[rpt_Questions_Answered]

AS

SELECT rn, [AssetId], [Asset], [Survey], [Recipient], [Application Department], [Application Type], [Functional Sponsor], [Message], [Contacted], [Response], [Response Text], [Response Date], [Time Elapsed] FROM ( 
SELECT row_number() over (ORDER BY [Asset] ASC, [Recipient] ASC, [Index] ASC)  rn, * FROM ( 

	select
		QG.[AssetId],
		QG.[Caption] as [Asset],
		S.[Name] AS [Survey],
		dbo.GetMemberValueById(M.Id) AS [Recipient],
		dbo.GetPropertyValueLookup(QG.[AssetId], '7547d3f1-84c8-4f96-af2d-8fded12da91c') AS [Application Department],
		dbo.GetPropertyValueLookup(QG.[AssetId], 'dfcf3646-c714-49f2-82bc-6e34b2174bbe') AS [Application Type],
		dbo.GetPropertyValueLookup(QG.[AssetId], 'ffe7d418-6234-4d6a-8ea8-93c8611b1103') AS [Functional Sponsor],
		Q.[Index],
		Q.[Message],
		Q.[Created] AS [Contacted],
		case Q.Response
			WHEN -1 THEN 'Unknown'
			WHEN 0 THEN 'No'
			WHEN 1 THEN 'Yes'
			WHEN 2 THEN 'N/A'
			ELSE NULL
		END AS [Response],
		dbo.[GetUnknownValue](Q.[ResponseText]) AS [Response Text],
		Q.[Responded] AS [Response Date],
		[dbo].[F_AGE_DD_HH_MM](q.Created, IsNull(Q.Responded, GETDATE())) AS [Time Elapsed]
	from [Questions] Q
		INNER JOIN [QuestionGroups] QG ON QG.[Id] = Q.[QuestionGroupId]
		INNER JOIN [Members] M ON M.[Id] = Q.[MemberId]
		INNER JOIN [Surveys] S ON S.[Id] = QG.[SurveyId] 
	WHERE [Response] IS NOT NULL
	
) AS someTable) AS anotherTable WHERE rn between 1 AND 1000000
GO
IF OBJECT_ID(N'dbo.rpt_Questions_Unanswered') IS NOT NULL DROP PROCEDURE [dbo].[rpt_Questions_Unanswered];
GO
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[rpt_Questions_Unanswered]

AS

SELECT rn, [AssetId], [Asset], [Survey], [Recipient], [Application Department], [Application Type], 
[Functional Sponsor], [Message], [Contacted] FROM ( 
SELECT row_number() over (ORDER BY [Asset] ASC, [Recipient] ASC, [Index] ASC)  rn, * FROM ( 

	select
		QG.[AssetId],
		QG.[Caption] as [Asset],
		S.[Name] AS [Survey],
		dbo.GetMemberValueById(M.Id) AS [Recipient],
		dbo.GetPropertyValueLookup(QG.[AssetId], '7547d3f1-84c8-4f96-af2d-8fded12da91c') AS [Application Department],
		dbo.GetPropertyValueLookup(QG.[AssetId], 'dfcf3646-c714-49f2-82bc-6e34b2174bbe') AS [Application Type],
		dbo.GetPropertyValueLookup(QG.[AssetId], 'ffe7d418-6234-4d6a-8ea8-93c8611b1103') AS [Functional Sponsor],
		Q.[Index],
		Q.[Message],
		Q.[Created] AS [Contacted]
	from [Questions] Q
		INNER JOIN [QuestionGroups] QG ON QG.[Id] = Q.[QuestionGroupId]
		INNER JOIN [Members] M ON M.[Id] = Q.[MemberId]
		INNER JOIN [Surveys] S ON S.[Id] = QG.[SurveyId] 
	WHERE [Response] IS NULL
	
) AS someTable) AS anotherTable WHERE rn between 1 AND 1000000
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
