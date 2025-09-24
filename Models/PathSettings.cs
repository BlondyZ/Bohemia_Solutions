using System;
using System.IO;
using System.Text.Json;

namespace Bohemia_Solutions.Models
{
    public class PathSettings
    {
        public string WorkshopRoot { get; set; } = DefaultPaths.WorkshopRoot;
        public string LocalWorkshopRoot { get; set; } = DefaultPaths.LocalWorkshopRoot;
        public string DayZStableDir { get; set; } = DefaultPaths.DayZStableDir;
        public string DayZExperimentalDir { get; set; } = DefaultPaths.DayZExperimentalDir;
        public string DayZServerStableRoot { get; set; } = DefaultPaths.DayZServerStableRoot;
        public string DayZServerExpRoot { get; set; } = DefaultPaths.DayZServerExpRoot;
        public string DayZInternalDir { get; set; } = DefaultPaths.DayZInternalDir;
        public string DayZServerInternalRoot { get; set; } = DefaultPaths.DayZServerInternalRoot;

        public string BdsRoot { get; set; } = @"C:\BDS";
        public string WorkDriveRoot { get; set; } = @"P:\";
        public string MikeroExtractPbo { get; set; } = @"C:\Program Files (x86)\Mikero\DePboTools\bin\ExtractPbo.exe";
     
        // Uživ. editor (např. VS Code / Rider)
        public string EditorExePath { get; set; } = "";

        // Až 10 zkratek(EXE + volitelně argumenty/working dir)
        public List<ShortcutItem> Shortcuts { get; set; } = new();

    }

    public class ShortcutItem
    {
        public string ExePath { get; set; } = "";     // cesta k EXE
        public string Arguments { get; set; } = "";   // volitelné (zatím nepoužito)
        public string WorkingDir { get; set; } = "";  // volitelné
    }


    public static class DefaultPaths
    {
        public static readonly string WorkshopRoot =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZ\!Workshop";
        public static readonly string LocalWorkshopRoot =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZ\!LocalWorkshop";
        public static readonly string DayZStableDir =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZ";
        public static readonly string DayZExperimentalDir =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZ Exp";
        public static readonly string DayZServerStableRoot =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZServer";
        public static readonly string DayZServerExpRoot =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZ Server Exp";
        public static readonly string DayZInternalDir =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZ Internal";
        public static readonly string DayZServerInternalRoot =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZ Server Internal";

    }

    public static class PathSettingsService
    {
        private static readonly object _lock = new object();
        private static PathSettings? _current;
        private static string? _configFilePath;



        // Preferuj vedle EXE; pokud není zapisovatelné, spadni do LocalAppData
        public static string ConfigFilePath
        {
            get
            {
                if (_configFilePath != null) return _configFilePath;

                var exeDir = AppContext.BaseDirectory;
                var exePath = Path.Combine(exeDir, "paths.json");

                if (IsDirectoryWritable(exeDir))
                {
                    _configFilePath = exePath;
                }
                else
                {
                    var appDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "BohemiaSolutions");
                    Directory.CreateDirectory(appDir);
                    _configFilePath = Path.Combine(appDir, "paths.json");
                }
                return _configFilePath!;
            }
        }

        public static PathSettings Current
        {
            get
            {
                lock (_lock)
                {
                    _current ??= LoadOrDefaults();
                    return _current;
                }
            }
        }

        public static void Save(PathSettings settings)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
                _current = settings;
            }
        }

        public static void ReloadFromDisk()
        {
            lock (_lock)
            {
                _current = LoadOrDefaults(); // znovu načti z paths.json (nebo defaulty)
            }
        }

        public static bool IsComplete(PathSettings s) =>
            !string.IsNullOrWhiteSpace(s.WorkshopRoot)
            && !string.IsNullOrWhiteSpace(s.LocalWorkshopRoot)
            && !string.IsNullOrWhiteSpace(s.DayZStableDir)
            && !string.IsNullOrWhiteSpace(s.DayZExperimentalDir)
            && !string.IsNullOrWhiteSpace(s.DayZServerStableRoot)
            && !string.IsNullOrWhiteSpace(s.DayZServerExpRoot)
            && !string.IsNullOrWhiteSpace(s.DayZInternalDir)
            && !string.IsNullOrWhiteSpace(s.DayZServerInternalRoot);

        public static bool AllExisting(PathSettings s) =>
            Directory.Exists(s.WorkshopRoot)
            && Directory.Exists(s.LocalWorkshopRoot)
            && Directory.Exists(s.DayZStableDir)
            && Directory.Exists(s.DayZExperimentalDir)
            && Directory.Exists(s.DayZServerStableRoot)
            && Directory.Exists(s.DayZServerExpRoot)
            && Directory.Exists(s.DayZInternalDir)
            && Directory.Exists(s.DayZServerInternalRoot);

        private static PathSettings LoadOrDefaults()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var loaded = JsonSerializer.Deserialize<PathSettings>(json);
                    if (loaded != null) return loaded;
                }
            }
            catch { /* fallback na defaulty */ }

            // Nevracíme nic na disk – jen defaulty v paměti; uloží se až po "Uložit"
            return new PathSettings();
        }

        private static bool IsDirectoryWritable(string dir)
        {
            try
            {
                var test = Path.Combine(dir, ".write_test_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(test, "x");
                File.Delete(test);
                return true;
            }
            catch { return false; }
        }
    }
}
