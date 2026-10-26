namespace Aris.UI;

partial class ChangePasswordForm
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
        lblNewPassword = new Label();
        lblConfirmPassword = new Label();
        txtNewPassword = new TextBox();
        txtConfirmPassword = new TextBox();
        btnChangePassword = new Button();
        lblCurrentPassword = new Label();
        txtCurrentPassword = new TextBox();
        SuspendLayout();
        //
        // lblTitle
        //
        lblTitle.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblTitle.ForeColor = Color.MidnightBlue;
        lblTitle.Location = new Point(120, 25);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(210, 35);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Change Password";
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        //
        // lblNewPassword
        //
        lblNewPassword.AutoSize = true;
        lblNewPassword.Location = new Point(40, 160);
        lblNewPassword.Name = "lblNewPassword";
        lblNewPassword.Size = new Size(94, 17);
        lblNewPassword.TabIndex = 1;
        lblNewPassword.Text = "New Password";
        //
        // lblConfirmPassword
        //
        lblConfirmPassword.AutoSize = true;
        lblConfirmPassword.Location = new Point(40, 235);
        lblConfirmPassword.Name = "lblConfirmPassword";
        lblConfirmPassword.Size = new Size(114, 17);
        lblConfirmPassword.TabIndex = 2;
        lblConfirmPassword.Text = "Confirm Password";
        //
        // txtNewPassword
        //
        txtNewPassword.BackColor = Color.White;
        txtNewPassword.BorderStyle = BorderStyle.FixedSingle;
        txtNewPassword.ForeColor = Color.Black;
        txtNewPassword.Location = new Point(40, 187);
        txtNewPassword.Name = "txtNewPassword";
        txtNewPassword.Size = new Size(350, 25);
        txtNewPassword.TabIndex = 3;
        txtNewPassword.UseSystemPasswordChar = true;
        txtNewPassword.TextChanged += this.txtNewPassword_TextChanged;
        //
        // txtConfirmPassword
        //
        txtConfirmPassword.BackColor = Color.White;
        txtConfirmPassword.BorderStyle = BorderStyle.FixedSingle;
        txtConfirmPassword.ForeColor = Color.Black;
        txtConfirmPassword.Location = new Point(40, 262);
        txtConfirmPassword.Name = "txtConfirmPassword";
        txtConfirmPassword.Size = new Size(350, 25);
        txtConfirmPassword.TabIndex = 4;
        txtConfirmPassword.UseSystemPasswordChar = true;
        txtConfirmPassword.TextChanged += txtConfirmPassword_TextChanged;
        //
        // btnChangePassword
        //
        btnChangePassword.BackColor = Color.SteelBlue;
        btnChangePassword.FlatStyle = FlatStyle.Flat;
        btnChangePassword.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnChangePassword.ForeColor = Color.White;
        btnChangePassword.Location = new Point(125, 320);
        btnChangePassword.Name = "btnChangePassword";
        btnChangePassword.Size = new Size(180, 40);
        btnChangePassword.TabIndex = 5;
        btnChangePassword.Text = "Change Password";
        btnChangePassword.UseVisualStyleBackColor = false;
        //
        // lblCurrentPassword
        //
        lblCurrentPassword.AutoSize = true;
        lblCurrentPassword.Location = new Point(40, 85);
        lblCurrentPassword.Name = "lblCurrentPassword";
        lblCurrentPassword.Size = new Size(111, 17);
        lblCurrentPassword.TabIndex = 7;
        lblCurrentPassword.Text = "Current Password";
        //
        // txtCurrentPassword
        //
        txtCurrentPassword.BackColor = Color.White;
        txtCurrentPassword.BorderStyle = BorderStyle.FixedSingle;
        txtCurrentPassword.ForeColor = Color.Black;
        txtCurrentPassword.Location = new Point(40, 122);
        txtCurrentPassword.Name = "txtCurrentPassword";
        txtCurrentPassword.Size = new Size(350, 25);
        txtCurrentPassword.TabIndex = 8;
        txtCurrentPassword.UseSystemPasswordChar = true;
        //
        // ChangePasswordForm
        //
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(434, 381);
        Controls.Add(txtCurrentPassword);
        Controls.Add(lblCurrentPassword);
        Controls.Add(btnChangePassword);
        Controls.Add(txtConfirmPassword);
        Controls.Add(txtNewPassword);
        Controls.Add(lblConfirmPassword);
        Controls.Add(lblNewPassword);
        Controls.Add(lblTitle);
        Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        ForeColor = Color.Black;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ChangePasswordForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Change Password";
        Load += ChangePasswordForm_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTitle;
    private Label lblNewPassword;
    private Label lblConfirmPassword;
    private TextBox txtNewPassword;
    private TextBox txtConfirmPassword;
    private Button btnChangePassword;
    private Label lblCurrentPassword;
    private TextBox txtCurrentPassword;
}