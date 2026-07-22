/* ==========================================================================
   002_CreateNotifications.sql

   Creates the notification store.

   Two tables on purpose:

     sm_Notifications       one row per event, scoped to a tenant
     sm_NotificationStates  one row per user who has read or dismissed it

   Storing the event once keeps a 500-row bulk import to a single notification
   row instead of one per user, while read state stays per person.

   Run before deploying the build that exposes /api/Notifications.
   Safe to re-run: every statement is guarded.
   ========================================================================== */

IF OBJECT_ID(N'dbo.sm_Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.sm_Notifications
    (
        Pk_NotificationId  BIGINT          IDENTITY(1,1) NOT NULL,

        /* Everyone in this tenant sees the notification. */
        Fk_TenentCode      NVARCHAR(10)    NOT NULL,

        /* StockInward | StockOutward | StockReturn | StockDeleted | Material | System */
        NotificationType   NVARCHAR(50)    NOT NULL,

        /* Info | Success | Warning | Critical */
        Severity           NVARCHAR(20)    NOT NULL,

        Title              NVARCHAR(200)   NOT NULL,
        Message            NVARCHAR(1000)  NOT NULL,

        MaterialNumber     NVARCHAR(100)   NULL,
        SerialNumber       NVARCHAR(100)   NULL,

        /* Lets the UI deep-link back to whatever the event was about. */
        ReferenceType      NVARCHAR(50)    NULL,
        ReferenceId        NVARCHAR(100)   NULL,

        CreatedByUser      NVARCHAR(256)   NULL,
        CreatedAtUtc       DATETIME2(3)    NOT NULL,

        CONSTRAINT PK_sm_Notifications PRIMARY KEY CLUSTERED (Pk_NotificationId)
    );

    PRINT 'Created table dbo.sm_Notifications.';
END
ELSE
    PRINT 'Table dbo.sm_Notifications already exists - skipped.';
GO

IF OBJECT_ID(N'dbo.sm_NotificationStates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.sm_NotificationStates
    (
        Pk_NotificationStateId BIGINT        IDENTITY(1,1) NOT NULL,
        Fk_NotificationId      BIGINT        NOT NULL,
        UserName               NVARCHAR(256) NOT NULL,

        ReadAtUtc              DATETIME2(3)  NULL,

        /* Clearing hides the notification for this user only. */
        DismissedAtUtc         DATETIME2(3)  NULL,

        CONSTRAINT PK_sm_NotificationStates PRIMARY KEY CLUSTERED (Pk_NotificationStateId),
        CONSTRAINT FK_sm_NotificationStates_Notification
            FOREIGN KEY (Fk_NotificationId)
            REFERENCES dbo.sm_Notifications (Pk_NotificationId)
            ON DELETE CASCADE
    );

    PRINT 'Created table dbo.sm_NotificationStates.';
END
ELSE
    PRINT 'Table dbo.sm_NotificationStates already exists - skipped.';
GO

/* The list query is always "this tenant, newest first". */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_sm_Notifications_Tenant_Created'
                 AND object_id = OBJECT_ID(N'dbo.sm_Notifications'))
BEGIN
    CREATE INDEX IX_sm_Notifications_Tenant_Created
        ON dbo.sm_Notifications (Fk_TenentCode, CreatedAtUtc DESC);

    PRINT 'Created index IX_sm_Notifications_Tenant_Created.';
END
GO

/* One state row per user per notification, and the join goes through it. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'UX_sm_NotificationStates_Notification_User'
                 AND object_id = OBJECT_ID(N'dbo.sm_NotificationStates'))
BEGIN
    CREATE UNIQUE INDEX UX_sm_NotificationStates_Notification_User
        ON dbo.sm_NotificationStates (Fk_NotificationId, UserName)
        INCLUDE (ReadAtUtc, DismissedAtUtc);

    PRINT 'Created index UX_sm_NotificationStates_Notification_User.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_sm_NotificationStates_UserName'
                 AND object_id = OBJECT_ID(N'dbo.sm_NotificationStates'))
BEGIN
    CREATE INDEX IX_sm_NotificationStates_UserName
        ON dbo.sm_NotificationStates (UserName);

    PRINT 'Created index IX_sm_NotificationStates_UserName.';
END
GO

/* --------------------------------------------------------------------------
   Optional housekeeping. Notifications are an activity feed, not an audit log
   (Log_record already covers auditing), so old rows can be discarded.
   Cascade removes the matching state rows.
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Sp_PurgeOldNotifications', N'P') IS NOT NULL
    DROP PROCEDURE dbo.Sp_PurgeOldNotifications;
GO

CREATE PROCEDURE dbo.Sp_PurgeOldNotifications
    @RetainDays INT = 90
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.sm_Notifications
    WHERE CreatedAtUtc < DATEADD(DAY, -@RetainDays, SYSUTCDATETIME());

    PRINT CONCAT('Purged ', @@ROWCOUNT, ' notification(s).');
END
GO
