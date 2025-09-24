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
using System.Security.Principal;

namespace Bohemia_Solutions.Services
{
    internal static class SimpleUpdater
    {

        private static async Task DownloadWithProgressAsync(string url, string dstFile, IProgress<(int, string)> progress)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            var total = resp.Content.Headers.ContentLength ?? -1L;
            await using var src = await resp.Content.ReadAsStreamAsync();
            await using var dst = File.Create(dstFile);

            var buffer = new byte[81920];
            long readTotal = 0;
            int read;
            while ((read = await src.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await dst.WriteAsync(buffer, 0, read);
                readTotal += read;
                if (total > 0)
                {
                    int p = (int)(readTotal * 60 / total); // 0–60% pro download
                    progress.Report((p, $"Downloading… {p}%"));
                }
            }
            progress.Report((60, "Download complete"));
        }

        public static async Task CheckAndOfferAsync(
     IWin32Window owner,
     Action<string>? onBegin = null,   // ← zavoláme těsně před downloadem
     Action? onEnd = null              // ← zavoláme při chybě / zrušení; při úspěchu se appka ukončí
 )
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
                // DEBUG: zjisti, zda app běží jako admin
                var isAdmin = IsRunAsAdmin();
              /*  MessageBox.Show(owner,
    $"App is running as: {(isAdmin ? "Administrator" : "Standard user")}\n" +
    $"Exe: {Application.ExecutablePath}\n" +
    $"PID: {Environment.ProcessId}",
    "Runtime elevation",
    MessageBoxButtons.OK,
    MessageBoxIcon.Information);*/
                var currentPretty = PrettyVersion(Application.ProductVersion);
                var msg = $"New version {remoteVer} is available.\n\n" +
                          $"Current version: {currentPretty}\n\n" +
                          $"Update now?";

                var res = MessageBox.Show(owner, msg, "Update available",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (res != DialogResult.Yes) return;

                // ←—— TADY zobrazíme overlay
                onBegin?.Invoke("Downloading and installing new version, please wait...");

                await ApplyUpdateAsync(owner, zipUrl, sha256);
                // při úspěšném update dojde v ApplyUpdateAsync k Application.Exit();
            }
            catch
            {
                // schválně ticho – nechceme rušit uživatele chybou sítě apod.
            }
            finally
            {
                // pokud jsme overlay ukázali a UPDATE nakonec neproběhl (chyba / zrušení),
                // tak ho schováme (při úspěchu se appka ukončí a tohle se už neprojeví).
                onEnd?.Invoke();
            }
        }

        private static bool IsRunAsAdmin()
        {
            using var id = WindowsIdentity.GetCurrent();
            var pr = new WindowsPrincipal(id);
            return pr.IsInRole(WindowsBuiltInRole.Administrator);
        }


        private static async Task ExtractZipWithProgressAsync(string zipPath, string extractDir, IProgress<(int, string)> progress)
        {
            Directory.CreateDirectory(extractDir);
            using var z = System.IO.Compression.ZipFile.OpenRead(zipPath);
            int total = z.Entries.Count;
            int i = 0;
            foreach (var e in z.Entries)
            {
                string fullPath = Path.Combine(extractDir, e.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                if (e.FullName.EndsWith("/")) { /* folder */ }
                else
                {
                    using var zs = e.Open();
                    using var fs = File.Create(fullPath);
                    await zs.CopyToAsync(fs);
                }
                i++;
                int p = 60 + (int)(25.0 * i / Math.Max(1, total)); // 60–85%
                progress.Report((p, $"Extracting… {p}%"));
            }
            progress.Report((85, "Extraction complete"));
        }


        private static async Task ApplyUpdateAsync(IWin32Window owner, string zipUrl, string sha256Hex)
        {
            string tmpRoot = Path.Combine(Path.GetTempPath(), "BohemiaSolutions", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmpRoot);

            // stáhni ZIP
            string zipPath = Path.Combine(tmpRoot, "update.zip");
            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            using (var s = await http.GetStreamAsync(zipUrl))
            using (var f = File.Create(zipPath))
                await s.CopyToAsync(f);

            // ověř checksum (pokud je k dispozici)
            if (!string.IsNullOrWhiteSpace(sha256Hex))
                VerifySha256(zipPath, sha256Hex);

            // rozbal
            string extractDir = Path.Combine(tmpRoot, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

            // připrav BAT
            string currentExe = Application.ExecutablePath;
            string installDir = AppContext.BaseDirectory.TrimEnd('\\');

            string batPath = Path.Combine(tmpRoot, "run_update.bat");
            File.WriteAllText(batPath, BuildUpdateBat(), Encoding.ASCII);

            // spusť BAT (zkopíruje soubory a nastartuje novou verzi)
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{batPath}\" \"{extractDir}\" \"{installDir}\" \"{currentExe}\" {Environment.ProcessId}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = tmpRoot
            };
            Process.Start(psi);

            // ukonči běžící aplikaci
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
  /XF configs.json config.json sp_configs.json filters_global.json paths.json settings.ini changelog_admin.json

:: 2) přepiš changelog z releasu
if exist ""%SRC%\changelog.json"" copy /Y ""%SRC%\changelog.json"" ""%DST%\changelog.json"" >nul

:: spusť novou verzi jako admin s korektním WorkingDirectory
pushd ""%DST%""
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  ""Start-Process -FilePath '%APP%' -WorkingDirectory '%DST%' -Verb RunAs""
popd

:: úklid
rmdir /s /q ""%SRC%""
del ""%~f0"" >nul 2>&1
endlocal
";
        }

    }
}
