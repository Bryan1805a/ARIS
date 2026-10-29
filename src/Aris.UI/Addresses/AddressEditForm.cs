using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Common;

namespace Aris.UI.Addresses;

/// <summary>Register a normalized address (<c>ADR-01</c>).</summary>
public sealed class AddressEditForm : Form
{
    private readonly IAddressService _addressService;
    private readonly IUserSession _session;

    private readonly TextBox _code = new();
    private readonly TextBox _line = new();
    private readonly TextBox _ward = new();
    private readonly TextBox _district = new();
    private readonly TextBox _province = new();
    private readonly DateTimePicker _validFrom = new();
    private readonly Button _saveButton = new();
    private readonly Button _cancelButton = new();

    public AddressEditForm(IAddressService addressService, IUserSession session)
    {
        _addressService = addressService;
        _session = session;

        Text = "ARIS - Register address";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(500, 380);

        BuildLayout();
    }

    /// <summary>The new address id after <see cref="DialogResult.OK"/>.</summary>
    public int? CreatedAddressId { get; private set; }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Register an address",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        AddRow("Address code", _code, 60);
        AddRow("Address line", _line, 100);
        AddRow("Ward", _ward, 140);
        AddRow("District (optional)", _district, 180);
        AddRow("Province", _province, 220);

        var validFromLabel = new Label
        {
            Text = "Valid from",
            AutoSize = true,
            Location = new Point(20, 263)
        };

        _validFrom.Location = new Point(150, 260);
        _validFrom.Size = new Size(320, 23);
        _validFrom.Format = DateTimePickerFormat.Custom;
        _validFrom.CustomFormat = "yyyy-MM-dd";

        _saveButton.Text = "Register address";
        _saveButton.Location = new Point(250, 315);
        _saveButton.Size = new Size(130, 32);
        _saveButton.BackColor = Color.SteelBlue;
        _saveButton.ForeColor = Color.White;
        _saveButton.FlatStyle = FlatStyle.Flat;
        _saveButton.Click += SaveButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(390, 315);
        _cancelButton.Size = new Size(90, 32);
        _cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        Controls.AddRange(new Control[]
        {
            title, validFromLabel, _validFrom, _saveButton, _cancelButton
        });
    }

    private void AddRow(string labelText, TextBox textBox, int top)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Location = new Point(20, top + 3)
        };

        textBox.Location = new Point(150, top);
        textBox.Size = new Size(320, 23);

        Controls.Add(label);
        Controls.Add(textBox);
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_session.UserId is not int userId)
        {
            MessageBox.Show(
                "Your session has expired. Sign in again.",
                "Session expired",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _saveButton.Enabled = false;

        try
        {
            var result = await _addressService.CreateAsync(new CreateAddressDto
            {
                AddressCode = _code.Text.Trim(),
                AddressLine = _line.Text.Trim(),
                Ward = _ward.Text.Trim(),
                District = string.IsNullOrWhiteSpace(_district.Text) ? null : _district.Text.Trim(),
                Province = _province.Text.Trim(),
                ValidFrom = DateOnly.FromDateTime(_validFrom.Value)
            }, userId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot register address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            CreatedAddressId = result.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while registering the address: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _saveButton.Enabled = true;
        }
    }
}
