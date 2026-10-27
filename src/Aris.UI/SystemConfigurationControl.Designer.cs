namespace Aris.UI;

partial class SystemConfigurationControl
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
        dgvConfigurations = new DataGridView();
        lblConfigValue = new Label();
        txtConfigValue = new TextBox();
        btnSaveChanges = new Button();
        lblTitle = new Label();
        ((System.ComponentModel.ISupportInitialize)dgvConfigurations).BeginInit();
        SuspendLayout();
        // 
        // dgvConfigurations
        // 
        dgvConfigurations.AllowUserToAddRows = false;
        dgvConfigurations.AllowUserToDeleteRows = false;
        dgvConfigurations.AllowUserToResizeColumns = false;
        dgvConfigurations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvConfigurations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvConfigurations.BackgroundColor = Color.White;
        dgvConfigurations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvConfigurations.Location = new Point(20, 60);
        dgvConfigurations.MultiSelect = false;
        dgvConfigurations.Name = "dgvConfigurations";
        dgvConfigurations.ReadOnly = true;
        dgvConfigurations.RowHeadersVisible = false;
        dgvConfigurations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvConfigurations.Size = new Size(757, 535);
        dgvConfigurations.TabIndex = 0;
        // 
        // lblConfigValue
        // 
        lblConfigValue.AutoSize = true;
        lblConfigValue.Location = new Point(20, 628);
        lblConfigValue.Name = "lblConfigValue";
        lblConfigValue.Size = new Size(115, 15);
        lblConfigValue.TabIndex = 1;
        lblConfigValue.Text = "Configuration Value:";
        // 
        // txtConfigValue
        // 
        txtConfigValue.Location = new Point(162, 625);
        txtConfigValue.Name = "txtConfigValue";
        txtConfigValue.Size = new Size(350, 23);
        txtConfigValue.TabIndex = 2;
        // 
        // btnSaveChanges
        // 
        btnSaveChanges.BackColor = Color.DarkSlateGray;
        btnSaveChanges.Enabled = false;
        btnSaveChanges.FlatStyle = FlatStyle.Flat;
        btnSaveChanges.ForeColor = Color.White;
        btnSaveChanges.Location = new Point(559, 618);
        btnSaveChanges.Name = "btnSaveChanges";
        btnSaveChanges.Size = new Size(140, 35);
        btnSaveChanges.TabIndex = 3;
        btnSaveChanges.Text = "Save Changes";
        btnSaveChanges.UseVisualStyleBackColor = false;
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblTitle.ForeColor = Color.DarkSlateGray;
        lblTitle.Location = new Point(20, 15);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(226, 30);
        lblTitle.TabIndex = 4;
        lblTitle.Text = "System Configuration";
        // 
        // SystemConfigurationControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(lblTitle);
        Controls.Add(btnSaveChanges);
        Controls.Add(txtConfigValue);
        Controls.Add(lblConfigValue);
        Controls.Add(dgvConfigurations);
        Name = "SystemConfigurationControl";
        Size = new Size(800, 685);
        ((System.ComponentModel.ISupportInitialize)dgvConfigurations).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private DataGridView dgvConfigurations;
    private Label lblConfigValue;
    private TextBox txtConfigValue;
    private Button btnSaveChanges;
    private Label lblTitle;
}