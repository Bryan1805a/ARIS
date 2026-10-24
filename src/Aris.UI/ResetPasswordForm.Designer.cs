namespace Aris.UI;

partial class ResetPasswordForm
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
        lblNewPassword = new Label();
        lblConfirmPassword = new Label();
        txtNewPassword = new TextBox();
        txtConfirmPassword = new TextBox();
        btnCancel = new Button();
        btnSave = new Button();
        SuspendLayout();
        // 
        // lblNewPassword
        // 
        lblNewPassword.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblNewPassword.ForeColor = Color.FromArgb(40, 40, 40);
        lblNewPassword.Location = new Point(30, 25);
        lblNewPassword.Name = "lblNewPassword";
        lblNewPassword.Size = new Size(160, 25);
        lblNewPassword.TabIndex = 0;
        lblNewPassword.Text = "New Password";
        // 
        // lblConfirmPassword
        // 
        lblConfirmPassword.AutoSize = true;
        lblConfirmPassword.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblConfirmPassword.ForeColor = Color.FromArgb(40, 40, 40);
        lblConfirmPassword.Location = new Point(30, 100);
        lblConfirmPassword.Name = "lblConfirmPassword";
        lblConfirmPassword.Size = new Size(120, 17);
        lblConfirmPassword.TabIndex = 1;
        lblConfirmPassword.Text = "Confirm Password";
        // 
        // txtNewPassword
        // 
        txtNewPassword.BorderStyle = BorderStyle.FixedSingle;
        txtNewPassword.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtNewPassword.Location = new Point(30, 55);
        txtNewPassword.Name = "txtNewPassword";
        txtNewPassword.Size = new Size(380, 27);
        txtNewPassword.TabIndex = 2;
        txtNewPassword.UseSystemPasswordChar = true;
        // 
        // txtConfirmPassword
        // 
        txtConfirmPassword.BorderStyle = BorderStyle.FixedSingle;
        txtConfirmPassword.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtConfirmPassword.Location = new Point(30, 130);
        txtConfirmPassword.Name = "txtConfirmPassword";
        txtConfirmPassword.Size = new Size(380, 27);
        txtConfirmPassword.TabIndex = 3;
        txtConfirmPassword.UseSystemPasswordChar = true;
        // 
        // btnCancel
        // 
        btnCancel.BackColor = Color.Gainsboro;
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnCancel.ForeColor = Color.Black;
        btnCancel.Location = new Point(190, 200);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(100, 40);
        btnCancel.TabIndex = 4;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = false;
        btnCancel.Click += btnCancel_Click;
        // 
        // btnSave
        // 
        btnSave.BackColor = Color.Firebrick;
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnSave.ForeColor = Color.White;
        btnSave.Location = new Point(310, 200);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(100, 40);
        btnSave.TabIndex = 5;
        btnSave.Text = "Save";
        btnSave.UseVisualStyleBackColor = false;
        btnSave.Click += btnSave_Click;
        // 
        // ResetPasswordForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(434, 296);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
        Controls.Add(txtConfirmPassword);
        Controls.Add(txtNewPassword);
        Controls.Add(lblConfirmPassword);
        Controls.Add(lblNewPassword);
        Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Name = "ResetPasswordForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Reset Password";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblNewPassword;
    private Label lblConfirmPassword;
    private TextBox txtNewPassword;
    private TextBox txtConfirmPassword;
    private Button btnCancel;
    private Button btnSave;
}