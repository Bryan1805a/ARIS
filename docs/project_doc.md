**1. Project Vision**

        Hệ thống quản lý thông tin cư trú là một ứng dụng desktop dành cho cán bộ quản lý cư trú, nhằm tập trung hóa việc quản lý và tra cứu thông tin liên quan đến nơi cư trú của công dân.

        Hệ thống hỗ trợ cán bộ tiếp nhận và kiểm tra thông tin cư trú trước khi ghi nhận vào hệ thống.

        Các thay đổi quan trọng đối với thông tin cư trú phải được xác nhận trước khi cập nhật và lịch sử thay đổi được duy trì để phục vụ tra cứu và kiểm tra sau này.

        Phạm vi của hệ thống tập trung vào thông tin cá nhân phục vụ quản lý cư trú, địa chỉ, đăng ký cư trú, thay đổi nơi cư trú, tình trạng cư trú hiện tại và lịch sử cư trú.

        Hệ thống không bao gồm việc quản lý hoặc xác thực các nghiệp vụ ngoài phạm vi cư trú như khai sinh, khai tử, bảo hiểm, giấy phép hoặc các dịch vụ hành chính khác.



**2. Problems**

        Hiện tại thông tin cư trú có thể được quản lý bằng hồ sơ giấy hoặc các nguồn dữ liệu phân tán, khiến việc:

        tra cứu thông tin;

        cập nhật thay đổi;

        theo dõi lịch sử cư trú;

        đảm bảo dữ liệu nhất quán trở nên khó khăn.



        Ví dụ: Cán bộ cần biết công dân Nguyễn Văn A đang cư trú ở đâu?

                Có thay đổi địa chỉ không?

                Ai thực hiện thay đổi?

                Thay đổi từ khi nào?

                Hiện tại đang ở đâu?



        Nếu A chuyển địa chỉ từ X -> Y:

                - Địa chỉ cũ phải khóa

                - Địa chỉ mới được ghi nhận

                - Lưu ngày chuyển

                - Lưu lịch sử



**3. Stakeholder**

        Ai sử dụng hệ thống



        Có 2 actor: Cán bộ quản lý dân cư và System administrator

        Cán bộ quản lý dân cư có thể:

                - Quản lý công dân

                - Quản lý địa chỉ

                - Đăng ký cư trú của hộ gia đình

                - Chuyển cư trú của hộ gia đình

                - Đăng ký cư trú của cá nhân

                - Chuyển cư

                - Xem lịch sử

                - Tìm kiếm

                - Xem báo cáo

        Cán bộ xác thực thông tin thông qua đối chiếu giấy tờ, trong tương lai có thể phát triển lên thành hệ thống tự xác thực



        System Administrator có thể:

                - Quản lý accounts

                - Phân quyền

                - Xem audit logs

                - Cấu hình hệ thống



**4. Goal**

        Xây dựng một hệ thống tập trung cho phép cán bộ:

        tiếp nhận → kiểm tra → xác nhận → ghi nhận → tra cứu thông tin cư trú



**5. Boundary**

        Chỉ tập trung vào:

        thông tin cá nhân cơ bản phục vụ quản lý cư trú;

        địa chỉ;

        đăng ký cư trú;

        thay đổi nơi cư trú;

        tình trạng cư trú;

        lịch sử cư trú.



        Không xử lý:

        khai sinh;

        khai tử;

        bảo hiểm;

        giấy phép;

        giấy tờ hành chính khác;

        các dịch vụ công khác.



**6. Một vài cơ chế cần làm rõ**

        Cơ chế quản lý hộ gia đình: 

                - Nếu chỉ một vài người trong hộ gia đình chuyển đi mà không phải toàn bộ thành viên thì thế nào?

                        + Cách xử lý: Tách thành viên đó ra khỏi entity hộ gia đình cũ và tạo một entity hộ gia đình mới.

        Cán bộ quản lý thông tin cư trú:

                - Cán bộ xác thực thông tin của công dân như thế nào?

                        + Hiện tại cán bộ thực hiện thủ tục xác thực bằng cách đối chiếu giấy tờ.



