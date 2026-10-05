# Hệ thống Quản lý Thông tin Cư trú
## Software Requirements Specification & Domain Model — v1.0

> **Mục đích của tài liệu:** Tài liệu nền tảng định nghĩa tổng thể yêu cầu phần mềm, domain model, quy tắc nghiệp vụ (business rules) và ràng buộc kỹ thuật. Tài liệu này đóng vai trò là "Single Source of Truth" để toàn bộ thành viên trong nhóm thống nhất trước khi bước vào thiết kế Database (ERD) và triển khai source code.
> 
> **Lưu ý:** Đây là đồ án môn học (Windows Programming), không phải hệ thống quản lý dân cư thực tế và không kết nối trực tiếp với CSDL quốc gia về dân cư hay ứng dụng VNeID.

---

## 1. Project Overview & Vision

### 1.1. Tầm nhìn dự án (Project Vision)
Hệ thống Quản lý Thông tin Cư trú là ứng dụng desktop dành cho cán bộ quản lý cư trú, hướng tới việc tập trung hóa toàn diện công tác quản lý và tra cứu thông tin cư trú của công dân. 

Hệ thống hỗ trợ cán bộ tiếp nhận, đối chiếu giấy tờ và kiểm tra thông tin trước khi ghi nhận chính thức vào cơ sở dữ liệu. Mọi biến động quan trọng đều phải được phê duyệt/xác nhận, đồng thời duy trì đầy đủ lịch sử cư trú và nhật ký kiểm toán (audit logs).

### 1.2. Vấn đề thực tế cần giải quyết (Problem Statement)
Trước đây, thông tin cư trú thường được quản lý trên hồ sơ giấy hoặc các nguồn dữ liệu rời rạc, dẫn đến nhiều bất cập:
- Khó khăn trong việc tra cứu nhanh thông tin công dân và hộ khẩu.
- Rủi ro sai sót hoặc thiếu nhất quán khi cập nhật thay đổi nơi ở.
- Khó khăn trong việc truy vết lịch sử: *Công dân A từng ở đâu? Chuyển đi từ thời điểm nào? Ai là người thực hiện/phê duyệt thay đổi?*
- Thiếu cơ chế ràng buộc toàn vẹn khi tách/chuyển hộ khẩu.

### 1.3. Mục tiêu (Goals)
Xây dựng một hệ thống khép kín theo quy trình:
$$\text{Tiếp nhận} \longrightarrow \text{Kiểm tra đối chiếu} \longrightarrow \text{Xác nhận} \longrightarrow \text{Ghi nhận (Atomic)} \longrightarrow \text{Tra cứu / Báo cáo}$$

### 1.4. Phạm vi & Giới hạn hệ thống (Scope & Boundaries)
- **Thuộc phạm vi:**
  - Quản lý hồ sơ công dân cơ bản phục vụ cư trú.
  - Quản lý danh mục địa chỉ và phả hệ thay đổi địa chỉ (Address Lineage).
  - Quản lý hộ gia đình, thành viên trong hộ, vai trò chủ hộ (Head).
  - Quản lý nơi cư trú hiện tại và lịch sử cư trú.
  - Đăng ký cư trú, chuyển hộ gia đình, tách hộ, đổi chủ hộ.
  - Cơ chế phê duyệt thông qua Request/Verification.
  - Ghi nhận Audit Log và xuất các thống kê báo cáo.
- **Không thuộc phạm vi:**
  - Nghiệp vụ hộ tịch: Khai sinh, khai tử, kết hôn.
  - Cấp đổi CCCD, bằng lái xe, thẻ bảo hiểm, dịch vụ công hành chính khác.
  - Tích hợp trực tiếp với CSDL Dân cư Quốc gia hoặc VNeID/Digital ID.

---

## 2. Stakeholders & User Roles

Hệ thống phục vụ 2 nhóm Actor chính:

