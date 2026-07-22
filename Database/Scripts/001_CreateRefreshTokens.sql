/* ==========================================================================
   001_CreateRefreshTokens.sql

   Creates the refresh-token store used by JWT authentication.

   Run this against the application database BEFORE deploying the API build
   that issues tokens - /api/Login/Login and /api/Login/Refresh both fail
   without it.

   Safe to re-run: every statement is guarded.
   ========================================================================== */

IF OBJECT_ID(N'dbo.sm_RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.sm_RefreshTokens
    (
        Pk_RefreshTokenId    BIGINT          IDENTITY(1,1) NOT NULL,

        /* SHA-256 of the token, base64 encoded. The raw token is never stored. */
        TokenHash            VARCHAR(88)     NOT NULL,

        /* Login id the token belongs to (sm_Users.LoginId). */
        UserName             NVARCHAR(256)   NOT NULL,
        UserCode             NVARCHAR(50)    NULL,

        /* Claim snapshot taken at sign-in, used when re-issuing an access token. */
        UserDisplayName      NVARCHAR(256)   NULL,
        Email                NVARCHAR(256)   NULL,
        UserType             NVARCHAR(50)    NULL,
        AccessLevel          NVARCHAR(50)    NULL,

        ExpiresAtUtc         DATETIME2(3)    NOT NULL,
        CreatedAtUtc         DATETIME2(3)    NOT NULL,
        CreatedByIp          NVARCHAR(64)    NULL,

        RevokedAtUtc         DATETIME2(3)    NULL,
        RevokedByIp          NVARCHAR(64)    NULL,
        ReplacedByTokenHash  VARCHAR(88)     NULL,
        RevokedReason        NVARCHAR(200)   NULL,

        CONSTRAINT PK_sm_RefreshTokens PRIMARY KEY CLUSTERED (Pk_RefreshTokenId)
    );

    PRINT 'Created table dbo.sm_RefreshTokens.';
END
ELSE
BEGIN
    PRINT 'Table dbo.sm_RefreshTokens already exists - skipped.';
END
GO

/* Every refresh request looks the token up by hash, and a hash must be unique. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'UX_sm_RefreshTokens_TokenHash'
                 AND object_id = OBJECT_ID(N'dbo.sm_RefreshTokens'))
BEGIN
    CREATE UNIQUE INDEX UX_sm_RefreshTokens_TokenHash
        ON dbo.sm_RefreshTokens (TokenHash);

    PRINT 'Created index UX_sm_RefreshTokens_TokenHash.';
END
GO

/* Supports "revoke every session for this user" and per-user auditing. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_sm_RefreshTokens_UserName'
                 AND object_id = OBJECT_ID(N'dbo.sm_RefreshTokens'))
BEGIN
    CREATE INDEX IX_sm_RefreshTokens_UserName
        ON dbo.sm_RefreshTokens (UserName)
        INCLUDE (ExpiresAtUtc, RevokedAtUtc);

    PRINT 'Created index IX_sm_RefreshTokens_UserName.';
END
GO

/* --------------------------------------------------------------------------
   Optional housekeeping.

   Rotated and expired rows accumulate - one row per refresh, per session.
   Schedule this (SQL Agent / Azure Automation) to keep the table small.
   Nothing in the application depends on the history being retained.
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Sp_PurgeExpiredRefreshTokens', N'P') IS NOT NULL
    DROP PROCEDURE dbo.Sp_PurgeExpiredRefreshTokens;
GO

CREATE PROCEDURE dbo.Sp_PurgeExpiredRefreshTokens
    @RetainDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.sm_RefreshTokens
    WHERE ExpiresAtUtc < DATEADD(DAY, -@RetainDays, SYSUTCDATETIME());

    PRINT CONCAT('Purged ', @@ROWCOUNT, ' expired refresh token(s).');
END
GO
