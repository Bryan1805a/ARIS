using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Aris.Application.Accounts;
using Aris.Application.Authentication;
using Aris.Application.Accounts.Dtos;

namespace Aris.UI;

public partial class AccountManagementControl : UserControl {
    private readonly IAccountService _accountService;
    private readonly IDemoDataService _demoDataService;
    private readonly Button _seedDemoButton = new();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LoginResult? CurrentUser { get; set; }

    public AccountManagementControl(
        IAccountService accountService,
        IDemoDataService demoDataService) {
        InitializeComponent();
        _accountService = accountService;
        _demoDataService = demoDataService;
        dgvAccounts.SelectionChanged += dgvAccounts_SelectionChanged;

        // Keep demo seeding separate from normal account administration because
        // it creates multiple related records and is intended for setup only.
        _seedDemoButton.Text = "Seed demo data";
        _seedDemoButton.Location = new Point(25, 580);
        _seedDemoButton.Size = new Size(180, 35);
        _seedDemoButton.BackColor = Color.DarkSlateGray;
        _seedDemoButton.ForeColor = Color.White;
        _seedDemoButton.FlatStyle = FlatStyle.Flat;
        _seedDemoButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _seedDemoButton.Click += SeedDemoButton_Click;
        Controls.Add(_seedDemoButton);

        this.Load += AccountManagementControl_Load;
        UiTheme.ApplyScreen(this);
    }

