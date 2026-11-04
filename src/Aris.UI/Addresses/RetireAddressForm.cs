using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Common;

namespace Aris.UI.Addresses;

/// <summary>Retire an address and record its successor (<c>ADR-03</c>, <c>D9</c>).</summary>
public sealed class RetireAddressForm : Form {
    private readonly IAddressService _addressService;
    private readonly IUserSession _session;
    private readonly int _addressId;
    private readonly string _addressText;

    private readonly ComboBox _successor = new();
    private readonly DateTimePicker _validTo = new();
    private readonly Button _retireButton = new();
    private readonly Button _cancelButton = new();

    public RetireAddressForm(
        IAddressService addressService,
        IUserSession session,
        int addressId,
        string addressText) {
        _addressService = addressService;
        _session = session;
        _addressId = addressId;
        _addressText = addressText;

        Text = "ARIS - Retire address";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 300);

        BuildLayout();
        Aris.UI.UiTheme.ApplyScreen(this);
        Shown += async (_, _) => await LoadSuccessorsAsync();
    }

    private void BuildLayout() {
        var title = new Label {
            Text = $"Retire address #{_addressId}",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var addressLabel = new Label {
            Text = _addressText,
            AutoSize = false,
            Size = new Size(520, 40),
            Location = new Point(20, 52),
            ForeColor = Color.DimGray
        };

        var successorLabel = new Label {
            Text = "Successor address",
            AutoSize = true,
            Location = new Point(20, 105)
        };

        _successor.Location = new Point(170, 102);
        _successor.Size = new Size(370, 23);
        _successor.DropDownStyle = ComboBoxStyle.DropDownList;
        _successor.DisplayMember = nameof(AddressDto.AddressText);
        _successor.ValueMember = nameof(AddressDto.AddressId);

        var validToLabel = new Label {
            Text = "Valid to",
            AutoSize = true,
            Location = new Point(20, 145)
        };

        _validTo.Location = new Point(170, 142);
        _validTo.Size = new Size(370, 23);
        _validTo.Format = DateTimePickerFormat.Custom;
        _validTo.CustomFormat = "yyyy-MM-dd";
        _validTo.Value = DateTime.Today;

        var warning = new Label {
            Text = "Historical residences keep the old address. The canonical view resolves it to the successor.",
            AutoSize = false,
            Size = new Size(520, 45),
            Location = new Point(20, 180),
            ForeColor = Color.Gray
        };

        _retireButton.Text = "Retire address";
        _retireButton.Location = new Point(320, 245);
        _retireButton.Size = new Size(120, 32);
        _retireButton.BackColor = Color.SteelBlue;
        _retireButton.ForeColor = Color.White;
        _retireButton.FlatStyle = FlatStyle.Flat;
        _retireButton.Click += RetireButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(450, 245);
        _cancelButton.Size = new Size(90, 32);
        _cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = _retireButton;
        CancelButton = _cancelButton;

        Controls.AddRange(new Control[]
        {
            title, addressLabel, successorLabel, _successor,
            validToLabel, _validTo, warning, _retireButton, _cancelButton
        });
    }

    private async Task LoadSuccessorsAsync() {
        try {
            var result = await _addressService.GetActiveAsync();

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot load addresses",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _retireButton.Enabled = false;
                return;
            }

            var candidates = result.Value is null ? new List<AddressDto>() : result.Value.Where(a => a.AddressId != _addressId).ToList();

            _successor.DataSource = candidates;

            if (candidates.Count == 0) {
                _retireButton.Enabled = false;
                MessageBox.Show(
                    "There are no other active addresses to use as a successor.",
                    "No successor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while loading addresses: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void RetireButton_Click(object? sender, EventArgs e) {
        if (_session.UserId is not int userId) {
            MessageBox.Show(
                "Your session has expired. Sign in again.",
                "Session expired",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_successor.SelectedValue is not int successorId) {
            MessageBox.Show(
                "Select the successor address.",
                "Missing information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _retireButton.Enabled = false;

        try {
            var result = await _addressService.RetireAsync(new RetireAddressDto {
                AddressId = _addressId,
                ReplacedByAddressId = successorId,
                ValidTo = DateOnly.FromDateTime(_validTo.Value)
            }, userId);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot retire address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                "The address was retired.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while retiring the address: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            _retireButton.Enabled = true;
        }
    }
}