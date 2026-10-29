using Aris.Application.Accounts;
using Aris.Application.Auditing;
using Aris.Application.Authentication;
using Aris.Application.Common;
using Aris.Application.Interfaces;
using Aris.Application.Reports;
using Aris.Application.SystemConfiguration;

using Microsoft.Extensions.DependencyInjection;

namespace Aris.UI;

public partial class MainForm : Form
{
    private readonly IAccountService _accountService;
    private readonly IDatabaseConnectionService _databaseConnectionService;
    private readonly IUserSession _userSession;
    private readonly IReportService _reportService;
    private readonly IAuditQueryService _auditQueryService;
    private readonly SystemConfigurationService _configurationService;
    private readonly IServiceProvider _serviceProvider;

    public MainForm(
    IAccountService accountService,
    IDatabaseConnectionService databaseConnectionService,
    IUserSession userSession,
    IReportService reportService,
    IAuditQueryService auditQueryService,
    SystemConfigurationService configurationService,
    IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _accountService = accountService;
        _databaseConnectionService = databaseConnectionService;
        _userSession = userSession;
        _reportService = reportService;
        _auditQueryService = auditQueryService;
        _configurationService = configurationService;
        _serviceProvider = serviceProvider;
        btnSystemConfiguration.Click += btnSystemConfiguration_Click;

        this.Shown += MainForm_Shown;
    }

    public LoginResult? CurrentUser { get; private set; }

    public void SetCurrentUser(LoginResult user)
    {
        CurrentUser = user;

        lblStatusUser.Text = $"User: {_userSession.Username}";
        lblStatusRole.Text = $"Role: {_userSession.Role}";
        bool isAdmin = string.Equals(
            _userSession.Role,
            "ADMIN",
            StringComparison.OrdinalIgnoreCase);

        btnAccounts.Visible = isAdmin;
        btnAuditLogs.Visible = isAdmin;
        btnSystemConfiguration.Visible = isAdmin;

        int nextY = btnDashboard.Bottom + 10;

        Button[] sidebarButtons =
        {
            btnAccounts,
            btnReports,
            btnAuditLogs,
            btnSystemConfiguration,
            btnCitizen,
            btnHousehold,
            btnTransfer,
            btnLogout
        };

        foreach (Button button in sidebarButtons)
        {
            // Skip ADMIN-only buttons for OFFICER
            if (!isAdmin &&
                (button == btnAccounts ||
                 button == btnAuditLogs ||
                 button == btnSystemConfiguration))
            {
                continue;
            }

            button.Top = nextY;
            nextY += button.Height + 8;
        }
    }

    private void btnDashboard_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        bool isAdmin = string.Equals(
            _userSession.Role,
            "ADMIN",
            StringComparison.OrdinalIgnoreCase);

        Label lblDashboard = new Label
        {
            Text = isAdmin ? "ADMIN Dashboard" : "OFFICER Dashboard",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(25, 25)
        };

        Label lblWelcome = new Label
        {
            Text = $"Welcome, {_userSession.Username}!",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.DimGray,
            AutoSize = true,
            Location = new Point(25, 80)
        };

        Label lblRole = new Label
        {
            Text = isAdmin
                ? "You are logged in as Administrator."
                : "You are logged in as Officer.",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(25, 120)
        };

        pnlContent.Controls.Add(lblDashboard);
        pnlContent.Controls.Add(lblWelcome);
        pnlContent.Controls.Add(lblRole);
    }

    private void btnAccounts_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        if (!_userSession.IsAuthenticated || _userSession.Role != "ADMIN")
        {
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
        accountControl.Dock = DockStyle.Fill; //cho man hinh tu gian theo kich thuoc

        pnlContent.Controls.Add(accountControl);
    }

    private void btnReports_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        ReportsControl reportsControl =
     _serviceProvider.GetRequiredService<ReportsControl>();
        reportsControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(reportsControl);
    }

    private void btnLogout_Click(object sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show(
            "Are you sure you want to log out?",
            "Confirm Logout",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _userSession.SignOut();
            CurrentUser = null;
            this.Close();
        }
    }
    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        await UpdateConnectionStatusAsync();
    }

    private async Task UpdateConnectionStatusAsync()
    {
        lblStatusConnection.Text = "Connection: Checking...";
        lblStatusConnection.ForeColor = Color.White;

        bool connected = await _databaseConnectionService.CheckConnectionAsync();

        if (connected)
        {
            lblStatusConnection.Text = "Connection: Connected";
            lblStatusConnection.ForeColor = Color.LightGreen;
        }
        else
        {
            lblStatusConnection.Text = "Connection: Disconnected";
            lblStatusConnection.ForeColor = Color.LightCoral;
        }
    }

    private void btnCitizen_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        CitizenControl citizenControl =
            _serviceProvider.GetRequiredService<CitizenControl>();
        citizenControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(citizenControl);
    }

    private void btnHousehold_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        HouseholdControl householdControl =
            _serviceProvider.GetRequiredService<HouseholdControl>();
        householdControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(householdControl);
    }

    private void btnTransfer_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        TransferControl transferControl = new TransferControl();
        transferControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(transferControl);
    }

    private async void btnAuditLogs_Click(object sender, EventArgs e)
    {
        // Only ADMIN can access Audit Logs.
        if (!_userSession.IsAuthenticated || _userSession.Role != "ADMIN")
        {
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

    private void btnSystemConfiguration_Click(object sender, EventArgs e)
    {
        if (!_userSession.IsAuthenticated ||
            !string.Equals(_userSession.Role, "ADMIN",
                StringComparison.OrdinalIgnoreCase))
        {
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
