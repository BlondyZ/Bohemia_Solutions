using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bohemia_Solutions.Models;

namespace Bohemia_Solutions.Services
{
    public static class ChangeLogStorage
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // Umístění vedle ostatních JSON configů (přizpůsob tvému Config rootu)
        public static string GetPath()
        {
            // Pokud máš centrální ConfigDir, nahraď:
            var appDir = AppContext.BaseDirectory;
            return Path.Combine(appDir, "changelog.json");
        }

        public static ChangeLog LoadOrCreate()
        {
            var path = GetPath();
            if (!File.Exists(path))
            {
                var seed = Seed();
                Save(seed);
                return seed;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ChangeLog>(json, JsonOpts) ?? new ChangeLog();
        }

        public static void Save(ChangeLog log)
        {
            var path = GetPath();
            var json = JsonSerializer.Serialize(log, JsonOpts);
            File.WriteAllText(path, json);
        }

        // Volitelné – první seed pro pěkné UI hned po nasazení
        private static ChangeLog Seed()
        {
            return new ChangeLog
            {
                Versions =
                {
                    new ChangeLogVersion
                    {
                        Version = "3.0.1",
                        Date = DateTime.Today,
                        Items =
                        {
                            new ChangeItem{ Type = ChangeType.FIXED, Text = "Automatically retry download after decoding/partial buffer errors."},
                            new ChangeItem{ Type = ChangeType.CHANGED, Text = "Update bds-lib to v0.46.2."}
                        }
                    },
                    new ChangeLogVersion
                    {
                        Version = "3.0.0",
                        Date = DateTime.Today.AddDays(-7),
                        Items =
                        {
                            new ChangeItem{ Type = ChangeType.ADDED, Text = "Build file list view."},
                            new ChangeItem{ Type = ChangeType.ADDED, Text = "Build difference view."},
                            new ChangeItem{ Type = ChangeType.ADDED, Text = "BDS administration UI."},
                            new ChangeItem{ Type = ChangeType.CHANGED, Text = "Fragment download protocol to HTTP."},
                            new ChangeItem{ Type = ChangeType.CHANGED, Text = "Improved logging."},
                            new ChangeItem{ Type = ChangeType.REMOVED, Text = "Experimental config options."},
                            new ChangeItem{ Type = ChangeType.REMOVED, Text = "Network traffic indicator in status bar."},
                            new ChangeItem{ Type = ChangeType.FIXED, Text = "Download not working on Linux."},
                            new ChangeItem{ Type = ChangeType.CHANGED, Text = "Update bds-lib to v0.45.0."}
                        }
                    }
                }
            };
        }
    }
}