**7. Use cases**

        AUTHENTICATION

        └── UC-01 Login





        CITIZEN / RESIDENCE MANAGEMENT

        ├── UC-02 Manage Citizen

        ├── UC-03 Manage Address

        ├── UC-04 Register Household

        ├── UC-05 Manage Household Members

        ├── UC-06 Transfer Household

        ├── UC-07 View Residence History

        ├── UC-08 Search

        └── UC-09 View Reports





        SYSTEM ADMINISTRATION

        ├── UC-10 Manage Accounts

        ├── UC-11 Manage Permissions

        ├── UC-12 View Audit Logs

        └── UC-13 Configure System



**8. Functional Requirements**

        **Authentication**
                AUTH-01 - User Login
                        Priority: Must
                        Actor: Officer, Administrator

                        Description:
                                System shall allow registered users to authenticate using their account credentials.

                        

                        Main flow:
                                User
                                        ↓
                                Enter username + password
                                        ↓
                                System validates credentials
                                        ↓
                                System identifies role
                                        ↓
                                Create authenticated session
                                        ↓
                                Display appropriate dashboard



                        Business rules:
                                Account phải tồn tại.
                                Account phải đang active.
                                Password phải chính xác.
                                User chỉ được truy cập chức năng phù hợp với role.



                        Failure cases:
                                Invalid username/password
                                ↓
                                Reject login



                                Inactive account
                                ↓
                                Reject login



                                Too many failed attempts
                                ↓
                                Optional account lock



        **Citizen Management**
                CIT-01 - Create Citizen
                        Actor: Officer
                        Priority: Must
                        Description: Officer có thể tạo hồ sơ công dân mới. Ví dụ:

                        Citizen
                        ────────────
                        Citizen ID
                        National ID
                        Full Name
                        Date of Birth
                        Gender
                        Status



                CIT-02 — View Citizen
                        Descriptions: Officer có thể xem thông tin chi tiết của citizen. Ví dụ:
                        Citizen
                        ────────────────────
                        ID: C000123
                        National ID: \*\*\*\*\*\*\*\*
                        Name: Nguyễn Văn A
                        DOB: 01/01/2000
                        Status: Active

                        Current Household:
                        H00045

                        Current Residence:
                        123 Nguyễn Trãi

                        [View Residence History]
                        [View Household]

                CIT-03 — Update Citizen
                        Description: Officer có thể cập nhật thông tin citizen.

                CIT-04 — Deactivate Citizen
                        Description: Chuyển status từ ACTIVE sang INACTIVE



        **Address Management**
                ADR-01 — Create Address
                        Description: Officer có thể tạo địa chỉ.

                ADR-02 — View / Search Address
                        Description: Cán bộ có thể search theo địa chỉ, search theo khu vực hành chính, xem công dân/hộ gia đình.

                ADR-03 — Update Address
                        Description: 



        **Household Management**

                HH-01 — Create Household
                        Description: Cán bộ tạo hộ gia đình mới. Ví dụ:

                        Household
                        ──────────────
                        Household ID
                        Registration Date
                        Current Address
                        Status



                HH-02 — Add Household Member
                        Description: Cán bộ thêm công dân vào hộ gia đình.

                        Business rules:
                                Citizen must exist.
                                Citizen must be active.
                                Citizen cannot belong to another active household.

                HH-03 — Remove Household Member
                        Description: Cán có thể loại công dân khỏi hộ gia đình, kết thúc membership.



                HH-04 — Change Household Head
                        Description: Cán bộ có thể thay đổi chủ hộ.

                        Business rule:
                                Household head must be an active member of the household.

                        Flow:
                                Current Head

                                        ↓

                                Select new member

                                        ↓

                                Validate membership

                                        ↓

                                End old head role

                                        ↓

                                Assign new head

                HH-05 — Transfer Household
                        Description: Chuyển địa chỉ hộ gia đình sang địa chỉ mới.

                        Hệ thống phải xác nhận:
                                Validate household.
                                Validate new address.
                                Close current residence.
                                Create new residence.
                                Maintain household membership.
                                Preserve history.
                                Create audit record.
                                Commit transaction.

                HH-06 — View Household History
                        Description: Cán bộ có thể xem lịch sử chuyển địa chỉ của hộ gia đình và lịch sử chuyển hộ gia đình của công dân.



        **Residence Management**
                RES-01 — Register Residence
                        Description: Tạo một record mới.

                RES-02 — End Residence:
                        Description: Khi công dân rời địa chỉ, chuyển status địa chỉ từ ACTIVE sang END.

                RES-03 — View Current Residence:
                        Description: Cán bộ có thể kiểm tra hộ gia đình và địa chỉ hiện tại của một công dân.



        **Search**
                SRCH-01 — Search Citizen:
                        Description: Cán bộ có thể tìm công dân bằng Tên, ID công dân.

                SRCH-02 — Search Household:
                        Description: Cán bộ có thể tìm hộ gia đình bằng ID, chủ hộ, địa chỉ.

                SRCH-03 — Search Address:
                        Description: Cán bộ có thể địa chỉ bằng khu vực hành chính, tên đường phố, số nhà.



        **Reports**
                RPT-01 — Residence Statistics:
                        Description: Trả về thông tin dân số của một khu vực.

                RPT-02 — Household Statistics:
                        Description: Trả về số lượng hộ gia đình, số lượng hộ gia đình đang ACTIVE, số lượng hộ gia đình INACTIVE, Average household size.

                RPT-03 — Residence Movement:
                        Description: Trả về báo cáo số lượng new registration, transfer, end.



        **Administrator**
                ACC-01 — Manage Accounts
                        Description: Admistrator có thể Create, View, Activate, Deactivate, Reset password

                PERM-01 — Manage Roles / Permissions
                        Description: Phân quyền hạn cho account.

                AUD-01 — View Audit Logs
                        Description: Admin có thể xem Timestamp, User, Action, Entity, Entity ID, Result.

                SYS-01 — System Configuration
                        Description: Admin có thể cấu hình hệ thống ví dụ như Application name, Password policy, Session timeout, Data retention settings.