| Actor | Vai trò & Thẩm quyền chính |
| :--- | :--- |
| **Cán bộ quản lý dân cư (Officer)** | - Tiếp nhận, kiểm tra đối chiếu hồ sơ giấy tờ thực tế.<br>- Quản lý thông tin công dân, địa chỉ.<br>- Thực hiện đăng ký cư trú, chuyển hộ, tách hộ, đổi chủ hộ.<br>- Xác nhận và commit các thay đổi cư trú.<br>- Tra cứu hồ sơ, xem lịch sử cư trú và kết xuất báo cáo thống kê. |
| **Quản trị hệ thống (System Administrator)** | - Quản lý tài khoản cán bộ (tạo, kích hoạt, khóa, đặt lại mật khẩu).<br>- Quản lý phân quyền truy cập chức năng (Role-based Authorization).<br>- Giám sát toàn bộ Audit Logs của hệ thống.<br>- Cấu hình các tham số hệ thống (chính sách mật khẩu, session, lưu trữ). |

---

## 3. Core Concepts & Mental Model

Hệ thống phân định rạch ròi giữa 5 khái niệm cốt lõi nhằm tránh sai lầm trong thiết kế CSDL:

```text
                  +-----------+
                  |  Citizen  | (Người này là ai?)
                  +-----+-----+
                        |
             +----------+----------+
             |                     |
             v                     v
   +------------------+    +----------------+
   | HouseholdMember- |    |   Residence    |
   |      ship        |    | (Ở đâu, từ bao |
   +--------+---------+    |   giờ đến bao  |
            |              |      giờ?)     |
            |              +-------+--------+
            v                      |
      +-----------+                |
      | Household |                |
      | (Thuộc về |                |
      | nhóm nào?)|                |
      +-----+-----+                |
            |                      |
            | CurrentAddress       v
            +---------------->+-----------+
                              |  Address  | (Địa điểm này ở đâu?)
                              +-----------+
```

1. **Citizen (Công dân):** Định danh duy nhất một cá thể (VD: CCCD, Tên, Ngày sinh).
2. **Household (Hộ gia đình):** Tập hợp một nhóm người cư trú cùng nhau. Một hộ có thể có 1 người.
3. **Address (Địa chỉ):** Vị trí vật lý cố định theo địa giới hành chính. Địa chỉ cũ không bị xóa khi địa giới hành chính thay đổi hoặc khi có địa chỉ mới.
4. **Residence (Quá trình cư trú):** Mối liên kết giữa một **Citizen** và một **Address** theo một khoảng thời gian nhất định.
   $$\text{Residence} \neq \text{Address}$$
   *(Address là địa điểm; Residence là lịch sử một người từng ở địa điểm đó trong khoảng thời gian nào).*
5. **HouseholdMembership (Quan hệ thành viên hộ):** Mối liên kết giữa **Citizen** và **Household** có thời gian bắt đầu, kết thúc và vai trò (HEAD hoặc MEMBER).
   $$\text{Không lưu trực tiếp } \texttt{Citizen.HouseholdId}$$
   *(Do một công dân có thể chuyển hộ nhiều lần qua các thời kỳ).*

---

## 4. System Architecture & Constraints

### 4.1. Kiến trúc phân tầng (Layered Architecture)
Hệ thống tuân thủ kiến trúc phân tầng rõ ràng, tách biệt logic nghiệp vụ khỏi giao diện:

```text
+-------------------------------------------------+
|      Presentation Layer (Windows Forms / WPF)   |
+------------------------+------------------------+
                         |
                         v
+-------------------------------------------------+
|    Application Layer (Services, DTOs, Use Cases)|
+------------------------+------------------------+
                         |
                         v
+-------------------------------------------------+
|      Domain Layer (Entities, Invariants, Rules) |
+------------------------+------------------------+
                         |
                         v
+-------------------------------------------------+
|   Infrastructure Layer (EF Core, SQL Server DB) |
+-------------------------------------------------+
```

> **Nguyên tắc:** Nghiệp vụ cốt lõi không được viết trực tiếp trong các event handler của giao diện (ví dụ: cấm viết logic SQL trực tiếp trong sự kiện `button_Click`).

