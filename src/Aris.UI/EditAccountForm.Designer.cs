namespace Aris.UI;

partial class EditAccountForm
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
        lblRole = new Label();
        txtUsername = new TextBox();
        cboRole = new ComboBox();
        lblStatus = new Label();
        cboStatus = new ComboBox();
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
        // lblRole
        // 
        lblRole.AutoSize = true;
        lblRole.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblRole.Location = new Point(40, 150);
        lblRole.Name = "lblRole";
        lblRole.Size = new Size(39, 20);
        lblRole.TabIndex = 1;
        lblRole.Text = "Role";
        // 
        // txtUsername
        // 
        txtUsername.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtUsername.Location = new Point(40, 100);
        txtUsername.Name = "txtUsername";
        txtUsername.PlaceholderText = "Enter username";
        txtUsername.Size = new Size(400, 27);
        txtUsername.TabIndex = 2;
        // 
        // cboRole
        // 
        cboRole.DropDownStyle = ComboBoxStyle.DropDownList;
        cboRole.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        cboRole.FormattingEnabled = true;
        cboRole.Items.AddRange(new object[] { "Admin", "Officer" });
        cboRole.Location = new Point(40, 180);
        cboRole.Name = "cboRole";
        cboRole.Size = new Size(400, 28);
        cboRole.TabIndex = 3;
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblStatus.Location = new Point(40, 230);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(49, 20);
        lblStatus.TabIndex = 4;
        lblStatus.Text = "Status";
        // 
        // cboStatus
        // 
        cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboStatus.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        cboStatus.FormattingEnabled = true;
        cboStatus.Items.AddRange(new object[] { "Active", "Disable" });
        cboStatus.Location = new Point(40, 260);
        cboStatus.Name = "cboStatus";
        cboStatus.Size = new Size(400, 28);
        cboStatus.TabIndex = 5;
        // 
        // btnSave
        // 
        btnSave.BackColor = Color.DarkSlateGray;
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.ForeColor = Color.White;
        btnSave.Location = new Point(100, 340);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(130, 40);
        btnSave.TabIndex = 6;
        btnSave.Text = "Save Changes";
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
        // EditAccountForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.WhiteSmoke;
        ClientSize = new Size(484, 411);
        Controls.Add(btnCancel);
        Controls.Add(btnSave);
        Controls.Add(cboStatus);
        Controls.Add(lblStatus);
        Controls.Add(cboRole);
        Controls.Add(txtUsername);
        Controls.Add(lblRole);
        Controls.Add(lblUsername);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "EditAccountForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Edit Account";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblUsername;
    private Label lblRole;
    private TextBox txtUsername;
    private ComboBox cboRole;
    private Label lblStatus;
    private ComboBox cboStatus;
    private Button btnSave;
    private Button btnCancel;
}