**9. Business rules**

        **Citizen Rules**
                BR-CIT-01 — Unique Citizen Identity
                        Description: Mỗi công dân phải có định danh duy nhất.

                BR-CIT-02 — Citizen must be identifiable
                        Description: Công dân không thể được tạo với những thông tin nhận dạng bắt buộc bị thiếu.

                BR-CIT-03 — Deactivated Citizen:
                        Description: Chỉ chuyển status từ ACTIVE sang INACTIVE, không được tạo residence/household relationship mới. Nhưng historical records vẫn tồn tại.

                BR-CIT-04 — Household Membership
                        Description: Một Citizen đang có trạng thái cư trú phải thuộc đúng một Active Household.

                BR-CIT-05 — Single-Person Household
                        Description: Một Household có thể chỉ có một Citizen.

        **Household Rules**
                BR-HH-01 — One Active Household
                        Description: Một citizen chỉ được thuộc một active household tại một thời điểm.

                BR-HH-02 — Household Head Must Be Member
                        Description: Chủ hộ phải là một thành viên của hộ gia đình.

                BR-HH-03 — Household Head Must Be Active
                        Description: Một inactive citizen không thể trở thành chủ hộ.

                BR-HH-04 — Head Eligibility
                        Description: Chỉ một member đáp ứng điều kiện eligibility mới có thể trở thành Household Head.

                BR-HH-05 — Household Must Have a Current Address
                        Description: Một household active phải có current residence/address hợp lệ.

                BR-HH-06 — Household Cannot Have Duplicate Members:
                        Description: Một citizen không thể xuất hiện hai lần trong cùng household.

                BR-HH-07 — Empty Household
                        Description: Chuyển empty household status thành INACTIVE, historical record vẫn phải tồn tại.

                BR-HH-08 — Mandatory Household Head
                        Description: Mỗi Active Household phải có chính xác một Household Head.

                BR-HH-09 — Household Split
                        Description: Một hoặc nhiều members có thể rời Household và hình thành Household mới; Household cũ tiếp tục tồn tại nếu vẫn còn members.

                BR-HH-10 — Head Succession
                        Description: Khi Household Head rời Household, một Head Selection Process phải được hoàn tất trước khi Household tiếp tục ở trạng thái ACTIVE.

                BR-HH-11 — Household Minimum Membership
                        Description: Active Household phải có ít nhất một active member.

                BR-HH-12 — Household Formation:
                        Description: Household mới có thể được tạo với một member duy nhất, với member đó giữ vai trò Head.

        **Residence Rules**
                BR-RES-01 — One Active Residence
                        Description: Một citizen chỉ có một active residence.

                BR-RES-02 — Residence Must Belong to Valid Address
                        Description: Residence phải tham chiếu tới address hợp lệ.

                BR-RES-03 — Valid Date Range
                        Description: Ngày tháng phải hợp lệ

                BR-RES-04 — No Overlapping Active Periods
                        Description: Một citizen không thể có residence periods overlap.

                BR-RES-05 — Transfer Must Preserve Continuity
                        Description: Khi chuyển residence, phải có một convention rõ ràng về ngày hiệu lực. Một semantic thống nhất. Ví dụ
                                        Old: 01/01 → 30/06
                                        New: 01/07 → Present

                BR-RES-06 — Historical Preservation
                        Description: Residence history phải được bảo toàn sau khi Residence kết thúc.

                BR-RES-07 — Historical Address Preservation
                        Description: Historical Residence phải giữ được thông tin cần thiết để xác định Address tại thời điểm Residence đó tồn tại, kể cả khi Address hiện tại đã thay đổi hoặc không còn active.

                BR-RES-08 — Residence Continuity
                        Description: Việc kết thúc Active Residence của Citizen phải đi kèm với việc xác định Residence tiếp theo trước khi transaction được hoàn tất, nếu Citizen vẫn đang ở trạng thái active residence.

                BR-RES-07 — Address Lineage
                        Description: Việc thay thế hoặc cập nhật Address phải bảo toàn mối liên hệ giữa Address hiện tại và các Address lịch sử liên quan.

                BR-RES-08 — Historical Reference Integrity
                        Description: Historical Residence records phải tiếp tục tham chiếu được tới Address tồn tại tại thời điểm Residence đó có hiệu lực.

                BR-RES-09 — Household Residence Consistency
                        Description: Một Citizen là Active Member của một Household phải có Active Residence tại cùng Address với Household đó.

        **Concurrency**
                BR-CON-01 — Single Active Household
                        Description: Hệ thống không được commit trạng thái trong đó một Citizen có nhiều hơn một Active Household.

                BR-CON-02 — Single Active Residence
                        Description: Hệ thống không được commit trạng thái trong đó một Citizen có nhiều hơn một Active Residence.

                BR-CON-03 — Atomic State Transition
                        Description: Các thao tác làm thay đổi đồng thời Household Membership, Residence và các historical records phải được thực hiện atomically.

        **Verification Rules**
                BR-VER-01 — Verification Before Confirmation
                        Description: Thông tin cư trú phải được officer kiểm tra trước khi được đánh dấu là verified/confirmed.

                BR-VER-02 — System Does Not Perform Legal Verification
                        Description: System chỉ record kết quả verification của officer.

                BR-VER-03 — Only Authorized Officer Can Verify
                        Description: Một user không có quyền nghiệp vụ không được xác nhận residence information.

                BR-VER-04 — Verification Before State Change
                        Description: Các thay đổi quan trọng đối với Residence/Household Membership chỉ có hiệu lực sau khi được cán bộ có thẩm quyền xác nhận.

                BR-VER-05 — No Automatic Departure
                        Description: Citizen không tự động rời Household hoặc kết thúc Residence chỉ vì có yêu cầu thay đổi; việc thay đổi phải trải qua quy trình xác nhận.

                BR-VER-06 — Verification Responsibility
                        Description: Hệ thống ghi nhận kết quả xác nhận của cán bộ; hệ thống không tự xác định tính hợp pháp hoặc tính xác thực của hồ sơ bên ngoài.

        **Historical Data Rules**
                BR-HIST-01 — Historical Records Are Immutable
                        Description: Historical Records không thể xóa hoặc chỉnh sửa

                BR-HIST-02 — Corrections Must Preserve Original Record
                        Description: Nếu phát hiện lỗi, tạo một record mới, không sửa record cũ.

        **Audit Rules**
                BR-AUD-01 — Important Operations Are Audited
                        Description: Mọi sự kiện quan trọng đều phải được Audited

                BR-AUD-02 — Audit Logs Are Not Editable
                        Description: Audits Logs không thể xóa hay chỉnh sửa.

                BR-AUD-03 — Audit Must Identify Actor
                        Description: Mỗi audit record cần biết Who, What, When, Which entity, Result

        **Constraints**
                CON-AUTH-01
                        Description: Chỉ authenticated users mới sử dụng hệ thống.

                CON-AUTH-02
                        Description: Officer chỉ truy cập nghiệp vụ Officer.

                CON-SEC-01
                        Description: Passwords không được lưu plaintext.

                CON-SEC-02
                        Description: Database credentials không được hard-code trong source code production configuration.

                CON-SYS-01
                        Description: Hệ thống sử dụng SQL Server làm centralized database.

                CON-SYS-02
                        Description: Desktop client chạy trên Windows.

                CON-PROJ-01
                        Description: Project được xây dựng bằng .NET, C#, SQL Server theo yêu cầu môn học.

                CON-PROJ-02
                        Description: Hệ thống là academic prototype, không tích hợp với hệ thống dữ liệu dân cư quốc gia thực tế.

                CON-TRANS-01
                        Description: Residence-changing operations that modify multiple related records must be atomic

