using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace SpotifyOverlay.OverlayWindow;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"), e.ExceptionObject.ToString());
            MessageBox.Show(e.ExceptionObject.ToString(), "Fatal Crash");
        };

        this.DispatcherUnhandledException += (s, e) => 
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), e.Exception.ToString());
            e.Handled = true;
            MessageBox.Show(e.Exception.Message, "Overlay Crash");
        };
    }
}

