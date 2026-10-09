/* =====================================================================
   Administrative Residence Information System (ARIS) - DATABASE SCHEMA v2 (final)
   Target : SQL Server 2019+  (uses rowversion, filtered indexes, CONCAT_WS, ISJSON)
   Run on : an EMPTY database. Batches are separated by GO (SSMS / Azure Data
            Studio / sqlcmd -I). File is ASCII-only on purpose (no encoding issues).
   Design notes: see database_design_v2.md  (decision log D1..D14)

   Layers of protection (cheapest first):
     1. Types / NOT NULL / CHECK        -> bad values can never be stored
     2. FK / UNIQUE / filtered UNIQUE   -> "at most one ACTIVE ..." rules
     3. Triggers                        -> history append-only, no overlap, audit append-only
     4. Views (vw_GoldenInvariantViolations) + Application layer
                                        -> rules that SQL Server cannot express as constraints
   ===================================================================== */

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------------------------------------------------------------------
   1. UserAccount  (D10)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.UserAccount (
    UserId             int           IDENTITY(1,1) NOT NULL,
    Username           nvarchar(50)  NOT NULL,
    PasswordHash       varchar(255)  NOT NULL,
    FullName           nvarchar(100) NOT NULL,
    Role               varchar(10)   NOT NULL,
    Status             varchar(10)   NOT NULL CONSTRAINT DF_UserAccount_Status       DEFAULT 'ACTIVE',
    FailedLoginCount   int           NOT NULL CONSTRAINT DF_UserAccount_Failed       DEFAULT 0,
    LockedUntil        datetime2(3)  NULL,
    MustChangePassword bit           NOT NULL CONSTRAINT DF_UserAccount_MustChange   DEFAULT 1,
    PasswordChangedAt  datetime2(3)  NULL,
    CreatedAt          datetime2(3)  NOT NULL CONSTRAINT DF_UserAccount_CreatedAt    DEFAULT SYSUTCDATETIME(),
    RowVersion         rowversion    NOT NULL,
    CONSTRAINT PK_UserAccount          PRIMARY KEY (UserId),
    CONSTRAINT UQ_UserAccount_Username UNIQUE (Username),
    CONSTRAINT CK_UserAccount_Role     CHECK (Role   IN ('OFFICER','ADMIN')),
    CONSTRAINT CK_UserAccount_Status   CHECK (Status IN ('ACTIVE','LOCKED')),
    CONSTRAINT CK_UserAccount_Failed   CHECK (FailedLoginCount >= 0)
);
GO

/* ---------------------------------------------------------------------
   2. SystemConfiguration  (SYS-01, D11)  - simple key/value
   --------------------------------------------------------------------- */
CREATE TABLE dbo.SystemConfiguration (
    ConfigKey   varchar(100)  NOT NULL,
    ConfigValue nvarchar(500) NOT NULL,
    Description nvarchar(300) NULL,
    UpdatedAt   datetime2(3)  NOT NULL CONSTRAINT DF_SysConfig_UpdatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedBy   int           NULL,
    CONSTRAINT PK_SystemConfiguration PRIMARY KEY (ConfigKey),
    CONSTRAINT FK_SysConfig_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.UserAccount (UserId)
);
GO

