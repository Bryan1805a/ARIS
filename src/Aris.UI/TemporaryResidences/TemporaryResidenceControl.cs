using Aris.Application.Addresses;
using Aris.Application.Citizens;
using Aris.Application.Common;
using Aris.Application.TemporaryResidences;
using Aris.Application.TemporaryResidences.Dtos;
using Aris.UI.Localization;

namespace Aris.UI.TemporaryResidences;

/// <summary>
/// Màn hình Quản lý Đăng ký Tạm trú (Temporary Residence Management).
/// Thực hiện đầy đủ các chức năng: Xem danh sách, Tìm kiếm, Lọc, Thêm mới, Gia hạn / Sửa, Hủy tạm trú.
/// </summary>
public sealed class TemporaryResidenceControl : UserControl
{
    private readonly ITemporaryResidenceService _tempResService;
    private readonly IAddressService _addressService;
    private readonly ICitizenService _citizenService;
    private readonly IUserSession _session;

    private readonly Label _title = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _status = new();
    private readonly Button _searchButton = new();
    private readonly Button _registerButton = new();
    private readonly Button _editButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _refreshButton = new();
    private readonly DataGridView _grid = new();

    private readonly Panel _pnlTop = new();
    private readonly Panel _pnlBottom = new();
    private readonly Panel _pnlCenter = new();

    private bool _loading;

    public TemporaryResidenceControl(
        ITemporaryResidenceService tempResService,
        IAddressService addressService,
        ICitizenService citizenService,
        IUserSession session)
    {
        _tempResService = tempResService;
        _addressService = addressService;
        _citizenService = citizenService;
        _session = session;

        BuildLayout();
        UiTheme.ApplyScreen(this);
        Load += async (_, _) => await LoadDataAsync();
    }

    private void BuildLayout()
    {
        Size = new Size(980, 560);
        MinimumSize = new Size(800, 480);
        AutoScroll = true;

        // --- 1. KHUNG TRÊN CÙNG (Tiêu đề & Thanh tìm kiếm) ---
        _pnlTop.Dock = DockStyle.Top;
        _pnlTop.Height = 115;
        _pnlTop.BackColor = Color.Transparent;

        _title.Text = LanguageManager.T("Quản lý Đăng ký Tạm trú");
        _title.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold);
        _title.ForeColor = UiTheme.Accent;
        _title.AutoSize = true;
        _title.Location = new Point(25, 18);

        _search.Location = new Point(25, 70);
        _search.Size = new Size(240, 28);
        _search.PlaceholderText = LanguageManager.T("Tìm theo CCCD hoặc Họ tên");

        _status.Location = new Point(275, 70);
        _status.Size = new Size(120, 28);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(new object[] { "Tất cả", "ACTIVE", "EXPIRED", "CANCELLED" });
        _status.SelectedIndex = 0;

        _searchButton.Text = LanguageManager.T("Tìm kiếm");
        _searchButton.Location = new Point(405, 69);
        _searchButton.Size = new Size(90, 30);

        _registerButton.Text = LanguageManager.T("+ Đăng ký tạm trú");
        _registerButton.Location = new Point(505, 69);
        _registerButton.Size = new Size(150, 30);
        _registerButton.BackColor = UiTheme.Accent;
        _registerButton.ForeColor = Color.White;
        _registerButton.FlatStyle = FlatStyle.Flat;

        _refreshButton.Text = LanguageManager.T("Làm mới");
        _refreshButton.Location = new Point(665, 69);
        _refreshButton.Size = new Size(95, 30);

        _pnlTop.Controls.AddRange(new Control[]
        {
            _title, _search, _status, _searchButton, _registerButton, _refreshButton
        });

        // --- 2. KHUNG DƯỚI CÙNG (Chứa 2 nút thao tác Gia hạn/Sửa và Hủy tạm trú ở dưới theo ý bạn) ---
        _pnlBottom.Dock = DockStyle.Bottom;
        _pnlBottom.Height = 65;
        _pnlBottom.BackColor = Color.Transparent;

