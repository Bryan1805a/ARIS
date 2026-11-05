using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Aris.Application.Accounts;

namespace Aris.UI;

public partial class EditAccountForm : Form
{
    private readonly IAccountService _accountService;
    private readonly int _userId;
    private readonly int _currentUserId;

    public EditAccountForm(
        IAccountService accountService,
        int userId,
        int currentUserId,
        string username,
        string role,
        string status) {
        InitializeComponent();

        _accountService = accountService;
        _userId = userId;
        _currentUserId = currentUserId;

        txtUsername.Text = username;
        cboRole.SelectedItem = role;
        cboStatus.SelectedItem = status;

        // FullName will be used when connecting the edit fields.
        UiTheme.ApplyScreen(this);
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.Close();
    }


    private async void btnSave_Click(object sender, EventArgs e) {
        string username = txtUsername.Text.Trim();
        string role = cboRole.Text.Trim().ToUpperInvariant();
        string status = cboStatus.Text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(username)) {
            MessageBox.Show("Please enter a username.");
            txtUsername.Focus();
            return;
        }

        if (role is not ("ADMIN" or "OFFICER")) {
            MessageBox.Show("Please select a valid role.");
            return;
        }

        if (status is not ("ACTIVE" or "LOCKED")) {
            MessageBox.Show("Please select a valid status.");
            return;
        }

        try {
            btnSave.Enabled = false;

            var accountsResult = await _accountService
                .GetAllAccountsAsync(_currentUserId);

            if (accountsResult.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, accountsResult.Errors));
                return;
            }

            var original = accountsResult.Value?
                .FirstOrDefault(a => a.UserId == _userId);

            if (original == null) {
                MessageBox.Show("Account not found.");
                return;
            }

            if (original.Username != username ||
                original.Role != role) {
                var updateResult = await _accountService.UpdateAccountAsync(
                    _userId,
                    username,
                    role,
                    _currentUserId);

                if (updateResult.IsFailure) {
                    MessageBox.Show(
                        string.Join(Environment.NewLine, updateResult.Errors),
                        "Update Failed");
                    return;
                }
            }

            if (original.Status != status) {
                var statusResult = await _accountService
                    .ChangeAccountStatusAsync(
                        _userId,
                        status,
                        _currentUserId);

                if (statusResult.IsFailure) {
                    MessageBox.Show(
                        string.Join(Environment.NewLine, statusResult.Errors),
                        "Status Update Failed");
                    return;
                }
            }

            MessageBox.Show(
                "Account updated successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unable to update account: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            btnSave.Enabled = true;
        }
    }

}
