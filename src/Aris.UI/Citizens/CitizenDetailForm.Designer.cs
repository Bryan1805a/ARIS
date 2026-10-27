namespace Aris.UI.Citizens;

partial class CitizenDetailForm
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
        txtNationalId = new TextBox();
        txtFullName = new TextBox();
        dtpDateOfBirth = new DateTimePicker();
        cmbGender = new ComboBox();
        label1 = new Label();
        label2 = new Label();
        label3 = new Label();
        label4 = new Label();
        label5 = new Label();
        txtStatus = new TextBox();
        btnSave = new Button();
        btnDeactivate = new Button();
        label6 = new Label();
        SuspendLayout();
        // 
        // txtNationalId
        // 
        txtNationalId.Location = new Point(139, 78);
        txtNationalId.Name = "txtNationalId";
        txtNationalId.Size = new Size(250, 27);
        txtNationalId.TabIndex = 0;
        // 
        // txtFullName
        // 
        txtFullName.Location = new Point(139, 134);
        txtFullName.Name = "txtFullName";
        txtFullName.Size = new Size(250, 27);
        txtFullName.TabIndex = 1;
        // 
        // dtpDateOfBirth
        // 
        dtpDateOfBirth.Location = new Point(139, 231);
        dtpDateOfBirth.Name = "dtpDateOfBirth";
        dtpDateOfBirth.Size = new Size(250, 27);
        dtpDateOfBirth.TabIndex = 3;
        // 
        // cmbGender
        // 
        cmbGender.FormattingEnabled = true;
        cmbGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
        cmbGender.Location = new Point(139, 293);
        cmbGender.Name = "cmbGender";
        cmbGender.Size = new Size(250, 28);
        cmbGender.TabIndex = 4;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(26, 85);
        label1.Name = "label1";
        label1.Size = new Size(85, 20);
        label1.TabIndex = 5;
        label1.Text = "National ID";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(26, 141);
        label2.Name = "label2";
        label2.Size = new Size(76, 20);
        label2.TabIndex = 6;
        label2.Text = "Full Name";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(26, 194);
        label3.Name = "label3";
        label3.Size = new Size(49, 20);
        label3.TabIndex = 7;
        label3.Text = "Status";
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(26, 301);
        label4.Name = "label4";
        label4.Size = new Size(57, 20);
        label4.TabIndex = 8;
        label4.Text = "Gender";
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(26, 238);
        label5.Name = "label5";
        label5.Size = new Size(94, 20);
        label5.TabIndex = 9;
        label5.Text = "Date of birth";
        // 
        // txtStatus
        // 
        txtStatus.Location = new Point(139, 187);
        txtStatus.Name = "txtStatus";
        txtStatus.Size = new Size(250, 27);
        txtStatus.TabIndex = 10;
        // 
        // btnSave
        // 
        btnSave.BackColor = Color.DarkSlateGray;
        btnSave.ForeColor = SystemColors.ControlLightLight;
        btnSave.Location = new Point(366, 393);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(168, 45);
        btnSave.TabIndex = 11;
        btnSave.Text = "Update information";
        btnSave.UseVisualStyleBackColor = false;
        // 
        // btnDeactivate
        // 
        btnDeactivate.BackColor = Color.Red;
        btnDeactivate.ForeColor = Color.Maroon;
        btnDeactivate.Location = new Point(540, 393);
        btnDeactivate.Name = "btnDeactivate";
        btnDeactivate.Size = new Size(248, 45);
        btnDeactivate.TabIndex = 12;
        btnDeactivate.Text = "Deactivate standard citizen";
        btnDeactivate.UseVisualStyleBackColor = false;
        // 
        // label6
        // 
        label6.AutoSize = true;
        label6.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
        label6.ForeColor = Color.DarkSlateGray;
        label6.Location = new Point(346, 20);
        label6.Name = "label6";
        label6.Size = new Size(158, 31);
        label6.TabIndex = 13;
        label6.Text = "Citizen Detail";
        // 
        // CitizenDetailForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.ControlLight;
        ClientSize = new Size(800, 450);
        Controls.Add(label6);
        Controls.Add(btnDeactivate);
        Controls.Add(btnSave);
        Controls.Add(txtStatus);
        Controls.Add(label5);
        Controls.Add(label4);
        Controls.Add(label3);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(cmbGender);
        Controls.Add(dtpDateOfBirth);
        Controls.Add(txtFullName);
        Controls.Add(txtNationalId);
        Name = "CitizenDetailForm";
        Text = "CitizenDirectoryForm";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TextBox txtNationalId;
    private TextBox txtFullName;
    private DateTimePicker dtpDateOfBirth;
    private ComboBox cmbGender;
    private Label label1;
    private Label label2;
    private Label label3;
    private Label label4;
    private Label label5;
    private TextBox txtStatus;
    private Button btnSave;
    private Button btnDeactivate;
    private Label label6;
}