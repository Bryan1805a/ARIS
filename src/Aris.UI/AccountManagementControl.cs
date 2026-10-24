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

namespace Aris.UI;

public partial class AccountManagementControl : UserControl
{
    private readonly IAccountService _accountService;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LoginResult? CurrentUser { get; set; }

    public AccountManagementControl(IAccountService accountService)
    {
        InitializeComponent();
        _accountService = accountService;
        dgvAccounts.SelectionChanged += dgvAccounts_SelectionChanged;

        this.Load += AccountManagementControl_Load;
    }
    private async void AccountManagementControl_Load(object? sender, EventArgs e)
    {
        await LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();

            dgvAccounts.AutoGenerateColumns = false;

            dgvAccounts.DataSource = accounts.Select(a => new
            {
                a.UserId,
                a.Username,
                a.Role,
                a.Status,
                a.CreatedAt
            }).ToList();
            btnDisableAccount.Text = "Disable Account";
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Failed to load accounts.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private async void btnAddAccount_Click(object sender, EventArgs e)
    {
        using (AddAccountForm form = new AddAccountForm(_accountService))
        {
            form.CurrentUser = CurrentUser;

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                await LoadAccountsAsync();
            }
        }
    }

    private void btnEditAccount_Click(object sender, EventArgs e)
    {
        if (dgvAccounts.CurrentRow == null)
        {
            MessageBox.Show(
                "Please select an account to edit.",
                "No Account Selected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        using (EditAccountForm form = new EditAccountForm())
        {
            form.ShowDialog(this);
        }
    }

    private async void btnDisableAccount_Click(object sender, EventArgs e)
    {
        if (dgvAccounts.CurrentRow == null)
        {
            MessageBox.Show("Please select an account.");
            return;
        }

        int userId = Convert.ToInt32(
             dgvAccounts.CurrentRow.Cells[0].Value
        );
        if (CurrentUser == null)
        {
            MessageBox.Show("User session not found.");
            return;
        }

        if (CurrentUser.UserId == userId)
        {
            MessageBox.Show(
                "You cannot disable your own account.",
                "Action Not Allowed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        string status = Convert.ToString(
    dgvAccounts.CurrentRow.Cells[3].Value) ?? "";

        bool isLocked = status == "LOCKED";

        string newStatus = isLocked ? "ACTIVE" : "LOCKED";

        string action = isLocked ? "activate" : "disable";

        var confirm = MessageBox.Show(
            $"Are you sure you want to {action} this account?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            bool success = await _accountService
    .ChangeAccountStatusAsync(
        userId,
        newStatus,
        CurrentUser.UserId
    );

            if (success)
            {
                MessageBox.Show(
                    isLocked
                        ? "Account activated successfully."
                        : "Account disabled successfully."
                );
                await LoadAccountsAsync();
            }
            else
            {
                MessageBox.Show("Account not found.");
            }
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Failed to disable account.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void btnSearch_Click(object sender, EventArgs e)
    {
        string keyword = txtSearch.Text.Trim();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            MessageBox.Show(
                "Please enter a username to search.",
                "Search",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            txtSearch.Focus();
            return;
        }

        MessageBox.Show(
            "Search functionality will be available after database integration.",
            "Information",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

    }

    private void dgvAccounts_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvAccounts.CurrentRow == null)
            return;

        string status = Convert.ToString(
            dgvAccounts.CurrentRow.Cells[3].Value
        ) ?? "";

        if (status == "LOCKED")
        {
            btnDisableAccount.Text = "Activate Account";
        }
        else
        {
            btnDisableAccount.Text = "Disable Account";
        }
    }

    private async void btnResetPassword_Click(object sender, EventArgs e)
    {
        if (CurrentUser == null)
        {
            MessageBox.Show("Please log in first.");
            return;
        }

        if (dgvAccounts.CurrentRow == null ||
            dgvAccounts.CurrentRow.IsNewRow)
        {
            MessageBox.Show("Please select an account.");
            return;
        }

        int userId = Convert.ToInt32(
            dgvAccounts.CurrentRow.Cells["colUserId"].Value
        );

        using (ResetPasswordForm resetForm = new ResetPasswordForm())
        {
            if (resetForm.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                btnResetPassword.Enabled = false;

                bool success = await _accountService.ResetPasswordAsync(
                    userId,
                    resetForm.NewPassword,
                    CurrentUser.UserId
                );

                if (success)
                {
                    MessageBox.Show(
                        "Password reset successfully.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Account not found.",
                        "Reset Password Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Reset Password Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                btnResetPassword.Enabled = true;
            }
        }
    }
}