### 4.2. Ràng buộc hệ thống (Constraints)
- **CON-PROJ-01:** Dự án được xây dựng bằng .NET, ngôn ngữ C# và CSDL SQL Server.
- **CON-PROJ-02:** Là đồ án học thuật prototype, độc lập hoàn toàn với hệ thống dân cư quốc gia.
- **CON-SYS-01:** CSDL SQL Server được triển khai tập trung.
- **CON-SYS-02:** Desktop Client chạy trên nền tảng Windows.
- **CON-SEC-01:** Mật khẩu người dùng không được lưu trữ dưới dạng văn bản rõ (plaintext), bắt buộc băm bằng thuật toán an toàn.
- **CON-SEC-02:** Chuỗi kết nối và thông tin bảo mật không được hard-code trong mã nguồn.
- **CON-AUTH-01:** Chỉ người dùng đã xác thực mới được sử dụng hệ thống.
- **CON-AUTH-02:** Cán bộ chỉ được thao tác trong phạm vi quyền hạn được cấp.
- **CON-TRANS-01:** Mọi thao tác thay đổi trạng thái cư trú tác động lên nhiều bảng dữ liệu bắt buộc phải nằm trong một Database Transaction thống nhất (Atomic).

---

## 5. Domain Entities (Mô hình thực thể)

### 5.1. Citizen
- `CitizenId` (PK)
- `NationalId` (CCCD/Định danh cá nhân, Unique)
- `FullName`
- `DateOfBirth`
- `Gender`
- `Status` (`ACTIVE`, `INACTIVE`)
- `CreatedAt`, `UpdatedAt`

*(Lưu ý: Không lưu trực tiếp `HouseholdId` hay `CurrentAddressId` trong entity này).*

### 5.2. Household
- `HouseholdId` (PK)
- `CurrentAddressId` (FK)
- `Status` (`ACTIVE`, `INACTIVE`)
- `CreatedAt`, `InactivatedAt`

### 5.3. HouseholdMembership
- `MembershipId` (PK)
- `CitizenId` (FK)
- `HouseholdId` (FK)
- `Role` (`HEAD`, `MEMBER`)
- `StartDate`, `EndDate`
- `Status` (`ACTIVE`, `ENDED`)
- `VerifiedBy`, `VerifiedAt`
- `CreatedAt`, `EndedAt`

### 5.4. Address
- `AddressId` (PK)
- `AddressCode`
- `AddressText` (Số nhà, đường phố, phường/xã, quận/huyện, tỉnh/thành)
- `Status` (`ACTIVE`, `INACTIVE`)
- `ValidFrom`, `ValidTo`
- `ReplacedByAddressId` (FK tự tham chiếu để quản lý phả hệ địa chỉ khi sáp nhập/đổi tên)
- `CreatedAt`, `DeactivatedAt`

### 5.5. Residence
- `ResidenceId` (PK)
- `CitizenId` (FK)
- `AddressId` (FK)
- `StartDate`, `EndDate`
- `Status` (`ACTIVE`, `ENDED`)
- `VerifiedBy`, `VerifiedAt`
- `CreatedAt`, `EndedAt`

### 5.6. TransferRequest / ResidenceChangeRequest
Thực thể trung gian lưu vết yêu cầu thay đổi trước khi commit vào dữ liệu chính:
- `RequestId` (PK)
- `SourceHouseholdId` (FK)
- `DestinationHouseholdId` (FK)
- `SourceResidenceId` (FK)
- `DestinationAddressId` (FK)
- `RequestedBy`, `RequestedAt`
- `VerifiedBy`, `VerifiedAt`
- `Status` (`PENDING`, `APPROVED`, `REJECTED`, `CANCELLED`)

### 5.7. AuditLog
- `AuditLogId` (PK)
- `ActorUserId` (FK)
- `Action` (VD: `CREATE_CITIZEN`, `TRANSFER_HOUSEHOLD`, v.v.)
- `EntityType`, `EntityId`
- `Timestamp`
- `Result` (`SUCCESS`, `FAILURE`)
- `Details` (Nội dung chi tiết thay đổi)

---

## 6. Business Rules (Quy tắc nghiệp vụ)

### 6.1. Quy tắc Công dân (Citizen Rules)
- **BR-CIT-01 (Định danh duy nhất):** Mỗi công dân có một mã định danh (CCCD/NationalId) duy nhất trên hệ thống.
- **BR-CIT-02 (Thông tin nhận dạng):** Không được tạo hồ sơ công dân nếu thiếu các thông tin nhân thân bắt buộc.
- **BR-CIT-03 (Công dân Inactive):** Chuyển `Status` sang `INACTIVE` khi công dân không còn được quản lý; không được gán quan hệ cư trú mới cho công dân inactive nhưng phải giữ nguyên toàn bộ lịch sử trước đó.
- **BR-CIT-04 (Hộ gia đình duy nhất):** Một công dân có trạng thái cư trú active bắt buộc phải thuộc về chính xác **một** Active Household.
- **BR-CIT-05 (Hộ gia đình một người):** Hệ thống cho phép tồn tại hộ gia đình chỉ gồm một công dân duy nhất.

