// ConfigForm.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing; // kvůli Color
using System.IO;
using System.IO.Compression; // ZipFile, ZipArchive
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks; // ujisti se, že máš using
using System.Windows.Forms;
using WindowsFormsApp1.Models;
using Timer = System.Windows.Forms.Timer;
using Bohemia_Solutions.Models;

namespace Bohemia_Solutions
{
    public partial class ConfigForm : Form
    {
        // Uchovává config pro editaci
        private DayZConfig? LoadedConfig;
        private bool isEdit = false;

        private readonly Timer symlinkStatusTimer = new Timer();

       

        // ---- NÁZVY SPECIÁLNÍCH POLOŽEK VERZE (Steam) ----
        private const string VersionItem_DayZInternalSteam = "DayZ Internal (Steam)";

        private readonly string _configsFilePath = "configs.json";

        // Datový model pro list
        private readonly BindingList<DayZConfig> _configs = new(); // vyžaduje using System.ComponentModel;
        private DayZConfig _current;    // právě editovaná položka
        private bool _dirty = false;    // změny neuložené v editoru

        // ConfigForm.cs (fields nahoře)
        private readonly List<string> _knownFilters = new();
        private CheckedListBox clbFilters;
        private TextBox txtNewFilter;
        private Button btnAddFilter;
        private GroupBox grpFilters;

        // Deklarace (dolů mezi fieldy v ConfigForm.Designer.cs)
        private Button btnEditServerParams;
        private Button btnEditClientParams;


        public DayZConfig ResultConfig { get; private set; }

        // === Mikero a pracovní cesty ===
        private static string WorkDriveRoot => PathSettingsService.Current.WorkDriveRoot;
        private static string WorkTempModsRoot => Path.Combine(WorkDriveRoot, "TempMods");


        // Sekundový UI timer + stav
        private readonly Timer progressUiTimer = new Timer();
        private Stopwatch _extractStopwatch;
        private ProgressDialog _progressDlg;          // aktivní progress okno
        private string _lastStatus = "In Progress…";      // poslední „holý“ status bez času (timer dopisuje čas)


        // ---- NÁZVY SPECIÁLNÍCH POLOŽEK VERZE (Steam) ----
        private const string VersionItem_DayZSteam = "DayZ (Steam)";
        private const string VersionItem_DayZExpSteam = "DayZ Experimental (Steam)";

