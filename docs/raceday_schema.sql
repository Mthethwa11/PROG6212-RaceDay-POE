/* =========================================================
   RaceDay Database Schema
   PROG6212 POE - Part 1, Section C
   Run in SQL Server Management Studio (SSMS) against a clean
   SQL Server Express instance.
   ========================================================= */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* Drop tables if they exist, in FK-safe order, so this script
   can be re-run cleanly on a database that already has them. */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Routes', 'U') IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* =========================================================
   TABLE: Users
   Holds both roles ('Organiser' and 'Participant').
   ========================================================= */
   /* Design note: Users is a single table for both roles (Organiser and
      Participant), distinguished by the Role column and enforced with a
      CHECK constraint. This avoids duplicating shared fields (name, email,
      password) across two separate tables. */
CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1)   NOT NULL,
    FullName        VARCHAR(100)        NOT NULL,
    Email           VARCHAR(150)        NOT NULL,
    PasswordHash    VARCHAR(255)        NOT NULL,
    Role            VARCHAR(20)         NOT NULL,
    CreatedAt       DATETIME            NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

/* =========================================================
   TABLE: Events
   Created and owned by a User with Role = 'Organiser'.
   ========================================================= */
CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1)   NOT NULL,
    OrganiserId     INT                 NOT NULL,
    Name            VARCHAR(150)        NOT NULL,
    Description     VARCHAR(MAX)        NULL,
    EventDate       DATE                NOT NULL,
    Location        VARCHAR(150)        NOT NULL,
    CreatedAt       DATETIME            NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId)
);
GO

/* =========================================================
   TABLE: Categories
   Each Event has one or more race Categories (e.g. 10km Run).
   ========================================================= */
CREATE TABLE dbo.Categories (
    CategoryId      INT IDENTITY(1,1)   NOT NULL,
    EventId         INT                 NOT NULL,
    Name            VARCHAR(100)        NOT NULL,
    DistanceKm      DECIMAL(5,2)        NOT NULL,
    Price           DECIMAL(8,2)        NOT NULL DEFAULT 0,
    MaxParticipants INT                 NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId)
);
GO

/* =========================================================
   TABLE: Routes
   Exactly one Route per Category (1..1) - carries the
   route/elevation info participants use to prepare for race day.
   ========================================================= */
CREATE TABLE dbo.Routes (
    RouteId         INT IDENTITY(1,1)   NOT NULL,
    CategoryId      INT                 NOT NULL,
    RouteName       VARCHAR(100)        NOT NULL,
    MapUrl          VARCHAR(255)        NULL,
    ElevationGainM  INT                 NULL,
    Description     VARCHAR(MAX)        NULL,
    CONSTRAINT PK_Routes PRIMARY KEY (RouteId),
    CONSTRAINT FK_Routes_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT UQ_Routes_CategoryId UNIQUE (CategoryId)
);
GO

/* =========================================================
   TABLE: Enrolments
   Junction table resolving the Users(Participant) <-> Categories
   many-to-many relationship.
   ========================================================= */
CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT IDENTITY(1,1)   NOT NULL,
    ParticipantId   INT                 NOT NULL,
    CategoryId      INT                 NOT NULL,
    EnrolmentDate   DATETIME            NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20)         NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Cancelled'))
);
GO

/* =========================================================
   TABLE: Results
   Exactly one Result per Enrolment (1..1).
   ========================================================= */
CREATE TABLE dbo.Results (
    ResultId            INT IDENTITY(1,1)  NOT NULL,
    EnrolmentId         INT                 NOT NULL,
    FinishTimeSeconds   INT                 NULL,
    Position            INT                 NULL,
    RecordedAt          DATETIME            NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId),
    CONSTRAINT UQ_Results_EnrolmentId UNIQUE (EnrolmentId)
);
GO

/* =========================================================
   SEED DATA
   2 Organisers, 2 Participants, 3 Events, categories per
   event, routes per category, sample enrolments and results.
   ========================================================= */

-- Users: 2 Organisers, 2 Participants
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role) VALUES
('Thabo Nkosi',     'thabo.nkosi@raceday.co.za',     'HASHED_PW_1', 'Organiser'),
('Lindiwe Zulu',     'lindiwe.zulu@raceday.co.za',     'HASHED_PW_2', 'Organiser'),
('Sipho Dlamini',    'sipho.dlamini@example.com',      'HASHED_PW_3', 'Participant'),
('Aisha Patel',      'aisha.patel@example.com',        'HASHED_PW_4', 'Participant');
GO

-- Events: 3 events, split across the 2 organisers
INSERT INTO dbo.Events (OrganiserId, Name, Description, EventDate, Location) VALUES
(1, 'Durban Beachfront Fun Run', 'A scenic morning run along the Durban promenade.', '2026-10-18', 'Durban, KwaZulu-Natal'),
(1, 'Comrades Community Cycle Tour', 'A charity cycling event supporting local road running clubs.', '2026-11-08', 'Pietermaritzburg, KwaZulu-Natal'),
(2, 'Cape Peninsula Trail Walk', 'A guided walking event along the Cape Peninsula coastline.', '2026-09-27', 'Cape Town, Western Cape'),
(2, 'Johannesburg City Park Run', 'A weekly community park run in the heart of Johannesburg.', '2026-09-20', 'Johannesburg, Gauteng');
GO

-- Categories: at least one per event
INSERT INTO dbo.Categories (EventId, Name, DistanceKm, Price, MaxParticipants) VALUES
(1, '5km Fun Run', 5.00, 100.00, 200),
(1, '10km Race', 10.00, 150.00, 150),
(2, '40km Cycle Tour', 40.00, 250.00, 100),
(3, '8km Guided Walk', 8.00, 80.00, 80),
(4, '5km Park Run', 5.00, 0.00, 300);
GO

-- Routes: one per category (1..1)
INSERT INTO dbo.Routes (CategoryId, RouteName, MapUrl, ElevationGainM, Description) VALUES
(1, 'Beachfront Loop', 'https://maps.example.com/beachfront-5km', 20, 'Flat, out-and-back route along the promenade.'),
(2, 'Beachfront Extended', 'https://maps.example.com/beachfront-10km', 45, 'Extended promenade route with one hill section.'),
(3, 'Comrades Access Road', 'https://maps.example.com/comrades-cycle-40km', 320, 'Rolling hills typical of the Comrades route.'),
(4, 'Cape Point Coastal Path', 'https://maps.example.com/cape-point-8km', 150, 'Coastal path with moderate elevation gain.');
GO

-- Enrolments: sample enrolments for both participants
INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status) VALUES
(3, 1, 'Confirmed'),
(3, 3, 'Confirmed'),
(4, 2, 'Confirmed'),
(4, 4, 'Confirmed');
GO

-- Results: sample results for two enrolments
INSERT INTO dbo.Results (EnrolmentId, FinishTimeSeconds, Position) VALUES
(1, 1620, 12),
(3, 3120, 5);
GO

/* =========================================================
   INDEXES
   Speed up lookups on foreign key columns that will be
   queried frequently by the API (Part 2).
   ========================================================= */
CREATE INDEX IX_Events_OrganiserId ON dbo.Events (OrganiserId);
CREATE INDEX IX_Categories_EventId ON dbo.Categories (EventId);
CREATE INDEX IX_Enrolments_ParticipantId ON dbo.Enrolments (ParticipantId);
CREATE INDEX IX_Enrolments_CategoryId ON dbo.Enrolments (CategoryId);
GO