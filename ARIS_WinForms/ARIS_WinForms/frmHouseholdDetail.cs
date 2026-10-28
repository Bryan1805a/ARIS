using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ARIS_WinForms
{
    public partial class frmHouseholdDetail : Form
    {
        private string currentHouseholdId;

        public frmHouseholdDetail(string householdId)
        {
            InitializeComponent();
            this.currentHouseholdId = householdId;

            LoadMemberData(householdId);
        }

        public frmHouseholdDetail()
        {
            InitializeComponent();
        }

        private void frmHouseholdDetail_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentHouseholdId))
            {
                LoadMemberData(currentHouseholdId);
            }
        }

        private void LoadMemberData(string householdId)
        {
            using (MySqlConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM v_HouseholdMembers WHERE HouseholdId = @HouseholdId";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HouseholdId", householdId);

                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            dgvHouseholds.DataSource = dt; 
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi tải danh sách thành viên: " + ex.Message);
                }
            }
        }
        private void btnDetail_Click(object sender, EventArgs e)
        {
            if (dgvHouseholds.SelectedRows.Count > 0)
            {
                string householdId = dgvHouseholds.SelectedRows[0].Cells["HouseholdId"].Value.ToString();

                frmHouseholdDetail detailForm = new frmHouseholdDetail(householdId);

                detailForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một hộ gia đình trên bảng để xem chi tiết!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}