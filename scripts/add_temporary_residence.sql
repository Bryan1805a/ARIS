/* =====================================================================
   ARIS - Administrative Residence Information System
   Migration Script: Add TemporaryResidence Table (Quản lý Đăng ký Tạm trú)
   ===================================================================== */

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TemporaryResidence' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TemporaryResidence (
        TempResId       int           IDENTITY(1,1) NOT NULL,
        CitizenId       int           NOT NULL,
        AddressId       int           NOT NULL,
        StartDate       date          NOT NULL,
        EndDate         date          NOT NULL,
        StayReason      nvarchar(200) NOT NULL,
        HostInfo        nvarchar(200) NULL,
        Status          varchar(10)   NOT NULL CONSTRAINT DF_TempRes_Status DEFAULT 'ACTIVE',
        Notes           nvarchar(500) NULL,
        CreatedBy       int           NOT NULL,
        CreatedAt       datetime2(3)  NOT NULL CONSTRAINT DF_TempRes_CreatedAt DEFAULT SYSUTCDATETIME(),
        EndedBy         int           NULL,
        EndedAt         datetime2(3)  NULL,
        RowVersion      rowversion    NOT NULL,
        CONSTRAINT PK_TemporaryResidence PRIMARY KEY (TempResId),
        CONSTRAINT FK_TempRes_Citizen FOREIGN KEY (CitizenId) REFERENCES dbo.Citizen (CitizenId),
        CONSTRAINT FK_TempRes_Address FOREIGN KEY (AddressId) REFERENCES dbo.Address (AddressId),
        CONSTRAINT FK_TempRes_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.UserAccount (UserId),
        CONSTRAINT FK_TempRes_EndedBy FOREIGN KEY (EndedBy) REFERENCES dbo.UserAccount (UserId),
        CONSTRAINT CK_TempRes_Status CHECK (Status IN ('ACTIVE','EXPIRED','CANCELLED')),
        CONSTRAINT CK_TempRes_Dates CHECK (EndDate >= StartDate)
    );

    CREATE INDEX IX_TempRes_Citizen ON dbo.TemporaryResidence (CitizenId, Status);
    CREATE INDEX IX_TempRes_Address ON dbo.TemporaryResidence (AddressId, Status);

    PRINT 'Table dbo.TemporaryResidence created successfully.';
END
ELSE
BEGIN
    PRINT 'Table dbo.TemporaryResidence already exists.';
END
GO

