using System;
using System.Text;
using System.Windows.Forms;

namespace BGITranslator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                // CRITICAL: Register all code page encodings (Shift-JIS, CP932, etc.)
                // EthornellEditor.dll's BGIEncoding class needs Shift-JIS.
                // .NET 5+ does not include non-Unicode encodings by default.
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error saat startup:\n\n" + ex.ToString(),
                    "BGI Translator - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
