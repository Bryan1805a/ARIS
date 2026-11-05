using Aris.UI.Localization;

namespace Aris.UI;

/// <summary>
/// Form Giới thiệu thành viên nhóm thực hiện đề tài (theo yêu cầu bắt buộc của tiểu luận).
/// </summary>
public sealed class AboutForm : Form
{
    public AboutForm()
    {
        InitializeUi();
        UiTheme.ApplyScreen(this);
    }

    private void InitializeUi()
    {
        Text = LanguageManager.T("Giới thiệu thành viên nhóm - ARIS");
        ClientSize = new Size(900, 540);
        MinimumSize = new Size(850, 480);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var lblTitle = new Label
        {
            Text = "HỆ THỐNG QUẢN LÝ THÔNG TIN CƯ TRÚ (ARIS)",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = UiTheme.Accent,
            Location = new Point(25, 18),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = "Đề tài Tiểu luận Học phần: LẬP TRÌNH TRÊN WINDOWS (C# - WinForms - SQL Server)",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
            ForeColor = UiTheme.Muted,
            Location = new Point(25, 48),
            AutoSize = true
        };

        var groupMembers = new GroupBox
        {
            Text = " Phân công nhiệm vụ 5 thành viên nhóm (Theo Barem giảng viên) ",
            Location = new Point(25, 80),
            Size = new Size(850, 390),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = UiTheme.SemiboldFont
        };

        var gridMembers = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            BackgroundColor = Color.White,
            Font = new Font("Segoe UI", 9.5F)
        };

        gridMembers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        gridMembers.DefaultCellStyle.Padding = new Padding(4, 6, 4, 6);

        var colStt = new DataGridViewTextBoxColumn
        {
            Name = "STT",
            HeaderText = "STT",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };
        var colFn = new DataGridViewTextBoxColumn
        {
            Name = "FullName",
            HeaderText = "Họ và tên",
            FillWeight = 65
        };
        var colMssv = new DataGridViewTextBoxColumn
        {
            Name = "MSSV",
            HeaderText = "MSSV",
            FillWeight = 45,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };
        var colRole = new DataGridViewTextBoxColumn
        {
            Name = "Role",
            HeaderText = "Vai trò",
            FillWeight = 60
        };
        var colMod = new DataGridViewTextBoxColumn
        {
            Name = "Module",
            HeaderText = "Form / Chức năng đảm nhận độc lập",
            FillWeight = 160
        };

        gridMembers.Columns.AddRange(colStt, colFn, colMssv, colRole, colMod);

        gridMembers.Rows.Add("1", "Trần Bửu Ngọc", "231A290130", "Nhóm trưởng / Dev", "Form Quản lý Chuyển hộ (TransferControl) & Maker-Checker");
        gridMembers.Rows.Add("2", "Trần Thị Mỹ Quyên", "231A010586", "DB Designer / Dev", "Form Đăng ký Tạm trú (TemporaryResidence) & Thiết kế CSDL");
        gridMembers.Rows.Add("3", "Hồ Ngọc Hân", "231A290010", "Developer", "Form Quản lý Công dân / Nhân khẩu (CitizenControl)");
        gridMembers.Rows.Add("4", "Đoàn Thị Thuý Duy", "231A290035", "Developer", "Form Quản lý Hộ gia đình (HouseholdControl)");
        gridMembers.Rows.Add("5", "Hà Tuyết Trinh", "231A290045", "Developer", "Form Quản lý Tài khoản (Account) & Nhật ký kiểm toán");

        groupMembers.Controls.Add(gridMembers);

        var btnClose = new Button
        {
            Text = "Đóng",
            Size = new Size(110, 34),
            Location = new Point(765, 485),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            DialogResult = DialogResult.OK
        };

        Controls.AddRange(new Control[] {
            lblTitle,
            lblSubtitle,
            groupMembers,
            btnClose
        });
    }
}