/* ---------------------------------------------------------------------
   3. Citizen  (D2)
      UNASSIGNED = record exists, not yet in any household
      ACTIVE     = exactly one ACTIVE membership + one ACTIVE residence (Golden Invariant)
      INACTIVE   = no longer managed (terminal in MVP)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Citizen (
    CitizenId     int           IDENTITY(1,1) NOT NULL,
    NationalId    char(12)      NOT NULL,
    FullName      nvarchar(100) NOT NULL,
    DateOfBirth   date          NOT NULL,
    Gender        varchar(10)   NOT NULL,
    Status        varchar(12)   NOT NULL CONSTRAINT DF_Citizen_Status    DEFAULT 'UNASSIGNED',
    CreatedAt     datetime2(3)  NOT NULL CONSTRAINT DF_Citizen_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt     datetime2(3)  NOT NULL CONSTRAINT DF_Citizen_UpdatedAt DEFAULT SYSUTCDATETIME(),
    DeactivatedAt datetime2(3)  NULL,
    RowVersion    rowversion    NOT NULL,
    CONSTRAINT PK_Citizen            PRIMARY KEY (CitizenId),
    CONSTRAINT UQ_Citizen_NationalId UNIQUE (NationalId),
    -- char(12) pads with spaces, so a value shorter than 12 digits fails this check too
    CONSTRAINT CK_Citizen_NationalId CHECK (NationalId NOT LIKE '%[^0-9]%'),
    CONSTRAINT CK_Citizen_Gender     CHECK (Gender IN ('MALE','FEMALE','OTHER')),
    CONSTRAINT CK_Citizen_Status     CHECK (Status IN ('UNASSIGNED','ACTIVE','INACTIVE')),
    CONSTRAINT CK_Citizen_Lifecycle  CHECK (
           (Status = 'INACTIVE' AND DeactivatedAt IS NOT NULL)
        OR (Status <> 'INACTIVE' AND DeactivatedAt IS NULL))
);
CREATE INDEX IX_Citizen_FullName ON dbo.Citizen (FullName);
CREATE INDEX IX_Citizen_Status   ON dbo.Citizen (Status);
GO

/* ---------------------------------------------------------------------
   4. Address  (D8, D9)
      Not edited when administrative boundaries change: create a NEW address and
      point the old one to it with ReplacedByAddressId (many old -> one new).
      Split (one old -> many new) is OUT OF SCOPE.
      District is nullable: legacy addresses only.
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Address (
    AddressId           int           IDENTITY(1,1) NOT NULL,
    AddressCode         varchar(30)   NOT NULL,
    AddressLine         nvarchar(200) NOT NULL,   -- house number + street / hamlet
    Ward                nvarchar(100) NOT NULL,   -- phuong / xa
    District            nvarchar(100) NULL,       -- quan / huyen (legacy only)
    Province            nvarchar(100) NOT NULL,
    AddressText         AS (CONCAT_WS(N', ', AddressLine, Ward, District, Province)),
    Status              varchar(10)   NOT NULL CONSTRAINT DF_Address_Status    DEFAULT 'ACTIVE',
    ValidFrom           date          NOT NULL,
    ValidTo             date          NULL,
    ReplacedByAddressId int           NULL,
    CreatedAt           datetime2(3)  NOT NULL CONSTRAINT DF_Address_CreatedAt DEFAULT SYSUTCDATETIME(),
    DeactivatedAt       datetime2(3)  NULL,
    RowVersion          rowversion    NOT NULL,
    CONSTRAINT PK_Address           PRIMARY KEY (AddressId),
    CONSTRAINT UQ_Address_Code      UNIQUE (AddressCode),
    CONSTRAINT FK_Address_ReplacedBy FOREIGN KEY (ReplacedByAddressId) REFERENCES dbo.Address (AddressId),
    CONSTRAINT CK_Address_Status    CHECK (Status IN ('ACTIVE','INACTIVE')),
    CONSTRAINT CK_Address_Lifecycle CHECK (
           (Status = 'ACTIVE'   AND ValidTo IS NULL     AND DeactivatedAt IS NULL)
        OR (Status = 'INACTIVE' AND ValidTo IS NOT NULL AND DeactivatedAt IS NOT NULL AND ValidTo >= ValidFrom)),
    CONSTRAINT CK_Address_Replacement CHECK (
           ReplacedByAddressId IS NULL
        OR (Status = 'INACTIVE' AND ReplacedByAddressId <> AddressId))
);
CREATE INDEX IX_Address_Area       ON dbo.Address (Province, Ward);
CREATE INDEX IX_Address_ReplacedBy ON dbo.Address (ReplacedByAddressId) WHERE ReplacedByAddressId IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   5. Household  (D4)
      The household's address lives ONLY in HouseholdAddress (single source of truth).
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Household (
    HouseholdId   int          IDENTITY(1,1) NOT NULL,
    Status        varchar(10)  NOT NULL CONSTRAINT DF_Household_Status    DEFAULT 'ACTIVE',
    CreatedAt     datetime2(3) NOT NULL CONSTRAINT DF_Household_CreatedAt DEFAULT SYSUTCDATETIME(),
    InactivatedAt datetime2(3) NULL,
    RowVersion    rowversion   NOT NULL,
    CONSTRAINT PK_Household           PRIMARY KEY (HouseholdId),
    CONSTRAINT CK_Household_Status    CHECK (Status IN ('ACTIVE','INACTIVE')),
    CONSTRAINT CK_Household_Lifecycle CHECK (
           (Status = 'ACTIVE'   AND InactivatedAt IS NULL)
        OR (Status = 'INACTIVE' AND InactivatedAt IS NOT NULL))
);
GO

/* ---------------------------------------------------------------------
   6. TransferRequest  (D1, D5, D6) - header of a transfer (maker-checker)
      JOIN_EXISTING  : citizens join an existing household (address = that household's address)
      NEW_HOUSEHOLD  : citizens split off into a brand-new household at DestinationAddressId
      HOUSEHOLD_MOVE : the WHOLE household moves to DestinationAddressId (same HouseholdId kept)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.TransferRequest (
    RequestId              int           IDENTITY(1,1) NOT NULL,
    RequestType            varchar(20)   NOT NULL,
    SourceHouseholdId      int           NOT NULL,
    DestinationHouseholdId int           NULL,
    DestinationAddressId   int           NULL,
    EffectiveDate          date          NOT NULL,
    NewHeadCitizenId       int           NULL,   -- head of the new household (NEW_HOUSEHOLD)
    SuccessorHeadCitizenId int           NULL,   -- new head of source household if current head leaves but others stay
    ResultHouseholdId      int           NULL,   -- household created / joined, filled on approval
    Status                 varchar(10)   NOT NULL CONSTRAINT DF_TR_Status      DEFAULT 'PENDING',
    RequestedBy            int           NOT NULL,
    RequestedAt            datetime2(3)  NOT NULL CONSTRAINT DF_TR_RequestedAt DEFAULT SYSUTCDATETIME(),
    DecidedBy              int           NULL,
    DecidedAt              datetime2(3)  NULL,
    DecisionNote           nvarchar(500) NULL,
    RowVersion             rowversion    NOT NULL,
    CONSTRAINT PK_TransferRequest PRIMARY KEY (RequestId),
    CONSTRAINT FK_TR_SourceHousehold FOREIGN KEY (SourceHouseholdId)      REFERENCES dbo.Household   (HouseholdId),
    CONSTRAINT FK_TR_DestHousehold   FOREIGN KEY (DestinationHouseholdId) REFERENCES dbo.Household   (HouseholdId),
    CONSTRAINT FK_TR_DestAddress     FOREIGN KEY (DestinationAddressId)   REFERENCES dbo.Address     (AddressId),
    CONSTRAINT FK_TR_NewHead         FOREIGN KEY (NewHeadCitizenId)       REFERENCES dbo.Citizen     (CitizenId),
    CONSTRAINT FK_TR_SuccessorHead   FOREIGN KEY (SuccessorHeadCitizenId) REFERENCES dbo.Citizen     (CitizenId),
    CONSTRAINT FK_TR_ResultHousehold FOREIGN KEY (ResultHouseholdId)      REFERENCES dbo.Household   (HouseholdId),
    CONSTRAINT FK_TR_RequestedBy     FOREIGN KEY (RequestedBy)            REFERENCES dbo.UserAccount (UserId),
    CONSTRAINT FK_TR_DecidedBy       FOREIGN KEY (DecidedBy)              REFERENCES dbo.UserAccount (UserId),
    CONSTRAINT CK_TR_Type   CHECK (RequestType IN ('JOIN_EXISTING','NEW_HOUSEHOLD','HOUSEHOLD_MOVE')),
    CONSTRAINT CK_TR_Status CHECK (Status IN ('PENDING','APPROVED','REJECTED','CANCELLED')),
    -- each request type must carry exactly the columns it needs (no redundant / contradictory destination)
    CONSTRAINT CK_TR_Shape CHECK (
           (RequestType = 'JOIN_EXISTING'
              AND DestinationHouseholdId IS NOT NULL AND DestinationHouseholdId <> SourceHouseholdId
              AND DestinationAddressId IS NULL AND NewHeadCitizenId IS NULL)
        OR (RequestType = 'NEW_HOUSEHOLD'
              AND DestinationHouseholdId IS NULL
              AND DestinationAddressId IS NOT NULL AND NewHeadCitizenId IS NOT NULL)
        OR (RequestType = 'HOUSEHOLD_MOVE'
              AND DestinationHouseholdId IS NULL
              AND DestinationAddressId IS NOT NULL AND NewHeadCitizenId IS NULL AND SuccessorHeadCitizenId IS NULL)),
    -- maker-checker: the approver/rejecter must be a different person than the requester
    CONSTRAINT CK_TR_Decision CHECK (
           (Status = 'PENDING' AND DecidedBy IS NULL AND DecidedAt IS NULL)
        OR (Status IN ('APPROVED','REJECTED') AND DecidedBy IS NOT NULL AND DecidedAt IS NOT NULL AND DecidedBy <> RequestedBy)
        OR (Status = 'CANCELLED' AND DecidedBy IS NOT NULL AND DecidedAt IS NOT NULL))
);
CREATE INDEX IX_TR_Status      ON dbo.TransferRequest (Status, RequestedAt);
CREATE INDEX IX_TR_SourceHH    ON dbo.TransferRequest (SourceHouseholdId);
CREATE INDEX IX_TR_DestHH      ON dbo.TransferRequest (DestinationHouseholdId) WHERE DestinationHouseholdId IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   7. HouseholdAddress  (D4) - address history of a household
   --------------------------------------------------------------------- */
