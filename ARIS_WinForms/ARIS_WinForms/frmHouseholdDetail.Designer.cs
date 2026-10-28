namespace ARIS_WinForms
{
    partial class frmHouseholdDetail
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
            panelSidebar = new Panel();
            btnClose = new Button();
            btnChangeHead = new Button();
            label1 = new Label();
            btnAddMember = new Button();
            dgvHouseholds = new DataGridView();
            panelSidebar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHouseholds).BeginInit();
            SuspendLayout();
            // 
            // panelSidebar
            // 
            panelSidebar.BackColor = Color.DarkSlateGray;
            panelSidebar.Controls.Add(btnClose);
            panelSidebar.Controls.Add(btnChangeHead);
            panelSidebar.Controls.Add(label1);
            panelSidebar.Controls.Add(btnAddMember);
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Location = new Point(0, 0);
            panelSidebar.Name = "panelSidebar";
            panelSidebar.Size = new Size(250, 450);
            panelSidebar.TabIndex = 0;
            // 
            // btnClose
            // 
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Microsoft Sans Serif", 10F);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(78, 293);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(94, 44);
            btnClose.TabIndex = 3;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // btnChangeHead
            // 
            btnChangeHead.FlatStyle = FlatStyle.Flat;
            btnChangeHead.Font = new Font("Microsoft Sans Serif", 10F);
            btnChangeHead.ForeColor = Color.White;
            btnChangeHead.Location = new Point(38, 214);
            btnChangeHead.Name = "btnChangeHead";
            btnChangeHead.Size = new Size(182, 48);
            btnChangeHead.TabIndex = 2;
            btnChangeHead.Text = "Change Head";
            btnChangeHead.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.DarkSlateGray;
            label1.Font = new Font("Microsoft Sans Serif", 18F);
            label1.ForeColor = Color.White;
            label1.Location = new Point(86, 34);
            label1.Name = "label1";
            label1.Size = new Size(86, 36);
            label1.TabIndex = 1;
            label1.Text = "ARIS";
            // 
            // btnAddMember
            // 
            btnAddMember.FlatStyle = FlatStyle.Flat;
            btnAddMember.Font = new Font("Microsoft Sans Serif", 10F);
            btnAddMember.ForeColor = Color.White;
            btnAddMember.Location = new Point(38, 133);
            btnAddMember.Name = "btnAddMember";
            btnAddMember.Size = new Size(182, 47);
            btnAddMember.TabIndex = 1;
            btnAddMember.Text = "Add Member";
            btnAddMember.UseVisualStyleBackColor = true;
            // 
            // dgvHouseholds
            // 
            dgvHouseholds.BackgroundColor = Color.White;
            dgvHouseholds.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHouseholds.Dock = DockStyle.Fill;
            dgvHouseholds.Location = new Point(250, 0);
            dgvHouseholds.Name = "dgvHouseholds";
            dgvHouseholds.RowHeadersWidth = 51;
            dgvHouseholds.Size = new Size(550, 450);
            dgvHouseholds.TabIndex = 1;
            // 
            // frmHouseholdDetail
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(dgvHouseholds);
            Controls.Add(panelSidebar);
            Name = "frmHouseholdDetail";
            panelSidebar.ResumeLayout(false);
            panelSidebar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHouseholds).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelSidebar;
        private Label label1;
        private Button btnClose;
        private Button btnChangeHead;
        private Button btnAddMember;
        private DataGridView dgvHouseholds;
    }
}