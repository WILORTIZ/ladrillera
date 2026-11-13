using System.IO;
using System.Windows;

namespace SistemaLadrillera.Wpf;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            LogLocalError(args.Exception);
            MessageBox.Show(
                $"Ocurrió una validación o error en la operación:\n\n{args.Exception.Message}",
                "Sistema Ladrillera - Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };
    }

    private static void LogLocalError(Exception ex)
    {
        try
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Ladrillera",
                "Logs");
            Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, $"errores_{DateTime.Now:yyyyMM}.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n--------------------------------------------------\r\n");
        }
        catch
        {
            // No bloquear la interfaz si falla el log
        }
    }
}