CREATE TABLE dbo.HouseholdAddress (
    HouseholdAddressId     int          IDENTITY(1,1) NOT NULL,
    HouseholdId            int          NOT NULL,
    AddressId              int          NOT NULL,
    StartDate              date         NOT NULL,
    EndDate                date         NULL,
    Status                 varchar(10)  NOT NULL CONSTRAINT DF_HA_Status    DEFAULT 'ACTIVE',
    CreatedBy              int          NOT NULL,
    CreatedAt              datetime2(3) NOT NULL CONSTRAINT DF_HA_CreatedAt DEFAULT SYSUTCDATETIME(),
    EndedBy                int          NULL,
    EndedAt                datetime2(3) NULL,
    OpenedByRequestId      int          NULL,
    CorrectsHouseholdAddressId int      NULL,
    RowVersion             rowversion   NOT NULL,
    CONSTRAINT PK_HouseholdAddress PRIMARY KEY (HouseholdAddressId),
    CONSTRAINT FK_HA_Household FOREIGN KEY (HouseholdId)        REFERENCES dbo.Household       (HouseholdId),
    CONSTRAINT FK_HA_Address   FOREIGN KEY (AddressId)          REFERENCES dbo.Address         (AddressId),
    CONSTRAINT FK_HA_CreatedBy FOREIGN KEY (CreatedBy)          REFERENCES dbo.UserAccount     (UserId),
    CONSTRAINT FK_HA_EndedBy   FOREIGN KEY (EndedBy)            REFERENCES dbo.UserAccount     (UserId),
    CONSTRAINT FK_HA_Request   FOREIGN KEY (OpenedByRequestId)  REFERENCES dbo.TransferRequest (RequestId),
    CONSTRAINT FK_HA_Corrects  FOREIGN KEY (CorrectsHouseholdAddressId) REFERENCES dbo.HouseholdAddress (HouseholdAddressId),
    CONSTRAINT CK_HA_Status    CHECK (Status IN ('ACTIVE','ENDED','VOID')),
    CONSTRAINT CK_HA_Dates     CHECK (EndDate IS NULL OR EndDate >= StartDate),
    -- Status, EndDate and EndedAt must tell the same story
    CONSTRAINT CK_HA_Lifecycle CHECK (
           (Status = 'ACTIVE' AND EndDate IS NULL     AND EndedAt IS NULL)
        OR (Status = 'ENDED'  AND EndDate IS NOT NULL AND EndedAt IS NOT NULL)
        OR  Status = 'VOID')
);
-- a household has at most ONE active address
CREATE UNIQUE INDEX UX_HA_OneActivePerHousehold ON dbo.HouseholdAddress (HouseholdId) WHERE Status = 'ACTIVE';
CREATE INDEX IX_HA_Address ON dbo.HouseholdAddress (AddressId, Status);
GO

