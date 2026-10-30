namespace Aris.UI.Localization;

/// <summary>
/// Small runtime language switch for the WinForms shell.
/// </summary>
/// <remarks>
/// Vietnamese is the default. The manager keeps an English-to-Vietnamese dictionary and
/// applies it to static control text on every idle tick, so controls created later
/// (including dialogs and auto-generated grid headers) are translated without every
/// constructor having to call it. Text boxes and combo boxes are deliberately skipped:
/// their Text is user data or a domain value, not a label.
/// </remarks>
public static class LanguageManager
{
    private static readonly Dictionary<string, string> VietnameseByEnglish = new(StringComparer.Ordinal)
    {
        // Shell and login
        ["ARIS MANAGEMENT SYSTEM"] = "HỆ THỐNG QUẢN LÝ ARIS",
        ["RESIDENCE MANAGEMENT SYSTEM"] = "HỆ THỐNG QUẢN LÝ CƯ TRÚ",
        ["Administrative Residence Information System (ARIS)"] = "Hệ thống thông tin cư trú hành chính (ARIS)",
        ["ARIS"] = "ARIS",
        ["ARIS - Login"] = "ARIS - Đăng nhập",
        ["Login"] = "Đăng nhập",
        ["Logout"] = "Đăng xuất",
        ["Dashboard"] = "Tổng quan",
        ["Citizen"] = "Công dân",
        ["Household"] = "Hộ gia đình",
        ["Transfer"] = "Chuyển hộ",
        ["Reports"] = "Báo cáo",
        ["Accounts"] = "Tài khoản",
        ["Audit Logs"] = "Nhật ký kiểm toán",
        ["System Configuration"] = "Cấu hình hệ thống",
        ["System Administration"] = "Quản trị hệ thống",
        ["Citizen Management"] = "Quản lý công dân",
        ["Household Management"] = "Quản lý hộ gia đình",
        ["Transfer Management"] = "Quản lý chuyển hộ",
        ["Account Management"] = "Quản lý tài khoản",
        ["ADMIN Dashboard"] = "Bảng điều khiển quản trị",
        ["OFFICER Dashboard"] = "Bảng điều khiển cán bộ",
        ["Welcome,"] = "Chào mừng,",
        ["You are logged in as Administrator."] = "Bạn đang đăng nhập với vai trò quản trị viên.",
        ["You are logged in as Officer."] = "Bạn đang đăng nhập với vai trò cán bộ.",
        ["User:"] = "Người dùng:",
        ["Role:"] = "Vai trò:",
        ["Role: None"] = "Vai trò: Không có",
        ["User: Not logged in"] = "Người dùng: Chưa đăng nhập",
        ["Connection: Checking..."] = "Kết nối: Đang kiểm tra...",
        ["Connection: Connected"] = "Kết nối: Đã kết nối",
        ["Connection: Disconnected"] = "Kết nối: Mất kết nối",
        ["Connection: Not checked"] = "Kết nối: Chưa kiểm tra",
        ["Language"] = "Ngôn ngữ",

        // Common actions
        ["Login"] = "Đăng nhập",
        ["Create"] = "Tạo",
        ["Create first administrator"] = "Tạo quản trị viên đầu tiên",
        ["Create the first administrator"] = "Tạo quản trị viên đầu tiên",
        ["Register"] = "Đăng ký",
        ["Save"] = "Lưu",
        ["Save Changes"] = "Lưu thay đổi",
        ["Cancel"] = "Hủy",
        ["Close"] = "Đóng",
        ["Search"] = "Tìm kiếm",
        ["Refresh"] = "Làm mới",
        ["Add"] = "Thêm",
        ["Add member"] = "Thêm thành viên",
        ["Add Account"] = "Thêm tài khoản",
        ["Edit Account"] = "Sửa tài khoản",
        ["Activate Account"] = "Kích hoạt tài khoản",
        ["Disable Account"] = "Khóa tài khoản",
        ["Reset Password"] = "Đặt lại mật khẩu",
        ["Change Password"] = "Đổi mật khẩu",
        ["Change head"] = "Đổi chủ hộ",
        ["Deactivate"] = "Vô hiệu hóa",
        ["Deactivate citizen"] = "Vô hiệu hóa công dân",
        ["Deactivate standard citizen"] = "Vô hiệu hóa công dân",
        ["View / Edit"] = "Xem / Sửa",
        ["View household"] = "Xem hộ gia đình",
        ["Use as head"] = "Dùng làm chủ hộ",
        ["Export CSV"] = "Xuất CSV",
        ["Generate Report"] = "Tạo báo cáo",
        ["Open request"] = "Mở yêu cầu",
        ["New request"] = "Yêu cầu mới",
        ["Create request"] = "Tạo yêu cầu",
        ["Submit decision"] = "Gửi quyết định",
        ["Cancel request"] = "Hủy yêu cầu",
        ["Approve"] = "Duyệt",
        ["Seed demo data"] = "Tạo dữ liệu mẫu",
        ["New..."] = "Mới...",
        ["Register citizen"] = "Đăng ký công dân",
        ["Register household"] = "Đăng ký hộ gia đình",
        ["Register address"] = "Đăng ký địa chỉ",
        ["Register an address"] = "Đăng ký địa chỉ",
        ["Register a household"] = "Đăng ký hộ gia đình",
        ["Update information"] = "Cập nhật thông tin",

        // Labels and fields
        ["Username"] = "Tên đăng nhập",
        ["Password"] = "Mật khẩu",
        ["Confirm Password"] = "Xác nhận mật khẩu",
        ["Current Password"] = "Mật khẩu hiện tại",
        ["New Password"] = "Mật khẩu mới",
        ["Enter username"] = "Nhập tên đăng nhập",
        ["Enter password"] = "Nhập mật khẩu",
        ["Search by username..."] = "Tìm theo tên đăng nhập...",
        ["National ID"] = "Số CCCD",
        ["Full Name"] = "Họ và tên",
        ["Date of birth"] = "Ngày sinh",
        ["Gender"] = "Giới tính",
        ["Status"] = "Trạng thái",
        ["Address"] = "Địa chỉ",
        ["Address history"] = "Lịch sử địa chỉ",
        ["Membership history"] = "Lịch sử thành viên",
        ["Created At"] = "Ngày tạo",
        ["User ID"] = "Mã người dùng",
        ["Role"] = "Vai trò",
        ["ID"] = "Mã",
        ["Time"] = "Thời gian",
        ["Entity"] = "Đối tượng",
        ["Details"] = "Chi tiết",
        ["Valid from"] = "Có hiệu lực từ",
        ["Configuration Value:"] = "Giá trị cấu hình:",
        ["From Date:"] = "Từ ngày:",
        ["To Date:"] = "Đến ngày:",
        ["Head citizen"] = "Công dân làm chủ hộ",
        ["New head"] = "Chủ hộ mới",
        ["Effective date"] = "Ngày hiệu lực",
        ["UNASSIGNED citizen"] = "Công dân chưa gán hộ",
        ["Household #, head name, National ID or address"] = "Mã hộ, tên chủ hộ, CCCD hoặc địa chỉ",
        ["National ID or full name"] = "Số CCCD hoặc họ tên",
        ["Reject reason (required when not approved)"] = "Lý do từ chối (bắt buộc khi không duyệt)",
        ["Citizen Detail"] = "Chi tiết công dân",
        ["CitizenDirectoryForm"] = "Hồ sơ công dân",
        ["Configuration Value:"] = "Giá trị cấu hình:",
        ["Address code"] = "Mã địa chỉ",
        ["Address line"] = "Địa chỉ chi tiết",
        ["Ward"] = "Phường/Xã",
        ["District (optional)"] = "Quận/Huyện (không bắt buộc)",
        ["Province"] = "Tỉnh/Thành phố",
        ["Request type"] = "Loại yêu cầu",
        ["Source household"] = "Hộ nguồn",
        ["Moving citizens (active members)"] = "Công dân chuyển đi (thành viên đang hoạt động)",
        ["Destination household"] = "Hộ đích",
        ["Destination address"] = "Địa chỉ đích",
        ["New household head (moving)"] = "Chủ hộ mới (đang chuyển đi)",
        ["Successor head in source"] = "Chủ hộ kế nhiệm ở hộ nguồn",
        ["Register a citizen"] = "Đăng ký công dân",
        ["Update citizen demographics"] = "Cập nhật thông tin công dân",
        ["Save changes"] = "Lưu thay đổi",
        ["Created:"] = "Ngày tạo:",
        ["Updated:"] = "Cập nhật:",
        ["Deactivated:"] = "Vô hiệu hóa:",
        ["Household:"] = "Hộ gia đình:",
        ["Address:"] = "Địa chỉ:",
        ["Household members:"] = "Thành viên hộ:",
        ["Residency timeline:"] = "Tiến trình cư trú:",
        ["(no residences)"] = "(chưa có nơi cư trú)",
        ["present"] = "hiện tại",
        ["Source:"] = "Nguồn:",
        ["Destination:"] = "Đích:",
        ["Maker:"] = "Người tạo:",
        ["Requested:"] = "Thời gian tạo:",
        ["Decided by"] = "Người quyết định",
        ["Transfer request"] = "Yêu cầu chuyển hộ",
        ["Status:"] = "Trạng thái:",
        ["Inactivated:"] = "Ngừng hoạt động:",
        ["Head:"] = "Chủ hộ:",
        ["You made this request. Another officer must approve or reject it."] =
            "Bạn đã tạo yêu cầu này. Một cán bộ khác phải duyệt hoặc từ chối.",
        ["Approve or reject each item. A rejection needs a reason."] =
            "Duyệt hoặc từ chối từng mục. Nếu từ chối phải có lý do.",
        ["ARIS - Household"] = "ARIS - Hộ gia đình",
        ["ARIS - Transfer request"] = "ARIS - Yêu cầu chuyển hộ",
        ["Household"] = "Hộ gia đình",
        ["no address"] = "chưa có địa chỉ",
        ["(none)"] = "(không có)",
        ["at"] = "lúc",

        // Configuration and report grids
        ["ConfigKey"] = "Khóa cấu hình",
        ["ConfigValue"] = "Giá trị",
        ["Description"] = "Mô tả",
        ["UpdatedAt"] = "Cập nhật lúc",
        ["UpdatedBy"] = "Người cập nhật",
        ["TotalHouseholds"] = "Tổng số hộ",
        ["ActiveHouseholds"] = "Hộ đang hoạt động",
        ["InactiveHouseholds"] = "Hộ ngừng hoạt động",
        ["AverageMembers"] = "Thành viên trung bình",
        ["Province"] = "Tỉnh/Thành phố",
        ["TotalResidents"] = "Tổng số cư dân",
        ["ReportDate"] = "Ngày báo cáo",
        ["NewRegistrations"] = "Đăng ký mới",
        ["Transfers"] = "Chuyển hộ",
        ["EndedResidences"] = "Kết thúc cư trú",

        // Form titles
        ["ARIS - Citizen detail"] = "ARIS - Chi tiết công dân",
        ["ARIS - Register citizen"] = "ARIS - Đăng ký công dân",
        ["ARIS - Update citizen"] = "ARIS - Cập nhật công dân",
        ["ARIS - Choose successor head"] = "ARIS - Chọn chủ hộ kế nhiệm",
        ["ARIS - Register household"] = "ARIS - Đăng ký hộ gia đình",
        ["ARIS - Add household member"] = "ARIS - Thêm thành viên hộ",
        ["ARIS - Change household head"] = "ARIS - Đổi chủ hộ",
        ["ARIS - Register address"] = "ARIS - Đăng ký địa chỉ",
        ["ARIS - Create first administrator"] = "ARIS - Tạo quản trị viên đầu tiên",
        ["ARIS - New transfer request"] = "ARIS - Yêu cầu chuyển hộ mới",
        ["ARIS - Login"] = "ARIS - Đăng nhập",

        // Hints and messages
        ["No accounts exist yet. This account receives the ADMIN role and can create officers afterwards."] =
            "Chưa có tài khoản. Tài khoản này sẽ có vai trò ADMIN và có thể tạo cán bộ sau đó.",
        ["Password must satisfy the PASSWORD_MIN_LENGTH system parameter (default 8)."] =
            "Mật khẩu phải đáp ứng tham số PASSWORD_MIN_LENGTH (mặc định 8).",
        ["The departing citizen is the household head. Choose a successor:"] =
            "Công dân rời đi là chủ hộ. Chọn người kế nhiệm:",
        ["There are no active households."] = "Không có hộ gia đình đang hoạt động.",
        ["There are no active addresses. Use New... to register one."] =
            "Không có địa chỉ đang hoạt động. Dùng Mới... để đăng ký.",
        ["This feature is under development."] = "Chức năng này đang được phát triển.",
        ["Select at least one moving citizen and choose the new head."] =
            "Chọn ít nhất một công dân chuyển đi và chọn chủ hộ mới.",
        ["The source head is moving. Choose a successor head who remains."] =
            "Chủ hộ nguồn đang chuyển đi. Chọn chủ hộ kế nhiệm ở lại.",
        ["Choose the moving citizens and the destination. HOUSEHOLD_MOVE requires every active member."] =
            "Chọn công dân chuyển đi và điểm đến. HOUSEHOLD_MOVE yêu cầu tất cả thành viên đang hoạt động.",
        ["The head must be UNASSIGNED. Registration creates the household, address history, HEAD membership and residence in one transaction."] =
            "Chủ hộ phải ở trạng thái UNASSIGNED. Đăng ký sẽ tạo hộ, lịch sử địa chỉ, thành viên HEAD và nơi cư trú trong một giao dịch.",
        ["There are no UNASSIGNED citizens. Register a citizen first, then return to register their household."] =
            "Không có công dân UNASSIGNED. Hãy đăng ký công dân trước, sau đó quay lại đăng ký hộ.",
        ["JOIN_EXISTING: choose the destination household.\nNEW_HOUSEHOLD: choose the destination address and the new head.\nHOUSEHOLD_MOVE: choose the destination address and select every member."] =
            "JOIN_EXISTING: chọn hộ đích.\nNEW_HOUSEHOLD: chọn địa chỉ đích và chủ hộ mới.\nHOUSEHOLD_MOVE: chọn địa chỉ đích và chọn tất cả thành viên.",

        // Grid property/header names
        ["CitizenId"] = "Mã công dân",
        ["FullName"] = "Họ và tên",
        ["DateOfBirth"] = "Ngày sinh",
        ["HouseholdId"] = "Mã hộ",
        ["NationalId"] = "Số CCCD",
        ["HeadCitizenId"] = "Mã chủ hộ",
        ["HeadFullName"] = "Chủ hộ",
        ["CurrentAddressText"] = "Địa chỉ hiện tại",
        ["ActiveMemberCount"] = "Số thành viên",
        ["RequestId"] = "Mã yêu cầu",
        ["RequestType"] = "Loại yêu cầu",
        ["EffectiveDate"] = "Ngày hiệu lực",
        ["RequestedBy"] = "Mã người tạo",
        ["RequestedByName"] = "Người tạo",
        ["RequestedAt"] = "Thời gian tạo",
        ["ItemCount"] = "Số mục",
        ["ApprovedItemCount"] = "Đã duyệt",
        ["RejectedItemCount"] = "Từ chối",
        ["AuditLogId"] = "Mã",
        ["EntityType"] = "Đối tượng",
        ["OccurredAt"] = "Thời gian",
        ["AddressId"] = "Mã địa chỉ",
        ["AddressText"] = "Địa chỉ",
        ["StartDate"] = "Bắt đầu",
        ["EndDate"] = "Kết thúc",
        ["MembershipId"] = "Mã thành viên",
        ["RejectReason"] = "Lý do từ chối",
        ["ItemStatus"] = "Trạng thái",
        ["Citizen"] = "Công dân",
        ["CreatedAt"] = "Ngày tạo"
    };

