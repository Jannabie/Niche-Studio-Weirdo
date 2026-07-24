using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class LasengleView : UserControl
    {
        private const string GithubUrl = "https://github.com/Jannabie/Niche-Studio-Weirdo/tree/main/MBTL%20Hook";

        public LasengleView()
        {
            InitializeComponent();
        }

        private void Log(string msg)
        {
            if (Application.Current.MainWindow is MainWindow mw)
                mw.LogToConsole(msg);
        }

        private static void Msg(string text, string title = "Lasengle")
            => MessageBox.Show(text, title, MessageBoxButton.OK,
                               title == "Error" ? MessageBoxImage.Error : MessageBoxImage.Information);

        private void OpenGithub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(GithubUrl) { UseShellExecute = true });
                Log("Opened GitHub: MBTL Hook folder.");
            }
            catch (Exception ex)
            {
                Msg($"Cannot open browser:\n{ex.Message}", "Error");
            }
        }
    }
}