/* ---------------------------------------------------------------------
   8. HouseholdMembership  (D3) - who belongs to which household, in which role
      Role change = close the old row, open a new row (never UPDATE Role).
   --------------------------------------------------------------------- */
CREATE TABLE dbo.HouseholdMembership (
    MembershipId        int          IDENTITY(1,1) NOT NULL,
    CitizenId           int          NOT NULL,
    HouseholdId         int          NOT NULL,
    Role                varchar(10)  NOT NULL,
    StartDate           date         NOT NULL,
    EndDate             date         NULL,
    Status              varchar(10)  NOT NULL CONSTRAINT DF_HM_Status    DEFAULT 'ACTIVE',
    CreatedBy           int          NOT NULL,
    CreatedAt           datetime2(3) NOT NULL CONSTRAINT DF_HM_CreatedAt DEFAULT SYSUTCDATETIME(),
    EndedBy             int          NULL,
    EndedAt             datetime2(3) NULL,
    OpenedByRequestId   int          NULL,
    CorrectsMembershipId int         NULL,
    RowVersion          rowversion   NOT NULL,
    CONSTRAINT PK_HouseholdMembership PRIMARY KEY (MembershipId),
    CONSTRAINT UQ_HM_Id_Citizen UNIQUE (MembershipId, CitizenId),   -- target of composite FK from TransferRequestItem
    CONSTRAINT FK_HM_Citizen   FOREIGN KEY (CitizenId)          REFERENCES dbo.Citizen         (CitizenId),
    CONSTRAINT FK_HM_Household FOREIGN KEY (HouseholdId)        REFERENCES dbo.Household       (HouseholdId),
    CONSTRAINT FK_HM_CreatedBy FOREIGN KEY (CreatedBy)          REFERENCES dbo.UserAccount     (UserId),
    CONSTRAINT FK_HM_EndedBy   FOREIGN KEY (EndedBy)            REFERENCES dbo.UserAccount     (UserId),
    CONSTRAINT FK_HM_Request   FOREIGN KEY (OpenedByRequestId)  REFERENCES dbo.TransferRequest (RequestId),
    CONSTRAINT FK_HM_Corrects  FOREIGN KEY (CorrectsMembershipId) REFERENCES dbo.HouseholdMembership (MembershipId),
    CONSTRAINT CK_HM_Role      CHECK (Role   IN ('HEAD','MEMBER')),
    CONSTRAINT CK_HM_Status    CHECK (Status IN ('ACTIVE','ENDED','VOID')),
    CONSTRAINT CK_HM_Dates     CHECK (EndDate IS NULL OR EndDate >= StartDate),
    CONSTRAINT CK_HM_Lifecycle CHECK (
           (Status = 'ACTIVE' AND EndDate IS NULL     AND EndedAt IS NULL)
        OR (Status = 'ENDED'  AND EndDate IS NOT NULL AND EndedAt IS NOT NULL)
        OR  Status = 'VOID')
);
-- a citizen is an active member of at most ONE household
CREATE UNIQUE INDEX UX_HM_OneActivePerCitizen ON dbo.HouseholdMembership (CitizenId) WHERE Status = 'ACTIVE';
-- a household has at most ONE active head ("at least one" is checked by the application / violations view)
CREATE UNIQUE INDEX UX_HM_OneActiveHeadPerHousehold ON dbo.HouseholdMembership (HouseholdId) WHERE Status = 'ACTIVE' AND Role = 'HEAD';
CREATE INDEX IX_HM_Household ON dbo.HouseholdMembership (HouseholdId, Status) INCLUDE (CitizenId, Role);
CREATE INDEX IX_HM_Citizen   ON dbo.HouseholdMembership (CitizenId, StartDate);
GO

