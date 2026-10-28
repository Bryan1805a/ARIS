using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ARIS_WinForms
{
    public partial class frmAddHousehold : Form
    {
        public frmAddHousehold()
        {
            InitializeComponent();
        }

        // 1. NÚT CANCEL (HỦY / ĐÓNG FORM 2)
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close(); // Đóng Form 2 quay về Form 1
        }

        // 2. NÚT SAVE (LƯU HỘ GIA ĐÌNH MỚI VÀO DATABASE)
        private void btnSave_Click(object sender, EventArgs e)
        {
            // Lấy địa chỉ từ TextBox (giả sử tên control là txtAddress)
            string addressText = txtAddress.Text.Trim();

            if (string.IsNullOrEmpty(addressText))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ hộ gia đình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    // Bước 2.1: Thêm địa chỉ mới vào bảng Address[cite: 2]
                    string insertAddressQuery = "INSERT INTO Address (AddressText, Status) VALUES (@AddressText, 'ACTIVE'); SELECT LAST_INSERT_ID();";
                    int newAddressId;

                    using (MySqlCommand cmdAddr = new MySqlCommand(insertAddressQuery, conn))
                    {
                        cmdAddr.Parameters.AddWithValue("@AddressText", addressText);
                        newAddressId = Convert.ToInt32(cmdAddr.ExecuteScalar());
                    }

                    // Bước 2.2: Thêm Hộ gia đình mới vào bảng Household[cite: 2]
                    string insertHouseholdQuery = "INSERT INTO Household (CurrentAddressId, Status) VALUES (@AddressId, 'ACTIVE'); SELECT LAST_INSERT_ID();";
                    int newHouseholdId;

                    using (MySqlCommand cmdHouse = new MySqlCommand(insertHouseholdQuery, conn))
                    {
                        cmdHouse.Parameters.AddWithValue("@AddressId", newAddressId);
                        newHouseholdId = Convert.ToInt32(cmdHouse.ExecuteScalar());
                    }

                    MessageBox.Show("Thêm hộ gia đình thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 3. NHẢY THẲNG SANG FORM 3 ĐỂ THÊM THÀNH VIÊN CHO HỘ MỚI NÀY
                    frmHouseholdDetail detailForm = new frmHouseholdDetail(newHouseholdId.ToString());
                    this.Hide(); // Ẩn Form 2
                    detailForm.ShowDialog(); // Mở Form 3 với mã hộ vừa tạo
                    this.Close(); // Đóng Form 2 hoàn toàn sau khi xong
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}