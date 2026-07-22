-- =============================================================================
-- iHomeDB schema script
-- Creates the SQL Server database used by the iHome WPF application (PRN212).
-- Run on a clean server: drops iHomeDB if it exists, recreates schema from scratch.
-- Entity order respects foreign-key dependencies (parents before children).
-- =============================================================================

-- Switch to master so we can drop/create the application database
USE master;
GO

-- Drop existing iHomeDB when present so this script can rebuild a clean schema
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'iHomeDB')
BEGIN
    -- Force disconnect all sessions before drop to avoid "database in use" errors
    ALTER DATABASE iHomeDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE iHomeDB;
END
GO

-- Create a fresh empty database for the rental-management application
CREATE DATABASE iHomeDB;
GO

-- All table DDL below runs inside the application database
USE iHomeDB;
GO

-- ============================================================
-- 1. Security & User Management
-- Stores landlord and manager login accounts, lockout state,
-- and optional ManagedPropertyId (FK added after Properties exists).
-- ============================================================

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Username VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(256) NOT NULL, -- BCrypt password hash
    Email VARCHAR(150) NOT NULL,
    PhoneNumber VARCHAR(20) NULL,
    Role NVARCHAR(30) NOT NULL, -- Chủ trọ / Quản lý
    IsActive BIT NOT NULL DEFAULT 1, -- Account active flag
    LoginAttempts INT NOT NULL DEFAULT 0, -- Consecutive failed login count
    LockedUntil DATETIME2 NULL, -- Temporary lockout expiry
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    ManagedPropertyId INT NULL, -- Manager owns one property (FK added after Properties exists)
    UNIQUE (Username),
    UNIQUE (Email)
);
GO

-- Index supports filtering user lists and audit views by role
CREATE INDEX IX_Users_Role ON Users (Role);
GO

-- ============================================================
-- 2. Properties, Buildings, Room Types, Rooms & Services
-- Core rental inventory: one landlord owns many properties;
-- each property has buildings, room types, rooms, and billable services.
-- ============================================================

CREATE TABLE Properties (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LandlordId INT NOT NULL, -- Owning landlord user id
    Name NVARCHAR(150) NOT NULL, -- Property display name
    Address NVARCHAR(300) NOT NULL, -- Property address
    Description NVARCHAR(500) NULL, -- Optional property description
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Properties_Users FOREIGN KEY (LandlordId) REFERENCES Users(Id)
);
GO

-- Index speeds landlord-scoped property lookups in the UI
CREATE INDEX IX_Properties_LandlordId ON Properties (LandlordId);
GO

-- Manager must reference exactly one property (after Properties table exists)
ALTER TABLE Users
ADD CONSTRAINT FK_Users_ManagedProperty FOREIGN KEY (ManagedPropertyId) REFERENCES Properties(Id);
GO

-- Index supports resolving managers assigned to a given property
CREATE INDEX IX_Users_ManagedPropertyId ON Users (ManagedPropertyId);
GO

CREATE TABLE Buildings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PropertyId INT NOT NULL, -- id của nhà trọ
    ManagerId INT NULL, -- userId của manager
    Name NVARCHAR(100) NOT NULL, -- Tên tòa nhà
    NumberOfFloors INT NOT NULL, -- Số tầng của tòa nhà
    Description NVARCHAR(500) NULL, -- Mô tả về tòa nhà
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Buildings_Users FOREIGN KEY (ManagerId) REFERENCES Users(Id),
    CONSTRAINT FK_Buildings_Properties FOREIGN KEY (PropertyId) REFERENCES Properties(Id)
);
GO

-- Indexes support manager dashboards and property-scoped building lists
CREATE INDEX IX_Buildings_ManagerId ON Buildings (ManagerId);
CREATE INDEX IX_Buildings_PropertyId ON Buildings (PropertyId);
GO

CREATE TABLE RoomTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PropertyId INT NOT NULL, -- id của nhà trọ
    TypeName NVARCHAR(100) NOT NULL, -- Tên loại phòng (Phòng thường, Phòng VIP...)
    MaxOccupancy INT NOT NULL, -- Số lượng người thuê tối đa
    Area DECIMAL(8, 2) NULL, -- Diện tích mặc định của loại phòng
    BaseRent DECIMAL(15, 2) NOT NULL, -- Giá thuê mặc định của loại phòng
    Description NVARCHAR(300) NULL, -- Mô tả về loại phòng
    CONSTRAINT FK_RoomTypes_Properties FOREIGN KEY (PropertyId) REFERENCES Properties(Id)
);
GO

-- Index supports loading room-type catalogs per property
CREATE INDEX IX_RoomTypes_PropertyId ON RoomTypes (PropertyId);
GO

