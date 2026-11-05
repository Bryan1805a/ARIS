using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;
using Aris.Application.TemporaryResidences;
using Aris.Application.TemporaryResidences.Dtos;
using Aris.UI.Localization;

namespace Aris.UI.TemporaryResidences;

public sealed class RegisterTemporaryResidenceForm : Form
{
    private readonly ITemporaryResidenceService _tempResService;
    private readonly IAddressService _addressService;
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;

    private readonly TextBox _txtNationalId = new();
    private readonly Button _btnLookupCitizen = new();
    private readonly TextBox _txtFullName = new();
    private readonly ComboBox _cboAddress = new();
    private readonly DateTimePicker _dtpStart = new();
    private readonly DateTimePicker _dtpEnd = new();
    private readonly ComboBox _cboReason = new();
    private readonly TextBox _txtHostInfo = new();
    private readonly TextBox _txtNotes = new();
    private readonly Label _lblError = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnCancel = new();

    private readonly List<AddressDto> _addresses = new();

    public RegisterTemporaryResidenceForm(
        ITemporaryResidenceService tempResService,
        IAddressService addressService,
        ICitizenService citizenService,
        IUserSession session)
    {
        _tempResService = tempResService;
        _addressService = addressService;
        _citizenService = citizenService;
        _session = session;

        BuildForm();
        UiTheme.ApplyScreen(this);
        Load += async (_, _) => await LoadAddressesAsync();
    }

