using Aris.Application.Accounts;
using Aris.Application.Auditing;
using Aris.Application.Authentication;
using Aris.Application.Common;
using Aris.Application.Interfaces;
using Aris.Application.Reports;
using Aris.Application.SystemConfiguration;
using Aris.UI.Localization;
using Aris.UI.Addresses;

using Microsoft.Extensions.DependencyInjection;

namespace Aris.UI;

// Main application window: contains the sidebar navigation, header/status bar, and content panel
public partial class MainForm : Form {
    private readonly IAccountService _accountService;
    private readonly IDatabaseConnectionService _databaseConnectionService;
    private readonly IUserSession _userSession;
    private readonly IReportService _reportService;
    private readonly IAuditQueryService _auditQueryService;
    private readonly SystemConfigurationService _configurationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ToolStripButton _languageButton = new();
    private Button[] _navButtons = Array.Empty<Button>();
    private MenuStrip _menuStrip = null!;
    private ToolStrip _quickToolStrip = null!;
    private ToolStripMenuItem _miAdministration = null!;

    public MainForm(
        IAccountService accountService,
        IDatabaseConnectionService databaseConnectionService,
        IUserSession userSession,
        IReportService reportService,
        IAuditQueryService auditQueryService,
        SystemConfigurationService configurationService,
        IServiceProvider serviceProvider) {
        InitializeComponent();

        _accountService = accountService;
        _databaseConnectionService = databaseConnectionService;
        _userSession = userSession;
        _reportService = reportService;
        _auditQueryService = auditQueryService;
        _configurationService = configurationService;
        _serviceProvider = serviceProvider;
        btnSystemConfiguration.Click += btnSystemConfiguration_Click;

        // Set up the Vietnamese / English language switch button on the right side of the status bar
        _languageButton.Text = LanguageManager.SwitchLabel;
        _languageButton.Alignment = ToolStripItemAlignment.Right;
        _languageButton.Click += (_, _) => LanguageManager.Toggle();
        statusStrip1.Items.Add(_languageButton);
        LanguageManager.LanguageChanged += OnLanguageChanged;

        ApplyTheme();
        InitializeChrome();

        this.Shown += MainForm_Shown;
    }

    /// <summary>
    /// Builds the menu bar and the quick-access toolbar required by the course
    /// brief (<c>MenuStrip</c>, <c>ToolStrip</c>, <c>StatusStrip</c> on the main
    /// form) and wires every item to the same navigation methods the sidebar uses.
    /// </summary>
    private void InitializeChrome() {
        _quickToolStrip = BuildQuickToolStrip();
        _menuStrip = BuildMenuStrip();

        // Docking runs from the highest child index down: adding the menu last makes
        // it claim the top edge, with the toolbar directly below and the sidebar
        // underneath both. The status strip stays docked at the bottom.
        Controls.Add(_quickToolStrip);
        Controls.Add(_menuStrip);

        LanguageManager.Apply(this);
    }

    private MenuStrip BuildMenuStrip() {
        MenuStrip menu = new() {
            Name = "menuStrip1",
            Dock = DockStyle.Top
        };
        UiTheme.StyleMenuStrip(menu);

        ToolStripMenuItem system = new("System");
        system.DropDownItems.Add(CreateMenuItem("Overview", btnDashboard_Click));
        system.DropDownItems.Add(CreateMenuItem("Log out", btnLogout_Click));
        system.DropDownItems.Add(new ToolStripSeparator());
        system.DropDownItems.Add(CreateMenuItem("Exit", (_, _) => Close()));

        _miAdministration = new ToolStripMenuItem("Administration");
        _miAdministration.DropDownItems.Add(CreateMenuItem("Accounts", btnAccounts_Click));
        _miAdministration.DropDownItems.Add(CreateMenuItem("Audit Logs", btnAuditLogs_Click));
        _miAdministration.DropDownItems.Add(
            CreateMenuItem("System Configuration", btnSystemConfiguration_Click));

        ToolStripMenuItem help = new("Help");
        help.DropDownItems.Add(
            CreateMenuItem("Team introduction", (_, _) => ShowTeamIntroduction()));
        help.DropDownItems.Add(CreateMenuItem("About ARIS", (_, _) => ShowAbout()));

        menu.Items.Add(system);
        menu.Items.Add(CreateMenuItem("Citizens", btnCitizen_Click));
        menu.Items.Add(CreateMenuItem("Households", btnHousehold_Click));
        menu.Items.Add(CreateMenuItem("Addresses", btnAddress_Click));
        menu.Items.Add(CreateMenuItem("Transfers", btnTransfer_Click));
        menu.Items.Add(CreateMenuItem("Reports", btnReports_Click));
        menu.Items.Add(_miAdministration);
        menu.Items.Add(help);

        return menu;
    }