CREATE TABLE Rooms (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BuildingId INT NOT NULL, -- id của tòa nhà
    RoomTypeId INT NOT NULL, -- id của loại phòng (thuộc cùng nhà trọ với tòa)
    RoomNumber NVARCHAR(20) NOT NULL, -- Số phòng
    Floor INT NOT NULL, -- Tầng
    Status NVARCHAR(30) NOT NULL, -- Trạng thái phòng: Đang ở / Trống / Đã đặt cọc / Bảo trì
    Notes NVARCHAR(300) NULL,
    CONSTRAINT FK_Rooms_Buildings FOREIGN KEY (BuildingId) REFERENCES Buildings(Id),
    CONSTRAINT FK_Rooms_RoomTypes FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(Id)
);
GO

-- Indexes support room boards filtered by building, type, or occupancy status
CREATE INDEX IX_Rooms_BuildingId ON Rooms (BuildingId);
CREATE INDEX IX_Rooms_RoomTypeId ON Rooms (RoomTypeId);
CREATE INDEX IX_Rooms_Status ON Rooms (Status);
GO

CREATE TABLE Services (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PropertyId INT NOT NULL, -- id của nhà trọ
    ServiceName NVARCHAR(100) NOT NULL, -- Tên dịch vụ (Điện, Nước, Internet...)
    Unit NVARCHAR(20) NOT NULL, -- Đơn vị tính dịch vụ (kWh, m3, Tháng...)
    UnitPrice DECIMAL(15, 2) NOT NULL, -- Đơn giá của một đơn vị dịch vụ
    CalculationMethod NVARCHAR(30) NOT NULL, -- Cách tính phí: Theo chỉ số / Theo người / Theo phòng
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Services_Properties FOREIGN KEY (PropertyId) REFERENCES Properties(Id)
);
GO

-- Index supports loading the service catalog per property for billing
CREATE INDEX IX_Services_PropertyId ON Services (PropertyId);
GO

-- Junction table: which services are enabled on which rooms (composite PK)
CREATE TABLE RoomServices (
    RoomId INT NOT NULL,
    ServiceId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_RoomServices PRIMARY KEY (RoomId, ServiceId),
    CONSTRAINT FK_RoomServices_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
    CONSTRAINT FK_RoomServices_Services FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);
GO

-- Index supports reverse lookups from service to assigned rooms
CREATE INDEX IX_RoomServices_ServiceId ON RoomServices (ServiceId);
GO

-- ============================================================
-- 3. Tenants, Contracts & Occupancy
-- Tenant master data plus rental contracts and named occupants.
-- ContractTenants links many tenants to one contract (main + co-tenants).
-- ============================================================

CREATE TABLE Tenants (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL, -- Họ và tên khách
    DateOfBirth DATE NOT NULL,
    IdCardNumber VARCHAR(20) NOT NULL, -- Số căn cước khách
    PhoneNumber VARCHAR(20) NOT NULL,
    Email VARCHAR(150) NULL,
    PermanentAddress NVARCHAR(300) NULL, -- Địa chỉ thường trú
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(), -- Ngày lưu thông tin khách
    CONSTRAINT UQ__Tenants__713A7B91DE688DB7 UNIQUE (IdCardNumber)
);
GO

CREATE TABLE Contracts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoomId INT NOT NULL, -- id phòng được thuê
    StartDate DATE NOT NULL, -- Ngày bắt đầu hiệu lực hợp đồng
    EndDate DATE NOT NULL, -- Ngày kết thúc hiệu lực hợp đồng
    MonthlyRent DECIMAL(15, 2) NOT NULL, -- Tiền phòng hàng tháng
    DepositAmount DECIMAL(15, 2) NOT NULL, -- Tiền đặt cọc
    Status NVARCHAR(30) NOT NULL, -- Trạng thái hợp đồng: Đang hoạt động / Đã hết hạn / Đã chấm dứt
    Notes NVARCHAR(300) NULL,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Contracts_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT FK_Contracts_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);
GO

-- Indexes support contract lists, expiry alerts, and status dashboards
CREATE INDEX IX_Contracts_RoomId ON Contracts (RoomId);
CREATE INDEX IX_Contracts_StartEnd ON Contracts (StartDate, EndDate);
CREATE INDEX IX_Contracts_Status ON Contracts (Status);
GO

CREATE TABLE ContractTenants (
    ContractId INT NOT NULL, -- id hợp đồng thuê phòng
    TenantId INT NOT NULL, -- id khách thuê tham gia hợp đồng
    IsMainTenant BIT NOT NULL DEFAULT 0, -- Khách thuê đại diện ký hợp đồng
    CONSTRAINT PK_ContractTenants PRIMARY KEY (ContractId, TenantId),
    CONSTRAINT FK_ContractTenants_Contracts FOREIGN KEY (ContractId) REFERENCES Contracts(Id),
    CONSTRAINT FK_ContractTenants_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
GO

-- Index supports finding all contracts a tenant has joined
CREATE INDEX IX_ContractTenants_TenantId ON ContractTenants (TenantId);
GO

-- ============================================================
-- 4. Service Readings
-- Meter readings for index-based services (electricity, water).
-- FK to RoomServices ensures readings exist only for assigned services.
-- ============================================================

CREATE TABLE ServiceReadings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoomId INT NOT NULL, -- id phòng
    ServiceId INT NOT NULL, -- id dịch vụ
    ReadingDate DATETIME2 NOT NULL DEFAULT GETDATE(), -- Ngày giờ ghi nhận chỉ số
    CurrentReading DECIMAL(15, 2) NOT NULL, -- Chỉ số của dịch vụ
    CONSTRAINT FK_ServiceReadings_RoomServices FOREIGN KEY (RoomId, ServiceId) REFERENCES RoomServices(RoomId, ServiceId)
);
GO

