using Aris.Application.Reports;

using System;
using System.Windows.Forms;

namespace Aris.UI;

public partial class ReportsControl : UserControl
{
    private readonly IReportService _reportService;

    public ReportsControl(IReportService reportService)
    {
        InitializeComponent();

        _reportService = reportService;

        btnGenerateReport.Click += btnGenerateReport_Click;

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
}