    private static readonly Dictionary<string, string> EnglishByVietnamese = BuildReverse();

    static LanguageManager()
    {
        System.Windows.Forms.Application.Idle += (_, _) => ApplyToOpenForms();
    }

    /// <summary>True when the UI is currently Vietnamese.</summary>
    public static bool IsVietnamese { get; private set; } = true;

    /// <summary>Text for the language button: the language it switches to.</summary>
    public static string SwitchLabel => IsVietnamese ? "English" : "Tiếng Việt";

    public static event Action? LanguageChanged;

    /// <summary>Translates a static label or message to the current language.</summary>
    public static string T(string english)
    {
        if (!IsVietnamese)
        {
            return english;
        }

        return VietnameseByEnglish.TryGetValue(english, out var vietnamese)
            ? vietnamese
            : english;
    }

    /// <summary>Switches the language and refreshes every open form.</summary>
    public static void Toggle()
    {
        IsVietnamese = !IsVietnamese;
        ApplyToOpenForms();
        LanguageChanged?.Invoke();
    }

    /// <summary>Applies the current language to a control tree.</summary>
    public static void Apply(Control root)
    {
        if (root.IsDisposed)
        {
            return;
        }

        switch (root)
        {
            case TextBox textBox:
                if (!string.IsNullOrEmpty(textBox.PlaceholderText))
                {
                    textBox.PlaceholderText = TranslateCurrent(textBox.PlaceholderText);
                }
                break;

            case DataGridView grid:
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    column.HeaderText = TranslateCurrent(column.HeaderText);
                }
                break;

            case ToolStrip strip:
                TranslateToolStripItems(strip.Items);
                break;

            case ComboBox or ListControl:
                // The Text/items are domain values or data; do not translate them.
                break;

            default:
                var text = TranslateCurrent(root.Text);
                if (!string.Equals(text, root.Text, StringComparison.Ordinal))
                {
                    root.Text = text;
                }
                break;
        }

