using Aris.Application.Common;
using Aris.Application.TemporaryResidences;
using Aris.Application.TemporaryResidences.Dtos;
using Aris.UI.Localization;

namespace Aris.UI.TemporaryResidences;

public sealed class EditTemporaryResidenceForm : Form
{
    private readonly ITemporaryResidenceService _tempResService;
    private readonly IUserSession _session;
    private readonly TemporaryResidenceDto _item;

    private readonly TextBox _txtCitizenInfo = new();
    private readonly TextBox _txtAddress = new();
    private readonly TextBox _txtStartDate = new();
    private readonly DateTimePicker _dtpEnd = new();
    private readonly ComboBox _cboReason = new();
    private readonly TextBox _txtHostInfo = new();
    private readonly TextBox _txtNotes = new();
    private readonly Label _lblError = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnCancel = new();

    public EditTemporaryResidenceForm(
        ITemporaryResidenceService tempResService,
        IUserSession session,
        TemporaryResidenceDto item)
    {
        _tempResService = tempResService;
        _session = session;
        _item = item;

        BuildForm();
        UiTheme.ApplyScreen(this);
    }

    private void BuildForm()
    {
        Text = LanguageManager.T("Gia hạn / Điều chỉnh tạm trú");
        ClientSize = new Size(540, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var lblTitle = new Label
        {
            Text = LanguageManager.T("GIA HẠN / ĐIỀU CHỈNH TẠM TRÚ"),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiTheme.Accent,
            Location = new Point(25, 20),
            AutoSize = true
        };

        // Công dân
        var lblCitizen = new Label { Text = LanguageManager.T("Công dân:"), Location = new Point(25, 65), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtCitizenInfo.Location = new Point(25, 88);
        _txtCitizenInfo.Size = new Size(490, 27);
        _txtCitizenInfo.ReadOnly = true;
        _txtCitizenInfo.BackColor = Color.FromArgb(241, 245, 249);
        _txtCitizenInfo.Text = $"{_item.FullName} (CCCD: {_item.NationalId})";

        // Địa chỉ
        var lblAddress = new Label { Text = LanguageManager.T("Địa chỉ tạm trú:"), Location = new Point(25, 125), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtAddress.Location = new Point(25, 148);
        _txtAddress.Size = new Size(490, 27);
        _txtAddress.ReadOnly = true;
        _txtAddress.BackColor = Color.FromArgb(241, 245, 249);
        _txtAddress.Text = _item.AddressText;

        // Ngày bắt đầu
        var lblStart = new Label { Text = LanguageManager.T("Từ ngày:"), Location = new Point(25, 185), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtStartDate.Location = new Point(25, 208);
        _txtStartDate.Size = new Size(230, 27);
        _txtStartDate.ReadOnly = true;
        _txtStartDate.BackColor = Color.FromArgb(241, 245, 249);
        _txtStartDate.Text = _item.StartDate.ToString("dd/MM/yyyy");

        // Ngày kết thúc (gia hạn)
        var lblEnd = new Label { Text = LanguageManager.T("Đến ngày (gia hạn):"), Location = new Point(285, 185), AutoSize = true, Font = UiTheme.SemiboldFont };
        _dtpEnd.Location = new Point(285, 208);
        _dtpEnd.Size = new Size(230, 27);
        _dtpEnd.Format = DateTimePickerFormat.Short;
        _dtpEnd.Value = _item.EndDate.ToDateTime(TimeOnly.MinValue);

        // Lý do
        var lblReason = new Label { Text = LanguageManager.T("Lý do tạm trú:"), Location = new Point(25, 245), AutoSize = true, Font = UiTheme.SemiboldFont };
        _cboReason.Location = new Point(25, 268);
        _cboReason.Size = new Size(230, 27);
        _cboReason.Items.AddRange(new object[] {
            "Thuê nhà / Thuê trọ",
            "Lao động / Làm việc",
            "Học tập / Sinh viên",
            "Ở nhờ người thân",
            "Chữa bệnh / Điều trị",
            "Khác"
        });
        _cboReason.Text = _item.StayReason;

        // Chủ hộ/người bảo lãnh
        var lblHost = new Label { Text = LanguageManager.T("Chủ hộ / Người bảo lãnh:"), Location = new Point(285, 245), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtHostInfo.Location = new Point(285, 268);
        _txtHostInfo.Size = new Size(230, 27);
        _txtHostInfo.Text = _item.HostInfo ?? string.Empty;

        // Ghi chú
        var lblNotes = new Label { Text = LanguageManager.T("Ghi chú:"), Location = new Point(25, 305), AutoSize = true, Font = UiTheme.SemiboldFont };
        _txtNotes.Location = new Point(25, 328);
        _txtNotes.Size = new Size(490, 27);
        _txtNotes.Text = _item.Notes ?? string.Empty;

        // Error message
        _lblError.Location = new Point(25, 365);
        _lblError.Size = new Size(490, 35);
        _lblError.ForeColor = Color.Firebrick;
        _lblError.Visible = false;

        // Buttons
        _btnSave.Text = LanguageManager.T("Lưu thay đổi");
        _btnSave.Location = new Point(275, 415);
        _btnSave.Size = new Size(115, 34);
        _btnSave.BackColor = UiTheme.Accent;
        _btnSave.ForeColor = Color.White;
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.Click += async (_, _) => await SaveAsync();

        _btnCancel.Text = LanguageManager.T("Hủy");
        _btnCancel.Location = new Point(400, 415);
        _btnCancel.Size = new Size(115, 34);
        _btnCancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[] {
            lblTitle,
            lblCitizen, _txtCitizenInfo,
            lblAddress, _txtAddress,
            lblStart, _txtStartDate,
            lblEnd, _dtpEnd,
            lblReason, _cboReason,
            lblHost, _txtHostInfo,
            lblNotes, _txtNotes,
            _lblError,
            _btnSave, _btnCancel
        });
    }

    private async Task SaveAsync()
    {
        var newEndDate = DateOnly.FromDateTime(_dtpEnd.Value);
        if (newEndDate < _item.StartDate)
        {
            _lblError.Text = "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.";
            _lblError.Visible = true;
            return;
        }

        var reason = _cboReason.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            _lblError.Text = "Lý do tạm trú không được để trống.";
            _lblError.Visible = true;
            return;
        }

        var dto = new UpdateTemporaryResidenceDto(
            newEndDate,
            reason,
            string.IsNullOrWhiteSpace(_txtHostInfo.Text) ? null : _txtHostInfo.Text.Trim(),
            string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim()
        );

        _btnSave.Enabled = false;
        try
        {
            var actorId = _session.UserId ?? 1;
            var result = await _tempResService.UpdateAsync(_item.TempResId, dto, actorId);
            if (result.IsFailure)
            {
                _lblError.Text = string.Join(Environment.NewLine, result.Errors);
                _lblError.Visible = true;
                return;
            }

            MessageBox.Show(
                "Cập nhật thông tin tạm trú thành công!",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = $"Lỗi: {ex.Message}";
            _lblError.Visible = true;
        }
        finally
        {
            _btnSave.Enabled = true;
        }
    }
}
