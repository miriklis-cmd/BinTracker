using BinTracker.Services;

namespace BinTracker.WinForms;

internal static class CustomerStatementWorkflow
{
    public static async Task RunAsync(
        IWin32Window owner,
        int customerId,
        ICustomerService customers,
        ICustomerStatementReportService statementReports,
        IBusinessClock clock)
    {
        if (customerId <= 0)
            return;

        var customer = await customers.GetAsync(customerId);
        if (customer is null)
            return;

        using var options = new StatementOptionsForm(clock);
        if (options.ShowDialog(owner) != DialogResult.OK)
            return;

        var safeCode = string.Join(
            "_",
            customer.CustomerCode.Split(
                Path.GetInvalidFileNameChars(),
                StringSplitOptions.RemoveEmptyEntries));

        var suggestedName =
            $"BinTracker_Statement_{safeCode}_" +
            $"{options.FromDate:yyyyMMdd}-{options.ToDate:yyyyMMdd}.pdf";

        try
        {
            string outputPath;

            if (options.OpenAfterGenerate)
            {
                var statementFolder = Path.Combine(
                    Path.GetTempPath(),
                    "BinTracker",
                    "Statements");

                Directory.CreateDirectory(statementFolder);
                outputPath = Path.Combine(statementFolder, suggestedName);
            }
            else
            {
                using var dialog = new SaveFileDialog
                {
                    Title = "Save Customer Statement",
                    Filter = "PDF document (*.pdf)|*.pdf",
                    FileName = suggestedName,
                    AddExtension = true,
                    DefaultExt = "pdf"
                };

                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;

                outputPath = dialog.FileName;
            }

            var pdf = await statementReports.BuildPdfAsync(
                customerId,
                options.FromDate,
                options.ToDate);
            await File.WriteAllBytesAsync(outputPath, pdf);

            if (options.OpenAfterGenerate)
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = outputPath,
                        UseShellExecute = true
                    });
            }
            else
            {
                MessageBox.Show(
                    owner,
                    $"Statement created successfully.\n\n{outputPath}",
                    "Customer Statement",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                owner,
                ex.Message,
                "Customer Statement",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
