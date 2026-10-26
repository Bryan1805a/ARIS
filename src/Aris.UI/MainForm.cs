using Aris.Application.Accounts;
using Aris.Application.Auditing;
using Aris.Application.Authentication;
using Aris.Application.Common;
using Aris.Application.Interfaces;
using Aris.Application.Reports;

namespace Aris.UI;

public partial class MainForm : Form
{
    private readonly IAccountService _accountService;
    private readonly IDatabaseConnectionService _databaseConnectionService;
    private readonly IUserSession _userSession;
    private readonly IReportService _reportService;
    private readonly IAuditQueryService _auditQueryService;

    public MainForm(
    IAccountService accountService,
    IDatabaseConnectionService databaseConnectionService,
    IUserSession userSession,
    IReportService reportService,
    IAuditQueryService auditQueryService)
    {
        InitializeComponent();

        _accountService = accountService;
        _databaseConnectionService = databaseConnectionService;
        _userSession = userSession;
        _reportService = reportService;
        _auditQueryService = auditQueryService;

        this.Shown += MainForm_Shown;
    }

    public LoginResult? CurrentUser { get; private set; }

    public void SetCurrentUser(LoginResult user)
    {
        CurrentUser = user;

        lblStatusUser.Text = $"User: {_userSession.Username}";
        lblStatusRole.Text = $"Role: {_userSession.Role}";
    }

    private void btnDashboard_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        Label lblDashboard = new Label();
        lblDashboard.Text = "Dashboard";
        lblDashboard.Font = new Font("Segoe UI", 20, FontStyle.Bold);
        lblDashboard.ForeColor = Color.DarkSlateGray;
        lblDashboard.AutoSize = true;
        lblDashboard.Location = new Point(25, 25);

        pnlContent.Controls.Add(lblDashboard);
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
        AccountManagementControl accountControl = new AccountManagementControl(_accountService);
        accountControl.CurrentUser = CurrentUser;
        accountControl.Dock = DockStyle.Fill; //cho man hinh tu gian theo kich thuoc

        pnlContent.Controls.Add(accountControl);
    }

    private void btnReports_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        ReportsControl reportsControl = new ReportsControl(_reportService);
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

        CitizenControl citizenControl = new CitizenControl();
        citizenControl.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(citizenControl);
    }

    private void btnHousehold_Click(object sender, EventArgs e)
    {
        pnlContent.Controls.Clear();

        HouseholdControl householdControl = new HouseholdControl();
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

        var auditViewer = new AuditViewerControl(_auditQueryService);
        auditViewer.Dock = DockStyle.Fill;

        pnlContent.Controls.Add(auditViewer);

        await auditViewer.LoadAuditLogsAsync();
    }
}
