using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace Bohemia_Solutions.Services
{
    internal static class SimpleUpdater
    {
        public static async Task CheckAndOfferAsync(IWin32Window owner)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var json = await http.GetStringAsync(UpdateConfig.ManifestUrl);
                var jo = JObject.Parse(json);

                var remoteVer = (string?)jo["version"] ?? "";
                var zipUrl = (string?)jo["url"] ?? "";
                var sha256 = ((string?)jo["sha256"] ?? "").Trim().ToLowerInvariant();
                var notesUrl = (string?)jo["releaseNotesUrl"] ?? "";

               
                if (string.IsNullOrWhiteSpace(remoteVer) || string.IsNullOrWhiteSpace(zipUrl))
                    return;

                if (!IsNewer(remoteVer, Application.ProductVersion))
                    return;

                var currentPretty = PrettyVersion(Application.ProductVersion);
                var msg = $"New version {remoteVer} is available.\n\n" +
                          $"Current version: {currentPretty}\n\n" +
                          $"Update now?";
                var res = MessageBox.Show(owner, msg, "Update available",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (res != DialogResult.Yes) return;

                await ApplyUpdateAsync(owner, zipUrl, sha256);
            }
            catch
            {
                // ticho – žádné otravování při chybě sítě apod.
            }
        }

        private static async Task ApplyUpdateAsync(IWin32Window owner, string zipUrl, string sha256Hex)
        {
            string tmpRoot = Path.Combine(Path.GetTempPath(), "BohemiaSolutions", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmpRoot);

            string zipPath = Path.Combine(tmpRoot, "update.zip");
            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            using (var s = await http.GetStreamAsync(zipUrl))
            using (var f = File.Create(zipPath))
                await s.CopyToAsync(f);

            if (!string.IsNullOrWhiteSpace(sha256Hex))
                VerifySha256(zipPath, sha256Hex);

            string extractDir = Path.Combine(tmpRoot, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

            // najdi novou EXE (název může být shodný s existujícím)
            string currentExe = Application.ExecutablePath;
            string installDir = AppContext.BaseDirectory.TrimEnd('\\');
            string newExe = FindExe(extractDir) ?? currentExe;

            // vytvoř update skript (BAT) do TEMP
            string batPath = Path.Combine(tmpRoot, "run_update.bat");
            File.WriteAllText(batPath, BuildUpdateBat(), Encoding.ASCII);

            // spusť BAT a předej parametry
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{batPath}\" \"{extractDir}\" \"{installDir}\" \"{currentExe}\" {Environment.ProcessId}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = tmpRoot
            };
            Process.Start(psi);


            // zavři aplikaci, aby mohl skript přepsat soubory
            Application.Exit();
        }

        private static void VerifySha256(string file, string expectedHex)
        {
            using var sha = SHA256.Create();
            using var f = File.OpenRead(file);
            var hash = sha.ComputeHash(f);
            var got = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            if (!string.Equals(got, expectedHex, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Update package checksum mismatch.\nExpected: {expectedHex}\nGot: {got}");
        }

        private static string? FindExe(string root)
        {
            var exes = Directory.GetFiles(root, "*.exe", SearchOption.AllDirectories);
            return exes.Length > 0 ? exes[0] : null;
        }

        private static bool IsNewer(string remote, string local)
        {
            var r = ParseCoreVersion(remote);
            var l = ParseCoreVersion(local);
            return r > l;
        }


        private static Version ParseCoreVersion(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return new Version(0, 0, 0, 0);

            // OEM: odstraň prefix 'v' a vše po '-' nebo '+'
            var s = v.Trim().TrimStart('v', 'V');
            int cut = s.IndexOfAny(new[] { '-', '+' });
            if (cut >= 0) s = s.Substring(0, cut);

            // Parse, případně fallback na čísla a tečky
            if (!Version.TryParse(s, out var ver))
            {
                var core = new string(Array.FindAll(s.ToCharArray(), c => char.IsDigit(c) || c == '.'));
                Version.TryParse(core, out ver);
            }
            return ver ?? new Version(0, 0, 0, 0);
        }

        private static string PrettyVersion(string v)
        {
            var ver = ParseCoreVersion(v);

            // Rozlož verzi na části
            var parts = ver.ToString().Split('.');

            // Zajisti minimálně 3 části (Major.Minor.Build)
            if (parts.Length < 3)
            {
                var list = new List<string>(parts);
                while (list.Count < 3) list.Add("0");
                parts = list.ToArray();
            }

            // Zkracuj jenom trailing nuly NAD rámec 3 částí (tj. čtvrtá a další)
            int n = parts.Length;
            while (n > 3 && parts[n - 1] == "0") n--;

            return string.Join(".", parts, 0, n);
        }

        private static string BuildUpdateBat()
        {
            //  %1 = extracted, %2 = installDir, %3 = appExe, %4 = pid
            return
        @"@echo off
setlocal
set SRC=%~1
set DST=%~2
set APP=%~3
set PID=%4

:: čekej, než se appka ukončí
:wait
tasklist /FI ""PID eq %PID%"" | find ""%PID%"" >nul
if %errorlevel%==0 (timeout /t 1 >nul & goto wait)

:: 1) vše kromě konfigů
robocopy ""%SRC%"" ""%DST%"" /E /R:3 /W:1 /NFL /NDL /NJH /NJS ^
  /XF configs.json config.json sp_configs.json filters_global.json paths.json

:: 2) přepiš changelog z releasu
if exist ""%SRC%\changelog.json"" copy /Y ""%SRC%\changelog.json"" ""%DST%\changelog.json"" >nul

:: spusť novou verzi
start """" ""%APP%""

:: úklid
rmdir /s /q ""%SRC%""
del ""%~f0"" >nul 2>&1
endlocal
";
        }

    }
}
