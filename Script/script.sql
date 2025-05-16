-- =====================================================
-- SCRIPT COMPLET DE CRÉATION DE BASE DE DONNÉES
-- Système de Gestion Hospitalière - Architecture Complète
-- =====================================================

-- Création de la base de données
CREATE DATABASE HospitalManagementSystem
GO

USE HospitalManagementSystem
GO
FK_AuditLog_Users
-- =====================================================
-- SECTION 1: TABLES FONDAMENTALES
-- =====================================================

-- Table des centres hospitaliers
CREATE TABLE HospitalCenters (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL UNIQUE,
    Address NVARCHAR(500) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Email NVARCHAR(256) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_HospitalCenters_Name (Name),
    INDEX IX_HospitalCenters_IsActive (IsActive)
)
GO

-- Table des utilisateurs (SuperAdmin et Personnel Soignant)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PhoneNumber NVARCHAR(20) NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    LastLoginDate DATETIME2 NULL,
    MustChangePassword BIT NOT NULL DEFAULT 0,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_Users_Email (Email),
    INDEX IX_Users_IsActive (IsActive),
    INDEX IX_Users_LastName_FirstName (LastName, FirstName)
)
GO

-- Table des affectations utilisateur-centre
CREATE TABLE UserCenterAssignments (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NOT NULL,
    HospitalCenterId INT NOT NULL,
    RoleType NVARCHAR(50) NOT NULL, -- 'SuperAdmin' ou 'MedicalStaff'
    IsActive BIT NOT NULL DEFAULT 1,
    AssignmentStartDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AssignmentEndDate DATETIME2 NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_UserCenterAssignments_Users 
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserCenterAssignments_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id) ON DELETE CASCADE,
    CONSTRAINT CK_UserCenterAssignments_RoleType 
        CHECK (RoleType IN ('SuperAdmin', 'MedicalStaff')),
    
    INDEX IX_UserCenterAssignments_User_Active (UserId, IsActive),
    INDEX IX_UserCenterAssignments_Center_Active (HospitalCenterId, IsActive)
)
GO

-- Table des sessions utilisateur
CREATE TABLE UserSessions (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NOT NULL,
    CurrentHospitalCenterId INT NOT NULL,
    SessionToken NVARCHAR(256) NOT NULL UNIQUE,
    LoginTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LogoutTime DATETIME2 NULL,
    IpAddress NVARCHAR(45) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT FK_UserSessions_Users 
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserSessions_HospitalCenters 
        FOREIGN KEY (CurrentHospitalCenterId) REFERENCES HospitalCenters(Id),
    
    INDEX IX_UserSessions_Token (SessionToken),
    INDEX IX_UserSessions_User_Active (UserId, IsActive),
    INDEX IX_UserSessions_LoginTime (LoginTime)
)
GO

-- Table de mémorisation du dernier centre sélectionné
CREATE TABLE UserLastSelectedCenters (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NOT NULL,
    LastSelectedHospitalCenterId INT NOT NULL,
    LastSelectionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT UQ_UserLastSelectedCenters_User UNIQUE (UserId),
    CONSTRAINT FK_UserLastSelectedCenters_Users 
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserLastSelectedCenters_HospitalCenters 
        FOREIGN KEY (LastSelectedHospitalCenterId) REFERENCES HospitalCenters(Id),
    
    INDEX IX_UserLastSelectedCenters_User (UserId)
)
GO

-- =====================================================
-- SECTION 2: GESTION DES STOCKS
-- =====================================================

-- Table des catégories de produits
CREATE TABLE ProductCategories (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_ProductCategories_Name (Name)
)
GO