### 6.2. Quy tắc Hộ gia đình (Household Rules)
- **BR-HH-01 (Duy nhất một chủ hộ):** Một Active Household phải có chính xác **một** chủ hộ (Role = `HEAD`) tại mọi thời điểm.
- **BR-HH-02 (Chủ hộ phải là thành viên):** Chủ hộ bắt buộc phải là một thành viên active thuộc chính hộ gia đình đó.
- **BR-HH-03 (Không trùng lặp thành viên):** Một công dân không thể có hai bản ghi thành viên (Membership) active trong cùng một hộ gia đình hoặc giữa các hộ khác nhau.
- **BR-HH-04 (Hộ trống):** Khi thành viên cuối cùng rời hộ, trạng thái Household chuyển sang `INACTIVE` (Soft-delete, không xóa vật lý khỏi CSDL).
- **BR-HH-05 (Tách hộ):** Cho phép một hoặc nhiều thành viên tách ra lập hộ mới; hộ cũ vẫn tiếp tục tồn tại nếu còn ít nhất một thành viên.
- **BR-HH-06 (Điều kiện làm chủ hộ):** Chủ hộ phải đáp ứng đủ điều kiện theo quy định pháp luật được cán bộ ghi nhận.
- **BR-HH-07 (Quy trình kế nhiệm chủ hộ):** Nếu chủ hộ rời hộ nhưng các thành viên khác vẫn còn, hệ thống không tự động chọn ngẫu nhiên chủ hộ mới mà yêu cầu cán bộ thực hiện quy trình chọn chủ hộ (Head Selection Process).
- **BR-HH-08 (Địa chỉ hiện tại của hộ):** Mọi Active Household phải gắn liền với một `CurrentAddress` hợp lệ.

### 6.3. Quy tắc Cư trú (Residence Rules)
- **BR-RES-01 (Một nơi cư trú active):** Một công dân tại một thời điểm chỉ có tối đa một Active Residence.
- **BR-RES-02 (Địa chỉ hợp lệ):** Active Residence phải trỏ tới một Address hợp lệ.
- **BR-RES-03 (Không chồng lấn thời gian):** Các khoảng thời gian cư trú (`StartDate` $\rightarrow$ `EndDate`) của cùng một công dân tuyệt đối không được chồng lấn nhau.
- **BR-RES-04 (Bảo toàn lịch sử):** Bản ghi cư trú cũ không bị xóa khi thời hạn cư trú kết thúc hoặc khi công dân chuyển nơi ở.
- **BR-RES-05 (Tính liên tục của cư trú):** Không được kết thúc nơi cư trú hiện tại của một công dân active nếu nơi cư trú mới hợp lệ chưa được xác nhận hoàn tất.
- **BR-RES-06 (Phả hệ địa chỉ - Address Lineage):** Khi một địa chỉ được thay thế/cập nhật do thay đổi địa giới hành chính, hệ thống phải liên kết thông qua `ReplacedByAddressId` để các bản ghi cư trú lịch sử vẫn truy xuất đúng địa chỉ thời kỳ đó.
- **BR-RES-07 (Quy chuẩn ngày chuyển đổi):** Khi chuyển nơi cư trú, phải có sự tiếp nối liên tục về mốc thời gian (Ví dụ: Địa chỉ cũ kết thúc ngày 30/06 thì địa chỉ mới bắt đầu từ 01/07).

### 6.4. Nguyên tắc Vàng của MVP (Golden Invariant)
Nhằm giảm độ phức tạp cho đồ án, trong phạm vi bản phát hành này áp dụng **Quy tắc nhất quán hộ khẩu - nơi cư trú**:
$$\text{Citizen đang là Active Member của Household bắt buộc phải có Active Residence tại đúng CurrentAddress của Household đó.}$$

```text
ACTIVE Citizen ──(duy nhất 1)──> ACTIVE HouseholdMembership ──> Household ──> CurrentAddress
       │                                                                            │
       └─────────(duy nhất 1)──> ACTIVE Residence ──────────────────────────────────┘
```

