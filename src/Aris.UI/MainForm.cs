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

        this.Shown += MainForm_Shown;
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

        Label lblDashboard = new Label {
            Text = LanguageManager.T(isAdmin ? "ADMIN Dashboard" : "OFFICER Dashboard"),
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(25, 25)
        };

        Label lblWelcome = new Label {
            Text = $"{LanguageManager.T("Welcome,")} {_userSession.Username}!",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.DimGray,
            AutoSize = true,
            Location = new Point(25, 80)
        };

        Label lblRole = new Label {
            Text = LanguageManager.T(isAdmin
                ? "You are logged in as Administrator."
                : "You are logged in as Officer."),
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(25, 120)
        };

        pnlContent.Controls.Add(lblDashboard);
        pnlContent.Controls.Add(lblWelcome);
        pnlContent.Controls.Add(lblRole);
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
