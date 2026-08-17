using BinTracker.Services;

namespace BinTracker.WinForms;

internal static class ReportCsvAudit
{
    public static async Task WriteAsync(
        IWin32Window owner,
        IAuditService audit,
        string action,
        string entityId,
        string description,
        string outputPath,
        int rowCount,
        object? context = null)
    {
        try
        {
            await audit.WriteAsync(
                action,
                "Report",
                entityId,
                description,
                after: new
                {
                    Format = "CSV",
                    RowCount = rowCount,
                    FileName = Path.GetFileName(outputPath),
                    Context = context
                });
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