### 6.5. Quy tắc Xác thực & Phê duyệt (Verification Rules)
- **BR-VER-01 (Phê duyệt trước khi đổi trạng thái):** Các thay đổi quan trọng đối với hộ tịch/cư trú chỉ có hiệu lực sau khi cán bộ có thẩm quyền xác nhận hồ sơ.
- **BR-VER-02 (Hệ thống không tự suy diễn pháp lý):** Hệ thống chỉ ghi nhận kết quả xác thực do cán bộ nhập dựa trên việc đối chiếu hồ sơ thực tế; hệ thống không tự động thẩm định pháp lý giấy tờ.
- **BR-VER-03 (Tách biệt Request và Domain State):** Khi yêu cầu chuyển cư trú đang ở trạng thái `PENDING`, trạng thái công dân, nơi cư trú và hộ gia đình hiện tại vẫn được giữ nguyên vẹn.
- **BR-VER-04 (Lưu vết kiểm tra):** Mỗi phê duyệt phải lưu lại thông tin: *Ai xác nhận? Vào thời điểm nào? Kết quả phê duyệt là gì?*

### 6.6. Tính toàn vẹn Lịch sử & Kiểm toán (History & Audit Rules)
- **BR-HIST-01 (Tính bất biến của lịch sử):** Dữ liệu lịch sử đã qua xác nhận tuyệt đối không được phép chỉnh sửa (`UPDATE`) hay xóa (`DELETE`) bằng các thao tác nghiệp vụ thông thường.
- **BR-HIST-02 (Điều chỉnh sai sót):** Khi phát hiện sai sót dữ liệu quá khứ, cán bộ tạo bản ghi mới phản ánh nội dung điều chỉnh, không ghi đè lên bản ghi gốc.
- **BR-AUD-01 (Kiểm toán mọi thao tác trọng yếu):** Mọi hành vi tạo mới, chỉnh sửa, chuyển hộ, hủy bỏ đều phải tạo bản ghi Audit Log.
- **BR-AUD-02 (Tính bất biến của Audit Log):** Nhật ký kiểm toán không thể bị xóa hoặc sửa đổi bởi bất kỳ người dùng nào.
- **BR-AUD-03 (Đầy đủ ngữ cảnh kiểm toán):** Mỗi bản ghi Audit Log bắt buộc ghi lại đầy đủ 5 thành phần: **Who, What, When, Which Entity, Result**.

### 6.7. Đồng thời & Toàn vẹn giao dịch (Concurrency & Transactions)
- **BR-CON-01 (Atomic State Transition):** Thao tác chuyển hộ/cư trú làm thay đổi đồng thời nhiều bảng phải được bọc trong một Database Transaction duy nhất. Nếu có lỗi phát sinh, hệ thống buộc phải `ROLLBACK` toàn bộ trạng thái.
- **BR-CON-02 (Xung đột đồng thời):** Khi hai cán bộ cùng thao tác trên một công dân/hộ gia đình, hệ thống phải áp dụng kiểm soát đồng thời (Optimistic/Pessimistic Concurrency) để từ chối thao tác thứ hai dựa trên dữ liệu cũ.
- **BR-CON-03 (Giới hạn bảo vệ):** Khóa giao diện (UI lock) chỉ mang tính hỗ trợ trải nghiệm; tính toàn vẹn dữ liệu bắt buộc phải được bảo đảm bằng Transaction và Constraint ở tầng CSDL.

---

## 7. Use Case Catalog

```text
AUTHENTICATION
└── UC-01: Login

CITIZEN / RESIDENCE MANAGEMENT
├── UC-02: Manage Citizen (Create, View, Update, Deactivate)
├── UC-03: Manage Address (Create, Search, Update)
├── UC-04: Register Household
├── UC-05: Manage Household Members (Add, Remove, Change Head)
├── UC-06: Transfer Household (Chuyển hộ khẩu & cư trú)
├── UC-07: View Residence History
├── UC-08: Search (Tra cứu Citizen, Household, Address)
└── UC-09: View Reports (Thống kê dân số, biến động cư trú)

SYSTEM ADMINISTRATION
├── UC-10: Manage Accounts
├── UC-11: Manage Roles & Permissions
├── UC-12: View Audit Logs
└── UC-13: System Configuration
```

