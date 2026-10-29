
using System;
using System.Windows.Forms;

using Aris.Application.Accounts;
using Aris.Application.Authentication;
using Aris.Application.Common;

using Microsoft.Extensions.DependencyInjection;

namespace Aris.UI;

public partial class LoginForm : Form
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserSession _userSession;
    private readonly IBootstrapService _bootstrapService;
    private readonly Button _bootstrapButton = new();
    private bool _bootstrapPromptShown;

    public LoginForm(
     IAuthService authService,
     IServiceProvider serviceProvider,
     IUserSession userSession,
     IBootstrapService bootstrapService)
    {
        InitializeComponent();

        _authService = authService;
        _serviceProvider = serviceProvider;
        _userSession = userSession;
        _bootstrapService = bootstrapService;

        _bootstrapButton.Text = "Create first administrator";
        _bootstrapButton.Location = new Point(122, 300);
        _bootstrapButton.Size = new Size(240, 28);
        _bootstrapButton.Visible = false;
        _bootstrapButton.Click += BootstrapButton_Click;
        Controls.Add(_bootstrapButton);

        Shown += async (_, _) => await CheckForBootstrapAsync();
    }

    private async Task CheckForBootstrapAsync()
    {
        try
        {
            var hasAccounts = await _bootstrapService.HasAnyAccountsAsync();

            _bootstrapButton.Visible = !hasAccounts;

            if (!hasAccounts && !_bootstrapPromptShown)
            {
                _bootstrapPromptShown = true;
                MessageBox.Show(
                    "No accounts exist yet. Create the first administrator to continue.",
                    "First run",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _bootstrapButton.Visible = false;
            MessageBox.Show(
                $"Cannot check the database: {ex.Message}",
                "Database unavailable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BootstrapButton_Click(object? sender, EventArgs e)
    {
        using var form = new BootstrapForm(_bootstrapService);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _bootstrapButton.Visible = false;
            txtUsername.Text = form.CreatedUsername ?? string.Empty;
            txtPassword.Clear();
            txtPassword.Focus();
        }
    }

    private async void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter your username.");
            txtUsername.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Please enter your password.");
            txtPassword.Focus();
            return;
        }

        btnLogin.Enabled = false;

        try
        {
            var result = await _authService.LoginAsync(username, password);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join("\n", result.Errors),
                    "Login Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var user = result.Value;

            if (user.MustChangePassword)
            {
                var accountService =
                    _serviceProvider.GetRequiredService<IAccountService>();

                using var changePasswordForm =
                    new ChangePasswordForm(accountService, user.UserId);

                if (changePasswordForm.ShowDialog(this) != DialogResult.OK)
                {
                    MessageBox.Show(
                        "You must change your password before continuing.",
                        "Password Change Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }
            }

            MessageBox.Show("Login successful.");

            _userSession.SignIn(
                user.UserId,
                user.Username,
                user.Role
            );

            var mainForm = _serviceProvider.GetRequiredService<MainForm>();

            mainForm.SetCurrentUser(result.Value);

            mainForm.FormClosed += (s, e) =>
            {
                _userSession.SignOut();

                txtUsername.Clear();
                txtPassword.Clear();

                this.Show();
                txtUsername.Focus();
            };

            this.Hide();

            mainForm.Show();

        }
        catch (Exception)
        {
            MessageBox.Show(
                "An unexpected error occurred during login.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnLogin.Enabled = true;
        }
    }
}
