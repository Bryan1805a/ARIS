namespace ARIS_WinForms
{
    partial class frmAddHousehold
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
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            txtHouseholdNumber = new TextBox();
            txtHeadCitizenId = new TextBox();
            txtAddress = new TextBox();
            btnSave = new Button();
            btnCancel = new Button();
            panelHeader = new Panel();
            panelHeader.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft Sans Serif", 10F);
            label1.ForeColor = Color.White;
            label1.Location = new Point(104, 91);
            label1.Name = "label1";
            label1.Size = new Size(153, 20);
            label1.TabIndex = 0;
            label1.Text = "Household Number";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft Sans Serif", 10F);
            label2.Location = new Point(104, 146);
            label2.Name = "label2";
            label2.Size = new Size(128, 20);
            label2.TabIndex = 1;
            label2.Text = "Head Citizen ID";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft Sans Serif", 10F);
            label3.Location = new Point(104, 205);
            label3.Name = "label3";
            label3.Size = new Size(93, 20);
            label3.TabIndex = 2;
            label3.Text = "Address ID";
            // 
            // txtHouseholdNumber
            // 
            txtHouseholdNumber.Font = new Font("Microsoft Sans Serif", 10F);
            txtHouseholdNumber.Location = new Point(320, 86);
            txtHouseholdNumber.Name = "txtHouseholdNumber";
            txtHouseholdNumber.Size = new Size(166, 26);
            txtHouseholdNumber.TabIndex = 3;
            // 
            // txtHeadCitizenId
            // 
            txtHeadCitizenId.Font = new Font("Microsoft Sans Serif", 10F);
            txtHeadCitizenId.Location = new Point(320, 139);
            txtHeadCitizenId.Name = "txtHeadCitizenId";
            txtHeadCitizenId.Size = new Size(166, 26);
            txtHeadCitizenId.TabIndex = 4;
            // 
            // txtAddress
            // 
            txtAddress.Font = new Font("Microsoft Sans Serif", 10F);
            txtAddress.Location = new Point(320, 198);
            txtAddress.Name = "txtAddress";
            txtAddress.Size = new Size(166, 26);
            txtAddress.TabIndex = 5;
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.DarkSlateGray;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Microsoft Sans Serif", 11F);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(144, 285);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(128, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.DarkSlateGray;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Microsoft Sans Serif", 11F);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(345, 285);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 29);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.DarkSlateGray;
            panelHeader.Controls.Add(label1);
            panelHeader.Controls.Add(btnCancel);
            panelHeader.Controls.Add(label2);
            panelHeader.Controls.Add(txtAddress);
            panelHeader.Controls.Add(btnSave);
            panelHeader.Controls.Add(txtHeadCitizenId);
            panelHeader.Controls.Add(label3);
            panelHeader.Controls.Add(txtHouseholdNumber);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.ForeColor = Color.White;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(614, 348);
            panelHeader.TabIndex = 8;
            // 
            // frmAddHousehold
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(614, 384);
            Controls.Add(panelHeader);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "frmAddHousehold";
            Text = "Add New Household";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private TextBox txtHouseholdNumber;
        private TextBox txtHeadCitizenId;
        private TextBox txtAddress;
        private Button btnSave;
        private Button btnCancel;
        private Panel panelHeader;
    }
}