    private void BuildForm()
    {
        Text = LanguageManager.T("Đăng ký tạm trú / Register Temporary Residence");
        ClientSize = new Size(580, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var lblTitle = new Label
        {
            Text = LanguageManager.T("ĐĂNG KÝ TẠM TRÚ"),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiTheme.Accent,
            Location = new Point(25, 20),
            AutoSize = true
        };

        // CCCD
        var lblNationalId = new Label { Text = LanguageManager.T("Số CCCD (12 số):"), Location = new Point(25, 65), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtNationalId.Location = new Point(25, 88);
        _txtNationalId.Size = new Size(240, 27);
        _txtNationalId.MaxLength = 12;

        _btnLookupCitizen.Text = LanguageManager.T("Kiểm tra CCCD");
        _btnLookupCitizen.Location = new Point(275, 87);
        _btnLookupCitizen.Size = new Size(120, 29);
        _btnLookupCitizen.Click += async (_, _) => await LookupCitizenAsync();

        // Họ tên (read-only)
        var lblFullName = new Label { Text = LanguageManager.T("Họ và tên công dân:"), Location = new Point(25, 125), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtFullName.Location = new Point(25, 148);
        _txtFullName.Size = new Size(520, 27);
        _txtFullName.ReadOnly = true;
        _txtFullName.BackColor = Color.FromArgb(241, 245, 249);

        // Địa chỉ tạm trú
        var lblAddress = new Label { Text = LanguageManager.T("Địa chỉ tạm trú:"), Location = new Point(25, 185), AutoSize = true, Font = UiTheme.SemiboldFont };
        _cboAddress.Location = new Point(25, 208);
        _cboAddress.Size = new Size(520, 27);
        _cboAddress.DropDownStyle = ComboBoxStyle.DropDownList;

        // Ngày bắt đầu & Ngày kết thúc
        var lblStart = new Label { Text = LanguageManager.T("Từ ngày:"), Location = new Point(25, 245), AutoSize = true, Font = UiTheme.SemiboldFont };
        _dtpStart.Location = new Point(25, 268);
        _dtpStart.Size = new Size(240, 27);
        _dtpStart.Format = DateTimePickerFormat.Short;
        _dtpStart.Value = DateTime.Today;

        var lblEnd = new Label { Text = LanguageManager.T("Đến ngày (tối đa 2 năm):"), Location = new Point(305, 245), AutoSize = true, Font = UiTheme.SemiboldFont };
        _dtpEnd.Location = new Point(305, 268);
        _dtpEnd.Size = new Size(240, 27);
        _dtpEnd.Format = DateTimePickerFormat.Short;
        _dtpEnd.Value = DateTime.Today.AddYears(1);

        // Lý do tạm trú
        var lblReason = new Label { Text = LanguageManager.T("Lý do tạm trú:"), Location = new Point(25, 305), AutoSize = true, Font = UiTheme.SemiboldFont };
        _cboReason.Location = new Point(25, 328);
        _cboReason.Size = new Size(240, 27);
        _cboReason.Items.AddRange(new object[] {
            "Thuê nhà / Thuê trọ",
            "Lao động / Làm việc",
            "Học tập / Sinh viên",
            "Ở nhờ người thân",
            "Chữa bệnh / Điều trị",
            "Khác"
        });
        _cboReason.SelectedIndex = 0;

        // Thông tin chủ trọ/bảo lãnh
        var lblHost = new Label { Text = LanguageManager.T("Chủ hộ / Người bảo lãnh:"), Location = new Point(305, 305), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtHostInfo.Location = new Point(305, 328);
        _txtHostInfo.Size = new Size(240, 27);
        _txtHostInfo.PlaceholderText = "Tên chủ trọ / SĐT";

        // Ghi chú
        var lblNotes = new Label { Text = LanguageManager.T("Ghi chú:"), Location = new Point(25, 365), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtNotes.Location = new Point(25, 388);
        _txtNotes.Size = new Size(520, 27);

        // Error message
        _lblError.Location = new Point(25, 420);
        _lblError.Size = new Size(520, 35);
        _lblError.ForeColor = Color.Firebrick;
        _lblError.Visible = false;

        // Buttons
        _btnSave.Text = LanguageManager.T("Lưu đăng ký");
        _btnSave.Location = new Point(310, 465);
        _btnSave.Size = new Size(115, 34);
        _btnSave.BackColor = UiTheme.Accent;
        _btnSave.ForeColor = Color.White;
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.Click += async (_, _) => await SaveAsync();

        _btnCancel.Text = LanguageManager.T("Hủy");
        _btnCancel.Location = new Point(435, 465);
        _btnCancel.Size = new Size(110, 34);
        _btnCancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[] {
            lblTitle,
            lblNationalId, _txtNationalId, _btnLookupCitizen,
            lblFullName, _txtFullName,
            lblAddress, _cboAddress,
            lblStart, _dtpStart,
            lblEnd, _dtpEnd,
            lblReason, _cboReason,
            lblHost, _txtHostInfo,
            lblNotes, _txtNotes,
            _lblError,
            _btnSave, _btnCancel
        });

        _txtNationalId.KeyDown += async (_, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                await LookupCitizenAsync();
            }
        };
    }

    private async Task LoadAddressesAsync()
    {
        var result = await _addressService.SearchAsync(new AddressSearchCriteria { Status = "ACTIVE" });
        if (result.IsSuccess && result.Value != null)
        {
            _addresses.Clear();
            _addresses.AddRange(result.Value);
            _cboAddress.Items.Clear();
            foreach (var addr in _addresses)
            {
                _cboAddress.Items.Add($"[{addr.AddressCode}] {addr.AddressText}");
            }
            if (_cboAddress.Items.Count > 0)
            {
                _cboAddress.SelectedIndex = 0;
            }
        }
    }

    private async Task LookupCitizenAsync()
    {
        var cccd = _txtNationalId.Text.Trim();
        if (cccd.Length != 12 || !cccd.All(char.IsDigit))
        {
            ShowError("Số CCCD phải bao gồm đúng 12 chữ số.");
            _txtFullName.Text = string.Empty;
            return;
        }

        var result = await _citizenService.SearchAsync(new CitizenSearchCriteria { NationalId = cccd });
        if (result.IsSuccess && result.Value != null)
        {
            var match = result.Value.FirstOrDefault(c => c.NationalId == cccd);
            if (match != null)
            {
                _txtFullName.Text = $"{match.FullName} (Sinh năm: {match.DateOfBirth:dd/MM/yyyy} - {match.Status})";
                HideError();
                return;
            }
        }

        ShowError($"Không tìm thấy công dân có số CCCD {cccd}. Hãy chắc chắn công dân đã được thêm trong mục 'Quản lý công dân'.");
        _txtFullName.Text = string.Empty;
    }

    private async Task SaveAsync()
    {
        HideError();

        var nationalId = _txtNationalId.Text.Trim();
        if (string.IsNullOrWhiteSpace(nationalId) || nationalId.Length != 12 || !nationalId.All(char.IsDigit))
        {
            ShowError("Vui lòng nhập đúng số CCCD (đúng 12 chữ số).");
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtFullName.Text))
        {
            ShowError("Vui lòng bấm 'Kiểm tra CCCD' để xác thực thông tin công dân trước khi lưu.");
            return;
        }

        if (_cboAddress.SelectedIndex < 0 || _cboAddress.SelectedIndex >= _addresses.Count)
        {
            ShowError("Vui lòng chọn địa chỉ nơi đăng ký tạm trú.");
            return;
        }

        var addressId = _addresses[_cboAddress.SelectedIndex].AddressId;
        var startDate = DateOnly.FromDateTime(_dtpStart.Value);
        var endDate = DateOnly.FromDateTime(_dtpEnd.Value);

        if (endDate < startDate)
        {
            ShowError("Ngày kết thúc tạm trú phải sau hoặc bằng ngày bắt đầu.");
            return;
        }

        if (endDate.DayNumber - startDate.DayNumber > 731)
        {
            ShowError("Theo quy định của Luật Cư trú, thời hạn đăng ký tạm trú tối đa là 2 năm (730 ngày).");
            return;
        }

        var reason = _cboReason.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            ShowError("Vui lòng nhập hoặc chọn lý do tạm trú.");
            return;
        }

        var dto = new RegisterTemporaryResidenceDto(
            nationalId,
            addressId,
            startDate,
            endDate,
            reason,
            string.IsNullOrWhiteSpace(_txtHostInfo.Text) ? null : _txtHostInfo.Text.Trim(),
            string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim()
        );

        _btnSave.Enabled = false;
        try
        {
            var actorId = _session.UserId ?? 1;
            var registerResult = await _tempResService.RegisterAsync(dto, actorId);
            if (registerResult.IsFailure)
            {
                ShowError(string.Join(Environment.NewLine, registerResult.Errors));
                return;
            }

            MessageBox.Show(
                "Đăng ký tạm trú thành công!",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi khi lưu dữ liệu: {ex.Message}");
        }
        finally
        {
            _btnSave.Enabled = true;
        }
    }

    private void ShowError(string msg)
    {
        _lblError.Text = msg;
        _lblError.Visible = true;
    }

    private void HideError()
    {
        _lblError.Text = string.Empty;
        _lblError.Visible = false;
    }
}