    private ToolStrip BuildQuickToolStrip() {
        ToolStrip strip = new() {
            Name = "toolStrip1",
            Dock = DockStyle.Top
        };
        UiTheme.StyleToolStrip(strip);

        strip.Items.Add(CreateToolButton("Citizens", btnCitizen_Click));
        strip.Items.Add(CreateToolButton("Households", btnHousehold_Click));
        strip.Items.Add(CreateToolButton("Addresses", btnAddress_Click));
        strip.Items.Add(CreateToolButton("Transfers", btnTransfer_Click));
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(CreateToolButton("Reports", btnReports_Click));
        strip.Items.Add(
            CreateToolButton("Team introduction", (_, _) => ShowTeamIntroduction()));
        strip.Items.Add(new ToolStripSeparator());
        strip.Items.Add(CreateToolButton("Refresh", (_, _) => _ = UpdateConnectionStatusAsync()));

        return strip;
    }

    private static ToolStripMenuItem CreateMenuItem(string text, EventHandler onClick) {
        ToolStripMenuItem item = new(text);
        item.Click += onClick;
        return item;
    }

    private static ToolStripButton CreateToolButton(string text, EventHandler onClick) {
        ToolStripButton button = UiTheme.StyleToolStripButton(new ToolStripButton(text));
        button.Click += onClick;
        return button;
    }

    private void ShowTeamIntroduction() {
        using TeamIntroductionForm form =
            _serviceProvider.GetRequiredService<TeamIntroductionForm>();
        form.ShowDialog(this);
    }

