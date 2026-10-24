namespace Aris.UI;

partial class LoginForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblUsername = new Label();
        txtUsername = new TextBox();
        txtPassword = new TextBox();
        lblPassword = new Label();
        btnLogin = new Button();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblTitle.Location = new Point(82, 26);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(306, 30);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "ARIS MANAGEMENT SYSTEM";
        // 
        // lblUsername
        // 
        lblUsername.AutoSize = true;
        lblUsername.Location = new Point(38, 102);
        lblUsername.Name = "lblUsername";
        lblUsername.Size = new Size(60, 15);
        lblUsername.TabIndex = 1;
        lblUsername.Text = "Username";
        // 
        // txtUsername
        // 
        txtUsername.Location = new Point(138, 99);
        txtUsername.Name = "txtUsername";
        txtUsername.Size = new Size(300, 23);
        txtUsername.TabIndex = 2;
        // 
        // txtPassword
        // 
        txtPassword.Location = new Point(138, 154);
        txtPassword.Name = "txtPassword";
        txtPassword.Size = new Size(300, 23);
        txtPassword.TabIndex = 3;
        txtPassword.UseSystemPasswordChar = true;
        // 
        // lblPassword
        // 
        lblPassword.AutoSize = true;
        lblPassword.Location = new Point(41, 157);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(57, 15);
        lblPassword.TabIndex = 4;
        lblPassword.Text = "Password";
        // 
        // btnLogin
        // 
        btnLogin.BackColor = Color.SteelBlue;
        btnLogin.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnLogin.ForeColor = Color.White;
        btnLogin.Location = new Point(160, 257);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(140, 30);
        btnLogin.TabIndex = 5;
        btnLogin.Text = "Login";
        btnLogin.UseVisualStyleBackColor = false;
        btnLogin.Click += btnLogin_Click;
        // 
        // LoginForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(484, 361);
        Controls.Add(btnLogin);
        Controls.Add(lblPassword);
        Controls.Add(txtPassword);
        Controls.Add(txtUsername);
        Controls.Add(lblUsername);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ARIS - Login";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTitle;
    private Label lblUsername;
    private TextBox txtUsername;
    private TextBox txtPassword;
    private Label lblPassword;
    private Button btnLogin;
}