        foreach (Control child in root.Controls.Cast<Control>().ToArray())
        {
            Apply(child);
        }
    }

    private static void ApplyToOpenForms()
    {
        foreach (Form form in System.Windows.Forms.Application.OpenForms.Cast<Form>().ToArray())
        {
            try
            {
                Apply(form);
            }
            catch (ObjectDisposedException)
            {
                // A form closed while the idle handler was iterating.
            }
        }
    }

    private static void TranslateToolStripItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            var text = TranslateCurrent(item.Text ?? string.Empty);
            if (!string.Equals(text, item.Text, StringComparison.Ordinal))
            {
                item.Text = text;
            }

            if (item is ToolStripDropDownItem dropDown)
            {
                TranslateToolStripItems(dropDown.DropDownItems);
            }
        }
    }

    private static string TranslateCurrent(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var dynamicText = TranslateDynamicPrefix(text);
        if (!string.Equals(dynamicText, text, StringComparison.Ordinal))
        {
            return dynamicText;
        }

        return IsVietnamese
            ? VietnameseByEnglish.TryGetValue(text, out var vietnamese) ? vietnamese : text
            : EnglishByVietnamese.TryGetValue(text, out var english) ? english : text;
    }

    /// <summary>
    /// Handles labels that combine a fixed prefix with data, such as
    /// <c>User: admin</c> or <c>Welcome: An</c>.
    /// </summary>
    private static string TranslateDynamicPrefix(string text)
    {
        if (IsVietnamese)
        {
            if (text.StartsWith("Welcome, ", StringComparison.Ordinal))
                return "Chào mừng, " + text["Welcome, ".Length..];
            if (text.StartsWith("User: ", StringComparison.Ordinal))
                return "Người dùng: " + text["User: ".Length..];
            if (text.StartsWith("Role: ", StringComparison.Ordinal))
                return "Vai trò: " + text["Role: ".Length..];
            if (text.StartsWith("Connection: ", StringComparison.Ordinal))
                return "Kết nối: " + text["Connection: ".Length..];
        }
        else
        {
            if (text.StartsWith("Chào mừng, ", StringComparison.Ordinal))
                return "Welcome, " + text["Chào mừng, ".Length..];
            if (text.StartsWith("Người dùng: ", StringComparison.Ordinal))
                return "User: " + text["Người dùng: ".Length..];
            if (text.StartsWith("Vai trò: ", StringComparison.Ordinal))
                return "Role: " + text["Vai trò: ".Length..];
            if (text.StartsWith("Kết nối: ", StringComparison.Ordinal))
                return "Connection: " + text["Kết nối: ".Length..];
        }

        return text;
    }

    private static Dictionary<string, string> BuildReverse()
    {
        var reverse = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var pair in VietnameseByEnglish)
        {
            // Keep the first English label for each Vietnamese value. Several headers
            // share a translation (Status/ItemStatus, Created At/CreatedAt); first-wins
            // restores the display label instead of the property name.
            if (!reverse.ContainsKey(pair.Value))
            {
                reverse[pair.Value] = pair.Key;
            }
        }

        return reverse;
    }
}
