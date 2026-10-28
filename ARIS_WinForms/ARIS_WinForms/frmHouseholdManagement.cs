using ARIS_WinForms.DAL;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace ARIS_WinForms
{
    public partial class frmHouseholdManagement : Form
    {
        private HouseholdDAL _householdDAL = new HouseholdDAL();

        public frmHouseholdManagement()
        {
            InitializeComponent();
        }

        private void frmHouseholdManagement_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        public void LoadData()
        {
            try
            {
                dgvHouseholds.DataSource = _householdDAL.GetAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddHousehold_Click(object sender, EventArgs e)
        {
            frmAddHousehold frm = new frmAddHousehold();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadData();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("Đang tìm kiếm hộ khẩu với từ khóa: " + keyword, "Thông báo");
            }
            else
            {
                LoadData();
            }
        }


        private void frmHouseholdManagement_Load_1(object sender, EventArgs e)
        {

        }



        private void button1_Click(object sender, EventArgs e)
        {

        }


        private void btnSearch_Click_1(object sender, EventArgs e)
        {

        }

        private void dgvHouseholds_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btnAddHousehold_Click_1(object sender, EventArgs e)
        {
            frmAddHousehold addForm = new frmAddHousehold();
            addForm.ShowDialog();
        }
        private void LoadHouseholdData()
        {
            using (MySqlConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM v_HouseholdList";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
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
                    MessageBox.Show("Lỗi kết nối CSDL MySQL: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();

            using (MySqlConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM v_HouseholdList WHERE HeadName LIKE @kw OR AddressText LIKE @kw OR HeadNationalId LIKE @kw";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

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
                    MessageBox.Show("Lỗi tìm kiếm: " + ex.Message);
                }
            }
        }

        private void btnDetail_Click(object sender, EventArgs e)
        {
            try
            {
                // Kiểm tra xem bảng có dữ liệu và đang chọn dòng nào không
                if (dgvHouseholds.CurrentRow != null)
                {
                    // Lấy giá trị cột HouseholdId
                    string householdId = dgvHouseholds.CurrentRow.Cells["HouseholdId"].Value.ToString();

                    // Mở Form 3
                    frmHouseholdDetail detailForm = new frmHouseholdDetail(householdId);
                    detailForm.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Chưa chọn dòng nào trên bảng kìa Duy ơi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // Nếu sai tên cột hoặc lỗi gì nó sẽ hiện bảng này lên để mình biết
                MessageBox.Show("Lỗi chi tiết: " + ex.Message, "Báo lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}


    
    