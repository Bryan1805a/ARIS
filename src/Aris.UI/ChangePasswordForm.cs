using Aris.Application.Accounts;

namespace Aris.UI;

public partial class ChangePasswordForm : Form {
    private readonly IAccountService _accountService;
    private readonly int _userId;

    public ChangePasswordForm(
        IAccountService accountService,
        int userId) {
        InitializeComponent();

        _accountService = accountService;
        _userId = userId;

        // Keep the click handler in code so the form can pass the authenticated
        // user ID captured by the parent flow to the application service.
        btnChangePassword.Click += btnChangePassword_Click;
    }

    private void ChangePasswordForm_Load(
        object sender, EventArgs e) {
        // Start with the first required field ready for keyboard input.
        txtNewPassword.Focus();
    }

    private async void btnChangePassword_Click(
        object? sender, EventArgs e) {
        string currentPassword = txtCurrentPassword.Text;
        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        // The form checks interaction-level requirements; password policy and
        // credential verification remain centralized in the application service.
        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword)) {
            MessageBox.Show(
                "Please fill in all password fields.");
            return;
        }

        if (newPassword != confirmPassword) {
            // Catch a mistyped confirmation before making a service call.
            MessageBox.Show(
                "Passwords do not match.");
            return;
        }

        btnChangePassword.Enabled = false;

        try {
            // The service verifies the current secret, hashes the replacement,
            // updates the forced-change state, and records the audit event.
            var result = await _accountService.ChangeOwnPasswordAsync(
                _userId,
                currentPassword,
                newPassword);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot change password",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                "Password changed successfully.");

            // DialogResult lets the caller distinguish a committed change from
            // cancellation or a validation/service failure.
            DialogResult = DialogResult.OK;
            Close();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while changing the password: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            // Restore the button if the operation fails so the user can retry.
            btnChangePassword.Enabled = true;
        }
    }

    private void txtNewPassword_TextChanged(
        object sender, EventArgs e) {
    }

    private void textBox1_TextChanged(object sender, EventArgs e) {
    }

    private void txtConfirmPassword_TextChanged(object sender, EventArgs e) {
    }
}