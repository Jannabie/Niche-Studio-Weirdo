using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System;
using System.Windows.Media;

namespace NicheStudioWeirdo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        LoadBundledFont();
    }

    private static void LoadBundledFont()
    {
        try
        {
            // Look for SFMonoMedium.otf in the Load/ subfolder next to the exe
            string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Load", "SFMonoMedium.otf");
            if (!File.Exists(fontPath)) return;

            // Register with WPF's font cache so styles can reference "SF Mono" by name
            string fontFolder = Path.GetDirectoryName(fontPath)!;
            Fonts.GetFontFamilies(new Uri(fontFolder + "/", UriKind.Absolute));
        }
        catch
        {
            // Non-fatal — UI will fall back to system fonts
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt"), "Dispatcher Crash:\n" + e.Exception.ToString());
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt"), "Domain Crash:\n" + e.ExceptionObject.ToString());
    }
}

