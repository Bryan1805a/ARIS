using Aris.Application.SystemConfiguration;
using Aris.Application.SystemConfiguration.Dtos;

using System;
using System.Windows.Forms;

namespace Aris.UI;

public partial class SystemConfigurationControl : UserControl {
    private readonly SystemConfigurationService _service;

    public SystemConfigurationControl(SystemConfigurationService service) {
        InitializeComponent();

        _service = service;

        // Wire refresh-sensitive events so the editor always follows the
        // currently selected configuration row.
        this.Load += SystemConfigurationControl_Load;
        dgvConfigurations.SelectionChanged += dgvConfigurations_SelectionChanged;
        btnSaveChanges.Click += btnSaveChanges_Click;
        UiTheme.ApplyScreen(this);
    }

    private async void SystemConfigurationControl_Load(
        object? sender, EventArgs e) {
        // The service performs the authoritative administrator-session check;
        // this control only displays the result or access error.
        await LoadConfigurationsAsync();
    }

    private async Task LoadConfigurationsAsync() {
        try {
            var result = await _service.GetAllAsync();

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Cannot load configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Rebinding replaces the previous snapshot and triggers selection
            // handling for the first available configuration, if any.
            dgvConfigurations.DataSource = result.Value?.ToList() ?? new List<SystemConfigurationDto>();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while loading configuration: {ex.Message}",
                "Configuration Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void dgvConfigurations_SelectionChanged(
        object? sender, EventArgs e) {
        if (dgvConfigurations.CurrentRow?.DataBoundItem is not SystemConfigurationDto config) {
            // Clear stale values when the grid has no valid selected DTO.
            txtConfigValue.Clear();
            btnSaveChanges.Enabled = false;
            return;
        }

        // Load the selected value into the editor without modifying the DTO
        // until the user explicitly saves.
        txtConfigValue.Text = config.ConfigValue;
        btnSaveChanges.Enabled = true;
    }

    private async void btnSaveChanges_Click(object? sender, EventArgs e) {
        if (dgvConfigurations.CurrentRow?.DataBoundItem is not SystemConfigurationDto config) {
            MessageBox.Show(
                "Please select a configuration.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        string newValue = txtConfigValue.Text.Trim();

        // Empty input is rejected immediately; numeric/range validation remains
        // centralized in the application service.
        if (string.IsNullOrWhiteSpace(newValue)) {
            MessageBox.Show(
                "Configuration value cannot be empty.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        btnSaveChanges.Enabled = false;

        try {
            // The service performs authorization, conversion, persistence, and
            // audit logging for the selected configuration key.
            var result = await _service.UpdateAsync(
                config.ConfigKey,
                newValue);

            if (result.IsFailure) {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Errors),
                    "Update Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Reload after saving so the grid reflects the committed value and
            // the selected DTO is not left with stale data.
            await LoadConfigurationsAsync();

            MessageBox.Show(
                "Configuration updated successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unexpected error while updating configuration: {ex.Message}",
                "Configuration Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        } finally {
            // Restore editing only when a valid configuration remains selected.
            btnSaveChanges.Enabled =
                dgvConfigurations.CurrentRow?.DataBoundItem
                is SystemConfigurationDto;
        }
    }
}