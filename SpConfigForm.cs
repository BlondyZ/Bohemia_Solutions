using Bohemia_Solutions.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Bohemia_Solutions
{
    public partial class SpConfigForm : Form
    {
        // === KONSTANTY / CESTY – převzaté z MP (udržujeme konzistenci) ===
       


        private const string VersionItem_DayZSteam = "DayZ (Steam)";
        private const string VersionItem_DayZExpSteam = "DayZ Experimental (Steam)";
        private const string VersionItem_DayZInternal = "DayZ Internal (Steam)";



        // === SP "extraction" progress – stejné pole jako v MP ===
        private ProgressDialog _progressDlg;
        private readonly System.Windows.Forms.Timer progressUiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        private Stopwatch _extractStopwatch;
        private string _lastStatus = "Starting…";



        // kam uložit dočasné rozbalené configy (vezmi stejnou logiku jako v MP, klidně Path.GetTempPath)
        private static readonly string WorkTempModsRoot_SP =
            Path.Combine(Path.GetTempPath(), "DayZ_SP_ConfigExtract");



        private static bool IsSteamVersion(string versionName) =>
            string.Equals(versionName, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(versionName, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(versionName, VersionItem_DayZInternal, StringComparison.OrdinalIgnoreCase);

        private static string? GetSteamClientRoot(string versionName)
        {
            if (string.Equals(versionName, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase)) return PathSettingsService.Current.DayZStableDir;
            if (string.Equals(versionName, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase)) return PathSettingsService.Current.DayZExperimentalDir;
            if (string.Equals(versionName, VersionItem_DayZInternal, StringComparison.OrdinalIgnoreCase)) return PathSettingsService.Current.DayZInternalDir;
            return null;
        }
        private static string? GetSteamServerRoot(string versionName)
        {
            if (string.Equals(versionName, VersionItem_DayZSteam, StringComparison.OrdinalIgnoreCase)) return PathSettingsService.Current.DayZServerStableRoot;
            if (string.Equals(versionName, VersionItem_DayZExpSteam, StringComparison.OrdinalIgnoreCase)) return PathSettingsService.Current.DayZServerExpRoot;
            if (string.Equals(versionName, VersionItem_DayZInternal, StringComparison.OrdinalIgnoreCase)) return PathSettingsService.Current.DayZServerInternalRoot;
            return null;
        }

        // === DATA ===
        private SinglePlayerConfig _cfg;                  // musí být vždy nastaveno (fix NRE)
        private readonly List<string> _knownFilters;

        // Lock pro edit režim: když načítáme existující config, neumožníme měnit
        // verzi/typ/misi ani automaticky nepřepočítávat profily.
        private readonly bool _isEditMode;

        // === UI (filtry) ===
        private CheckedListBox clbFilters;
        private TextBox txtNewFilter;
        private Button btnAddFilter;

        // Výsledek dialogu (Form1 si ho čte po OK)
        public SinglePlayerConfig ResultConfig { get; private set; }



        // === Konstruktory – sjednocené ===
        public SpConfigForm(SinglePlayerConfig cfg, List<string>? knownFilters)
        {
            InitializeComponent();

            _cfg = cfg ?? new SinglePlayerConfig();
            ResultConfig = _cfg;
            // Edit mode: jakmile máme uloženou profilovou cestu (nebo misi), bereme to jako existující
            _isEditMode =
                !string.IsNullOrWhiteSpace(_cfg.ProfilesFolder)
                || !string.IsNullOrWhiteSpace(_cfg.MissionAbsPath);

            _knownFilters = (knownFilters ?? new List<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            // ==== původní navěšení eventů z druhého konstruktoru ====
            btnCancelSP.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };
            btnBrowseProfilesSP.Click += BtnBrowseProfilesSP_Click;
            cmbVersionSP.SelectedIndexChanged += CmbVersionSP_SelectedIndexChanged;
            cmbTypeSP.SelectedIndexChanged += CmbTypeSP_SelectedIndexChanged;
            btnEditClientParamsSP.Click += BtnEditClientParamsSP_Click;
            cb_chooseSP.SelectionChangeCommitted += Cb_chooseSP_SelectionChangeCommitted;
            btnBackupStorageSP.Click += BtnBackupStorageSP_Click;
            btnOpenStorageSP.Click += BtnOpenStorageSP_Click;

            btnBackupInitSP.Click += BtnBackupInitSP_Click;
            btnOpenInitFolderSP.Click += BtnOpenInitFolderSP_Click;
            btnEditInitSP.Click += BtnEditInitSP_Click;

            // ==== filtry ====
            BuildFiltersUi();
            PopulateFilters();

            progressUiTimer.Tick += (_, __) =>
            {
                if (_progressDlg != null && !_progressDlg.IsDisposed)
                    _progressDlg.SetStatus($"{_lastStatus}\nTime: {_extractStopwatch?.Elapsed:mm\\:ss}");
            };

        }

        // === DOPLNIT někam mezi metody ===
        private void InitTypeComboIfNeeded()
        {
            if (cmbTypeSP.Items.Count > 0) return;
            cmbTypeSP.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTypeSP.Items.Add("Vanilla");
            cmbTypeSP.Items.Add("Modded");
        }

        // unikátní složka pokud už existuje
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

        // POUŽÍVEJTE TOTO: skládá ClientDZ{verze}{typ}{mise}
        private string BuildAutoProfilesPathFromSettings()
        {
            var client = _cfg.ClientPath;
            if (string.IsNullOrWhiteSpace(client) || !Directory.Exists(client)) return "";

            var version = _cfg.VersionFolder ?? (cmbVersionSP.SelectedItem?.ToString() ?? "");
            var type = _cfg.Type ?? (cmbTypeSP.SelectedItem?.ToString() ?? "Vanilla");

            var missionParam = _cfg.MissionParam;
            if (string.IsNullOrWhiteSpace(missionParam) && cb_chooseSP.SelectedItem is MissionItem mi)
                missionParam = mi.Param ?? "";

            var last = (missionParam ?? "").Replace('/', '\\').Split('\\').LastOrDefault() ?? "";

            // ⬇️ KLÍČOVÉ: bez mise nevracej nic → žádné poloviční návrhy
            if (string.IsNullOrWhiteSpace(last)) return "";

            var root = Path.Combine(client, "profiles");
            var leaf = $"ClientDZ{SafeSegment(version)}{SafeSegment(type)}{SafeSegment(last)}";
            return Path.Combine(root, leaf);
        }


        // zeptá se, když chybí client\profiles
        private static bool EnsureClientProfilesRoot(string clientPath)
        {
            if (string.IsNullOrWhiteSpace(clientPath) || !Directory.Exists(clientPath)) return false;
            var root = Path.Combine(clientPath, "profiles");
            if (Directory.Exists(root)) return true;

           
            return false;
        }

        // navrhne/aktualizuje cestu v textboxu podle klienta a názvu konfigurace
        private void AutoSuggestProfilesPath(bool force = false)
        {
            var client = _cfg.ClientPath;
            if (string.IsNullOrWhiteSpace(client) || !Directory.Exists(client)) return;

            var current = (txtProfilesFolder.Text ?? "").Trim();
            var profilesRoot = Path.Combine(client, "profiles");

            bool shouldSet =
                force ||
                string.IsNullOrWhiteSpace(current) ||
                current.StartsWith(profilesRoot, StringComparison.OrdinalIgnoreCase);

            if (!shouldSet) return;

            var suggested = BuildAutoProfilesPathFromSettings();
            if (!string.IsNullOrWhiteSpace(suggested))
                txtProfilesFolder.Text = suggested;
        }

        // Mise bez CE (např. "empty.*") → storage nedává smysl
        private static bool IsMissionWithoutCE(SpConfigForm.MissionItem it)
        {
            var folder = Path.GetFileName(it.AbsPath) ?? "";
            return folder.StartsWith("empty", StringComparison.OrdinalIgnoreCase);
        }

        // Přepnutí stavu tlačítek pro storage podle vybrané mise
        private void UpdateStorageButtonsEnabled()
        {
            bool enable = true;
            if (cb_chooseSP.SelectedItem is MissionItem it)
                enable = !IsMissionWithoutCE(it);

            btnBackupStorageSP.Enabled = enable;
            btnOpenStorageSP.Enabled = enable;
            btnWipeStorageSP.Enabled = enable;
        }


        // ====== Form Load ======
        private void SpConfigForm_Load(object sender, EventArgs e)
        {
            // naplň typy (Vanilla/Modded)
            InitTypeComboSpIfNeeded();

            // Name + Type
            txtNameSP.Text = _cfg.Name ?? "";
            txtNameSP.TextChanged += (_, __) =>
            {
                if (!_isEditMode)      // ← v editaci nic nepřepočítávej
                    UpdateProfilesPathFromSelection(promptToCreate: false, forceText: false);
            };



            // vyber typ ze _cfg, jinak Vanilla
            var typeToSelect = string.IsNullOrWhiteSpace(_cfg.Type) ? "Vanilla" : _cfg.Type;
            int tix = cmbTypeSP.FindStringExact(typeToSelect);
            cmbTypeSP.SelectedIndex = tix >= 0 ? tix : 0;

            // při změně typu zkus znovu navrhnout profiles (nepřepisuj ručně zadanou cestu)
            cmbTypeSP.SelectedIndexChanged += (_, __) => AutoSuggestProfilesPath(force: false);

            // Verze
            FillVersions();
            if (!string.IsNullOrWhiteSpace(_cfg.VersionFolder))
            {
                int ix = cmbVersionSP.Items.IndexOf(_cfg.VersionFolder);
                if (ix >= 0) cmbVersionSP.SelectedIndex = ix;
            }

            // Profily – načti uloženou hodnotu (pokud je)
            txtProfilesFolder.Text = _cfg.ProfilesFolder ?? "";

            // Client flags (preview CLB – defaulty)
            InitClientFlagsIfNeeded();
            ApplyClientFlagsFromString(_cfg.ClientArguments);

            // Mods
            LoadWorkshopMods();

            // předvyplň In-game Name
            tb_ingame_name.Text = _cfg?.IngameName ?? string.Empty;

            // zaškrtni módy podle uložené konfigurace
            ApplyCheckedModsFromConfig(_cfg?.Mods);
            EnableModsUi();
            // In-game name z konfigurace
            tb_ingame_name.Text = _cfg.IngameName ?? "";

            // Před-check vybraných modů z konfigurace
            if (_cfg.Mods?.Count > 0)
            {
                var wanted = _cfg.Mods
                    .Select(m => Path.GetFileName(m))           // vezmi "@CF" z případné celé cesty
                    .Select(n => n.TrimStart('@'))              // porovnávej bez '@'
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < clbWorkshopModsSP.Items.Count; i++)
                {
                    var itemName = clbWorkshopModsSP.Items[i]?.ToString() ?? "";
                    var clean = itemName.TrimStart('@');
                    clbWorkshopModsSP.SetItemChecked(i, wanted.Contains(clean));
                }
            }
            // Exe + mise podle verze
            if (cmbVersionSP.SelectedIndex >= 0)
            {
                LoadExeNames();
                LoadMissions();
                SelectExeIfMatches(_cfg.ExeName);
                SelectMissionIfMatches(_cfg.MissionAbsPath);
            }

            // Po prvotním naplnění UI navrhni cestu podle aktuálního výběru
            UpdateProfilesPathFromSelection(promptToCreate: false, forceText: false);

            // Po prvotním naplnění UI navrhni cestu jen když nejsme v editaci
            if (!_isEditMode)
                UpdateProfilesPathFromSelection(promptToCreate: false, forceText: true);

            // EDIT MODE: disable přepínače, aby si uživatel neměnil základ konfigurace
            if (_isEditMode)
            {
                cmbVersionSP.Enabled = false;
                cmbTypeSP.Enabled = false;
                cb_chooseSP.Enabled = false;
            }

        }


        private void ApplyCheckedModsFromConfig(IReadOnlyList<string>? mods)
        {
            if (mods == null || clbWorkshopModsSP.Items.Count == 0) return;

            // normalizuj -> @název (case-insensitive)
            var wanted = new HashSet<string>(
                mods.Where(m => !string.IsNullOrWhiteSpace(m))
                    .Select(m =>
                    {
                        var s = m.Trim().Trim('"');
                        // pokud je uložená absolutní cesta, vezmeme jen @folder
                        if (System.IO.Path.IsPathRooted(s))
                            s = "@" + System.IO.Path.GetFileName(s.TrimEnd('\\', '/'));
                        if (!s.StartsWith("@")) s = "@" + s;
                        return s;
                    }),
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < clbWorkshopModsSP.Items.Count; i++)
            {
                var item = clbWorkshopModsSP.Items[i]?.ToString() ?? "";
                var key = item.StartsWith("@") ? item : "@" + item;
                clbWorkshopModsSP.SetItemChecked(i, wanted.Contains(key));
            }
        }


        /// Navrhne cestu k profilu podle (Client, Verze, Typ, Mise) a
        /// volitelně se zeptá, zda cílovou složku vytvořit.
        /// - Nikdy nic nevytváří bez potvrzení.
        /// - TextBox aktualizuje jen když je prázdný NEBO ukazuje pod <client>\profiles\...
        private void UpdateProfilesPathFromSelection(bool promptToCreate, bool forceText = false)
        {
            var client = _cfg.ClientPath;
            if (string.IsNullOrWhiteSpace(client) || !Directory.Exists(client)) return;

            var version = _cfg.VersionFolder ?? (cmbVersionSP.SelectedItem?.ToString() ?? "");
            var type = _cfg.Type ?? (cmbTypeSP.SelectedItem?.ToString() ?? "Vanilla");

            var missionParam = _cfg.MissionParam;
            if (string.IsNullOrWhiteSpace(missionParam) && cb_chooseSP.SelectedItem is MissionItem mi)
                missionParam = mi.Param ?? "";

            var root = Path.Combine(client, "profiles");
            var last = (missionParam ?? "").Replace('/', '\\').Split('\\').LastOrDefault() ?? "";
            var leaf = $"ClientDZ{SafeSegment(version)}{SafeSegment(type)}{SafeSegment(last)}";
            var suggested = Path.Combine(root, leaf); // bez MakeUniqueDirectory

            var current = (txtProfilesFolder.Text ?? "").Trim();

            // ← KLÍČOVÁ ZMĚNA: navrhuj jen když je prázdné, nebo když to výslovně vynutím
            if (forceText || string.IsNullOrWhiteSpace(current))
                txtProfilesFolder.Text = suggested;

            // část „promptToCreate“ úplně vypusť/ignoruj (vytváříme jen při Save)
        }




        private static string SafeSegment(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var cleaned = Regex.Replace(s, "[^A-Za-z0-9]+", "_");
            cleaned = Regex.Replace(cleaned, "_{2,}", "_").Trim('_');
            return cleaned;
        }


        private string EnsureClientProfilesRootExistsOrAsk(string clientDir)
        {
            if (string.IsNullOrWhiteSpace(clientDir)) return null;

            string profilesRoot = Path.Combine(clientDir, "profiles");
            if (!Directory.Exists(profilesRoot))
            {
                var res = MessageBox.Show(
                    $"Client 'profiles' folder was not found:\n{profilesRoot}\n\nCreate it now?",
                    "Create profiles folder",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    try { Directory.CreateDirectory(profilesRoot); }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to create profiles folder:\n" + ex.Message);
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            return profilesRoot;
        }

        private string BuildAutoProfilesFolderPathForSP(string clientDir, string version, string type, string missionParam)
        {
            if (string.IsNullOrWhiteSpace(clientDir) || !Directory.Exists(clientDir)) return null;

            string root = Path.Combine(clientDir, "profiles"); // jen sestavit, nic netvořit
            string missionName = missionParam ?? "";
            var last = missionName.Replace('/', '\\').Split('\\').LastOrDefault() ?? missionName;

            string name = $"ClientDZ{SafeSegment(version)}{SafeSegment(type)}{SafeSegment(last)}";
            string target = Path.Combine(root, name);
            return target; // ← žádné MakeUniqueDirectory
        }



        // pole si klidně nech stejné (clbFilters, txtNewFilter, btnAddFilter, případně grpFilters)
        private GroupBox grpFilters;
        // ====== UI filtrů ======
        private void BuildFiltersUi()
        {
            // rodič = stejný container jako levý sloupec
            var parent = txtNameSP?.Parent ?? this;

            // vypočítej levý sloupec: zarovnáme na Name textbox a šířku vezmeme po SP mission combobox
            int left = txtNameSP.Left;
            int right = cb_chooseSP.Right;                // konec levého sloupce
            int top = btnEditInitSP.Bottom + 12;        // pod tlačítky "Init file"
            int width = Math.Max(300, right - left);
            int height = 180;

            grpFilters = new GroupBox
            {
                Text = "Filters / Groups",
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            clbFilters = new CheckedListBox
            {
                CheckOnClick = true,
                IntegralHeight = false,
                Left = 10,
                Top = 20,
                Width = grpFilters.ClientSize.Width - 20,
                Height = 110,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            txtNewFilter = new TextBox
            {
                Left = 10,
                Top = clbFilters.Bottom + 8,
                Width = grpFilters.ClientSize.Width - 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnAddFilter = new Button
            {
                Text = "Add",
                Top = txtNewFilter.Top - 2,
                Width = 80,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAddFilter.Left = grpFilters.ClientSize.Width - btnAddFilter.Width - 10;
            btnAddFilter.Click += (_, __) => AddNewFilterFromTextbox();

            grpFilters.Controls.Add(clbFilters);
            grpFilters.Controls.Add(txtNewFilter);
            grpFilters.Controls.Add(btnAddFilter);

            parent.Controls.Add(grpFilters);
            grpFilters.BringToFront(); // kdyby tam byl jiný overlay/kontejner
        }

        private void PopulateFilters()
        {
            clbFilters.Items.Clear();
            foreach (var f in _knownFilters)
            {
                bool check = _cfg.Filters?.Any(x => x.Equals(f, StringComparison.OrdinalIgnoreCase)) == true;
                clbFilters.Items.Add(f, check);
            }
        }

        private void AddNewFilterFromTextbox()
        {
            var name = (txtNewFilter.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            int idx = IndexOfFilter(name);
            if (idx < 0)
                clbFilters.Items.Add(name, true);
            else
                clbFilters.SetItemChecked(idx, true);

            txtNewFilter.Clear();
        }

        private int IndexOfFilter(string name)
        {
            for (int i = 0; i < clbFilters.Items.Count; i++)
                if (string.Equals(clbFilters.Items[i]?.ToString(), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        // uloží výběr filtrů do ResultConfig/_cfg
        private void PersistToResult()
        {
            ResultConfig.Filters = clbFilters.CheckedItems.Cast<object>()
                .Select(o => o?.ToString() ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ====== Versions ======
        private void FillVersions()
        {
            cmbVersionSP.Items.Clear();

            // BDS verze
            string bdsPath = PathSettingsService.Current.BdsRoot;
            if (Directory.Exists(bdsPath))
            {
                var versions = Directory.GetDirectories(bdsPath)
                    .Select(Path.GetFileName)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                cmbVersionSP.Items.AddRange(versions);
            }

            // Steam položky jako "verze"
            cmbVersionSP.Items.Add(VersionItem_DayZSteam);
            cmbVersionSP.Items.Add(VersionItem_DayZExpSteam);
            cmbVersionSP.Items.Add(VersionItem_DayZInternal);
        }

        private void CmbVersionSP_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isEditMode) return;   // ← nic nepřepočítávej ani nezasahuj do _cfg
            var v = cmbVersionSP.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(v)) return;



            _cfg.VersionFolder = v;

            if (IsSteamVersion(v))
            {
                _cfg.ClientPath = GetSteamClientRoot(v) ?? "";
                _cfg.ServerPath = GetSteamServerRoot(v) ?? "";
            }
            else
            {
                var bds = PathSettingsService.Current.BdsRoot;
                _cfg.ClientPath = Path.Combine(bds, v, "Client");
                _cfg.ServerPath = Path.Combine(bds, v, "Server");
            }

            UpdateProfilesPathFromSelection(promptToCreate: false, forceText: false);


            LoadExeNames();
            LoadMissions();
            UpdateStorageButtonsEnabled(); // ⬅️ pro jistotu po přegenerování seznamu
        }

        // ====== EXE ======
        private void LoadExeNames()
        {
            cmbExeName.Items.Clear();
            if (string.IsNullOrWhiteSpace(_cfg.ClientPath) || !Directory.Exists(_cfg.ClientPath))
                return;

            var list = Directory.GetFiles(_cfg.ClientPath, "*.exe", SearchOption.TopDirectoryOnly)
                                .Select(Path.GetFileName)
                                .Where(n => n.EndsWith("_x64.exe", StringComparison.OrdinalIgnoreCase) ||
                                            n.IndexOf("DayZ", StringComparison.OrdinalIgnoreCase) >= 0)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                                .ToArray();
            if (list.Length == 0) list = new[] { "DayZ_x64.exe" };

            cmbExeName.Items.AddRange(list);
            cmbExeName.SelectedIndex = 0;
        }

        private void SelectExeIfMatches(string exeName)
        {
            if (string.IsNullOrWhiteSpace(exeName)) return;
            int ix = -1;
            for (int i = 0; i < cmbExeName.Items.Count; i++)
                if (string.Equals(cmbExeName.Items[i]?.ToString(), exeName, StringComparison.OrdinalIgnoreCase))
                { ix = i; break; }
            if (ix >= 0) cmbExeName.SelectedIndex = ix;
        }

        // ====== Profiles ======
        private void BtnBrowseProfilesSP_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog { Description = "Choose client profiles folder" };

            // Preferuj <client>\profiles (pokud existuje), jinak client root
            if (!string.IsNullOrEmpty(_cfg.ClientPath) && Directory.Exists(_cfg.ClientPath))
            {
                var root = Path.Combine(_cfg.ClientPath, "profiles");
                dlg.SelectedPath = Directory.Exists(root) ? root : _cfg.ClientPath;
            }

            if (dlg.ShowDialog(this) == DialogResult.OK)
                txtProfilesFolder.Text = dlg.SelectedPath;
        }

        private bool ConfirmCreateFolderIfMissing(string path, string title = "Create profiles folder?")
        {
            if (Directory.Exists(path)) return true;

            var res = MessageBox.Show(
                $"The profiles folder does not exist:\n{path}\n\nCreate it now?",
                title,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel) return false;        // zrušíme save
            if (res == DialogResult.No) return true;            // nepřipravíme ji, ale save povolíme
                                                                // (BAT může ukazovat na neexistující složku – vědomé rozhodnutí)
            try { Directory.CreateDirectory(path); return true; }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to create the folder:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        // ====== Missions ======
        private sealed class MissionItem
        {
            public string Label { get; set; } = "";
            public string AbsPath { get; set; } = "";
            public string Param { get; set; } = ""; // missions\xxx | mpmissions\xxx
            public override string ToString() => Label;
        }

        private void LoadMissions()
        {
            cb_chooseSP.Items.Clear();
            if (string.IsNullOrWhiteSpace(_cfg.ClientPath)) return;

            var items = new List<MissionItem>();

            // ✅ Pouze Client\missions
            var cliMissions = Path.Combine(_cfg.ClientPath, "missions");
            if (Directory.Exists(cliMissions))
            {
                foreach (var dir in Directory.GetDirectories(cliMissions))
                {
                    var name = Path.GetFileName(dir);
                    items.Add(new MissionItem
                    {
                        Label = $"Client: missions\\{name}",
                        AbsPath = dir,
                        Param = $"missions\\{name}"
                    });
                }
            }

            foreach (var it in items.OrderBy(i => i.Label, StringComparer.OrdinalIgnoreCase))
                cb_chooseSP.Items.Add(it);

            if (cb_chooseSP.Items.Count > 0)
            {
                cb_chooseSP.SelectedIndex = 0;
                UpdateStorageButtonsEnabled();  // ⬅️ nově
            }
        }

        private void SelectMissionIfMatches(string absPath)
        {
            if (string.IsNullOrWhiteSpace(absPath)) return;
            for (int i = 0; i < cb_chooseSP.Items.Count; i++)
            {
                if (cb_chooseSP.Items[i] is MissionItem it && string.Equals(it.AbsPath, absPath, StringComparison.OrdinalIgnoreCase))
                {
                    cb_chooseSP.SelectedIndex = i;
                    UpdateStorageButtonsEnabled(); // ⬅️ nově
                    break;
                }
            }
        }

        private static void CopyDirectoryRecursive(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(src, dst));
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(src, dst), true);
        }

        private void Cb_chooseSP_SelectionChangeCommitted(object? sender, EventArgs e)
        {
            if (_isEditMode) return;
            if (cb_chooseSP.SelectedItem is not MissionItem it) return;

            _cfg.MissionAbsPath = it.AbsPath;
            _cfg.MissionParam = it.Param;

            // ⬇️ Přepiš textbox i když už něco obsahuje
            UpdateProfilesPathFromSelection(promptToCreate: false, forceText: true);


            // pokud ještě není vyplněno, nebo směřuje mimo Client\profiles, nabídni auto-generaci
            if (string.IsNullOrWhiteSpace(txtProfilesFolder.Text))
            {
                var path = BuildAutoProfilesFolderPathForSP(_cfg.ClientPath, _cfg.VersionFolder ?? "", _cfg.Type ?? "Vanilla", _cfg.MissionParam ?? "");
                if (!string.IsNullOrEmpty(path)) txtProfilesFolder.Text = path;
            }
            UpdateStorageButtonsEnabled();
        }

        // ====== Client parameters ======
        private static readonly string[] DefaultClientFlags = new[] { "-window", "-nopause", "-disableCrashReport", "-nolauncher" };

        private void InitClientFlagsIfNeeded()
        {
            if (clbClientFlagsSp.Items.Count == 0)
                clbClientFlagsSp.Items.AddRange(DefaultClientFlags);
        }

        private void ApplyClientFlagsFromString(string args)
        {
            var selected = new HashSet<string>(
                (args ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            bool hadAny = selected.Count > 0;
            for (int i = 0; i < clbClientFlagsSp.Items.Count; i++)
            {
                var flag = clbClientFlagsSp.Items[i]?.ToString() ?? "";
                bool check = hadAny ? selected.Contains(flag)
                                    : DefaultClientFlags.Contains(flag, StringComparer.OrdinalIgnoreCase);
                clbClientFlagsSp.SetItemChecked(i, check);
            }
        }

        private string BuildClientArgsFromCLB()
        {
            var list = new List<string>();
            for (int i = 0; i < clbClientFlagsSp.Items.Count; i++)
                if (clbClientFlagsSp.GetItemChecked(i))
                    list.Add(clbClientFlagsSp.Items[i]?.ToString() ?? "");
            return string.Join(" ", list.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private void BtnEditClientParamsSP_Click(object? sender, EventArgs e)
        {
            var rows = clbClientFlagsSp.Items.Cast<object>()
                .Select((it, i) => new ParamsEditorForm.Row
                {
                    Parameter = it?.ToString() ?? "",
                    Enabled = clbClientFlagsSp.GetItemChecked(i)
                })
                .ToList();

            using var dlg = new ParamsEditorForm(rows, "Edit Client Parameters");
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var data = (dlg.Result ?? new List<ParamsEditorForm.Row>())
                    .Select(r => (val: (r.Parameter ?? "").Trim(), en: r.Enabled))
                    .Where(x => !string.IsNullOrWhiteSpace(x.val))
                    .ToList();

                clbClientFlagsSp.BeginUpdate();
                clbClientFlagsSp.Items.Clear();
                foreach (var p in data)
                {
                    int idx = clbClientFlagsSp.Items.Add(p.val);
                    clbClientFlagsSp.SetItemChecked(idx, p.en);
                }
                clbClientFlagsSp.EndUpdate();
            }
        }

        // ====== Mods ======
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

            clbWorkshopModsSP.Items.Clear();
            foreach (var n in names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                clbWorkshopModsSP.Items.Add(n);

            lblWorkshopStatusSP.Text = names.Count == 0
                ? "Workshop folders not found / empty."
                : "!Workshop / !LocalWorkshop OK";
        }

        private void CmbTypeSP_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isEditMode) return;   // typ nelze změnit při editaci
            EnableModsUi();
            _cfg.Type = cmbTypeSP.SelectedItem?.ToString() ?? "Vanilla";
            UpdateProfilesPathFromSelection(promptToCreate: false, forceText: false);
        }


        private void EnableModsUi()
        {
            bool modded = string.Equals(cmbTypeSP.SelectedItem?.ToString(), "Modded", StringComparison.OrdinalIgnoreCase);
            clbWorkshopModsSP.Enabled = modded;
            clbWorkshopModsSP.BackColor = modded ? SystemColors.Window : SystemColors.Control;
            lblWorkshopStatusSP.Enabled = modded;
        }

        private static string ResolveModFolder(string modName)
        {
            var local = Path.Combine(PathSettingsService.Current.LocalWorkshopRoot, modName);
            if (Directory.Exists(local)) return local;
            return Path.Combine(PathSettingsService.Current.WorkshopRoot, modName);
        }

        // ====== Storage / Init – jednoduché akce ======
        private string? GetStoragePath()
        {
            // preferuj složku v misi (tam ji máš)
            var candidates = new[] { _cfg.MissionAbsPath, txtProfilesFolder.Text, _cfg.ClientPath };
            foreach (var root in candidates)
            {
                var found = FindStorageDirUnder(root);
                if (found != null) return found;
            }
            return null;
        }


        private void BtnOpenStorageSP_Click(object? sender, EventArgs e)
        {
            var p = GetStoragePath();
            if (string.IsNullOrEmpty(p)) { MessageBox.Show("Storage folder not found."); return; }
            Process.Start("explorer.exe", p);
        }

        private void BtnBackupStorageSP_Click(object? sender, EventArgs e)
        {
            var p = GetStoragePath();
            if (string.IsNullOrEmpty(p)) { MessageBox.Show("Storage folder not found."); return; }

            var backupRoot = Path.Combine(_cfg.ClientPath, "_Backups", "Storage");
            Directory.CreateDirectory(backupRoot);
            var dst = Path.Combine(backupRoot, $"{SafeFile(_cfg.Name)}_{DateTime.Now:yyyyMMdd_HHmmss}");
            CopyDirectoryRecursive(p, dst);
            MessageBox.Show($"Storage backed up to:\n{dst}");
        }


        private string GetInitPath() => Path.Combine(_cfg.MissionAbsPath ?? "", "init.c");

        private void BtnOpenInitFolderSP_Click(object? sender, EventArgs e)
        {
            var dir = _cfg.MissionAbsPath;
            if (!Directory.Exists(dir)) { MessageBox.Show("Mission folder not found."); return; }
            Process.Start("explorer.exe", dir);
        }

        private void BtnBackupInitSP_Click(object? sender, EventArgs e)
        {
            var init = GetInitPath();
            if (!File.Exists(init)) { MessageBox.Show("init.c not found."); return; }

            var backupRoot = Path.Combine(_cfg.ClientPath, "_Backups", "Init");
            Directory.CreateDirectory(backupRoot);
            var dst = Path.Combine(backupRoot, $"init_{SafeFile(_cfg.Name)}_{DateTime.Now:yyyyMMdd_HHmmss}.c");
            File.Copy(init, dst, true);
            MessageBox.Show($"init.c backed up to:\n{dst}");
        }

        private void BtnEditInitSP_Click(object? sender, EventArgs e)
        {
            var init = GetInitPath();
            if (!File.Exists(init)) { MessageBox.Show("init.c not found."); return; }
            var editor = PathSettingsService.Current.EditorExePath;
            try
            {
                if (!string.IsNullOrWhiteSpace(editor) && File.Exists(editor))
                {
                    Process.Start(new ProcessStartInfo(editor, $"\"{init}\"")
                    {
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(init) ?? Environment.CurrentDirectory
                    });
                }
                else
                {
                    // Fallback: výchozí asociovaná aplikace
                    Process.Start(new ProcessStartInfo(init) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Cannot open editor.\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private static string SafeFile(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            s = Regex.Replace(s, "_{2,}", "_").Trim('_');
            return string.IsNullOrWhiteSpace(s) ? "SP" : s;
        }

        private static string BuildBatContent(SinglePlayerConfig c)
        {
            // exe voláme jen jménem; BAT spouštěj z Client\_SP_BATS → CD do parentu
            var sb = new StringBuilder();
            sb.AppendLine($"@echo off");
            sb.AppendLine($"cd /d \"{c.ClientPath}\"");

            var args = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(c.ClientArguments))
                args.Append(" " + c.ClientArguments.Trim());

            if (!string.IsNullOrWhiteSpace(c.ProfilesFolder) && Directory.Exists(c.ProfilesFolder))
                args.Append($" -profiles=\"{c.ProfilesFolder}\"");


            if (!string.IsNullOrWhiteSpace(c.MissionParam))
                args.Append($" -mission={c.MissionParam}");

            if (string.Equals(c.Type, "Modded", StringComparison.OrdinalIgnoreCase) &&
                c.Mods != null && c.Mods.Count > 0)
            {
                args.Append(" -mod=" + string.Join(";", c.Mods.Select(m => $"\"{m}\"")));
            }

            sb.Append($"{c.ExeName}{args}");
            sb.AppendLine();
            return sb.ToString();
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

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

        private static string ResolveModRoot(string modName)
        {
            return Directory.Exists(Path.Combine(PathSettingsService.Current.LocalWorkshopRoot, modName))
                ? PathSettingsService.Current.LocalWorkshopRoot
                : PathSettingsService.Current.WorkshopRoot;
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


        private async void btnSaveSpConfig_Click(object? sender, EventArgs e)
        {
            // === 1) Sesbírej UI do _cfg ===
           
            _cfg.Name = (txtNameSP.Text ?? "").Trim();
            _cfg.Type = cmbTypeSP.SelectedItem?.ToString() ?? "Vanilla";
            _cfg.VersionFolder = cmbVersionSP.SelectedItem?.ToString() ?? _cfg.VersionFolder;
            _cfg.ExeName = cmbExeName.SelectedItem?.ToString() ?? "DayZ_x64.exe";
            _cfg.IngameName = (tb_ingame_name.Text ?? "").Trim();

            // Ujisti se, že máme missionParam i v edit modu
            if (string.IsNullOrWhiteSpace(_cfg.MissionParam))
            {
                var mp = GetCurrentMissionParam();
                if (!string.IsNullOrWhiteSpace(mp))
                    _cfg.MissionParam = mp;
            }

            // Mise – pokud ještě není nastaveno (jinak se nastavuje v SelectionChangeCommitted)
            if (string.IsNullOrWhiteSpace(_cfg.MissionAbsPath) && cb_chooseSP.SelectedItem is MissionItem it)
            {
                _cfg.MissionAbsPath = it.AbsPath;
                _cfg.MissionParam = it.Param;
            }

            // === 2) Profily – rozhodni finální cestu a případně se zeptej na vytvoření ===
            string profiles = (txtProfilesFolder.Text ?? "").Trim();

            // Když je prázdné, dopočítej návrh jako u MP (ale do Client\profiles)
            if (string.IsNullOrWhiteSpace(profiles) && Directory.Exists(_cfg.ClientPath))
            {
                profiles = BuildAutoProfilesFolderPathForSP(
                 _cfg.ClientPath,
                 _cfg.VersionFolder ?? "",
                 _cfg.Type ?? "Vanilla",
                 GetCurrentMissionParam()); // ← místo _cfg.MissionParam

            }

            // Pokud máme kandidáta, ověř existenci a případně se zeptej na vytvoření
            if (!string.IsNullOrWhiteSpace(profiles))
            {
                if (!ConfirmCreateFolderIfMissing(profiles, "Create profiles folder?")) return; // Cancel nebo error → ukonči Save
                txtProfilesFolder.Text = profiles; // může být nově vytvořeno nebo už existovalo
            }

            _cfg.ProfilesFolder = (txtProfilesFolder.Text ?? "").Trim();

            // === 3) Client flags ===
            _cfg.ClientArguments = BuildClientArgsFromCLB();

            // === 4) Mods & dependency check (jen když Modded) ===
            _cfg.Mods.Clear();

            if (string.Equals(_cfg.Type, "Modded", StringComparison.OrdinalIgnoreCase))
            {
                // vybrané módy z pravého CheckListBoxu (bereme aliasy @Name – stejné jako v MP)
                var selectedMods = clbWorkshopModsSP.CheckedItems
                    .Cast<object>()
                    .Select(o => (o?.ToString() ?? "").Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.StartsWith("@") ? s : "@" + s)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (selectedMods.Count == 0)
                {
                    MessageBox.Show("No mods selected.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // === extraction + progress (stejně jako MP) ===
                var sessionRoot = Path.Combine(WorkTempModsRoot_SP, "sp_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(sessionRoot);

                var logSb = new StringBuilder();

                _progressDlg = new ProgressDialog();
                _progressDlg.StartPosition = FormStartPosition.CenterScreen;
                _progressDlg.SetStatus("Starting…\nTime: 00:00");
                _progressDlg.SetProgress(0);
                _progressDlg.Show(this);

                _extractStopwatch = Stopwatch.StartNew();
                progressUiTimer.Start();
                // Ověření Mikera z nastavení
                var mikeroPath = PathSettingsService.Current.MikeroExtractPbo;
                if (string.IsNullOrWhiteSpace(mikeroPath) || !File.Exists(mikeroPath))
                {
                    MessageBox.Show(this,
                        "Mikero ExtractPbo.exe path is not set or invalid.\nOpen Paths setup and fix it.",
                        "Mikero", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                for (int i = 0; i < selectedMods.Count; i++)
                {
                    string modName = selectedMods[i];
                    _lastStatus = $"[{i + 1}/{selectedMods.Count}] {modName} – extracting config.cpp…";

                    // ⬇⬇⬇ STEJNÁ FUNKCE jako v MP
                    await ExtractOnlyConfigsForModAsync(
                        modName,
                        sessionRoot,
                        s => _lastStatus = s,
                        logSb
                    );

                    _progressDlg.SetProgress((int)Math.Round(((i + 1) * 100.0) / selectedMods.Count));
                }

                progressUiTimer.Stop();
                _extractStopwatch?.Stop();
                if (_progressDlg != null)
                {
                    _progressDlg.Done($"Done. Total time: {_extractStopwatch.Elapsed:mm\\:ss}");
                    _progressDlg.Close();
                    _progressDlg = null;
                }

                // ulož log (stejně jako MP)
                File.WriteAllText(Path.Combine(sessionRoot, "configs_session.log"), logSb.ToString(), Encoding.UTF8);

                // === analýza + finální pořadí (stejné jako MP) ===
                var (sorted, missing, cycles) = AnalyzeConfigsAndSort(sessionRoot, selectedMods);

                // odfiltruj "missing" systémové DZ_* (stejně jako MP)
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
                    foreach (var m in missingNoDz) err.AppendLine(" - " + m);

                    MessageBox.Show(err.ToString(), "Missing dependencies", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // neukládat
                }

                // informativní rekapitulace (stejně jako MP)
                var rep = new StringBuilder();
                rep.AppendLine("Mods will be saved in this order:");
                rep.AppendLine();
                rep.AppendLine(string.Join("\n", sorted));
                if (cycles.Count > 0)
                {
                    /* volitelné varování na cykly; v MP máš zakomentováno */
                }
                MessageBox.Show(rep.ToString(), "Final Mod Order (will be saved)", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // => sem ulož finální pořadí
                _cfg.Mods = sorted;
            }
            else
            {
                // Vanilla → bez modů
                _cfg.Mods.Clear();
            }


            // === 5) Ulož filtry ===
            PersistToResult();

            // === 6) Vygeneruj BAT ===
            try
            {
                var outRoot = Path.Combine(_cfg.ClientPath, "_SP_BATS");
                Directory.CreateDirectory(outRoot);
                var outBat = Path.Combine(outRoot, $"{SafeFile(_cfg.Name)}.bat");
                File.WriteAllText(outBat, BuildBatContent(_cfg), Encoding.UTF8);

                ResultConfig = _cfg;
                _cfg.Updated = DateTime.Now;

                /*MessageBox.Show($"Saved.\nBAT generated:\n{outBat}", "OK",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);*/
              
                

                // (volitelně) přepiš ResultConfig, pokud používáš jiný instance-holder:
                ResultConfig = _cfg;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while generating BAT:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void InitTypeComboSpIfNeeded()
        {
            if (cmbTypeSP.Items.Count == 0)
            {
                cmbTypeSP.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbTypeSP.Items.Add("Vanilla");
                cmbTypeSP.Items.Add("Modded");
            }
        }


        private static string? FindStorageDirUnder(string? baseDir)
        {
            if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir))
                return null;

            // 1) přesně "storage"
            var exact = Path.Combine(baseDir, "storage");
            if (Directory.Exists(exact)) return exact;

            // 2) cokoliv "storage*"
            var hit = Directory.GetDirectories(baseDir, "storage*", SearchOption.TopDirectoryOnly)
                               .OrderByDescending(d => new DirectoryInfo(d).LastWriteTimeUtc)
                               .FirstOrDefault();
            return hit; // může být null
        }

        private string GetCurrentMissionParam()
        {
            // 1) Pokud už je v _cfg
            if (!string.IsNullOrWhiteSpace(_cfg.MissionParam))
                return _cfg.MissionParam;

            // 2) Pokud je něco vybrané v combu
            if (cb_chooseSP.SelectedItem is MissionItem mi && !string.IsNullOrWhiteSpace(mi.Param))
                return mi.Param;

            // 3) Zkus odvodit z uložené absolutní cesty mise
            if (!string.IsNullOrWhiteSpace(_cfg.MissionAbsPath))
            {
                var leaf = Path.GetFileName(_cfg.MissionAbsPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(leaf))
                    return $@"missions\{leaf}";
            }

            return "";
        }


        private void btnWipeStorageSP_Click(object? sender, EventArgs e)
        {
            var p = GetStoragePath();
            if (string.IsNullOrEmpty(p)) { MessageBox.Show("Storage folder not found."); return; }
            if (MessageBox.Show("Really wipe storage?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Directory.Delete(p, true);
                Directory.CreateDirectory(p);
                MessageBox.Show("Storage wiped.");
            }
        }

    }
}