/* ---------------------------------------------------------------------
   9. Residence  (D3) - where a citizen lives, over time
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Residence (
    ResidenceId         int          IDENTITY(1,1) NOT NULL,
    CitizenId           int          NOT NULL,
    AddressId           int          NOT NULL,
    StartDate           date         NOT NULL,
    EndDate             date         NULL,
    Status              varchar(10)  NOT NULL CONSTRAINT DF_Res_Status    DEFAULT 'ACTIVE',
    CreatedBy           int          NOT NULL,
    CreatedAt           datetime2(3) NOT NULL CONSTRAINT DF_Res_CreatedAt DEFAULT SYSUTCDATETIME(),
    EndedBy             int          NULL,
    EndedAt             datetime2(3) NULL,
    OpenedByRequestId   int          NULL,
    CorrectsResidenceId int          NULL,
    RowVersion          rowversion   NOT NULL,
    CONSTRAINT PK_Residence PRIMARY KEY (ResidenceId),
    CONSTRAINT UQ_Res_Id_Citizen UNIQUE (ResidenceId, CitizenId),   -- target of composite FK from TransferRequestItem
    CONSTRAINT FK_Res_Citizen   FOREIGN KEY (CitizenId)           REFERENCES dbo.Citizen         (CitizenId),
    CONSTRAINT FK_Res_Address   FOREIGN KEY (AddressId)           REFERENCES dbo.Address         (AddressId),
    CONSTRAINT FK_Res_CreatedBy FOREIGN KEY (CreatedBy)           REFERENCES dbo.UserAccount     (UserId),
    CONSTRAINT FK_Res_EndedBy   FOREIGN KEY (EndedBy)             REFERENCES dbo.UserAccount     (UserId),
    CONSTRAINT FK_Res_Request   FOREIGN KEY (OpenedByRequestId)   REFERENCES dbo.TransferRequest (RequestId),
    CONSTRAINT FK_Res_Corrects  FOREIGN KEY (CorrectsResidenceId) REFERENCES dbo.Residence       (ResidenceId),
    CONSTRAINT CK_Res_Status    CHECK (Status IN ('ACTIVE','ENDED','VOID')),
    CONSTRAINT CK_Res_Dates     CHECK (EndDate IS NULL OR EndDate >= StartDate),
    CONSTRAINT CK_Res_Lifecycle CHECK (
           (Status = 'ACTIVE' AND EndDate IS NULL     AND EndedAt IS NULL)
        OR (Status = 'ENDED'  AND EndDate IS NOT NULL AND EndedAt IS NOT NULL)
        OR  Status = 'VOID')
);
-- a citizen has at most ONE active residence
CREATE UNIQUE INDEX UX_Res_OneActivePerCitizen ON dbo.Residence (CitizenId) WHERE Status = 'ACTIVE';
CREATE INDEX IX_Res_Address ON dbo.Residence (AddressId, Status);
CREATE INDEX IX_Res_Citizen ON dbo.Residence (CitizenId, StartDate) INCLUDE (EndDate, Status, AddressId);
GO

/* ---------------------------------------------------------------------
   10. TransferRequestItem  (D5) - one row per citizen in a request (supports partial approval)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.TransferRequestItem (
    ItemId             int           IDENTITY(1,1) NOT NULL,
    RequestId          int           NOT NULL,
    CitizenId          int           NOT NULL,
    SourceMembershipId int           NOT NULL,
    SourceResidenceId  int           NOT NULL,
    Status             varchar(10)   NOT NULL CONSTRAINT DF_TRI_Status DEFAULT 'PENDING',
    RejectReason       nvarchar(300) NULL,
    RowVersion         rowversion    NOT NULL,
    CONSTRAINT PK_TransferRequestItem PRIMARY KEY (ItemId),
    CONSTRAINT UQ_TRI_Request_Citizen UNIQUE (RequestId, CitizenId),
    CONSTRAINT FK_TRI_Request    FOREIGN KEY (RequestId) REFERENCES dbo.TransferRequest (RequestId),
    CONSTRAINT FK_TRI_Citizen    FOREIGN KEY (CitizenId) REFERENCES dbo.Citizen (CitizenId),
    -- composite FKs: the membership / residence MUST belong to the same citizen as the item
    CONSTRAINT FK_TRI_Membership FOREIGN KEY (SourceMembershipId, CitizenId) REFERENCES dbo.HouseholdMembership (MembershipId, CitizenId),
    CONSTRAINT FK_TRI_Residence  FOREIGN KEY (SourceResidenceId,  CitizenId) REFERENCES dbo.Residence (ResidenceId, CitizenId),
    CONSTRAINT CK_TRI_Status CHECK (Status IN ('PENDING','APPROVED','REJECTED','CANCELLED')),
    CONSTRAINT CK_TRI_Reject CHECK (Status <> 'REJECTED' OR RejectReason IS NOT NULL)
);
-- precondition "no other pending request": a citizen can be in at most ONE open item.
-- Application rule: item.Status = 'PENDING' if and only if its request is PENDING.
CREATE UNIQUE INDEX UX_TRI_OneOpenPerCitizen ON dbo.TransferRequestItem (CitizenId) WHERE Status = 'PENDING';
GO

/* ---------------------------------------------------------------------
   11. AuditLog  (D7) - append-only
       FAILURE rows must be written on a SEPARATE connection/transaction after rollback.
       CorrelationId groups all rows produced by one business action.
   --------------------------------------------------------------------- */
