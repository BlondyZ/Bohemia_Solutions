
using Bohemia_Solutions.Models;  // ⬅ přidej na začátek souboru
using System.IO;
namespace Bohemia_Solutions
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Directory.SetCurrentDirectory(AppContext.BaseDirectory); } catch { /* ignore */ }
            // ⬇⬇⬇ Guard: bez nastavených cest nespouštěj aplikaci
            if (!PathsSetupForm.EnsureConfiguredAndShowIfNeeded())
            {
                // Uživatel zrušil nebo nastavení neúplné → konec
                return;
            }
            Application.Run(new Form1());
        }
    }
}