---

## 8. Chi tiết Functional Requirements (Yêu cầu chức năng)

### 8.1. Phân hệ Authentication & Quản trị hệ thống
- **AUTH-01 (User Login):** Cán bộ đăng nhập bằng Username/Password. Hệ thống xác thực danh tính, kiểm tra trạng thái hoạt động của tài khoản, nhận diện Role và hiển thị Dashboard tương ứng.
- **ACC-01 (Manage Accounts):** Admin có quyền tạo tài khoản mới cho cán bộ, kích hoạt, vô hiệu hóa hoặc reset mật khẩu.
- **PERM-01 (Manage Roles/Permissions):** Admin phân bổ quyền hạn theo chức vụ (Cán bộ quản lý hoặc Admin).
- **AUD-01 (View Audit Logs):** Xem danh sách lịch sử thao tác: Timestamp, User, Action, Entity, EntityId, Result.
- **SYS-01 (System Configuration):** Admin cấu hình các tham số: Tên đơn vị quản lý, độ dài/chính sách mật khẩu, thời gian hết hạn phiên làm việc (session timeout).

### 8.2. Phân hệ Quản lý Công dân (Citizen Management)
- **CIT-01 (Create Citizen):** Tạo hồ sơ công dân mới với các thông tin nhân thân bắt buộc (CCCD, Họ tên, Ngày sinh, Giới tính).
- **CIT-02 (View Citizen):** Tra cứu và xem hồ sơ chi tiết của công dân bao gồm: Thông tin cá nhân, Hộ gia đình hiện tại, Nơi cư trú hiện tại, kèm lối tắt xem lịch sử cư trú.
- **CIT-03 (Update Citizen):** Chỉnh sửa các thông tin cơ bản khi có yêu cầu đính chính sai sót.
- **CIT-04 (Deactivate Citizen):** Chuyển trạng thái công dân sang Inactive khi công dân không còn thuộc phạm vi quản lý.

### 8.3. Phân hệ Quản lý Địa chỉ (Address Management)
- **ADR-01 (Create Address):** Tạo mới một địa chỉ chuẩn hóa (Số nhà, tên đường, phường/xã, quận/huyện).
- **ADR-02 (View/Search Address):** Tìm kiếm địa chỉ theo khu vực hành chính, tên đường hoặc xem danh sách hộ/công dân đang cư trú tại địa chỉ đó.
- **ADR-03 (Update Address / Lineage):** Cập nhật tên đường hoặc thay đổi địa chỉ; ghi nhận liên kết thay thế `ReplacedByAddressId` nhằm duy trì phả hệ địa chỉ.

### 8.4. Phân hệ Quản lý Hộ gia đình (Household Management)
- **HH-01 (Create Household):** Tạo hộ gia đình mới gồm Mã hộ, Ngày tạo, Địa chỉ hiện tại và chỉ định Chủ hộ ban đầu.
- **HH-02 (Add Household Member):** Thêm một công dân vào hộ. Ràng buộc: Công dân phải tồn tại, đang active và chưa thuộc hộ gia đình active nào khác.
- **HH-03 (Remove Household Member):** Xóa tư cách thành viên của một công dân khỏi hộ (kết thúc membership).
- **HH-04 (Change Household Head):** Chuyển giao vai trò chủ hộ cho một thành viên active khác trong cùng hộ. Quy trình: Chọn thành viên $\rightarrow$ Xác thực điều kiện $\rightarrow$ Đổi role chủ hộ cũ thành `MEMBER` $\rightarrow$ Đổi role thành viên mới thành `HEAD`.
- **HH-05 (Transfer Household):** Chuyển hộ khẩu sang địa chỉ mới hoặc chuyển thành viên sang hộ khác (xem chi tiết tại Mục 9).
- **HH-06 (View Household History):** Xem lịch sử biến động địa chỉ của cả hộ và lịch sử gia nhập/rời hộ của từng cá nhân.

### 8.5. Phân hệ Quản lý Cư trú (Residence Management)
- **RES-01 (Register Residence):** Ghi nhận bản ghi cư trú mới cho công dân tại một địa chỉ đã xác định.
- **RES-02 (End Residence):** Đóng bản ghi cư trú hiện tại (chuyển sang `ENDED` kèm ngày kết thúc) khi công dân chuyển đi.
- **RES-03 (View Current Residence):** Xem thông tin nơi cư trú hiện thời của công dân.