        _editButton.Text = LanguageManager.T("Gia hạn / Sửa");
        _editButton.Location = new Point(25, 12);
        _editButton.Size = new Size(140, 36);
        _editButton.BackColor = Color.SteelBlue;
        _editButton.ForeColor = Color.White;
        _editButton.FlatStyle = FlatStyle.Flat;

        _cancelButton.Text = LanguageManager.T("Hủy tạm trú");
        _cancelButton.Location = new Point(180, 12);
        _cancelButton.Size = new Size(130, 36);
        _cancelButton.BackColor = Color.IndianRed;
        _cancelButton.ForeColor = Color.White;
        _cancelButton.FlatStyle = FlatStyle.Flat;

        _pnlBottom.Controls.AddRange(new Control[] { _editButton, _cancelButton });

        // --- 3. KHUNG GIỮA: BẢNG DỮ LIỆU CÓ ĐỦ 2 THANH CUỘN (NGANG & DỌC) ---
        _pnlCenter.Dock = DockStyle.Fill;
        _pnlCenter.Padding = new Padding(25, 0, 25, 5);
        _pnlCenter.BackColor = Color.Transparent;

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // Không ép cột để kích hoạt thanh cuộn ngang
        _grid.BackgroundColor = Color.White;
        _grid.ScrollBars = ScrollBars.Both; // Đầy đủ thanh cuộn dọc (bên phải) và thanh cuộn ngang (ở dưới)

        _pnlCenter.Controls.Add(_grid);

        // Thứ tự Add controls vào UserControl để docking chuẩn xác
        Controls.Add(_pnlCenter);
        Controls.Add(_pnlBottom);
        Controls.Add(_pnlTop);

        // Sự kiện click
        _searchButton.Click += async (_, _) => await LoadDataAsync();
        _refreshButton.Click += async (_, _) => await LoadDataAsync();
        _status.SelectedIndexChanged += async (_, _) => await LoadDataAsync();
        _registerButton.Click += RegisterButton_Click;
        _editButton.Click += EditButton_Click;
        _cancelButton.Click += CancelButton_Click;
        _grid.CellDoubleClick += (_, _) => EditButton_Click(this, EventArgs.Empty);

