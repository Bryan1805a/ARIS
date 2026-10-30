using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;
using Aris.Application.Transfers;
using Aris.Application.Transfers.Dtos;
using Aris.UI.Addresses;

namespace Aris.UI.Transfers;

/// <summary>Maker wizard for the three UC-06 request shapes.</summary>
public sealed class TransferRequestForm : Form
{
    private readonly ITransferService _transferService;
    private readonly IHouseholdService _householdService;
    private readonly IAddressService _addressService;
    private readonly IUserSession _session;

    private readonly ComboBox _type = new();
    private readonly ComboBox _sourceHousehold = new();
    private readonly CheckedListBox _members = new();
    private readonly ComboBox _destinationHousehold = new();
    private readonly ComboBox _destinationAddress = new();
    private readonly Button _newAddressButton = new();
    private readonly ComboBox _newHead = new();
    private readonly ComboBox _successor = new();
    private readonly DateTimePicker _effectiveDate = new();
    private readonly Button _createButton = new();
    private readonly Button _cancelButton = new();
    private readonly Label _hint = new();

    private readonly Label _typeLabel = new();
    private readonly Label _sourceLabel = new();
    private readonly Label _membersLabel = new();
    private readonly Label _destinationHouseholdLabel = new();
    private readonly Label _destinationAddressLabel = new();
    private readonly Label _newHeadLabel = new();
    private readonly Label _successorLabel = new();
    private readonly Label _effectiveLabel = new();

    private List<HouseholdDto> _households = new();
    private List<AddressDto> _addresses = new();
    private List<HouseholdMemberDto> _sourceMembers = new();

    private bool _loading;
    private bool _suppressSourceChange;