### 8.6. Phân hệ Tra cứu & Báo cáo (Search & Reports)
- **SRCH-01 (Search Citizen):** Tìm kiếm công dân theo Họ tên, Số CCCD, Mã định danh.
- **SRCH-02 (Search Household):** Tìm kiếm hộ gia đình theo Mã hộ, Tên chủ hộ, Số CCCD chủ hộ hoặc Địa chỉ.
- **SRCH-03 (Search Address):** Tìm kiếm theo tên đường, số nhà, khu vực hành chính.
- **RPT-01 (Residence Statistics):** Thống kê số lượng nhân khẩu phân bổ theo từng khu vực địa lý.
- **RPT-02 (Household Statistics):** Thống kê tổng số hộ, số hộ active/inactive, quy mô hộ trung bình (average household size).
- **RPT-03 (Residence Movement Report):** Báo cáo biến động cư trú trong kỳ (số lượng đăng ký mới, số chuyển hộ, số kết thúc cư trú).

---

## 9. Đặc tả Use Case Trọng tâm: UC-HH-05 — Transfer Household

| Thuộc tính | Chi tiết |
| :--- | :--- |
| **Use Case ID** | `UC-HH-05` (tương ứng `UC-06`) |
| **Tên Use Case** | Transfer Household (Chuyển hộ khẩu / Chuyển nơi cư trú) |
| **Actor** | Cán bộ quản lý dân cư (Officer) |
| **Độ ưu tiên** | Must (Bắt buộc) |
| **Thực thể liên quan** | `Citizen`, `Household`, `HouseholdMembership`, `Residence`, `Address`, `AuditLog` |
| **Yêu cầu Transaction** | Có (Bắt buộc tính nguyên tố - Atomic Transaction) |

### 9.1. Mục đích
Cho phép cán bộ ghi nhận việc một hoặc nhiều công dân rời hộ gia đình hiện tại để nhập vào hộ gia đình khác, hoặc tách ra thành lập hộ mới, đồng thời bảo đảm cập nhật đồng bộ thông tin cư trú, lịch sử và audit log một cách toàn vẹn.

### 9.2. Tiền điều kiện (Preconditions)
1. Cán bộ đã đăng nhập thành công và có thẩm quyền thực hiện nghiệp vụ.
2. Công dân cần chuyển đang ở trạng thái `ACTIVE` và thuộc duy nhất một Active Household.
3. Nếu chuyển vào hộ có sẵn: Hộ đích phải đang `ACTIVE` và có địa chỉ hợp lệ.
4. Nếu tách/tạo hộ mới: Cán bộ đã chuẩn bị đủ thông tin về chủ hộ mới và địa chỉ đích.
5. Không có xung đột chuyển hộ (pending request) đang xử lý đối với công dân này.

### 9.3. Luồng chính (Main Flow)

```text
Cán bộ chọn công dân cần chuyển
              │
              ▼
Chọn hình thức: (A) Nhập vào hộ có sẵn  HOẶC  (B) Tạo hộ mới
              │
              ▼
Khai báo/chọn Địa chỉ đích & Ngày hiệu lực chuyển đổi
              │
              ▼
Cán bộ kiểm tra đối chiếu hồ sơ và nhấn "Xác nhận (Confirm Verification)"
              │
              ▼
HỆ THỐNG MỞ DATABASE TRANSACTION:
  ├── 1. Đóng HouseholdMembership cũ của công dân (Set EndDate, EndedAt, Status=ENDED)
  ├── 2. Đóng Residence cũ của công dân (Set EndDate, EndedAt, Status=ENDED)
  ├── 3. Tạo/Cập nhật Hộ gia đình đích (nếu là hộ mới: gán Chủ hộ, Địa chỉ)
  ├── 4. Tạo HouseholdMembership mới tại hộ đích
  ├── 5. Tạo Residence mới trỏ tới Địa chỉ đích
  ├── 6. Cập nhật trạng thái hộ nguồn (nếu hết thành viên -> chuyển sang INACTIVE)
  ├── 7. Ghi nhận Audit Log (Who, What, When, Affected Entity, Result)
              │
              ▼
TẤT CẢ THÀNH CÔNG?
  ├── [CÓ] ──> COMMIT TRANSACTION ──> Thông báo thành công cho cán bộ
  └── [KHÔNG] ──> ROLLBACK TOÀN BỘ ──> Báo lỗi, giữ nguyên hiện trạng cũ
```