**10. Test Use case**
    # UC-HH-05 — Transfer Household

    ## 1. Use Case Information

    | Item                           | Description                                                  |
    | ------------------------------ | ------------------------------------------------------------ |
    | **Use Case ID**                | UC-HH-05                                                     |
    | **Name**                       | Transfer Household                                           |
    | **Actor**                      | Officer                                                      |
    | **Priority**                   | Must                                                         |
    | **Related Entities**           | Citizen, Household, Household Membership, Residence, Address |
    | **Related Business Processes** | Verification, Household Membership Change, Residence Change  |
    | **Transaction Required**       | Yes                                                          |

    ---

    ## 2. Purpose

    Cho phép cán bộ quản lý ghi nhận việc một hoặc nhiều công dân rời khỏi Household hiện tại và chuyển sang Household khác hoặc hình thành Household mới.

    Use Case phải bảo đảm rằng sau khi thao tác hoàn tất, thông tin về:

    * Household Membership
    * Household Head
    * Current Residence
    * Residence History
    * Historical Records

    vẫn nhất quán với nhau.

    ---

    ## 3. Preconditions

    Trước khi bắt đầu Use Case:

    1. Officer đã đăng nhập thành công.
    2. Officer có quyền thực hiện thao tác thay đổi Household.
    3. Citizen cần chuyển đang tồn tại và đang ở trạng thái active.
    4. Citizen hiện thuộc đúng một Active Household.
    5. Nếu chuyển tới Household đã tồn tại:

    * Household đích tồn tại.
    * Household đích đang ở trạng thái Active.
    6. Nếu tạo Household mới:

    * Officer có đủ thông tin cần thiết để tạo Household.
    7. Address đích đã tồn tại hoặc có thể được tạo theo quy trình quản lý Address.
    8. Không có một transfer request đã được xác nhận khác đang xử lý cho cùng Citizen.

    ---

    ## 4. Trigger

    Officer tiếp nhận yêu cầu thay đổi Household/Residence của Citizen và bắt đầu thao tác **Transfer Household**.

    ---

    # 5. Main Flow

    ### Step 1 — Identify Citizen

    Officer tìm và chọn Citizen cần chuyển.

    System hiển thị:

    * Citizen information
    * Current Household
    * Current Household Head
    * Current Address
    * Current Residence status
    * Relevant residence history

    ---

    ### Step 2 — Select Transfer Type

    Officer lựa chọn:

    1. Chuyển vào Household đã tồn tại.
    2. Tạo Household mới.

    ---

    ### Step 3 — Select Destination

    ### Case A — Existing Household

    Officer chọn Household đích.

    System kiểm tra:

    * Household tồn tại.
    * Household đang Active.
    * Household có Current Address hợp lệ.
    * Citizen chưa thuộc Household đích.

    ### Case B — New Household

    Officer nhập thông tin Household mới và xác định:

    * Household Head
    * Current Address
    * Members ban đầu

    Nếu Citizen là người duy nhất chuyển sang Household mới, Citizen có thể được chỉ định làm Head.

    ---

    ### Step 4 — Declare New Residence

    Officer xác định Residence mới của Citizen.

    System yêu cầu:

    * Destination Address
    * Residence Start Date
    * Các thông tin cần thiết khác theo domain model.

    System kiểm tra:

    > Citizen không được có nhiều hơn một Active Residence sau khi transfer hoàn tất.

    ---

    ### Step 5 — Verify Information

    System hiển thị toàn bộ thay đổi dự kiến:

    ```text
    Current State
    ────────────────────────
    Citizen: A
    Household: H001
    Address: X

    New State
    ────────────────────────
    Household: H002
    Address: Y
    Residence Start: 01/07/2026
    ```

    Officer kiểm tra thông tin và thực hiện **Confirm Verification**.

    Nếu thông tin chưa hợp lệ, Officer có thể quay lại chỉnh sửa.

    ---

    ### Step 6 — Apply Transfer

    Sau khi Officer xác nhận:

    System thực hiện một transaction duy nhất:

    1. Kết thúc Household Membership hiện tại của Citizen.
    2. Tạo Household Membership mới.
    3. Kết thúc Active Residence hiện tại.
    4. Tạo Residence mới.
    5. Nếu tạo Household mới:

    * Tạo Household.
    * Gán Household Head.
    * Gán các member ban đầu.
    6. Cập nhật các trạng thái liên quan.
    7. Tạo Historical Records.
    8. Tạo Audit Log.

    ---

    ### Step 7 — Commit

    Nếu tất cả operations thành công:

    ```text
    COMMIT
    ```

    System thông báo Transfer thành công.

    Nếu bất kỳ operation nào thất bại:

    ```text
    ROLLBACK
    ```

    Không có thay đổi nào được áp dụng vào trạng thái chính.

    ---

    # 6. Alternative Flows

    ## A1 — Citizen creates a new Household

    Citizen hiện tại:

    ```text
    H001
    └── A
    ```

    Officer tạo:

    ```text
    H002
    └── A (Head)
    ```

    Sau transaction:

    ```text
    H001 → INACTIVE
    H002 → ACTIVE
    A → H002
    ```

    Historical records của H001 và Residence cũ được giữ lại.

    ---

    ## A2 — Multiple Citizens split from Household

    Ví dụ:

    ```text
    H001
    ├── A (Head)
    ├── B
    ├── C
    └── D
    ```

    B và C chuyển sang Household mới:

    ```text
    H001
    ├── A (Head)
    └── D

    H002
    ├── B (Head)
    └── C
    ```

    H001 tiếp tục Active vì vẫn còn members.

    H002 trở thành Active sau khi thỏa mãn các điều kiện của Household.

    ---

    ## A3 — Current Head leaves but other members remain

    Ví dụ:

    ```text
    H001
    ├── A (Head)
    ├── B
    └── C
    ```

    A chuyển đi.

    System **không tự động chọn B hoặc C làm Head**.

    System chuyển sang Head Selection Process.

    Một member mới chỉ trở thành Head sau khi:

    1. Được xác định là eligible.
    2. Được lựa chọn theo cơ chế phù hợp.
    3. Được Officer xác nhận.

    ---

    ## A4 — Current Head leaves and no eligible member remains

    Ví dụ:

    ```text
    H001
    └── A (Head)
    ```

    A chuyển đi.

    H001 không còn member.

    System:

    * Kết thúc Household Membership của A.
    * Chuyển H001 → `INACTIVE`.
    * Không tạo Head mới.
    * Bảo toàn toàn bộ historical records.

    ---

    ## A5 — Transfer requires a new Address

    Nếu Household đích chưa có Current Address hợp lệ:

    System không cho phép hoàn tất transfer.

    Officer phải hoàn tất quy trình xác định Address trước.

    ---

    ## A6 — Verification rejected

    Nếu Officer xác định thông tin không đủ điều kiện:

    ```text
    Transfer Request
        ↓
        REJECTED
    ```

    Current Household và Current Residence của Citizen không thay đổi.

    ---

    ## A7 — Transfer cancelled

    Nếu Officer hủy thao tác trước khi verification được xác nhận:

    Không có thay đổi nào đối với domain state.

    ---

    # 7. Exception Flows

    ## E1 — Citizen does not exist

    System từ chối operation.

    Không tạo historical record.

    ---

    ## E2 — Citizen is inactive

    System từ chối transfer.

    ---

    ## E3 — Destination Household is inactive

    System không cho phép Citizen transfer trực tiếp vào Household đó.

    Officer phải sử dụng một quy trình phù hợp để tạo/khôi phục Household theo business rules.

    ---

    ## E4 — Citizen already has another pending transfer

    System không cho phép tạo thêm một transfer process có khả năng xung đột.

    ---

    ## E5 — Concurrent modification

    Nếu một Officer khác đã thay đổi Citizen/Household trong lúc transaction đang được xử lý:

    System phải phát hiện conflict.

    Operation hiện tại không được commit dựa trên dữ liệu cũ.

    Officer phải reload dữ liệu và thực hiện lại thao tác.

    ---

    ## E6 — Database/transaction failure

    Nếu một bước trong transaction thất bại:

    ```text
    ROLLBACK
    ```

    System phải đảm bảo không xảy ra trạng thái:

    ```text
    Citizen removed from H001
    BUT
    Citizen not added to H002
    ```

    hoặc:

    ```text
    Old Residence ended
    BUT
    New Residence not created
    ```

    ---

    # 8. Postconditions

    Sau khi Use Case hoàn tất thành công:

    ### Citizen

    Citizen thuộc đúng một Active Household.

    ### Household Membership

    Membership cũ được kết thúc và Membership mới được tạo.

    ### Residence

    Residence cũ được kết thúc.

    Residence mới trở thành Active.

    ### Household

    Household nguồn:

    * vẫn Active nếu còn members;
    * trở thành Inactive nếu không còn members.

    Household đích:

    * tồn tại;
    * Active;
    * có đúng một Head.

    ### History

    Các historical records liên quan được bảo toàn.

    Không historical record nào bị overwrite hoặc hard-delete.

    ### Audit

    Một Audit Log record được tạo để ghi nhận operation.

    Audit phải xác định được tối thiểu:

    * Officer
    * Operation
    * Timestamp
    * Affected entity
    * Result

    ---

    # 9. Business Rules Involved

    Use Case này phải tuân thủ tối thiểu:

    * BR-CIT-01 — Citizen phải thuộc một Active Household.
    * BR-HH-01 — Active Household có chính xác một Head.
    * BR-HH-02 — Head phải là member.
    * BR-HH-03 — Không duplicate membership.
    * BR-HH-04 — Household không còn member → Inactive.
    * BR-HH-05 — Household có thể được split.
    * BR-HH-06 — Head phải đáp ứng eligibility.
    * BR-RES-01 — Citizen có tối đa một Active Residence.
    * BR-RES-03 — Residence periods không overlap.
    * BR-RES-05 — Historical Address phải được bảo toàn.
    * BR-VER-01 — Thay đổi quan trọng phải được verification.
    * BR-HIST-01 — Historical records immutable.
    * BR-CON-01 — Không được commit nhiều Active Household.
    * BR-CON-02 — Không được commit nhiều Active Residence.
    * BR-CON-03 — Transfer phải atomic.

    ---

    # 10. Transaction Boundary

    Toàn bộ state transition chính của Transfer phải nằm trong **một transaction**.

    Conceptually:

    ```text
    BEGIN TRANSACTION

        Validate current state

        End old membership

        End old residence

        Create/update destination household

        Create new membership

        Create new residence

        Update household state

        Create history records

        Create audit record

    COMMIT
    ```

    Nếu bất kỳ bước nào thất bại:

    ```text
    ROLLBACK
    ```

    ---

    # 11. Audit Event

    Một successful transfer phải tạo Audit Log tương ứng.

    Ví dụ:

    ```text
    Actor:
    Officer #102

    Action:
    TRANSFER_HOUSEHOLD

    Entity:
    Citizen #C001

    From:
    Household H001
    Address A

    To:
    Household H002
    Address B

    Timestamp:
    2026-07-01 09:32:15

    Result:
    SUCCESS
    ```

    Audit Log là system history của **operation**, không phải Residence History.

    ---

    # 12. Important Domain Distinction

    Use Case này không đồng nghĩa với:

    > “Citizen moves house.”

    Nó là một business process bao gồm nhiều state transition có liên quan:

    ```text
    Household Membership
            +
    Residence
            +
    Household State
            +
    Verification
            +
    Historical Record
            +
    Audit
    ```

    Do đó không nên implement nó như một nút:

    ```text
    UPDATE Citizen
    SET HouseholdID = ...
    ```

    Một Transfer Household phải được xem là **domain transaction**.


**11. Domain Model**
        ┌──────────────────┐
        │     Citizen      │
        ├──────────────────┤
        │ CitizenId        │
        │ NationalId       │
        │ FullName         │
        │ DateOfBirth      │
        │ Gender           │
        │ Status           │
        └────────┬─────────┘
                 │
            ┌────┴─────┐
            │          │
            ▼          ▼
        ┌───────────┐ ┌───────────┐
        │ Membership│ │ Residence │
        └─────┬─────┘ └─────┬─────┘
              │             │
              ▼             ▼
        ┌───────────┐ ┌───────────┐
        │ Household │ │  Address  │
        └───────────┘ └───────────┘

        ┌────────────────────┐
        │   TransferRequest  │
        └────────────────────┘
                │
                │ changes
                ▼
        Membership + Residence

        **Main invariant**
        ACTIVE Citizen
                │
                ├── exactly 1 Active HouseholdMembership
                │              │
                │              └── Household
                │                     │
                │                     └── exactly 1 Active HEAD
                │
                └── exactly 1 Active Residence
                               │
                               └── valid Address

        **History**
                Membership History ── preserved
                Residence History   ── preserved
                Address Lineage     ── preserved
                Audit History       ── preserved