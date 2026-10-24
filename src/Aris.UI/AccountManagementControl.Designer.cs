namespace Aris.UI;

partial class AccountManagementControl
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        lblAccountsTitle = new Label();
        txtSearch = new TextBox();
        btnSearch = new Button();
        dgvAccounts = new DataGridView();
        colUserId = new DataGridViewTextBoxColumn();
        colUsername = new DataGridViewTextBoxColumn();
        colRole = new DataGridViewTextBoxColumn();
        colStatus = new DataGridViewTextBoxColumn();
        colCreatedAt = new DataGridViewTextBoxColumn();
        btnAddAccount = new Button();
        btnEditAccount = new Button();
        btnDisableAccount = new Button();
        btnResetPassword = new Button();
        ((System.ComponentModel.ISupportInitialize)dgvAccounts).BeginInit();
        SuspendLayout();
        // 
        // lblAccountsTitle
        // 
        lblAccountsTitle.AutoSize = true;
        lblAccountsTitle.BackColor = Color.Transparent;
        lblAccountsTitle.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblAccountsTitle.ForeColor = Color.DarkSlateGray;
        lblAccountsTitle.Location = new Point(25, 25);
        lblAccountsTitle.Name = "lblAccountsTitle";
        lblAccountsTitle.Size = new Size(300, 37);
        lblAccountsTitle.TabIndex = 0;
        lblAccountsTitle.Text = "Account Management";
        // 
        // txtSearch
        // 
        txtSearch.BorderStyle = BorderStyle.FixedSingle;
        txtSearch.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtSearch.Location = new Point(25, 90);
        txtSearch.Name = "txtSearch";
        txtSearch.PlaceholderText = "Search by username...";
        txtSearch.Size = new Size(300, 27);
        txtSearch.TabIndex = 1;
        // 
        // btnSearch
        // 
        btnSearch.BackColor = Color.DarkSlateGray;
        btnSearch.FlatStyle = FlatStyle.Flat;
        btnSearch.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        btnSearch.ForeColor = Color.White;
        btnSearch.Location = new Point(340, 88);
        btnSearch.Name = "btnSearch";
        btnSearch.Size = new Size(100, 35);
        btnSearch.TabIndex = 2;
        btnSearch.Text = "Search";
        btnSearch.UseVisualStyleBackColor = false;
        btnSearch.Click += btnSearch_Click;
        // 
        // dgvAccounts
        // 
        dgvAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvAccounts.BackgroundColor = Color.White;
        dgvAccounts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvAccounts.Columns.AddRange(new DataGridViewColumn[] { colUserId, colUsername, colRole, colStatus, colCreatedAt });
        dgvAccounts.Location = new Point(25, 150);
        dgvAccounts.MultiSelect = false;
        dgvAccounts.Name = "dgvAccounts";
        dgvAccounts.ReadOnly = true;
        dgvAccounts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvAccounts.Size = new Size(740, 350);
        dgvAccounts.TabIndex = 3;
        // 
        // colUserId
        // 
        colUserId.DataPropertyName = "UserId";
        colUserId.HeaderText = "User ID";
        colUserId.Name = "colUserId";
        colUserId.ReadOnly = true;
        // 
        // colUsername
        // 
        colUsername.DataPropertyName = "Username";
        colUsername.HeaderText = "Username";
        colUsername.Name = "colUsername";
        colUsername.ReadOnly = true;
        // 
        // colRole
        // 
        colRole.DataPropertyName = "Role";
        colRole.HeaderText = "Role";
        colRole.Name = "colRole";
        colRole.ReadOnly = true;
        // 
        // colStatus
        // 
        colStatus.DataPropertyName = "Status";
        colStatus.HeaderText = "Status";
        colStatus.Name = "colStatus";
        colStatus.ReadOnly = true;
        // 
        // colCreatedAt
        // 
        colCreatedAt.DataPropertyName = "CreatedAt";
        dataGridViewCellStyle1.Format = "dd/MM/yyyy HH:mm";
        colCreatedAt.DefaultCellStyle = dataGridViewCellStyle1;
        colCreatedAt.HeaderText = "Created At";
        colCreatedAt.Name = "colCreatedAt";
        colCreatedAt.ReadOnly = true;
        // 
        // btnAddAccount
        // 
        btnAddAccount.BackColor = Color.DarkSlateGray;
        btnAddAccount.FlatStyle = FlatStyle.Flat;
        btnAddAccount.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnAddAccount.ForeColor = Color.White;
        btnAddAccount.Location = new Point(25, 525);
        btnAddAccount.Name = "btnAddAccount";
        btnAddAccount.Size = new Size(140, 40);
        btnAddAccount.TabIndex = 4;
        btnAddAccount.Text = "Add Account";
        btnAddAccount.UseVisualStyleBackColor = false;
        btnAddAccount.Click += btnAddAccount_Click;
        // 
        // btnEditAccount
        // 
        btnEditAccount.BackColor = Color.SteelBlue;
        btnEditAccount.FlatStyle = FlatStyle.Flat;
        btnEditAccount.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnEditAccount.ForeColor = Color.White;
        btnEditAccount.Location = new Point(180, 525);
        btnEditAccount.Name = "btnEditAccount";
        btnEditAccount.Size = new Size(140, 40);
        btnEditAccount.TabIndex = 5;
        btnEditAccount.Text = "Edit Account";
        btnEditAccount.UseVisualStyleBackColor = false;
        btnEditAccount.Click += btnEditAccount_Click;
        // 
        // btnDisableAccount
        // 
        btnDisableAccount.BackColor = Color.IndianRed;
        btnDisableAccount.FlatStyle = FlatStyle.Flat;
        btnDisableAccount.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnDisableAccount.ForeColor = Color.White;
        btnDisableAccount.Location = new Point(335, 525);
        btnDisableAccount.Name = "btnDisableAccount";
        btnDisableAccount.Size = new Size(150, 40);
        btnDisableAccount.TabIndex = 6;
        btnDisableAccount.Text = "Disable Account";
        btnDisableAccount.UseVisualStyleBackColor = false;
        btnDisableAccount.Click += btnDisableAccount_Click;
        // 
        // button1
        // 
        btnResetPassword.BackColor = Color.Firebrick;
        btnResetPassword.FlatStyle = FlatStyle.Flat;
        btnResetPassword.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnResetPassword.ForeColor = Color.White;
        btnResetPassword.Location = new Point(501, 525);
        btnResetPassword.Name = "btnResetPassword";
        btnResetPassword.Size = new Size(150, 40);
        btnResetPassword.TabIndex = 7;
        btnResetPassword.Text = "Reset Password";
        btnResetPassword.UseVisualStyleBackColor = false;
        btnResetPassword.Click += btnResetPassword_Click;
        // 
        // AccountManagementControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        BackColor = Color.WhiteSmoke;
        Controls.Add(btnResetPassword);
        Controls.Add(btnDisableAccount);
        Controls.Add(btnEditAccount);
        Controls.Add(btnAddAccount);
        Controls.Add(dgvAccounts);
        Controls.Add(btnSearch);
        Controls.Add(txtSearch);
        Controls.Add(lblAccountsTitle);
        Name = "AccountManagementControl";
        Size = new Size(800, 650);
        ((System.ComponentModel.ISupportInitialize)dgvAccounts).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblAccountsTitle;
    private TextBox txtSearch;
    private Button btnSearch;
    private DataGridView dgvAccounts;
    private DataGridViewTextBoxColumn colUserId;
    private DataGridViewTextBoxColumn colUsername;
    private DataGridViewTextBoxColumn colRole;
    private DataGridViewTextBoxColumn colStatus;
    private DataGridViewTextBoxColumn colCreatedAt;
    private Button btnAddAccount;
    private Button btnEditAccount;
    private Button btnDisableAccount;
    private Button btnResetPassword;
}