    public TransferRequestForm(
        ITransferService transferService,
        IHouseholdService householdService,
        IAddressService addressService,
        IUserSession session)
    {
        _transferService = transferService;
        _householdService = householdService;
        _addressService = addressService;
        _session = session;

        Text = "ARIS - New transfer request";
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(720, 680);

        BuildLayout();
        Shown += async (_, _) => await LoadInitialAsync();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "New relocation request",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        ConfigureLabel(_typeLabel, "Request type", 20, 60);
        _type.Location = new Point(20, 80);
        _type.Size = new Size(660, 24);
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.Items.AddRange(new object[]
        {
            "JOIN_EXISTING", "NEW_HOUSEHOLD", "HOUSEHOLD_MOVE"
        });
        _type.SelectedIndex = 0;

        ConfigureLabel(_sourceLabel, "Source household", 20, 115);
        _sourceHousehold.Location = new Point(20, 135);
        _sourceHousehold.Size = new Size(660, 24);
        _sourceHousehold.DropDownStyle = ComboBoxStyle.DropDownList;
        _sourceHousehold.DisplayMember = nameof(HouseholdDto.DisplayName);
        _sourceHousehold.ValueMember = nameof(HouseholdDto.HouseholdId);

        ConfigureLabel(_membersLabel, "Moving citizens (active members)", 20, 170);
        _members.Location = new Point(20, 192);
        _members.Size = new Size(320, 390);
        _members.CheckOnClick = true;
        _members.IntegralHeight = false;

        ConfigureLabel(_destinationHouseholdLabel, "Destination household", 360, 170);
        _destinationHousehold.Location = new Point(360, 192);
        _destinationHousehold.Size = new Size(330, 24);
        _destinationHousehold.DropDownStyle = ComboBoxStyle.DropDownList;
        _destinationHousehold.DisplayMember = nameof(HouseholdDto.DisplayName);
        _destinationHousehold.ValueMember = nameof(HouseholdDto.HouseholdId);

        ConfigureLabel(_destinationAddressLabel, "Destination address", 360, 228);
        _destinationAddress.Location = new Point(360, 250);
        _destinationAddress.Size = new Size(240, 24);
        _destinationAddress.DropDownStyle = ComboBoxStyle.DropDownList;
        _destinationAddress.DisplayMember = nameof(AddressDto.AddressText);
        _destinationAddress.ValueMember = nameof(AddressDto.AddressId);

        _newAddressButton.Text = "New...";
        _newAddressButton.Location = new Point(610, 248);
        _newAddressButton.Size = new Size(80, 27);
        _newAddressButton.Click += NewAddressButton_Click;

        ConfigureLabel(_newHeadLabel, "New household head (moving)", 360, 286);
        _newHead.Location = new Point(360, 308);
        _newHead.Size = new Size(330, 24);
        _newHead.DropDownStyle = ComboBoxStyle.DropDownList;
        _newHead.DisplayMember = nameof(HouseholdMemberDto.FullName);
        _newHead.ValueMember = nameof(HouseholdMemberDto.CitizenId);

        ConfigureLabel(_successorLabel, "Successor head in source", 360, 344);
        _successor.Location = new Point(360, 366);
        _successor.Size = new Size(330, 24);
        _successor.DropDownStyle = ComboBoxStyle.DropDownList;
        _successor.DisplayMember = nameof(HouseholdMemberDto.FullName);
        _successor.ValueMember = nameof(HouseholdMemberDto.CitizenId);

        ConfigureLabel(_effectiveLabel, "Effective date", 360, 404);
        _effectiveDate.Location = new Point(360, 426);
        _effectiveDate.Size = new Size(240, 24);
        _effectiveDate.Format = DateTimePickerFormat.Custom;
        _effectiveDate.CustomFormat = "yyyy-MM-dd";

        _hint.Location = new Point(360, 464);
        _hint.Size = new Size(330, 105);
        _hint.ForeColor = Color.DimGray;
        _hint.Text = "JOIN_EXISTING: choose the destination household.\n" +
                     "NEW_HOUSEHOLD: choose the destination address and the new head.\n" +
                     "HOUSEHOLD_MOVE: choose the destination address and select every member.";

        _createButton.Text = "Create request";
        _createButton.Location = new Point(470, 610);
        _createButton.Size = new Size(130, 34);
        _createButton.BackColor = Color.SteelBlue;
        _createButton.ForeColor = Color.White;
        _createButton.FlatStyle = FlatStyle.Flat;
        _createButton.Click += CreateButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(610, 610);
        _cancelButton.Size = new Size(80, 34);
        _cancelButton.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            title, _typeLabel, _type, _sourceLabel, _sourceHousehold,
            _membersLabel, _members, _destinationHouseholdLabel, _destinationHousehold,
            _destinationAddressLabel, _destinationAddress, _newAddressButton,
            _newHeadLabel, _newHead, _successorLabel, _successor,
            _effectiveLabel, _effectiveDate, _hint, _createButton, _cancelButton
        });

        _type.SelectedIndexChanged += (_, _) => UpdateTypeVisibility();
        _sourceHousehold.SelectedIndexChanged += async (_, _) =>
        {
            if (_suppressSourceChange)
            {
                return;
            }

            await LoadSourceMembersAsync();
        };
        _members.ItemCheck += (_, _) => BeginInvoke(new Action(UpdateHeadPickers));
    }

    private static void ConfigureLabel(Label label, string text, int x, int y)
    {
        label.Text = text;
        label.AutoSize = true;
        label.Location = new Point(x, y);
    }

    private async Task LoadInitialAsync()
    {
        _createButton.Enabled = false;

        try
        {
            var households = await _householdService.SearchAsync(new HouseholdSearchCriteria
            {
                Status = "ACTIVE"
            });

            if (households.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, households.Errors),
                    "Cannot load households",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _households = households.Value?.ToList() ?? new List<HouseholdDto>();

            // Assigning DataSource raises SelectedIndexChanged. Suppress it here so the
            // form does not start a second query on the shared scoped DbContext while
            // this method is still loading addresses.
            _suppressSourceChange = true;
            try
            {
                _sourceHousehold.DataSource = _households;
            }
            finally
            {
                _suppressSourceChange = false;
            }

            var addresses = await _addressService.GetActiveAsync();
            _addresses = addresses.Value?.ToList() ?? new List<AddressDto>();
            _destinationAddress.DataSource = _addresses;

            if (_households.Count == 0)
            {
                _createButton.Enabled = false;
                _hint.Text = "There are no active households.";
                return;
            }

            UpdateTypeVisibility();
            await LoadSourceMembersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading request data: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void UpdateTypeVisibility()
    {
        var type = _type.SelectedItem?.ToString() ?? "JOIN_EXISTING";
        var isJoin = type == "JOIN_EXISTING";
        var isNew = type == "NEW_HOUSEHOLD";
        var isMove = type == "HOUSEHOLD_MOVE";

        _destinationHouseholdLabel.Visible = isJoin;
        _destinationHousehold.Visible = isJoin;

        _destinationAddressLabel.Visible = isNew || isMove;
        _destinationAddress.Visible = isNew || isMove;
        _newAddressButton.Visible = isNew || isMove;

        _newHeadLabel.Visible = isNew;
        _newHead.Visible = isNew;

        _successorLabel.Visible = !isMove;
        _successor.Visible = !isMove;

        if (isMove && _members.Items.Count > 0)
        {
            for (var i = 0; i < _members.Items.Count; i++)
            {
                _members.SetItemChecked(i, true);
            }
        }

        RebuildDestinationHouseholds();
        UpdateHeadPickers();
    }

    private void RebuildDestinationHouseholds()
    {
        if (_sourceHousehold.SelectedValue is not int sourceId)
        {
            return;
        }

        var destinations = _households
            .Where(h => h.HouseholdId != sourceId)
            .ToList();

        _destinationHousehold.DataSource = destinations;
    }

    private async Task LoadSourceMembersAsync()
    {
        if (_loading)
        {
            return;
        }

        if (_sourceHousehold.SelectedValue is not int householdId)
        {
            return;
        }

        _loading = true;
        _sourceHousehold.Enabled = false;
        _createButton.Enabled = false;
        _newAddressButton.Enabled = false;

        try
        {
            var detail = await _householdService.GetByIdAsync(householdId);

            if (detail.IsFailure || detail.Value is null)
            {
                _sourceMembers = new List<HouseholdMemberDto>();
                _members.Items.Clear();

                MessageBox.Show(
                    string.Join(Environment.NewLine, detail.Errors.DefaultIfEmpty("Household not found.")),
                    "Cannot load household",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _sourceMembers = detail.Value.Members
                .Where(m => m.Status == "ACTIVE")
                .ToList();

            _members.Items.Clear();

            foreach (var member in _sourceMembers)
            {
                _members.Items.Add($"{member.FullName} ({member.NationalId}) - {member.Role}");
            }

            RebuildDestinationHouseholds();
            UpdateTypeVisibility();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while loading household members: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _loading = false;
            _sourceHousehold.Enabled = true;
            _newAddressButton.Enabled = true;
            _createButton.Enabled = _sourceMembers.Count > 0;
        }
    }

    private List<HouseholdMemberDto> CheckedMembers()
    {
        var members = new List<HouseholdMemberDto>();

        for (var i = 0; i < _members.Items.Count; i++)
        {
            if (_members.GetItemChecked(i) && i < _sourceMembers.Count)
            {
                members.Add(_sourceMembers[i]);
            }
        }

        return members;
    }

    private void UpdateHeadPickers()
    {
        var selected = CheckedMembers();
        var selectedIds = selected.Select(m => m.CitizenId).ToHashSet();
        var remaining = _sourceMembers
            .Where(m => !selectedIds.Contains(m.CitizenId))
            .ToList();

        _newHead.DataSource = selected.ToList();
        _successor.DataSource = remaining;

        var headSelected = _sourceMembers.Any(
            m => m.Role == "HEAD" && selectedIds.Contains(m.CitizenId));
        _successor.Enabled = headSelected && remaining.Count > 0;

        if (_type.SelectedItem?.ToString() == "NEW_HOUSEHOLD" && selected.Count == 0)
        {
            _hint.Text = "Select at least one moving citizen and choose the new head.";
        }
        else if (_type.SelectedItem?.ToString() != "HOUSEHOLD_MOVE"
                 && _sourceMembers.Any(m => m.Role == "HEAD" && selectedIds.Contains(m.CitizenId))
                 && remaining.Count > 0)
        {
            _hint.Text = "The source head is moving. Choose a successor head who remains.";
        }
        else
        {
            _hint.Text = "Choose the moving citizens and the destination. " +
                         "HOUSEHOLD_MOVE requires every active member.";
        }
    }

    private async void NewAddressButton_Click(object? sender, EventArgs e)
    {
        using var form = new AddressEditForm(_addressService, _session);

        if (form.ShowDialog(this) == DialogResult.OK && form.CreatedAddressId is int addressId)
        {
            var addresses = await _addressService.GetActiveAsync();
            _addresses = addresses.Value?.ToList() ?? new List<AddressDto>();
            _destinationAddress.DataSource = _addresses;
            _destinationAddress.SelectedValue = addressId;
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

        var type = _type.SelectedItem?.ToString();
        if (type is null
            || _sourceHousehold.SelectedValue is not int sourceHouseholdId)
        {
            MessageBox.Show(
                "Select the request type and source household.",
                "Missing information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var selected = CheckedMembers();
        if (selected.Count == 0)
        {
            MessageBox.Show(
                "Select at least one moving citizen.",
                "Missing information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        int? destinationHouseholdId = null;
        int? destinationAddressId = null;
        int? newHeadId = null;
        int? successorId = null;

        if (type == "JOIN_EXISTING")
        {
            if (_destinationHousehold.SelectedValue is not int destinationId)
            {
                MessageBox.Show(
                    "Select the destination household.",
                    "Missing information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            destinationHouseholdId = destinationId;
        }
        else
        {
            if (_destinationAddress.SelectedValue is not int addressId)
            {
                MessageBox.Show(
                    "Select the destination address.",
                    "Missing information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            destinationAddressId = addressId;
        }

        if (type == "NEW_HOUSEHOLD")
        {
            if (_newHead.SelectedValue is not int headId)
            {
                MessageBox.Show(
                    "Choose the new household head from the moving citizens.",
                    "Missing information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            newHeadId = headId;
        }

        if (type != "HOUSEHOLD_MOVE"
            && _successor.Enabled
            && _successor.SelectedValue is int successorValue)
        {
            successorId = successorValue;
        }

        _createButton.Enabled = false;

        try
        {
            var result = await _transferService.CreateRequestAsync(new CreateTransferRequestDto
            {
                RequestType = type,
                SourceHouseholdId = sourceHouseholdId,
                CitizenIds = selected.Select(m => m.CitizenId).ToList(),
                DestinationHouseholdId = destinationHouseholdId,
                DestinationAddressId = destinationAddressId,
                EffectiveDate = DateOnly.FromDateTime(_effectiveDate.Value),
                NewHeadCitizenId = newHeadId,
                SuccessorHeadCitizenId = successorId
            }, userId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot create transfer request",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                $"Transfer request #{result.Value} was queued for another officer to decide.",
                "Request created",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unexpected error while creating the request: {ex.Message}",
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