CREATE TABLE dbo.AuditLog (
    AuditLogId        bigint           IDENTITY(1,1) NOT NULL,
    ActorUserId       int              NULL,
    AttemptedUsername nvarchar(50)     NULL,          -- failed login with unknown user
    Action            varchar(50)      NOT NULL,
    EntityType        varchar(50)      NOT NULL,
    EntityId          int              NULL,
    OccurredAt        datetime2(3)     NOT NULL CONSTRAINT DF_Audit_OccurredAt DEFAULT SYSUTCDATETIME(),
    Result            varchar(10)      NOT NULL,
    Details           nvarchar(max)    NULL,          -- JSON: before / after / reason
    CorrelationId     uniqueidentifier NULL,
    CONSTRAINT PK_AuditLog PRIMARY KEY (AuditLogId),
    CONSTRAINT FK_Audit_Actor    FOREIGN KEY (ActorUserId) REFERENCES dbo.UserAccount (UserId),
    CONSTRAINT CK_Audit_Result   CHECK (Result IN ('SUCCESS','FAILURE')),
    CONSTRAINT CK_Audit_Details  CHECK (Details IS NULL OR ISJSON(Details) = 1),
    CONSTRAINT CK_Audit_Actor    CHECK (ActorUserId IS NOT NULL OR AttemptedUsername IS NOT NULL)
);
CREATE INDEX IX_Audit_Entity      ON dbo.AuditLog (EntityType, EntityId, OccurredAt);
CREATE INDEX IX_Audit_Actor       ON dbo.AuditLog (ActorUserId, OccurredAt);
CREATE INDEX IX_Audit_Correlation ON dbo.AuditLog (CorrelationId) WHERE CorrelationId IS NOT NULL;
GO


/* =====================================================================
   TRIGGERS  (D12)
   Each trigger rolls back the current transaction and raises an error.
   NOTE: the overlap check reads committed data, so the Application layer must
         still run the transfer inside a transaction with UPDLOCK/SERIALIZABLE
         (see database_design_v2.md section 6).
   ===================================================================== */

/* ---- Residence: history is append-only + no overlapping periods per citizen ---- */
CREATE TRIGGER dbo.trg_Residence_Guard
ON dbo.Residence
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- (1) BR-HIST-01: identity columns never change; VOID rows are frozen; ENDED rows may only become VOID
    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN inserted i ON i.ResidenceId = d.ResidenceId
        WHERE i.CitizenId <> d.CitizenId
           OR i.AddressId <> d.AddressId
           OR i.StartDate <> d.StartDate
           OR d.Status = 'VOID'
           OR (d.Status = 'ENDED' AND (i.Status <> 'VOID' OR i.EndDate <> d.EndDate))
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51001, N'Residence history is append-only: identity columns are immutable, VOID rows are frozen, ENDED rows can only become VOID.', 1;
    END;

    -- (2) BR-RES-03: periods of the same citizen must not overlap (VOID rows are ignored)
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Residence r
          ON r.CitizenId = i.CitizenId AND r.ResidenceId <> i.ResidenceId
        WHERE i.Status <> 'VOID' AND r.Status <> 'VOID'
          AND i.StartDate <= ISNULL(r.EndDate, '9999-12-31')
          AND r.StartDate <= ISNULL(i.EndDate, '9999-12-31')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51002, N'Residence periods of the same citizen overlap.', 1;
    END;
END;
GO

/* ---- HouseholdMembership: same two rules ---- */
CREATE TRIGGER dbo.trg_Membership_Guard
ON dbo.HouseholdMembership
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN inserted i ON i.MembershipId = d.MembershipId
        WHERE i.CitizenId   <> d.CitizenId
           OR i.HouseholdId <> d.HouseholdId
           OR i.Role        <> d.Role
           OR i.StartDate   <> d.StartDate
           OR d.Status = 'VOID'
           OR (d.Status = 'ENDED' AND (i.Status <> 'VOID' OR i.EndDate <> d.EndDate))
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51011, N'Membership history is append-only: citizen/household/role/start are immutable (change role = close + open a new row), VOID rows are frozen, ENDED rows can only become VOID.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.HouseholdMembership m
          ON m.CitizenId = i.CitizenId AND m.MembershipId <> i.MembershipId
        WHERE i.Status <> 'VOID' AND m.Status <> 'VOID'
          AND i.StartDate <= ISNULL(m.EndDate, '9999-12-31')
          AND m.StartDate <= ISNULL(i.EndDate, '9999-12-31')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51012, N'Membership periods of the same citizen overlap.', 1;
    END;
END;
GO

