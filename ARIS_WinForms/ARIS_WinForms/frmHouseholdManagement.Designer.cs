namespace ARIS_WinForms
{
    partial class frmHouseholdManagement
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
            dgvHouseholds = new DataGridView();
            btnSearch = new Button();
            btnAddHousehold = new Button();
            btnDetail = new Button();
            panelSidebar = new Panel();
            txtSearch = new TextBox();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvHouseholds).BeginInit();
            panelSidebar.SuspendLayout();
            SuspendLayout();
            // 
            // dgvHouseholds
            // 
            dgvHouseholds.BackgroundColor = SystemColors.Control;
            dgvHouseholds.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHouseholds.Dock = DockStyle.Fill;
            dgvHouseholds.Location = new Point(0, 0);
            dgvHouseholds.Name = "dgvHouseholds";
            dgvHouseholds.RowHeadersWidth = 51;
            dgvHouseholds.Size = new Size(611, 543);
            dgvHouseholds.TabIndex = 0;
            dgvHouseholds.CellContentClick += dgvHouseholds_CellContentClick;
            // 
            // btnSearch
            // 
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.Font = new Font("Microsoft Sans Serif", 9F);
            btnSearch.ForeColor = SystemColors.Control;
            btnSearch.Location = new Point(32, 164);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(154, 27);
            btnSearch.TabIndex = 2;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click_1;
            // 
            // btnAddHousehold
            // 
            btnAddHousehold.FlatStyle = FlatStyle.Flat;
            btnAddHousehold.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnAddHousehold.ForeColor = SystemColors.Control;
            btnAddHousehold.Location = new Point(32, 229);
            btnAddHousehold.Name = "btnAddHousehold";
            btnAddHousehold.Size = new Size(154, 52);
            btnAddHousehold.TabIndex = 3;
            btnAddHousehold.Text = "Add Household";
            btnAddHousehold.UseVisualStyleBackColor = true;
            btnAddHousehold.Click += btnAddHousehold_Click_1;
            // 
            // btnDetail
            // 
            btnDetail.FlatStyle = FlatStyle.Flat;
            btnDetail.Font = new Font("Microsoft Sans Serif", 9F);
            btnDetail.ForeColor = SystemColors.Control;
            btnDetail.Location = new Point(32, 324);
            btnDetail.Name = "btnDetail";
            btnDetail.Size = new Size(154, 52);
            btnDetail.TabIndex = 4;
            btnDetail.Text = "View Detail";
            btnDetail.UseVisualStyleBackColor = true;
            btnDetail.Click += btnDetail_Click;
            // 
            // panelSidebar
            // 
            panelSidebar.BackColor = Color.DarkSlateGray;
            panelSidebar.Controls.Add(txtSearch);
            panelSidebar.Controls.Add(label1);
            panelSidebar.Controls.Add(btnAddHousehold);
            panelSidebar.Controls.Add(btnDetail);
            panelSidebar.Controls.Add(btnSearch);
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Location = new Point(0, 0);
            panelSidebar.Name = "panelSidebar";
            panelSidebar.Size = new Size(218, 543);
            panelSidebar.TabIndex = 5;
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(32, 116);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(154, 27);
            txtSearch.TabIndex = 6;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft Sans Serif", 18F);
            label1.ForeColor = SystemColors.ButtonHighlight;
            label1.Location = new Point(78, 48);
            label1.Name = "label1";
            label1.Size = new Size(86, 36);
            label1.TabIndex = 5;
            label1.Text = "ARIS";
            // 
            // frmHouseholdManagement
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(611, 543);
            Controls.Add(panelSidebar);
            Controls.Add(dgvHouseholds);
            Name = "frmHouseholdManagement";
            Text = "frmHouseholdManagement";
            Load += frmHouseholdManagement_Load_1;
            ((System.ComponentModel.ISupportInitialize)dgvHouseholds).EndInit();
            panelSidebar.ResumeLayout(false);
            panelSidebar.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dataGridView1;
        private DataGridView dgvHouseholds;
        private Button btnSearch;
        private Button btnAddHousehold;
        private Button btnDetail;
        private Panel panelSidebar;
        private Label label1;
        private TextBox txtSearch;
    }
}