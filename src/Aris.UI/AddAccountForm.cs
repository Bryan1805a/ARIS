using System.ComponentModel;

using Aris.Application.Accounts;
using Aris.Application.Authentication;

namespace Aris.UI;

public partial class AddAccountForm : Form {
    private readonly IAccountService _accountService;

    public AddAccountForm(IAccountService accountService) {
        InitializeComponent();
        _accountService = accountService;
        UiTheme.ApplyScreen(this);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    // Supplied by the account-management control so the service can attribute
    // the creation audit entry to the signed-in administrator.
    public LoginResult? CurrentUser { get; set; }

    private void btnCancel_Click(object sender, EventArgs e) {
        // Closing without setting DialogResult keeps the operation classified as
        // cancelled by the parent form.
        this.Close();
    }

    private async void btnSave_Click(object sender, EventArgs e) {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string fullName = txtFullName.Text.Trim();

        // These checks provide immediate field-level feedback; the application
        // service still performs the authoritative validation.
        if (string.IsNullOrWhiteSpace(username)) {
            MessageBox.Show("Please enter a username.");
            txtUsername.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(fullName)) {
            MessageBox.Show("Please enter a full name.");
            txtFullName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password)) {
            MessageBox.Show("Please enter a password.");
            txtPassword.Focus();
            return;
        }

        if (cboRole.SelectedIndex == -1) {
            MessageBox.Show("Please select a role.");
            cboRole.Focus();
            return;
        }

        btnSave.Enabled = false;

        try {
            // The session is checked at submission time because the form may
            // remain open after the original login state has changed.
            if (CurrentUser == null) {
                MessageBox.Show("User session not found.");
                return;
            }

            var result = await _accountService.CreateAccountAsync(
                username,
                fullName,
                password,
                cboRole.SelectedItem!.ToString()!,
                CurrentUser.UserId);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot create account",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // OK tells the parent to refresh its account list after the
            // transaction has completed successfully.
            MessageBox.Show(
                "Account created successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while creating the account: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            // Re-enable saving after both failures and successful returns.
            btnSave.Enabled = true;
        }
    }

    private void lblUsername_Click(object sender, EventArgs e) {
    }
}