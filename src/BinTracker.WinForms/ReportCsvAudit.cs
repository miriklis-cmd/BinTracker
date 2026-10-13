using BinTracker.Services;

namespace BinTracker.WinForms;

internal static class ReportCsvAudit
{
    public static async Task WriteAsync(
        IWin32Window owner,
        IReportExportAuditService audit,
        CsvReportExportAuditIntent intent)
    {
        try
        {
            await audit.RecordCsvExportAsync(intent);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                owner,
                "The CSV file was created, but BinTracker could not write the export audit event.\n\n" + ex.Message,
                "CSV Audit Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