        private static bool IsSteamVersion(string versionName) =>
            string.Equals(versionName, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(versionName, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(versionName, VersionItem_DayZInternalSteam, StringComparison.OrdinalIgnoreCase);

        private static string? GetSteamRoot(string versionName)
        {
            if (string.Equals(versionName, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZStableDir;
            if (string.Equals(versionName, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZExperimentalDir;
            if (string.Equals(versionName, VersionItem_DayZInternalSteam, StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZInternalDir;
            return null;
        }

        private static void UncheckAll(CheckedListBox clb)
        {
            for (int i = 0; i < clb.Items.Count; i++)
                clb.SetItemChecked(i, false);
        }

        private void LoadFlagsFromConfig()
        {
            // SERVER
            UncheckAll(clbServerFlags); // začneme čistě
            var srvTokens = ParseFlags(LoadedConfig?.ServerParameters?.AdditionalParams);
            EnsureItemsAndCheck(clbServerFlags, srvTokens); // chybějící položky přidá, všechny z configu zaškrtne

            // CLIENT
            UncheckAll(clbClientFlags);
            var cliTokens = ParseFlags(LoadedConfig?.ClientParameters?.Arguments);
            EnsureItemsAndCheck(clbClientFlags, cliTokens);
        }



        // pro položky v comboboxu EXE
        private sealed class ExeChoice
        {
            public string Label { get; set; }  // text v comboboxu
            public string Path { get; set; }  // plná cesta k EXE
            public override string ToString() => Label;
        }


        // Reprezentace jedné položky parametru
        private sealed class ParamItem
        {
            public string Value { get; set; } = "";
            public bool Enabled { get; set; }
        }

        private void btnEditServerParams_Click(object? sender, EventArgs e)
        {
            OpenParamsEditor(clbServerFlags, "Edit Server Parameters");
        }

        private void btnEditClientParams_Click(object? sender, EventArgs e)
        {
            OpenParamsEditor(clbClientFlags, "Edit Client Parameters");
        }

        private void OpenParamsEditor(CheckedListBox clb, string title)
        {
            // 1) Načti stávající parametry z CLB
            var rows = clb.Items.Cast<object>()
                .Select((it, i) => new ParamsEditorForm.Row
                {
                    Parameter = it?.ToString() ?? string.Empty,
                    Enabled = clb.GetItemChecked(i)
                })
                .ToList();

            // 2) Otevři dialog se silně typovanými řádky
            using (var dlg = new ParamsEditorForm(rows, title))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // 3) Přemapuj zpět na vnitřní model ParamItem
                    var converted = (dlg.Result ?? new List<ParamsEditorForm.Row>())
                        .Select(r => new ParamItem { Value = r.Parameter, Enabled = r.Enabled })
                        .ToList();

                    ApplyParamsToCheckedListBox(clb, converted);
                }
            }
        }


        private static void ApplyParamsToCheckedListBox(CheckedListBox clb, IList<ParamItem> data)
        {
            clb.BeginUpdate();
            try
            {
                clb.Items.Clear();
                foreach (var p in data)
                {
                    var val = (p.Value ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(val)) continue;

                    int idx = clb.Items.Add(val);
                    clb.SetItemChecked(idx, p.Enabled);
                }
            }
            finally
            {
                clb.EndUpdate();
            }
        }

        private List<string> CollectSelectedFilters()
        {
            var tags = clbFilters.CheckedItems.Cast<string>().ToList();
            var pendingNew = (txtNewFilter.Text ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(pendingNew)) tags.Add(pendingNew);

            return tags
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => Regex.Replace(s.Trim(), @"\s{2,}", " "))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void PopulateExeNameFromDayZInstalls()
        {
            cmbExeName.Items.Clear();

            void addGroup(string groupTitle, string root)
            {
                if (!Directory.Exists(root)) return;

                // vybereme jen relevantní EXE (klidně si přidej další vzory, pokud potřebuješ)
                var exePaths = Directory.GetFiles(root, "*.exe", SearchOption.TopDirectoryOnly)
                                        .Where(p =>
                                            string.Equals(Path.GetFileName(p), "DayZ_x64.exe", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(Path.GetFileName(p), "DayZDiag_x64.exe", StringComparison.OrdinalIgnoreCase))
                                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                                        .ToList();

                if (exePaths.Count == 0) return;

                // „divider“ řádek (viz i ochrana v SelectedIndexChanged níže)
                cmbExeName.Items.Add($"──────── {groupTitle} ────────");

                foreach (var p in exePaths)
                {
                    cmbExeName.Items.Add(new ExeChoice
                    {
                        Label = $"[{groupTitle}] {Path.GetFileName(p)}",
                        Path = p
                    });
                }
            }

            addGroup("DayZ", PathSettingsService.Current.DayZStableDir);
            addGroup("DayZ Experimental", PathSettingsService.Current.DayZExperimentalDir);
            addGroup("DayZ Internal", PathSettingsService.Current.DayZInternalDir);

            // vyber první reálnou položku (přeskočí divider)
            for (int i = 0; i < cmbExeName.Items.Count; i++)
            {
                if (cmbExeName.Items[i] is ExeChoice) { cmbExeName.SelectedIndex = i; break; }
            }
        }

        // =====================
        // Helpers (ConfigForm.cs)
        // =====================

        // Sestaví bezpečný segment pro název složky
        private static string SafeSeg(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var cleaned = System.Text.RegularExpressions.Regex.Replace(s, "[^A-Za-z0-9]+", "_");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "_{2,}", "_").Trim('_');
            return cleaned;
        }

        // POUZE PŘEDIKCE cest k .cfg a profilům pro MP – NIC na disk netvoří
        // - vychází z UI (cmbVersion, cmbType, cb_chooseMP) a existujících helperů
        // - doplní jen txtServerConfigFile a txtProfilesFolder
        private void UpdateProfilesPathFromSelection_MP(bool forceText = false)
        {
            if (isEdit) return; // při editaci nepřepisuj uživateli jeho hodnoty

            // musíme mít zvolené: verzi a typ (mise si bere BuildAutoConfigFileName z cb_chooseMP)
            if (cmbVersion.SelectedIndex < 0 || cmbType.SelectedIndex < 0)
                return;

            string? serverDir = GetServerDirForSelectedVersion();
            if (string.IsNullOrWhiteSpace(serverDir) || !Directory.Exists(serverDir))
                return;

            // vždy navrhni unikátní název .cfg
            string cfgFileName = BuildAutoConfigFileName();
            string cfgFullPath = MakeUniquePath(serverDir, cfgFileName);

            // když je prázdno, nebo to vynucuju → rovnou dosadím unikát
            if (forceText || string.IsNullOrWhiteSpace(txtServerConfigFile.Text))
            {
                txtServerConfigFile.Text = cfgFullPath;
            }
            else
            {
                // když už tam něco je a ten soubor existuje, navrhni unikát
                var cur = txtServerConfigFile.Text.Trim();
                if (!string.IsNullOrEmpty(cur) && File.Exists(cur))
                {
                    var unique = MakeUniquePath(serverDir, Path.GetFileName(cur));
                    txtServerConfigFile.Text = unique;
                }
            }

            // navrhni profily (stejné skládání názvu jako .cfg; MakeUniqueDirectory jen vrací TEXT)
            string profilesSuggestion = BuildAutoProfilesFolderPath(serverDir, cfgFileName);
            if (forceText || string.IsNullOrWhiteSpace(txtProfilesFolder.Text))
                txtProfilesFolder.Text = profilesSuggestion;
        }



        // Sestav obsah serverového .cfg jako TEXT (bez zápisu na disk)
        private string BuildServerCfgPreview()
        {
            // Sem vlož svou existující logiku generování .cfg – zde skeleton:
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("// Generated by Bohemia Solutions");
            sb.AppendLine("// This file is created on Save (OK) in ConfigForm.");
            // příklad (přizpůsob si svým políčkům):
            // sb.AppendLine($"hostname = \"{txtServerName.Text}\";");
            // sb.AppendLine($"maxPlayers = {numMaxPlayers.Value};");
            // sb.AppendLine($"verifySignatures = 2;");
            return sb.ToString();
        }


        private static string ResolveModFolder(string modName)
        {
            var local = Path.Combine(PathSettingsService.Current.LocalWorkshopRoot, modName);
            if (Directory.Exists(local)) return local;
            return Path.Combine(PathSettingsService.Current.WorkshopRoot, modName);
        }

        private static string ResolveModRoot(string modName)
        {
            return Directory.Exists(Path.Combine(PathSettingsService.Current.LocalWorkshopRoot, modName))
                ? PathSettingsService.Current.LocalWorkshopRoot
                :PathSettingsService.Current.WorkshopRoot;
        }

        public static bool IsLinkPresent(string dir, string linkName)
        {
            if (string.IsNullOrWhiteSpace(dir)) return false;
            var p = Path.Combine(dir, linkName);
            return Directory.Exists(p) || File.Exists(p);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        // ====== Politika řazení modů (Expansion cheatsheet) ======

        private static string NormalizeModNameForOrder(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            name = name.Trim();
            if (name.StartsWith("@")) name = name.Substring(1);
            name = name.Replace('_', '-').Replace(' ', '-');
            name = Regex.Replace(name, "-{2,}", "-");
            return name.ToLowerInvariant();
        }

        private static bool IsExpansionModNormalized(string normalized) =>
            normalized.StartsWith("dayz-expansion");

        private static int GetModSortPriority(string modName)
        {
            var n = NormalizeModNameForOrder(modName);

            // Absolutní začátek
            if (n == "cf" || n == "community-framework") return 0;
            if (n == "community-online-tools" || n == "cot") return 1;
            if (n == "dabs-framework" || n == "df") return 2;

            if (IsExpansionModNormalized(n))
            {
                // Základ Expansion dopředu
                if (n == "dayz-expansion-core" || n == "dayz-expansion") return 3;
                if (n == "dayz-expansion-licensed") return 4;
                if (n == "dayz-expansion-bundle") return 5;
                if (n == "dayz-expansion-map-assets") return 6;

                // Jemný pořádek uvnitř Expansion (není tvrdě nutný, ale pomáhá stabilitě)
                if (n.EndsWith("-basebuilding")) return 21;
                if (n.EndsWith("-ai")) return 22;
                if (n.EndsWith("-book")) return 23;
                if (n.EndsWith("-chat")) return 24;
                if (n.EndsWith("-groups")) return 25;
                if (n.EndsWith("-hardline")) return 26;
                if (n.EndsWith("-market")) return 27;
                if (n.EndsWith("-missions")) return 28;
                if (n.EndsWith("-name-tags") || n.EndsWith("-nametags")) return 29;
                if (n.EndsWith("-navigation")) return 30;
                if (n.EndsWith("-personalstorage")) return 31;
                if (n.EndsWith("-quests")) return 32;
                if (n.EndsWith("-spawn-selection") || n.EndsWith("-spawnselection")) return 33;
                if (n.EndsWith("-vehicles")) return 34;
                if (n.EndsWith("-weapons")) return 35;
                if (n.EndsWith("-animations")) return 36;

                return 49; // ostatní Expansion
            }

            // vše ostatní (když jsou ve hře rovnosti, Expansion má přednost)
            return 1000;
        }

        private static int CompareModsByPolicy(string a, string b)
        {
            int pa = GetModSortPriority(a);
            int pb = GetModSortPriority(b);
            int cmp = pa.CompareTo(pb);
            if (cmp != 0) return cmp;
            return StringComparer.OrdinalIgnoreCase.Compare(a, b);
        }

        // === Struktura závislostí (per mod) ===
        private sealed class ModDeps
        {
            public HashSet<string> ProvidedPatches { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> RequiredPatches { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        // Regex parser CfgPatches z config.cpp
        private static ModDeps ParseConfigCppForDeps(string configCppPath)
        {
            var deps = new ModDeps();
            var txt = File.ReadAllText(configCppPath);

            var rxClass = new Regex(@"class\s+CfgPatches\s*\{(?<body>[\s\S]*?)\}\s*;", RegexOptions.IgnoreCase);
            var m = rxClass.Match(txt);
            if (!m.Success) return deps;

            string body = m.Groups["body"].Value;

            var rxPatch = new Regex(@"class\s+(?<name>[A-Za-z0-9_]+)\s*\{(?<pb>[\s\S]*?)\};", RegexOptions.IgnoreCase);
            var rxReq = new Regex(@"requiredAddons\s*\[\s*\]\s*=\s*\{(?<items>[\s\S]*?)\};", RegexOptions.IgnoreCase);

            foreach (Match mp in rxPatch.Matches(body))
            {
                var patchName = mp.Groups["name"].Value.Trim();
                if (!string.IsNullOrWhiteSpace(patchName))
                    deps.ProvidedPatches.Add(patchName);

                var pb = mp.Groups["pb"].Value;
                var mr = rxReq.Match(pb);
                if (mr.Success)
                {
                    foreach (Match qi in Regex.Matches(mr.Groups["items"].Value, "\"([^\"]+)\""))
                    {
                        var item = qi.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(item))
                            deps.RequiredPatches.Add(item);
                    }
                }
            }
            return deps;
        }

        // (Ponecháno – nepoužíváš ji, ale srovnání nulových uzlů jsem upravila stejně podle politiky)
        private List<string> SortModsByRequiredPatches(
            List<string> selectedMods,
            Dictionary<string, ModDeps> map, // mod -> deps
            out List<string> missingDeps,
            out List<(string Mod, string DependsOn)> cycles)
        {
            missingDeps = new List<string>();
            cycles = new List<(string, string)>();

            var patchProviders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in map)
            {
                foreach (var p in kv.Value.ProvidedPatches)
                {
                    if (!patchProviders.TryGetValue(p, out var list))
                        patchProviders[p] = list = new List<string>();
                    if (!list.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                        list.Add(kv.Key);
                }
            }

            var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var indegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in selectedMods) { graph[m] = new HashSet<string>(StringComparer.OrdinalIgnoreCase); indegree[m] = 0; }

            foreach (var m in selectedMods)
            {
                foreach (var req in map[m].RequiredPatches)
                {
                    if (!patchProviders.TryGetValue(req, out var providers) || providers.Count == 0)
                    {
                        missingDeps.Add($"{m} → (patch) {req}");
                        continue;
                    }
                    foreach (var prov in providers)
                    {
                        if (!selectedMods.Contains(prov, StringComparer.OrdinalIgnoreCase)) continue;
                        if (string.Equals(prov, m, StringComparison.OrdinalIgnoreCase)) continue;
                        if (graph[m].Add(prov)) indegree[m]++;
                    }
                }
            }

            if (missingDeps.Count > 0) return selectedMods;

            var sorted = new List<string>();
            var indeg = new Dictionary<string, int>(indegree, StringComparer.OrdinalIgnoreCase);

            while (sorted.Count < selectedMods.Count)
            {
                var zero = indeg.Where(kv => kv.Value == 0 && !sorted.Contains(kv.Key))
                                .Select(kv => kv.Key)
                                .ToList();
                if (zero.Count == 0)
                {
                    var left = indeg.FirstOrDefault(kv => kv.Value > 0).Key;
                    var dependsOn = left != null ? (graph[left].FirstOrDefault() ?? "(unknown)") : "(unknown)";
                    cycles.Add((left ?? "(unknown)", dependsOn));
                    break;
                }

                zero.Sort(CompareModsByPolicy);

                foreach (var z in zero)
                {
                    if (sorted.Contains(z)) continue;
                    sorted.Add(z);
                    foreach (var n in graph.Where(kv => kv.Value.Contains(z)).Select(kv => kv.Key).ToList())
                        indeg[n] = Math.Max(0, indeg[n] - 1);
                }
            }

            foreach (var m in selectedMods)
                if (!sorted.Contains(m)) sorted.Add(m);

            return sorted;
        }

        private static IEnumerable<string> ParseFlags(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Enumerable.Empty<string>();

            // Rozděl podle whitespace, zachová "-key=value"
            return raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrEmpty(s));
        }

        private static int IndexOfItem(CheckedListBox clb, string value)
        {
            for (int i = 0; i < clb.Items.Count; i++)
            {
                var it = clb.Items[i]?.ToString() ?? string.Empty;
                if (string.Equals(it, value, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static void EnsureItemsAndCheck(CheckedListBox clb, IEnumerable<string> flags)
        {
            foreach (var flag in flags)
            {
                if (string.IsNullOrWhiteSpace(flag)) continue;

                int idx = IndexOfItem(clb, flag);
                if (idx < 0)
                    idx = clb.Items.Add(flag);

                clb.SetItemChecked(idx, true);
            }
        }

        // Konstruktor
        public ConfigForm(DayZConfig configToEdit = null, IEnumerable<string> knownFilters = null)
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.MinimumSize = new Size(1000, 700);

            panel1.Dock = DockStyle.Fill;
            panel1.AutoScroll = true;

            pnlServerConfig.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            LoadedConfig = configToEdit;

            if (knownFilters != null)
                _knownFilters.AddRange(knownFilters
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        private void BuildFiltersUI()
        {
            grpFilters = new GroupBox
            {
                Text = "Filters / Groups",
                AutoSize = false,
                Width = pnlServerConfig.Width,
                Height = 180,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            clbFilters = new CheckedListBox
            {
                CheckOnClick = true,
                IntegralHeight = false,
                Width = grpFilters.Width - 20,
                Height = 110,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            txtNewFilter = new TextBox
            {
                Width = grpFilters.Width - 120,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnAddFilter = new Button
            {
                Text = "Add",
                Width = 80,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAddFilter.Click += (_, __) =>
            {
                var s = (txtNewFilter.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(s)) return;
                AddFilterItemIfMissing(s);
                // nově přidaný rovnou zaškrtni
                int ix = clbFilters.Items.IndexOf(s);
                if (ix >= 0) clbFilters.SetItemChecked(ix, true);
                txtNewFilter.Clear();
                txtNewFilter.Focus();
            };

            // layout
            clbFilters.Location = new Point(10, 20);
            txtNewFilter.Location = new Point(10, clbFilters.Bottom + 8);
            btnAddFilter.Location = new Point(grpFilters.Width - btnAddFilter.Width - 10, clbFilters.Bottom + 6);

            grpFilters.Controls.Add(clbFilters);
            grpFilters.Controls.Add(txtNewFilter);
            grpFilters.Controls.Add(btnAddFilter);

            // umísti groupbox na konec panelu se server nastavením
            grpFilters.Location = new Point(10, pnlServerConfig.Bottom + 12);
            panel1.Controls.Add(grpFilters);

            // naplň známé filtry
            foreach (var f in _knownFilters)
                clbFilters.Items.Add(f);
        }

        private void AddFilterItemIfMissing(string tag)
        {
            // normalizuj „hezký“ zápis
            tag = Regex.Replace(tag.Trim(), @"\s{2,}", " ");
            if (clbFilters.Items.Contains(tag)) return;
            // vlož abecedně
            var all = clbFilters.Items.Cast<object>().Select(o => o.ToString()).ToList();
            all.Add(tag);
            all = all
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            clbFilters.Items.Clear();
            foreach (var f in all) clbFilters.Items.Add(f);
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            symlinkStatusTimer.Interval = 1000;
            symlinkStatusTimer.Tick += SymlinkStatusTimer_Tick;
            symlinkStatusTimer.Start();

            cmbType.Items.Clear();
            cmbType.Items.Add("Vanilla");
            cmbType.Items.Add("Modded");

            cmbVersion.Items.Clear();
            string bdsPath = PathSettingsService.Current.BdsRoot;
            if (Directory.Exists(bdsPath))
            {
                var versions = Directory.GetDirectories(bdsPath)
                    .Select(Path.GetFileName)
                    .ToArray();
                cmbVersion.Items.AddRange(versions);
                // přidej i Steam instalace jako "verze"
                cmbVersion.Items.Add(VersionItem_DayZSteam);
                cmbVersion.Items.Add(VersionItem_DayZExpSteam);
                cmbVersion.Items.Add(VersionItem_DayZInternalSteam);
            }

            // počáteční symlink check jen pro BDS verze
            string selectedVersion = cmbVersion.Items.Count > 0 ? cmbVersion.Items[0].ToString() : null;
            string serverDir = null, clientDir = null;
            if (!string.IsNullOrEmpty(selectedVersion) && !IsSteamVersion(selectedVersion))
            {
                serverDir = Path.Combine(PathSettingsService.Current.BdsRoot, selectedVersion, "Server");
                clientDir = Path.Combine(PathSettingsService.Current.BdsRoot, selectedVersion, "Client");
            }

            if (cmbType.SelectedItem?.ToString() == "Modded" && !IsSteamVersion(selectedVersion))
                CheckWorkshopSymlinks(serverDir, clientDir);
            else
                lblWorkshopStatus.Text = "";

            CheckWorkshopSymlinks(serverDir, clientDir);
            LoadWorkshopMods();
            InitEnvTypeComboIfNeeded();
            BuildFiltersUI();
            if (LoadedConfig != null)
            {
                isEdit = true;
                FillForm(LoadedConfig);
            }
            else
            {
                isEdit = false;
                numPort.Value = 2502;
                numCpuCount.Value = 2;
                txtServerName.Text = "";
                // Nevybírej nic – uživatel musí vyplnit
                cmbType.SelectedIndex = -1;
                cmbVersion.SelectedIndex = -1;

                // reaguj na změny základních polí
                txtName.TextChanged -= BaseInfoChanged;
                txtName.TextChanged += BaseInfoChanged;
            }

            ApplyEditModeUi();

            progressUiTimer.Interval = 1000;
            progressUiTimer.Tick -= ProgressUiTimer_Tick;
            progressUiTimer.Tick += ProgressUiTimer_Tick;

            UpdateOkButtonText();
            ApplyStep1Lock();   // nastav výchozí enable/disable
            PopulateChooseMp();
            UpdateOnlineFieldsAvailability(); // nastaví Enabled/Disabled podle vybrané mise
            LoadFlagsFromConfig();


        }

        private void InitEnvTypeComboIfNeeded()
        {
            if (cb_cfg_enviromenttype.Items.Count == 0)
            {
                cb_cfg_enviromenttype.DropDownStyle = ComboBoxStyle.DropDownList;

                // ⬇⬇⬇ PŘIDÁNO: prázdná volba
                cb_cfg_enviromenttype.Items.Add("[Empty]");

                cb_cfg_enviromenttype.Items.AddRange(new object[] { "Internal", "Stable", "Experimental" });
            }
        }

        private string GetCurrentSelectedMission()
        {
            // preferuj výběr z cb_chooseMP, jinak z cb_cfg_mpmission
            var m = cb_chooseMP.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(m)) return m;
            return cb_cfg_mpmission.SelectedItem as string;
        }

        private void UpdateOnlineFieldsAvailability()
        {
            var mission = GetCurrentSelectedMission();
            bool online = IsOnlineMission(mission);

            tb_cfg_shard_id.Enabled = online;
            cb_cfg_enviromenttype.Enabled = online;

            if (!online)
            {
                // Offline mise → nastav defaulty a smaž výběr env typu
                tb_cfg_shard_id.Text = "123abc";
                cb_cfg_enviromenttype.SelectedIndex = -1; // prázdná hodnota v UI
            }
            else
            {
                // Online mise → když nic není, doplň rozumné defaulty
                if (string.IsNullOrWhiteSpace(tb_cfg_shard_id.Text))
                    tb_cfg_shard_id.Text = "123abc";
                if (cb_cfg_enviromenttype.SelectedIndex < 0)
                    cb_cfg_enviromenttype.SelectedItem = "[Empty]"; // dříve "Stable"
            }
        }

        // volitelné: odstraní existující klíče z cfg při offline misi
        private void RemoveConfigKey(ref List<string> lines, string key)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var t = lines[i].TrimStart();
                if (t.StartsWith(key + " ", StringComparison.OrdinalIgnoreCase))
                    lines.RemoveAt(i);
            }
        }

        private void LoadWorkshopMods()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(PathSettingsService.Current.WorkshopRoot))
                foreach (var d in Directory.GetDirectories(PathSettingsService.Current.WorkshopRoot))
                {
                    var name = Path.GetFileName(d);
                    if (!name.StartsWith("!")) names.Add(name);
                }

            if (Directory.Exists(PathSettingsService.Current.LocalWorkshopRoot))
                foreach (var d in Directory.GetDirectories(PathSettingsService.Current.LocalWorkshopRoot))
                {
                    var name = Path.GetFileName(d);
                    if (!name.StartsWith("!")) names.Add(name);
                }

            clbWorkshopMods.Items.Clear();
            clbWorkshopMods.Items.AddRange(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray());

            if (names.Count == 0)
                MessageBox.Show("Workshop/LocalWorkshop folders were not found or are empty.");
        }

        // ConfigForm.cs (fields)
        private static readonly string[] DefaultClientFlags = new[]
        {
            "-window"
        };

        private void InitClientFlagsIfNeeded()
        {
            if (clbClientFlags.Items.Count == 0)
                clbClientFlags.Items.AddRange(DefaultClientFlags);
        }


        public void FillForm(DayZConfig config)
        {
            if (config == null) return;

            string configTypeTrimmed = config.Type?.Trim() ?? "";
            int typeIx = -1;
            for (int i = 0; i < cmbType.Items.Count; i++)
            {
                if (string.Equals(cmbType.Items[i].ToString().Trim(), configTypeTrimmed, StringComparison.OrdinalIgnoreCase))
                {
                    typeIx = i;
                    break;
                }
            }
            if (typeIx >= 0)
                cmbType.SelectedIndex = typeIx;
            else if (cmbType.Items.Count > 0)
                cmbType.SelectedIndex = 0;

            int verIx = cmbVersion.Items.IndexOf(config.VersionFolder);
            if (verIx >= 0) cmbVersion.SelectedIndex = verIx;
            else cmbVersion.SelectedIndex = -1;

            cmbVersion_SelectedIndexChanged(null, null);

            if (!string.IsNullOrWhiteSpace(config.ServerParameters?.ExeName))
            {
                for (int i = 0; i < cmbExeName.Items.Count; i++)
                {
                    if (cmbExeName.Items[i] is ExeChoice ec)
                    {
                        string target = config.ServerParameters.ExeName;
                        if (string.Equals(ec.Path, target, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(Path.GetFileName(ec.Path), target, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ec.Label, target, StringComparison.OrdinalIgnoreCase))
                        {
                            cmbExeName.SelectedIndex = i;
                            break;
                        }
                    }
                }
                if (cmbExeName.SelectedIndex < 0 && cmbExeName.Items.Count > 0)
                    cmbExeName.SelectedIndex = 0;
            }

            txtName.Text = config.Name ?? "";
            txtServerName.Text = config.ServerName ?? "";
            txtServerConfigFile.Text = config.ServerParameters?.ConfigFile ?? "";
            txtProfilesFolder.Text = config.ServerParameters?.ProfilesFolder ?? "";
            numPort.Value = config.ServerParameters?.Port > 0 ? config.ServerParameters.Port : 2502;
            numCpuCount.Value = config.ServerParameters?.CpuCount > 0 ? config.ServerParameters.CpuCount : 2;

            string[] flags = (config.ServerParameters?.AdditionalParams ?? "")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < clbServerFlags.Items.Count; i++)
            {
                clbServerFlags.SetItemChecked(i, flags.Contains(clbServerFlags.Items[i].ToString()));
            }

            // CLIENT FLAGS ← čte z ClientParameters.Arguments
            var cargs = LoadedConfig?.ClientParameters?.Arguments ?? "";
            var selected = new HashSet<string>(
                cargs.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase
            );

            // Defaultně, když nic není uloženo, zaškrtni výchozí sadu:
            bool hadAny = selected.Count > 0;

            for (int i = 0; i < clbClientFlags.Items.Count; i++)
            {
                var flag = clbClientFlags.Items[i].ToString();
                bool check = hadAny ? selected.Contains(flag)
                                    : DefaultClientFlags.Contains(flag, StringComparer.OrdinalIgnoreCase);
                clbClientFlags.SetItemChecked(i, check);
            }

            var mods = config.ServerParameters?.Mods ?? new List<string>();
            for (int i = 0; i < clbWorkshopMods.Items.Count; i++)
            {
                clbWorkshopMods.SetItemChecked(i, mods.Contains(clbWorkshopMods.Items[i].ToString()));
            }

            CopyModKeysToServer();
            UpdateOkButtonText();
            // === Filters pro tento config ===
            var cfgFilters = config.Filters ?? new List<string>();

            // přidej případné doposud neznámé tagy (aby je šlo zobrazit a zaškrtnout)
            foreach (var f in cfgFilters)
                AddFilterItemIfMissing(f);

            // zaškrtni
            for (int i = 0; i < clbFilters.Items.Count; i++)
            {
                var tag = clbFilters.Items[i].ToString();
                bool check = cfgFilters.Contains(tag, StringComparer.OrdinalIgnoreCase);
                clbFilters.SetItemChecked(i, check);
            }
        }

        private void cmbVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            workshopFixPrompted = false;

            string selectedVersion = cmbVersion.SelectedItem as string;
            cmbExeName.Items.Clear();

            if (string.IsNullOrEmpty(selectedVersion))
                return;

            // ponecháno: auto-copy diag/BE pro Steam/BDS verze (to je mimo profily/.cfg)
            EnsureClientExecutablesOnServer(); // IO jen pro exe, požadavek se týkal profilů a cfg  :contentReference[oaicite:1]{index=1}

            // --- Steam položky (DayZ / Exp / Internal) ---
            if (IsSteamVersion(selectedVersion))
            {
                var root = GetSteamRoot(selectedVersion);
                if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                {
                    cmbExeName.Items.Add($"──────── {selectedVersion} ────────");

                    var candidates = new[] { "DayZ_x64.exe", "DayZDiag_x64.exe" };
                    foreach (var name in candidates)
                    {
                        string p = Path.Combine(root, name);
                        if (File.Exists(p))
                            cmbExeName.Items.Add(new ExeChoice { Label = $"[{selectedVersion}] {name}", Path = p });
                    }
                }
            }
            else
            {
                // --- BDS verze: sbíráme EXE ze Server i Client ---
                string root = Path.Combine(PathSettingsService.Current.BdsRoot, selectedVersion);
                string serverDir = Path.Combine(root, "Server");
                string clientDir = Path.Combine(root, "Client");

                if (Directory.Exists(serverDir))
                {
                    foreach (var p in Directory.GetFiles(serverDir, "*.exe", SearchOption.TopDirectoryOnly)
                                               .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                    {
                        cmbExeName.Items.Add(new ExeChoice { Label = $"[Server] {Path.GetFileName(p)}", Path = p });
                    }
                }

                if (Directory.Exists(clientDir))
                {
                    var list = new[] { "DayZ_x64.exe", "DayZDiag_x64.exe" }
                               .Select(n => Path.Combine(clientDir, n))
                               .Where(File.Exists)
                               .ToList();

                    // (divider pro Client byl dříve volitelný – nechávám skrytý)
                }
            }

            // vyber první reálnou položku (přeskočí „divider“)
            for (int i = 0; i < cmbExeName.Items.Count; i++)
                if (cmbExeName.Items[i] is ExeChoice) { cmbExeName.SelectedIndex = i; break; }

            CopyModKeysToServer();
            ApplyStep1Lock();
            PopulateChooseMp();
            // CreateAutoConfigIfReady();  // ❌ pryč – nic nevytvářet
            PredictAutoFieldsIfReady();    // ✅ jen predikce (text)
            UpdateOnlineFieldsAvailability();
            LoadFlagsFromConfig();
        }

        // POUZE PŘEDIKCE cest .cfg a Profiles – nic na disk nezapisuje
        private void PredictAutoFieldsIfReady()
        {
            if (isEdit) return; // v režimu editace nic nevnucuj

            // musí být zvolena Verze + Typ (mise je volitelná; když není, návrh bude bez mise v názvu)
            if (cmbVersion.SelectedIndex < 0 || cmbType.SelectedIndex < 0)
                return;

            string serverDir = GetServerDirForSelectedVersion();
            if (string.IsNullOrEmpty(serverDir) || !Directory.Exists(serverDir))
                return;

            // unikát přes MakeUniquePath
            string fileName = BuildAutoConfigFileName();
            string cfgFull = MakeUniquePath(serverDir, fileName);

            if (string.IsNullOrWhiteSpace(txtServerConfigFile.Text))
            {
                txtServerConfigFile.Text = cfgFull;
            }
            else
            {
                var cur = txtServerConfigFile.Text.Trim();
                if (!string.IsNullOrEmpty(cur) && File.Exists(cur))
                {
                    var unique = MakeUniquePath(serverDir, Path.GetFileName(cur));
                    txtServerConfigFile.Text = unique;
                }
            }


            // navrhni profily dle stejného vzoru jako .cfg (bez vytváření složky)
            // BuildAutoProfilesFolderPath vrací unikátní kandidáta pomocí Directory.Exists → jen čtení
            string profilesSuggestion = BuildAutoProfilesFolderPath(serverDir, fileName);
            if (string.IsNullOrWhiteSpace(txtProfilesFolder.Text))
                txtProfilesFolder.Text = profilesSuggestion;
        }


        // Zapne/vypne akční tlačítka pro Init/Storage sekci podle režimu
        private void SetInitStorageButtonsEnabled(bool enabled)
        {
            btnBackupStorage.Enabled = enabled;
            btnWipeStorage.Enabled = enabled;
            btnBrowseStorage.Enabled = enabled;

            btnBackupInit.Enabled = enabled;
            btnBrowseInit.Enabled = enabled;
            btnOpenInitFile.Enabled = enabled;
        }


        /// <summary>
        /// Vytvoří základní server .cfg podle aktuálního UI, ale jen pokud ještě neexistuje.
        /// - Respektuje vybranou verzi/typ/misi (BuildAutoConfigFileName + MakeUniquePath)
        /// - Pokud textbox pro .cfg je prázdný, dopočítá název a doplní jej.
        /// - Nevytváří profilovou složku! (tu řeší ConfirmCreateFolderIfMissing_MP v btnOK).
        /// </summary>
        private void CreateCfgIfMissingFromUi()
        {
            // 1) Najdi/stanov cílovou cestu .cfg
            if (string.IsNullOrWhiteSpace(txtServerConfigFile.Text))
            {
                if (cmbVersion.SelectedIndex < 0 || cmbType.SelectedIndex < 0) return;

                var serverDir = GetServerDirForSelectedVersion();
                if (string.IsNullOrWhiteSpace(serverDir) || !Directory.Exists(serverDir)) return;

                var fileName = BuildAutoConfigFileName();                 // např. ServerDZ<ver><typ><mise>.cfg
                var fullPath = MakeUniquePath(serverDir, fileName);       // přidej _2, _3... pokud koliduje
                txtServerConfigFile.Text = fullPath;
            }

            var full = (txtServerConfigFile.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(full)) return;

            // 2) Když už existuje, nic nedělej (jen později UpdateServerConfigFile upraví klíče)
            if (File.Exists(full)) return;

            // 3) Ujisti se, že cílová složka existuje
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 4) Posbírej hodnoty z UI (převzato z tvé CreateAutoConfigIfReady kostry)
            string hostname = string.IsNullOrWhiteSpace(tb_cfg_hostname.Text) ? "DayZ Server" : tb_cfg_hostname.Text;
            string password = tb_cfg_password.Text ?? "";
            int maxPlayers = (int)nb_cfg_max_players.Value;
            int battleye = (int)nb_cfg_battleye.Value;
            string template = cb_chooseMP.SelectedItem?.ToString() ?? "dayzOffline.chernarusplus";

            // defaulty
            string passwordAdmin = "";
            string description = "";
            int enableWhitelist = 0;
            int verifySignatures = 2;
            int forceSameBuild = 1;
            int disableVoN = 0;
            int vonCodecQuality = 20;
            int disable3rdPerson = 0;
            int disableCrosshair = 0;
            int disablePersonalLight = 1;
            int lightingConfig = 0;
            string serverTime = "SystemTime";
            int serverTimeAcceleration = 12;
            int serverNightTimeAcceleration = 1;
            int serverTimePersistent = 0;
            int guaranteedUpdates = 1;
            int loginQueueConcurrentPlayers = 5;
            int loginQueueMaxPlayers = 500;
            int instanceId = 1;
            int storageAutoFix = 1;
            int enableCfgGameplayFile = 1;
         

            // online mise → env/shard
            bool isOnline = IsOnlineMission(template);
            string shardId = isOnline
                ? (string.IsNullOrWhiteSpace(tb_cfg_shard_id.Text) ? "123abc" : tb_cfg_shard_id.Text)
                : null;

            string envType = isOnline
                ? (cb_cfg_enviromenttype.SelectedItem?.ToString() ?? "Stable")
                : null;

            if (string.Equals(envType, "[Empty]", StringComparison.OrdinalIgnoreCase))
                envType = "";

            // 5) Postav obsah .cfg (tvoje kostra)
            var sb = new StringBuilder();
            sb.AppendLine($"hostname = \"{hostname}\";");
            sb.AppendLine($"password = \"{password}\";");
            sb.AppendLine($"passwordAdmin = \"{passwordAdmin}\";         // Password to become a server admin");
            sb.AppendLine();
            sb.AppendLine($"description = \"{description}\";\t\t\t// Description of the server. Gets displayed to users in client server browser.");
            sb.AppendLine();
            sb.AppendLine($"enableWhitelist = {enableWhitelist};        // Enable/disable whitelist (value 0-1)");
            sb.AppendLine();
            sb.AppendLine($"maxPlayers = {maxPlayers};");
            sb.AppendLine();
            sb.AppendLine($"verifySignatures = {verifySignatures};       // Verifies .pbos against .bisign files. (only 2 is supported)");
            sb.AppendLine($"forceSameBuild = {forceSameBuild};         // When enabled, allows only clients with same .exe revision (0-1)");
            sb.AppendLine();
            sb.AppendLine($"disableVoN = {disableVoN};             // Enable/disable voice over network (0-1)");
            sb.AppendLine($"vonCodecQuality = {vonCodecQuality};       // Voice over network codec quality (0-30)");
            sb.AppendLine();
            sb.AppendLine($"shardId = {shardId};"); 
            sb.AppendLine();
            if (isOnline)
            {
                sb.AppendLine($"enviromentType = \"{envType}\";     // Internal / Stable / Experimental (online economy DB)");
            }
            else
            {
                sb.AppendLine($"enviromentType = \"\";      // Empty");
            }
            sb.AppendLine($"disable3rdPerson={disable3rdPerson};         // Toggles the 3rd person view for players (0-1)");
            sb.AppendLine($"disableCrosshair={disableCrosshair};         // Toggles the cross-hair (0-1)");
            sb.AppendLine();
            sb.AppendLine($"disablePersonalLight = {disablePersonalLight};   // Disables personal light for all clients connected to server");
            sb.AppendLine($"lightingConfig = {lightingConfig};         // 0 for brighter night setup, 1 for darker night setup");
            sb.AppendLine();
            sb.AppendLine($"serverTime=\"{serverTime}\";    // Initial in-game time. \"SystemTime\" = local machine time.");
            sb.AppendLine($"serverTimeAcceleration={serverTimeAcceleration};  // Accelerated Time (0-24)");
            sb.AppendLine($"serverNightTimeAcceleration={serverNightTimeAcceleration};  // Night multiplier (0.1-64), multiplied by serverTimeAcceleration");
            sb.AppendLine($"serverTimePersistent={serverTimePersistent};     // Persistent Time (0-1)");
            sb.AppendLine();
            sb.AppendLine($"guaranteedUpdates={guaranteedUpdates};        // Communication protocol (use only 1)");
            sb.AppendLine();
            sb.AppendLine($"loginQueueConcurrentPlayers={loginQueueConcurrentPlayers};  // Players processed concurrently during login");
            sb.AppendLine($"loginQueueMaxPlayers={loginQueueMaxPlayers};       // Max players in login queue");
            sb.AppendLine();
            sb.AppendLine($"instanceId = {instanceId};             // DayZ server instance id");
            sb.AppendLine();
            sb.AppendLine($"enableCfgGameplayFile = {enableCfgGameplayFile};      //Enable/Disable cfggamplay.json ");
            sb.AppendLine();
            sb.AppendLine($"storageAutoFix = {storageAutoFix};         // Checks persistence files and replaces corrupted ones");
            sb.AppendLine($"battleye = {battleye};");
           
             
            sb.AppendLine();
            sb.AppendLine("class Missions");
            sb.AppendLine("{");
            sb.AppendLine("    class DayZ");
            sb.AppendLine("    {");
            sb.AppendLine($"        template=\"{template}\";");
            sb.AppendLine("        // Vanilla mission: dayzOffline.chernarusplus");
            sb.AppendLine("        // DLC mission: dayzOffline.enoch");
            sb.AppendLine("    };");
            sb.AppendLine("};");

            // 6) Zapiš nový .cfg
            File.WriteAllText(full, sb.ToString(), Encoding.UTF8);

            // 7) Předvyber vytvořený soubor do textboxu (už je tam), a doladíme UI
            txtServerConfigFile.Text = full;
            UpdateOnlineFieldsAvailability();
        }




        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            workshopFixPrompted = false;
            txtServerConfigFile.Text = "";
            txtProfilesFolder.Text = "";
            string selected = cmbType.SelectedItem?.ToString();
            if (selected == "Modded")
            {
                clbWorkshopMods.Enabled = true;
                clbWorkshopMods.BackColor = SystemColors.Window;
            }
            else
            {
                clbWorkshopMods.Enabled = false;
                clbWorkshopMods.BackColor = SystemColors.Control;
                for (int i = 0; i < clbWorkshopMods.Items.Count; i++)
                    clbWorkshopMods.SetItemChecked(i, false);
            }

            CopyModKeysToServer();
            UpdateOkButtonText();
            ApplyStep1Lock();

            // CreateAutoConfigIfReady();  // ❌ pryč
            PredictAutoFieldsIfReady();    // ✅ jen textové návrhy
            UpdateOnlineFieldsAvailability();
            LoadFlagsFromConfig();
        }

        // 1) Reakce na změnu základních polí (Name, případně i další)
        private void BaseInfoChanged(object sender, EventArgs e)
        {
            ApplyStep1Lock();
        }

        // 2) Zamknout/odemknout server sekci, dokud není vyplněno Name + Type + Version
        private void ApplyStep1Lock()
        {
            if (isEdit) { EnableServerSection(true); return; }

            bool ready =
                !string.IsNullOrWhiteSpace(txtName.Text) &&
                cmbType.SelectedIndex >= 0 &&
                cmbVersion.SelectedIndex >= 0;

            EnableServerSection(ready);
        }

        // 3) Opravdu povolit/zakázat prvky
        private void EnableServerSection(bool enabled)
        {
            pnlServerConfig.Enabled = enabled;
            txtServerName.Enabled = enabled;
            txtServerConfigFile.Enabled = enabled;
            btnBrowseServerConfig.Enabled = enabled;
            txtProfilesFolder.Enabled = enabled;
            btnBrowseProfiles.Enabled = enabled;

            bool modded = string.Equals(cmbType.SelectedItem?.ToString(), "Modded", StringComparison.OrdinalIgnoreCase);
            clbWorkshopMods.Enabled = enabled && modded;
            clbWorkshopMods.BackColor = clbWorkshopMods.Enabled ? SystemColors.Window : SystemColors.Control;
        }


        private static string EnsureWritableDirectory(string preferredPath, out bool usedFallback)
        {
            usedFallback = false;

            try
            {
                // Založí celý řetězec složek (nevadí, pokud už existuje)
                Directory.CreateDirectory(preferredPath);
                return preferredPath;
            }
            catch
            {
                // Fallback, když disk/složka není dostupná (např. odpojený síťový/USB disk P:)
                usedFallback = true;
                var fallback = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BohemiaSolutions", "Temp");

                Directory.CreateDirectory(fallback);
                return fallback;
            }
        }


        private string BuildParamsString(CheckedListBox clb)
        {
            var list = new List<string>();
            for (int i = 0; i < clb.Items.Count; i++)
                if (clb.GetItemChecked(i))
                    list.Add(clb.Items[i]?.ToString() ?? string.Empty);

            return string.Join(" ", list.Where(s => !string.IsNullOrWhiteSpace(s)));
        }


        private bool ConfirmCreateFolderIfMissing_MP(string path, string title = "Create profiles folder?")
        {
            if (string.IsNullOrWhiteSpace(path)) return true;
            if (Directory.Exists(path)) return true;

            var res = MessageBox.Show(
                $"The profiles folder does not exist:\n{path}\n\nCreate it now?",
                title,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel) return false; // zrušit celé uložení
            if (res == DialogResult.No) return true;      // uložit bez vytvoření složky

            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to create the folder:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        private async void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                var additionalParams = string.Join(" ", clbServerFlags.CheckedItems.Cast<string>());
                var type = cmbType.SelectedItem?.ToString();

                // Make sure Experimental server has DayZDiag_x64.exe available
                EnsureClientExecutablesOnServer();

                var clientParams = string.Join(" ", clbClientFlags.CheckedItems.Cast<string>());
                if (string.IsNullOrWhiteSpace(clientParams))
                {
                    // bezpečný fallback na tvoje defaulty
                    clientParams = "-window -nopause -disableCrashReport";
                }

                // --- VANILLA: žádná kontrola modů, uložit a pryč ---
                if (string.Equals(type, "Vanilla", StringComparison.OrdinalIgnoreCase))
                {
                    // Sestavíme výsledek přesně jako dřív
                    ResultConfig = new DayZConfig
                    {
                        Name = txtName.Text.Trim(),
                        ServerName = txtServerName.Text.Trim(),
                        Type = "Vanilla",
                        VersionFolder = cmbVersion.SelectedItem?.ToString(),
                        ServerPath = GetServerDirForSelectedVersion(),
                        ServerParameters = new ServerParams
                        {
                            ConfigFile = txtServerConfigFile.Text.Trim(),
                            ProfilesFolder = txtProfilesFolder.Text.Trim(),
                            Port = (int)numPort.Value,
                            Mods = new List<string>(),
                            CpuCount = (int)numCpuCount.Value,
                            ExeName = (cmbExeName.SelectedItem as ExeChoice)?.Path
                                      ?? (cmbExeName.SelectedItem as string) ?? "",
                            AdditionalParams = string.Join(" ", clbServerFlags.CheckedItems.Cast<string>())
                        },
                        ClientParameters = new ClientParams
                        {
                            Arguments = clientParams
                        }
                    };

                    // seber vybrané tagy + případný nový z textboxu
                    ResultConfig.Filters = CollectSelectedFilters();

                    // ✅ NOVĚ: potvrdit / (případně) vytvořit profily až TEĎ
                    var profiles = ResultConfig.ServerParameters?.ProfilesFolder?.Trim();
                    if (!ConfirmCreateFolderIfMissing_MP(profiles ?? "")) return;

                    // ✅ vytvoř kostru .cfg jen když chybí
                    CreateCfgIfMissingFromUi();
                    // zápis .cfg (jako dřív – probíhá až při OK)
                    UpdateServerConfigFile();

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }

                // --- MODDED: kontrola modů a závislostí ---
                var selectedMods = clbWorkshopMods.CheckedItems.Cast<string>().ToList();
                if (selectedMods.Count == 0)
                {
                    MessageBox.Show("No mods selected.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var logSb = new StringBuilder();

                bool usedFallback;
                var tempRoot = EnsureWritableDirectory(WorkTempModsRoot, out usedFallback);
                var sessionRoot = Path.Combine(tempRoot, "configs_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(sessionRoot);

                if (usedFallback)
                {
                    /* MessageBox.Show(
                        "The destination folder for the configurations is not available.:\n" + WorkTempModsRoot +
                        "\nI am saving to:\n" + sessionRoot,
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information); */
                }

                // progress dialog
                _progressDlg = new ProgressDialog();
                _progressDlg.StartPosition = FormStartPosition.CenterScreen;
                _progressDlg.Show(this);

                _extractStopwatch = Stopwatch.StartNew();
                _lastStatus = "Starting…";
                _progressDlg.SetStatus($"{_lastStatus}\nTime: 00:00");
                _progressDlg.SetProgress(0);
                progressUiTimer.Start();

                var perModPaths = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);
                for (int m = 0; m < selectedMods.Count; m++)
                {
                    string modName = selectedMods[m];
                    _lastStatus = $"[{m + 1}/{selectedMods.Count}] {modName} – extracting config.cpp…";
                    _progressDlg.SetStatus($"{_lastStatus}\nTime: {_extractStopwatch.Elapsed:mm\\:ss}");

                    var extracted = await ExtractOnlyConfigsForModAsync(
                        modName, sessionRoot, s => { _lastStatus = s; }, logSb);

                    perModPaths[modName] = extracted;

                    _progressDlg.SetProgress((int)Math.Round(((m + 1) * 100.0) / selectedMods.Count));
                }

                File.WriteAllText(Path.Combine(sessionRoot, "configs_session.log"), logSb.ToString(), Encoding.UTF8);

                progressUiTimer.Stop();
                _extractStopwatch?.Stop();
                _progressDlg.Done($"Done. Total time: {_extractStopwatch.Elapsed:mm\\:ss}");
                _progressDlg.Close();
                _progressDlg = null;

                // --- Analýza + řazení ---
                var (sorted, missing, cycles) = AnalyzeConfigsAndSort(sessionRoot, selectedMods);

                // Missing deps bez DZ_*
                var missingNoDz = missing
                    .Select(x =>
                    {
                        var arrowIdx = x.IndexOf('→');
                        var rhs = arrowIdx >= 0 ? x[(arrowIdx + 1)..].Trim() : x;
                        rhs = Regex.Replace(rhs, @"^\(patch\)\s*", "", RegexOptions.IgnoreCase).Trim();
                        rhs = rhs.TrimStart('@').Trim();
                        return (Orig: x, Rhs: rhs);
                    })
                    .Where(t => !t.Rhs.StartsWith("DZ_", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Orig)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (missingNoDz.Count > 0)
                {
                    var err = new StringBuilder();
                    err.AppendLine("Missing dependencies detected.");
                    err.AppendLine();
                    err.AppendLine("The configuration was NOT saved.");
                    err.AppendLine("Fix this by adding the missing dependency mods,");
                    err.AppendLine("or remove the mods that require them, and try again.");
                    err.AppendLine();
                    err.AppendLine("Details:");
                    foreach (var m in missingNoDz)
                        err.AppendLine(" - " + m);

                    MessageBox.Show(err.ToString(), "Missing dependencies", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // neukládat
                }

                // Info o pořadí (cykly jen varování)
                var rep = new StringBuilder();
                rep.AppendLine("Mods will be saved in this order:");
                rep.AppendLine();
                rep.AppendLine(string.Join("\n", sorted));
                if (cycles.Count > 0)
                {
                    /*  rep.AppendLine();
                        rep.AppendLine("Cycles detected (order may be approximate):");
                        rep.AppendLine(string.Join("\n", cycles.Select(c => $" - {c.A} ↔ {c.B}")));*/
                }
                MessageBox.Show(rep.ToString(), "Final Mod Order (will be saved)", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // ❌ PŮVODNÍ AUTO-CREATE PROFILŮ – odstraněno
                // if (!string.IsNullOrWhiteSpace(txtProfilesFolder.Text) &&
                //     !Directory.Exists(txtProfilesFolder.Text))
                // {
                //     Directory.CreateDirectory(txtProfilesFolder.Text);
                // }

                // --- Uložení (Modded) ---
                ResultConfig = new DayZConfig
                {
                    Name = txtName.Text.Trim(),
                    ServerName = txtServerName.Text.Trim(),
                    Type = "Modded",
                    VersionFolder = cmbVersion.SelectedItem?.ToString(),
                    ServerPath = GetServerDirForSelectedVersion(),
                    ClientParameters = new ClientParams
                    {
                        Arguments = clientParams
                    },
                    ServerParameters = new ServerParams
                    {
                        ConfigFile = txtServerConfigFile.Text.Trim(),
                        ProfilesFolder = txtProfilesFolder.Text.Trim(),
                        Port = (int)numPort.Value,
                        Mods = sorted.ToList(),
                        CpuCount = (int)numCpuCount.Value,
                        ExeName = (cmbExeName.SelectedItem as ExeChoice)?.Path
                                  ?? (cmbExeName.SelectedItem as string)
                                  ?? "",
                        AdditionalParams = additionalParams
                    }
                };
                ResultConfig.Filters = CollectSelectedFilters();

                // ✅ NOVĚ: potvrdit / (případně) vytvořit profily až TEĎ (po sestavení ResultConfig)
                var profilesModded = ResultConfig.ServerParameters?.ProfilesFolder?.Trim();
                if (!ConfirmCreateFolderIfMissing_MP(profilesModded ?? "")) return;


                // ✅ vytvoř kostru .cfg jen když chybí
                CreateCfgIfMissingFromUi();

                // zápis .cfg (jako dřív – probíhá až při OK)
                UpdateServerConfigFile();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                progressUiTimer.Stop();
                _extractStopwatch?.Stop();
                if (_progressDlg != null) { try { _progressDlg.Close(); } catch { } _progressDlg = null; }
                MessageBox.Show("Error: " + ex.Message);
            }
        }



        private void UpdateServerConfigFile()
        {
            string configPath = txtServerConfigFile.Text.Trim();
            if (string.IsNullOrWhiteSpace(configPath)) return;

            var lines = File.Exists(configPath) ? File.ReadAllLines(configPath).ToList() : new List<string>();

            UpdateOrInsertConfigValue(ref lines, "hostname", tb_cfg_hostname.Text);
            UpdateOrInsertConfigValue(ref lines, "password", tb_cfg_password.Text);
            UpdateOrInsertConfigValue(ref lines, "shardId", tb_cfg_shard_id.Text); // online only viz níž
            UpdateOrInsertConfigValue(ref lines, "maxPlayers", nb_cfg_max_players.Value.ToString(), quote: false);
            UpdateOrInsertConfigValue(ref lines, "battleye", nb_cfg_battleye.Value.ToString(), quote: false);

            // enviromentType jen když je online mise (dayz.*)
            var mission = cb_cfg_mpmission.SelectedItem?.ToString() ?? "";
            bool online = IsOnlineMission(mission);
            if (online)
            {
                var env = cb_cfg_enviromenttype.SelectedItem?.ToString() ?? "Stable";

                // ⬇⬇⬇ PŘIDÁNO: [Empty] znamená prázdný string
                if (string.Equals(env, "[Empty]", StringComparison.OrdinalIgnoreCase))
                    env = "";

                UpdateOrInsertConfigValue(ref lines, "enviromentType", env);
            }
            else
            {
                RemoveConfigKey(ref lines, "enviromentType");
                RemoveConfigKey(ref lines, "shardId");
            }

            UpdateTemplateInMissions(ref lines, mission);
            File.WriteAllLines(configPath, lines);
        }

        void UpdateOrInsertConfigValue(ref List<string> lines, string key, string value, bool quote = true)
        {
            bool found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].TrimStart();
                if (t.StartsWith(key + " "))
                {
                    string indent = lines[i].Substring(0, lines[i].IndexOf(key));
                    string v = quote ? $"\"{value}\"" : value;
                    lines[i] = $"{indent}{key} = {v};";
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                int insertIndex = lines.FindLastIndex(line => line.Trim() == "};");
                if (insertIndex == -1) insertIndex = lines.Count;
                string v = quote ? $"\"{value}\"" : value;
                lines.Insert(insertIndex, $"{key} = {v};");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnBrowseServerConfig_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Config Files (*.xml;*.json;*.cfg)|*.xml;*.json;*.cfg|All files (*.*)|*.*";
                openFileDialog.Title = "Choose Config File";

                var selectedVersion = cmbVersion.SelectedItem as string;
                if (!string.IsNullOrEmpty(selectedVersion))
                {
                    string initialDir = GetServerDirForSelectedVersion();
                    if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
                    {
                        openFileDialog.InitialDirectory = initialDir; // u server configu
                                                                      // dlg.InitialDirectory = initialDir;        // u profiles
                    }
                }

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtServerConfigFile.Text = openFileDialog.FileName;
                }
            }
        }

        private void btnBrowseProfiles_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Choose Profiles Folder";
                dlg.ValidateNames = false;   // umožní vybrat "pseudo soubor"
                dlg.CheckFileExists = false;
                dlg.CheckPathExists = true;
                dlg.RestoreDirectory = true;
                dlg.FileName = "Select Folder"; // placeholder, aby šlo potvrdit dialog

                string initialDir = GetServerDirForSelectedVersion();
                if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
                {
                    dlg.InitialDirectory = initialDir;   // <— TADY byla chyba
                }

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var picked = Path.GetDirectoryName(dlg.FileName); // skutečná složka
                    if (!string.IsNullOrEmpty(picked))
                        txtProfilesFolder.Text = picked;
                }
            }
        }

        private void btnOpenConfigFile_Click(object sender, EventArgs e)
        {
            string configPath = txtServerConfigFile.Text;
            if (File.Exists(configPath))
            {
                string EditorExePath = PathSettingsService.Current.EditorExePath;
                try
                {
                    Process.Start(EditorExePath, $"\"{configPath}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Cannot open VS Code.\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Config file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static bool IsWorkshopLinkPresent(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return false;
            string workshopPath = Path.Combine(dir, "!Workshop");
            return Directory.Exists(workshopPath) || File.Exists(workshopPath);
        }

        private void CreateSymlink(string link, string target)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C mklink /D \"{link}\" \"{target}\"",
                UseShellExecute = true,
                Verb = "runas"
            };
            try { Process.Start(psi); }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating symlink:\n" + ex.Message);
            }
        }

        private void doWorkshopFix() { }

        private void btnWorkshopFix_Click(object sender, EventArgs e)
        {
            string selectedVersion = cmbVersion.SelectedItem?.ToString();
            string serverDir = GetServerDirForSelectedVersion();
            string clientDir = (!string.IsNullOrEmpty(selectedVersion) && !IsSteamVersion(selectedVersion))
                ? Path.Combine(PathSettingsService.Current.BdsRoot, selectedVersion, "Client")
                : null;

            if (string.IsNullOrEmpty(serverDir) || !Directory.Exists(serverDir))
            {
                MessageBox.Show("Server directory not found.", "Info");
                return;
            }

            // zdroje (nemění se)
            string wsSrc =PathSettingsService.Current.WorkshopRoot;
            string lwsSrc = PathSettingsService.Current.LocalWorkshopRoot;

            // cíle
            string wsSrvLink = Path.Combine(serverDir, "!Workshop");
            string lwsSrvLink = Path.Combine(serverDir, "!LocalWorkshop");

            if (!IsLinkPresent(serverDir, "!Workshop") && Directory.Exists(wsSrc)) CreateSymlink(wsSrvLink, wsSrc);
            if (!IsLinkPresent(serverDir, "!LocalWorkshop") && Directory.Exists(lwsSrc)) CreateSymlink(lwsSrvLink, lwsSrc);

            if (!string.IsNullOrEmpty(clientDir) && Directory.Exists(clientDir))
            {
                string wsCliLink = Path.Combine(clientDir, "!Workshop");
                string lwsCliLink = Path.Combine(clientDir, "!LocalWorkshop");

                if (!IsLinkPresent(clientDir, "!Workshop") && Directory.Exists(wsSrc)) CreateSymlink(wsCliLink, wsSrc);
                if (!IsLinkPresent(clientDir, "!LocalWorkshop") && Directory.Exists(lwsSrc)) CreateSymlink(lwsCliLink, lwsSrc);
            }

            CheckWorkshopSymlinks(serverDir, clientDir);
        }

        private void CheckWorkshopSymlinks(string serverDir, string clientDir)
        {
            bool srvWS = IsLinkPresent(serverDir, "!Workshop");
            bool srvLWS = IsLinkPresent(serverDir, "!LocalWorkshop");

            bool cliWS = string.IsNullOrEmpty(clientDir) ? true : IsLinkPresent(clientDir, "!Workshop");
            bool cliLWS = string.IsNullOrEmpty(clientDir) ? true : IsLinkPresent(clientDir, "!LocalWorkshop");

            if (srvWS && srvLWS && cliWS && cliLWS)
            {
                lblWorkshopStatus.Text = "!Workshop / !LocalWorkshop OK";
                lblWorkshopStatus.ForeColor = Color.Green;
            }
            else
            {
                lblWorkshopStatus.Text = "Missing !Workshop or !LocalWorkshop symlink!";
                lblWorkshopStatus.ForeColor = Color.DarkRed;
            }
        }

        private void CopyModKeysToServer()
        {
            string selectedVersion = cmbVersion.SelectedItem?.ToString();
            string serverDir = GetServerDirForSelectedVersion();
            if (string.IsNullOrEmpty(serverDir) || !Directory.Exists(serverDir)) return;


            var mods = clbWorkshopMods.CheckedItems.Cast<string>().ToList();

            string keysTarget = Path.Combine(serverDir, "keys");
            Directory.CreateDirectory(keysTarget);

            foreach (var modName in mods)
            {
                // preferuj lokální, pak workshop
                var candidates = new[]
                {
                    Path.Combine(PathSettingsService.Current.LocalWorkshopRoot, modName, "keys"),
                    Path.Combine(PathSettingsService.Current.WorkshopRoot,     modName, "keys")
                };

                foreach (var modKeys in candidates.Where(Directory.Exists))
                {
                    foreach (var keyFile in Directory.GetFiles(modKeys, "*.bikey", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var dst = Path.Combine(keysTarget, Path.GetFileName(keyFile));
                            File.Copy(keyFile, dst, overwrite: true);
                        }
                        catch { /* ignore */ }
                    }
                }
            }
        }

        private bool workshopFixPrompted = false;

        private void SymlinkStatusTimer_Tick(object sender, EventArgs e)
        {
            string ver = cmbVersion.SelectedItem?.ToString();
            string serverDir = GetServerDirForSelectedVersion();
            string clientDir = (!string.IsNullOrEmpty(ver) && !IsSteamVersion(ver))
                ? Path.Combine(PathSettingsService.Current.BdsRoot, ver, "Client")
                : null;

            if (cmbType.SelectedItem?.ToString() == "Modded")
            {
                bool srvWS = IsLinkPresent(serverDir, "!Workshop");
                bool srvLWS = IsLinkPresent(serverDir, "!LocalWorkshop");
                bool cliWS = string.IsNullOrEmpty(clientDir) ? true : IsLinkPresent(clientDir, "!Workshop");
                bool cliLWS = string.IsNullOrEmpty(clientDir) ? true : IsLinkPresent(clientDir, "!LocalWorkshop");

                if ((!srvWS || !srvLWS || !cliWS || !cliLWS) && !workshopFixPrompted)
                {
                    workshopFixPrompted = true;
                    var res = MessageBox.Show(
                        "Symlinks !Workshop / !LocalWorkshop were not detected in Server and/or Client folders.\n" +
                        "They will be created automatically (Admin rights required).\n\nProceed?",
                        "Create symlinks",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

                    if (res == DialogResult.OK) btnWorkshopFix_Click(sender, e);
                }
                CheckWorkshopSymlinks(serverDir, clientDir);
            }
            else
            {
                lblWorkshopStatus.Text = "";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            symlinkStatusTimer.Stop();
            symlinkStatusTimer.Dispose();

            progressUiTimer.Stop();
            base.OnFormClosing(e);
        }

        private void btnBackupStorage_Click(object sender, EventArgs e)
        {
            string configPath = txtServerConfigFile.Text.Trim();
            string version = cmbVersion.SelectedItem?.ToString();
            string missionName = FindMissionNameInConfig(configPath);
            string missionDir = GetMissionDirectory(version, missionName);

            if (string.IsNullOrEmpty(missionDir))
            {
                MessageBox.Show("Mission directory not found!");
                return;
            }

            var storageDirs = Directory.GetDirectories(missionDir, "storage_*");
            if (storageDirs.Length == 0)
            {
                MessageBox.Show("No storage_x folder found in mission directory.");
                return;
            }

            foreach (var storage in storageDirs)
            {
                string zipPath = storage + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip";
                if (File.Exists(zipPath)) File.Delete(zipPath);
                System.IO.Compression.ZipFile.CreateFromDirectory(storage, zipPath);
                MessageBox.Show($"Storage backup created:\n{zipPath}");
            }
        }

        private string FindMissionNameInConfig(string configPath)
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return null;

            var lines = File.ReadAllLines(configPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("template="))
                {
                    var idx1 = trimmed.IndexOf('"');
                    var idx2 = trimmed.LastIndexOf('"');
                    if (idx1 >= 0 && idx2 > idx1)
                        return trimmed.Substring(idx1 + 1, idx2 - idx1 - 1);
                }
            }
            return null;
        }

        private string GetMissionDirectory(string version, string missionName)
        {
            if (string.IsNullOrWhiteSpace(missionName))
                return null;

            // BDS verze (C:\BDS\<ver>\Server\mpmissions\<mise>)
            if (!string.IsNullOrWhiteSpace(version) && !IsSteamVersion(version))
            {
                string p = Path.Combine(PathSettingsService.Current.BdsRoot, version, "Server", "mpmissions", missionName);
                return Directory.Exists(p) ? p : null;
            }

            // Steam verze – zkus dedikovaný server root, pak client root
            string[] roots;
            if (string.Equals(version, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase))
            {
                roots = new[] { PathSettingsService.Current.DayZServerStableRoot, PathSettingsService.Current.DayZStableDir };
            }
            else if (string.Equals(version, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase))
            {
                roots = new[] { PathSettingsService.Current.DayZServerExpRoot, PathSettingsService.Current.DayZExperimentalDir };
            }
            else if (string.Equals(version, VersionItem_DayZInternalSteam, StringComparison.OrdinalIgnoreCase))
            {
                roots = new[] { PathSettingsService.Current.DayZServerInternalRoot, PathSettingsService.Current.DayZInternalDir }; // NEW
            }
            else
            {
                // Fallback – projdi vše známé
                roots = new[] {
                    PathSettingsService.Current.DayZServerStableRoot, PathSettingsService.Current.DayZServerExpRoot, PathSettingsService.Current.DayZServerInternalRoot, // NEW
                    PathSettingsService.Current.DayZStableDir, PathSettingsService.Current.DayZExperimentalDir, PathSettingsService.Current.DayZInternalDir              // NEW
                };
            }

            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
                string p = Path.Combine(root, "mpmissions", missionName);
                if (Directory.Exists(p)) return p;
            }
            return null;
        }

        private void btnWipeStorage_Click(object sender, EventArgs e)
        {
            string configPath = txtServerConfigFile.Text.Trim();
            string version = cmbVersion.SelectedItem?.ToString();
            string missionName = FindMissionNameInConfig(configPath);
            string missionDir = GetMissionDirectory(version, missionName);

            if (string.IsNullOrEmpty(missionDir))
            {
                MessageBox.Show("Mission directory not found!");
                return;
            }

            var storageDirs = Directory.GetDirectories(missionDir, "storage_*");
            if (storageDirs.Length == 0)
            {
                MessageBox.Show("No storage_x folder found in mission directory.");
                return;
            }

            bool hadError = false;
            foreach (var storage in storageDirs)
            {
                try
                {
                    Directory.Delete(storage, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting storage: " + ex.Message);
                    hadError = true;
                }
            }

            if (!hadError)
                MessageBox.Show("All storage folders wiped!");

            btnBackupStorage.Enabled = false;
            btnWipeStorage.Enabled = false;
        }

        private void txtServerConfigFile_TextChanged(object sender, EventArgs e)
        {
            string configPath = txtServerConfigFile.Text.Trim();

            lbl_cfg_title.Text = Path.GetFileName(configPath);
            if (!File.Exists(configPath)) return;

            string hostname = "";
            string password = "";
            string shardId = "";     // ← NOVÉ
            int maxPlayers = 60;
            int battleye = 1;
            string template = "";
            string enviromentType = ""; // NOVÉ

            var lines = File.ReadAllLines(configPath);
            foreach (var line in lines)
            {
                var t = line.Trim();
                if (t.StartsWith("hostname")) hostname = ExtractConfigString(t);
                else if (t.StartsWith("passwordAdmin")) { /* ignore / doplníš později */ }
                else if (t.StartsWith("password")) password = ExtractConfigString(t);
                else if (t.StartsWith("shardId")) shardId = ExtractConfigString(t); // ← NOVÉ
                else if (t.StartsWith("maxPlayers")) int.TryParse(ExtractConfigNumber(t), out maxPlayers);
                else if (t.StartsWith("battleye")) int.TryParse(ExtractConfigNumber(t), out battleye);
                else if (t.StartsWith("template")) template = ExtractConfigString(t);
                else if (t.StartsWith("enviromentType", StringComparison.OrdinalIgnoreCase))
                    enviromentType = ExtractConfigString(t);
            }

            tb_cfg_hostname.Text = hostname;
            tb_cfg_password.Text = password;
            tb_cfg_shard_id.Text = shardId; // ← NOVÉ
            nb_cfg_max_players.Value = maxPlayers;
            nb_cfg_battleye.Value = battleye;

            // env type vybrat podle .cfg (pojištěno, že Items existují)
            InitEnvTypeComboIfNeeded();

            if (enviromentType != null)
            {
                if (enviromentType.Length == 0)
                {
                    // ⬇⬇⬇ PŘIDÁNO: prázdná hodnota → zvol [Empty]
                    int ixEmpty = cb_cfg_enviromenttype.FindStringExact("[Empty]");
                    if (ixEmpty >= 0) cb_cfg_enviromenttype.SelectedIndex = ixEmpty;
                }
                else
                {
                    int ix = cb_cfg_enviromenttype.FindStringExact(enviromentType);
                    if (ix >= 0) cb_cfg_enviromenttype.SelectedIndex = ix;
                }
            }

            string version = cmbVersion.SelectedItem?.ToString();
            string missionName = FindMissionNameInConfig(configPath);
            string missionDir = GetMissionDirectory(version, missionName);

            bool missionExists = !string.IsNullOrEmpty(missionDir) && Directory.Exists(missionDir);
            btnBrowseInit.Enabled = missionExists;
            btnBrowseStorage.Enabled = missionExists;
            btnBackupStorage.Enabled = missionExists;
            btnWipeStorage.Enabled = missionExists;
            bool hasInit = missionExists && File.Exists(Path.Combine(missionDir, "init.c"));
            btnBackupInit.Enabled = hasInit;     // tlačítko z UI

            // pokud máš tlačítko na otevření initu:
            try
            {
                hasInit = missionExists && File.Exists(Path.Combine(missionDir, "init.c"));
                btnOpenInitFile.Enabled = hasInit;   // UI připravené – jen zapnout/vypnout
            }
            catch
            {
                btnOpenInitFile.Enabled = false;
            }

            cb_cfg_mpmission.Items.Clear();

            string serverRoot = GetServerDirForSelectedVersion();
            if (!string.IsNullOrEmpty(serverRoot))
            {
                string mpmissionsPath = Path.Combine(serverRoot, "mpmissions");
                if (Directory.Exists(mpmissionsPath))
                {
                    var missions = Directory.GetDirectories(mpmissionsPath)
                                            .Select(Path.GetFileName)
                                            .ToArray();
                    cb_cfg_mpmission.Items.AddRange(missions);
                }
            }
            cb_cfg_mpmission.SelectedItem = template;

            // když je mise online a nic ještě není vybráno, nastav default na Stable
            UpdateOnlineFieldsAvailability();
            if (cb_cfg_enviromenttype.Enabled && cb_cfg_enviromenttype.SelectedIndex < 0)
                cb_cfg_enviromenttype.SelectedItem = "Stable";
        }

        private string ExtractConfigString(string line)
        {
            var firstQuote = line.IndexOf('"');
            var lastQuote = line.LastIndexOf('"');
            if (firstQuote >= 0 && lastQuote > firstQuote)
                return line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);

            var eq = line.IndexOf('=');
            var semi = line.LastIndexOf(';');
            if (eq >= 0 && semi > eq)
                return line.Substring(eq + 1, semi - eq - 1).Trim().Trim('"');

            return "";
        }

        private string ExtractConfigNumber(string line)
        {
            var eq = line.IndexOf('=');
            var semi = line.LastIndexOf(';');
            if (eq >= 0 && semi > eq)
                return line.Substring(eq + 1, semi - eq - 1).Trim();
            return "";
        }

        private void clbWorkshopMods_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                CopyModKeysToServer();
            });
        }

        // KROK 1: vyzobat jen config.cpp ze všech PBO daného modu,
        // s logováním a fail-safe čekáním.
        // ZÁVISLOSTI: StartProcess(...), WaitForConfigOrExitAsync(...), SanitizeFileName(...),PathSettingsService.Current.WorkshopRoot.
        private async Task<Dictionary<string, List<string>>> ExtractOnlyConfigsForModAsync(
            string modName,
            string outputRoot,
            Action<string>? status,
            StringBuilder logSb)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // @<mod>\Addons
            string modDir = ResolveModFolder(modName);   // místo dřívějšího Path.Combine(WorkshopRoot, modName)
            string addons = Path.Combine(modDir, "Addons");
            if (!Directory.Exists(addons))
                throw new DirectoryNotFoundException($"Mod '{modName}' nemá složku Addons: {addons}");

            // výstup: <sessionRoot>\<modSanitized>\
            string modOut = Path.Combine(outputRoot, SanitizeFileName(modName));
            Directory.CreateDirectory(modOut);

            var pboList = Directory.GetFiles(addons, "*.pbo", SearchOption.TopDirectoryOnly);
            if (pboList.Length == 0) return result;

            // smyčka přes PBO
            for (int i = 0; i < pboList.Length; i++)
            {
                var pbo = pboList[i];
                string pboName = Path.GetFileName(pbo);
                string pboStem = Path.GetFileNameWithoutExtension(pbo);

                // rychlé dočasné místo ve %TEMP%
                string tempBase = Path.Combine(Path.GetTempPath(), "dz_cfg_tmp");
                Directory.CreateDirectory(tempBase);
                string tempDir = Path.Combine(tempBase, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                // status bez času (čas dopisuje UI timer)
                string s = $"[{modName}] Extract {pboName} ({i + 1}/{pboList.Length})…";
                status?.Invoke(s);
                logSb?.AppendLine($"{DateTime.Now:HH:mm:ss} START {modName}:{pboName} -> {tempDir}");

                Process proc = null;
                try
                {

                    // spustíme Mikero bez redirectů (ať neblokuje pipe)
                    var mikero = PathSettingsService.Current.MikeroExtractPbo;
                    proc = StartProcess(
                        mikero,
                        $"\"{pbo}\" \"{tempDir}\"",
                        Path.GetDirectoryName(mikero)!);

                    // FAIL-SAFE: čekej max 20 s na první config.cpp (poll 150 ms), pak 300 ms grace a kill
                    await WaitForConfigOrExitAsync(
                        proc,
                        tempDir,
                        maxWaitForConfig: TimeSpan.FromSeconds(20),
                        pollInterval: TimeSpan.FromMilliseconds(150),
                        killGrace: TimeSpan.FromMilliseconds(300)
                    );
                }
                finally
                {
                    try { if (proc != null && !proc.HasExited) proc.Kill(true); } catch { }
                    try { proc?.Dispose(); } catch { }
                }

                // najdi configy a zkopíruj do <sessionRoot>\<mod>\<pbo>\config*.cpp
                var cfgs = Directory.Exists(tempDir)
                    ? Directory.GetFiles(tempDir, "config.cpp", SearchOption.AllDirectories)
                    : Array.Empty<string>();

                if (cfgs.Length > 0)
                {
                    string pboOut = Path.Combine(modOut, SanitizeFileName(pboStem));
                    Directory.CreateDirectory(pboOut);

                    var saved = new List<string>();
                    foreach (var cfg in cfgs)
                    {
                        string target = Path.Combine(pboOut, "config.cpp");
                        if (File.Exists(target))
                        {
                            int idx = 2;
                            while (true)
                            {
                                string cand = Path.Combine(pboOut, $"config_{idx}.cpp");
                                if (!File.Exists(cand)) { target = cand; break; }
                                idx++;
                            }
                        }

                        File.Copy(cfg, target, true);
                        saved.Add(target);
                        logSb?.AppendLine($"{DateTime.Now:HH:mm:ss} FOUND  {modName}:{pboName} -> {target}");
                    }
                    result[pboStem] = saved;
                }
                else
                {
                    logSb?.AppendLine($"{DateTime.Now:HH:mm:ss} SKIP   {modName}:{pboName} (no config.cpp in {tempDir})");
                }

                // úklid dočasného adresáře
                try { Directory.Delete(tempDir, true); } catch { }
            }

            return result;
        }

        private async Task<int> RunToolAsync(string exe, string args, string workDir, int timeoutMs = 120000)
        {
            var tcs = new TaskCompletionSource<int>();

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = workDir ?? Path.GetDirectoryName(exe)!,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = true
            };

            if (!p.Start())
                throw new InvalidOperationException($"Process start failed: {exe}");

            p.Exited += (_, __) =>
            {
                try { tcs.TrySetResult(p.ExitCode); }
                finally { try { p.Dispose(); } catch { } }
            };

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            if (completed != tcs.Task)
            {
                try { p.Kill(true); } catch { }
                throw new TimeoutException($"{Path.GetFileName(exe)} timeout ({timeoutMs} ms). Args: {args}");
            }

            return await tcs.Task;
        }

        // >>> TADY UŽ OPRAVDU UPDATUJEME KAŽDOU SEKUNDU <<<
        private void ProgressUiTimer_Tick(object sender, EventArgs e)
        {
            if (_progressDlg == null || _extractStopwatch == null) return;
            _progressDlg.SetStatus($"{_lastStatus}\nTime: {_extractStopwatch.Elapsed:mm\\:ss}");
        }

        // --- zbytek beze změn (UpdateTemplateInMissions, ExtractRequiredAddonsFromConfigCpp atd.) ---
        private void UpdateTemplateInMissions(ref List<string> lines, string newTemplate)
        {
            int missionsStart = -1, dayzStart = -1, dayzEnd = -1, braceDepth = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("class Missions"))
                    missionsStart = i;

                if (missionsStart >= 0 && line.StartsWith("class DayZ"))
                {
                    dayzStart = i;
                    braceDepth = 0;

                    for (int j = i; j < lines.Count; j++)
                    {
                        foreach (char c in lines[j])
                        {
                            if (c == '{') braceDepth++;
                            if (c == '}') braceDepth--;
                        }
                        if (j > dayzStart && braceDepth == 0)
                        {
                            dayzEnd = j;
                            break;
                        }
                    }
                    break;
                }
            }

            if (dayzStart < 0 || dayzEnd <= dayzStart) return;

            bool replaced = false;
            for (int i = dayzStart; i < dayzEnd; i++)
            {
                var t = lines[i].TrimStart();
                if (t.StartsWith("template"))
                {
                    string indent = lines[i].Substring(0, lines[i].IndexOf('t'));
                    lines[i] = $"{indent}template=\"{newTemplate}\";";
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                int insert = -1;
                for (int i = dayzStart; i < dayzEnd; i++)
                {
                    if (lines[i].Contains("{"))
                    {
                        insert = i + 1;
                        break;
                    }
                }
                if (insert > -1)
                    lines.Insert(insert, "    template=\"" + newTemplate + "\";");
            }
        }

        // START proces bez čekání (beze změny stdout/err, ať to neblokuje pipe)
        private Process StartProcess(string exe, string args, string workDir)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = workDir ?? Path.GetDirectoryName(exe)!,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            if (!p.Start()) throw new InvalidOperationException($"Process start failed: {exe}");
            return p;
        }

        // Čeká na výskyt config.cpp NEBO na exit procesu.
        // Když se najde config dřív, dáme krátký "grace" a proces ukončíme.
        private async Task<bool> WaitForConfigOrExitAsync(Process p, string tempDir,
            TimeSpan maxWaitForConfig, TimeSpan pollInterval, TimeSpan killGrace)
        {
            var sw = Stopwatch.StartNew();

            while (true)
            {
                if (p.HasExited) return true;

                if (Directory.Exists(tempDir))
                {
                    var anyCfg = Directory.EnumerateFiles(tempDir, "config.cpp", SearchOption.AllDirectories).Any();
                    if (anyCfg)
                    {
                        await Task.Delay(killGrace);
                        try { if (!p.HasExited) p.Kill(true); } catch { }
                        return true;
                    }
                }

                if (sw.Elapsed >= maxWaitForConfig) break;
                await Task.Delay(pollInterval);
            }

            // fallback: ještě krátce počkej na exit, pak kill
            var tcs = new TaskCompletionSource<bool>();
            p.EnableRaisingEvents = true;
            p.Exited += (_, __) => { try { tcs.TrySetResult(true); } catch { } };

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            if (completed != tcs.Task)
            {
                try { if (!p.HasExited) p.Kill(true); } catch { }
            }
            return true;
        }

        private static List<string> ExtractRequiredAddonsFromConfigCpp(string cppPath)
        {
            var txt = File.ReadAllText(cppPath);
            var list = new List<string>();

            var rxPatches = new Regex(@"class\s+CfgPatches\s*\{(?<body>[\s\S]*?)\}\s*;", RegexOptions.IgnoreCase);
            var m = rxPatches.Match(txt);
            if (!m.Success) return list;

            var body = m.Groups["body"].Value;
            var rxReq = new Regex(@"requiredAddons\s*\[\s*\]\s*=\s*\{(?<items>[\s\S]*?)\};", RegexOptions.IgnoreCase);

            foreach (Match r in rxReq.Matches(body))
            {
                foreach (Match s in Regex.Matches(r.Groups["items"].Value, "\"([^\"]+)\""))
                    list.Add(s.Groups[1].Value.Trim());
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // Vrátí všechny config*.cpp pro daný mod ve session rootu
        private static IEnumerable<string> EnumerateSavedConfigs(string sessionRoot, string modName)
        {
            string modDir = Path.Combine(sessionRoot, SanitizeFileName(modName));
            if (!Directory.Exists(modDir)) yield break;

            foreach (var pboDir in Directory.EnumerateDirectories(modDir))
            {
                foreach (var f in Directory.EnumerateFiles(pboDir, "config*.cpp", SearchOption.TopDirectoryOnly))
                    yield return f;
            }
        }

        // Krok 2: z načtených configů sestaví mapu patchů + načte meta deps a pak seřadí
        private (List<string> Sorted, List<string> Missing, List<(string A, string B)> Cycles)
            AnalyzeConfigsAndSort(string sessionRoot, List<string> selectedMods)
        {
            // 1) posbírej per-mod patche
            var perMod = new Dictionary<string, ModDeps>(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in selectedMods)
            {
                var md = new ModDeps();

                foreach (var cfg in EnumerateSavedConfigs(sessionRoot, mod))
                {
                    var (prov, req) = ParseDepsFromConfigCpp(cfg);
                    foreach (var p in prov) md.ProvidedPatches.Add(p);
                    foreach (var r in req) md.RequiredPatches.Add(r);
                }

                perMod[mod] = md;
            }

            // 2) postav mapu "patch -> kteří mody ho poskytují"
            var patchProviders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in perMod)
                foreach (var p in kv.Value.ProvidedPatches)
                {
                    if (!patchProviders.TryGetValue(p, out var list))
                        patchProviders[p] = list = new List<string>();
                    if (!list.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                        list.Add(kv.Key);
                }

            // 3) přidej META mod-deps (dependencies[] z meta.cpp)
            var metaDeps = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in selectedMods)
            {
                var deps = ModDependencyResolver.ReadMetaDependencies(ResolveModRoot(mod), mod)
                    .Select(s => s.Trim().TrimStart('@'))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                metaDeps[mod] = deps;
            }

            // 4) Sestav graf modů: hrana A->B pokud A vyžaduje patch, který poskytuje B, NEBO pokud A závisí na B (meta)
            var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var indeg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in selectedMods) { graph[m] = new HashSet<string>(StringComparer.OrdinalIgnoreCase); indeg[m] = 0; }

            var missing = new List<string>();

            // edges z patchů
            foreach (var m in selectedMods)
            {
                foreach (var req in perMod[m].RequiredPatches)
                {
                    if (!patchProviders.TryGetValue(req, out var providers) || providers.Count == 0)
                    {
                        missing.Add($"{m} → (patch) {req}");
                        continue;
                    }
                    foreach (var prov in providers)
                    {
                        if (!selectedMods.Contains(prov, StringComparer.OrdinalIgnoreCase)) continue;
                        if (string.Equals(prov, m, StringComparison.OrdinalIgnoreCase)) continue;
                        if (graph[m].Add(prov)) indeg[m]++;
                    }
                }
            }

            // edges z META (jen mezi vybranými)
            foreach (var m in selectedMods)
            {
                foreach (var dep in metaDeps[m])
                {
                    var target = selectedMods.FirstOrDefault(x => string.Equals(x.Trim('@'), dep, StringComparison.OrdinalIgnoreCase)
                                                               || string.Equals(x, dep, StringComparison.OrdinalIgnoreCase)
                                                               || string.Equals(x.TrimStart('@'), dep.TrimStart('@'), StringComparison.OrdinalIgnoreCase));
                    if (string.IsNullOrEmpty(target))
                    {
                        // meta závislost existuje, ale mod není vybrán
                        missing.Add($"{m} → @{dep}");
                        continue;
                    }
                    if (!string.Equals(target, m, StringComparison.OrdinalIgnoreCase))
                    {
                        if (graph[m].Add(target)) indeg[m]++;
                    }
                }
            }

            // 5) Topologické řazení (Kahn) s prioritním výběrem podle cheatsheetu
            var sorted = new List<string>();
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var zero = new List<string>(indeg.Where(kv => kv.Value == 0).Select(kv => kv.Key));

            while (zero.Count > 0)
            {
                zero.Sort(CompareModsByPolicy);
                var u = zero[0];
                zero.RemoveAt(0);

                if (!processed.Add(u)) continue;

                sorted.Add(u);

                if (!graph.TryGetValue(u, out var outs)) continue;
                foreach (var v in outs)
                {
                    indeg[v]--;
                    if (indeg[v] == 0) zero.Add(v);
                }
            }

            var cycles = new List<(string A, string B)>();
            if (sorted.Count < indeg.Count)
            {
                var remaining = indeg.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();

                // Najdi odhadem páry v cyklu
                foreach (var r in remaining)
                {
                    if (!graph.TryGetValue(r, out var outs)) continue;
                    foreach (var v in outs)
                        if (indeg[v] > 0) cycles.Add((r, v));
                }

                // Doplň zbylé v prioritním pořadí (už s vědomím, že jsou v cyklu)
                foreach (var r in remaining.OrderBy(m => m, Comparer<string>.Create(CompareModsByPolicy)))
                    if (!sorted.Contains(r))
                        sorted.Add(r);
            }

            var finalSorted = ReorderWithExpansionCore(sorted, perMod, metaDeps);
            return (finalSorted, missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), cycles);
        }

