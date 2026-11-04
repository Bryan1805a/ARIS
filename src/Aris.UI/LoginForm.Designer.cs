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
        pnlAccent = new Panel();
        pnlCard = new Panel();
        lblTitle = new Label();
        lblSubtitle = new Label();
        lblUsername = new Label();
        txtUsername = new TextBox();
        txtPassword = new TextBox();
        lblPassword = new Label();
        btnLogin = new Button();
        pnlCard.SuspendLayout();
        SuspendLayout();
        //
        // pnlAccent
        //
        pnlAccent.BackColor = Color.FromArgb(37, 99, 235);
        pnlAccent.Dock = DockStyle.Top;
        pnlAccent.Location = new Point(0, 0);
        pnlAccent.Name = "pnlAccent";
        pnlAccent.Size = new Size(520, 6);
        pnlAccent.TabIndex = 0;
        //
        // pnlCard
        //
        pnlCard.BackColor = Color.White;
        pnlCard.Controls.Add(lblTitle);
        pnlCard.Controls.Add(lblSubtitle);
        pnlCard.Controls.Add(lblUsername);
        pnlCard.Controls.Add(txtUsername);
        pnlCard.Controls.Add(txtPassword);
        pnlCard.Controls.Add(lblPassword);
        pnlCard.Controls.Add(btnLogin);
        pnlCard.Location = new Point(40, 44);
        pnlCard.Name = "pnlCard";
        pnlCard.Size = new Size(440, 370);
        pnlCard.TabIndex = 1;
        //
        // lblTitle
        //
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
        lblTitle.Location = new Point(30, 22);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(82, 44);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "ARIS";
        //
        // lblSubtitle
        //
        lblSubtitle.AutoSize = true;
        lblSubtitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
        lblSubtitle.Location = new Point(32, 68);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new Size(234, 19);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "RESIDENCE MANAGEMENT SYSTEM";
        //
        // lblUsername
        //
        lblUsername.AutoSize = true;
        lblUsername.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblUsername.ForeColor = Color.FromArgb(51, 65, 85);
        lblUsername.Location = new Point(30, 112);
        lblUsername.Name = "lblUsername";
        lblUsername.Size = new Size(64, 17);
        lblUsername.TabIndex = 2;
        lblUsername.Text = "Username";
        //
        // txtUsername
        //
        txtUsername.BorderStyle = BorderStyle.FixedSingle;
        txtUsername.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtUsername.Location = new Point(30, 134);
        txtUsername.Name = "txtUsername";
        txtUsername.Size = new Size(380, 25);
        txtUsername.TabIndex = 3;
        //
        // txtPassword
        //
        txtPassword.BorderStyle = BorderStyle.FixedSingle;
        txtPassword.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtPassword.Location = new Point(30, 202);
        txtPassword.Name = "txtPassword";
        txtPassword.Size = new Size(380, 25);
        txtPassword.TabIndex = 4;
        txtPassword.UseSystemPasswordChar = true;
        //
        // lblPassword
        //
        lblPassword.AutoSize = true;
        lblPassword.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblPassword.ForeColor = Color.FromArgb(51, 65, 85);
        lblPassword.Location = new Point(30, 180);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(61, 17);
        lblPassword.TabIndex = 5;
        lblPassword.Text = "Password";
        //
        // btnLogin
        //
        btnLogin.BackColor = Color.FromArgb(37, 99, 235);
        btnLogin.FlatStyle = FlatStyle.Flat;
        btnLogin.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnLogin.ForeColor = Color.White;
        btnLogin.Location = new Point(30, 258);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(380, 44);
        btnLogin.TabIndex = 6;
        btnLogin.Text = "Login";
        btnLogin.UseVisualStyleBackColor = false;
        btnLogin.Click += btnLogin_Click;
        //
        // LoginForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(241, 245, 249);
        ClientSize = new Size(520, 470);
        Controls.Add(pnlCard);
        Controls.Add(pnlAccent);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ARIS - Login";
        AcceptButton = btnLogin;
        pnlCard.ResumeLayout(false);
        pnlCard.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private Panel pnlAccent;
    private Panel pnlCard;
    private Label lblTitle;
    private Label lblSubtitle;
    private Label lblUsername;
    private TextBox txtUsername;
    private TextBox txtPassword;
    private Label lblPassword;
    private Button btnLogin;
}
