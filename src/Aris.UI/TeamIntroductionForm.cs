using Aris.UI.Localization;

namespace Aris.UI;

/// <summary>
/// Team introduction screen required by the course brief: it shows the project,
/// the course context and the member-to-screen ownership map (one business screen
/// per member, excluding login, this screen and the statistics screens).
/// </summary>
public sealed class TeamIntroductionForm : Form {
    private static readonly string[][] Members = {
        new[] { "Nguyễn Văn A", "2__________", "Nhóm trưởng", "Kiến trúc · Tổng quan · Quản lý công dân" },
        new[] { "Trần Thị B", "2__________", "Thiết kế CSDL", "Quản lý hộ gia đình · Quản lý địa chỉ" },
        new[] { "Lê Văn C", "2__________", "Lập trình viên", "Quản lý chuyển hộ · Điều chỉnh lịch sử" },
        new[] { "Phạm Thị D", "2__________", "Lập trình viên", "Quản trị tài khoản · Nhật ký kiểm toán" },
        new[] { "Hoàng Văn E", "2__________", "Lập trình viên", "Đăng nhập · Cấu hình hệ thống · Báo cáo" }
    };

    private static readonly string[][] ProjectInfo = {
        new[] { "Course", "Lập trình trên Windows (Windows Programming)" },
        new[] { "Topic", "Xây dựng hệ thống quản lý thông tin cư trú (ARIS)" },
        new[] { "Technology", "C# · .NET 10 · Windows Forms · SQL Server 2022 · EF Core 10" },
        new[] { "Architecture", "4 lớp: UI → Application → Domain; Infrastructure → Application/Domain" },
        new[] { "Team", "LTWIN_N__ - ARIS" },
        new[] { "Version", "3.9 (11/2026)" }
    };

    private readonly DataGridView _gridMembers = new();

    public TeamIntroductionForm() {
        Text = "ARIS - Team introduction";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(880, 700);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiTheme.Background;

        BuildLayout();
        UiTheme.ApplyScreen(this);
        RestoreHeaderColors();
        LanguageManager.Apply(this);
    }

    private void BuildLayout() {
        Panel header = new() {
            Dock = DockStyle.Top,
            Height = 96,
            BackColor = UiTheme.Sidebar
        };

        Label logo = new() {
            Text = "ARIS",
            AutoSize = true,
            Location = new Point(28, 14),
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        Label tagline = new() {
            Name = "lblTeamTagline",
            Text = LanguageManager.T("RESIDENCE MANAGEMENT SYSTEM"),
            AutoSize = true,
            Location = new Point(30, 60),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent
        };

        header.Controls.Add(logo);
        header.Controls.Add(tagline);

        Label sectionProject = CreateSectionLabel("Project information", 24, 112);
        Controls.Add(sectionProject);

        int top = 146;
        foreach (string[] pair in ProjectInfo) {
            Label caption = new() {
                Text = pair[0],
                AutoSize = false,
                Size = new Size(130, 24),
                Location = new Point(30, top),
                Font = UiTheme.SemiboldFont,
                ForeColor = UiTheme.Text
            };

            Label value = new() {
                Text = pair[1],
                AutoSize = false,
                Size = new Size(700, 24),
                Location = new Point(170, top),
                Font = UiTheme.BaseFont,
                ForeColor = UiTheme.Text
            };

            Controls.Add(caption);
            Controls.Add(value);
            top += 26;
        }

        Label sectionMembers = CreateSectionLabel("Members", 24, top + 10);
        Controls.Add(sectionMembers);

        ConfigureMembersGrid();
        _gridMembers.Location = new Point(30, top + 46);
        _gridMembers.Size = new Size(820, 210);
        Controls.Add(_gridMembers);

        Label note = new() {
            Text = "Each member owns at least one business screen; login, team introduction and statistics are excluded.",
            AutoSize = false,
            Size = new Size(700, 40),
            Location = new Point(30, top + 268),
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Muted
        };
        Controls.Add(note);

        Button close = new() {
            Text = "Close",
            Location = new Point(730, ClientSize.Height - 54),
            Size = new Size(120, 38),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            DialogResult = DialogResult.OK
        };
        UiTheme.StyleSecondaryButton(close);
        Controls.Add(close);

        AcceptButton = close;
        CancelButton = close;

        // Add the header last so it keeps the top edge when the form is resized.
        Controls.Add(header);
    }

    private void ConfigureMembersGrid() {
        _gridMembers.Name = "gridMembers";
        _gridMembers.ReadOnly = true;
        _gridMembers.AllowUserToAddRows = false;
        _gridMembers.AllowUserToDeleteRows = false;
        _gridMembers.AllowUserToResizeRows = false;
        _gridMembers.RowHeadersVisible = false;
        _gridMembers.MultiSelect = false;
        _gridMembers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gridMembers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _gridMembers.Columns.Add(new DataGridViewTextBoxColumn {
            HeaderText = "Member",
            Name = "colMember",
            FillWeight = 24
        });
        _gridMembers.Columns.Add(new DataGridViewTextBoxColumn {
            HeaderText = "Student ID",
            Name = "colStudentId",
            FillWeight = 14
        });
        _gridMembers.Columns.Add(new DataGridViewTextBoxColumn {
            HeaderText = "Role",
            Name = "colRole",
            FillWeight = 18
        });
        _gridMembers.Columns.Add(new DataGridViewTextBoxColumn {
            HeaderText = "Screens owned",
            Name = "colScreens",
            FillWeight = 44
        });

        foreach (string[] member in Members) {
            _gridMembers.Rows.Add(member);
        }

        UiTheme.StyleGrid(_gridMembers);
    }

    private static Label CreateSectionLabel(string text, int x, int y) {
        Label label = new() {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y),
            Font = UiTheme.SectionFont,
            ForeColor = UiTheme.Text
        };
        return label;
    }

    /// <summary>
    /// Re-applies the dark header palette after <see cref="UiTheme.ApplyScreen"/>
    /// restyles every label with the screen-wide text colour.
    /// </summary>
    private void RestoreHeaderColors() {
        foreach (Control control in Controls) {
            if (control is Panel panel && panel.Dock == DockStyle.Top) {
                panel.BackColor = UiTheme.Sidebar;
                foreach (Control child in panel.Controls) {
                    if (child is Label label) {
                        label.BackColor = Color.Transparent;
                        label.ForeColor = label.Text == "ARIS"
                            ? Color.White
                            : Color.FromArgb(148, 163, 184);
                    }
                }
            }
        }
    }
}