        private static (List<string> provided, List<string> required) ParseDepsFromConfigCpp(string cfgCppPath)
        {
            string txt = File.ReadAllText(cfgCppPath);

            // 1) odstranit komentáře //... a /* ... */
            txt = Regex.Replace(txt, @"//.*?$", "", RegexOptions.Multiline);
            txt = Regex.Replace(txt, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // 2) začátek class CfgPatches
            var mStart = Regex.Match(txt, @"class\s+CfgPatches\b", RegexOptions.IgnoreCase);
            if (!mStart.Success) return (new List<string>(), new List<string>());

            int i = mStart.Index;
            int braceOpen = txt.IndexOf('{', i);
            if (braceOpen < 0) return (new List<string>(), new List<string>());

            // 3) vyříznout tělo CfgPatches { ... } počítáním závorek
            int pos = braceOpen + 1, depth = 1;
            while (pos < txt.Length && depth > 0)
            {
                char c = txt[pos++];
                if (c == '{') depth++;
                else if (c == '}') depth--;
            }
            if (depth != 0) return (new List<string>(), new List<string>());

            string body = txt.Substring(braceOpen + 1, pos - braceOpen - 2);

            // 4) uvnitř hledat class XYZ { ... }; a z nich requiredAddons
            var provided = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var classRx = new Regex(@"class\s+(?<name>[A-Za-z0-9_]+)\s*\{", RegexOptions.IgnoreCase);
            var mc = classRx.Matches(body);
            foreach (Match cm in mc)
            {
                string name = cm.Groups["name"].Value;

                // tělo třídy
                int s = cm.Index;
                int b = body.IndexOf('{', s);
                if (b < 0) continue;

                int p = b + 1, d = 1;
                while (p < body.Length && d > 0)
                {
                    char ch = body[p++];
                    if (ch == '{') d++;
                    else if (ch == '}') d--;
                }
                if (d != 0) continue;

                string clsBody = body.Substring(b + 1, p - b - 2);

                if (!string.IsNullOrWhiteSpace(name))
                    provided.Add(name);

                // requiredAddons[] = { "A","B" };
                var reqMatch = Regex.Match(clsBody, @"requiredAddons\s*\[\s*\]\s*=\s*\{(?<items>.*?)\};",
                                           RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (reqMatch.Success)
                {
                    foreach (Match q in Regex.Matches(reqMatch.Groups["items"].Value, "\"([^\"]+)\""))
                        required.Add(q.Groups[1].Value.Trim());
                }
            }

            return (provided.ToList(), required.ToList());
        }

        private void UpdateOkButtonText()
        {
            var t = cmbType.SelectedItem?.ToString();
            if (string.Equals(t, "Vanilla", StringComparison.OrdinalIgnoreCase))
                btnOK.Text = "Save";
            else
                btnOK.Text = "Save And Check Mods Dependencies";
        }

        private string? GetMpmissionsRootForSelectedVersion()
        {
            // Použij jednotnou logiku – vždy si nech vrátit kořen serveru pro zvolenou "verzi"
            var serverDir = GetServerDirForSelectedVersion();
            if (string.IsNullOrWhiteSpace(serverDir) || !Directory.Exists(serverDir))
                return null;

            // Misions jsou vždy v <serverDir>\mpmissions
            var root = Path.Combine(serverDir, "mpmissions");
            return Directory.Exists(root) ? root : null;
        }

        private void PopulateChooseMp()
        {
            cb_chooseMP.Items.Clear();
            cb_chooseMP.Enabled = false;

            var root = GetMpmissionsRootForSelectedVersion();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return;

            var missions = Directory
                .GetDirectories(root)
                .Select(Path.GetFileName)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            cb_chooseMP.Items.AddRange(missions);
            cb_chooseMP.Enabled = missions.Length > 0;
        }

        private void cb_chooseMP_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtServerConfigFile.Text = "";
            txtProfilesFolder.Text = "";
            var mission = cb_chooseMP.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(mission)) return;

            int ix = cb_cfg_mpmission.Items.IndexOf(mission);
            if (ix < 0) { cb_cfg_mpmission.Items.Add(mission); ix = cb_cfg_mpmission.Items.IndexOf(mission); }
            cb_cfg_mpmission.SelectedIndex = ix;

            // CreateAutoConfigIfReady();  // ❌ žádné vytváření souborů/dirs
            PredictAutoFieldsIfReady();    // ✅ jen doplnit textboxy (bez IO)
            UpdateOnlineFieldsAvailability();
        }