### 9.4. Các luồng phụ & Xử lý tình huống biên (Alternative & Edge Cases)
- **A1 (Tách hộ đơn nhân):** Công dân A là thành viên duy nhất của H001 chuyển đi lập hộ H002 $\rightarrow$ H001 chuyển sang `INACTIVE`, H002 kích hoạt `ACTIVE` với A là HEAD.
- **A2 (Nhiều thành viên cùng tách hộ):** Một nhóm thành viên chuyển từ H001 sang H002 $\rightarrow$ H001 vẫn `ACTIVE` nếu còn thành viên; H002 chỉ định một người làm HEAD.
- **A3 (Chủ hộ chuyển đi nhưng hộ cũ vẫn còn thành viên):** Hệ thống ngăn chặn việc chọn ngẫu nhiên chủ hộ mới; kích hoạt quy trình chọn chủ hộ kế nhiệm trước khi hoàn tất commit.
- **A4 (Chỉ một người được duyệt trong hồ sơ đề nghị chung):** Nếu hồ sơ xin chuyển cùng lúc nhiều người nhưng chỉ một người đủ điều kiện, chỉ chuyển trạng thái của người đó, giữ nguyên trạng thái của những người chưa đủ điều kiện.
- **A5 (Địa chỉ đích chưa có sẵn):** Yêu cầu cán bộ khai báo địa chỉ mới vào danh mục trước khi thực hiện chuyển hộ.

### 9.5. Luồng ngoại lệ (Exception Flows)
- **E1:** Công dân không tồn tại hoặc đang Inactive $\rightarrow$ Hệ thống từ chối thực hiện.
- **E2:** Hộ gia đình đích đang Inactive $\rightarrow$ Hệ thống ngăn chặn gán thành viên vào hộ không hoạt động.
- **E3:** Xung đột đồng thời (Một cán bộ khác đã sửa đổi công dân/hộ trong quá trình xử lý) $\rightarrow$ Hệ thống phát hiện xung đột phiên bản dữ liệu, hủy giao dịch và yêu cầu tải lại dữ liệu mới nhất.
- **E4:** Lỗi kết nối CSDL hoặc lỗi hệ thống giữa chừng $\rightarrow$ Hệ thống kích hoạt `ROLLBACK` ngay lập tức để tránh tình trạng "đã xóa khỏi hộ cũ nhưng chưa vào hộ mới".

### 9.6. Hậu điều kiện (Postconditions)
- Công dân thuộc về đúng một Active Household duy nhất.
- Nơi cư trú cũ kết thúc, nơi cư trú mới có hiệu lực và trùng với địa chỉ hộ gia đình đích.
- Lịch sử cư trú và thành viên hộ được bổ sung, không bị ghi đè.
- Audit log ghi nhận thành công sự kiện chuyển hộ.

---

## 10. Checklist nghiệm thu thiết kế (Definition of Done for Domain Model)

Trước khi chuyển sang bước vẽ sơ đồ ERD vật lý và viết code, nhóm cần đối chiếu bảng kiểm tra sau:

- [ ] **Thực thể & Quan hệ:** Tách bạch hoàn toàn giữa `Citizen`, `Household`, `HouseholdMembership`, `Address` và `Residence`. Không dùng một trường đơn `Citizen.HouseholdId` để quản lý cư trú.
- [ ] **Bảo toàn Invariant:** Mỗi hộ active có duy nhất 1 chủ hộ; mỗi công dân active có duy nhất 1 hộ active và 1 nơi cư trú active.
- [ ] **Không ghi đè lịch sử:** Thiết kế CSDL bảo đảm việc sửa/chuyển địa chỉ luôn lưu bản ghi lịch sử, không dùng `UPDATE` đè lên giá trị quá khứ.
- [ ] **Ranh giới Transaction:** Khâu chuyển cư trú được đóng gói trong một transaction CSDL duy nhất.
- [ ] **Audit Trail:** Bảng `AuditLog` độc lập với bảng lịch sử cư trú và có cơ chế ngăn chặn xóa sửa.