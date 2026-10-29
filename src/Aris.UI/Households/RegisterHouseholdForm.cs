using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;
using Aris.UI.Addresses;

namespace Aris.UI.Households;

/// <summary>Atomic household registration with a head, address and effective date.</summary>
public sealed class RegisterHouseholdForm : Form
{
    private readonly IHouseholdService _householdService;
    private readonly ICitizenService _citizenService;
    private readonly IAddressService _addressService;
    private readonly IUserSession _session;

    private readonly ComboBox _head = new();
    private readonly ComboBox _address = new();
    private readonly DateTimePicker _effectiveDate = new();
    private readonly Button _createButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _newAddressButton = new();
    private readonly Label _hint = new();

    public RegisterHouseholdForm(
        IHouseholdService householdService,
        ICitizenService citizenService,
        IAddressService addressService,
        IUserSession session)
    {
        _householdService = householdService;
        _citizenService = citizenService;
        _addressService = addressService;
        _session = session;

        Text = "ARIS - Register household";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(540, 345);

        BuildLayout();
        Shown += async (_, _) => await LoadCandidatesAsync();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Register a household",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var headLabel = new Label
        {
            Text = "Head citizen",
            AutoSize = true,
            Location = new Point(20, 68)
        };

        _head.Location = new Point(150, 65);
        _head.Size = new Size(360, 23);
        _head.DropDownStyle = ComboBoxStyle.DropDownList;
        _head.DisplayMember = nameof(CitizenDto.FullName);
        _head.ValueMember = nameof(CitizenDto.CitizenId);

        var addressLabel = new Label
        {
            Text = "Address",
            AutoSize = true,
            Location = new Point(20, 108)
        };

        _address.Location = new Point(150, 105);
        _address.Size = new Size(250, 23);
        _address.DropDownStyle = ComboBoxStyle.DropDownList;
        _address.DisplayMember = nameof(AddressDto.AddressText);
        _address.ValueMember = nameof(AddressDto.AddressId);

        _newAddressButton.Text = "New...";
        _newAddressButton.Location = new Point(410, 103);
        _newAddressButton.Size = new Size(100, 27);
        _newAddressButton.Click += NewAddressButton_Click;

        var dateLabel = new Label
        {
            Text = "Effective date",
            AutoSize = true,
            Location = new Point(20, 148)
        };

        _effectiveDate.Location = new Point(150, 145);
        _effectiveDate.Size = new Size(250, 23);
        _effectiveDate.Format = DateTimePickerFormat.Custom;
        _effectiveDate.CustomFormat = "yyyy-MM-dd";

        _hint.AutoSize = false;
        _hint.Size = new Size(500, 45);
        _hint.Location = new Point(20, 185);
        _hint.ForeColor = Color.DimGray;
        _hint.Text = "The head must be UNASSIGNED. Registration creates the household, " +
                     "address history, HEAD membership and residence in one transaction.";

        _createButton.Text = "Register";
        _createButton.Location = new Point(285, 285);
        _createButton.Size = new Size(110, 32);
        _createButton.BackColor = Color.SteelBlue;
        _createButton.ForeColor = Color.White;
        _createButton.FlatStyle = FlatStyle.Flat;
        _createButton.Click += CreateButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(410, 285);
        _cancelButton.Size = new Size(100, 32);
        _cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = _createButton;
        CancelButton = _cancelButton;

        Controls.AddRange(new Control[]
        {
            title, headLabel, _head, addressLabel, _address, _newAddressButton,
            dateLabel, _effectiveDate, _hint, _createButton, _cancelButton
        });
    }

    private async Task LoadCandidatesAsync()
    {
        _createButton.Enabled = false;

        try
        {
            var citizens = await _citizenService.SearchAsync(new CitizenSearchCriteria
            {
                Status = "UNASSIGNED"
            });

            if (citizens.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, citizens.Errors),
                    "Cannot load citizens",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var headCandidates = citizens.Value?.ToList() ?? new List<CitizenDto>();
            _head.DataSource = headCandidates;

            await LoadAddressesAsync();

            if (headCandidates.Count == 0)
            {
                _hint.Text = "There are no UNASSIGNED citizens. Register a citizen first, " +
                             "then return to register their household.";
            }
            else if (_address.Items.Count == 0)
            {
                _hint.Text = "There are no active addresses. Use New... to register one.";
            }
            else
            {
                _createButton.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading household candidates: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task LoadAddressesAsync()
    {
        var addresses = await _addressService.GetActiveAsync();

        if (addresses.IsFailure)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, addresses.Errors),
                "Cannot load addresses",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _address.DataSource = addresses.Value?.ToList() ?? new List<AddressDto>();
    }

    private async void NewAddressButton_Click(object? sender, EventArgs e)
    {
        using var form = new AddressEditForm(_addressService, _session);

        if (form.ShowDialog(this) == DialogResult.OK && form.CreatedAddressId is int addressId)
        {
            await LoadAddressesAsync();
            _address.SelectedValue = addressId;
        }
    }

    private async void CreateButton_Click(object? sender, EventArgs e)
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

        if (_head.SelectedValue is not int headCitizenId
            || _address.SelectedValue is not int addressId)
        {
            MessageBox.Show(
                "Select a head citizen and an address.",
                "Missing information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _createButton.Enabled = false;

        try
        {
            var result = await _householdService.RegisterHouseholdAsync(
                new RegisterHouseholdDto
                {
                    HeadCitizenId = headCitizenId,
                    AddressId = addressId,
                    EffectiveDate = DateOnly.FromDateTime(_effectiveDate.Value)
                },
                userId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot register household",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                $"Household #{result.Value} was registered.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while registering the household: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _createButton.Enabled = true;
        }
    }
}