/* ---- HouseholdAddress: same two rules (overlap is per household) ---- */
CREATE TRIGGER dbo.trg_HouseholdAddress_Guard
ON dbo.HouseholdAddress
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN inserted i ON i.HouseholdAddressId = d.HouseholdAddressId
        WHERE i.HouseholdId <> d.HouseholdId
           OR i.AddressId   <> d.AddressId
           OR i.StartDate   <> d.StartDate
           OR d.Status = 'VOID'
           OR (d.Status = 'ENDED' AND (i.Status <> 'VOID' OR i.EndDate <> d.EndDate))
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51021, N'Household address history is append-only: identity columns are immutable, VOID rows are frozen, ENDED rows can only become VOID.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.HouseholdAddress h
          ON h.HouseholdId = i.HouseholdId AND h.HouseholdAddressId <> i.HouseholdAddressId
        WHERE i.Status <> 'VOID' AND h.Status <> 'VOID'
          AND i.StartDate <= ISNULL(h.EndDate, '9999-12-31')
          AND h.StartDate <= ISNULL(i.EndDate, '9999-12-31')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51022, N'Address periods of the same household overlap.', 1;
    END;
END;
GO

/* ---- TransferRequest: a decided request is frozen ---- */
CREATE TRIGGER dbo.trg_TransferRequest_Frozen
ON dbo.TransferRequest
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE Status <> 'PENDING')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51040, N'A decided transfer request (APPROVED / REJECTED / CANCELLED) can no longer be changed.', 1;
    END;
END;
GO

/* ---- AuditLog: append-only (BR-AUD-02) ----
   A DBA who must archive old rows does it with: DISABLE TRIGGER ... ; archive ; ENABLE TRIGGER ... */
CREATE TRIGGER dbo.trg_AuditLog_AppendOnly
ON dbo.AuditLog
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    ROLLBACK TRANSACTION;
    THROW 51030, N'AuditLog is append-only (BR-AUD-02). Never UPDATE or DELETE audit rows.', 1;
END;
GO


/* =====================================================================
   VIEWS  (D13)
   ===================================================================== */

/* Follows ReplacedByAddressId until the last address in the chain.
   "Valid address" (BR-RES-02, BR-HH-08) = its canonical address is ACTIVE. */
CREATE VIEW dbo.vw_AddressCanonical
AS
WITH chain AS (
    SELECT a.AddressId AS OriginId,
           a.AddressId AS CurrentId,
           a.ReplacedByAddressId AS NextId,
           0 AS Depth
    FROM dbo.Address a
    UNION ALL
    SELECT c.OriginId, n.AddressId, n.ReplacedByAddressId, c.Depth + 1
    FROM chain c
    JOIN dbo.Address n ON n.AddressId = c.NextId
    WHERE c.Depth < 20
)
SELECT c.OriginId  AS AddressId,
       c.CurrentId AS CanonicalAddressId,
       a.Status    AS CanonicalStatus
FROM chain c
JOIN dbo.Address a ON a.AddressId = c.CurrentId
WHERE c.NextId IS NULL;
GO

/* Current state of every household in one row (replaces the old Household.CurrentAddressId column) */
CREATE VIEW dbo.vw_HouseholdCurrent
AS
SELECT h.HouseholdId,
       h.Status,
       ha.AddressId   AS CurrentAddressId,
       head.CitizenId AS HeadCitizenId,
       (SELECT COUNT(*)
          FROM dbo.HouseholdMembership m
         WHERE m.HouseholdId = h.HouseholdId AND m.Status = 'ACTIVE') AS ActiveMemberCount
FROM dbo.Household h
LEFT JOIN dbo.HouseholdAddress ha
       ON ha.HouseholdId = h.HouseholdId AND ha.Status = 'ACTIVE'
LEFT JOIN dbo.HouseholdMembership head
       ON head.HouseholdId = h.HouseholdId AND head.Status = 'ACTIVE' AND head.Role = 'HEAD';
GO

/* GOLDEN INVARIANT CHECKER.  Must return ZERO rows at all times (after every committed operation).
   Use it in integration tests and as an after-the-fact audit query. */
CREATE VIEW dbo.vw_GoldenInvariantViolations
AS
-- C1: ACTIVE citizen without an active membership
SELECT CAST('CITIZEN_NO_ACTIVE_MEMBERSHIP' AS varchar(50)) AS ViolationType,
       c.CitizenId, CAST(NULL AS int) AS HouseholdId
FROM dbo.Citizen c
WHERE c.Status = 'ACTIVE'
  AND NOT EXISTS (SELECT 1 FROM dbo.HouseholdMembership m
                   WHERE m.CitizenId = c.CitizenId AND m.Status = 'ACTIVE')
UNION ALL
-- C2: ACTIVE citizen without an active residence
SELECT 'CITIZEN_NO_ACTIVE_RESIDENCE', c.CitizenId, NULL
FROM dbo.Citizen c
WHERE c.Status = 'ACTIVE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Residence r
                   WHERE r.CitizenId = c.CitizenId AND r.Status = 'ACTIVE')
UNION ALL
-- C3: residence address differs from the household address (compared after resolving address lineage)
SELECT 'CITIZEN_ADDRESS_MISMATCH', c.CitizenId, m.HouseholdId
FROM dbo.Citizen c
JOIN dbo.HouseholdMembership m ON m.CitizenId = c.CitizenId AND m.Status = 'ACTIVE'
JOIN dbo.HouseholdAddress   ha ON ha.HouseholdId = m.HouseholdId AND ha.Status = 'ACTIVE'
JOIN dbo.Residence           r ON r.CitizenId = c.CitizenId AND r.Status = 'ACTIVE'
JOIN dbo.vw_AddressCanonical rc ON rc.AddressId = r.AddressId
JOIN dbo.vw_AddressCanonical hc ON hc.AddressId = ha.AddressId
WHERE c.Status = 'ACTIVE'
  AND rc.CanonicalAddressId <> hc.CanonicalAddressId
