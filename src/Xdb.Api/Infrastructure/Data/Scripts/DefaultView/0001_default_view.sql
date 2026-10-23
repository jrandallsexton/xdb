-- A fresh install's first view. The UI admits only members who can see a stand-alone view through an active role,
-- and a new database has none, so not even an administrator could get in. This seeds a "Users" view of the User
-- asset type for the Admin role. Only when the database has no views at all: an existing database is unchanged.

DECLARE @View uniqueidentifier = '112A4897-EB60-4222-A356-105C11343554';
DECLARE @UserType uniqueidentifier = '6C5DE6FF-1F85-4016-84ED-2F78B7C10517';
DECLARE @AdminRole uniqueidentifier = '68F8BBAB-C026-4ED4-A875-164C48E8874D';
DECLARE @SystemAdmin uniqueidentifier = '6CACA132-7827-4DE6-81E6-2B7D8DB939B9';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Views])
BEGIN
    INSERT INTO [dbo].[Views] ([Id], [Name], [DisplayValue], [Description], [AssetTypeId], [CreatedBy], [IsInstance], [IsStandAlone], [IsCreation])
    VALUES (@View, 'Users', 'Users', 'The members of this installation', @UserType, @SystemAdmin, 0, 1, 0);

    INSERT INTO [dbo].[RolesViews] ([RoleId], [ViewId], [CreatedBy])
    VALUES (@AdminRole, @View, @SystemAdmin);
END