    private static void ShowAbout() {
        MessageBox.Show(
            "ARIS - Administrative Residence Information System\n" +
            "Phiên bản 3.9 · .NET 10 · Windows Forms · SQL Server 2022\n\n" +
            "Tiểu luận học phần Lập trình trên Windows.",
            "Về hệ thống ARIS",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ApplyTheme() {
        UiTheme.ApplyShell(this);
        pnlSidebar.BackColor = UiTheme.Sidebar;
        pnlContent.BackColor = UiTheme.Background;

        lblLogo.Text = "ARIS";
        lblLogo.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);
        lblLogo.ForeColor = Color.White;
        lblLogo.Location = new Point(22, 22);

        var subtitle = new Label {
            Name = "lblSidebarSubtitle",
            Text = LanguageManager.T("RESIDENCE MANAGEMENT SYSTEM"),
            AutoSize = false,
            Size = new Size(180, 28),
            Location = new Point(24, 64),
            Font = new Font("Segoe UI", 8.25F),
            ForeColor = Color.FromArgb(148, 163, 184)
        };
        pnlSidebar.Controls.Add(subtitle);

        _navButtons = new[] {
            btnDashboard,
            btnAccounts,
            btnReports,
            btnAuditLogs,
            btnSystemConfiguration,
            btnCitizen,
            btnHousehold,
            btnAddress,
            btnTransfer
        };

        var allSidebarButtons = new[] {
            btnDashboard,
            btnAccounts,
            btnReports,
            btnAuditLogs,
            btnSystemConfiguration,
            btnCitizen,
            btnHousehold,
            btnAddress,
            btnTransfer,
            btnLogout
        };

        foreach (var button in allSidebarButtons) {
            UiTheme.StyleSidebarButton(button);
        }

        foreach (var button in _navButtons) {
            button.Click += (_, _) => UiTheme.SetActiveNav(button, _navButtons);
        }

        UiTheme.SetActiveNav(btnDashboard, _navButtons);
        UiTheme.StyleStatusStrip(statusStrip1);

        lblStatusUser.ForeColor = Color.White;
        lblStatusRole.ForeColor = Color.FromArgb(203, 213, 225);
        lblStatusConnection.ForeColor = Color.White;
        _languageButton.ForeColor = Color.White;
        _languageButton.Font = UiTheme.SemiboldFont;
    }

    // Refreshes UI text when the user toggles between English and Vietnamese
    private void OnLanguageChanged() {
        _languageButton.Text = LanguageManager.SwitchLabel;
        LanguageManager.Apply(this);
        RefreshStatusTexts();
        _ = UpdateConnectionStatusAsync();
    }

    private void RefreshStatusTexts() {
        lblStatusUser.Text = $"{LanguageManager.T("User:")} {_userSession.Username}";
        lblStatusRole.Text = $"{LanguageManager.T("Role:")} {_userSession.Role}";
    }

    public LoginResult? CurrentUser { get; private set; }

    // Sets the logged-in user and configures role-based visibility on the sidebar
    public void SetCurrentUser(LoginResult user) {
        CurrentUser = user;

        lblStatusUser.Text = $"{LanguageManager.T("User:")} {_userSession.Username}";
        lblStatusRole.Text = $"{LanguageManager.T("Role:")} {_userSession.Role}";
        bool isAdmin = string.Equals(
            _userSession.Role,
            "ADMIN",
            StringComparison.OrdinalIgnoreCase);

        // Hide admin-only menu buttons if the logged-in user is an OFFICER
        btnAccounts.Visible = isAdmin;
        btnAuditLogs.Visible = isAdmin;
        btnSystemConfiguration.Visible = isAdmin;
        _miAdministration.Visible = isAdmin;

        // Reposition sidebar buttons smoothly so there are no empty gaps when admin buttons are hidden
        int nextY = btnDashboard.Bottom + 10;

        Button[] sidebarButtons =
        {
            btnAccounts,
            btnReports,
            btnAuditLogs,
            btnSystemConfiguration,
            btnCitizen,
            btnHousehold,
            btnAddress,
            btnTransfer,
            btnLogout
        };

        foreach (Button button in sidebarButtons) {
            // Skip ADMIN-only buttons for OFFICER
            if (!isAdmin &&
                (button == btnAccounts ||
                 button == btnAuditLogs ||
                 button == btnSystemConfiguration)) {
                continue;
            }

            button.Top = nextY;
            nextY += button.Height + 8;
        }
    }

    // Shows the default welcome dashboard
    private void btnDashboard_Click(object? sender, EventArgs e) {
        pnlContent.Controls.Clear();

        bool isAdmin = string.Equals(
            _userSession.Role,
            "ADMIN",
            StringComparison.OrdinalIgnoreCase);

        var card = new Panel {
            BackColor = UiTheme.Card,
            Location = new Point(25, 50),
            Size = new Size(Math.Max(420, pnlContent.ClientSize.Width - 50), 210),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        Label lblDashboard = new Label {
            Text = LanguageManager.T(isAdmin ? "ADMIN Dashboard" : "OFFICER Dashboard"),
            AutoSize = true,
            Location = new Point(24, 24)
        };
        UiTheme.StyleTitle(lblDashboard);

        Label lblWelcome = new Label {
            Text = $"{LanguageManager.T("Welcome,")} {_userSession.Username}!",
            AutoSize = true,
            Location = new Point(26, 84)
        };
        UiTheme.StyleSubtitle(lblWelcome);

        Label lblRole = new Label {
            Text = LanguageManager.T(isAdmin
                ? "You are logged in as Administrator."
                : "You are logged in as Officer."),
            AutoSize = true,
            Location = new Point(26, 118)
        };
        UiTheme.StyleMuted(lblRole);

        card.Controls.Add(lblDashboard);
        card.Controls.Add(lblWelcome);
        card.Controls.Add(lblRole);
        pnlContent.Controls.Add(card);
    }

    // Navigates to User Account Management (ADMIN only)
    private void btnAccounts_Click(object? sender, EventArgs e) {
        pnlContent.Controls.Clear();

        // Defense-in-depth permission check in UI
        if (!_userSession.IsAuthenticated || _userSession.Role != "ADMIN") {
            MessageBox.Show(
                "You do not have permission to access Account Management.",
                "Access Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        AccountManagementControl accountControl =
            _serviceProvider.GetRequiredService<AccountManagementControl>();
        accountControl.CurrentUser = CurrentUser;
        accountControl.Dock = DockStyle.Fill; // auto-expand to fill content area

        pnlContent.Controls.Add(accountControl);
    }

    // Navigates to Reports
    private void btnReports_Click(object? sender, EventArgs e) {
        pnlContent.Controls.Clear();

        ReportsControl reportsControl =
            _serviceProvider.GetRequiredService<ReportsControl>();
        reportsControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(reportsControl);
    }

    // Prompts confirmation and logs out
    private void btnLogout_Click(object? sender, EventArgs e) {
        DialogResult result = MessageBox.Show(
            "Are you sure you want to log out?",
            "Confirm Logout",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes) {
            _userSession.SignOut();
            CurrentUser = null;
            this.Close();
        }
    }

    private async void MainForm_Shown(object? sender, EventArgs e) {
        await UpdateConnectionStatusAsync();
    }

    // Pings database and updates connection status indicator on bottom status bar
    private async Task UpdateConnectionStatusAsync() {
        lblStatusConnection.Text = LanguageManager.T("Connection: Checking...");
        lblStatusConnection.ForeColor = Color.White;

        bool connected = await _databaseConnectionService.CheckConnectionAsync();

        if (connected) {
            lblStatusConnection.Text = LanguageManager.T("Connection: Connected");
            lblStatusConnection.ForeColor = Color.LightGreen;
        } else {
            lblStatusConnection.Text = LanguageManager.T("Connection: Disconnected");
            lblStatusConnection.ForeColor = Color.LightCoral;
        }
    }

    // Navigates to Citizen Management screen
    private void btnCitizen_Click(object? sender, EventArgs e) {
        pnlContent.Controls.Clear();

        CitizenControl citizenControl =
            _serviceProvider.GetRequiredService<CitizenControl>();
        citizenControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(citizenControl);
    }

    // Navigates to Household Management screen
    private void btnHousehold_Click(object? sender, EventArgs e) {
        pnlContent.Controls.Clear();

        HouseholdControl householdControl =
            _serviceProvider.GetRequiredService<HouseholdControl>();
        householdControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(householdControl);
    }

    // Navigates to Address Management screen (ADR-02, ADR-03, SRCH-03)
    private void btnAddress_Click(object? sender, EventArgs e) {
        pnlContent.Controls.Clear();

        AddressControl addressControl = _serviceProvider.GetRequiredService<AddressControl>();
        addressControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(addressControl);
    }

    // Navigates to Relocation/Transfer screen (maker-checker wizard and review inbox)
    private void btnTransfer_Click(object? sender, EventArgs e) {
        pnlContent.Controls.Clear();

        TransferControl transferControl =
            _serviceProvider.GetRequiredService<TransferControl>();
        transferControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(transferControl);
    }

    // Navigates to Audit Log viewer (ADMIN only)
    private async void btnAuditLogs_Click(object? sender, EventArgs e) {
        // Only ADMIN can access Audit Logs
        if (!_userSession.IsAuthenticated || _userSession.Role != "ADMIN") {
            MessageBox.Show(
                "You do not have permission to access Audit Logs.",
                "Access Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        pnlContent.Controls.Clear();

        var auditViewer =
            _serviceProvider.GetRequiredService<AuditViewerControl>();
        auditViewer.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(auditViewer);

        await auditViewer.LoadAuditLogsAsync();
    }

    // Navigates to System Configuration parameters editor (ADMIN only)
    private void btnSystemConfiguration_Click(object? sender, EventArgs e) {
        if (!_userSession.IsAuthenticated ||
            !string.Equals(_userSession.Role, "ADMIN",
                StringComparison.OrdinalIgnoreCase)) {
            MessageBox.Show(
                "You do not have permission to access System Configuration.",
                "Access Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        pnlContent.Controls.Clear();

        var configurationControl =
            _serviceProvider.GetRequiredService<SystemConfigurationControl>();

        configurationControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(configurationControl);
    }
}