UNION ALL
-- C4: UNASSIGNED / INACTIVE citizen that still holds an active membership or residence
SELECT 'CITIZEN_NOT_ACTIVE_BUT_ASSIGNED', c.CitizenId, NULL
FROM dbo.Citizen c
WHERE c.Status <> 'ACTIVE'
  AND (EXISTS (SELECT 1 FROM dbo.HouseholdMembership m WHERE m.CitizenId = c.CitizenId AND m.Status = 'ACTIVE')
    OR EXISTS (SELECT 1 FROM dbo.Residence r          WHERE r.CitizenId = c.CitizenId AND r.Status = 'ACTIVE'))
UNION ALL
-- C5: active residence whose canonical address is not ACTIVE (or lineage is broken)
SELECT 'RESIDENCE_ADDRESS_NOT_ACTIVE', r.CitizenId, NULL
FROM dbo.Residence r
LEFT JOIN dbo.vw_AddressCanonical rc ON rc.AddressId = r.AddressId
WHERE r.Status = 'ACTIVE'
  AND (rc.AddressId IS NULL OR rc.CanonicalStatus <> 'ACTIVE')
UNION ALL
-- H1: ACTIVE household without exactly one active head
SELECT 'HOUSEHOLD_ACTIVE_NO_HEAD', NULL, h.HouseholdId
FROM dbo.Household h
WHERE h.Status = 'ACTIVE'
  AND NOT EXISTS (SELECT 1 FROM dbo.HouseholdMembership m
                   WHERE m.HouseholdId = h.HouseholdId AND m.Status = 'ACTIVE' AND m.Role = 'HEAD')
UNION ALL
-- H2: ACTIVE household without any active member
SELECT 'HOUSEHOLD_ACTIVE_NO_MEMBERS', NULL, h.HouseholdId
FROM dbo.Household h
WHERE h.Status = 'ACTIVE'
  AND NOT EXISTS (SELECT 1 FROM dbo.HouseholdMembership m
                   WHERE m.HouseholdId = h.HouseholdId AND m.Status = 'ACTIVE')
UNION ALL
-- H3: ACTIVE household without an active address, or whose canonical address is not ACTIVE
SELECT 'HOUSEHOLD_ACTIVE_NO_VALID_ADDRESS', NULL, h.HouseholdId
FROM dbo.Household h
LEFT JOIN dbo.HouseholdAddress ha ON ha.HouseholdId = h.HouseholdId AND ha.Status = 'ACTIVE'
LEFT JOIN dbo.vw_AddressCanonical hc ON hc.AddressId = ha.AddressId
WHERE h.Status = 'ACTIVE'
  AND (ha.HouseholdAddressId IS NULL OR hc.AddressId IS NULL OR hc.CanonicalStatus <> 'ACTIVE')
UNION ALL
-- H4: INACTIVE household that still has active members
SELECT 'HOUSEHOLD_INACTIVE_HAS_MEMBERS', NULL, h.HouseholdId
FROM dbo.Household h
WHERE h.Status = 'INACTIVE'
  AND EXISTS (SELECT 1 FROM dbo.HouseholdMembership m
               WHERE m.HouseholdId = h.HouseholdId AND m.Status = 'ACTIVE');
GO


/* =====================================================================
   SECURITY  (D12) - role for the application's login
   Add the app's database user with:  ALTER ROLE aris_app ADD MEMBER [your_app_user];
   ===================================================================== */
IF DATABASE_PRINCIPAL_ID('aris_app') IS NULL
    CREATE ROLE aris_app;
GO
GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO aris_app;
DENY  DELETE ON SCHEMA::dbo TO aris_app;           -- history is never deleted
DENY  UPDATE ON dbo.AuditLog TO aris_app;          -- audit log is append-only (INSERT + SELECT only)
GO


/* =====================================================================
   SEED - system configuration defaults (SYS-01)
   The first ADMIN account must be created by the application (it knows how to hash passwords).
   ===================================================================== */
INSERT INTO dbo.SystemConfiguration (ConfigKey, ConfigValue, Description) VALUES
 ('PASSWORD_MIN_LENGTH',     N'8',  N'Minimum password length'),
 ('SESSION_TIMEOUT_MINUTES', N'15', N'Idle minutes before the session is closed'),
 ('MAX_FAILED_LOGINS',       N'5',  N'Failed logins before the account is locked'),
 ('LOCKOUT_MINUTES',         N'15', N'Lock duration after too many failed logins');
GO

/* Post-deploy sanity check: both queries must return 0 rows on a fresh database */
-- SELECT * FROM dbo.vw_GoldenInvariantViolations;
-- SELECT name FROM sys.triggers WHERE parent_class = 1 ORDER BY name;   -- expect 5 triggers