        _search.KeyDown += async (_, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                await LoadDataAsync();
            }
        };
    }

    private async Task LoadDataAsync()
    {
        if (_loading || IsDisposed)
        {
            return;
        }

        _loading = true;
        SetBusy(true);

        try
        {
            var text = _search.Text.Trim();
            var selectedStatus = _status.SelectedItem?.ToString();
            if (selectedStatus == "Tất cả")
            {
                selectedStatus = "All";
            }

            var criteria = new TemporaryResidenceSearchCriteria(
                SearchText: string.IsNullOrWhiteSpace(text) ? null : text,
                Status: selectedStatus
            );

            var result = await _tempResService.SearchAsync(criteria);
            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Lỗi tra cứu tạm trú",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var list = result.Value?.ToList() ?? new List<TemporaryResidenceDto>();
            _grid.DataSource = list;

            // Đặt tên tiêu đề cột hiển thị thân thiện
            ConfigureGridColumns();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Đã xảy ra lỗi khi tải dữ liệu tạm trú: {ex.Message}",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _loading = false;
            SetBusy(false);
        }
    }

    private void ConfigureGridColumns()
    {
        if (_grid.Columns.Count == 0) return;

        if (_grid.Columns["TempResId"] is { } cId)
        {
            cId.HeaderText = "Mã TT";
            cId.Width = 70;
        }
        if (_grid.Columns["CitizenId"] is { } cCid) cCid.Visible = false;
        if (_grid.Columns["AddressId"] is { } cAid) cAid.Visible = false;

        if (_grid.Columns["NationalId"] is { } cNid)
        {
            cNid.HeaderText = "Số CCCD";
            cNid.Width = 125;
        }
        if (_grid.Columns["FullName"] is { } cFn)
        {
            cFn.HeaderText = "Họ và tên";
            cFn.Width = 180;
        }
        if (_grid.Columns["AddressText"] is { } cAddr)
        {
            cAddr.HeaderText = "Địa chỉ tạm trú";
            cAddr.Width = 280;
        }
        if (_grid.Columns["StartDate"] is { } cSd)
        {
            cSd.HeaderText = "Từ ngày";
            cSd.Width = 110;
            cSd.DefaultCellStyle.Format = "dd/MM/yyyy";
        }
        if (_grid.Columns["EndDate"] is { } cEd)
        {
            cEd.HeaderText = "Đến ngày";
            cEd.Width = 110;
            cEd.DefaultCellStyle.Format = "dd/MM/yyyy";
        }
        if (_grid.Columns["StayReason"] is { } cSr)
        {
            cSr.HeaderText = "Lý do";
            cSr.Width = 150;
        }
        if (_grid.Columns["HostInfo"] is { } cHi)
        {
            cHi.HeaderText = "Chủ hộ / Bảo lãnh";
            cHi.Width = 160;
        }
        if (_grid.Columns["Status"] is { } cSt)
        {
            cSt.HeaderText = "Trạng thái";
            cSt.Width = 100;
        }
        if (_grid.Columns["Notes"] is { } cNt)
        {
            cNt.HeaderText = "Ghi chú";
            cNt.Width = 170;
        }
        if (_grid.Columns["CreatedAt"] is { } cCa)
        {
            cCa.HeaderText = "Ngày lập";
            cCa.Width = 145;
            cCa.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }
        if (_grid.Columns["CreatedByUsername"] is { } cCb)
        {
            cCb.HeaderText = "Cán bộ lập";
            cCb.Width = 115;
        }
    }

    private void SetBusy(bool busy)
    {
        _searchButton.Enabled = !busy;
        _registerButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        _editButton.Enabled = !busy;
        _cancelButton.Enabled = !busy;
        _status.Enabled = !busy;
    }

    private TemporaryResidenceDto? GetSelectedItem()
        => _grid.CurrentRow?.DataBoundItem as TemporaryResidenceDto;

    private async void RegisterButton_Click(object? sender, EventArgs e)
    {
        using var form = new RegisterTemporaryResidenceForm(
            _tempResService, _addressService, _citizenService, _session);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await LoadDataAsync();
        }
    }

    private async void EditButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedItem();
        if (selected == null)
        {
            MessageBox.Show(
                "Vui lòng chọn một dòng đăng ký tạm trú để xem hoặc chỉnh sửa.",
                "Chưa chọn dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (selected.Status != "ACTIVE")
        {
            MessageBox.Show(
                $"Hồ sơ tạm trú này có trạng thái '{selected.Status}', chỉ hồ sơ đang còn hiệu lực (ACTIVE) mới được gia hạn/chỉnh sửa.",
                "Không thể chỉnh sửa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var form = new EditTemporaryResidenceForm(_tempResService, _session, selected);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await LoadDataAsync();
        }
    }

    private async void CancelButton_Click(object? sender, EventArgs e)
    {
        var selected = GetSelectedItem();
        if (selected == null)
        {
            MessageBox.Show(
                "Vui lòng chọn một dòng đăng ký tạm trú để thực hiện hủy.",
                "Chưa chọn dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (selected.Status != "ACTIVE")
        {
            MessageBox.Show(
                $"Hồ sơ tạm trú này đã ở trạng thái '{selected.Status}'.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn hủy đăng ký tạm trú của công dân '{selected.FullName}' (CCCD: {selected.NationalId}) tại '{selected.AddressText}' không?",
            "Xác nhận hủy đăng ký tạm trú",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        var actorId = _session.UserId ?? 1;
        var result = await _tempResService.CancelAsync(selected.TempResId, "Công dân kết thúc tạm trú / rời đi", actorId);
        if (result.IsFailure)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Errors),
                "Lỗi hủy tạm trú",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(
            "Đã hủy đăng ký tạm trú thành công!",
            "Thành công",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        await LoadDataAsync();
    }
}