-- Index supports fetching latest/previous readings per room-service pair
CREATE INDEX IX_ServiceReadings_RoomService ON ServiceReadings (RoomId, ServiceId);
GO

-- ============================================================
-- 5. Invoices & Payments
-- Monthly billing: invoice header, line items, and payment records.
-- Payments reference the manager (ReceivedBy) who recorded collection.
-- ============================================================

CREATE TABLE Invoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ContractId INT NOT NULL, -- Mã hợp đồng lập hóa đơn
    InvoiceDate  DATETIME2 NOT NULL DEFAULT GETDATE(), -- Ngày lập hóa đơn
    TotalAmount DECIMAL(15, 2) NOT NULL, -- Tổng tiền cần trả trên hóa đơn
    Status NVARCHAR(30) NOT NULL, -- Trạng thái thanh toán: Đã thanh toán / Chưa thanh toán / Thanh toán một phần / Quá hạn
    DueDate DATE NOT NULL, -- Hạn chót thanh toán hóa đơn
    CONSTRAINT FK_Invoices_Contracts FOREIGN KEY (ContractId) REFERENCES Contracts(Id),
    CONSTRAINT UQ_Invoices_ContractDate UNIQUE (ContractId, InvoiceDate)
);
GO

-- Indexes support overdue reports and contract billing history
CREATE INDEX IX_Invoices_ContractId ON Invoices (ContractId);
CREATE INDEX IX_Invoices_DueDate ON Invoices (DueDate);
CREATE INDEX IX_Invoices_Status ON Invoices (Status);
GO

CREATE TABLE InvoiceItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId INT NOT NULL, -- Mã hóa đơn chứa mục thanh toán
    Description NVARCHAR(200) NOT NULL,
    Quantity DECIMAL(15, 2) NOT NULL, -- Số lượng dịch vụ sử dụng
    UnitPrice DECIMAL(15, 2) NOT NULL, -- Đơn giá dịch vụ tại thời điểm lập
    Amount DECIMAL(15, 2) NOT NULL, -- Thành tiền của khoản này
    CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
);
GO

CREATE TABLE Payments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId INT NOT NULL, -- Mã hóa đơn được thanh toán
    PaymentDate DATE NOT NULL, -- Ngày thực hiện trả tiền
    Amount DECIMAL(15, 2) NOT NULL, -- Số tiền đã thanh toán thực tế
    Method NVARCHAR(30) NOT NULL, -- Phương thức thanh toán: Tiền mặt / Chuyển khoản / Thẻ
    ReceivedBy INT NOT NULL, -- userId của manager
    Notes NVARCHAR(300) NULL, -- Ghi chú chi tiết giao dịch thanh toán
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(), -- Thời điểm ghi nhận giao dịch vào hệ thống
    CONSTRAINT FK_Payments_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id),
    CONSTRAINT FK_Payments_Users FOREIGN KEY (ReceivedBy) REFERENCES Users(Id)
);
GO

-- Index supports summing payments and loading payment history per invoice
CREATE INDEX IX_Payments_InvoiceId ON Payments (InvoiceId);
GO

-- ============================================================
-- 6. Audit trail
-- Append-only log of user actions for landlord audit screens.
-- OldValue/NewValue store human-readable before/after snapshots.
-- ============================================================

CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL, -- Mã người dùng thực hiện hành động
    Action NVARCHAR(100) NOT NULL, -- Loại hành động (INSERT/UPDATE/LOGIN...)
    TableName NVARCHAR(100) NOT NULL, -- Tên bảng dữ liệu bị tác động
    RecordId NVARCHAR(50) NULL, -- Khóa chính của dòng bị tác động
    Detail NVARCHAR(200) NULL, -- Nhãn nhận diện (số phòng, username,...)
    OldValue NVARCHAR(MAX) NULL, -- Giá trị cũ của dữ liệu trước sửa
    NewValue NVARCHAR(MAX) NULL, -- Giá trị mới của dữ liệu sau sửa
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(), -- Ngày giờ thực hiện hành động
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

-- Indexes support chronological audit feeds and per-user filtering
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs (Timestamp);
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs (UserId);
GO
