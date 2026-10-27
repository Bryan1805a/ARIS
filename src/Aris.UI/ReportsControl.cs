using System;
using System.Text;
using System.Windows.Forms;
using System.IO;

using Aris.Application.Reports;

namespace Aris.UI;

public partial class ReportsControl : UserControl
{
    private readonly IReportService _reportService;

    public ReportsControl(IReportService reportService)
    {
        InitializeComponent();

        _reportService = reportService;

        btnGenerateReport.Click += btnGenerateReport_Click;
        btnExportCsv.Click += btnExportCsv_Click;

        cboReportType.SelectedIndexChanged += cboReportType_SelectedIndexChanged;

        dtpFromDate.Value = DateTime.Today.AddMonths(-1);
        dtpToDate.Value = DateTime.Today;

        UpdateDateFilterVisibility();
    }

    private async void btnGenerateReport_Click(object? sender, EventArgs e)
    {
        if (cboReportType.SelectedIndex == -1)
        {
            MessageBox.Show(
                "Please select a report type.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        string reportType = cboReportType.SelectedItem!.ToString()!;



        btnGenerateReport.Enabled = false;
        btnExportCsv.Enabled = false;

        try
        {
            dgvReports.DataSource = null;
            dgvReports.Columns.Clear();

            if (reportType == "Residence Statistics")
            {
                var data = await _reportService.GetResidenceStatisticsAsync();
                dgvReports.DataSource = data;
            }
            else if (reportType == "Household Statistics")
            {
                var data = await _reportService.GetHouseholdStatisticsAsync();
                dgvReports.DataSource = data;
            }
            else if (reportType == "Residence Movement Report")
            {
                DateOnly fromDate = DateOnly.FromDateTime(dtpFromDate.Value);
                DateOnly toDate = DateOnly.FromDateTime(dtpToDate.Value);

                if (fromDate > toDate)
                {
                    MessageBox.Show(
                        "From Date cannot be later than To Date.",
                        "Invalid Date Range",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                var data = await _reportService.GetResidenceMovementAsync(
                    fromDate,
                    toDate
                );

                dgvReports.DataSource = data;
            }
            btnExportCsv.Enabled = dgvReports.DataSource != null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Report Error Details",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnGenerateReport.Enabled = true;
        }
    }

    private void cboReportType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateDateFilterVisibility();
    }

    private void UpdateDateFilterVisibility()
    {
        bool showDateFilter =
            cboReportType.SelectedItem?.ToString() == "Residence Movement Report";

        lblFromDate.Visible = showDateFilter;
        lblToDate.Visible = showDateFilter;

        dtpFromDate.Visible = showDateFilter;
        dtpToDate.Visible = showDateFilter;
    }

    private void btnExportCsv_Click(object? sender, EventArgs e)
    {
        if (dgvReports.DataSource == null)
            return;

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = "ARIS_Report.csv",
            Title = "Export Report to CSV"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            var csv = new StringBuilder();

            var columns = dgvReports.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c.Visible)
                .OrderBy(c => c.DisplayIndex)
                .ToList();

            string EscapeCsv(string? value)
            {
                return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
            }

            // Export column headers
            csv.AppendLine(string.Join(",",
                columns.Select(c => EscapeCsv(c.HeaderText))));

            // Export report data
            foreach (DataGridViewRow row in dgvReports.Rows)
            {
                if (row.IsNewRow)
                    continue;

                csv.AppendLine(string.Join(",",
                    columns.Select(c =>
                        EscapeCsv(Convert.ToString(row.Cells[c.Index].Value)))));
            }

            File.WriteAllText(
                dialog.FileName,
                csv.ToString(),
                new UTF8Encoding(true)
            );

            MessageBox.Show(
                "Report exported successfully.",
                "Export CSV",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Unable to export the report. Please try again.",
                "Export Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}