    private async void SeedDemoButton_Click(object? sender, EventArgs e) {
        if (CurrentUser is null) {
            return;
        }

        // Seeding is irreversible sample-data setup, so require explicit
        // confirmation before disabling the button and calling the service.
        var confirmation = MessageBox.Show(
            "Seed demo addresses, citizens and households? " +
            "This is sample data, it respects the residency invariant, and it can only be run once.",
            "Seed demo data",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes) {
            return;
        }

        _seedDemoButton.Enabled = false;

        try {
            // The application service owns the one-time check and transaction;
            // the control only presents the result to the operator.
            var result = await _demoDataService.SeedAsync(CurrentUser.UserId);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot seed demo data",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (result.Value is { } summary) {
                MessageBox.Show(
                    $"Seeded {summary.Addresses} addresses, {summary.Citizens} citizens and " +
                    $"{summary.Households} households. One citizen is UNASSIGNED for the " +
                    "registration demo.",
                    "Demo data seeded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while seeding demo data: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            _seedDemoButton.Enabled = true;
        }
    }
    private async void AccountManagementControl_Load(object? sender, EventArgs e) {
        // Load the list once the control has been initialized and has a session.
        await LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync() {
        try {
            if (CurrentUser == null) {
                MessageBox.Show(
                    "You must log in to view accounts.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Authorization is enforced by the service as well; the UI check
            // provides a clearer message when the control has no active session.
            var result = await _accountService.GetAllAccountsAsync(
                CurrentUser.UserId);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot load accounts",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var accounts = result.Value ?? new List<AccountDto>();

            // Bind a display projection rather than domain/application objects
            // so the grid exposes only account-management fields.
            dgvAccounts.AutoGenerateColumns = false;

            dgvAccounts.DataSource = accounts.Select(a => new {
                a.UserId,
                a.Username,
                a.Role,
                a.Status,
                a.CreatedAt
            }).ToList();
            btnDisableAccount.Text = "Disable Account";
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while loading accounts: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void btnAddAccount_Click(object sender, EventArgs e) {
        using (AddAccountForm form = new AddAccountForm(_accountService)) {
            form.CurrentUser = CurrentUser;

            // Refresh only after the child form reports a successful creation.
            if (form.ShowDialog(this) == DialogResult.OK) {
                await LoadAccountsAsync();
            }
        }
    }


    private async void btnEditAccount_Click(object sender, EventArgs e) {
        if (CurrentUser == null) {
            MessageBox.Show("Please log in first.");
            return;
        }

        if (dgvAccounts.CurrentRow == null ||
            dgvAccounts.CurrentRow.IsNewRow) {
            MessageBox.Show("Please select an account to edit.");
            return;
        }

        int userId = Convert.ToInt32(
            dgvAccounts.CurrentRow.Cells["colUserId"].Value);

        var result = await _accountService.GetAllAccountsAsync(
            CurrentUser.UserId);

        if (result.IsFailure) {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Errors),
                "Cannot load account",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var account = result.Value?.FirstOrDefault(
            a => a.UserId == userId);

        if (account == null) {
            MessageBox.Show("Account not found.");
            return;
        }

        using (var form = new EditAccountForm(
            _accountService,
            account.UserId,
            CurrentUser.UserId,
            account.Username,
            account.Role,
            account.Status)) {
            if (form.ShowDialog(this) == DialogResult.OK) {
                await LoadAccountsAsync();
            }
        }
    }


    private async void btnDisableAccount_Click(object sender, EventArgs e) {
        if (dgvAccounts.CurrentRow == null) {
            MessageBox.Show("Please select an account.");
            return;
        }

        int userId = Convert.ToInt32(
             dgvAccounts.CurrentRow.Cells[0].Value
        );
        if (CurrentUser == null) {
            MessageBox.Show("User session not found.");
            return;
        }

        if (CurrentUser.UserId == userId) {
            MessageBox.Show(
                "You cannot disable your own account.",
                "Action Not Allowed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        string status = Convert.ToString(dgvAccounts.CurrentRow.Cells[3].Value) ?? "";

        bool isLocked = status == "LOCKED";

        string newStatus = isLocked ? "ACTIVE" : "LOCKED";

        string action = isLocked ? "activate" : "disable";

        // The same command toggles the two supported states, while the
        // confirmation text reflects the direction of the transition.
        var confirm = MessageBox.Show(
            $"Are you sure you want to {action} this account?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes) {
            return;
        }

        try {
            // The service performs the authorization, self-lock protection,
            // persistence, and audit logging for this state transition.
            var result = await _accountService.ChangeAccountStatusAsync(
                userId,
                newStatus,
                CurrentUser.UserId);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot change account status",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                isLocked
                    ? "Account activated successfully."
                    : "Account disabled successfully.");

            // Rebind after the mutation so status text and row data are current.
            await LoadAccountsAsync();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while changing the account status: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private async void btnSearch_Click(object sender, EventArgs e) {
        string keyword = txtSearch.Text.Trim();

        if (CurrentUser == null) {
            MessageBox.Show("Please log in first.");
            return;
        }

        try {
            var result = await _accountService.GetAllAccountsAsync(
                CurrentUser.UserId);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Search Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var accounts = result.Value ?? new List<AccountDto>();

            var filteredAccounts = accounts
                .Where(a => a.Username.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase))
                .Select(a => new {
                    a.UserId,
                    a.Username,
                    a.Role,
                    a.Status,
                    a.CreatedAt
                })
                .ToList();

            dgvAccounts.DataSource = filteredAccounts;
        } catch (Exception ex) {
            MessageBox.Show(
                $"Search failed: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void dgvAccounts_SelectionChanged(object? sender, EventArgs e) {
        if (dgvAccounts.CurrentRow == null)
            return;

        string status = Convert.ToString(
            dgvAccounts.CurrentRow.Cells[3].Value
        ) ?? "";

        // Keep the action label synchronized with the selected account's state.
        if (status == "LOCKED") {
            btnDisableAccount.Text = "Activate Account";
        } else {
            btnDisableAccount.Text = "Disable Account";
        }
    }

    private async void btnResetPassword_Click(object sender, EventArgs e) {
        if (CurrentUser == null) {
            MessageBox.Show("Please log in first.");
            return;
        }

        if (dgvAccounts.CurrentRow == null ||
            dgvAccounts.CurrentRow.IsNewRow) {
            MessageBox.Show("Please select an account.");
            return;
        }

        int userId = Convert.ToInt32(
            dgvAccounts.CurrentRow.Cells["colUserId"].Value
        );

        // Let the password dialog collect and validate the secret; this control
        // passes it to the service only after the operator confirms the dialog.
        using (ResetPasswordForm resetForm = new ResetPasswordForm()) {
            if (resetForm.ShowDialog(this) != DialogResult.OK)
                return;

            try {
                btnResetPassword.Enabled = false;

                // Password hashing, forced-change policy, and audit details stay
                // inside the application service rather than the UI layer.
                var result = await _accountService.ResetPasswordAsync(
                    userId,
                    resetForm.NewPassword,
                    CurrentUser.UserId);

                if (result.IsFailure) {
                    MessageBox.Show(
                        string.Join(Environment.NewLine, result.Errors),
                        "Reset Password Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(
                    "Password reset successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            } catch (Exception ex) {
                MessageBox.Show(
                    $"Unexpected error while resetting the password: {ex.Message}",
                    "Reset Password Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            } finally {
                btnResetPassword.Enabled = true;
            }
        }
    }
}
