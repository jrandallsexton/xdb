-- System rows a working install needs, with the ids the application hard-codes. Each insert is skipped when the
-- row exists, so an existing database (which has them all) is unchanged. Created dates use the column defaults;
-- seeded rows are attributed to the built-in system administrator.

DECLARE @SystemAdmin uniqueidentifier = '6CACA132-7827-4DE6-81E6-2B7D8DB939B9';
DECLARE @UserType uniqueidentifier = '6C5DE6FF-1F85-4016-84ED-2F78B7C10517';
DECLARE @AdminRole uniqueidentifier = '68F8BBAB-C026-4ED4-A875-164C48E8874D';

-- The User asset type: every member is an asset of this type, with the member's id.
IF NOT EXISTS (SELECT 1 FROM [dbo].[AssetTypes] WHERE [Id] = @UserType)
    INSERT INTO [dbo].[AssetTypes] ([Id], [Name], [Pluralization], [DefinitionLabel], [CreatedBy], [AllowAssets], [IsSystem])
    VALUES (@UserType, 'User', 'Users', 'User', @SystemAdmin, 1, 1);

-- System properties. The current set has AssetTypeIsInstance = 0 (or 1 for the instance-of pair); the System_*
-- duplicates kept from older versions have NULL and appear only on instance gen tables.
DECLARE @Properties TABLE (
    [Id] uniqueidentifier, [DataTypeId] tinyint, [Name] varchar(100), [DisplayValue] varchar(100), [Description] varchar(max),
    [AssetTypeId] uniqueidentifier, [AssetTypeIsInstance] bit, [SystemTypeId] int, [Singular] varchar(100), [Plural] varchar(100));

INSERT INTO @Properties VALUES
    ('8D145EE0-54D2-47DB-8785-5AE086C20DDE', 6, 'AssetName', 'Asset Name', 'System property for the asset name', NULL, 0, 0, NULL, NULL),
    ('48014265-A355-45EC-9878-94C041ABD927', 6, 'AssetType', 'Asset Type', 'The type of asset this is', NULL, 0, 1, NULL, NULL),
    ('39BA425F-845D-43E0-B92A-BA7D5CFEAAF7', 1, 'Created', 'Created', 'System creation date', NULL, 0, 2, 'Created', 'Created'),
    ('8BC5EFCE-7773-4A46-87AD-A902E815380E', 0, 'System_Created', 'Created', 'When the asset or value was created', NULL, NULL, 2, NULL, NULL),
    ('D8CCD7B5-C4F2-45E5-81FD-FE6F8585B0D3', 8, 'CreatedBy', 'Created By', 'Who created the asset', @UserType, 0, 3, 'Created By', 'Created By'),
    ('E9ADC8E6-A513-4C3C-80F7-675F1D4B3D1A', 8, 'System_CreatedBy', 'Created By', 'Who created the asset or value', @UserType, NULL, 3, NULL, NULL),
    ('E2A550BC-D54D-47F1-9019-EA8170A8EAFB', 6, 'Description', 'Description', 'Short description for this asset', NULL, 0, 6, NULL, NULL),
    ('E1AE755A-E987-42F4-8028-BF3C5AAA97DF', 9, 'InstanceOf', 'Instance Of', 'System property for the instance of asset name', NULL, 1, 7, NULL, NULL),
    ('080D9FBF-8EA3-47FE-8340-FB8C12353CF8', 6, 'InstanceOfDesc', 'Instance of Description', 'System property instance of description', NULL, 1, 8, NULL, NULL),
    ('3D0624F8-A5D3-44F9-948E-30C124F6A343', 1, 'LastModified', 'Last Modified', 'When the asset was last modified', NULL, 0, 9, 'Last Modified', 'Last Modified'),
    ('D08387F4-BB02-4FB2-B8BB-CC1EF4FFFF1A', 0, 'System_LastModified', 'Last Modified', 'When the asset or value was last modified', NULL, NULL, 9, NULL, NULL),
    ('CB195972-2166-4A7A-BCDD-FCA5D8548AB1', 8, 'LastModifiedBy', 'Last Modified By', 'Who last modified the asset', @UserType, 0, 10, 'Last Modified By', 'Last Modified By'),
    ('9401CCCB-9056-4E01-A709-214BE9C0C9CD', 8, 'System_LastModifiedBy', 'Last Modified By', 'Who last modified the asset or value', @UserType, NULL, 10, NULL, NULL),
    ('FCF8E45A-CD12-4A9D-8C3F-779444F61359', 6, 'System_DisplayValue', 'Display Value', 'The display value of the asset - if different from the name', NULL, 0, 12, NULL, NULL);

INSERT INTO [dbo].[Properties] ([Id], [DataTypeId], [IsOrdered], [AllowMultiValue], [Name], [DisplayValue], [Description],
    [AssetTypeId], [AssetTypeIsInstance], [IsSystem], [SystemTypeId], [Singular], [Plural], [CreatedBy], [Approved], [ApprovedBy])
SELECT S.[Id], S.[DataTypeId], 0, 0, S.[Name], S.[DisplayValue], S.[Description],
    S.[AssetTypeId], S.[AssetTypeIsInstance], 1, S.[SystemTypeId], S.[Singular], S.[Plural], @SystemAdmin, GETDATE(), @SystemAdmin
FROM @Properties S
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Properties] P WHERE P.[Id] = S.[Id]);

-- System roles. Membership in Admin is what makes a member an administrator.
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @AdminRole)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [CreatedBy], [IsSystem], [IsActive])
    VALUES (@AdminRole, 'Admin', 'Admin', @SystemAdmin, 1, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = '1BCD0F1B-FC8E-4495-BA7E-D123A7F49D58')
    INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [CreatedBy], [IsSystem], [IsActive])
    VALUES ('1BCD0F1B-FC8E-4495-BA7E-D123A7F49D58', 'DB Admin', 'Database admins', @SystemAdmin, 1, 1);

-- The built-in system administrator: a member, its User asset, and its Admin membership.
IF NOT EXISTS (SELECT 1 FROM [dbo].[Members] WHERE [Id] = @SystemAdmin)
    INSERT INTO [dbo].[Members] ([Id], [UserId], [LName], [FName], [CreatedBy])
    VALUES (@SystemAdmin, 'admin', 'Administrator', 'System', @SystemAdmin);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Assets] WHERE [Id] = @SystemAdmin)
    INSERT INTO [dbo].[Assets] ([Id], [AssetTypeId], [Name], [DisplayValue], [CreatedBy], [Approved], [ApprovedBy], [IsInstance])
    VALUES (@SystemAdmin, @UserType, 'Admin', 'Administrator, System [Admin]', @SystemAdmin, GETDATE(), @SystemAdmin, 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[RoleMembers] WHERE [RoleId] = @AdminRole AND [MemberId] = @SystemAdmin)
    INSERT INTO [dbo].[RoleMembers] ([RoleId], [MemberId], [CreatedBy])
    VALUES (@AdminRole, @SystemAdmin, @SystemAdmin);
