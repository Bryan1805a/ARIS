
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

    public LoginForm(
     IAuthService authService,
     IServiceProvider serviceProvider,
     IUserSession userSession)
    {
        InitializeComponent();

        _authService = authService;
        _serviceProvider = serviceProvider;
        _userSession = userSession;
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
