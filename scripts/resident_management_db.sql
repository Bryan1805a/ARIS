-- =====================================================================
-- HỆ THỐNG QUẢN LÝ THÔNG TIN CƯ TRÚ (RESIDENCE MANAGEMENT SYSTEM)
-- Script khởi tạo Database cho MySQL - Khớp 100% với ERD và Dữ liệu mẫu (Mục 9)
-- Đồ án: Lập trình Windows (C# WinForms)
-- Bảng mã: UTF-8 (utf8mb4 hỗ trợ đầy đủ tiếng Việt có dấu)
-- =====================================================================

-- 1. TẠO CƠ SỞ DỮ LIỆU
CREATE DATABASE IF NOT EXISTS `ResidenceDB`
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `ResidenceDB`;

-- Tắt kiểm tra khóa ngoại tạm thời để xóa/tạo lại bảng an toàn
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `AuditLog`;
DROP TABLE IF EXISTS `TransferRequest`;
DROP TABLE IF EXISTS `HouseholdMembership`;
DROP TABLE IF EXISTS `Residence`;
DROP TABLE IF EXISTS `Household`;
DROP TABLE IF EXISTS `Citizen`;
DROP TABLE IF EXISTS `Address`;
DROP TABLE IF EXISTS `UserAccount`;

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
-- 2. TẠO CÁC BẢNG THEO ĐÚNG CẤU TRÚC ERD
-- =====================================================================

-- ---------------------------------------------------------------------
-- 9.1 Bảng TÀI KHOẢN CÁN BỘ (UserAccount / USER_ACCOUNT)
-- ---------------------------------------------------------------------
CREATE TABLE `UserAccount` (
    `UserId` INT AUTO_INCREMENT PRIMARY KEY,
    `Username` VARCHAR(50) NOT NULL UNIQUE COMMENT 'Tên đăng nhập',
    `PasswordHash` VARCHAR(255) NOT NULL COMMENT 'Mật khẩu đã băm (BCrypt/SHA-256)',
    `Role` VARCHAR(50) NOT NULL DEFAULT 'Officer' COMMENT 'Vai trò: Officer, Admin...',
    `Status` VARCHAR(50) NOT NULL DEFAULT 'ACTIVE' COMMENT 'ACTIVE, INACTIVE',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Tài khoản cán bộ';

-- ---------------------------------------------------------------------
-- 9.3 Bảng ĐỊA CHỈ (Address / ADDRESS)
-- Phả hệ địa chỉ qua ReplacedByAddressId (địa chỉ 2 được thay bởi địa chỉ 3)
-- ---------------------------------------------------------------------
CREATE TABLE `Address` (
    `AddressId` INT AUTO_INCREMENT PRIMARY KEY,
    `AddressCode` VARCHAR(50) NULL COMMENT 'Mã địa chỉ (VD: ADDR-001)',
    `AddressText` VARCHAR(255) NOT NULL COMMENT 'Chi tiết địa chỉ hành chính',
    `Status` VARCHAR(50) NOT NULL DEFAULT 'ACTIVE' COMMENT 'ACTIVE, INACTIVE',
    `ValidFrom` DATE NULL COMMENT 'Ngày bắt đầu hiệu lực',
    `ValidTo` DATE NULL COMMENT 'Ngày kết thúc hiệu lực',
    `ReplacedByAddressId` INT NULL COMMENT 'FK thay thế bởi địa chỉ mới',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `DeactivatedAt` DATETIME NULL COMMENT 'Thời điểm vô hiệu hóa',
    CONSTRAINT `FK_Address_ReplacedBy` FOREIGN KEY (`ReplacedByAddressId`) 
        REFERENCES `Address`(`AddressId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Thông tin địa chỉ';

-- ---------------------------------------------------------------------
-- 9.2 Bảng CÔNG DÂN (Citizen / CITIZEN)
-- ---------------------------------------------------------------------
CREATE TABLE `Citizen` (
    `CitizenId` INT AUTO_INCREMENT PRIMARY KEY,
    `NationalId` VARCHAR(20) NOT NULL UNIQUE COMMENT 'Số định danh cá nhân / CCCD (Duy nhất)',
    `FullName` VARCHAR(100) NOT NULL COMMENT 'Họ và tên đầy đủ',
    `DateOfBirth` DATE NOT NULL COMMENT 'Ngày tháng năm sinh',
    `Gender` VARCHAR(10) NOT NULL COMMENT 'Giới tính: Nam, Nữ',
    `Status` VARCHAR(50) NOT NULL DEFAULT 'ACTIVE' COMMENT 'ACTIVE, INACTIVE',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hồ sơ công dân';

-- ---------------------------------------------------------------------
-- 9.4 Bảng HỘ GIA ĐÌNH (Household / HOUSEHOLD)
-- ---------------------------------------------------------------------
CREATE TABLE `Household` (
    `HouseholdId` INT AUTO_INCREMENT PRIMARY KEY,
    `CurrentAddressId` INT NOT NULL COMMENT 'FK: Địa chỉ hiện tại của hộ',
    `Status` VARCHAR(50) NOT NULL DEFAULT 'ACTIVE' COMMENT 'ACTIVE, INACTIVE',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `InactivatedAt` DATETIME NULL COMMENT 'Thời điểm hộ trở thành INACTIVE',
    CONSTRAINT `FK_Household_Address` FOREIGN KEY (`CurrentAddressId`) 
        REFERENCES `Address`(`AddressId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hộ gia đình';

-- ---------------------------------------------------------------------
-- 9.6 Bảng CƯ TRÚ (Residence / RESIDENCE)
-- Ghi nhận lịch sử cư trú: mỗi công dân active chỉ có 1 Residence ACTIVE
-- ---------------------------------------------------------------------
CREATE TABLE `Residence` (
    `ResidenceId` INT AUTO_INCREMENT PRIMARY KEY,
    `CitizenId` INT NOT NULL COMMENT 'FK: Công dân cư trú',
    `AddressId` INT NOT NULL COMMENT 'FK: Địa chỉ cư trú',
    `StartDate` DATE NOT NULL COMMENT 'Ngày bắt đầu cư trú',
    `EndDate` DATE NULL COMMENT 'Ngày kết thúc cư trú',
    `Status` VARCHAR(50) NOT NULL DEFAULT 'ACTIVE' COMMENT 'ACTIVE, ENDED',
    `VerifiedBy` INT NULL COMMENT 'FK: Cán bộ xác nhận',
    `VerifiedAt` DATETIME NULL COMMENT 'Thời điểm cán bộ xác nhận',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `EndedAt` DATETIME NULL COMMENT 'Thời điểm kết thúc thực tế',
    CONSTRAINT `FK_Residence_Citizen` FOREIGN KEY (`CitizenId`) 
        REFERENCES `Citizen`(`CitizenId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_Residence_Address` FOREIGN KEY (`AddressId`) 
        REFERENCES `Address`(`AddressId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_Residence_VerifiedBy` FOREIGN KEY (`VerifiedBy`) 
        REFERENCES `UserAccount`(`UserId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Thông tin đăng ký cư trú';

-- ---------------------------------------------------------------------
-- 9.5 Bảng THÀNH VIÊN HỘ (HouseholdMembership / HOUSEHOLD_MEMBERSHIP)
-- Mỗi hộ active chỉ có 1 HEAD; Công dân chuyển hộ có 1 dòng ENDED và 1 dòng ACTIVE
-- ---------------------------------------------------------------------
CREATE TABLE `HouseholdMembership` (
    `MembershipId` INT AUTO_INCREMENT PRIMARY KEY,
    `CitizenId` INT NOT NULL COMMENT 'FK: Công dân',
    `HouseholdId` INT NOT NULL COMMENT 'FK: Hộ gia đình',
    `Role` VARCHAR(50) NOT NULL COMMENT 'Vai trò: HEAD (Chủ hộ), MEMBER (Thành viên)',
    `StartDate` DATE NOT NULL COMMENT 'Ngày vào hộ',
    `EndDate` DATE NULL COMMENT 'Ngày rời hộ',
    `Status` VARCHAR(50) NOT NULL DEFAULT 'ACTIVE' COMMENT 'ACTIVE, ENDED',
    `VerifiedBy` INT NULL COMMENT 'FK: Cán bộ xác nhận',
    `VerifiedAt` DATETIME NULL COMMENT 'Thời điểm cán bộ xác nhận',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `EndedAt` DATETIME NULL COMMENT 'Thời điểm kết thúc',
    CONSTRAINT `FK_Membership_Citizen` FOREIGN KEY (`CitizenId`) 
        REFERENCES `Citizen`(`CitizenId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_Membership_Household` FOREIGN KEY (`HouseholdId`) 
        REFERENCES `Household`(`HouseholdId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_Membership_VerifiedBy` FOREIGN KEY (`VerifiedBy`) 
        REFERENCES `UserAccount`(`UserId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Thành viên trong hộ gia đình';

-- ---------------------------------------------------------------------
-- 9.7 Bảng YÊU CẦU CHUYỂN HỘ (TransferRequest / TRANSFER_REQUEST)
-- ---------------------------------------------------------------------
CREATE TABLE `TransferRequest` (
    `RequestId` INT AUTO_INCREMENT PRIMARY KEY,
    `SourceHouseholdId` INT NULL COMMENT 'FK: Hộ nguồn (nơi đi)',
    `DestinationHouseholdId` INT NULL COMMENT 'FK: Hộ đích (nơi đến)',
    `SourceResidenceId` INT NULL COMMENT 'FK: Cư trú nguồn',
    `DestinationAddressId` INT NULL COMMENT 'FK: Địa chỉ đích',
    `RequestedBy` INT NOT NULL COMMENT 'FK: Cán bộ tạo yêu cầu',
    `RequestedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thời điểm tạo yêu cầu',
    `VerifiedBy` INT NULL COMMENT 'FK: Cán bộ thẩm định/duyệt',
    `VerifiedAt` DATETIME NULL COMMENT 'Thời điểm phê duyệt/từ chối',
    `Status` VARCHAR(50) NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING, APPROVED, REJECTED',
    CONSTRAINT `FK_Transfer_SourceHousehold` FOREIGN KEY (`SourceHouseholdId`) 
        REFERENCES `Household`(`HouseholdId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Transfer_DestHousehold` FOREIGN KEY (`DestinationHouseholdId`) 
        REFERENCES `Household`(`HouseholdId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Transfer_SourceResidence` FOREIGN KEY (`SourceResidenceId`) 
        REFERENCES `Residence`(`ResidenceId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Transfer_DestAddress` FOREIGN KEY (`DestinationAddressId`) 
        REFERENCES `Address`(`AddressId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Transfer_RequestedBy` FOREIGN KEY (`RequestedBy`) 
        REFERENCES `UserAccount`(`UserId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_Transfer_VerifiedBy` FOREIGN KEY (`VerifiedBy`) 
        REFERENCES `UserAccount`(`UserId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Yêu cầu chuyển hộ';

-- ---------------------------------------------------------------------
-- 9.8 Bảng NHẬT KÝ KIỂM TOÁN (AuditLog / AUDIT_LOG)
-- ---------------------------------------------------------------------
CREATE TABLE `AuditLog` (
    `AuditLogId` INT AUTO_INCREMENT PRIMARY KEY,
    `ActorUserId` INT NULL COMMENT 'FK: Cán bộ thực hiện hành động',
    `Action` VARCHAR(50) NOT NULL COMMENT 'CREATE_TRANSFER_REQUEST, APPROVE_TRANSFER_REQUEST, END_MEMBERSHIP, REJECT_TRANSFER_REQUEST, LOGIN...',
    `EntityType` VARCHAR(50) NOT NULL COMMENT 'TransferRequest, HouseholdMembership, UserAccount, Citizen...',
    `EntityId` VARCHAR(50) NULL COMMENT 'Mã bản ghi tác động',
    `Timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thời điểm ghi log',
    `Result` VARCHAR(50) NOT NULL DEFAULT 'SUCCESS' COMMENT 'SUCCESS, FAILURE',
    `Details` TEXT NULL COMMENT 'Chi tiết nội dung',
    CONSTRAINT `FK_AuditLog_Actor` FOREIGN KEY (`ActorUserId`) 
        REFERENCES `UserAccount`(`UserId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký kiểm toán hệ thống';

-- =====================================================================
-- 3. TẠO INDEXES (CHỈ MỤC TĂNG TỐC TÌM KIẾM CHO WINFORM)
-- =====================================================================
CREATE INDEX `IDX_Citizen_FullName` ON `Citizen` (`FullName`);
CREATE INDEX `IDX_Citizen_NationalId` ON `Citizen` (`NationalId`);
CREATE INDEX `IDX_Residence_Status` ON `Residence` (`Status`);
CREATE INDEX `IDX_TransferRequest_Status` ON `TransferRequest` (`Status`);
CREATE INDEX `IDX_AuditLog_Timestamp` ON `AuditLog` (`Timestamp`);

-- =====================================================================
-- 4. NẠP DỮ LIỆU MẪU KHỚP 100% VỚI BẢNG DỮ LIỆU CỦA BẠN (MỤC 9)
-- =====================================================================

SET FOREIGN_KEY_CHECKS = 0;

-- ---------------------------------------------------------------------
-- 9.1 Dữ liệu USER_ACCOUNT (Tài khoản cán bộ)
-- Mật khẩu hash mẫu (tương ứng '123456')
-- ---------------------------------------------------------------------
INSERT INTO `UserAccount` (`UserId`, `Username`, `PasswordHash`, `Role`, `Status`) VALUES
(1, 'canbo01', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Officer', 'ACTIVE'),
(2, 'canbo02', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Officer', 'ACTIVE'),
(3, 'admin',   '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Admin',   'ACTIVE');

-- ---------------------------------------------------------------------
-- 9.2 Dữ liệu CITIZEN (Công dân)
-- ---------------------------------------------------------------------
INSERT INTO `Citizen` (`CitizenId`, `NationalId`, `FullName`, `DateOfBirth`, `Gender`, `Status`) VALUES
(1, '079201001234', 'Nguyễn Văn A', '1990-03-12', 'Nam', 'ACTIVE'),
(2, '079201005678', 'Trần Thị B',   '1995-07-25', 'Nữ',  'ACTIVE'),
(3, '079201009012', 'Lê Văn C',     '1948-01-01', 'Nam', 'INACTIVE'),
(4, '079223001111', 'Nguyễn Văn D', '2023-10-10', 'Nam', 'ACTIVE'),
(5, '079201007890', 'Phạm Thị E',   '1985-11-02', 'Nữ',  'ACTIVE');

-- ---------------------------------------------------------------------
-- 9.3 Dữ liệu ADDRESS (Địa chỉ)
-- Địa chỉ 2 bị thay bằng địa chỉ 3 sau đổi địa giới hành chính (ReplacedByAddressId = 3)
-- ---------------------------------------------------------------------
INSERT INTO `Address` (`AddressId`, `AddressCode`, `AddressText`, `Status`, `ValidFrom`, `ValidTo`, `ReplacedByAddressId`) VALUES
(1, 'ADDR-001', '12 Lê Lợi, P. Bến Nghé, Q.1', 'ACTIVE', '2000-01-01', NULL, NULL),
(2, 'ADDR-002', '88 Trần Hưng Đạo, P. Cầu Ông Lãnh, Q.1', 'INACTIVE', '2000-01-01', '2025-06-30', 3),
(3, 'ADDR-003', '88 Trần Hưng Đạo, P. Sài Gòn', 'ACTIVE', '2025-07-01', NULL, NULL);

-- ---------------------------------------------------------------------
-- 9.4 Dữ liệu HOUSEHOLD (Hộ gia đình)
-- Hộ 3 INACTIVE vì thành viên cuối cùng (công dân C) rời hộ
-- ---------------------------------------------------------------------
INSERT INTO `Household` (`HouseholdId`, `CurrentAddressId`, `Status`, `CreatedAt`, `InactivatedAt`) VALUES
(1, 1, 'ACTIVE',   '2020-01-01 00:00:00', NULL),
(2, 3, 'ACTIVE',   '2019-05-01 00:00:00', NULL),
(3, 2, 'INACTIVE', '2010-01-01 00:00:00', '2025-03-15 00:00:00');

-- ---------------------------------------------------------------------
-- 9.5 Dữ liệu HOUSEHOLD_MEMBERSHIP (Thành viên hộ)
-- Công dân A có 2 dòng: ENDED ở hộ 1, ACTIVE ở hộ 2. Mỗi hộ active chỉ có một HEAD.
-- ---------------------------------------------------------------------
INSERT INTO `HouseholdMembership` (`MembershipId`, `CitizenId`, `HouseholdId`, `Role`, `StartDate`, `EndDate`, `Status`, `VerifiedBy`, `VerifiedAt`) VALUES
(1, 1, 1, 'MEMBER', '2020-01-01', '2026-06-30', 'ENDED',  2, '2026-06-28 10:16:00'),
(2, 2, 1, 'HEAD',   '2020-01-01', NULL,         'ACTIVE', 1, '2020-01-01 08:00:00'),
(3, 4, 1, 'MEMBER', '2023-10-10', NULL,         'ACTIVE', 1, '2023-10-10 09:00:00'),
(4, 5, 2, 'HEAD',   '2019-05-01', NULL,         'ACTIVE', 1, '2019-05-01 08:30:00'),
(5, 1, 2, 'MEMBER', '2026-07-01', NULL,         'ACTIVE', 2, '2026-06-28 10:15:00'),
(6, 3, 3, 'HEAD',   '2010-01-01', '2025-03-15', 'ENDED',  1, '2010-01-01 09:00:00');

-- ---------------------------------------------------------------------
-- 9.6 Dữ liệu RESIDENCE (Cư trú)
-- Công dân A (và E) nối liền ngày nhau (30/06 rồi 01/07), không chồng lấn.
-- Địa chỉ trùng CurrentAddressId của hộ đang ở.
-- ---------------------------------------------------------------------
INSERT INTO `Residence` (`ResidenceId`, `CitizenId`, `AddressId`, `StartDate`, `EndDate`, `Status`, `VerifiedBy`, `VerifiedAt`) VALUES
(1, 1, 1, '2020-01-01', '2026-06-30', 'ENDED',  2, '2026-06-28 10:16:00'),
(2, 1, 3, '2026-07-01', NULL,         'ACTIVE', 2, '2026-06-28 10:15:00'),
(3, 2, 1, '2020-01-01', NULL,         'ACTIVE', 1, '2020-01-01 08:00:00'),
(4, 4, 1, '2023-10-10', NULL,         'ACTIVE', 1, '2023-10-10 09:00:00'),
(5, 5, 2, '2019-05-01', '2025-06-30', 'ENDED',  1, '2019-05-01 08:30:00'),
(6, 5, 3, '2025-07-01', NULL,         'ACTIVE', 1, '2025-07-01 08:00:00'),
(7, 3, 2, '2010-01-01', '2025-03-15', 'ENDED',  1, '2010-01-01 09:00:00');

-- ---------------------------------------------------------------------
-- 9.7 Dữ liệu TRANSFER_REQUEST (Yêu cầu chuyển hộ)
-- Yêu cầu 1: A chuyển từ hộ 1 sang hộ 2 (APPROVED).
-- Yêu cầu 2: Bị từ chối vì hộ đích 3 đang INACTIVE (REJECTED - Lỗi E2).
-- ---------------------------------------------------------------------
INSERT INTO `TransferRequest` (`RequestId`, `SourceHouseholdId`, `DestinationHouseholdId`, `SourceResidenceId`, `DestinationAddressId`, `RequestedBy`, `RequestedAt`, `VerifiedBy`, `VerifiedAt`, `Status`) VALUES
(1, 1, 2, 1, 3, 1, '2026-06-25 08:30:00', 2, '2026-06-28 10:15:00', 'APPROVED'),
(2, 1, 3, 4, 2, 1, '2026-08-10 10:00:00', 2, '2026-08-11 14:00:00', 'REJECTED');

-- ---------------------------------------------------------------------
-- 9.8 Dữ liệu AUDIT_LOG (Nhật ký kiểm toán)
-- Lưu lại vết mọi nghiệp vụ: tạo, phê duyệt, từ chối, đóng thành viên, đăng nhập sai
-- ---------------------------------------------------------------------
INSERT INTO `AuditLog` (`AuditLogId`, `ActorUserId`, `Action`, `EntityType`, `EntityId`, `Timestamp`, `Result`, `Details`) VALUES
(1, 1, 'CREATE_TRANSFER_REQUEST',  'TransferRequest',     '1', '2026-06-25 08:30:00', 'SUCCESS', 'A: hộ 1 sang hộ 2'),
(2, 2, 'APPROVE_TRANSFER_REQUEST', 'TransferRequest',     '1', '2026-06-28 10:15:00', 'SUCCESS', 'Đã đối chiếu hồ sơ'),
(3, 2, 'END_MEMBERSHIP',           'HouseholdMembership', '1', '2026-06-28 10:16:00', 'SUCCESS', 'Đóng thành viên hộ 1'),
(4, 1, 'CREATE_TRANSFER_REQUEST',  'TransferRequest',     '2', '2026-08-10 10:00:00', 'SUCCESS', 'D: hộ 1 sang hộ 3'),
(5, 2, 'REJECT_TRANSFER_REQUEST',  'TransferRequest',     '2', '2026-08-11 14:00:00', 'SUCCESS', 'Hộ đích H003 đang INACTIVE'),
(6, 1, 'LOGIN',                    'UserAccount',         '1', '2026-09-01 07:55:00', 'FAILURE', 'Sai mật khẩu');

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
-- 5. CÁC VIEW TIỆN ÍCH DÀNH CHO GIAO DIỆN WINFORMS
-- =====================================================================

-- 5.1 Danh sách hộ gia đình kèm thông tin Chủ hộ và Địa chỉ
CREATE OR REPLACE VIEW `v_HouseholdList` AS
SELECT 
    h.HouseholdId,
    c.FullName AS HeadName,
    c.NationalId AS HeadNationalId,
    a.AddressCode,
    a.AddressText,
    h.Status,
    h.CreatedAt
FROM Household h
JOIN Address a ON h.CurrentAddressId = a.AddressId
LEFT JOIN HouseholdMembership hm ON h.HouseholdId = hm.HouseholdId AND hm.Role = 'HEAD' AND hm.Status = 'ACTIVE'
LEFT JOIN Citizen c ON hm.CitizenId = c.CitizenId;

-- 5.2 Danh sách thành viên chi tiết của từng hộ gia đình
CREATE OR REPLACE VIEW `v_HouseholdMembers` AS
SELECT 
    hm.MembershipId,
    hm.HouseholdId,
    c.CitizenId,
    c.NationalId,
    c.FullName,
    c.DateOfBirth,
    c.Gender,
    hm.Role,
    hm.StartDate,
    hm.EndDate,
    hm.Status
FROM HouseholdMembership hm
JOIN Citizen c ON hm.CitizenId = c.CitizenId;

-- 5.3 Danh sách yêu cầu chuyển hộ kèm tên công dân và hộ liên quan
CREATE OR REPLACE VIEW `v_TransferRequests` AS
SELECT 
    tr.RequestId,
    c.NationalId,
    c.FullName AS CitizenName,
    tr.SourceHouseholdId,
    tr.DestinationHouseholdId,
    dest_addr.AddressText AS DestinationAddress,
    req_u.Username AS RequestedByOfficer,
    tr.RequestedAt,
    ver_u.Username AS VerifiedByOfficer,
    tr.VerifiedAt,
    tr.Status
FROM TransferRequest tr
LEFT JOIN Residence r ON tr.SourceResidenceId = r.ResidenceId
LEFT JOIN Citizen c ON r.CitizenId = c.CitizenId
LEFT JOIN Address dest_addr ON tr.DestinationAddressId = dest_addr.AddressId
LEFT JOIN UserAccount req_u ON tr.RequestedBy = req_u.UserId
LEFT JOIN UserAccount ver_u ON tr.VerifiedBy = ver_u.UserId;

-- =====================================================================
-- KẾT THÚC SCRIPT KHỞI TẠO VÀ NẠP DỮ LIỆU
-- =====================================================================