        private static string SafeSegment(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var cleaned = Regex.Replace(s, "[^A-Za-z0-9]+", "_"); // tečky, mezery → _
            cleaned = Regex.Replace(cleaned, "_{2,}", "_").Trim('_');
            return cleaned;
        }

        private static string MakeUniquePath(string dir, string fileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            string full = Path.Combine(dir, fileName);
            int i = 2;
            while (File.Exists(full))
            {
                full = Path.Combine(dir, $"{baseName}_{i}{ext}");
                i++;
            }
            return full;
        }

        private string BuildAutoConfigFileName()
        {
            string ver = SafeSegment(cmbVersion.SelectedItem as string);
            string typ = SafeSegment(cmbType.SelectedItem as string);
            string mp = SafeSegment(cb_chooseMP.SelectedItem as string); // <- mise do názvu

            // výsledný formát: ServerDZ<ver><typ><mise>.cfg
            // příklad: ServerDZ128_BranchVanilladayzOffline_chernarusplus.cfg
            return $"ServerDZ{ver}{typ}{mp}.cfg";
        }


        private string? GetServerDirForSelectedVersion()
        {
            var v = cmbVersion.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(v)) return null;

            if (string.Equals(v, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZServerStableRoot;
            if (string.Equals(v, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZServerExpRoot;
            if (string.Equals(v, VersionItem_DayZInternalSteam, StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZServerInternalRoot;

            // BDS verze
            return Path.Combine(PathSettingsService.Current.BdsRoot, v, "Server");
        }

        private string? GetServerDirForVersion(string versionName)
        {
            if (string.IsNullOrWhiteSpace(versionName)) return null;

            if (string.Equals(versionName, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZServerStableRoot;

            if (string.Equals(versionName, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZServerExpRoot;

            return Path.Combine(PathSettingsService.Current.BdsRoot, versionName, "Server");
        }

        private static string MakeUniqueDirectory(string basePath)
        {
            if (!Directory.Exists(basePath)) return basePath;
            int i = 2;
            while (true)
            {
                var candidate = basePath + "_" + i;
                if (!Directory.Exists(candidate)) return candidate;
                i++;
            }
        }

        // stejné složení názvu jako cfg soubor (bez .cfg)
        private static string BuildAutoProfilesFolderPath(string serverDir, string cfgFileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(cfgFileName); // např. ServerDZ128_BranchVanilladayz_chernarusplus
            string raw = Path.Combine(serverDir, "profiles", baseName);
            return MakeUniqueDirectory(raw);
        }

        private void ApplyEditModeUi()
        {
            // Výběr šablony MP mise se zobrazuje jen při zakládání nové konfigurace
            bool showChooseMp = !isEdit;
            label17.Visible = showChooseMp;
            label17.TabStop = false;
            cb_chooseMP.Visible = showChooseMp;
            cb_chooseMP.TabStop = showChooseMp;
            cb_chooseMP.Enabled = showChooseMp;

            // Tlačítka pro práci se soubory (Init/Storage) povol jen v editaci
            SetInitStorageButtonsEnabled(isEdit);
        }

        private void CreateAutoConfigIfReady()
        {
            if (isEdit) return;  // v editaci přeskočit

            // musí být vybraná všechna 3 pole
            if (cmbVersion.SelectedIndex < 0 || cmbType.SelectedIndex < 0)
                return;

            string serverDir = GetServerDirForSelectedVersion();
            if (string.IsNullOrEmpty(serverDir) || !Directory.Exists(serverDir))
                return;

            string fileName = BuildAutoConfigFileName();
            string fullPath = MakeUniquePath(serverDir, fileName);

            // >>> NOVÉ: auto-profiles složka (stejné složení názvu jako cfg) <<<
            string profilesPath = BuildAutoProfilesFolderPath(serverDir, fileName);
            Directory.CreateDirectory(profilesPath);
            txtProfilesFolder.Text = profilesPath;

            // --- hodnoty z UI ---
            string hostname = string.IsNullOrWhiteSpace(tb_cfg_hostname.Text) ? "DayZ Server" : tb_cfg_hostname.Text;
            string password = tb_cfg_password.Text ?? "";
            int maxPlayers = (int)nb_cfg_max_players.Value;
            int battleye = (int)nb_cfg_battleye.Value;
            string template = cb_chooseMP.SelectedItem?.ToString() ?? "dayzOffline.chernarusplus";

            // --- defaulty / další klíče (můžeš je časem navázat na další ovládací prvky) ---
            string passwordAdmin = "";
            string description = "";
            int enableWhitelist = 0;
            int verifySignatures = 2;
            int forceSameBuild = 1;
            int disableVoN = 0;
            int vonCodecQuality = 20;
            int disable3rdPerson = 0;
            int disableCrosshair = 0;
            int disablePersonalLight = 1;
            int lightingConfig = 0;
            string serverTime = "SystemTime";
            int serverTimeAcceleration = 12;
            int serverNightTimeAcceleration = 1;
            int serverTimePersistent = 0;
            int guaranteedUpdates = 1;
            int loginQueueConcurrentPlayers = 5;
            int loginQueueMaxPlayers = 500;
            int instanceId = 1;
            int storageAutoFix = 1;
            int enableCfgGameplayFile = 1;

            // --- online mise? (dayz.*) → přidej shardId a enviromentType ---
            bool isOnline = IsOnlineMission(template);
            string shardId = isOnline
                ? (string.IsNullOrWhiteSpace(tb_cfg_shard_id.Text) ? "123abc" : tb_cfg_shard_id.Text)
                : null;
            string envType = isOnline
                ? (cb_cfg_enviromenttype.SelectedItem?.ToString() ?? "Stable")
                : null;

            // ⬇⬇⬇ PŘIDÁNO
            if (string.Equals(envType, "[Empty]", StringComparison.OrdinalIgnoreCase))
                envType = "";

            var sb = new StringBuilder();
            sb.AppendLine($"hostname = \"{hostname}\";");
            sb.AppendLine($"password = \"{password}\";");
            sb.AppendLine($"passwordAdmin = \"{passwordAdmin}\";         // Password to become a server admin");
            sb.AppendLine();
            sb.AppendLine($"description = \"{description}\";\t\t\t// Description of the server. Gets displayed to users in client server browser.");
            sb.AppendLine();
            sb.AppendLine($"enableWhitelist = {enableWhitelist};        // Enable/disable whitelist (value 0-1)");
            sb.AppendLine();
            sb.AppendLine($"maxPlayers = {maxPlayers};");
            sb.AppendLine();
            sb.AppendLine($"verifySignatures = {verifySignatures};       // Verifies .pbos against .bisign files. (only 2 is supported)");
            sb.AppendLine($"forceSameBuild = {forceSameBuild};         // When enabled, allows only clients with same .exe revision (0-1)");
            sb.AppendLine();
            sb.AppendLine($"disableVoN = {disableVoN};             // Enable/disable voice over network (0-1)");
            sb.AppendLine($"vonCodecQuality = {vonCodecQuality};       // Voice over network codec quality (0-30)");
            sb.AppendLine();
            if (isOnline)
            {
                sb.AppendLine($"enviromentType = \"{envType}\";     // Internal / Stable / Experimental (online economy DB)");
            }
            sb.AppendLine($"disable3rdPerson={disable3rdPerson};         // Toggles the 3rd person view for players (0-1)");
            sb.AppendLine($"disableCrosshair={disableCrosshair};         // Toggles the cross-hair (0-1)");
            sb.AppendLine();
            sb.AppendLine($"disablePersonalLight = {disablePersonalLight};   // Disables personal light for all clients connected to server");
            sb.AppendLine($"lightingConfig = {lightingConfig};         // 0 for brighter night setup, 1 for darker night setup");
            sb.AppendLine();
            sb.AppendLine($"serverTime=\"{serverTime}\";    // Initial in-game time. \"SystemTime\" = local machine time.");
            sb.AppendLine($"serverTimeAcceleration={serverTimeAcceleration};  // Accelerated Time (0-24)");
            sb.AppendLine($"serverNightTimeAcceleration={serverNightTimeAcceleration};  // Night multiplier (0.1-64), multiplied by serverTimeAcceleration");
            sb.AppendLine($"serverTimePersistent={serverTimePersistent};     // Persistent Time (0-1)");
            sb.AppendLine();
            sb.AppendLine($"guaranteedUpdates={guaranteedUpdates};        // Communication protocol (use only 1)");
            sb.AppendLine();
            sb.AppendLine($"loginQueueConcurrentPlayers={loginQueueConcurrentPlayers};  // Players processed concurrently during login");
            sb.AppendLine($"loginQueueMaxPlayers={loginQueueMaxPlayers};       // Max players in login queue");
            sb.AppendLine();
            sb.AppendLine($"instanceId = {instanceId};             // DayZ server instance id");
            sb.AppendLine();
            sb.AppendLine($"enableCfgGameplayFile = {enableCfgGameplayFile};      //Enable/Disable cfggamplay.json ");
            sb.AppendLine();
            sb.AppendLine($"storageAutoFix = {storageAutoFix};         // Checks persistence files and replaces corrupted ones");
            sb.AppendLine($"battleye = {battleye};");
            if (isOnline)
            {
                sb.AppendLine($"enviromentType = \"{envType}\";     // Internal / Stable / Experimental (online economy DB)");
            }
            sb.AppendLine();
            sb.AppendLine("class Missions");
            sb.AppendLine("{");
            sb.AppendLine("    class DayZ");
            sb.AppendLine("    {");
            sb.AppendLine($"        template=\"{template}\";");
            sb.AppendLine("        // Vanilla mission: dayzOffline.chernarusplus");
            sb.AppendLine("        // DLC mission: dayzOffline.enoch");
            sb.AppendLine("    };");
            sb.AppendLine("};");

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            // předvyber nově vytvořený .cfg do textboxu (včetně cesty)
            txtServerConfigFile.Text = fullPath;

            // pro jistotu přepočítat dostupnost online polí (když by se šablona změnila)
            UpdateOnlineFieldsAvailability();
        }

        // Enforce: CF -> COT -> Dabs -> Expansion-Core -> ostatní Expansion (needsCore pak noCore) -> ostatní s deps -> ostatní bez deps
        private static List<string> ReorderWithExpansionCore(
            List<string> current,
            Dictionary<string, ModDeps> perMod,
            Dictionary<string, List<string>> metaDeps)
        {
            string Norm(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                s = s.Trim();
                if (s.StartsWith("@")) s = s.Substring(1);
                s = s.Replace('_', '-').Replace(' ', '-');
                s = Regex.Replace(s, "-{2,}", "-");
                return s.ToLowerInvariant();
            }

            bool IsExpansion(string m) => Norm(m).StartsWith("dayz-expansion");

            bool HasAnyDeps(string mod)
            {
                bool hasMeta = metaDeps.TryGetValue(mod, out var md) && md != null && md.Any();
                bool hasCfg = perMod.TryGetValue(mod, out var pd) && pd != null && pd.RequiredPatches.Count > 0;
                return hasMeta || hasCfg;
            }

            // 1) Hlavičky v přesném pořadí
            var headsOrder = new[] { "@CF", "@Community-Online-Tools", "@Dabs Framework" };
            var heads = new List<string>();
            foreach (var h in headsOrder)
            {
                var found = current.FirstOrDefault(m =>
                    string.Equals(m, h, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Norm(m), Norm(h), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(found) && !heads.Any(x => string.Equals(x, found, StringComparison.OrdinalIgnoreCase)))
                    heads.Add(found);
            }

            var rest = current.Where(m => !heads.Any(h => string.Equals(h, m, StringComparison.OrdinalIgnoreCase))).ToList();

            // 2) Core
            var core = rest.FirstOrDefault(m => Norm(m) == "dayz-expansion-core");
            if (!string.IsNullOrEmpty(core))
                rest.Remove(core);

            // 3) Všechny Expansion mody (mimo Core)
            var expAll = rest.Where(IsExpansion).ToList();
            foreach (var m in expAll) rest.Remove(m);

            bool RequiresCore(string mod)
            {
                if (!perMod.TryGetValue(mod, out var md) || md == null) md = new ModDeps();
                metaDeps.TryGetValue(mod, out var mdeps);

                bool meta = (mdeps?.Any(x => Norm(x).Contains("dayz-expansion-core")) ?? false);
                bool cfg = md.RequiredPatches.Any(p =>
                    p.IndexOf("DayZExpansion_Core", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.IndexOf("Expansion_Core", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.IndexOf("DZ_Expansion_Core", StringComparison.OrdinalIgnoreCase) >= 0);

                return meta || cfg;
            }

            var expNeedsCore = expAll.Where(RequiresCore).ToList();
            var expNoCore = expAll.Except(expNeedsCore, StringComparer.OrdinalIgnoreCase).ToList();

            // 4) Ostatní mody – rozděl na „má závislosti“ vs „nemá závislosti“
            var othersWithDeps = rest.Where(HasAnyDeps).ToList();
            var othersNoDeps = rest.Except(othersWithDeps, StringComparer.OrdinalIgnoreCase).ToList();

            // 5) Finální pořadí
            var final = new List<string>();
            final.AddRange(heads);
            if (!string.IsNullOrEmpty(core)) final.Add(core);
            final.AddRange(expNeedsCore);
            final.AddRange(expNoCore);
            final.AddRange(othersWithDeps);
            final.AddRange(othersNoDeps);
            return final;
        }

        private static bool IsOfflineMission(string mission) =>
            !string.IsNullOrWhiteSpace(mission) &&
            mission.IndexOf("offline", StringComparison.OrdinalIgnoreCase) >= 0;

        // Online mise = cokoliv, co NENÍ offline
        private static bool IsOnlineMission(string mission) =>
            !string.IsNullOrWhiteSpace(mission) &&
            !IsOfflineMission(mission);

        private void cb_cfg_mpmission_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateOnlineFieldsAvailability();
        }

        // === Auto-copy DayZDiag_x64.exe for Experimental (Steam) ===
        private static string TryGetFileVersion(string path)
        {
            try
            {
                var f = FileVersionInfo.GetVersionInfo(path)?.FileVersion;
                return string.IsNullOrWhiteSpace(f) ? null : f;
            }
            catch { return null; }
        }

        private static bool NeedsCopyByVersionOrTime(string src, string dst)
        {
            if (!File.Exists(dst)) return true;

            // 1) zkus verze EXE
            var vSrc = TryGetFileVersion(src);
            var vDst = TryGetFileVersion(dst);
            if (vSrc != null && vDst != null && !string.Equals(vSrc, vDst, StringComparison.OrdinalIgnoreCase))
                return true;

            // 2) fallback: porovnej mtime
            try
            {
                var tSrc = File.GetLastWriteTimeUtc(src);
                var tDst = File.GetLastWriteTimeUtc(dst);
                return tSrc > tDst;
            }
            catch { return false; }
        }


        private void EnsureClientExecutablesOnServer()
        {
            try
            {
                var selectedVersion = cmbVersion.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(selectedVersion)) return;

                // 1) STEAM verze (Stable / Exp / Internal)
                if (string.Equals(selectedVersion, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(selectedVersion, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(selectedVersion, VersionItem_DayZInternalSteam, StringComparison.OrdinalIgnoreCase))
                {
                    string srcDir = GetSteamRoot(selectedVersion);
                    string dstDir = GetServerDirForSelectedVersion();
                    if (string.IsNullOrWhiteSpace(srcDir) || string.IsNullOrWhiteSpace(dstDir)) return;

                    // vždy zkusíme zkopírovat Diag i BE (a případně i vanilla klient)
                    CopyIfNewer(Path.Combine(srcDir, "DayZDiag_x64.exe"), Path.Combine(dstDir, "DayZDiag_x64.exe"));
                    CopyIfNewer(Path.Combine(srcDir, "DayZ_BE.exe"), Path.Combine(dstDir, "DayZ_BE.exe"));
                    // Volitelně i vanilla klient, hodí se pro fallback/spouštění přes BE:
                    // CopyIfNewer(Path.Combine(srcDir, "DayZ_x64.exe"),     Path.Combine(dstDir, "DayZ_x64.exe"));
                    return;
                }

                // 2) BDS verze (C:\BDS\<ver>\Client -> Server)
                // (kopírujeme stejné dvojice, ale z BDS klientské složky)
                string bdsRoot = Path.Combine(PathSettingsService.Current.BdsRoot, selectedVersion);
                string bdsClient = Path.Combine(bdsRoot, "Client");
                string bdsServer = Path.Combine(bdsRoot, "Server");

                if (!Directory.Exists(bdsClient) || !Directory.Exists(bdsServer)) return;

                CopyIfNewer(Path.Combine(bdsClient, "DayZDiag_x64.exe"), Path.Combine(bdsServer, "DayZDiag_x64.exe"));
                CopyIfNewer(Path.Combine(bdsClient, "DayZ_BE.exe"), Path.Combine(bdsServer, "DayZ_BE.exe"));
                // Volitelné:
                // CopyIfNewer(Path.Combine(bdsClient, "DayZ_x64.exe"),     Path.Combine(bdsServer, "DayZ_x64.exe"));
            }
            catch
            {
                // nech ticho – neblokovat UI
            }
        }



        private void cmbExeName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbExeName.SelectedItem is string s && s.StartsWith("────────"))
            {
                for (int i = cmbExeName.SelectedIndex + 1; i < cmbExeName.Items.Count; i++)
                {
                    if (cmbExeName.Items[i] is ExeChoice) { cmbExeName.SelectedIndex = i; break; }
                }
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnOpenInitFile_Click(object sender, EventArgs e)
        {
            string configPath = txtServerConfigFile.Text.Trim();
            string version = cmbVersion.SelectedItem?.ToString();
            string missionName = FindMissionNameInConfig(configPath);
            string missionDir = GetMissionDirectory(version, missionName);

            if (string.IsNullOrEmpty(missionDir) || !Directory.Exists(missionDir))
            {
                MessageBox.Show("Mission directory not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string initPath = Path.Combine(missionDir, "init.c");
            if (File.Exists(initPath))
            {
                var editor = PathSettingsService.Current.EditorExePath;
                try
                {
                    if (!string.IsNullOrWhiteSpace(editor) && File.Exists(editor))
                    {
                        Process.Start(new ProcessStartInfo(editor, $"\"{initPath}\"")
                        {
                            UseShellExecute = false,
                            WorkingDirectory = Path.GetDirectoryName(initPath) ?? Environment.CurrentDirectory
                        });
                    }
                    else
                    {
                        // Fallback: otevři výchozím asociovaným editorem
                        Process.Start(new ProcessStartInfo(initPath) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Cannot open editor.\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("init.c does not exist in the mission folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        private static string BdsVersionRoot(string version)
    => Path.Combine(PathSettingsService.Current.BdsRoot, version);

        private static string BdsServerDir(string version)
            => Path.Combine(BdsVersionRoot(version), "Server");

        private static string BdsClientDir(string version)
            => Path.Combine(BdsVersionRoot(version), "Client");


        private void btnBackupInit_Click(object sender, EventArgs e)
        {
            try
            {
                string configPath = txtServerConfigFile.Text.Trim();
                string version = cmbVersion.SelectedItem?.ToString();
                string missionName = FindMissionNameInConfig(configPath);
                string missionDir = GetMissionDirectory(version, missionName);

                if (string.IsNullOrEmpty(missionDir) || !Directory.Exists(missionDir))
                {
                    MessageBox.Show("Mission directory not found!");
                    return;
                }

                string initPath = Path.Combine(missionDir, "init.c");
                if (!File.Exists(initPath))
                {
                    MessageBox.Show("init.c not found in mission directory.");
                    return;
                }

                // Název zálohy: init_<mise>_YYYYMMDD_HHMMSS.zip (v téže složce mise)
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string safeMission = Regex.Replace(missionName ?? "mission", "[^A-Za-z0-9_-]+", "_");
                string zipInitPath = Path.Combine(missionDir, $"init_{safeMission}_{stamp}.zip");

                using (var zip = ZipFile.Open(zipInitPath, ZipArchiveMode.Create))
                {
                    // uvnitř archivu bude soubor pojmenovaný prostě init.c
                    zip.CreateEntryFromFile(initPath, "init.c", CompressionLevel.Optimal);
                }

                MessageBox.Show($"Init backup created:\n{zipInitPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating init backup: " + ex.Message);
            }
        }

        private void btnBrowseInit_Click(object sender, EventArgs e)
        {
            string configPath = txtServerConfigFile.Text.Trim();
            string version = cmbVersion.SelectedItem?.ToString();
            string missionName = FindMissionNameInConfig(configPath);
            string missionDir = GetMissionDirectory(version, missionName);

            if (string.IsNullOrEmpty(missionDir) || !Directory.Exists(missionDir))
            {
                MessageBox.Show("Mission directory not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string initPath = Path.Combine(missionDir, "init.c");

            try
            {
                if (File.Exists(initPath))
                {
                    // Otevři průzkumníka a označ init.c
                    Process.Start("explorer.exe", $"/select,\"{initPath}\"");
                }
                else
                {
                    // init.c není – otevři alespoň složku mise
                    Process.Start("explorer.exe", $"\"{missionDir}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open mission folder:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CopyIfNewer(string src, string dst)
        {
            try
            {
                if (!File.Exists(src)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                if (NeedsCopyByVersionOrTime(src, dst))
                    File.Copy(src, dst, overwrite: true);
            }
            catch { /* ticho, nechceme blokovat UI */ }
        }


        private void btnBrowseStorage_Click(object sender, EventArgs e)
        {
            string configPath = txtServerConfigFile.Text.Trim();
            string version = cmbVersion.SelectedItem?.ToString();
            string missionName = FindMissionNameInConfig(configPath);
            string missionDir = GetMissionDirectory(version, missionName);

            if (string.IsNullOrEmpty(missionDir) || !Directory.Exists(missionDir))
            {
                MessageBox.Show("Mission directory not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var storageDirs = Directory.GetDirectories(missionDir, "storage_*");

            if (storageDirs.Length == 0)
            {
                MessageBox.Show("No storage_* folder found in mission directory.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // otevři aspoň složku mise
                try { Process.Start("explorer.exe", $"\"{missionDir}\""); } catch { }
                return;
            }

            try
            {
                if (storageDirs.Length == 1)
                {
                    // rovnou otevři storage
                    Process.Start("explorer.exe", $"\"{storageDirs[0]}\"");
                }
                else
                {
                    // více storage_* – otevři root mise, ať si vybereš
                    MessageBox.Show(
                        "Multiple storage_* folders found. Opening the mission folder so you can pick one.",
                        "Choose storage",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    Process.Start("explorer.exe", $"\"{missionDir}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open storage folder:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}