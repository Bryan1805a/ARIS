namespace Aris.UI;

partial class AddAccountForm
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
        lblUsername = new Label();
        txtUsername = new TextBox();
        txtPassword = new TextBox();
        lblPassword = new Label();
        lblRole = new Label();
        cboRole = new ComboBox();
        btnSave = new Button();
        btnCancel = new Button();
        SuspendLayout();
        // 
        // lblUsername
        // 
        lblUsername.AutoSize = true;
        lblUsername.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblUsername.Location = new Point(40, 70);
        lblUsername.Name = "lblUsername";
        lblUsername.Size = new Size(75, 20);
        lblUsername.TabIndex = 0;
        lblUsername.Text = "Username";
        // 
        // txtUsername
        // 
        txtUsername.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtUsername.Location = new Point(40, 100);
        txtUsername.Name = "txtUsername";
        txtUsername.PlaceholderText = "Enter username";
        txtUsername.Size = new Size(400, 27);
        txtUsername.TabIndex = 1;
        // 
        // txtPassword
        // 
        txtPassword.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtPassword.Location = new Point(40, 180);
        txtPassword.Name = "txtPassword";
        txtPassword.PlaceholderText = "Enter password";
        txtPassword.Size = new Size(400, 27);
        txtPassword.TabIndex = 2;
        txtPassword.UseSystemPasswordChar = true;
        // 
        // lblPassword
        // 
        lblPassword.AutoSize = true;
        lblPassword.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblPassword.Location = new Point(40, 150);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(70, 20);
        lblPassword.TabIndex = 3;
        lblPassword.Text = "Password";
        // 
        // lblRole
        // 
        lblRole.AutoSize = true;
        lblRole.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblRole.Location = new Point(40, 230);
        lblRole.Name = "lblRole";
        lblRole.Size = new Size(39, 20);
        lblRole.TabIndex = 4;
        lblRole.Text = "Role";
        // 
        // cboRole
        // 
        cboRole.DropDownStyle = ComboBoxStyle.DropDownList;
        cboRole.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        cboRole.FormattingEnabled = true;
        cboRole.Items.AddRange(new object[] { "Admin", "Officer" });
        cboRole.Location = new Point(40, 260);
        cboRole.Name = "cboRole";
        cboRole.Size = new Size(400, 28);
        cboRole.TabIndex = 5;
        // 
        // btnSave
        // 
        btnSave.BackColor = Color.DarkSlateGray;
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.ForeColor = Color.White;
        btnSave.Location = new Point(100, 340);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(120, 40);
        btnSave.TabIndex = 6;
        btnSave.Text = "Save";
        btnSave.UseVisualStyleBackColor = false;
        btnSave.Click += btnSave_Click;
        // 
        // btnCancel
        // 
        btnCancel.BackColor = Color.Gray;
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.ForeColor = Color.White;
        btnCancel.Location = new Point(250, 340);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(120, 40);
        btnCancel.TabIndex = 7;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = false;
        btnCancel.Click += btnCancel_Click;
        // 
        // AddAccountForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.WhiteSmoke;
        ClientSize = new Size(484, 411);
        Controls.Add(btnCancel);
        Controls.Add(btnSave);
        Controls.Add(cboRole);
        Controls.Add(lblRole);
        Controls.Add(lblPassword);
        Controls.Add(txtPassword);
        Controls.Add(txtUsername);
        Controls.Add(lblUsername);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Name = "AddAccountForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Add Account";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblUsername;
    private TextBox txtUsername;
    private TextBox txtPassword;
    private Label lblPassword;
    private Label lblRole;
    private ComboBox cboRole;
    private Button btnSave;
    private Button btnCancel;
}