-- Table des produits (médicaments et consommables)
CREATE TABLE Products (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    ProductCategoryId INT NOT NULL,
    UnitOfMeasure NVARCHAR(50) NOT NULL, -- comprimé, ml, boîte, etc.
    SellingPrice DECIMAL(18,2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Products_ProductCategories 
        FOREIGN KEY (ProductCategoryId) REFERENCES ProductCategories(Id),
    
    INDEX IX_Products_Name (Name),
    INDEX IX_Products_Category (ProductCategoryId),
    INDEX IX_Products_IsActive (IsActive)
)
GO

-- Table des stocks par centre
CREATE TABLE StockInventory (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL,
    HospitalCenterId INT NOT NULL,
    CurrentQuantity DECIMAL(18,2) NOT NULL DEFAULT 0,
    MinimumThreshold DECIMAL(18,2) NULL,
    MaximumThreshold DECIMAL(18,2) NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_StockInventory_Products 
        FOREIGN KEY (ProductId) REFERENCES Products(Id),
    CONSTRAINT FK_StockInventory_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    
    -- Un produit ne peut avoir qu'un seul stock par centre
    CONSTRAINT UQ_StockInventory_Product_Center UNIQUE (ProductId, HospitalCenterId),
    
    INDEX IX_StockInventory_Product (ProductId),
    INDEX IX_StockInventory_Center (HospitalCenterId),
    INDEX IX_StockInventory_LowStock (CurrentQuantity, MinimumThreshold)
)
GO

-- Table des mouvements de stock
CREATE TABLE StockMovements (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL,
    HospitalCenterId INT NOT NULL,
    MovementType NVARCHAR(50) NOT NULL, -- 'Initial', 'Entry', 'Sale', 'Transfer', 'Adjustment', 'Care'
    Quantity DECIMAL(18,2) NOT NULL,
    ReferenceType NVARCHAR(50) NULL, -- 'Sale', 'Care', 'Transfer', etc.
    ReferenceId INT NULL, -- ID de la vente, soin, transfert, etc.
    Notes NVARCHAR(500) NULL,
    MovementDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_StockMovements_Products 
        FOREIGN KEY (ProductId) REFERENCES Products(Id),
    CONSTRAINT FK_StockMovements_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT CK_StockMovements_MovementType 
        CHECK (MovementType IN ('Initial', 'Entry', 'Sale', 'Transfer', 'Adjustment', 'Care')),
    
    INDEX IX_StockMovements_Product_Date (ProductId, MovementDate),
    INDEX IX_StockMovements_Center_Date (HospitalCenterId, MovementDate),
    INDEX IX_StockMovements_Type (MovementType)
)
GO

-- Table des transferts inter-centres
CREATE TABLE StockTransfers (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL,
    FromHospitalCenterId INT NOT NULL,
    ToHospitalCenterId INT NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    TransferReason NVARCHAR(500) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Approved', 'Completed', 'Cancelled'
    RequestDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ApprovedDate DATETIME2 NULL,
    CompletedDate DATETIME2 NULL,
    ApprovedBy INT NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_StockTransfers_Products 
        FOREIGN KEY (ProductId) REFERENCES Products(Id),
    CONSTRAINT FK_StockTransfers_FromCenter 
        FOREIGN KEY (FromHospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_StockTransfers_ToCenter 
        FOREIGN KEY (ToHospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_StockTransfers_ApprovedBy 
        FOREIGN KEY (ApprovedBy) REFERENCES Users(Id),
    CONSTRAINT CK_StockTransfers_Status 
        CHECK (Status IN ('Pending', 'Approved', 'Completed', 'Cancelled')),
    
    INDEX IX_StockTransfers_Status (Status),
    INDEX IX_StockTransfers_Product (ProductId),
    INDEX IX_StockTransfers_FromCenter (FromHospitalCenterId),
    INDEX IX_StockTransfers_ToCenter (ToHospitalCenterId)
)
GO

-- =====================================================
-- SECTION 3: GESTION DES PATIENTS
-- =====================================================

-- Table des patients
CREATE TABLE Patients (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    DateOfBirth DATE NULL,
    Gender NVARCHAR(10) NULL, -- 'Male', 'Female', 'Other'
    PhoneNumber NVARCHAR(20) NOT NULL, -- Obligatoire selon spec
    Email NVARCHAR(256) NULL, -- Optionnel selon spec
    Address NVARCHAR(500) NULL,
    EmergencyContactName NVARCHAR(200) NULL,
    EmergencyContactPhone NVARCHAR(20) NULL,
    BloodType NVARCHAR(10) NULL,
    Allergies NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_Patients_LastName_FirstName (LastName, FirstName),
    INDEX IX_Patients_PhoneNumber (PhoneNumber),
    INDEX IX_Patients_Email (Email),
    INDEX IX_Patients_IsActive (IsActive)
)
GO

-- Table des diagnostics
CREATE TABLE Diagnoses (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PatientId INT NOT NULL,
    HospitalCenterId INT NOT NULL,
    DiagnosedBy INT NOT NULL, -- Référence vers Users (Personnel Soignant)
    DiagnosisCode NVARCHAR(50) NULL, -- Code ICD-10 ou autre
    DiagnosisName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Severity NVARCHAR(50) NULL, -- 'Mild', 'Moderate', 'Severe', 'Critical'
    DiagnosisDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Diagnoses_Patients 
        FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_Diagnoses_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_Diagnoses_DiagnosedBy 
        FOREIGN KEY (DiagnosedBy) REFERENCES Users(Id),
    
    INDEX IX_Diagnoses_Patient (PatientId),
    INDEX IX_Diagnoses_Center (HospitalCenterId),
    INDEX IX_Diagnoses_DiagnosedBy (DiagnosedBy),
    INDEX IX_Diagnoses_Date (DiagnosisDate)
)
GO

-- =====================================================
-- SECTION 4: SOINS ET TRAITEMENTS
-- =====================================================

-- Table des types de soins
CREATE TABLE CareTypes (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    BasePrice DECIMAL(18,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_CareTypes_Name (Name)
)
GO

-- Table des épisodes de soins
CREATE TABLE CareEpisodes (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PatientId INT NOT NULL,
    DiagnosisId INT NOT NULL,
    HospitalCenterId INT NOT NULL,
    PrimaryCaregiver INT NOT NULL, -- Référence vers Users (Personnel Soignant principal)
    EpisodeStartDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EpisodeEndDate DATETIME2 NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Active', -- 'Active', 'Completed', 'Interrupted'
    InterruptionReason NVARCHAR(500) NULL,
    TotalCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    AmountPaid DECIMAL(18,2) NOT NULL DEFAULT 0,
    RemainingBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_CareEpisodes_Patients 
        FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_CareEpisodes_Diagnoses 
        FOREIGN KEY (DiagnosisId) REFERENCES Diagnoses(Id),
    CONSTRAINT FK_CareEpisodes_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_CareEpisodes_PrimaryCaregiver 
        FOREIGN KEY (PrimaryCaregiver) REFERENCES Users(Id),
    CONSTRAINT CK_CareEpisodes_Status 
        CHECK (Status IN ('Active', 'Completed', 'Interrupted')),
    
    INDEX IX_CareEpisodes_Patient (PatientId),
    INDEX IX_CareEpisodes_Diagnosis (DiagnosisId),
    INDEX IX_CareEpisodes_Center (HospitalCenterId),
    INDEX IX_CareEpisodes_Caregiver (PrimaryCaregiver),
    INDEX IX_CareEpisodes_Status (Status)
)
GO

-- Table des soins individuels
CREATE TABLE CareServices (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CareEpisodeId INT NOT NULL,
    CareTypeId INT NOT NULL,
    AdministeredBy INT NOT NULL, -- Référence vers Users (Personnel Soignant)
    ServiceDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Duration INT NULL, -- Durée en minutes
    Notes NVARCHAR(1000) NULL,
    Cost DECIMAL(18,2) NOT NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_CareServices_CareEpisodes 
        FOREIGN KEY (CareEpisodeId) REFERENCES CareEpisodes(Id),
    CONSTRAINT FK_CareServices_CareTypes 
        FOREIGN KEY (CareTypeId) REFERENCES CareTypes(Id),
    CONSTRAINT FK_CareServices_AdministeredBy 
        FOREIGN KEY (AdministeredBy) REFERENCES Users(Id),
    
    INDEX IX_CareServices_Episode (CareEpisodeId),
    INDEX IX_CareServices_Type (CareTypeId),
    INDEX IX_CareServices_AdministeredBy (AdministeredBy),
    INDEX IX_CareServices_Date (ServiceDate)
)
GO

-- Table des produits utilisés dans les soins
CREATE TABLE CareServiceProducts (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CareServiceId INT NOT NULL,
    ProductId INT NOT NULL,
    QuantityUsed DECIMAL(18,2) NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL,
    TotalCost DECIMAL(18,2) NOT NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_CareServiceProducts_CareServices 
        FOREIGN KEY (CareServiceId) REFERENCES CareServices(Id),
    CONSTRAINT FK_CareServiceProducts_Products 
        FOREIGN KEY (ProductId) REFERENCES Products(Id),
    
    INDEX IX_CareServiceProducts_Service (CareServiceId),
    INDEX IX_CareServiceProducts_Product (ProductId)
)
GO

-- =====================================================
-- SECTION 5: PRESCRIPTIONS
-- =====================================================

-- Table des prescriptions
CREATE TABLE Prescriptions (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PatientId INT NOT NULL,
    DiagnosisId INT NULL, -- Peut être lié à un diagnostic spécifique
    CareEpisodeId INT NULL, -- Peut être lié à un épisode de soin
    HospitalCenterId INT NOT NULL,
    PrescribedBy INT NOT NULL, -- Référence vers Users (Personnel Soignant)
    PrescriptionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Instructions NVARCHAR(1000) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Active', -- 'Active', 'Dispensed', 'Cancelled'
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Prescriptions_Patients 
        FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_Prescriptions_Diagnoses 
        FOREIGN KEY (DiagnosisId) REFERENCES Diagnoses(Id),
    CONSTRAINT FK_Prescriptions_CareEpisodes 
        FOREIGN KEY (CareEpisodeId) REFERENCES CareEpisodes(Id),
    CONSTRAINT FK_Prescriptions_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_Prescriptions_PrescribedBy 
        FOREIGN KEY (PrescribedBy) REFERENCES Users(Id),
    CONSTRAINT CK_Prescriptions_Status 
        CHECK (Status IN ('Active', 'Dispensed', 'Cancelled')),
    
    INDEX IX_Prescriptions_Patient (PatientId),
    INDEX IX_Prescriptions_Diagnosis (DiagnosisId),
    INDEX IX_Prescriptions_Episode (CareEpisodeId),
    INDEX IX_Prescriptions_Center (HospitalCenterId),
    INDEX IX_Prescriptions_PrescribedBy (PrescribedBy),
    INDEX IX_Prescriptions_Status (Status)
)
GO

-- Table des détails de prescription (produits prescrits)
CREATE TABLE PrescriptionItems (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PrescriptionId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    Dosage NVARCHAR(100) NULL,
    Frequency NVARCHAR(100) NULL, -- ex: "3 fois par jour"
    Duration NVARCHAR(100) NULL, -- ex: "7 jours"
    Instructions NVARCHAR(500) NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_PrescriptionItems_Prescriptions 
        FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id),
    CONSTRAINT FK_PrescriptionItems_Products 
        FOREIGN KEY (ProductId) REFERENCES Products(Id),
    
    INDEX IX_PrescriptionItems_Prescription (PrescriptionId),
    INDEX IX_PrescriptionItems_Product (ProductId)
)
GO

-- =====================================================
-- SECTION 6: EXAMENS MÉDICAUX
-- =====================================================

-- Table des types d'examens
CREATE TABLE ExaminationTypes (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(100) NULL, -- ex: "Radiologie", "Laboratoire", etc.
    BasePrice DECIMAL(18,2) NOT NULL,
    SubcontractorPrice DECIMAL(18,2) NULL, -- Prix de sous-traitance
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_ExaminationTypes_Name (Name),
    INDEX IX_ExaminationTypes_Category (Category)
)
GO

-- Table des examens demandés/réalisés
CREATE TABLE Examinations (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PatientId INT NOT NULL,
    ExaminationTypeId INT NOT NULL,
    CareEpisodeId INT NULL, -- Peut être lié à un épisode de soin
    HospitalCenterId INT NOT NULL,
    RequestedBy INT NOT NULL, -- Référence vers Users (Personnel Soignant)
    PerformedBy INT NULL, -- Référence vers Users (Personnel Soignant qui a réalisé)
    RequestDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ScheduledDate DATETIME2 NULL,
    PerformedDate DATETIME2 NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Requested', -- 'Requested', 'Scheduled', 'InProgress', 'Completed', 'Cancelled'
    FinalPrice DECIMAL(18,2) NOT NULL, -- Prix final (peut inclure ristournes)
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Notes NVARCHAR(1000) NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Examinations_Patients 
        FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_Examinations_ExaminationTypes 
        FOREIGN KEY (ExaminationTypeId) REFERENCES ExaminationTypes(Id),
    CONSTRAINT FK_Examinations_CareEpisodes 
        FOREIGN KEY (CareEpisodeId) REFERENCES CareEpisodes(Id),
    CONSTRAINT FK_Examinations_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_Examinations_RequestedBy 
        FOREIGN KEY (RequestedBy) REFERENCES Users(Id),
    CONSTRAINT FK_Examinations_PerformedBy 
        FOREIGN KEY (PerformedBy) REFERENCES Users(Id),
    CONSTRAINT CK_Examinations_Status 
        CHECK (Status IN ('Requested', 'Scheduled', 'InProgress', 'Completed', 'Cancelled')),
    
    INDEX IX_Examinations_Patient (PatientId),
    INDEX IX_Examinations_Type (ExaminationTypeId),
    INDEX IX_Examinations_Episode (CareEpisodeId),
    INDEX IX_Examinations_Center (HospitalCenterId),
    INDEX IX_Examinations_RequestedBy (RequestedBy),
    INDEX IX_Examinations_Status (Status),
    INDEX IX_Examinations_ScheduledDate (ScheduledDate)
)
GO

-- Table des résultats d'examens
CREATE TABLE ExaminationResults (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ExaminationId INT NOT NULL,
    ResultData NVARCHAR(MAX) NULL, -- Données structurées des résultats
    ResultNotes NVARCHAR(2000) NULL,
    AttachmentPath NVARCHAR(500) NULL, -- Chemin vers fichiers joints (images, PDF, etc.)
    ReportedBy INT NOT NULL, -- Référence vers Users
    ReportDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_ExaminationResults_Examinations 
        FOREIGN KEY (ExaminationId) REFERENCES Examinations(Id),
    CONSTRAINT FK_ExaminationResults_ReportedBy 
        FOREIGN KEY (ReportedBy) REFERENCES Users(Id),
    
    -- Un examen ne peut avoir qu'un seul rapport final
    CONSTRAINT UQ_ExaminationResults_Examination UNIQUE (ExaminationId),
    
    INDEX IX_ExaminationResults_Examination (ExaminationId),
    INDEX IX_ExaminationResults_ReportedBy (ReportedBy)
)
GO

-- =====================================================
-- SECTION 7: VENTES ET FACTURATION
-- =====================================================

-- Table des ventes/factures
CREATE TABLE Sales (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SaleNumber NVARCHAR(50) NOT NULL UNIQUE, -- Numéro de facture généré
    PatientId INT NULL, -- Peut être NULL pour vente anonyme
    HospitalCenterId INT NOT NULL,
    SoldBy INT NOT NULL, -- Référence vers Users (Personnel Soignant)
    SaleDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    FinalAmount DECIMAL(18,2) NOT NULL,
    PaymentStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Partial', 'Paid'
    Notes NVARCHAR(500) NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Sales_Patients 
        FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_Sales_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_Sales_SoldBy 
        FOREIGN KEY (SoldBy) REFERENCES Users(Id),
    CONSTRAINT CK_Sales_PaymentStatus 
        CHECK (PaymentStatus IN ('Pending', 'Partial', 'Paid')),
    
    INDEX IX_Sales_SaleNumber (SaleNumber),
    INDEX IX_Sales_Patient (PatientId),
    INDEX IX_Sales_Center (HospitalCenterId),
    INDEX IX_Sales_SoldBy (SoldBy),
    INDEX IX_Sales_Date (SaleDate),
    INDEX IX_Sales_PaymentStatus (PaymentStatus)
)
GO

-- Table des détails de vente
CREATE TABLE SaleItems (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SaleId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_SaleItems_Sales 
        FOREIGN KEY (SaleId) REFERENCES Sales(Id),
    CONSTRAINT FK_SaleItems_Products 
        FOREIGN KEY (ProductId) REFERENCES Products(Id),
    
    INDEX IX_SaleItems_Sale (SaleId),
    INDEX IX_SaleItems_Product (ProductId)
)
GO

-- =====================================================
-- SECTION 8: PAIEMENTS ET ENCAISSEMENTS
-- =====================================================

-- Table des méthodes de paiement
CREATE TABLE PaymentMethods (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL, -- 'Cash', 'OrangeMoney', 'MTNMoney', etc.
    IsActive BIT NOT NULL DEFAULT 1,
    RequiresBankAccount BIT NOT NULL DEFAULT 0,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_PaymentMethods_Name (Name)
)
GO

-- Table des paiements
CREATE TABLE Payments (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReferenceType NVARCHAR(50) NOT NULL, -- 'Sale', 'CareEpisode', 'Examination'
    ReferenceId INT NOT NULL, -- ID de la vente, épisode de soin, ou examen
    PatientId INT NULL,
    HospitalCenterId INT NOT NULL,
    PaymentMethodId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ReceivedBy INT NOT NULL, -- Référence vers Users (Personnel Soignant)
    TransactionReference NVARCHAR(100) NULL, -- Référence de transaction mobile money
    Notes NVARCHAR(500) NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Payments_Patients 
        FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_Payments_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_Payments_PaymentMethods 
        FOREIGN KEY (PaymentMethodId) REFERENCES PaymentMethods(Id),
    CONSTRAINT FK_Payments_ReceivedBy 
        FOREIGN KEY (ReceivedBy) REFERENCES Users(Id),
    CONSTRAINT CK_Payments_ReferenceType 
        CHECK (ReferenceType IN ('Sale', 'CareEpisode', 'Examination')),
    
    INDEX IX_Payments_Reference (ReferenceType, ReferenceId),
    INDEX IX_Payments_Patient (PatientId),
    INDEX IX_Payments_Center (HospitalCenterId),
    INDEX IX_Payments_Method (PaymentMethodId),
    INDEX IX_Payments_ReceivedBy (ReceivedBy),
    INDEX IX_Payments_Date (PaymentDate)
)
GO

-- Table des financiers par centre
CREATE TABLE Financiers (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    HospitalCenterId INT NOT NULL,
    ContactInfo NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Financiers_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    
    INDEX IX_Financiers_Center (HospitalCenterId),
    INDEX IX_Financiers_Name (Name)
)
GO

-- Table des remises aux financiers
CREATE TABLE CashHandovers (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    HospitalCenterId INT NOT NULL,
    FinancierId INT NOT NULL,
    HandoverDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    TotalCashAmount DECIMAL(18,2) NOT NULL,
    HandoverAmount DECIMAL(18,2) NOT NULL,
    RemainingCashAmount DECIMAL(18,2) NOT NULL,
    HandedOverBy INT NOT NULL, -- Référence vers Users (Personnel Soignant)
    Notes NVARCHAR(500) NULL,
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    CONSTRAINT FK_CashHandovers_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    CONSTRAINT FK_CashHandovers_Financiers 
        FOREIGN KEY (FinancierId) REFERENCES Financiers(Id),
    CONSTRAINT FK_CashHandovers_HandedOverBy 
        FOREIGN KEY (HandedOverBy) REFERENCES Users(Id),
    
    INDEX IX_CashHandovers_Center (HospitalCenterId),
    INDEX IX_CashHandovers_Financier (FinancierId),
    INDEX IX_CashHandovers_Date (HandoverDate),
    INDEX IX_CashHandovers_HandedOverBy (HandedOverBy)
)
GO

-- =====================================================
-- SECTION 9: AUDIT ET TRAÇABILITÉ
-- =====================================================

-- Table d'audit général
CREATE TABLE AuditLog (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NULL,
    ActionType NVARCHAR(100) NOT NULL,
    EntityType NVARCHAR(100) NOT NULL,
    EntityId INT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    Description NVARCHAR(500) NULL,
    IpAddress NVARCHAR(45) NULL,
    HospitalCenterId INT NULL,
    ActionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_AuditLog_Users 
        FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_AuditLog_HospitalCenters 
        FOREIGN KEY (HospitalCenterId) REFERENCES HospitalCenters(Id),
    
    INDEX IX_AuditLog_ActionDate (ActionDate),
    INDEX IX_AuditLog_User_ActionDate (UserId, ActionDate),
    INDEX IX_AuditLog_EntityType_EntityId (EntityType, EntityId),
    INDEX IX_AuditLog_Center (HospitalCenterId)
)
GO

-- =====================================================
-- SECTION 10: TABLES DE RAPPORTS (REMPLACENT LES VUES)
-- =====================================================

-- Table de rapport: Détails utilisateur-centre
CREATE TABLE rpt_UserCenterDetails (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    UserIsActive BIT NOT NULL,
    LastLoginDate DATETIME2 NULL,
    AssignmentId INT NULL,
    RoleType NVARCHAR(50) NULL,
    AssignmentIsActive BIT NULL,
    HospitalCenterId INT NULL,
    HospitalCenterName NVARCHAR(200) NULL,
    AssignmentStartDate DATETIME2 NULL,
    AssignmentEndDate DATETIME2 NULL,
    ReportGeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_rpt_UserCenterDetails_User (UserId),
    INDEX IX_rpt_UserCenterDetails_Center (HospitalCenterId),
    INDEX IX_rpt_UserCenterDetails_Generated (ReportGeneratedAt)
)
GO

-- Table de rapport: Sessions actives
CREATE TABLE rpt_ActiveSessions (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SessionId INT NOT NULL,
    UserId INT NOT NULL,
    UserName NVARCHAR(201) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    CurrentHospitalCenter NVARCHAR(200) NOT NULL,
    LoginTime DATETIME2 NOT NULL,
    IpAddress NVARCHAR(45) NULL,
    HoursConnected INT NOT NULL,
    ReportGeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_rpt_ActiveSessions_User (UserId),
    INDEX IX_rpt_ActiveSessions_Session (SessionId),
    INDEX IX_rpt_ActiveSessions_Generated (ReportGeneratedAt)
)
GO

-- Table de rapport: État des stocks
CREATE TABLE rpt_StockStatus (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    ProductCategory NVARCHAR(200) NOT NULL,
    HospitalCenterId INT NOT NULL,
    HospitalCenterName NVARCHAR(200) NOT NULL,
    CurrentQuantity DECIMAL(18,2) NOT NULL,
    MinimumThreshold DECIMAL(18,2) NULL,
    MaximumThreshold DECIMAL(18,2) NULL,
    StockStatus NVARCHAR(50) NOT NULL, -- 'Normal', 'Low', 'Critical', 'Overstock'
    LastMovementDate DATETIME2 NULL,
    ReportGeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_rpt_StockStatus_Product (ProductId),
    INDEX IX_rpt_StockStatus_Center (HospitalCenterId),
    INDEX IX_rpt_StockStatus_Status (StockStatus),
    INDEX IX_rpt_StockStatus_Generated (ReportGeneratedAt)
)
GO

-- Table de rapport: Activité financière
CREATE TABLE rpt_FinancialActivity (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    HospitalCenterId INT NOT NULL,
    HospitalCenterName NVARCHAR(200) NOT NULL,
    ReportDate DATE NOT NULL,
    TotalSales DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalCareRevenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalExaminationRevenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalRevenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalCashPayments DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalMobilePayments DECIMAL(18,2) NOT NULL DEFAULT 0,
    TransactionCount INT NOT NULL DEFAULT 0,
    PatientCount INT NOT NULL DEFAULT 0,
    ReportGeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_rpt_FinancialActivity_Center_Date (HospitalCenterId, ReportDate),
    INDEX IX_rpt_FinancialActivity_Date (ReportDate),
    INDEX IX_rpt_FinancialActivity_Generated (ReportGeneratedAt)
)
GO

-- Table de rapport: Performance des soignants
CREATE TABLE rpt_CaregiverPerformance (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NOT NULL,
    CaregiverName NVARCHAR(201) NOT NULL,
    HospitalCenterId INT NOT NULL,
    HospitalCenterName NVARCHAR(200) NOT NULL,
    ReportDate DATE NOT NULL,
    PatientsServed INT NOT NULL DEFAULT 0,
    CareServicesProvided INT NOT NULL DEFAULT 0,
    ExaminationsRequested INT NOT NULL DEFAULT 0,
    PrescriptionsIssued INT NOT NULL DEFAULT 0,
    SalesMade INT NOT NULL DEFAULT 0,
    TotalRevenueGenerated DECIMAL(18,2) NOT NULL DEFAULT 0,
    ReportGeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Champs d'audit
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedAt DATETIME2 NULL,
    
    INDEX IX_rpt_CaregiverPerformance_User_Date (UserId, ReportDate),
    INDEX IX_rpt_CaregiverPerformance_Center_Date (HospitalCenterId, ReportDate),
    INDEX IX_rpt_CaregiverPerformance_Generated (ReportGeneratedAt)
)
GO


-- Table des logs applicatifs consultables depuis l'interface
CREATE TABLE ApplicationLogs (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId INT NULL, -- Utilisateur concerné (NULL pour logs système)
    HospitalCenterId INT NULL, -- Centre concerné
    LogLevel NVARCHAR(20) NOT NULL, -- 'Info', 'Warning', 'Error', 'Critical'
    Category NVARCHAR(100) NOT NULL, -- 'Authentication', 'Stock', 'Sales', etc.
    Action NVARCHAR(100) NOT NULL, -- 'Login', 'StockUpdate', 'SaleCreated', etc.
    Message NVARCHAR(1000) NOT NULL, -- Message descriptif
    Details NVARCHAR(MAX) NULL, -- Détails JSON si nécessaire
    EntityType NVARCHAR(100) NULL, -- Type d'entité concernée
    EntityId INT NULL, -- ID de l'entité concernée
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    RequestPath NVARCHAR(500) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Index pour performances
    INDEX IX_ApplicationLogs_Timestamp (Timestamp),
    INDEX IX_ApplicationLogs_UserId_Timestamp (UserId, Timestamp),
    INDEX IX_ApplicationLogs_Category_Action (Category, Action),
    INDEX IX_ApplicationLogs_Level (LogLevel),
    INDEX IX_ApplicationLogs_Center (HospitalCenterId)
)
GO

-- Table des erreurs système détaillées
CREATE TABLE SystemErrorLogs (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ErrorId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(), -- ID unique pour traçage
    UserId INT NULL,
    HospitalCenterId INT NULL,
    Severity NVARCHAR(20) NOT NULL, -- 'Low', 'Medium', 'High', 'Critical'
    Source NVARCHAR(200) NOT NULL, -- Nom de la classe/méthode
    ErrorType NVARCHAR(200) NOT NULL, -- Type d'exception
    Message NVARCHAR(1000) NOT NULL,
    StackTrace NVARCHAR(MAX) NULL,
    InnerException NVARCHAR(MAX) NULL,
    RequestData NVARCHAR(MAX) NULL, -- Données de la requête en JSON
    UserAgent NVARCHAR(500) NULL,
    IpAddress NVARCHAR(45) NULL,
    RequestPath NVARCHAR(500) NULL,
    IsResolved BIT NOT NULL DEFAULT 0,
    ResolvedBy INT NULL,
    ResolvedAt DATETIME2 NULL,
    ResolutionNotes NVARCHAR(1000) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Index pour performances et requêtes fréquentes
    INDEX IX_SystemErrorLogs_Timestamp (Timestamp),
    INDEX IX_SystemErrorLogs_IsResolved (IsResolved),
    INDEX IX_SystemErrorLogs_Severity (Severity),
    INDEX IX_SystemErrorLogs_Source (Source)
)
GO

-- =====================================================
-- DONNÉES D'INITIALISATION
-- =====================================================

-- Insertion d'un utilisateur système
INSERT INTO Users (FirstName, LastName, Email, PhoneNumber, PasswordHash, CreatedBy)
VALUES ('System', 'Account', 'system@hospital.local', '0000000000', 'SYSTEM_ACCOUNT', 1)
GO

-- Création d'un SuperAdmin initial
INSERT INTO Users (FirstName, LastName, Email, PhoneNumber, PasswordHash, MustChangePassword, CreatedBy)
VALUES ('Super', 'Administrator', 'admin@hospital.local', '0000000001', 
        '$2a$11$example.hash.for.AdminTemp123!', 1, 1)
GO

-- Création de centres d'exemple
INSERT INTO HospitalCenters (Name, Address, PhoneNumber, Email, CreatedBy)
VALUES 
    ('Centre Principal', '123 Rue de la Santé, Douala', '+237600000000', 'principal@hospital.local', 2),
    ('Centre Nord', '456 Avenue du Nord, Douala', '+237600000001', 'nord@hospital.local', 2),
    ('Centre Sud', '789 Boulevard du Sud, Douala', '+237600000002', 'sud@hospital.local', 2)
GO

-- Affectation du SuperAdmin aux centres
INSERT INTO UserCenterAssignments (UserId, HospitalCenterId, RoleType, CreatedBy)
VALUES 
    (2, 1, 'SuperAdmin', 1),
    (2, 2, 'SuperAdmin', 1),
    (2, 3, 'SuperAdmin', 1)
GO

-- Création des catégories de produits de base
INSERT INTO ProductCategories (Name, Description, CreatedBy)
VALUES 
    ('Médicaments', 'Tous les médicaments et traitements', 2),
    ('Consommables', 'Matériel médical à usage unique', 2),
    ('Équipements', 'Équipements médicaux réutilisables', 2)
GO

-- Création des méthodes de paiement de base
INSERT INTO PaymentMethods (Name, RequiresBankAccount, CreatedBy)
VALUES 
    ('Espèces', 0, 2),
    ('Orange Money', 1, 2),
    ('MTN Money', 1, 2),
    ('Carte Bancaire', 1, 2)
GO

-- Création des types de soins de base
INSERT INTO CareTypes (Name, Description, BasePrice, CreatedBy)
VALUES 
    ('Consultation Générale', 'Consultation médicale générale', 5000.00, 2),
    ('Injection', 'Administration d injection', 1000.00, 2),
    ('Pansement', 'Soins de pansement', 500.00, 2),
    ('Perfusion', 'Administration de perfusion', 2000.00, 2)
GO

-- Création des types d'examens de base
INSERT INTO ExaminationTypes (Name, Description, Category, BasePrice, CreatedBy)
VALUES 
    ('Analyse de Sang', 'Analyse sanguine complète', 'Laboratoire', 3000.00, 2),
    ('Radiographie Thoracique', 'Radio du thorax', 'Radiologie', 8000.00, 2),
    ('Électrocardiogramme', 'ECG', 'Cardiologie', 5000.00, 2),
    ('Échographie Abdominale', 'Échographie de l abdomen', 'Radiologie', 12000.00, 2)
GO

-- =====================================================
-- PROCÉDURES STOCKÉES UTILES
-- =====================================================

-- Procédure pour nettoyer les sessions expirées
CREATE PROCEDURE sp_CleanExpiredSessions
AS
BEGIN
    UPDATE UserSessions 
    SET IsActive = 0, 
        LogoutTime = GETUTCDATE()
    WHERE IsActive = 1 
        AND LogoutTime IS NULL
        AND DATEDIFF(HOUR, LoginTime, GETUTCDATE()) >= 12
END
GO

-- Procédure pour mettre à jour les statistiques de stock
CREATE PROCEDURE sp_UpdateStockReports
AS
BEGIN
    -- Vider la table de rapport existante
    TRUNCATE TABLE rpt_StockStatus
    
    -- Régénérer les données de rapport
    INSERT INTO rpt_StockStatus (
        ProductId, ProductName, ProductCategory, HospitalCenterId, HospitalCenterName,
        CurrentQuantity, MinimumThreshold, MaximumThreshold, StockStatus, 
        LastMovementDate, ReportGeneratedAt, CreatedBy
    )
    SELECT 
        p.Id AS ProductId,
        p.Name AS ProductName,
        pc.Name AS ProductCategory,
        hc.Id AS HospitalCenterId,
        hc.Name AS HospitalCenterName,
        si.CurrentQuantity,
        si.MinimumThreshold,
        si.MaximumThreshold,
        CASE 
            WHEN si.CurrentQuantity <= ISNULL(si.MinimumThreshold, 0) THEN 'Critical'
            WHEN si.CurrentQuantity <= ISNULL(si.MinimumThreshold, 0) * 1.5 THEN 'Low'
            WHEN si.CurrentQuantity >= ISNULL(si.MaximumThreshold, 999999) THEN 'Overstock'
            ELSE 'Normal'
        END AS StockStatus,
        (SELECT MAX(MovementDate) FROM StockMovements WHERE ProductId = p.Id AND HospitalCenterId = hc.Id) AS LastMovementDate,
        GETUTCDATE() AS ReportGeneratedAt,
        1 AS CreatedBy
    FROM Products p
    INNER JOIN ProductCategories pc ON p.ProductCategoryId = pc.Id
    CROSS JOIN HospitalCenters hc
    LEFT JOIN StockInventory si ON p.Id = si.ProductId AND hc.Id = si.HospitalCenterId
    WHERE p.IsActive = 1 AND hc.IsActive = 1
END
GO

-- =====================================================
-- COMMENTAIRES FINAUX
-- =====================================================

/*
Ce script établit une architecture complète pour le système hospitalier avec :

1. GESTION DES UTILISATEURS : Authentification flexible, multi-centres, gestion des rôles
2. STOCKS : Gestion complète avec mouvements, transferts, seuils d'alerte
3. PATIENTS : Dossiers complets avec diagnostics et historique
4. SOINS : Épisodes de soins avec tracking des produits utilisés
5. PRESCRIPTIONS : Gestion complète avec détails des médicaments
6. EXAMENS : Types d'examens, planification, résultats
7. VENTES : Facturation avec gestion des paiements multiples
8. FINANCES : Encaissements, remises aux financiers
9. AUDIT : Traçabilité complète de toutes les actions
10. RAPPORTS : Tables matérialisées pour tableaux de bord

Toutes les tables ont des champs d'audit complets.
L'architecture supporte la croissance future.
Les index optimisent les performances des requêtes fréquentes.
Les contraintes maintiennent l'intégrité référentielle.
*/