using Bohemia_Solutions.Models;
using Bohemia_Solutions.Services;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1.Models;
using static Bohemia_Solutions.ParamsEditorForm;
using Timer = System.Windows.Forms.Timer;
using System.IO;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;


namespace Bohemia_Solutions
{
    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();
            // (volitelně) si jednorázově vytvoř flag u sebe v debug buildu:
#if DEBUG
            if (!File.Exists(Bohemia_Solutions.Paths.UserAdminFlag))
                File.WriteAllText(Bohemia_Solutions.Paths.UserAdminFlag, "PH admin");
#endif

            tsbEditChangeLog.Visible = File.Exists(Bohemia_Solutions.Paths.UserAdminFlag);


            EnsureChangelogIsUpToDate();  // ← přenese changelog z release do uživatelského configu
            // TabControl vyplní okno (bez potřeby OnResize hacku)
            tbControllAll.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // --- SINGLE-PLAYER layout (stejně jako u MP) ---
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            panel_SP_info.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; // ← změna

            // obsah levého panelu SP
            listViewConfigsSP.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btn_add_sp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btn_edit_sp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btn_remove_sp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            InitFooter();
            UpdateFooterVersionFromJson(); // ← doplněné načtení verze z JSON
            _shortcutButtons = new[]
            {
                pbButton1, pbButton2, pbButton3, pbButton4, pbButton5,
                pbButton6, pbButton7, pbButton8, pbButton9, pbButton10
            };

            WireShortcutButtons();
            ApplyShortcutsToButtons();
            pnl_Server_Info.AutoScroll = true;

            foreach (Control c in pnl_Server_Info.Controls)
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; // NE Bottom

            pnl_Server_Info.Resize += (s, e) =>
            {
                int usable = pnl_Server_Info.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8;
                foreach (Control c in pnl_Server_Info.Controls)
                    c.Width = Math.Max(c.MinimumSize.Width, usable);
            };

            // a) DPI a minimální velikost okna
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.MinimumSize = new Size(1100, 800);   // klidně uprav podle sebe


            // b) Levý panel (se seznamem konfigurací) nech fixní na šířku, ale ať roste na výšku
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

            // c) Pravý panel (info/server tools) ať se roztahuje se zbytkem okna
            pnl_Server_Info.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // d) ListView ať vyplní svůj panel (aby vidět vždy vše)
            listViewConfigs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // e) Tlačítka dole v levém panelu ať „drží“ u spodního okraje
            btn_add.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btn_edit.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btn_remove.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            // f) Bezpečnost: když bude okno moc malé – přidej scrollbary do panelů
            panel1.AutoScroll = true;
            pnl_Server_Info.AutoScroll = true;
            // SP pravý panel – stejné chování jako MP info panel
            panel_SP_info.AutoScroll = true;
            panel_SP_info.Resize -= PanelSpInfo_Resize;   // pojistka proti duplicitě
            panel_SP_info.Resize += PanelSpInfo_Resize;
            // --- Single-player inicializace ---
            InitSpListView();
            LoadSpConfigsFromJson();
            RefreshSpListView();
            BuildSpFilterBarUI();



            // Dvojklik na řádek = Edit
            listViewConfigsSP.DoubleClick += btn_edit_sp_Click;
            listViewConfigs.DoubleClick += btn_edit_Click;

        }


        private void PanelSpInfo_Resize(object? sender, EventArgs e)
        {
            // DisplayRectangle odečítá scrollbary; díky tomu pasuje pravý okraj
            int usable = panel_SP_info.DisplayRectangle.Width - 8; // malý vnitřní offset

            foreach (Control c in panel_SP_info.Controls)
                c.Width = Math.Max(c.MinimumSize.Width, usable);
        }



        private volatile bool _appClosing = false;

        private readonly ToolTip ipTip = new ToolTip();
        private readonly string configsFilePath = "configs.json"; // můžeš změnit cestu
        private List<DayZConfig> configs = new();
        private System.Windows.Forms.Timer serverStatusTimer = new System.Windows.Forms.Timer();
        // --- Crash logs auto-refresh ---
        private readonly System.Windows.Forms.Timer crashLogsTimer = new System.Windows.Forms.Timer();
        private string _crashSigMp = "";
        private string _crashSigSp = "";


        // --- Filters UI ---
        private ComboBox cmbConfigFilter;
        private Button btnCreateFilter;
        private Button btnManageFilters;   // ⬅️ PŘIDAT
        private string _currentFilter = AllFiltersItem; // "All" na startu
        private const string AllFiltersItem = "All";
        private ListViewGroup grpVanillaOnline;
        private ListViewGroup grpVanillaOffline;
        private ListViewGroup grpModdedOnline;
        private ListViewGroup grpModdedOffline;
        private LinkLabel lnkSpAddCopy;
        private bool _suppressFilterEvents = false;
        // vzhled pro běžící server v ListView
        private Font _lvBold;
        private volatile bool _statusPollBusy = false;


        // --- Mods panel ---
        private GroupBox grpMods;
        private ListBox lstMods;

        // --- pravý info panel (doplněno) ---
        private GroupBox grpServerParams, grpClientParams;
        private Label spExeVal, spCpuVal, spAddVal, spArgsVal;
        private LinkLabel lnkSpArgsCopy;

        private Label clCfgArgsVal, clFinalArgsVal;
        private LinkLabel lnkClCfgCopy, lnkClFinalCopy;

        private const string PrimaryUID = "DEV76561199879902438";
        private const string SecondaryUID = "DEV98765432101234567"; // fake

        // --- Sync progress (Form1) ---
        private readonly Timer syncProgressTimer = new Timer();
        private Stopwatch _syncStopwatch;
        private ProgressDialog _syncDlg;
        private string _syncBaseStatus = "Preparing…";
        private Label spInfoIngameName;


        // --- Auto-update check (Form1) ---
        private readonly Timer autoUpdateTimer = new Timer();
        private bool _autoUpdateCheckBusy = false;
        private DateTime _lastUpdatePromptUtc = DateTime.MinValue;
        private static readonly TimeSpan AutoPromptCooldown = TimeSpan.FromSeconds(10);




        // --- pravý info panel (runtime UI) ---
        private Label infoName, infoType, infoVersion, infoMission, infoPort, infoStatusText;
        private LinkLabel lnkCfgPath, lnkCfgOpen, lnkCfgCopy, lnkProfilesPath, lnkProfilesOpen, lnkIp, lnkIpCopy, lnkQuickCopy;
        private Label lblQuick;           // quick connect text
        private Button btnInfoRun, btnInfoStop;
        private CheckBox chkInfoAutoConnect;
        private Panel dotStatus;
        private DayZConfig _selectedConfig;    // aktuálně zvolený v listu
        private string _localIp = "127.0.0.1"; // aktualizuje se z UpdateLocalIpLabel()

        // === Quick access to mission ===
        private GroupBox grpQuickMission;
        private Label lblQuickMission;
        private LinkLabel lnkQuickMissionOpen;
        private CheckBox chkEnableCfgGameplay;
        private bool _suppressQuickMissionUi = false;
        private LinkLabel lnkEditCfgGameplay;


        // === Crash Logs (MP) ===
        private GroupBox grpCrashLogs;
        private ComboBox cmbCrashLogSelect;
        private LinkLabel lnkCrashOpen, lnkCrashShow, lnkCrashBackup, lnkCrashDelete;

        // === Crash Logs (SP) ===
        private GroupBox grpCrashLogsSP;
        private ComboBox cmbCrashLogSelectSP;
        private LinkLabel lnkCrashOpenSP, lnkCrashShowSP, lnkCrashBackupSP, lnkCrashDeleteSP;


        // PIDy klientů spuštěných z této appky podle konfigurace
        private readonly Dictionary<string, HashSet<int>> _clientPidsByConfig = new();




        // ==== SINGLE-PLAYER storage ====
        private readonly string SpConfigsFilePath = "sp_configs.json";
        private readonly BindingList<SinglePlayerConfig> _spConfigs = new(); // pro data-bind a snadný refresh
        private static readonly JsonSerializerOptions SpJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        // ==== SP ListView groups + filter UI ====
        private ListViewGroup spGrpVanillaOnline;
        private ListViewGroup spGrpVanillaOffline;
        private ListViewGroup spGrpModdedOnline;
        private ListViewGroup spGrpModdedOffline;

        private ComboBox cmbSpFilter;
        private Button btnSpCreateFilter;
        private Button btnSpManageFilters;

        private const string SpAllFiltersItem = "All";
        private string _currentSpFilter = SpAllFiltersItem;
        private bool _suppressSpFilterEvents = false;

        private Label spInfoIngame;   // nový label pro in-game name
        private ShortcutRules.ShortcutContext _shortcutCtx = new();

        // --- SP watchdog: parsování argumentů z command line ---
        private static readonly Regex RxProfiles = new(@"-profiles\s*=\s*(""([^""]+)""|\S+)", RegexOptions.IgnoreCase);
        private static readonly Regex RxMission = new(@"-mission\s*=\s*(""([^""]+)""|\S+)", RegexOptions.IgnoreCase);

        private PictureBox[] _shortcutButtons;
        /// Fallback kill pro klienta spuštěného přes BE:
        /// dohledá a zabije DayZ_x64.exe i DayZ_BE.exe podle -connect/-port.
        /// Vrací počet zabitých procesů; fails = počet chyb při Kill().
        /// 
        private ChangeLog _changeLog = null!;
        private string _currentVersion = "";

        private bool _isChangelogAdmin = false;
        private ChangelogAuthConfig? _changelogAdminCfg;

    private void EnsureChangelogIsUpToDate()
    {
        var src = Paths.InstallChangeLog; // součást release (vedle EXE)
        var dst = Paths.UserChangeLog;    // uživatelova kopie (%AppData%)

        if (!File.Exists(src)) return;

        if (!File.Exists(dst))
        {
            SafeCopy(src, dst);
            return;
        }

        string? verSrc = GetLatestVersionFromFile(src);
        string? verDst = GetLatestVersionFromFile(dst);

        bool srcIsNewer =
            verSrc != null && verDst != null
            ? string.Compare(verSrc, verDst, StringComparison.OrdinalIgnoreCase) > 0
            : File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dst);

        if (srcIsNewer) SafeCopy(src, dst);
    }

    private static void SafeCopy(string src, string dst)
    {
        try
        {
            if (File.Exists(dst)) File.Copy(dst, dst + ".bak", true);
            File.Copy(src, dst, true);
        }
        catch { /* logni pokud chceš, ale neshazuj UI */ }
    }

    private static string? GetLatestVersionFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var log = System.Text.Json.JsonSerializer.Deserialize<Bohemia_Solutions.Models.ChangeLog>(json);
                var latest = log?.Versions?
                .OrderByDescending(v => v.Date)
                .ThenByDescending(v => v.Version, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            return latest?.Version;
        }
        catch { return null; }
    }


    private void InitFooter()
        {
            _changeLog = ChangeLogStorage.LoadOrCreate();

            // verze z AssemblyInformationalVersion nebo FileVersion
            var asm = Assembly.GetExecutingAssembly();
            var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var fileVer = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

            _currentVersion = infoVer ?? fileVer ?? "0.0.0";

            lblVersion.IsLink = true;
            lblVersion.LinkBehavior = LinkBehavior.HoverUnderline;
            lblVersion.Text = $"v{_currentVersion} (Changelog)";

            // build time (volitelně můžeš mít vlastní hodnotu)
            var buildTime = DateTime.Now; // nebo z Resource/CI proměnné
            lblBuildTime.Text = "🔄 " + buildTime.ToString("dddd, MMMM d 'at' h:mm tt");

           
            lblVersion.Click += (_, __) =>
            {
                using var dlg = new ChangeLogViewerForm(_changeLog);
                dlg.ShowDialog(this);
            };
           
        }





        private static int KillClientsByPortFallback(DayZConfig cfg, out int fails)
        {
            int ok = 0;
            fails = 0;

            try
            {
                int port = cfg.ServerParameters?.Port ?? 2302;

                // Hledáme oba možné klienty
                string wql = "SELECT ProcessId, Name, CommandLine FROM Win32_Process "
                           + "WHERE Name='DayZ_x64.exe' OR Name='DayZ_BE.exe'";

                using var searcher = new ManagementObjectSearcher(wql);
                foreach (ManagementObject mo in searcher.Get())
                {
                    string name = (mo["Name"] as string) ?? "";
                    string cmd = (mo["CommandLine"] as string) ?? "";

                    // Tolerantní match: -connect ...127.0.0.1 a -port=<port> (podporuj i mezerový zápis)
                    bool hasConnectLocal = cmd.IndexOf("-connect=127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0
                                        || cmd.IndexOf("-connect 127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0;

                    bool hasPort = cmd.IndexOf($"-port={port}", StringComparison.OrdinalIgnoreCase) >= 0
                                || cmd.IndexOf($"-port {port}", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!hasConnectLocal || !hasPort) continue;

                    if (int.TryParse(mo["ProcessId"]?.ToString(), out int pid))
                    {
                        try
                        {
                            var p = System.Diagnostics.Process.GetProcessById(pid);
                            if (!p.HasExited) { p.Kill(); ok++; }
                        }
                        catch { fails++; }
                    }
                }
            }
            catch
            {
                // ignore – fallback jen doplňuje standardní cestu
            }

            return ok;
        }

        private void WireShortcutButtons()
        {
            for (int i = 0; i < _shortcutButtons.Length; i++)
            {
                int idx = i;
                var pb = _shortcutButtons[i];
                pb.Cursor = Cursors.Default;
                pb.Tag = idx;
                pb.Click -= ShortcutClickHandler; // pro jistotu
                pb.Click += ShortcutClickHandler;
                var tip = new ToolTip();
                tip.SetToolTip(pb, $"Shortcut {idx + 1}");
            }
        }

        private static bool IsSteamVersion(string versionName)
        {
            if (string.IsNullOrWhiteSpace(versionName)) return false;
            return versionName.StartsWith("DayZ", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetServerDirForVersion(string versionName)
        {
            // stejné větvení jako v ConfigForm.cs
            if (string.Equals(versionName, "DayZ (Steam)", StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZServerStableRoot;
            if (string.Equals(versionName, "DayZ Experimental (Steam)", StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZServerExpRoot;
            if (string.Equals(versionName, "DayZ Internal (Steam)", StringComparison.OrdinalIgnoreCase))
                return PathSettingsService.Current.DayZServerInternalRoot;

            // BDS verze: C:\BDS\<ver>\Server
            return Path.Combine(PathSettingsService.Current.BdsRoot, versionName ?? "", "Server");
        }

        private static string GetMissionDirectory(string version, string missionName)
        {
            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(missionName)) return null;
            string serverDir = GetServerDirForVersion(version);
            if (string.IsNullOrWhiteSpace(serverDir) || !Directory.Exists(serverDir)) return null;
            return Path.Combine(serverDir, "mpmissions", missionName);
        }

        private static bool ReadEnableCfgGameplayFromCfg(string cfgPath)
        {
            if (string.IsNullOrWhiteSpace(cfgPath) || !File.Exists(cfgPath)) return false;
            foreach (var line in File.ReadAllLines(cfgPath))
            {
                var t = line.Trim();
                if (t.StartsWith("enableCfgGameplayFile", StringComparison.OrdinalIgnoreCase))
                {
                    // očekáváme: enableCfgGameplayFile = 0/1;
                    var m = Regex.Match(t, @"enableCfgGameplayFile\s*=\s*(\d+)\s*;", RegexOptions.IgnoreCase);
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int v)) return v != 0;
                }
            }
            return false;
        }

        private static void UpdateOrInsertConfigValueInFile(string cfgPath, string key, string value, bool quote)
        {
            if (string.IsNullOrWhiteSpace(cfgPath) || !File.Exists(cfgPath)) return;

            var lines = File.ReadAllLines(cfgPath).ToList();

            // 1) Najdi začátek bloku "class Missions"
            int missionsIdx = lines.FindIndex(l =>
                Regex.IsMatch(l, @"^\s*class\s+Missions\b", RegexOptions.IgnoreCase));
            if (missionsIdx < 0) missionsIdx = lines.Count; // když není, vložíme na konec

            // 2) Najdi existující řádek s klíčem (kdekoliv)
            int keyIdx = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var t = lines[i].TrimStart();
                if (t.StartsWith(key + " ", StringComparison.Ordinal))
                {
                    keyIdx = i;
                    break;
                }
            }

            string val = quote ? $"\"{value}\"" : value;
            string newLine = $"{key} = {val};";

            if (keyIdx >= 0)
            {
                // a) Klíč existuje → aktualizuj hodnotu
                string indent = lines[keyIdx].Substring(0, Math.Max(0, lines[keyIdx].IndexOf(key)));
                lines[keyIdx] = $"{indent}{newLine}";

                // b) Když je až ZA "class Missions", přesuň ho PŘED "class Missions"
                if (keyIdx > missionsIdx)
                {
                    lines.RemoveAt(keyIdx);
                    // index missionsIdx se nemění, protože odebíráme položku až za ním
                    lines.Insert(missionsIdx, newLine);
                }
            }
            else
            {
                // Klíč neexistuje → vlož přímo PŘED "class Missions"
                lines.Insert(missionsIdx, newLine);
            }

            File.WriteAllLines(cfgPath, lines);
        }



        private void ShortcutClickHandler(object? sender, EventArgs e)
        {
            if (sender is PictureBox pb && pb.Tag is int idx)
                LaunchShortcut(idx);
        }

        private void ApplyShortcutsToButtons()
        {
            var list = PathSettingsService.Current.Shortcuts ?? new();
            for (int i = 0; i < _shortcutButtons.Length; i++)
            {
                var pb = _shortcutButtons[i];
                if (i < list.Count && File.Exists(list[i].ExePath))
                {
                    SetExeIconToPictureBox(list[i].ExePath, pb);
                    pb.Enabled = true;
                    pb.Cursor = Cursors.Hand;
                    pb.Visible = true;
                }
                else
                {
                    pb.Image = null;
                    pb.Enabled = false;
                    pb.Cursor = Cursors.Default;
                    pb.Visible = false;
                }
            }
        }

        private void LaunchShortcut(int idx)
        {
            var list = PathSettingsService.Current.Shortcuts ?? new();
            if (idx >= list.Count) return;

            var sc = list[idx];
            if (!File.Exists(sc.ExePath)) return;

            // 1) speciální pravidla (Tracy, RemoteDebugger…)
            if (ShortcutRules.TryHandle(sc, this, _shortcutCtx)) return;

            // 2) defaultní spuštění
            try
            {
                var psi = new ProcessStartInfo(sc.ExePath)
                {
                    UseShellExecute = true,
                    WorkingDirectory = string.IsNullOrWhiteSpace(sc.WorkingDir)
                        ? Path.GetDirectoryName(sc.ExePath)!
                        : sc.WorkingDir,
                    Arguments = sc.Arguments ?? ""
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Nelze spustit zkratku #{idx + 1}: {ex.Message}",
                    "Launch error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private static string NormalizePath(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return "";
            p = p.Trim().Trim('"').Replace('/', '\\');
            try { p = Path.GetFullPath(p); } catch { }
            return p.TrimEnd('\\').ToLowerInvariant();
        }

        private static string ExtractArg(Regex rx, string cmdLine)
        {
            var m = rx.Match(cmdLine ?? "");
            if (!m.Success) return "";
            // skupina 2 je bez uvozovek, jinak 1
            return m.Groups[2].Success ? m.Groups[2].Value : m.Groups[1].Value;
        }

        private static string NormalizeMissionArg(string? m)
        {
            if (string.IsNullOrWhiteSpace(m)) return "";
            var s = m.Trim().Trim('"').Replace('/', '\\');
            if (s.StartsWith(".\\", StringComparison.Ordinal)) s = s.Substring(2);
            return s.ToLowerInvariant();
        }


        // --- Global filters registry (MP + SP) ---
        private sealed class FiltersGlobal
        {
            public List<string> MP { get; set; } = new();
            public List<string> SP { get; set; } = new();
        }
        private FiltersGlobal _filtersGlobal = new();
        private const string FiltersGlobalPath = "filters_global.json";

        private static string NormalizeFilter(string s) =>
            Regex.Replace((s ?? "").Trim(), @"\s{2,}", " ");

        // ==== SP pravý info panel (runtime UI) ====
        private Label spInfoName, spInfoType, spInfoVersion, spInfoMission, spInfoProfiles, spInfoStatusText;
        private Panel spDotStatus;
        private Button btnSpRun, btnSpStop;

        private GroupBox grpSpClientParams, grpSpMods;
        private Label spExePathVal, spCfgArgsVal, spFinalArgsVal;
        private LinkLabel lnkSpProfilesOpen, lnkSpExeShow, lnkSpCfgCopy, lnkSpFinalCopy;

        private SinglePlayerConfig _selectedSpConfig;

        // PIDs klientů spuštěných *z této appky* pro SP konfigurace
        private readonly Dictionary<string, HashSet<int>> _spClientPidsByConfig = new();

        private void BuildSpClientInfoPanel()
        {
            // Pravý host = přímo panel_SP_info (NE tabpage)
            var host = panel_SP_info;
            host.SuspendLayout();
            host.AutoScroll = true;
            host.Controls.Clear();            // smažeme jen pravý obsah, list vlevo zůstává
            host.BringToFront();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(8)
            };

            // ==== Actions ==========================================================
            var grpActions = new GroupBox { Text = "Actions", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
            var flowA = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            btnSpRun = new Button { Text = "Run client !", AutoSize = true };
            btnSpStop = new Button { Text = "Stop client !", AutoSize = true, Enabled = false };
            flowA.Controls.AddRange(new Control[] { btnSpRun, btnSpStop });
            grpActions.Controls.Add(flowA);

            // ==== Selected client ==================================================
            var grpSelected = new GroupBox { Text = "Selected client", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var gridSel = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
            gridSel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridSel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpSelected.Controls.Add(gridSel);

            spDotStatus = new Panel { Width = 12, Height = 12, BackColor = Color.DarkRed, Margin = new Padding(0, 3, 6, 0) };
            spInfoStatusText = new Label { Text = "Stopped", AutoSize = true };
            var statusRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            statusRow.Controls.Add(spDotStatus);
            statusRow.Controls.Add(spInfoStatusText);

            spInfoName = new Label { AutoSize = true };
            spInfoType = new Label { AutoSize = true };
            spInfoVersion = new Label { AutoSize = true };
            spInfoMission = new Label { AutoSize = true };
            spInfoProfiles = new Label { AutoSize = true };

            lnkSpProfilesOpen = new LinkLabel { AutoSize = true, Text = "[Open Folder]" };
            var profRow = new FlowLayoutPanel { AutoSize = true };
            profRow.Controls.Add(spInfoProfiles);
            profRow.Controls.Add(lnkSpProfilesOpen);

            void addSel(string k, Control v)
            {
                gridSel.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridSel.Controls.Add(v);
            }

            addSel("Status:", statusRow);
            addSel("Name:", spInfoName);
            addSel("Type:", spInfoType);
            addSel("Version:", spInfoVersion);
            addSel("SP mission:", spInfoMission);
            addSel("Profiles:", profRow);

            spInfoIngame = new Label { AutoSize = true };
            addSel("In-game name:", spInfoIngame);


            // ==== Crash Logs (Single-player) ==========================================
            grpCrashLogsSP = new GroupBox { Text = "Crash Logs", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var gridCLsp = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
            gridCLsp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridCLsp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpCrashLogsSP.Controls.Add(gridCLsp);

            cmbCrashLogSelectSP = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 560 };
            lnkCrashOpenSP = new LinkLabel { AutoSize = true, Text = "[Open Folder]" };
            lnkCrashShowSP = new LinkLabel { AutoSize = true, Text = "[Show]" };
            lnkCrashBackupSP = new LinkLabel { AutoSize = true, Text = "[Backup logs]" };
            lnkCrashDeleteSP = new LinkLabel { AutoSize = true, Text = "[Delete]" };

            var rowCLsp = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            rowCLsp.Controls.Add(cmbCrashLogSelectSP);
            rowCLsp.Controls.Add(lnkCrashOpenSP);
            rowCLsp.Controls.Add(lnkCrashShowSP);
            rowCLsp.Controls.Add(lnkCrashBackupSP);
            rowCLsp.Controls.Add(lnkCrashDeleteSP);

            void addCLsp(string k, Control v)
            {
                gridCLsp.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridCLsp.Controls.Add(v);
            }
            addCLsp("Latest logs:", rowCLsp);






            // ==== Client parameters ===============================================
            grpSpClientParams = new GroupBox { Text = "Client parameters", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var gridCP = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
            gridCP.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridCP.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpSpClientParams.Controls.Add(gridCP);

            spExePathVal = new Label { AutoSize = true, MaximumSize = new Size(800, 0), Text = "-" };
            lnkSpExeShow = new LinkLabel { AutoSize = true, Text = "[Show]" };
            var exeRow = new FlowLayoutPanel { AutoSize = true };
            exeRow.Controls.Add(spExePathVal);
            exeRow.Controls.Add(lnkSpExeShow);

            spCfgArgsVal = new Label { AutoSize = true, MaximumSize = new Size(800, 0), Text = "-" };
            lnkSpCfgCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };
            var cfgRow = new FlowLayoutPanel { AutoSize = true };
            cfgRow.Controls.Add(spCfgArgsVal);
            cfgRow.Controls.Add(lnkSpCfgCopy);

            spFinalArgsVal = new Label { AutoSize = true, MaximumSize = new Size(800, 0), Text = "-" };
            lnkSpFinalCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };
            var finRow = new FlowLayoutPanel { AutoSize = true };
            finRow.Controls.Add(spFinalArgsVal);
            finRow.Controls.Add(lnkSpFinalCopy);

            void addCP(string k, Control v)
            {
                gridCP.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridCP.Controls.Add(v);
            }
            addCP("Executable:", exeRow);
            addCP("Configured args:", cfgRow);
            addCP("Final args:", finRow);




            lnkCrashOpenSP.LinkClicked += (s, e) =>
            {
                var prof = (string)(spInfoProfiles?.Tag ?? "");  // pokud máš SP label/link pro profiles, použij jeho .Tag
                if (!string.IsNullOrWhiteSpace(prof) && Directory.Exists(prof))
                    Process.Start("explorer.exe", $"\"{prof}\"");
            };

            lnkCrashShowSP.LinkClicked += (s, e) =>
            {
                if (cmbCrashLogSelectSP.SelectedItem is null) return;
                var path = (string)cmbCrashLogSelectSP.SelectedItem.GetType().GetProperty("Path").GetValue(cmbCrashLogSelectSP.SelectedItem);
                try
                {
                    var editor = PathSettingsService.Current.EditorExePath;
                    if (!string.IsNullOrWhiteSpace(editor) && File.Exists(editor))
                        Process.Start(new ProcessStartInfo(editor, $"\"{path}\"") { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(path) });
                    else
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open log:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            lnkCrashBackupSP.LinkClicked += (s, e) =>
            {
                var prof = (string)(spInfoProfiles?.Tag ?? "");
                if (string.IsNullOrWhiteSpace(prof) || !Directory.Exists(prof)) return;

                var latest = GetLatestLogsPerType(prof);
                if (latest.Count == 0) { MessageBox.Show("No logs to backup.", "Info"); return; }

                string zip = Path.Combine(prof, $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                try
                {
                    using (var za = ZipFile.Open(zip, ZipArchiveMode.Create))
                        foreach (var f in latest) za.CreateEntryFromFile(f.FullName, f.Name, CompressionLevel.Optimal);

                    var ask = MessageBox.Show($"Backup created:\n{zip}\n\nDelete original latest logs?", "Backup logs", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (ask == DialogResult.Yes)
                    {
                        foreach (var f in latest) { try { f.Delete(); } catch { } }
                        PopulateCrashLogsSP(prof);
                    }
                    else PopulateCrashLogsSP(prof);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Backup failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            lnkCrashDeleteSP.LinkClicked += (s, e) =>
            {
                if (cmbCrashLogSelectSP.SelectedItem is null) return;
                var path = (string)cmbCrashLogSelectSP.SelectedItem.GetType().GetProperty("Path").GetValue(cmbCrashLogSelectSP.SelectedItem);

                var ask = MessageBox.Show($"Delete selected log?\n{path}", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (ask != DialogResult.Yes) return;

                try { File.Delete(path); }
                catch (Exception ex)
                {
                    MessageBox.Show("Delete failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var prof = (string)(spInfoProfiles?.Tag ?? "");
                if (!string.IsNullOrWhiteSpace(prof)) PopulateCrashLogsSP(prof);
            };


            // ==== Mods =============================================================
            grpSpMods = new GroupBox
            {
                Text = "Mods",
                Dock = DockStyle.Top,
                AutoSize = false,                // ← nevypočítávej automaticky
                Padding = new Padding(10, 10, 10, 10) // ← nahoře místo na titulek rámu
            };

            // ListBox uvnitř rámu
            var lbMods = new ListBox
            {
                Dock = DockStyle.Fill,           // ← vyplní celý GroupBox (uvnitř paddingu)
                IntegralHeight = false,          // ← dovolí přesnou výšku bez “ořezu” na řádek
                Margin = new Padding(0)          // ← žádný extra okraj proti paddingu groupboxu
            };

            // cílová výška viditelné části listboxu (např. 200 px)
            int listVisibleHeight = 200;
            grpSpMods.Height = listVisibleHeight + grpSpMods.Padding.Vertical;

            // vložení
            grpSpMods.Controls.Add(lbMods);
            // root.Controls.Add(grpSpMods);  // (kam to teď přidáváš)


            // Pořadí v kořeni
            root.Controls.Add(grpActions);
            root.Controls.Add(grpSelected);
            // a přidej grpCrashLogsSP do příslušného SP kontejneru (tam, kam vkládáš ostatní SP groupboxy)
            root.Controls.Add(grpCrashLogsSP);
            root.Controls.Add(grpSpClientParams);
            root.Controls.Add(grpSpMods);

            // Handlery
            btnSpRun.Click += (_, __) => BtnSpRun_Click();
            btnSpStop.Click += (_, __) => BtnSpStop_Click();

            lnkSpProfilesOpen.LinkClicked += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(spInfoProfiles.Tag as string) && Directory.Exists((string)spInfoProfiles.Tag))
                    Process.Start("explorer.exe", (string)spInfoProfiles.Tag);
            };
            lnkSpExeShow.LinkClicked += (_, __) =>
            {
                var p = spExePathVal.Tag as string;
                if (!string.IsNullOrWhiteSpace(p) && File.Exists(p))
                    Process.Start("explorer.exe", "/select,\"" + p + "\"");
            };
            lnkSpCfgCopy.LinkClicked += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(spCfgArgsVal.Text) && spCfgArgsVal.Text != "-") Clipboard.SetText(spCfgArgsVal.Text);
            };
            lnkSpFinalCopy.LinkClicked += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(spFinalArgsVal.Text) && spFinalArgsVal.Text != "-") Clipboard.SetText(spFinalArgsVal.Text);
            };

            // Vložit do pravého panelu
            host.Controls.Add(root);
            host.ResumeLayout();
            PanelSpInfo_Resize(panel_SP_info, EventArgs.Empty);

            // ===== helper pro Mods a uložení do Tagu pravého panelu =====
            void SetMods(IEnumerable<string> mods)
            {
                lbMods.BeginUpdate();
                lbMods.Items.Clear();
                foreach (var m in mods) lbMods.Items.Add(m);
                lbMods.EndUpdate();
                grpSpMods.Visible = lbMods.Items.Count > 0;
            }
            // Ulož DELEGÁT do panelu_SP_info (ne do pnl_Client_Info!)
            panel_SP_info.Tag = (Action<IEnumerable<string>>)SetMods;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            tbControllAll.Height = this.ClientSize.Height - statusStrip1.Height - tbControllAll.Top;
        }

        private void FixTabPageRoots()
        {
            foreach (TabPage tp in tbControllAll.TabPages)
            {
                // pokud má tab jednu hlavní „obálku“ (Panel/UserControl/…),
                // nastav ji na Fill – vyřeší to i tp_SinglePlayer
                if (tp.Controls.Count == 1)
                {
                    var root = tp.Controls[0];
                    root.Dock = DockStyle.Fill;
                }
                // pokud bys tam měl víc prvků, zvaž, který je ten root a nastav Dock=Fill právě jemu
            }
        }



        private void LoadGlobalFilters()
        {
            try
            {
                if (File.Exists(FiltersGlobalPath))
                {
                    var json = File.ReadAllText(FiltersGlobalPath);
                    _filtersGlobal = System.Text.Json.JsonSerializer
                        .Deserialize<FiltersGlobal>(json, SpJsonOptions) ?? new FiltersGlobal();
                }
                else _filtersGlobal = new FiltersGlobal();
            }
            catch { _filtersGlobal = new FiltersGlobal(); }
        }

        private void SaveGlobalFilters()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_filtersGlobal, SpJsonOptions);
                File.WriteAllText(FiltersGlobalPath, json);
            }
            catch { /* případně zaloguj */ }
        }


        private void LoadSpConfigsFromJson()
        {
            _spConfigs.Clear();
            try
            {
                if (!File.Exists(SpConfigsFilePath)) return;
                var json = File.ReadAllText(SpConfigsFilePath);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<SinglePlayerConfig>>(json, SpJsonOptions)
                           ?? new List<SinglePlayerConfig>();

                foreach (var c in list)
                {
                    if (string.IsNullOrWhiteSpace(c.Id))
                        c.Id = Guid.NewGuid().ToString("N");   // zajisti ID
                    _spConfigs.Add(c);
                }

                // persistuj nově vytvořená Id, ať tam příště jsou
                SaveSpConfigsToJson();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load SP configs:\n" + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSpConfigsToJson()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_spConfigs.ToList(), SpJsonOptions);
                File.WriteAllText(SpConfigsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save SP configs:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitSpListView()
        {
            var lv = listViewConfigsSP;
            lv.BeginUpdate();
            lv.Clear();
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.MultiSelect = true;
            lv.GridLines = true;

            // Sloupce jako u MP, jen "SP mission"
            lv.Columns.Add("Name", 280);
            lv.Columns.Add("Version", 100);
            lv.Columns.Add("SP mission", 300);

            // Skupiny (sekce) jako u MP
            lv.ShowGroups = true;
            lv.Groups.Clear();
            spGrpVanillaOnline = new ListViewGroup("Vanilla - Online", HorizontalAlignment.Left);
            spGrpVanillaOffline = new ListViewGroup("Vanilla - Offline", HorizontalAlignment.Left);
            spGrpModdedOnline = new ListViewGroup("Modded - Online", HorizontalAlignment.Left);
            spGrpModdedOffline = new ListViewGroup("Modded - Offline", HorizontalAlignment.Left);
            lv.Groups.AddRange(new[] { spGrpVanillaOnline, spGrpVanillaOffline, spGrpModdedOnline, spGrpModdedOffline });

            lv.EndUpdate();
        }


        private void RefreshSpListView()
        {
            IEnumerable<SinglePlayerConfig> src = _spConfigs;

            if (!string.IsNullOrEmpty(_currentSpFilter) && _currentSpFilter != SpAllFiltersItem)
            {
                src = src.Where(c => (c.Filters ?? new List<string>())
                    .Any(f => string.Equals(f, _currentSpFilter, StringComparison.CurrentCultureIgnoreCase)));
            }

            var lv = listViewConfigsSP;
            lv.BeginUpdate();
            lv.Items.Clear();

            foreach (var c in src)
            {
                string name = c.Name ?? "";
                string version = c.VersionFolder ?? "";
                string mission = c.MissionParam ?? "";      // SP mise bereme z MissionParam
                bool isModded = string.Equals(c.Type?.Trim(), "Modded", StringComparison.OrdinalIgnoreCase);
                bool offline = IsOfflineMission(mission);

                var it = new ListViewItem(name);
                it.SubItems.Add(version);
                it.SubItems.Add(mission);

                // zařazení do 4 skupin
                it.Group = isModded
                    ? (offline ? spGrpModdedOffline : spGrpModdedOnline)
                    : (offline ? spGrpVanillaOffline : spGrpVanillaOnline);

                // stejné jemné zabarvení jako u MP
                if (isModded)
                    it.BackColor = offline ? Color.FromArgb(255, 240, 230) : Color.FromArgb(255, 248, 238);
                else
                    it.BackColor = offline ? Color.FromArgb(230, 240, 255) : Color.FromArgb(238, 248, 255);

                it.Tag = c;
                lv.Items.Add(it);
            }

            lv.EndUpdate();
        }

        private void BuildSpFilterBarUI()
        {
            if (cmbSpFilter != null) return; // jednou stačí

            // rodič panelu – dej to do stejného kontejneru jako listViewConfigsSP
            var host = listViewConfigsSP.Parent;

            cmbSpFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbSpFilter",
                Width = 300
            };
            btnSpCreateFilter = new Button { Text = "Create Filter", Name = "btnSpCreateFilter", AutoSize = true };
            btnSpManageFilters = new Button { Text = "Manage", Name = "btnSpManageSpFilters", AutoSize = true };

            // Umístění nad listview (stejně jako u MP)
            cmbSpFilter.Location = new Point(listViewConfigsSP.Left, listViewConfigsSP.Top);
            btnSpCreateFilter.Location = new Point(cmbSpFilter.Right + 8, listViewConfigsSP.Top - 2);
            btnSpManageFilters.Location = new Point(btnSpCreateFilter.Right + 8, btnSpCreateFilter.Top);

            // Rezervuj místo nahoře
            int shift = cmbSpFilter.Height + 8;
            listViewConfigsSP.Top += shift;
            listViewConfigsSP.Height -= shift;

            // Eventy
            cmbSpFilter.SelectedIndexChanged += (_, __) => ApplySpFilterAndRefresh();
            btnSpCreateFilter.Click += (_, __) => HandleSpCreateFilter();
            btnSpManageFilters.Click += (_, __) => HandleSpManageFilters();

            // Anchor
            cmbSpFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnSpCreateFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnSpManageFilters.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            host.Controls.Add(cmbSpFilter);
            host.Controls.Add(btnSpCreateFilter);
            host.Controls.Add(btnSpManageFilters);

            RefreshSpFilterSelector(); // naplnit
        }

        private List<string> CollectKnownSpFilters()
        {
            var fromConfigs = _spConfigs
                .SelectMany(c => c?.Filters ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeFilter);

            var fromRegistry = (_filtersGlobal?.SP ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeFilter);

            return fromConfigs
                .Concat(fromRegistry)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


        private List<string> CollectAllKnownFilters()
        {
            var mp = CollectKnownFilters();     // už existující funkce (MP)
            var sp = CollectKnownSpFilters();
            return mp.Concat(sp)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                     .ToList();
        }

        private void RefreshSpFilterSelector()
        {
            if (cmbSpFilter == null) return;

            var distinct = CollectUsedSpFilters(); // stejné jako MP (CollectUsedFilters)

            var prev = (string)(cmbSpFilter.SelectedItem ?? SpAllFiltersItem);

            _suppressSpFilterEvents = true;
            try
            {
                cmbSpFilter.BeginUpdate();
                cmbSpFilter.Items.Clear();
                cmbSpFilter.Items.Add(SpAllFiltersItem);
                foreach (var f in distinct) cmbSpFilter.Items.Add(f);

                int idx = Math.Max(0, cmbSpFilter.Items.IndexOf(prev));
                if (cmbSpFilter.SelectedIndex != idx) cmbSpFilter.SelectedIndex = idx;
            }
            finally
            {
                cmbSpFilter.EndUpdate();
                _suppressSpFilterEvents = false;
            }
        }

        private void ApplySpFilterAndRefresh()
        {
            if (_suppressSpFilterEvents) return;
            _currentSpFilter = (string)(cmbSpFilter?.SelectedItem ?? SpAllFiltersItem);
            RefreshSpListView();
        }

        // Jednoduchý "Create Filter": požádá o jméno a přidá ho VYBRANÝM SP konfiguracím
        private void HandleSpCreateFilter()
        {
            using var dlg = new CreateSpFilterForm(_spConfigs.ToList());
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var name = NormalizeFilter(dlg.FilterName ?? "");
            if (string.IsNullOrWhiteSpace(name)) return;

            if (!_filtersGlobal.SP.Any(f => f.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                _filtersGlobal.SP.Add(name);
                SaveGlobalFilters();
            }

            var ids = new HashSet<string>(dlg.SelectedConfigIds ?? Enumerable.Empty<string>(),
                                          StringComparer.OrdinalIgnoreCase);

            int assigned = 0; // ⬅️ count assignments
            foreach (var cfg in _spConfigs)
            {
                if (string.IsNullOrWhiteSpace(cfg.Id))
                    cfg.Id = Guid.NewGuid().ToString("N");

                if (ids.Contains(cfg.Id))
                {
                    cfg.Filters ??= new List<string>();
                    if (!cfg.Filters.Any(f => f.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        cfg.Filters.Add(name);
                        assigned++;
                    }
                }
            }

            SaveSpConfigsToJson();
            RefreshSpFilterSelector();
            cmbSpFilter.SelectedItem = name;
            RefreshSpListView();

            if (assigned == 0)
            {
                MessageBox.Show(
                    $"Filter \"{name}\" was created and is currently empty.",
                    "Filter created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }


        // Jednoduchý "Manage": nechá vybrat existující filtr a smaže ho ze všech SP konfigurací
        private void HandleSpManageFilters()
        {

            var allSpFilters = CollectKnownSpFilters();
            using var dlg = new ManageSpFiltersForm(_spConfigs.ToList(), allSpFilters);

            var prev = (string)(cmbSpFilter?.SelectedItem ?? SpAllFiltersItem);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                SaveSpConfigsToJson();

                var stillUsed = _spConfigs
                    .SelectMany(c => c?.Filters ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(NormalizeFilter)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                _filtersGlobal.SP = (_filtersGlobal.SP ?? new List<string>())
                    .Where(f => stillUsed.Contains(NormalizeFilter(f)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                SaveGlobalFilters();

                RefreshSpFilterSelector();

                if (cmbSpFilter != null)
                {
                    var target = cmbSpFilter.Items.Contains(prev) ? prev : SpAllFiltersItem;
                    cmbSpFilter.SelectedItem = target;

                    // DŮLEŽITÉ: srovnat model se stavem UI
                    _currentSpFilter = (string)(cmbSpFilter.SelectedItem ?? SpAllFiltersItem);
                }

                RefreshSpListView(); // nebo místo těch dvou řádků: ApplySpFilterAndRefresh();
            }
            else
            {
                RefreshSpFilterSelector();

                if (cmbSpFilter != null && !cmbSpFilter.Items.Contains(_currentSpFilter))
                {
                    cmbSpFilter.SelectedIndex = 0;           // All
                    _currentSpFilter = SpAllFiltersItem;     // a srovnat i model
                }

                RefreshSpListView(); // nebo ApplySpFilterAndRefresh();
            }
        }






        // 1) Jednotná helper funkce
        private static bool IsInIgnoredTopLevel(string sourceRoot, string path)
        {
            var rel = Path.GetRelativePath(sourceRoot, path);
            var first = rel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                                  StringSplitOptions.RemoveEmptyEntries)
                           .FirstOrDefault();

            return string.Equals(first, "!Workshop", StringComparison.OrdinalIgnoreCase)
                || string.Equals(first, "!LocalWorkshop", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetConfigKey(DayZConfig cfg)
        {
            return string.Join("|",
                Path.GetFullPath(cfg.ServerPath ?? ""),
                Path.GetFullPath(cfg.ServerParameters?.ConfigFile ?? ""),
                cfg.VersionFolder ?? "",
                (cfg.ServerParameters?.Port ?? 2302).ToString());
        }

        private void OnNetworkAddressChanged(object? sender, EventArgs e)
        {
            if (_appClosing || !IsHandleCreated || IsDisposed) return;
            try { BeginInvoke((System.Windows.Forms.MethodInvoker)UpdateLocalIpLabel); } catch { }
        }


        /// Fallback killer pouze pro DayZ_BE.exe, bez filtru na cestu k EXE.
        /// Použije jen rozumné ověření přes -connect 127.0.0.1 a -port=<cfg.Port>.
        private static int KillBeClientsByPortFallback(DayZConfig cfg, out int fails)
        {
            int ok = 0;
            fails = 0;

            try
            {
                int port = cfg.ServerParameters?.Port ?? 2302;

                // Vem všechny DayZ_BE.exe a ověř jen CommandLine
                string wql = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='DayZ_BE.exe'";
                using (var searcher = new System.Management.ManagementObjectSearcher(wql))
                {
                    foreach (System.Management.ManagementObject mo in searcher.Get())
                    {
                        string cmd = (mo["CommandLine"] as string) ?? string.Empty;

                        bool match = true;

                        // Když je spuštěno s -connect, porovnáme 127.0.0.1 a port
                        if (cmd.IndexOf("-connect=", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (cmd.IndexOf("-connect=127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0)
                                match &= cmd.IndexOf($"-port={port}", StringComparison.OrdinalIgnoreCase) >= 0;
                        }

                        if (!match) continue;

                        if (int.TryParse(mo["ProcessId"]?.ToString(), out int pid))
                        {
                            try
                            {
                                var p = System.Diagnostics.Process.GetProcessById(pid);
                                if (!p.HasExited) { p.Kill(); ok++; }
                            }
                            catch { fails++; }
                        }
                    }
                }
            }
            catch
            {
                // ignore – v nejhorším už něco sestřelil standardní KillClientsForConfig
            }

            return ok;
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateLocalIpLabel();
            lbl_ip_adress.Cursor = Cursors.Hand;
            ipTip.SetToolTip(lbl_ip_adress, "Click to copy IP");


            SetupListView();
            BuildFilterBarUI();
            BuildServerInfoPanel();
            BuildSpClientInfoPanel();

            listViewConfigsSP.SelectedIndexChanged += (_, __) =>
            {
                var spCfg = (listViewConfigsSP.SelectedItems.Count == 0)
                    ? null
                    : (SinglePlayerConfig)listViewConfigsSP.SelectedItems[0].Tag;
                UpdateSpClientInfoPanel(spCfg);
            };

            // po načtení SP konfigurací a refreshi listu vyresetuj panel
            UpdateSpClientInfoPanel(null);
            _lvBold = new Font(listViewConfigs.Font, FontStyle.Bold);

            LoadVersions();
            configs = LoadConfigsFromJson();
            foreach (var cfg in configs)
            {
                if (string.IsNullOrWhiteSpace(cfg.Id))
                    cfg.Id = Guid.NewGuid().ToString("N");
            }
            SaveConfigsToJson(configs); // uloží doplněná Id, ať je máš příště
            FillListView();
            LoadGlobalFilters();
            RefreshFilterSelector();
            RefreshSpFilterSelector();
            ShowServerStatus(false); // Server neběží

            serverStatusTimer.Interval = 500;
            serverStatusTimer.Tick += serverStatusTimer_Tick;
            serverStatusTimer.Start();

            crashLogsTimer.Interval = 2500; // 2.5 s je příjemný kompromis
            crashLogsTimer.Tick += CrashLogsTimer_Tick;
            crashLogsTimer.Start();


            //pbButton1.Enabled = false;




            syncProgressTimer.Interval = 1000;
            syncProgressTimer.Tick += SyncProgressTimer_Tick;

            // kontrola každých 5 min
            autoUpdateTimer.Interval = 300_000; // 5 min
            autoUpdateTimer.Tick += async (_, __) => await AutoCheckAndPromptAsync();
            autoUpdateTimer.Start();

            // první kontrola hned po startu (ponech)
            this.BeginInvoke(new Action(async () => await AutoCheckAndPromptAsync()));

            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            //  BuildFilterBarUI();


        }

        private static string ResolveClientExeName(DayZConfig cfg)
        {
            // Když je server nastaven na DayZServer_x64.exe → klient = DayZ_BE.exe
            var serverExe = Path.GetFileName(cfg.ServerParameters?.ExeName ?? "");
            if (serverExe.Equals("DayZServer_x64.exe", StringComparison.OrdinalIgnoreCase))
                return "DayZ_BE.exe";

            // Jinak nech to, co je v konfiguraci (pokud není, spadni na DayZDiag_x64.exe)
            return string.IsNullOrWhiteSpace(serverExe) ? "DayZDiag_x64.exe" : serverExe;
        }


        private static string BuildSpMissionArgs(SinglePlayerConfig c)
        {
            // kořen klienta (WorkingDirectory je právě tohle)
            string baseDir = GetSpBaseClientDir(c);

            // 1) Preferuj absolutní cestu, pokud je zadaná – ale převeď ji na relativní .\…,
            //    a HLAVNĚ bez uvozovek u -mission
            var abs = (c.MissionAbsPath ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(abs))
            {
                try
                {
                    string rel = Path.GetRelativePath(baseDir, abs).Replace('/', '\\');
                    if (!rel.StartsWith(".\\", StringComparison.Ordinal)) rel = ".\\" + rel;
                    // return $"-world=none -mission={rel}";
                    return $"-mission={rel}";
                }
                catch
                {
                    // fallback: když relativizace selže, použij absolutní (jen kdyby byla nutná)
                    // POZOR: bez uvozovek u -mission (většinou funguje i s mezerami v cestě)
                    string cleaned = abs.Replace('/', '\\');
                    // return $"-world=none -mission={cleaned}";
                    return $"-mission={cleaned}";
                }
            }

            // 2) Jinak slož z MissionParam
            var m = (c.MissionParam ?? "").Trim().Replace('/', '\\');
            if (string.IsNullOrWhiteSpace(m)) return "";

            if (!m.Contains("\\", StringComparison.Ordinal))
                m = Path.Combine("missions", m);

            if (!m.StartsWith(".\\", StringComparison.OrdinalIgnoreCase) &&
                !m.StartsWith("..\\", StringComparison.OrdinalIgnoreCase))
                m = ".\\" + m;

            // BEZ uvozovek
            //  return $"-world=none -mission={m}";
            return $"-mission={m}";
        }



        private void UpdateSpClientInfoPanel(SinglePlayerConfig cfg)
        {
            _selectedSpConfig = cfg;
            var setMods = panel_SP_info.Tag as Action<IEnumerable<string>>;
            // Reset?
            // Reset?
            if (cfg == null)
            {
                spInfoName.Text = spInfoType.Text = spInfoVersion.Text = spInfoMission.Text = spInfoProfiles.Text = "-";
                spInfoProfiles.Tag = null;
                // Crash Logs (SP) – reset
                cmbCrashLogSelectSP?.Items.Clear();
                lnkCrashOpenSP.Enabled = false;
                lnkCrashShowSP.Enabled = false;
                lnkCrashBackupSP.Enabled = false;
                lnkCrashDeleteSP.Enabled = false;
                if (cmbCrashLogSelectSP != null) cmbCrashLogSelectSP.Items.Clear();
                if (lnkCrashOpenSP != null) lnkCrashOpenSP.Enabled = false;
                if (lnkCrashShowSP != null) lnkCrashShowSP.Enabled = false;
                if (lnkCrashBackupSP != null) lnkCrashBackupSP.Enabled = false;
                if (lnkCrashDeleteSP != null) lnkCrashDeleteSP.Enabled = false;
                spExePathVal.Text = spCfgArgsVal.Text = spFinalArgsVal.Text = "-";
                spExePathVal.Tag = null;

                spInfoIngame.Text = "—";                     // <<< doplněno
                setMods?.Invoke(Array.Empty<string>());      // <<< schovej a vyprázdni Mods

                SetSpStatus(false);
                btnSpRun.Enabled = btnSpStop.Enabled = false;
                _crashSigSp = "";
                return;
            }


            spInfoName.Text = cfg.Name ?? "-";
            spInfoType.Text = cfg.Type ?? "-";
            spInfoVersion.Text = cfg.VersionFolder ?? "-";
            spInfoMission.Text = cfg.MissionParam ?? "-";

            var profiles = cfg.ProfilesFolder ?? "";
            spInfoProfiles.Text = string.IsNullOrWhiteSpace(profiles) ? "-" : profiles;
            spInfoProfiles.Tag = profiles;

            // Refresh Crash Logs (SP)
            PopulateCrashLogsSP(profiles);

            string exePath = GetSpExePath(cfg);
            spExePathVal.Text = File.Exists(exePath) ? exePath : "(exe not found)";
            spExePathVal.Tag = exePath;

            string cfgArgs = (cfg.ClientArguments ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cfgArgs))
                cfgArgs = "-window -nopause -disableCrashReport";
            spCfgArgsVal.Text = cfgArgs;

            string finalArgs = BuildSpClientArgs(cfg);
            spFinalArgsVal.Text = finalArgs;


            // >>> DOPLŇ TOTO:
            spInfoIngame.Text = string.IsNullOrWhiteSpace(cfg.IngameName) ? "—" : cfg.IngameName;

            var modsToShow = (cfg.Mods ?? Enumerable.Empty<string>())
                             .Where(m => !string.IsNullOrWhiteSpace(m));
            setMods?.Invoke(modsToShow);

            // Kontrola běžících procesů asynchronně, aby UI nezamrzlo
            Task.Run(() =>
            {
                bool running = FindSpClientProcessesForConfig(cfg).Any();

                this.BeginInvoke((System.Windows.Forms.MethodInvoker)(() =>
                {
                    SetSpStatus(running);
                    btnSpRun.Enabled = !running;
                    btnSpStop.Enabled = running;
                }));
            });
        }

        private static string GetSpBaseClientDir(SinglePlayerConfig c)
        {
            // 1) explicitní ClientPath
            if (!string.IsNullOrWhiteSpace(c.ClientPath))
                return c.ClientPath;

            // 2) fallback: {BdsRoot}\{Version}\Client
            if (!string.IsNullOrWhiteSpace(c.VersionFolder))
                return Path.Combine(PathSettingsService.Current.BdsRoot, c.VersionFolder, "Client");

            // 3) chytrý fallback: najdi první existující verzi s \Client
            var bds = PathSettingsService.Current.BdsRoot;
            try
            {
                if (Directory.Exists(bds))
                {
                    var anyClient = Directory.GetDirectories(bds)
                                             .Select(ver => Path.Combine(ver, "Client"))
                                             .FirstOrDefault(Directory.Exists);
                    if (!string.IsNullOrEmpty(anyClient))
                        return anyClient;
                }
            }
            catch
            {
                // ignoruj – spadneme na poslední nouzový fallback
            }

            // 4) nouzový fallback, ať to nespadne – může (a nemusí) existovat
            return Path.Combine(bds, "Client");
        }

        private static string GetSpExePath(SinglePlayerConfig c)
        {
            string exeName = string.IsNullOrWhiteSpace(c.ExeName) ? "DayZDiag_x64.exe" : c.ExeName.Trim();
            return Path.Combine(GetSpBaseClientDir(c), exeName);
        }

        private static string BuildSpClientArgs(SinglePlayerConfig c)
        {
            string args = (c.ClientArguments ?? "").Trim();
            if (string.IsNullOrWhiteSpace(args))
                args = "-window -nopause -disableCrashReport";

            if (!string.IsNullOrWhiteSpace(c.IngameName))
                args += $" -name={c.IngameName.Trim()}";

            if (!string.IsNullOrWhiteSpace(c.ProfilesFolder))
                args += $" \"-profiles={c.ProfilesFolder}\""; // u profiles uvozovky nech

            // přidej -mission (už správně bez uvozovek)
            var missionFlags = BuildSpMissionArgs(c);
            if (!string.IsNullOrWhiteSpace(missionFlags))
                args += " " + missionFlags;

            // v BuildSpClientArgs(...)
            var mods = c.Mods ?? new List<string>();
            if (mods.Count > 0)
            {
                var modArg = BuildModArgForSp(mods);
                if (!string.IsNullOrWhiteSpace(modArg)) args += " " + modArg;
            }

            return args;
        }


        private static string GetSpConfigKey(SinglePlayerConfig c)
        {
            var exe = GetSpExePath(c);
            return string.Join("|",
                Path.GetFullPath(exe),
                c.MissionParam ?? "",
                c.VersionFolder ?? "");
        }

        private IEnumerable<Process> FindSpClientProcessesForConfig(SinglePlayerConfig c)
        {
            // ocekavany exe + parametry z konfigurace
            var expectedExe = NormalizePath(GetSpExePath(c));
            var expectedProfiles = NormalizePath(c.ProfilesFolder);

            // co nejstabilnější očekávaný text -mission (missionParam je preferované)
            string expectedMission =
                NormalizeMissionArg(!string.IsNullOrWhiteSpace(c.MissionParam)
                    ? c.MissionParam
                    : (!string.IsNullOrWhiteSpace(c.MissionAbsPath)
                        ? Path.Combine("missions", Path.GetFileName(c.MissionAbsPath))
                        : ""));

            var result = new List<Process>();

            try
            {
                var exeName = Path.GetFileName(GetSpExePath(c));
                string wql = $"SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process WHERE Name='{exeName.Replace("'", "''")}'";
                using var searcher = new ManagementObjectSearcher(wql);

                foreach (ManagementObject mo in searcher.Get())
                {
                    var exe = NormalizePath(mo["ExecutablePath"] as string);
                    if (exe != expectedExe) continue; // jiná verze/umístění klienta

                    var cmd = (mo["CommandLine"] as string) ?? "";

                    var procProfiles = NormalizePath(ExtractArg(RxProfiles, cmd));
                    var procMission = NormalizeMissionArg(ExtractArg(RxMission, cmd));

                    bool profilesOk = string.IsNullOrEmpty(expectedProfiles) || procProfiles == expectedProfiles;
                    bool missionOk =
                        string.IsNullOrEmpty(expectedMission) ||
                        procMission.EndsWith(expectedMission, StringComparison.OrdinalIgnoreCase) ||
                        expectedMission.EndsWith(procMission, StringComparison.OrdinalIgnoreCase);

                    if (!(profilesOk && missionOk)) continue;

                    if (int.TryParse(mo["ProcessId"]?.ToString(), out int pid))
                    {
                        try { result.Add(Process.GetProcessById(pid)); } catch { /* mohl skončit */ }
                    }
                }
            }
            catch
            {
                // WMI nemusí být k dispozici → klidně tichý fail; doplníme vlastní PIDy níže
            }

            // doplň běžící procesy, které sis spustil z appky (a máš jejich PIDy)
            var key = GetSpConfigKey(c);
            if (_spClientPidsByConfig.TryGetValue(key, out var pids))
            {
                foreach (var pid in pids.ToList())
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (!p.HasExited) result.Add(p);
                    }
                    catch { /* nic */ }
                }
            }

            // unikátní dle PID
            return result.GroupBy(p => p.Id).Select(g => g.First());
        }

        private int KillSpClientsForConfig(SinglePlayerConfig c, out int fails)
        {
            int ok = 0; fails = 0;
            var key = GetSpConfigKey(c);

            if (_spClientPidsByConfig.TryGetValue(key, out var pids))
            {
                foreach (var pid in pids.ToList())
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (!p.HasExited) { p.Kill(); ok++; }
                    }
                    catch { fails++; }
                    finally { pids.Remove(pid); }
                }
                if (pids.Count == 0) _spClientPidsByConfig.Remove(key);
            }

            foreach (var p in FindSpClientProcessesForConfig(c))
            {
                try { if (!p.HasExited) { p.Kill(); ok++; } }
                catch { fails++; }
            }
            return ok;
        }

        private void BtnSpRun_Click()
        {
            if (listViewConfigsSP.SelectedItems.Count == 0)
            {
                MessageBox.Show("Choose SP configuration!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var cfg = listViewConfigsSP.SelectedItems[0].Tag as SinglePlayerConfig;
            if (cfg == null) return;

            var exePath = GetSpExePath(cfg);
            var workDir = Path.GetDirectoryName(exePath) ?? GetSpBaseClientDir(cfg);
            var args = BuildSpClientArgs(cfg);

            if (!File.Exists(exePath))
            {
                MessageBox.Show("Client exe not found:\n" + exePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    WorkingDirectory = workDir,
                    UseShellExecute = true
                };
                var p = Process.Start(psi);
                if (p != null)
                {
                    var key = GetSpConfigKey(cfg);
                    if (!_spClientPidsByConfig.TryGetValue(key, out var set))
                    {
                        set = new HashSet<int>();
                        _spClientPidsByConfig[key] = set;
                    }
                    set.Add(p.Id);
                }

                // okamžitá obnova UI
                SetSpStatus(true);
                btnSpRun.Enabled = false;
                btnSpStop.Enabled = true;
                MessageBox.Show("Client started.", "Info");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start client:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSpStop_Click()
        {
            if (listViewConfigsSP.SelectedItems.Count == 0)
            {
                MessageBox.Show("Choose SP configuration!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var cfg = listViewConfigsSP.SelectedItems[0].Tag as SinglePlayerConfig;
            if (cfg == null) return;

            int fails;
            int killed = KillSpClientsForConfig(cfg, out fails);

            // obnova UI
            SetSpStatus(false);
            btnSpRun.Enabled = true;
            btnSpStop.Enabled = false;

            MessageBox.Show($"Stopped clients: {killed} (failed: {fails})", "Terminate", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void SetSpStatus(bool running)
        {
            spDotStatus.BackColor = running ? Color.SeaGreen : Color.DarkRed;
            spInfoStatusText.Text = running ? "Running" : "Stopped";
        }


        private void SetListItemRunning(ListViewItem item, bool running)
        {
            if (item == null) return;
            item.UseItemStyleForSubItems = true; // barva/tučnost i pro subitemy
            item.ForeColor = running ? Color.ForestGreen : SystemColors.WindowText;
            item.Font = running ? _lvBold : null; // null = dědí výchozí font ListView
        }

        private async Task AutoCheckAndPromptAsync()
        {
            if (_appClosing || IsDisposed) return;
            if (_autoUpdateCheckBusy) return;
            if (_syncDlg != null) return; // už probíhá ruční sync
            if (DateTime.UtcNow - _lastUpdatePromptUtc < AutoPromptCooldown) return;

            _autoUpdateCheckBusy = true;
            try
            {
                var jobs = new[]
                {
                    new
                    {
                        Name = "Client",
                        Source = PathSettingsService.Current.DayZStableDir,
                        Target = Path.Combine(PathSettingsService.Current.BdsRoot, "MEBOD", "Client")
                    },
                    new
                    {
                        Name = "Server",
                        Source = PathSettingsService.Current.DayZServerStableRoot,
                        Target = Path.Combine(PathSettingsService.Current.BdsRoot, "MEBOD", "Server")
                    }
                };


                var updates = new List<string>();
                foreach (var j in jobs)
                {
                    if (!Directory.Exists(j.Source)) continue;
                    bool need = await Task.Run(() => SourceHasNewerOrMissingFiles(j.Source, j.Target));
                    if (need) updates.Add(j.Name);
                }

                if (updates.Count > 0)
                {
                    _lastUpdatePromptUtc = DateTime.UtcNow; // cooldown
                    var res = MessageBox.Show(
                        "Updates are available for: " + string.Join(", ", updates) + ".\n\nDo you want to sync now?",
                        "Updates available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (res == DialogResult.Yes)
                    {
                        // zavolej tvůj existující sync s progress oknem
                        btnRefreshBaseDayzServer_Click(this, EventArgs.Empty);
                    }
                }
            }
            catch
            {
                // tichý fail – nechceme spamovat
            }
            finally
            {
                _autoUpdateCheckBusy = false;
            }
        }

        // rychlá detekce: vrať true, pokud je ve zdroji něco nového/novějšího/chybějícího v cíli
        // 2) Použij ji v rychlé kontrole
        private static bool SourceHasNewerOrMissingFiles(string srcDir, string dstDir)
        {
            if (!Directory.Exists(srcDir)) return false;
            if (!Directory.Exists(dstDir)) return true;

            foreach (var srcPath in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
            {
                if (IsInIgnoredTopLevel(srcDir, srcPath)) continue; // <<< DŮLEŽITÉ

                string rel = Path.GetRelativePath(srcDir, srcPath);
                string dstPath = Path.Combine(dstDir, rel);

                var src = new FileInfo(srcPath);
                var dst = new FileInfo(dstPath);

                if (!dst.Exists) return true;
                if (src.Length != dst.Length) return true;

                var dt = (src.LastWriteTimeUtc - dst.LastWriteTimeUtc).Duration();
                if (dt.TotalSeconds > 2) return true;
            }
            return false;
        }

        private void SyncProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_appClosing || _syncDlg == null || _syncDlg.IsDisposed) return;
            _syncDlg.SetStatus($"{_syncBaseStatus}\nTime: {_syncStopwatch.Elapsed:mm\\:ss}");
        }

        private void BuildServerInfoPanel()
        {
            pnl_Server_Info.SuspendLayout();
            pnl_Server_Info.Controls.Clear();

            // dovol svislý scroll, když je obsah delší
            pnl_Server_Info.AutoScroll = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Top,                         // ⬅️ místo Fill
                AutoSize = true,                              // ⬅️ nechť se měří podle obsahu
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(8)
            };



            // ==== Selected server =====================================================
            var grpSelected = new GroupBox { Text = "Selected server", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var gridSel = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
            gridSel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridSel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpSelected.Controls.Add(gridSel);

            dotStatus = new Panel { Width = 12, Height = 12, BackColor = Color.DarkRed, Margin = new Padding(0, 3, 6, 0) };
            infoStatusText = new Label { Text = "Stopped", AutoSize = true };
            var statusRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            statusRow.Controls.Add(dotStatus);
            statusRow.Controls.Add(infoStatusText);

            infoName = new Label { AutoSize = true };
            infoType = new Label { AutoSize = true };
            infoVersion = new Label { AutoSize = true };
            infoMission = new Label { AutoSize = true };
            infoPort = new Label { AutoSize = true };

            lnkCfgPath = new LinkLabel { AutoSize = true, Text = "-" };
            lnkCfgOpen = new LinkLabel { AutoSize = true, Text = "[Edit]" };
            var lnkCfgShow = new LinkLabel { AutoSize = true, Text = "[Show]" };
            lnkCfgCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };
            var cfgRow = new FlowLayoutPanel { AutoSize = true };
            cfgRow.Controls.Add(lnkCfgPath);
            cfgRow.Controls.Add(lnkCfgOpen);
            cfgRow.Controls.Add(lnkCfgShow);
            cfgRow.Controls.Add(lnkCfgCopy);

            lnkProfilesPath = new LinkLabel { AutoSize = true, Text = "-" };
            lnkProfilesOpen = new LinkLabel { AutoSize = true, Text = "[Open Folder]" };
            var profRow = new FlowLayoutPanel { AutoSize = true };
            profRow.Controls.Add(lnkProfilesPath);
            profRow.Controls.Add(lnkProfilesOpen);

            void addSel(string k, Control v)
            {
                gridSel.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridSel.Controls.Add(v);
            }

            addSel("Status:", statusRow);
            addSel("Name:", infoName);
            addSel("Type:", infoType);
            addSel("Version:", infoVersion);
            addSel("Mission:", infoMission);
            spInfoIngameName = new Label { AutoSize = true, Text = "-" };
            addSel("In-game name:", spInfoIngameName);
            addSel("Port:", infoPort);
            addSel("Config:", cfgRow);
            addSel("Profiles:", profRow);


            // ==== Quick access to mission files =========================================
            grpQuickMission = new GroupBox { Text = "Mission files", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var gridQM = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
            gridQM.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridQM.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpQuickMission.Controls.Add(gridQM);



            // ==== Crash Logs ==========================================================
            grpCrashLogs = new GroupBox { Text = "Crash Logs", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var gridCL = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
            gridCL.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridCL.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpCrashLogs.Controls.Add(gridCL);

            // řádek: Combo + [Open Folder] [Show] [Backup logs] [Delete]
            cmbCrashLogSelect = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 560 };
            lnkCrashOpen = new LinkLabel { AutoSize = true, Text = "[Open Folder]" };
            lnkCrashShow = new LinkLabel { AutoSize = true, Text = "[Show]" };
            lnkCrashBackup = new LinkLabel { AutoSize = true, Text = "[Backup logs]" };
            lnkCrashDelete = new LinkLabel { AutoSize = true, Text = "[Delete]" };

            var rowCL = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            rowCL.Controls.Add(cmbCrashLogSelect);
            rowCL.Controls.Add(lnkCrashOpen);
            rowCL.Controls.Add(lnkCrashShow);
            rowCL.Controls.Add(lnkCrashBackup);
            rowCL.Controls.Add(lnkCrashDelete);

            void addCL(string k, Control v)
            {
                gridCL.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridCL.Controls.Add(v);
            }
            addCL("Latest logs:", rowCL);




            // řádek Mission: <název> [Open Folder]
            lblQuickMission = new Label { AutoSize = true, Text = "-" };
            lnkQuickMissionOpen = new LinkLabel { AutoSize = true, Text = "[Open Folder]" };
            var missionRow = new FlowLayoutPanel { AutoSize = true };
            missionRow.Controls.Add(lblQuickMission);
            missionRow.Controls.Add(lnkQuickMissionOpen);

            // řádek checkbox + [Edit] vedle sebe
            chkEnableCfgGameplay = new CheckBox { AutoSize = true, Text = "Enable cfggameplay.json" };
            lnkEditCfgGameplay = new LinkLabel { AutoSize = true, Text = "[Edit]" };

            var gameplayRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            gameplayRow.Controls.Add(chkEnableCfgGameplay);
            lnkEditCfgGameplay.Margin = new Padding(8, 0, 0, 0); // malý odsaz za checkboxem
            gameplayRow.Controls.Add(lnkEditCfgGameplay);


            lnkEditCfgGameplay.LinkClicked += (s, e) =>
            {
                if (_selectedConfig == null) return;

                var sp = _selectedConfig.ServerParameters ?? new ServerParams();
                string mission = GetMissionTemplateFromCfg(sp.ConfigFile);
                string missionDir = GetMissionDirectory(_selectedConfig.VersionFolder, mission);

                if (string.IsNullOrWhiteSpace(missionDir) || !Directory.Exists(missionDir))
                {
                    MessageBox.Show("Mission directory not found.", "Info");
                    return;
                }

                string gp = Path.Combine(missionDir, "cfggameplay.json");

                // pokud soubor neexistuje, nabídni vytvoření
                if (!File.Exists(gp))
                {
                    var res = MessageBox.Show(
                        $"cfggameplay.json neexistuje v:\n{missionDir}\n\nChceš vytvořit prázdný soubor?",
                        "Create cfggameplay.json", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (res == DialogResult.Yes)
                    {
                        try
                        {
                            // minimalistický skeleton (validní JSON)
                            File.WriteAllText(gp, "{\r\n}", System.Text.Encoding.UTF8);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Nelze vytvořit cfggameplay.json:\n" + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else return;
                }

                // otevři v editoru (nebo default aplikaci)
                try
                {
                    var editor = PathSettingsService.Current.EditorExePath;
                    if (!string.IsNullOrWhiteSpace(editor) && File.Exists(editor))
                    {
                        Process.Start(new ProcessStartInfo(editor, $"\"{gp}\"")
                        {
                            UseShellExecute = false,
                            WorkingDirectory = missionDir
                        });
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo(gp) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open cfggameplay.json:\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            lnkCrashOpen.LinkClicked += (s, e) =>
            {
                var prof = lnkProfilesPath?.Tag as string;
                if (!string.IsNullOrWhiteSpace(prof) && Directory.Exists(prof))
                    Process.Start("explorer.exe", $"\"{prof}\"");
            };

            lnkCrashShow.LinkClicked += (s, e) =>
            {
                if (cmbCrashLogSelect.SelectedItem is null) return;
                var path = (string)cmbCrashLogSelect.SelectedItem.GetType().GetProperty("Path").GetValue(cmbCrashLogSelect.SelectedItem);

                try
                {
                    var editor = PathSettingsService.Current.EditorExePath;
                    if (!string.IsNullOrWhiteSpace(editor) && File.Exists(editor))
                        Process.Start(new ProcessStartInfo(editor, $"\"{path}\"") { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(path) });
                    else
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open log:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            lnkCrashBackup.LinkClicked += (s, e) =>
            {
                var prof = lnkProfilesPath?.Tag as string;
                if (string.IsNullOrWhiteSpace(prof) || !Directory.Exists(prof)) return;

                var latest = GetLatestLogsPerType(prof);
                if (latest.Count == 0) { MessageBox.Show("No logs to backup.", "Info"); return; }

                string zip = Path.Combine(prof, $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                try
                {
                    using (var za = ZipFile.Open(zip, ZipArchiveMode.Create))
                        foreach (var f in latest) za.CreateEntryFromFile(f.FullName, f.Name, CompressionLevel.Optimal);

                    var ask = MessageBox.Show($"Backup created:\n{zip}\n\nDelete original latest logs?", "Backup logs", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (ask == DialogResult.Yes)
                    {
                        foreach (var f in latest) { try { f.Delete(); } catch { } }
                        PopulateCrashLogsMP(prof);
                    }
                    else
                    {
                        // jen refresh seznamu (pro případ, že se změnily časy)
                        PopulateCrashLogsMP(prof);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Backup failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            lnkCrashDelete.LinkClicked += (s, e) =>
            {
                if (cmbCrashLogSelect.SelectedItem is null) return;
                var path = (string)cmbCrashLogSelect.SelectedItem.GetType().GetProperty("Path").GetValue(cmbCrashLogSelect.SelectedItem);

                var ask = MessageBox.Show($"Delete selected log?\n{path}", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (ask != DialogResult.Yes) return;

                try { File.Delete(path); }
                catch (Exception ex)
                {
                    MessageBox.Show("Delete failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var prof = lnkProfilesPath?.Tag as string;
                if (!string.IsNullOrWhiteSpace(prof)) PopulateCrashLogsMP(prof);
            };


            void addQM(string k, Control v)
            {
                gridQM.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridQM.Controls.Add(v);
            }
            addQM("Mission:", missionRow);
            addQM("Gameplay:", gameplayRow);              // ✅ jediná správná varianta



            lnkQuickMissionOpen.LinkClicked += (s, e) =>
            {
                if (_selectedConfig == null) return;
                var sp = _selectedConfig.ServerParameters ?? new ServerParams();
                string mission = GetMissionTemplateFromCfg(sp.ConfigFile);
                string dir = GetMissionDirectory(_selectedConfig.VersionFolder, mission);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    Process.Start("explorer.exe", $"\"{dir}\"");
                else
                    MessageBox.Show("Mission directory not found.", "Info");
            };

            chkEnableCfgGameplay.CheckedChanged += (s, e) =>
            {
                if (_suppressQuickMissionUi) return;
                if (_selectedConfig == null) return;

                var cfgPath = _selectedConfig.ServerParameters?.ConfigFile;
                if (string.IsNullOrWhiteSpace(cfgPath) || !File.Exists(cfgPath))
                {
                    MessageBox.Show("Config file not found.", "Info");
                    return;
                }

                // zapiš 1/0 do enableCfgGameplayFile
                UpdateOrInsertConfigValueInFile(cfgPath, "enableCfgGameplayFile", chkEnableCfgGameplay.Checked ? "1" : "0", quote: false);
            };

            // ==== Server parameters ===================================================
            grpServerParams = new GroupBox { Text = "Server parameters", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var gridSP = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
            gridSP.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridSP.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpServerParams.Controls.Add(gridSP);

            spExeVal = new Label { AutoSize = true, Text = "-" };
            spCpuVal = new Label { AutoSize = true, Text = "-" };

            // ⬇️ Additional: Label + [Copy] do jednoho řádku
            spAddVal = new Label { AutoSize = true, MaximumSize = new Size(800, 0), Text = "-" };
            lnkSpAddCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };
            var spAddRow = new FlowLayoutPanel { AutoSize = true };
            spAddRow.Controls.Add(spAddVal);
            spAddRow.Controls.Add(lnkSpAddCopy);

            spArgsVal = new Label { AutoSize = true, MaximumSize = new Size(800, 0) };
            lnkSpArgsCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };
            var spArgsRow = new FlowLayoutPanel { AutoSize = true };
            spArgsRow.Controls.Add(spArgsVal);
            spArgsRow.Controls.Add(lnkSpArgsCopy);

            void addSP(string k, Control v)
            {
                gridSP.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridSP.Controls.Add(v);
            }

            addSP("Executable:", spExeVal);
            addSP("CPU count:", spCpuVal);

            // ⬇️ dříve tu bylo: addSP("Additional:", spAddVal);
            addSP("Additional:", spAddRow);
            //addSP("Final args:",  spArgsRow); // necháš-li zakomentované, nic se neroztahuje

            // ==== Client parameters ===================================================
            grpClientParams = new GroupBox { Text = "Client parameters", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var gridCP = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
            gridCP.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridCP.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpClientParams.Controls.Add(gridCP);

            clCfgArgsVal = new Label { AutoSize = true, MaximumSize = new Size(800, 0) };
            clFinalArgsVal = new Label { AutoSize = true, MaximumSize = new Size(800, 0) };
            lnkClCfgCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };
            lnkClFinalCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };

            var rowCfg = new FlowLayoutPanel { AutoSize = true };
            rowCfg.Controls.Add(clCfgArgsVal);
            rowCfg.Controls.Add(lnkClCfgCopy);

            var rowFin = new FlowLayoutPanel { AutoSize = true };
            rowFin.Controls.Add(clFinalArgsVal);
            rowFin.Controls.Add(lnkClFinalCopy);

            void addCP(string k, Control v)
            {
                gridCP.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridCP.Controls.Add(v);
            }

            addCP("Configured args:", rowCfg);
            //addCP("Final args (now):", rowFin);

            // ==== Mods ================================================================
            grpMods = new GroupBox
            {
                Text = "Mods",
                Dock = DockStyle.Top,
                AutoSize = false,
                Padding = new Padding(10),
                Height = 250,
                Visible = false
            };
            lstMods = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            grpMods.Controls.Add(lstMods);

            // ==== Connect =============================================================
            var grpConnect = new GroupBox
            {
                Text = "Connect",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 10, 10, 0),   // ⬅️ nulový spodní padding
                Margin = new Padding(0, 6, 0, 6)       // ⬅️ menší mezery okolo groupboxu
            };
            var gridC = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Top,                   // ⬅️ ne Fill, ať si vezme jen nutnou výšku
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            gridC.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gridC.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grpConnect.Controls.Add(gridC);

            lnkIp = new LinkLabel { AutoSize = true, Text = "-" };
            lnkIpCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };
            var ipRow = new FlowLayoutPanel { AutoSize = true };
            ipRow.Controls.Add(lnkIp);
            ipRow.Controls.Add(lnkIpCopy);

            lblQuick = new Label { AutoSize = true, Text = "-" };
            lnkQuickCopy = new LinkLabel { AutoSize = true, Text = "[Copy]" };
            var quickRow = new FlowLayoutPanel { AutoSize = true };
            quickRow.Controls.Add(lblQuick);
            quickRow.Controls.Add(lnkQuickCopy);

            chkInfoAutoConnect = new CheckBox
            {
                Text = "Auto-connect to localhost",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 0)       // ⬅️ bez spodní mezery
            };

            void addC(string k, Control v)
            {
                gridC.Controls.Add(new Label { Text = k, AutoSize = true, Margin = new Padding(0, 4, 12, 4) });
                gridC.Controls.Add(v);
            }
            addC("IPv4:", ipRow);
            addC("Quick connect:", quickRow);
            gridC.Controls.Add(chkInfoAutoConnect);
            gridC.SetColumnSpan(chkInfoAutoConnect, 2);

            // ==== Actions =============================================================
            var grpActions = new GroupBox { Text = "Actions", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5) };
            var flowA = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            btnInfoRun = new Button { Text = "Run server !", AutoSize = true };
            btnInfoStop = new Button { Text = "Stop server !", AutoSize = true, Enabled = false };
            flowA.Controls.AddRange(new Control[] { btnInfoRun, btnInfoStop });
            grpActions.Controls.Add(flowA);

            // ==== Pořadí v kořenovém panelu ==========================================
            root.Controls.Add(grpActions);
            // vložit do rootu (po grpSelected)

            root.Controls.Add(grpSelected);
            // vlož groupbox do rootu (stejně jako ostatní sekce)
            root.Controls.Add(grpCrashLogs);
            root.Controls.Add(grpQuickMission);   // ⬅️ nová sekce
            root.Controls.Add(grpConnect);


            root.Controls.Add(grpServerParams);
            root.Controls.Add(grpClientParams);
            root.Controls.Add(grpMods);



            // ==== Události ============================================================
            lnkCfgOpen.LinkClicked += (_, __) =>
            {
                if (lnkCfgPath.Tag is string p) OpenConfigFileInVSCode(p);
            };
            lnkCfgShow.LinkClicked += (_, __) =>
            {
                if (lnkCfgPath.Tag is string p && File.Exists(p))
                    Process.Start("explorer.exe", "/select,\"" + p + "\"");
            };
            lnkCfgCopy.LinkClicked += (_, __) =>
            {
                if (lnkCfgPath.Tag is string p) Clipboard.SetText(p);
            };
            lnkProfilesOpen.LinkClicked += (_, __) =>
            {
                if (lnkProfilesPath.Tag is string p && Directory.Exists(p))
                    Process.Start("explorer.exe", p);
            };
            lnkIpCopy.LinkClicked += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(lnkIp.Text) && lnkIp.Text != "-") Clipboard.SetText(lnkIp.Text);
            };
            lnkQuickCopy.LinkClicked += (_, __) =>
            {
                if (lblQuick.Text != "-") Clipboard.SetText(lblQuick.Text);
            };

            lnkSpArgsCopy.LinkClicked += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(spArgsVal.Text)) Clipboard.SetText(spArgsVal.Text);
            };
            lnkClCfgCopy.LinkClicked += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(clCfgArgsVal.Text)) Clipboard.SetText(clCfgArgsVal.Text);
            };
            lnkClFinalCopy.LinkClicked += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(clFinalArgsVal.Text)) Clipboard.SetText(clFinalArgsVal.Text);
            };

            lnkSpAddCopy.LinkClicked += (_, __) =>
            {
                var t = spAddVal.Text;
                if (!string.IsNullOrWhiteSpace(t) && t != "-")
                    Clipboard.SetText(t);
            };

            btnInfoRun.Click += (s, e) => btn_execute_Click(s, e);
            btnInfoStop.Click += (s, e) => btnStopServer_Click(s, e);

            // finálně vlož do panelu
            pnl_Server_Info.Controls.Add(root);
            pnl_Server_Info.ResumeLayout();
        }


        private static IEnumerable<System.Diagnostics.Process> FindServerProcessesForConfig(DayZConfig cfg)
        {
            var exeField = cfg.ServerParameters?.ExeName ?? "DayZServer_x64.exe";
            var configuredFile = Path.GetFileName(exeField);

            // Výjimka: když je v konfiguraci DayZ_BE.exe, reálně spouštíme server přes DayZServer_x64.exe
            var serverExeForDetection = configuredFile.Equals("DayZ_BE.exe", StringComparison.OrdinalIgnoreCase)
                ? "DayZServer_x64.exe"
                : configuredFile;

            var exeNameOnly = serverExeForDetection;
            var expectedExePath = Path.GetFullPath(
                Path.IsPathRooted(serverExeForDetection)
                    ? serverExeForDetection
                    : Path.Combine(cfg.ServerPath ?? "", serverExeForDetection)
            );

            var port = cfg.ServerParameters?.Port ?? 2302;
            var cfgName = Path.GetFileName(cfg.ServerParameters?.ConfigFile ?? "");

            var list = new List<System.Diagnostics.Process>();
            try
            {
                string wql = $"SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process WHERE Name='{exeNameOnly.Replace("'", "''")}'";
                using var searcher = new ManagementObjectSearcher(wql);
                foreach (ManagementObject mo in searcher.Get())
                {
                    string path = (mo["ExecutablePath"] as string) ?? "";
                    if (!string.Equals(Path.GetFullPath(path), expectedExePath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string cmd = (mo["CommandLine"] as string) ?? "";
                    if (!cmd.Contains($"-port={port}", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!string.IsNullOrEmpty(cfgName) && !cmd.Contains($"-config={cfgName}", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (int.TryParse(mo["ProcessId"]?.ToString(), out int pid))
                    {
                        try { list.Add(System.Diagnostics.Process.GetProcessById(pid)); } catch { }
                    }
                }
            }
            catch { }

            return list;
        }

        private static IEnumerable<System.Diagnostics.Process> FindClientProcessesForConfig(DayZConfig cfg)
        {
            var version = cfg.VersionFolder ?? string.Empty;
            var exeName = ResolveClientExeName(cfg);
            var expectedClientExe = Path.GetFullPath(
     Path.Combine(PathSettingsService.Current.BdsRoot, version, "Client", exeName));


            var list = new List<System.Diagnostics.Process>();

            try
            {
                string wql =
     $"SELECT ProcessId, CommandLine, ExecutablePath " +
     $"FROM Win32_Process WHERE Name='{exeName.Replace("'", "''")}'";

                using (var searcher = new ManagementObjectSearcher(wql))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string path = (mo["ExecutablePath"] as string) ?? string.Empty;
                        if (!string.Equals(Path.GetFullPath(path), expectedClientExe, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string cmd = (mo["CommandLine"] as string) ?? string.Empty;

                        bool ok = true;

                        // Pokud je config Modded, čekáme -mod= v cmd line
                        bool isModded = string.Equals(cfg.Type?.Trim(), "Modded", StringComparison.OrdinalIgnoreCase);
                        if (isModded)
                            ok &= cmd.IndexOf("-mod=", StringComparison.OrdinalIgnoreCase) >= 0;

                        // Když klient běží s -connect, zkusíme porovnat port (a host 127.0.0.1)
                        int port = cfg.ServerParameters?.Port ?? 2302;
                        if (cmd.IndexOf("-connect=", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (cmd.IndexOf("-connect=127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0)
                                ok &= cmd.IndexOf($"-port={port}", StringComparison.OrdinalIgnoreCase) >= 0;
                        }

                        if (!ok) continue;

                        if (int.TryParse(mo["ProcessId"]?.ToString(), out int pid))
                        {
                            try { list.Add(System.Diagnostics.Process.GetProcessById(pid)); } catch { /* process už mohl skončit */ }
                        }
                    }
                }
            }
            catch
            {
                // ignore – WMI nemusí být dostupné bez práv; v takovém případě použij jen PIDy, které si ukládáš
            }

            return list;
        }


        private int KillClientsForConfig(DayZConfig cfg, out int fails)
        {
            int ok = 0; fails = 0;
            var key = GetConfigKey(cfg);

            // 1) Klienti, které jsme spustili my (PIDy)
            if (_clientPidsByConfig.TryGetValue(key, out var pids))
            {
                foreach (var pid in pids.ToList())
                {
                    try
                    {
                        var p = System.Diagnostics.Process.GetProcessById(pid);
                        if (!p.HasExited) { p.Kill(); ok++; }
                    }
                    catch { fails++; }
                    finally { pids.Remove(pid); }
                }
                if (pids.Count == 0) _clientPidsByConfig.Remove(key);
            }

            // 2) Záloha – dohledat podle WMI
            foreach (var p in FindClientProcessesForConfig(cfg))
            {
                try { if (!p.HasExited) { p.Kill(); ok++; } }
                catch { fails++; }
            }

            return ok;
        }



        private void UpdateServerInfoPanel(DayZConfig cfgOrNull)
        {
            _selectedConfig = cfgOrNull;



            // --- Nic nevybráno: reset všeho a pryč ---
            if (_selectedConfig == null)
            {
                infoName.Text = infoType.Text = infoVersion.Text = infoMission.Text = infoPort.Text = "-";
                cmbCrashLogSelect?.Items.Clear();
                lnkCrashOpen.Enabled = false;
                lnkCrashShow.Enabled = false;
                lnkCrashBackup.Enabled = false;
                lnkCrashDelete.Enabled = false;
                if (cmbCrashLogSelect != null) cmbCrashLogSelect.Items.Clear();
                if (lnkCrashOpen != null) lnkCrashOpen.Enabled = false;
                if (lnkCrashShow != null) lnkCrashShow.Enabled = false;
                if (lnkCrashBackup != null) lnkCrashBackup.Enabled = false;
                if (lnkCrashDelete != null) lnkCrashDelete.Enabled = false;
                lnkCfgPath.Text = "-"; lnkCfgPath.Tag = null;
                lnkProfilesPath.Text = "-"; lnkProfilesPath.Tag = null;

                if (lnkIp != null) lnkIp.Text = _localIp;
                if (lblQuick != null) lblQuick.Text = "-";

                if (spExeVal != null) spExeVal.Text = "-";
                if (spCpuVal != null) spCpuVal.Text = "-";
                if (spAddVal != null) spAddVal.Text = "-";
                if (spArgsVal != null) spArgsVal.Text = "-";
                if (clCfgArgsVal != null) clCfgArgsVal.Text = "-";
                if (clFinalArgsVal != null) clFinalArgsVal.Text = "-";

                if (grpMods != null) grpMods.Visible = false;
                if (lstMods != null) { lstMods.Items.Clear(); }

                SetStatus(false);
                if (btnInfoRun != null) btnInfoRun.Enabled = false;
                if (btnInfoStop != null) btnInfoStop.Enabled = false;
                if (lblQuickMission != null) lblQuickMission.Text = "-";

                if (lnkQuickMissionOpen != null) lnkQuickMissionOpen.Enabled = false;
                if (lnkEditCfgGameplay != null) lnkEditCfgGameplay.Enabled = false;
                if (chkEnableCfgGameplay != null)
                {
                    _suppressQuickMissionUi = true;
                    chkEnableCfgGameplay.Checked = false;
                    chkEnableCfgGameplay.Enabled = false;
                    _suppressQuickMissionUi = false;
                }
                _crashSigMp = "";


                return;
            }

            var c = _selectedConfig;
            var sp = c.ServerParameters ?? new ServerParams();

            // --- Základní info ---
            string mission = GetMissionTemplateFromCfg(sp.ConfigFile);
            string missionDir = GetMissionDirectory(c.VersionFolder, mission);
            bool missionExists = !string.IsNullOrWhiteSpace(missionDir) && Directory.Exists(missionDir);
            bool isModded = string.Equals(c.Type?.Trim(), "Modded", StringComparison.OrdinalIgnoreCase);

            infoName.Text = c.ServerName ?? c.Name ?? "-";
            infoType.Text = c.Type ?? "-";
            infoVersion.Text = c.VersionFolder ?? "-";
            infoMission.Text = string.IsNullOrWhiteSpace(mission) ? "-" : mission;
            // Quick mission box
            if (lblQuickMission != null) lblQuickMission.Text = string.IsNullOrWhiteSpace(mission) ? "-" : mission;


            if (lnkQuickMissionOpen != null) lnkQuickMissionOpen.Enabled = missionExists;
            if (lnkEditCfgGameplay != null) lnkEditCfgGameplay.Enabled = missionExists;

            // enableCfgGameplayFile -> načti ze souboru a nastav checkbox
            if (chkEnableCfgGameplay != null)
            {
                _suppressQuickMissionUi = true;
                bool enabled = ReadEnableCfgGameplayFromCfg(sp.ConfigFile);
                chkEnableCfgGameplay.Checked = enabled;
                chkEnableCfgGameplay.Enabled = !string.IsNullOrWhiteSpace(sp.ConfigFile) && File.Exists(sp.ConfigFile);
                _suppressQuickMissionUi = false;
            }

            infoPort.Text = (sp.Port == 0 ? 2302 : sp.Port).ToString();

            lnkCfgPath.Text = string.IsNullOrWhiteSpace(sp.ConfigFile) ? "-" : Path.GetFileName(sp.ConfigFile);
            lnkCfgPath.Tag = sp.ConfigFile ?? "";
            lnkProfilesPath.Text = sp.ProfilesFolder ?? "-";
            lnkProfilesPath.Tag = sp.ProfilesFolder ?? "";
            // Refresh Crash Logs podle aktuální složky profiles
            PopulateCrashLogsMP(lnkProfilesPath.Tag as string ?? "");
            // Crash Logs (MP) – naplnit seznam posledních logů podle Profiles
            PopulateCrashLogsMP(sp.ProfilesFolder ?? "");


            if (lnkIp != null) lnkIp.Text = _localIp;
            if (lblQuick != null) lblQuick.Text = $"{_localIp}:{(sp.Port == 0 ? 2302 : sp.Port)}";

            // --- Mods box (jen pro Modded + když jsou) ---
            var mods = sp.Mods ?? new List<string>();
            bool showMods = isModded && mods.Count > 0;
            if (grpMods != null) grpMods.Visible = showMods;
            if (lstMods != null)
            {
                lstMods.BeginUpdate();
                lstMods.Items.Clear();
                if (showMods) lstMods.Items.AddRange(mods.Cast<object>().ToArray());
                lstMods.EndUpdate();
            }

            // --- Server parameters box ---
            if (spExeVal != null) spExeVal.Text = sp.ExeName ?? "DayZServer_x64.exe";
            if (spCpuVal != null) spCpuVal.Text = (sp.CpuCount > 0 ? sp.CpuCount : 1).ToString();
            if (spAddVal != null) spAddVal.Text = string.IsNullOrWhiteSpace(sp.AdditionalParams) ? "-" : sp.AdditionalParams;
            if (lnkSpAddCopy != null) lnkSpAddCopy.Enabled = !string.IsNullOrWhiteSpace(sp.AdditionalParams);
            if (spArgsVal != null) spArgsVal.Text = BuildServerArgs(c);   // finální args pro server

            // --- Client parameters box ---
            string cfgArgs = (c.ClientParameters?.Arguments ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cfgArgs))
                cfgArgs = "-window -nopause -disableCrashReport"; // tvůj fallback
            if (clCfgArgsVal != null) clCfgArgsVal.Text = cfgArgs;

            bool autoConn = chkInfoAutoConnect?.Checked ?? false;
            string host = "127.0.0.1";
            int port = (sp.Port == 0 ? 2302 : sp.Port);
            string? pass = autoConn ? ReadPasswordFromCfg(sp.ConfigFile) : null;

            string finalClientArgs = BuildClientArgs(
                c,
                usePrimaryUid: true,
                host: autoConn ? host : null,
                port: autoConn ? port : (int?)null,
                password: autoConn ? pass : null,
                autoConnect: autoConn
            );
            if (clFinalArgsVal != null) clFinalArgsVal.Text = finalClientArgs;

            // --- Stav běhu + tlačítka ---
            // Kontrola procesů přes WMI asynchronně
            Task.Run(() => FindServerProcessesForConfig(c).Any()).ContinueWith(t =>
             {
                 if (IsDisposed || !IsHandleCreated) return;
                 try
                 {
                     BeginInvoke((System.Windows.Forms.MethodInvoker)(() =>
                     {
                         bool running = (t.Status == TaskStatus.RanToCompletion) && t.Result;
                         SetStatus(running);
                         if (btnInfoRun != null) btnInfoRun.Enabled = !running;
                         if (btnInfoStop != null) btnInfoStop.Enabled = running;
                     }));
                 }
                 catch { /* ignore if closing */ }
             });
        }



        private static IEnumerable<FileInfo> SafeEnumerateFiles(string dir, string pattern)
        {
            try
            {
                return new DirectoryInfo(dir).EnumerateFiles(pattern, SearchOption.TopDirectoryOnly);
            }
            catch { return Enumerable.Empty<FileInfo>(); }
        }

        private static FileInfo LatestOrNull(IEnumerable<FileInfo> files)
            => files.OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();

        private static List<FileInfo> GetLatestLogsPerType(string profilesDir)
        {
            if (string.IsNullOrWhiteSpace(profilesDir) || !Directory.Exists(profilesDir))
                return new List<FileInfo>();

            var result = new List<FileInfo>();

            // 1) *.RPT
            var rpt = LatestOrNull(SafeEnumerateFiles(profilesDir, "*.RPT").Concat(SafeEnumerateFiles(profilesDir, "*.rpt")));
            if (rpt != null) result.Add(rpt);

            // 2) *.ADM
            var adm = LatestOrNull(SafeEnumerateFiles(profilesDir, "*.ADM").Concat(SafeEnumerateFiles(profilesDir, "*.adm")));
            if (adm != null) result.Add(adm);

            // 3) ostatní: debug.log, script_*.log, scriptExt.log, server.log, crash_*.log
            var debugLog = LatestOrNull(SafeEnumerateFiles(profilesDir, "debug.log"));
            var scriptLog = LatestOrNull(SafeEnumerateFiles(profilesDir, "script_*.log"));
            var scriptExt = LatestOrNull(SafeEnumerateFiles(profilesDir, "scriptExt.log"));
            var serverLog = LatestOrNull(SafeEnumerateFiles(profilesDir, "server.log"));
            var crashLog = LatestOrNull(SafeEnumerateFiles(profilesDir, "crash_*.log"));
            var crashReporterLog = LatestOrNull(SafeEnumerateFiles(profilesDir, "CrashReporter.log"));
            var audiobufferdebug = LatestOrNull(SafeEnumerateFiles(profilesDir, "audiobufferdebug.log"));
            if (debugLog != null) result.Add(debugLog);
            if (scriptLog != null) result.Add(scriptLog);
            if (scriptExt != null) result.Add(scriptExt);
            if (serverLog != null) result.Add(serverLog);
            if (crashLog != null) result.Add(crashLog);
            if (crashReporterLog != null) result.Add(crashReporterLog);
            if (audiobufferdebug != null) result.Add(audiobufferdebug);

            // 4) *.mdmp
            var mdmp = LatestOrNull(SafeEnumerateFiles(profilesDir, "*.mdmp"));
            if (mdmp != null) result.Add(mdmp);

            return result
                .GroupBy(f => f.Extension.ToLower() == ".log" ? f.Name.Split('_')[0].ToLower() : f.Extension.ToLower())
                .Select(g => g.OrderByDescending(x => x.LastWriteTimeUtc).First())
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();
        }

        private static string PrettyLogItem(FileInfo f)
        {
            string kind = f.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase)
                ? (f.Name.StartsWith("script_", StringComparison.OrdinalIgnoreCase) ? "script" :
                   f.Name.StartsWith("crash_", StringComparison.OrdinalIgnoreCase) ? "crash" :
                   Path.GetFileNameWithoutExtension(f.Name))
                : f.Extension.Trim('.').ToUpper();

            return $"{kind} — {f.Name}  ({f.LastWriteTime:yyyy-MM-dd HH:mm})";
        }

        private void CrashLogsTimer_Tick(object? sender, EventArgs e)
        {
            if (_appClosing || !IsHandleCreated || IsDisposed) return;

            // --- MP ---
            var mpProfiles = lnkProfilesPath?.Tag as string ?? "";
            if (!string.IsNullOrWhiteSpace(mpProfiles) && Directory.Exists(mpProfiles))
            {
                var latestMp = GetLatestLogsPerType(mpProfiles);
                var sigMp = BuildLogsSignature(latestMp);
                if (!string.Equals(sigMp, _crashSigMp, StringComparison.Ordinal))
                {
                    _crashSigMp = sigMp;
                    PopulateCrashLogsMP(mpProfiles);
                }
            }

            // --- SP ---
            var spProfiles = spInfoProfiles?.Tag as string ?? "";
            if (!string.IsNullOrWhiteSpace(spProfiles) && Directory.Exists(spProfiles))
            {
                var latestSp = GetLatestLogsPerType(spProfiles);
                var sigSp = BuildLogsSignature(latestSp);
                if (!string.Equals(sigSp, _crashSigSp, StringComparison.Ordinal))
                {
                    _crashSigSp = sigSp;
                    PopulateCrashLogsSP(spProfiles);
                }
            }
        }


        private static string BuildLogsSignature(IEnumerable<FileInfo> files)
        {
            return string.Join("|",
                (files ?? Enumerable.Empty<FileInfo>())
                .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(f => $"{f.FullName}|{f.LastWriteTimeUtc.Ticks}|{f.Length}"));
        }





        private void SetStatus(bool running)
        {
            dotStatus.BackColor = running ? Color.SeaGreen : Color.DarkRed;
            infoStatusText.Text = running ? "Running" : "Stopped";
        }


        private void PopulateCrashLogsMP(string profilesDir)
        {
            if (cmbCrashLogSelect == null) return;
            cmbCrashLogSelect.BeginUpdate();
            cmbCrashLogSelect.Items.Clear();

            foreach (var f in GetLatestLogsPerType(profilesDir))
                cmbCrashLogSelect.Items.Add(new { Text = PrettyLogItem(f), Path = f.FullName });

            cmbCrashLogSelect.DisplayMember = "Text";
            cmbCrashLogSelect.ValueMember = "Path";

            if (cmbCrashLogSelect.Items.Count > 0)
                cmbCrashLogSelect.SelectedIndex = 0;

            bool any = cmbCrashLogSelect.Items.Count > 0;
            lnkCrashShow.Enabled = any;
            lnkCrashDelete.Enabled = any;
            lnkCrashBackup.Enabled = Directory.Exists(profilesDir);
            lnkCrashOpen.Enabled = Directory.Exists(profilesDir);

            cmbCrashLogSelect.EndUpdate();
        }

        private void PopulateCrashLogsSP(string profilesDir)
        {
            if (cmbCrashLogSelectSP == null) return;
            cmbCrashLogSelectSP.BeginUpdate();
            cmbCrashLogSelectSP.Items.Clear();

            foreach (var f in GetLatestLogsPerType(profilesDir))
                cmbCrashLogSelectSP.Items.Add(new { Text = PrettyLogItem(f), Path = f.FullName });

            cmbCrashLogSelectSP.DisplayMember = "Text";
            cmbCrashLogSelectSP.ValueMember = "Path";

            if (cmbCrashLogSelectSP.Items.Count > 0)
                cmbCrashLogSelectSP.SelectedIndex = 0;

            bool any = cmbCrashLogSelectSP.Items.Count > 0;
            lnkCrashShowSP.Enabled = any;
            lnkCrashDeleteSP.Enabled = any;
            lnkCrashBackupSP.Enabled = Directory.Exists(profilesDir);
            lnkCrashOpenSP.Enabled = Directory.Exists(profilesDir);

            cmbCrashLogSelectSP.EndUpdate();
        }




        // Vrátí "primární" IPv4 (adaptér UP, ne Loopback/Tunnel, preferuje ten s default gateway)
        private static IPAddress? GetLocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                 n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                 n.NetworkInterfaceType != NetworkInterfaceType.Tunnel))
            {
                var props = ni.GetIPProperties();
                bool hasGw = props.GatewayAddresses.Any(g =>
                    g?.Address?.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(g.Address));

                var ipv4 = props.UnicastAddresses
                    .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;

                if (hasGw && ipv4 != null) return ipv4; // preferuj adaptér s gateway
                if (!hasGw && ipv4 != null)            // fallback – vezmi první IPv4
                    return ipv4;
            }

            // poslední fallback přes DNS
            return Dns.GetHostEntry(Dns.GetHostName())
                      .AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
        }

        private void UpdateLocalIpLabel()
        {
            var ip = GetLocalIPv4();
            _localIp = ip?.ToString() ?? "N/A";
            lbl_ip_adress.Text = _localIp;
            lbl_ip_adress.ForeColor = ip != null ? Color.ForestGreen : Color.DarkRed;
            if (lnkIp != null) lnkIp.Text = _localIp;
        }

        private static string ReadPasswordFromCfg(string cfgPath)
        {
            if (string.IsNullOrWhiteSpace(cfgPath) || !File.Exists(cfgPath)) return "";
            foreach (var line in File.ReadLines(cfgPath))
            {
                var t = line.Trim();
                if (t.StartsWith("password", StringComparison.OrdinalIgnoreCase))
                {
                    // ber jen "password = "..., ne passwordAdmin
                    if (t.StartsWith("passwordAdmin", StringComparison.OrdinalIgnoreCase)) continue;
                    int q1 = t.IndexOf('"'); int q2 = t.LastIndexOf('"');
                    if (q1 >= 0 && q2 > q1) return t.Substring(q1 + 1, q2 - q1 - 1);
                    var eq = t.IndexOf('='); var semi = t.LastIndexOf(';');
                    if (eq >= 0 && semi > eq) return t.Substring(eq + 1, semi - eq - 1).Trim().Trim('"');
                }
            }
            return "";
        }



        // Offline = jakýkoli název mise obsahuje "offline" (bez ohledu na velikost písmen).
        private static bool IsOfflineMission(string mission) =>
            !string.IsNullOrWhiteSpace(mission) &&
            mission.IndexOf("offline", StringComparison.OrdinalIgnoreCase) >= 0;


        // Pomocník pro sestavení -mod=... ze seřazených modů
        // ===== 1) VŽDY absolutní cesty do !LocalWorkshop  =====
        private static string BuildModArg(IReadOnlyList<string> sortedMods)
        {
            if (sortedMods == null || sortedMods.Count == 0) return "";

            // poskládáme absolutní cesty: C:\Program Files (x86)\Steam\steamapps\common\DayZ\!LocalWorkshop\@Mod
            var abs = sortedMods
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m =>
                {
                    var mod = m.Trim().Trim('"');
                    if (!mod.StartsWith("@")) mod = "@" + mod; // pro jistotu doplnit @
                    return Path.Combine(PathSettingsService.Current.LocalWorkshopRoot, mod);
                });

            var modList = string.Join(";", abs) + ";";
            return $"\"-mod={modList}\""; // celý argument v uvozovkách kvůli mezerám
        }


        // --- DEV helper: jen pro testy ---
        // přesune @TestB před @TestA, pokud jsou oba přítomné
        // ZAJISTÍ, že @TestPBO1 je PŘED @TestPBO2 (pokud jsou oba přítomné)
        private static List<string> Dev_ForceTestPbo1BeforePbo2(List<string> mods)
        {
            if (mods == null) return new List<string>();
            var list = mods.ToList();

            static string Norm(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                s = s.Trim().Trim('"').TrimStart('@');
                s = s.Replace(" ", "").Replace("_", "");
                return s.ToLowerInvariant();
            }

            // hledáme case-insensitive bez @, mezer a _
            int idx1 = list.FindIndex(m => Norm(m) == "testpbo1");
            int idx2 = list.FindIndex(m => Norm(m) == "testpbo2");

            // pokud je PBO1 až ZA PBO2, přesuneme PBO1 před PBO2
            if (idx1 >= 0 && idx2 >= 0 && idx1 > idx2)
            {
                var item1 = list[idx1];
                list.RemoveAt(idx1);
                list.Insert(idx2, item1);
            }

            return list;
        }



        private static string GetMissionTemplateFromCfg(string cfgPath)
        {
            if (string.IsNullOrWhiteSpace(cfgPath) || !File.Exists(cfgPath))
                return "";

            try
            {
                foreach (var line in File.ReadLines(cfgPath))
                {
                    var t = line.Trim();
                    if (t.StartsWith("template", StringComparison.OrdinalIgnoreCase))
                    {
                        int first = t.IndexOf('"');
                        int last = t.LastIndexOf('"');
                        if (first >= 0 && last > first)
                            return t.Substring(first + 1, last - first - 1);

                        int eq = t.IndexOf('=');
                        int semi = t.LastIndexOf(';');
                        if (eq >= 0 && semi > eq)
                            return t.Substring(eq + 1, semi - eq - 1).Trim().Trim('"');
                    }
                }
            }
            catch { /* klidně zaloguj */ }

            return "";
        }



        void SetExeIconToPictureBox(string exePath, PictureBox pb)
        {
            if (File.Exists(exePath))
            {
                var ico = Icon.ExtractAssociatedIcon(exePath);
                if (ico != null)
                    pb.Image = ico.ToBitmap();
            }
        }



        // V konstruktoru nebo při načtení formuláře:
        private void LoadVersions()
        {
            string bdsPath = PathSettingsService.Current.BdsRoot;
            if (string.IsNullOrWhiteSpace(bdsPath) || !Directory.Exists(bdsPath))
            {
                MessageBox.Show($"BDS root neexistuje: {bdsPath}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var versionFolders = Directory.GetDirectories(bdsPath)
                                          .Select(Path.GetFileName)
                                          .ToList();



        }
        private List<string> CollectKnownFilters()
        {
            var fromConfigs = configs
                .Where(c => c?.Filters != null)
                .SelectMany(c => c.Filters!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeFilter);

            var fromRegistry = (_filtersGlobal?.MP ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeFilter);

            return fromConfigs
                .Concat(fromRegistry)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }



        private void btn_add_Click(object sender, EventArgs e)
        {
            var knownFilters = CollectKnownFiltersForSuggestions();
            var configForm = new ConfigForm(configToEdit: null, knownFilters: knownFilters);

            if (configForm.ShowDialog() == DialogResult.OK)
            {
                configs.Add(configForm.ResultConfig);
                SaveConfigsToJson(configs);
                FillListView();
                RefreshFilterSelector();
            }
        }

        private void FillListView()
        {
            IEnumerable<DayZConfig> src = configs;

            if (!string.IsNullOrEmpty(_currentFilter) && _currentFilter != AllFiltersItem)
            {
                src = configs.Where(c => (c.Filters ?? new List<string>())
                            .Any(f => string.Equals(f, _currentFilter, StringComparison.CurrentCultureIgnoreCase)));
            }

            FillListView(src.ToList()); // volá tvoji stávající metodu s List<DayZConfig>
        }

        private void FillListView(List<DayZConfig> configs)
        {

            listViewConfigs.BeginUpdate();
            listViewConfigs.Items.Clear();

            foreach (var config in configs)
            {
                string port = config.ServerParameters?.Port.ToString() ?? "";
                string configFile = config.ServerParameters?.ConfigFile ?? "";
                string mission = GetMissionTemplateFromCfg(configFile);   // z .cfg
                bool isModded = string.Equals(config.Type?.Trim(), "Modded", StringComparison.OrdinalIgnoreCase);
                bool offline = IsOfflineMission(mission);

                var item = new ListViewItem(config.Name ?? "");
                item.SubItems.Add(config.VersionFolder ?? "");
                item.SubItems.Add(mission);     // MP mission
                item.SubItems.Add(port);
                item.SubItems.Add(configFile);

                // přiřazení do 4 skupin
                item.Group = isModded
                    ? (offline ? grpModdedOffline : grpModdedOnline)
                    : (offline ? grpVanillaOffline : grpVanillaOnline);

                // (volitelně lehké odlišení barvou)
                if (isModded)
                    item.BackColor = offline ? Color.FromArgb(255, 240, 230) : Color.FromArgb(255, 248, 238);
                else
                    item.BackColor = offline ? Color.FromArgb(230, 240, 255) : Color.FromArgb(238, 248, 255);

                item.Tag = config;
                SetListItemRunning(item, false);
                listViewConfigs.Items.Add(item);
            }

            listViewConfigs.EndUpdate();
        }


        private void SetupListView()
        {
            listViewConfigs.View = View.Details;
            listViewConfigs.FullRowSelect = true;
            listViewConfigs.GridLines = true;

            listViewConfigs.Columns.Clear();
            listViewConfigs.Columns.Add("Name", 250);
            listViewConfigs.Columns.Add("Version", 160);
            listViewConfigs.Columns.Add("MP mission", 200);
            listViewConfigs.Columns.Add("Port", 60);
            // listViewConfigs.Columns.Add("Config file", 220);


            listViewConfigs.ShowGroups = true;
            listViewConfigs.Groups.Clear();

            grpVanillaOnline = new ListViewGroup("Vanilla - Online", HorizontalAlignment.Left);
            grpVanillaOffline = new ListViewGroup("Vanilla - Offline", HorizontalAlignment.Left);
            grpModdedOnline = new ListViewGroup("Modded - Online", HorizontalAlignment.Left);
            grpModdedOffline = new ListViewGroup("Modded - Offline", HorizontalAlignment.Left);

            listViewConfigs.Groups.AddRange(new[]
            {
                grpVanillaOnline, grpVanillaOffline, grpModdedOnline, grpModdedOffline
            });

        }

        private void btn_remove_Click(object sender, EventArgs e)
        {
            if (listViewConfigs.SelectedItems.Count == 0)
            {
                MessageBox.Show("Choose a configuration to delete.", "Info",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var config = (DayZConfig)listViewConfigs.SelectedItems[0].Tag;
            var name = string.IsNullOrWhiteSpace(config?.Name) ? "(unnamed)" : config.Name;

            var confirm = MessageBox.Show(
             $"Do you really want to delete '{name}'?",
             "Delete configuration",
             MessageBoxButtons.YesNo,
             MessageBoxIcon.Question,
     MessageBoxDefaultButton.Button2);


            if (confirm != DialogResult.Yes) return;
            // === also purge disk artifacts (profiles dir + server .cfg) ===
            try
            {
                string prof = config?.ServerParameters?.ProfilesFolder ?? "";
                if (!string.IsNullOrWhiteSpace(prof) && Directory.Exists(prof))
                {
                    Directory.Delete(prof, true); // recursive
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to delete profiles folder:\n{ex.Message}", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            try
            {
                string cfgFile = config?.ServerParameters?.ConfigFile ?? "";
                if (!string.IsNullOrWhiteSpace(cfgFile) && File.Exists(cfgFile))
                {
                    File.Delete(cfgFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to delete server config (.cfg):\n{ex.Message}", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            configs.Remove(config);
            SaveConfigsToJson(configs);
            FillListView();
            RefreshFilterSelector();
        }

        private List<DayZConfig> LoadConfigsFromJson()
        {
            if (!File.Exists(configsFilePath))
                return new List<DayZConfig>();

            string json = File.ReadAllText(configsFilePath);
            var configs = JsonConvert.DeserializeObject<List<DayZConfig>>(json);
            return configs ?? new List<DayZConfig>();
        }

        private void SaveConfigsToJson(List<DayZConfig> configs)
        {
            string json = JsonConvert.SerializeObject(configs, Formatting.Indented);
            File.WriteAllText(configsFilePath, json);
        }

        private void btn_edit_Click(object sender, EventArgs e)
        {
            if (listViewConfigs.SelectedItems.Count == 0) return;
            var selectedConfig = (DayZConfig)listViewConfigs.SelectedItems[0].Tag;
            var knownFilters = CollectKnownFiltersForSuggestions();
            var configForm = new ConfigForm(configToEdit: selectedConfig, knownFilters: knownFilters);

            if (configForm.ShowDialog() == DialogResult.OK)
            {
                int idx = configs.IndexOf(selectedConfig);
                if (idx >= 0)
                {
                    configs[idx] = configForm.ResultConfig;
                    SaveConfigsToJson(configs);
                    FillListView();
                    RefreshFilterSelector();
                }
            }
        }




        private async void btnExecute_Click(object sender, EventArgs e)
        {

            if (listViewConfigs.SelectedItems.Count == 0)
            {
                MessageBox.Show("Choose Configuration!");
                return;
            }

            var config = (DayZConfig)listViewConfigs.SelectedItems[0].Tag;

            var configuredFile = Path.GetFileName(config.ServerParameters?.ExeName ?? "DayZServer_x64.exe");
            var serverExeFile = configuredFile.Equals("DayZ_BE.exe", StringComparison.OrdinalIgnoreCase)
                ? "DayZServer_x64.exe"
                : configuredFile;

            string serverExe = Path.Combine(config.ServerPath ?? "", serverExeFile);
            if (!File.Exists(serverExe))
            {
                MessageBox.Show("The exe file was not found!\n" + serverExe, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            var mods = config.ServerParameters?.Mods ?? new List<string>(); // už seřazené při uložení
            if (mods.Count > 0)
                MessageBox.Show("Mods will be loaded in the following order:\n\n" + string.Join("\n", mods),
                    "Final Mod Load Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
            string cfgFull = config.ServerParameters?.ConfigFile ?? "";
            string args =
                $"-server -config={Path.GetFileName(config.ServerParameters?.ConfigFile)} " +
                $"-port={config.ServerParameters?.Port} " +
                $"-profiles=\"{config.ServerParameters?.ProfilesFolder}\" " +
                $"-cpuCount={config.ServerParameters?.CpuCount} ";

            var modArg = BuildModArg(mods);
            if (!string.IsNullOrEmpty(modArg)) args += modArg + " ";

            if (!string.IsNullOrWhiteSpace(config.ServerParameters?.AdditionalParams))
                args += config.ServerParameters.AdditionalParams + " ";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = serverExe,
                    Arguments = args,
                    WorkingDirectory = config.ServerPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
                MessageBox.Show("Server is executed successfully!");
                if (listViewConfigs.SelectedItems.Count > 0)
                    SetListItemRunning(listViewConfigs.SelectedItems[0], true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during running of server: " + ex.Message);
            }
        }

        private void btn_execute_Click(object sender, EventArgs e)
        {

            // 1) hromadný update DayZDiag_x64.exe (ponecháno)
            string diagBatContent = GenerateDayZDiagCopyBatch();
            string diagBat = Path.Combine(Path.GetTempPath(), $"UpdateDayZDiag_{Guid.NewGuid():N}.bat");
            File.WriteAllText(diagBat, diagBatContent, Encoding.Default);

            try
            {
                var updateDiag = new ProcessStartInfo
                {
                    FileName = diagBat,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb = "runas"
                };
                Process.Start(updateDiag);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update all DayZDiag_x64.exe:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (listViewConfigs.SelectedItems.Count == 0)
            {
                MessageBox.Show("Choose Configuration!");
                return;
            }

            var config = (DayZConfig)listViewConfigs.SelectedItems[0].Tag;

            var mods = config.ServerParameters?.Mods ?? new List<string>();
            //mods = Dev_ForceTestPbo1BeforePbo2(mods);   // <<< tady
            string batContent = GenerateServerBat_WithSorted(config, mods);

            if (mods.Count > 0)
                MessageBox.Show("Mods will be loaded in the following order:\n\n" + string.Join("\n", mods),
                    "Final Mod Load Order", MessageBoxButtons.OK, MessageBoxIcon.Information);



            string tempBat = Path.Combine(Path.GetTempPath(), $"DayZServer_{Guid.NewGuid():N}.bat");
            File.WriteAllText(tempBat, batContent, Encoding.Default);

            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempBat,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                System.Diagnostics.Process.Start(processInfo);
                ShowServerStatus(true);
                if (listViewConfigs.SelectedItems.Count > 0)
                    SetListItemRunning(listViewConfigs.SelectedItems[0], true);

                // zeptej se, zda hned spustit klienta
                var go = MessageBox.Show(
                    "Server is running.\nDo you want to start the client now?",
                    "Start client?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (go == DialogResult.Yes && _selectedConfig != null)
                    StartClient(_selectedConfig);
            }
            catch (Exception ex)
            {
                ShowServerStatus(false);
                MessageBox.Show("Error during executing bat file: " + ex.Message);
            }
        }

        private void StartClient(DayZConfig config)
        {
            // respektuj checkbox "Auto-connect to localhost"
            bool autoConn = chkInfoAutoConnect?.Checked ?? true;

            string host = "127.0.0.1";
            int port = config.ServerParameters?.Port ?? 2302;
            string password = ReadPasswordFromCfg(config.ServerParameters?.ConfigFile);

            // 1) primární hráč (bez forceUID)
            StartClientWithArgs(config, usePrimaryUid: true,
                                host: host, port: port, password: password,
                                autoConnect: autoConn);

            // 2) nabídka druhého hráče (jen Vanilla + offline mise)
            bool isVanilla = string.Equals(config.Type?.Trim(), "Vanilla", StringComparison.OrdinalIgnoreCase);
            bool isOffline = IsOfflineMission(GetMissionTemplateFromCfg(config.ServerParameters?.ConfigFile));

            bool startedSecond = false;
            if (isVanilla)
            {
                var second = MessageBox.Show(
                    "Do you also want to start the second (fake UID) client?",
                    "Second client",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (second == DialogResult.Yes)
                {
                    StartClientWithArgs(config, usePrimaryUid: false,
                                        host: host, port: port, password: password,
                                        autoConnect: autoConn);
                    startedSecond = true;
                }
            }

            MessageBox.Show(startedSecond ? "Both clients started." : "Client started.", "Info");
        }

        // Bezpečné spuštění procesu
        private bool LaunchClient(string exePath, string workingDir, string args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = workingDir,
                UseShellExecute = true
            };
            try { System.Diagnostics.Process.Start(psi); return true; }
            catch (Exception ex) { MessageBox.Show("Failed to start client:\n" + ex.Message, "Error"); return false; }
        }


        private void StartClientWithArgs(
     DayZConfig config,
     bool usePrimaryUid,
     string? host = null,
     int? port = null,
     string? password = null,
     bool autoConnect = false)
        {
            var version = config.VersionFolder;
            string clientDir = Path.Combine(PathSettingsService.Current.BdsRoot, version, "Client");
            string exeName = ResolveClientExeName(config);

            string exePath = Path.Combine(clientDir, exeName);


            if (!File.Exists(exePath))
            {
                MessageBox.Show("Client exe not found:\n" + exePath);
                return;
            }

            string args = BuildClientArgs(config, usePrimaryUid, host, port, password, autoConnect);

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = clientDir,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var proc = Process.Start(psi);
            if (proc != null)
            {
                var key = GetConfigKey(config);
                if (!_clientPidsByConfig.TryGetValue(key, out var set))
                {
                    set = new HashSet<int>();
                    _clientPidsByConfig[key] = set;
                }
                set.Add(proc.Id);
            }
        }

        // Složí argumenty pro klienta (prvni = bez forceUID, druhy/fake = s forceUID)
        // ===== 2) Argumenty klienta (zůstává, jen spoléhá na BuildModArg výše) =====
        private static string BuildClientArgs(
    DayZConfig config,
    bool usePrimaryUid,
    string? host = null,
    int? port = null,
    string? password = null,
    bool autoConnect = false)
        {
            string args = (config.ClientParameters?.Arguments ?? "").Trim();
            if (string.IsNullOrWhiteSpace(args))
                args = "-window -nopause -disableCrashReport"; // fallback na dnešní default

            bool isVanilla = string.Equals(config.Type?.Trim(), "Vanilla", StringComparison.OrdinalIgnoreCase);
            if (isVanilla && !usePrimaryUid)
                args += $" -forceUID={SecondaryUID}";

            var mods = config.ServerParameters?.Mods ?? new List<string>();
            // mods = Dev_ForceTestPbo1BeforePbo2(mods);   // <<< tady

            if (mods.Count > 0) { var modArg = BuildModArg(mods); if (!string.IsNullOrEmpty(modArg)) args += $" {modArg}"; }

            if (autoConnect && !string.IsNullOrWhiteSpace(host) && port.HasValue)
            {
                args += $" -connect={host} -port={port.Value}";
                if (!string.IsNullOrWhiteSpace(password))
                    args += $" -password={password}";
            }

            return args;
        }

        private static string BuildServerArgs(DayZConfig cfg)
        {
            var sp = cfg.ServerParameters ?? new ServerParams();
            var mods = sp.Mods ?? new List<string>();

            string profilesArg = !string.IsNullOrWhiteSpace(sp.ProfilesFolder)
                ? $"\"-profiles={sp.ProfilesFolder}\" "
                : "";

            string modsArg = BuildModArg(mods); // absolutní cesty do !LocalWorkshop
            string cfgName = Path.GetFileName(sp.ConfigFile ?? "");

            string args =
                $"-server -config={cfgName} " +
                $"-port={(sp.Port == 0 ? 2302 : sp.Port)} " +
                $"{profilesArg}-cpuCount={(sp.CpuCount <= 0 ? 1 : sp.CpuCount)} " +
                $"{(string.IsNullOrEmpty(modsArg) ? "" : modsArg + " ")}" +
                $"{(sp.AdditionalParams ?? "")}";

            return args.Trim();
        }




        private static string ExtractCfgString(string line)
        {
            var t = line.Trim();
            int q1 = t.IndexOf('"');
            int q2 = t.LastIndexOf('"');
            if (q1 >= 0 && q2 > q1) return t.Substring(q1 + 1, q2 - q1 - 1);

            int eq = t.IndexOf('=');
            int sc = t.LastIndexOf(';');
            if (eq >= 0 && sc > eq) return t.Substring(eq + 1, sc - eq - 1).Trim().Trim('"');
            return "";
        }

        private static string GetServerPasswordFromCfg(string cfgPath)
        {
            if (string.IsNullOrWhiteSpace(cfgPath) || !File.Exists(cfgPath)) return "";
            foreach (var line in File.ReadLines(cfgPath))
            {
                var t = line.Trim();
                if (t.StartsWith("passwordAdmin", StringComparison.OrdinalIgnoreCase)) continue;
                if (t.StartsWith("password", StringComparison.OrdinalIgnoreCase))
                    return ExtractCfgString(t);
            }
            return "";
        }




        private static string GenerateServerBat_WithSorted(DayZConfig config, List<string> sortedMods)
        {
            var sp = config.ServerParameters ?? new ServerParams();
            var finalMods = (sortedMods != null && sortedMods.Count > 0)
                ? sortedMods
                : (sp.Mods ?? new List<string>());
            var modsList = BuildModArg(finalMods);
            var profiles = !string.IsNullOrWhiteSpace(sp.ProfilesFolder)
                ? $"\"-profiles={sp.ProfilesFolder}\""
                : "";
            var additionalParams = sp.AdditionalParams ?? "";
            var configuredExe = (sp.ExeName ?? "DayZServer_x64.exe").Trim();
            var configuredFile = Path.GetFileName(configuredExe);

            // Výjimka: když je v konfiguraci DayZ_x64.exe, server musí jet přes DayZServer_x64.exe
            var exeName = configuredFile.Equals("DayZ_x64.exe", StringComparison.OrdinalIgnoreCase)
                ? "DayZServer_x64.exe"
                : configuredFile;
            var serverConfigName = Path.GetFileName(sp.ConfigFile);

            var bat = $@"@echo off
set serverName={config.ServerName}
set serverLocation=""{config.ServerPath}""
set serverPort={sp.Port}
set serverConfig={serverConfigName}
set serverCPU={sp.CpuCount}
set serverExe={exeName}

title %serverName%
cd ""%serverLocation%""
echo (%time%) %serverName% starting...
""%serverExe%"" -server -config=%serverConfig% -port=%serverPort% {profiles} {modsList} -cpuCount=%serverCPU% {additionalParams}
echo (%time%) %serverName% exited with code %errorlevel%.
";
            return bat;
        }

        // ===== 3) Batch pro server – také přepnuto na BuildModArg (absolutní cesty) =====
        public static string GenerateServerBat(DayZConfig config)
        {
            var sp = config.ServerParameters ?? new ServerParams();
            var mods = sp.Mods ?? new List<string>();

            var sorted = ModDependencyResolver.ResolveOrder(
                mods,
                PathSettingsService.Current.LocalWorkshopRoot,
                out var missing, out var cycles
            );

            var modsList = BuildModArg(sorted);
            var profiles = !string.IsNullOrWhiteSpace(sp.ProfilesFolder) ? $"\"-profiles={sp.ProfilesFolder}\"" : "";
            var additionalParams = sp.AdditionalParams ?? "";
            var configuredExe = (sp.ExeName ?? "DayZServer_x64.exe").Trim();
            var configuredFile = Path.GetFileName(configuredExe);

            // Výjimka: když je v konfiguraci DayZ_x64.exe, server musí jet přes DayZServer_x64.exe
            var exeName = configuredFile.Equals("DayZ_BE.exe", StringComparison.OrdinalIgnoreCase)
                ? "DayZServer_x64.exe"
                : configuredFile;
            var serverConfigName = Path.GetFileName(sp.ConfigFile);

            var bat = $@"@echo off
set serverName={config.ServerName}
set serverLocation=""{config.ServerPath}""
set serverPort={sp.Port}
set serverConfig={serverConfigName}
set serverCPU={sp.CpuCount}
set serverExe={exeName}

title %serverName%
cd ""%serverLocation%""
echo (%time%) %serverName% starting...
""%serverExe%"" -server -config=%serverConfig% -port=%serverPort% {profiles} {modsList} -cpuCount=%serverCPU% {additionalParams}
echo (%time%) %serverName% exited with code %errorlevel%.
";
            return bat;
        }


        // Najde a ukončí launcher .bat (cmd.exe) patřící aktuální konfiguraci.
        // Vrací počet úspěšně ukončených launcherů, out: počet failů a počet smazaných .bat souborů.
        private int KillServerLauncherBatsForConfig(DayZConfig cfg, out int fails, out int deletedBatFiles)
        {
            int ok = 0;
            fails = 0;
            deletedBatFiles = 0;

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='cmd.exe'");
                foreach (ManagementObject mo in searcher.Get())
                {
                    string cmdLine = (mo["CommandLine"] as string) ?? "";
                    // Hledáme jen launchery z našich temp .bat
                    if (cmdLine.IndexOf("DayZServer_", StringComparison.OrdinalIgnoreCase) < 0 ||
                        cmdLine.IndexOf(".bat", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    string? batPath = TryExtractBatPathFromCmdLine(cmdLine);
                    if (string.IsNullOrWhiteSpace(batPath))
                        continue;

                    // Ověř, že .bat patří právě vybrané konfiguraci (serverLocation/serverConfig[/serverName])
                    if (!BatBelongsToConfig(batPath, cfg))
                        continue;

                    if (int.TryParse(mo["ProcessId"]?.ToString(), out int pid))
                    {
                        try
                        {
                            var p = Process.GetProcessById(pid);
                            if (!p.HasExited) { p.Kill(); ok++; }
                        }
                        catch { fails++; }
                    }

                    try
                    {
                        // úklid temp .bat (volitelné)
                        if (File.Exists(batPath))
                        {
                            File.Delete(batPath);
                            deletedBatFiles++;
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch
            {
                // WMI nemusí být dostupné -> tichý fail; nic dalšího nekazit
            }

            return ok;
        }

        // Vytáhne cestu k DayZServer_*.bat z command line cmd.exe
        private static string? TryExtractBatPathFromCmdLine(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return null;

            // Primárně vytáhnout cestu v uvozovkách obsahující DayZServer_*.bat
            var m = Regex.Match(cmd, "\"([^\"]*DayZServer_[^\"]*?\\.bat)\"", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            // Fallback bez uvozovek
            int idx = cmd.IndexOf("DayZServer_", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int end = cmd.IndexOf(".bat", idx, StringComparison.OrdinalIgnoreCase);
                if (end > idx)
                {
                    int start = cmd.LastIndexOf('"', end);
                    if (start >= 0)
                        return cmd.Substring(start + 1, end + 4 - (start + 1));

                    int space = cmd.LastIndexOf(' ', idx);
                    if (space >= 0)
                        return cmd.Substring(space + 1, end + 4 - (space + 1));
                }
            }
            return null;
        }

        // Ověří, že DayZServer_*.bat byl vygenerován pro daný config (porovná serverLocation/serverConfig[/serverName])
        private static bool BatBelongsToConfig(string batPath, DayZConfig cfg)
        {
            try
            {
                if (!File.Exists(batPath)) return false;

                string? batServerConfig = null;
                string? batServerLoc = null;
                string? batServerName = null;

                foreach (var line in File.ReadLines(batPath))
                {
                    var t = line.Trim();
                    if (t.StartsWith("set serverConfig=", StringComparison.OrdinalIgnoreCase))
                        batServerConfig = t.Substring("set serverConfig=".Length).Trim();
                    else if (t.StartsWith("set serverLocation=", StringComparison.OrdinalIgnoreCase))
                        batServerLoc = t.Substring("set serverLocation=".Length).Trim().Trim('"');
                    else if (t.StartsWith("set serverName=", StringComparison.OrdinalIgnoreCase))
                        batServerName = t.Substring("set serverName=".Length).Trim();

                    if (batServerConfig != null && batServerLoc != null && batServerName != null)
                        break;
                }

                var sp = cfg.ServerParameters ?? new ServerParams();
                string cfgName = Path.GetFileName(sp.ConfigFile ?? "");
                string serverLoc = Path.GetFullPath(cfg.ServerPath ?? "");
                string serverName = cfg.ServerName ?? "";

                if (batServerConfig != null &&
                    !string.Equals(batServerConfig, cfgName, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (batServerLoc != null &&
                    !string.Equals(Path.GetFullPath(batServerLoc), serverLoc, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (batServerName != null &&
                    !string.Equals(batServerName, serverName, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Musí sedět aspoň jeden z rozpoznaných parametrů (abychom nekillovali cizí launchery)
                return (batServerConfig != null) || (batServerLoc != null) || (batServerName != null);
            }
            catch
            {
                return false;
            }
        }

        private void ShowServerStatus(bool running)
        {
            pb_red.Visible = !running;
            pb_green.Visible = running;
        }

        private void listViewConfigs_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cfg = (listViewConfigs.SelectedItems.Count == 0)
         ? null
         : (DayZConfig)listViewConfigs.SelectedItems[0].Tag;

            UpdateServerInfoPanel(cfg);
            // původní ShowServerStatus(false); pryč
        }


        private async void serverStatusTimer_Tick(object sender, EventArgs e)
        {
            if (_statusPollBusy) return;
            _statusPollBusy = true;
            try
            {
                // --- Připravíme položky z UI ---
                var mpItems = listViewConfigs.Items.Cast<ListViewItem>().ToList();
                var spItems = listViewConfigsSP.Items.Cast<ListViewItem>().ToList();

                int mpSelIndex = (listViewConfigs.SelectedIndices.Count > 0) ? listViewConfigs.SelectedIndices[0] : -1;
                int spSelIndex = (listViewConfigsSP.SelectedIndices.Count > 0) ? listViewConfigsSP.SelectedIndices[0] : -1;

                DayZConfig mpSelectedCfg = (mpSelIndex >= 0) ? (DayZConfig)listViewConfigs.Items[mpSelIndex].Tag : null;
                SinglePlayerConfig spSelectedCfg = (spSelIndex >= 0) ? (SinglePlayerConfig)listViewConfigsSP.Items[spSelIndex].Tag : null;

                // --- Těžká práce mimo UI vlákno ---
                var result = await Task.Run(() =>
                {
                    // MP: stav pro všechny položky
                    var mpStates = mpItems.Select(it =>
                    {
                        var cfg = it.Tag as DayZConfig;
                        return (cfg != null) && FindServerProcessesForConfig(cfg).Any();
                    }).ToList();

                    // SP: stav pro všechny položky
                    var spStates = spItems.Select(it =>
                    {
                        var cfg = it.Tag as SinglePlayerConfig;
                        return (cfg != null) && FindSpClientProcessesForConfig(cfg).Any();
                    }).ToList();

                    return (mpStates, spStates);
                });

                // --- Aktualizace MP UI (obarvit vše) ---
                for (int i = 0; i < mpItems.Count; i++)
                    SetListItemRunning(mpItems[i], result.mpStates[i]);

                // Semafor / popisky / tlačítka podle vybrané MP položky
                if (mpSelIndex < 0 || mpSelectedCfg == null)
                {
                    ShowServerStatus(false);
                    lblRunningServer.Text = "The selected server is down.";
                    lblRunningServer.ForeColor = Color.DarkRed;
                    btnStopServer.Enabled = false;

                    SetStatus(false);
                    btnInfoRun.Enabled = false;
                    btnInfoStop.Enabled = false;
                }
                else
                {
                    bool mpSelRunning = result.mpStates[mpSelIndex];

                    ShowServerStatus(mpSelRunning);
                    SetStatus(mpSelRunning);
                    btnInfoRun.Enabled = !mpSelRunning;
                    btnInfoStop.Enabled = mpSelRunning;

                    lblRunningServer.Text = mpSelRunning
                        ? $"Running server: {mpSelectedCfg.ServerName}"
                        : "The selected server is down.";
                    lblRunningServer.ForeColor = mpSelRunning ? Color.Green : Color.DarkRed;
                    btnStopServer.Enabled = mpSelRunning;

                    // Kontext pro ShortcutRules
                    _shortcutCtx = new ShortcutRules.ShortcutContext
                    {
                        MpRunning = mpSelRunning,
                        MpCfgType = mpSelectedCfg.Type
                    };
                    ApplyShortcutsEnabledByRules();
                }

                // --- Aktualizace SP UI (obarvit vše) ---
                for (int i = 0; i < spItems.Count; i++)
                {
                    bool running = result.spStates[i];
                    var it = spItems[i];
                    it.UseItemStyleForSubItems = true;
                    it.ForeColor = running ? Color.ForestGreen : SystemColors.WindowText;
                    it.Font = running ? _lvBold : null;
                }

                // Stav detail panelu pro SP vybranou položku
                if (spSelIndex < 0 || spSelectedCfg == null)
                {
                    SetSpStatus(false);
                    btnSpRun.Enabled = false;
                    btnSpStop.Enabled = false;
                }
                else
                {
                    bool spSelRunning = result.spStates[spSelIndex];
                    SetSpStatus(spSelRunning);
                    btnSpRun.Enabled = !spSelRunning;
                    btnSpStop.Enabled = spSelRunning;
                }
            }
            finally
            {
                _statusPollBusy = false;
            }
        }


        private void ApplyShortcutsEnabledByRules()
        {
            var list = PathSettingsService.Current.Shortcuts ?? new();
            for (int i = 0; i < _shortcutButtons.Length; i++)
            {
                var pb = _shortcutButtons[i];
                if (i < list.Count && File.Exists(list[i].ExePath))
                {
                    // ponechá obraz i kurzor, jen přepne Enabled podle pravidel
                    pb.Enabled = ShortcutRules.IsAllowed(list[i], _shortcutCtx);
                }
                else
                {
                    pb.Enabled = false;
                }
            }
        }



        public static void OpenInConfiguredEditor(string path)
        {
            try
            {
                var ed = PathSettingsService.Current.EditorExePath ?? "";
                if (!string.IsNullOrWhiteSpace(ed) && File.Exists(ed))
                {
                    Process.Start(new ProcessStartInfo(ed)
                    {
                        UseShellExecute = true,
                        Arguments = $"\"{path}\""
                    });
                }
                else
                {
                    // fallback na výchozí asociaci
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Editor nelze spustit: {ex.Message}");
            }
        }

        /// Dorazí DayZ_x64.exe i DayZ_BE.exe navázané na danou konfiguraci.
        /// 1) Primárně podle ExecutablePath v klientském adresáři vybrané verze (spolehlivé pro BE special).
        /// 2) Druhotně podle -connect 127.0.0.1 a -port=<cfg.Port>.
        private static int KillClientsByPathOrPortFallback(DayZConfig cfg, out int fails)
        {
            int ok = 0; fails = 0;

            try
            {
                int port = cfg.ServerParameters?.Port ?? 2302;

                // Kořen klienta pro vybranou verzi
                string version = cfg.VersionFolder ?? string.Empty;
                string clientDir = Path.Combine(PathSettingsService.Current.BdsRoot, version, "Client");
                string dayzExe = Path.GetFullPath(Path.Combine(clientDir, "DayZ_x64.exe"));
                string beExe = Path.GetFullPath(Path.Combine(clientDir, "DayZ_BE.exe"));

                // Hledáme obě jména
                string wql = "SELECT ProcessId, Name, ExecutablePath, CommandLine FROM Win32_Process "
                           + "WHERE Name='DayZ_x64.exe' OR Name='DayZ_BE.exe'";

                using var searcher = new System.Management.ManagementObjectSearcher(wql);
                foreach (System.Management.ManagementObject mo in searcher.Get())
                {
                    string name = (mo["Name"] as string) ?? "";
                    string path = (mo["ExecutablePath"] as string) ?? "";
                    string cmd = (mo["CommandLine"] as string) ?? "";

                    bool match = false;

                    // (A) SHODA PODLE CESTY: běží z našeho klientského adresáře?
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        var full = Path.GetFullPath(path);
                        if (full.Equals(dayzExe, StringComparison.OrdinalIgnoreCase) ||
                            full.Equals(beExe, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                        }
                    }

                    // (B) SHODA PODLE PORTU: -connect 127.0.0.1 a -port=<port>
                    if (!match && !string.IsNullOrEmpty(cmd))
                    {
                        bool hasConnectLocal =
                               cmd.IndexOf("-connect=127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0
                            || cmd.IndexOf("-connect 127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0;

                        bool hasPort =
                               cmd.IndexOf($"-port={port}", StringComparison.OrdinalIgnoreCase) >= 0
                            || cmd.IndexOf($"-port {port}", StringComparison.OrdinalIgnoreCase) >= 0;

                        match = hasConnectLocal && hasPort;
                    }

                    if (!match) continue;

                    if (int.TryParse(mo["ProcessId"]?.ToString(), out int pid))
                    {
                        try
                        {
                            var p = System.Diagnostics.Process.GetProcessById(pid);
                            if (!p.HasExited) { p.Kill(); ok++; }
                        }
                        catch { fails++; }
                    }
                }
            }
            catch
            {
                // tichý fallback
            }

            return ok;
        }
        // Zabije všechny procesy se zadanými názvy (bez ohledu na cestu).
        private static int KillByNames(IEnumerable<string> exeNames, out int fails)
        {
            int ok = 0; fails = 0;
            foreach (var exe in exeNames)
            {
                var nameNoExt = Path.GetFileNameWithoutExtension(exe);
                try
                {
                    foreach (var p in Process.GetProcessesByName(nameNoExt))
                    {
                        try
                        {
                            if (!p.HasExited)
                            {
                                // bezpečnost: neukonči sám sebe
                                if (p.Id != Process.GetCurrentProcess().Id)
                                {
                                    p.Kill();
                                    ok++;
                                }
                            }
                        }
                        catch { fails++; }
                    }
                }
                catch { /* ignore */ }
            }
            return ok;
        }

        // „Nuclear“ kill všech běžných DayZ procesů (klient + BE + diag + avx; server už stejně zabíjíš výš)
        private static int KillAllDayZProcesses(out int fails, bool includeServer = false)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DayZ_x64.exe",
                "DayZ_x64_AVX.exe",
                "DayZ_BE.exe",
                "DayZDiag_x64.exe",
                // přidej další, pokud používáš speciální varianty
            };

            if (includeServer)
            {
                names.Add("DayZServer_x64.exe");
                // případně: names.Add("DayZServer_AVX.exe");
            }

            return KillByNames(names, out fails);
        }


        private void btnStopServer_Click(object sender, EventArgs e)
        {
            if (listViewConfigs.SelectedItems.Count == 0)
            {
                MessageBox.Show("Choose configuration!");
                return;
            }

            var config = (DayZConfig)listViewConfigs.SelectedItems[0].Tag;

            // 1) Kill server EXE (podle vybrané konfigurace)
            var serverProcs = FindServerProcessesForConfig(config).ToArray();
            int sOk = 0, sFail = 0;
            foreach (var proc in serverProcs)
            {
                try { proc.Kill(); sOk++; }
                catch { sFail++; }
            }

            // 2) Kill klienti (PID evidence + WMI s filtrem na cestu – původní logika)
            int cFail;
            int cOk = KillClientsForConfig(config, out cFail);

            // 2b) SPECIFICKÝ PŘÍPAD: server = DayZServer_x64.exe → doraz i DayZ_x64.exe/BE v klientském adresáři (nejen dle -connect)
            var serverExeName = System.IO.Path.GetFileName(config.ServerParameters?.ExeName ?? "");
            if (serverExeName.Equals("DayZServer_x64.exe", StringComparison.OrdinalIgnoreCase))
            {
                int fbFails;
                int fbOk = KillClientsByPathOrPortFallback(config, out fbFails);
                cOk += fbOk;
                cFail += fbFails;
            }

            // 3) Kill launcher .bat (cmd.exe), který by server po čase znovu spustil
            int lFail, lBatDeleted;
            int lOk = KillServerLauncherBatsForConfig(config, out lFail, out lBatDeleted);


            // 4) „Nuclear“ kill všech DayZ klientů (a volitelně i serveru) – aby nic nezůstalo viset
            int dzFails;
            int dzOk = KillAllDayZProcesses(out dzFails, includeServer: false); // server už jsme shodili výše
            // === OKAMŽITÁ OBNOVA UI ===
            ShowServerStatus(false);
            SetStatus(false);
            btnStopServer.Enabled = false;
            btn_execute.Enabled = true;
            btnInfoRun.Enabled = true;
            btnInfoStop.Enabled = false;
            lblRunningServer.Text = "The selected server is down.";
            lblRunningServer.ForeColor = Color.DarkRed;
            if (listViewConfigs.SelectedItems.Count > 0)
                SetListItemRunning(listViewConfigs.SelectedItems[0], false);

            /* MessageBox.Show(
                 $"Stopped server:   {sOk} (failed: {sFail})\n" +
                 $"Stopped clients:  {cOk} (failed: {cFail})\n" +
                 $"Stopped launchers:{lOk} (failed: {lFail})\n" +
                 $"Extra DayZ kills: {dzOk} (failed: {dzFails})\n" +
                 $"Deleted temp .bat:{lBatDeleted}",
                 "Terminate",
                 MessageBoxButtons.OK,
                 MessageBoxIcon.Information);*/
        }



        private string GenerateDayZDiagCopyBatch()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine();

            string bdsPath = PathSettingsService.Current.BdsRoot;
            if (Directory.Exists(bdsPath))
            {
                var versions = Directory.GetDirectories(bdsPath)
                    .Select(Path.GetFileName)
                    .ToArray();

                foreach (var version in versions)
                {
                    string clientDiag = $@"{bdsPath}\{version}\Client\DayZDiag_x64.exe";
                    string serverDiag = $@"{bdsPath}\{version}\Server\DayZDiag_x64.exe";
                    sb.AppendLine($@"copy ""{clientDiag}"" ""{serverDiag}"" /Y");

                    // NOVÉ: kopíruj i klasického klienta
                    string clientStd = $@"{bdsPath}\{version}\Client\DayZ_BE.exe";
                    string serverStd = $@"{bdsPath}\{version}\Server\DayZ_BE.exe";
                    sb.AppendLine($@"copy ""{clientStd}"" ""{serverStd}"" /Y");
                }
            }
            sb.AppendLine();
            sb.AppendLine("REM echo Vsechny klienti (DayZDiag_x64.exe + DayZ_BE.exe) byly aktualizovany.");
            return sb.ToString();
        }

        private void OpenConfigFileInVSCode(string? configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            {
                MessageBox.Show("Config file does not exist.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // známé instalace + fallback na PATH („code“)
            var candidates = new[]
            {
        @"C:\Users\husekpet\AppData\Local\Programs\Microsoft VS Code\Code.exe",
        @"C:\Program Files\Microsoft VS Code\Code.exe",
        @"C:\Program Files (x86)\Microsoft VS Code\Code.exe"
    };

            var exe = candidates.FirstOrDefault(File.Exists);
            try
            {
                if (!string.IsNullOrEmpty(exe))
                {
                    Process.Start(exe, $"\"{configPath}\"");
                }
                else
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "code",
                        Arguments = $"\"{configPath}\"",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
            }
            catch
            {
                // nouzově otevřít asociovaným editorem
                Process.Start(new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
        }


        private void lbl_ip_adress_Click(object sender, EventArgs e)
        {
            // vezmi IP z Tagu; fallback: vytáhni z textu regexem
            var ip = (lbl_ip_adress.Tag as string) ?? "";
            if (string.IsNullOrWhiteSpace(ip))
            {
                var m = Regex.Match(lbl_ip_adress.Text, @"\b\d{1,3}(?:\.\d{1,3}){3}\b");
                ip = m.Success ? m.Value : "";
            }

            if (!string.IsNullOrWhiteSpace(ip))
            {
                try
                {
                    Clipboard.SetText(ip);
                    ipTip.Show("Copied!", lbl_ip_adress, 0, lbl_ip_adress.Height, 1200);
                }
                catch
                {
                    MessageBox.Show("Failed to copy to clipboard.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        static class Paths
            {
                public static string InstallDir => AppContext.BaseDirectory;

                public static string UserConfigDir
                {
                    get
                    {
                        var asm = Assembly.GetExecutingAssembly();
                        var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "BohemiaSolutions";
                        var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? Application.ProductName.Replace(' ', '_');

                        var dir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            company, product);

                        Directory.CreateDirectory(dir);
                        return dir;
                    }
                }

                public static string InstallChangeLog => Path.Combine(InstallDir, "changelog.json");
                public static string UserChangeLog => Path.Combine(UserConfigDir, "changelog.json");

                // (volitelné) admin config – podle toho, zda chceš editor zobrazit jen sobě
                public static string UserAdminCfg => Path.Combine(UserConfigDir, "changelog_admin.json");
        }



        private async void btnRefreshBaseDayzServer_Click(object sender, EventArgs e)
        {
            var jobs = new[]
            {
                new
                {
                    Name = "Client",
                    Source = PathSettingsService.Current.DayZStableDir,
                    Target = Path.Combine(PathSettingsService.Current.BdsRoot, "MEBOD", "Client")
                },
                new
                {
                    Name = "Server",
                    Source = PathSettingsService.Current.DayZServerStableRoot,
                    Target = Path.Combine(PathSettingsService.Current.BdsRoot, "MEBOD", "Server")
                }
            };


            // Kontrola zdrojů
            var missing = jobs.Where(j => !Directory.Exists(j.Source)).ToList();
            if (missing.Count == jobs.Length)
            {
                MessageBox.Show(
                    "Neither source folder exists.\n\n" +
                    string.Join("\n", jobs.Select(j => j.Source)),
                    "Nothing to sync",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (missing.Count > 0)
            {
                var warn = "Some source folders are missing and will be skipped:\n\n" +
                           string.Join("\n", missing.Select(j => $"{j.Name}: {j.Source}"));
                MessageBox.Show(warn, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Připrav progress dialog
            _syncDlg = new ProgressDialog();
            _syncDlg.StartPosition = FormStartPosition.CenterScreen;
            _syncDlg.SetProgress(0);
            _syncDlg.SetStatus("Preparing…\nTime: 00:00");
            _syncDlg.Show(this);

            _syncStopwatch = Stopwatch.StartNew();
            syncProgressTimer.Start();

            // Spočti celkový počet souborů (kvůli % přes oba joby)
            var existingJobs = jobs.Where(j => Directory.Exists(j.Source)).ToArray();
            var fileLists = existingJobs.ToDictionary(
                j => j.Name,
                j => Directory.EnumerateFiles(j.Source, "*", SearchOption.AllDirectories)
                                .Where(p =>
                                {
                                    var rel = Path.GetRelativePath(j.Source, p);
                                    var first = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                                   .FirstOrDefault();
                                    return !string.Equals(first, "!Workshop", StringComparison.OrdinalIgnoreCase)
                                        && !string.Equals(first, "!LocalWorkshop", StringComparison.OrdinalIgnoreCase);
                                })
                                .ToList()
            );
            int grandTotal = fileLists.Sum(kv => kv.Value.Count);
            if (grandTotal == 0)
            {
                syncProgressTimer.Stop();
                _syncStopwatch.Stop();
                _syncDlg.Done("Nothing to sync.");
                _syncDlg.Close(); _syncDlg = null;

                MessageBox.Show("No files found in sources.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // IProgress => UI update (přidáváme jen „holý“ status, čas dopisuje timer)
            var progress = new Progress<(int percent, string status)>(tuple =>
            {
                _syncBaseStatus = tuple.status;
                _syncDlg?.SetProgress(tuple.percent);
                // Text s časem doplní timer
            });

            // Běh na background threadu
            var sb = new StringBuilder();
            int totalCreatedDirs = 0, totalCopied = 0, totalUpdated = 0, totalSkipped = 0, totalFailed = 0;
            try
            {
                int processed = 0;

                foreach (var job in existingJobs)
                {
                    Directory.CreateDirectory(job.Target); // jistota

                    var stats = await Task.Run(() =>
                        SyncOneJobWithProgress(
                            job.Name,
                            job.Source,
                            job.Target,
                            fileLists[job.Name],
                            ref processed,
                            grandTotal,
                            progress
                        ));

                    totalCreatedDirs += stats.CreatedDirs;
                    totalCopied += stats.Copied;
                    totalUpdated += stats.Updated;
                    totalSkipped += stats.Skipped;
                    totalFailed += stats.Failed;

                    sb.AppendLine($"{job.Name}:");
                    sb.AppendLine($"  {job.Source}");
                    sb.AppendLine($"  -> {job.Target}");
                    sb.AppendLine($"  Created folders: {stats.CreatedDirs}");
                    sb.AppendLine($"  New files:       {stats.Copied}");
                    sb.AppendLine($"  Updated files:   {stats.Updated}");
                    sb.AppendLine($"  Skipped:         {stats.Skipped}");
                    sb.AppendLine($"  Errors:          {stats.Failed}");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Synchronization failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                syncProgressTimer.Stop();
                _syncStopwatch?.Stop();
                if (_syncDlg != null)
                {
                    _syncDlg.Done($"Done. Total time: {_syncStopwatch.Elapsed:mm\\:ss}");
                    _syncDlg.Close();
                    _syncDlg = null;
                }
            }

            // Souhrn v EN
            sb.AppendLine("Summary:");
            sb.AppendLine($"  Created folders: {totalCreatedDirs}");
            sb.AppendLine($"  New files:       {totalCopied}");
            sb.AppendLine($"  Updated files:   {totalUpdated}");
            sb.AppendLine($"  Skipped:         {totalSkipped}");
            sb.AppendLine($"  Errors:          {totalFailed}");

            MessageBox.Show(sb.ToString(), "Sync finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private sealed class SyncStats
        {
            public int CreatedDirs;
            public int Copied;
            public int Updated;
            public int Skipped;
            public int Failed;
        }

        private static bool AreFilesDifferent(FileInfo src, FileInfo dst)
        {
            if (!dst.Exists) return true;
            if (src.Length != dst.Length) return true;
            var delta = (src.LastWriteTimeUtc - dst.LastWriteTimeUtc).Duration();
            return delta.TotalSeconds > 2.0; // drobná tolerance
        }

        private static bool IsUnderRootWorkshop(string fullPath, string sourceRoot)
        {
            // první segment relativní cesty je "!Workshop"?
            var rel = Path.GetRelativePath(sourceRoot, fullPath);
            if (string.IsNullOrWhiteSpace(rel)) return false;

            var first = rel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                                  StringSplitOptions.RemoveEmptyEntries)
                           .FirstOrDefault();

            return string.Equals(first, "!Workshop", StringComparison.OrdinalIgnoreCase);
        }

        private SyncStats SyncOneJobWithProgress(
            string jobName,
            string sourceDir,
            string targetDir,
            List<string> allFiles,          // už předpočítané soubory zdroje
            ref int processedGlobal,        // napříč všemi joby
            int grandTotal,                 // součet všech souborů ve všech jobech
            IProgress<(int percent, string status)> progress)
        {
            var stats = new SyncStats();

            foreach (var srcPath in allFiles)
            {
                string rel = Path.GetRelativePath(sourceDir, srcPath);
                string dstPath = Path.Combine(targetDir, rel);
                string dstDir = Path.GetDirectoryName(dstPath)!;

                try
                {
                    if (!Directory.Exists(dstDir))
                    {
                        Directory.CreateDirectory(dstDir);
                        stats.CreatedDirs++;
                    }

                    var srcInfo = new FileInfo(srcPath);
                    var dstInfo = new FileInfo(dstPath);

                    if (!dstInfo.Exists)
                    {
                        File.Copy(srcPath, dstPath, overwrite: false);
                        File.SetLastWriteTimeUtc(dstPath, srcInfo.LastWriteTimeUtc);
                        stats.Copied++;
                    }
                    else
                    {
                        if (AreFilesDifferent(srcInfo, dstInfo))
                        {
                            // odemknout ReadOnly, pokud je
                            if ((dstInfo.Attributes & FileAttributes.ReadOnly) != 0)
                                File.SetAttributes(dstPath, dstInfo.Attributes & ~FileAttributes.ReadOnly);

                            File.Copy(srcPath, dstPath, overwrite: true);
                            File.SetLastWriteTimeUtc(dstPath, srcInfo.LastWriteTimeUtc);
                            stats.Updated++;
                        }
                        else
                        {
                            stats.Skipped++;
                        }
                    }
                }
                catch
                {
                    stats.Failed++;
                }
                finally
                {
                    // progress (globální % napříč oběma joby)
                    processedGlobal++;
                    int percent = Math.Min(100, (int)Math.Round((processedGlobal * 100.0) / Math.Max(1, grandTotal)));

                    // krátký status (čas doplní UI timer v SyncProgressTimer_Tick)
                    string fileName = Path.GetFileName(srcPath);
                    string status = $"[{jobName}] {processedGlobal}/{grandTotal} – {fileName}";

                    progress?.Report((percent, status));
                }
            }

            return stats;
        }

        // ===== Filters (UI + logic) =====
        private void BuildFilterBarUI()
        {
            if (cmbConfigFilter != null) return;

            cmbConfigFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbConfigFilter",
                Width = 300
            };
            btnCreateFilter = new Button
            {
                Text = "Create Filter",
                Name = "btnCreateFilter",
                AutoSize = true
            };
            btnManageFilters = new Button                    // ⬅️ NOVÉ
            {
                Text = "Manage",
                Name = "btnManageFilters",
                AutoSize = true
            };

            // Umístění
            cmbConfigFilter.Location = new Point(listViewConfigs.Left, listViewConfigs.Top);
            btnCreateFilter.Location = new Point(cmbConfigFilter.Right + 8, listViewConfigs.Top - 2);
            btnManageFilters.Location = new Point(btnCreateFilter.Right + 8, btnCreateFilter.Top);   // ⬅️ NOVÉ

            // Rezervuj místo nahoře
            int shift = cmbConfigFilter.Height + 8;
            listViewConfigs.Top += shift;
            listViewConfigs.Height -= shift;

            // Eventy
            cmbConfigFilter.SelectedIndexChanged += (_, __) => ApplyFilterAndRefresh();
            btnCreateFilter.Click += (_, __) => HandleCreateFilter();
            btnManageFilters.Click += (_, __) => HandleManageFilters();   // ⬅️ NOVÉ

            panel1.Controls.Add(cmbConfigFilter);
            panel1.Controls.Add(btnCreateFilter);
            panel1.Controls.Add(btnManageFilters);                        // ⬅️ NOVÉ

            cmbConfigFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnCreateFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnManageFilters.Anchor = AnchorStyles.Top | AnchorStyles.Left; // ⬅️ NOVÉ
        }

        private void HandleManageFilters()
        {
            var allMpFilters = CollectKnownFilters();

            using (var dlg = new ManageFiltersForm(configs, allMpFilters))
            {
                // zapamatuj si, co je teď vybrané
                var prev = (string)(cmbConfigFilter?.SelectedItem ?? AllFiltersItem);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SaveConfigsToJson(configs);

                    // uklidit globál podle reálně použitých
                    var stillUsed = configs
                        .Where(c => c?.Filters != null)
                        .SelectMany(c => c.Filters!)
                        .Select(NormalizeFilter)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    _filtersGlobal.MP = (_filtersGlobal.MP ?? new List<string>())
                        .Where(f => stillUsed.Contains(NormalizeFilter(f)))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    SaveGlobalFilters();

                    // znovu postavit combobox
                    RefreshFilterSelector();

                    // pokud původní filtr už neexistuje, přepni na All
                    if (cmbConfigFilter != null)
                    {
                        if (!cmbConfigFilter.Items.Contains(prev))
                            cmbConfigFilter.SelectedIndex = 0;   // All
                        else
                            cmbConfigFilter.SelectedItem = prev;

                        // ⚠️ ručně srovnat model se stavem UI
                        _currentFilter = (string)(cmbConfigFilter.SelectedItem ?? AllFiltersItem);
                    }

                    // a skutečně překreslit list
                    FillListView();
                }
            }
        }

        // jen reálně použité (do comboboxu)
        private List<string> CollectUsedFilters()
        {
            return configs
                .Where(c => c?.Filters != null)
                .SelectMany(c => c.Filters!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeFilter)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // návrhy (configy ∪ globální registr) – používej pro Create/Edit
        private List<string> CollectKnownFiltersForSuggestions()
        {
            var fromConfigs = configs
                .Where(c => c?.Filters != null)
                .SelectMany(c => c.Filters!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeFilter);

            var fromRegistry = (_filtersGlobal?.MP ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeFilter);

            return fromConfigs
                .Concat(fromRegistry)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


        private void RefreshFilterSelector()
        {
            if (cmbConfigFilter == null) return;

            var distinct = CollectUsedFilters();   // ⬅️ dříve CollectKnownFilters()

            var prev = (string)(cmbConfigFilter.SelectedItem ?? AllFiltersItem);
            _suppressFilterEvents = true;
            try
            {
                cmbConfigFilter.BeginUpdate();
                cmbConfigFilter.Items.Clear();
                cmbConfigFilter.Items.Add(AllFiltersItem);
                foreach (var f in distinct) cmbConfigFilter.Items.Add(f);

                int idx = Math.Max(0, cmbConfigFilter.Items.IndexOf(prev));
                if (cmbConfigFilter.SelectedIndex != idx) cmbConfigFilter.SelectedIndex = idx;
            }
            finally
            {
                cmbConfigFilter.EndUpdate();
                _suppressFilterEvents = false;
            }
        }



        private void ApplyFilterAndRefresh()
        {
            if (_suppressFilterEvents) return;

            _currentFilter = (string)(cmbConfigFilter?.SelectedItem ?? AllFiltersItem);
            FillListView(); // vždy přes centrální filtr
        }

        private void HandleCreateFilter()
        {
            using (var dlg = new CreateFilterForm(configs))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string name = NormalizeFilter(dlg.FilterName);
                if (string.IsNullOrWhiteSpace(name)) return;

                if (!_filtersGlobal.MP.Any(f => f.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    _filtersGlobal.MP.Add(name);
                    SaveGlobalFilters();
                }

                var ids = new HashSet<string>(dlg.SelectedConfigIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                int assigned = 0; // ⬅️ count assignments
                foreach (var cfg in configs)
                {
                    string key = !string.IsNullOrWhiteSpace(cfg.Id) ? cfg.Id : GetConfigKey(cfg);
                    if (!ids.Contains(key)) continue;

                    cfg.Filters ??= new List<string>();
                    if (!cfg.Filters.Any(f => f.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        cfg.Filters.Add(name);
                        assigned++;
                    }
                }

                SaveConfigsToJson(configs);
                RefreshFilterSelector();
                cmbConfigFilter.SelectedItem = name;

                if (assigned == 0)
                {
                    MessageBox.Show(
                        $"Filter \"{name}\" was created and is currently empty.",
                        "Filter created",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _appClosing = true;
            try { NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged; } catch { }
            try { autoUpdateTimer.Stop(); autoUpdateTimer.Dispose(); } catch { }
            try { serverStatusTimer.Stop(); serverStatusTimer.Dispose(); } catch { }
            try { syncProgressTimer.Stop(); syncProgressTimer.Dispose(); } catch { }
            try { if (_syncDlg != null && !_syncDlg.IsDisposed) _syncDlg.Close(); } catch { }
            _syncDlg = null;
            base.OnFormClosing(e);
        }

        private static void CopySp(SinglePlayerConfig target, SinglePlayerConfig src)
        {
            target.Name = src.Name;
            target.Type = src.Type;
            target.VersionFolder = src.VersionFolder;
            target.ClientPath = src.ClientPath;
            target.ServerPath = src.ServerPath;
            target.ExeName = src.ExeName;
            target.ProfilesFolder = src.ProfilesFolder;
            target.MissionParam = src.MissionParam;
            target.MissionAbsPath = src.MissionAbsPath;
            target.ClientArguments = src.ClientArguments;
            target.IngameName = src.IngameName;
            target.Mods = src.Mods?.ToList() ?? new List<string>();
            target.Filters = src.Filters?.ToList() ?? new List<string>();
        }


        private void SelectSpInList(SinglePlayerConfig cfg)
        {
            for (int i = 0; i < listViewConfigsSP.Items.Count; i++)
            {
                if (ReferenceEquals(listViewConfigsSP.Items[i].Tag, cfg))
                {
                    listViewConfigsSP.Items[i].Selected = true;
                    listViewConfigsSP.EnsureVisible(i);
                    break;
                }
            }
        }


        private void btn_add_sp_Click(object sender, EventArgs e)
        {
            var cfg = new SinglePlayerConfig { Name = "New SP config", Type = "Vanilla" };
            if (string.IsNullOrWhiteSpace(cfg.Id))            // ať má nový záznam ID
                cfg.Id = Guid.NewGuid().ToString("N");

            var known = CollectKnownSpFilters();              // nebo CollectAllKnownFilters(), pokud chceš MP+SP
            using var dlg = new SpConfigForm(cfg, known);     // ← tady použij cfg

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var result = dlg.ResultConfig ?? cfg;
                var existing = _spConfigs.FirstOrDefault(x =>
                    string.Equals(x.Id, result.Id, StringComparison.OrdinalIgnoreCase));
                if (existing == null) _spConfigs.Add(result);
                else CopySp(existing, result);

                SaveSpConfigsToJson();
                RefreshSpListView();
                SelectSpInList(result);
                RefreshSpFilterSelector();
            }
        }

        private void btn_edit_sp_Click(object sender, EventArgs e)
        {
            if (listViewConfigsSP.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vyber konfiguraci pro editaci.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var cfg = listViewConfigsSP.SelectedItems[0].Tag as SinglePlayerConfig;
            if (cfg == null) return;

            var clone = System.Text.Json.JsonSerializer.Deserialize<SinglePlayerConfig>(
                System.Text.Json.JsonSerializer.Serialize(cfg, SpJsonOptions), SpJsonOptions)!;

            // ⬇️ PŮVODNĚ: CollectAllKnownFilters();
            var known = CollectKnownSpFilters();          // jen SP filtry

            using var dlg = new SpConfigForm(clone, known);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var result = dlg.ResultConfig ?? clone;
                CopySp(cfg, result);

                SaveSpConfigsToJson();
                RefreshSpListView();
                SelectSpInList(cfg);
                RefreshSpFilterSelector();
            }
        }

        // jen reálně použité SP filtry (pro combobox)
        private List<string> CollectUsedSpFilters()
        {
            return _spConfigs
                .SelectMany(c => c?.Filters ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeFilter)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Používej pro SP: přijímá buď absolutní cesty (C:\...\!LocalWorkshop\@CF)
        // nebo jen názvy ("CF", "@CF") a ty mapuje do !LocalWorkshop.
        private static string BuildModArgForSp(IReadOnlyList<string> mods)
        {
            if (mods == null || mods.Count == 0) return "";

            var abs = mods
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m =>
                {
                    var s = m.Trim().Trim('"');
                    // když je to už absolutní cesta, nech ji být
                    if (Path.IsPathRooted(s)) return s;

                    // jinak je to název → namapuj do !LocalWorkshop
                    if (!s.StartsWith("@")) s = "@" + s;
                    return Path.Combine(PathSettingsService.Current.LocalWorkshopRoot, s);
                });

            var joined = string.Join(";", abs) + ";";
            return $"\"-mod={joined}\""; // celý argument v uvozovkách
        }


        private void btn_remove_sp_Click(object sender, EventArgs e)
        {
            if (listViewConfigsSP.SelectedItems.Count == 0)
            {
                MessageBox.Show("Choose a configuration to delete.", "Info",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (listViewConfigsSP.SelectedItems.Count > 1)
            {
                int count = listViewConfigsSP.SelectedItems.Count;
                var confirmMany = MessageBox.Show(
                    $"Do you really want to delete {count} selected configurations?",
                    "Delete configurations",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirmMany != DialogResult.Yes) return;

                foreach (ListViewItem it in listViewConfigsSP.SelectedItems)
                {
                    if (it.Tag is SinglePlayerConfig c)
                    {
                        // purge profiles dir
                        try
                        {
                            string prof = c?.ProfilesFolder ?? "";
                            if (!string.IsNullOrWhiteSpace(prof) && Directory.Exists(prof))
                                Directory.Delete(prof, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Unable to delete profiles folder for '{c?.Name ?? "(unnamed)"}':\n{ex.Message}",
                                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        _spConfigs.Remove(c);
                    }
                }
            }
            else
            {
                var cfg = listViewConfigsSP.SelectedItems[0].Tag as SinglePlayerConfig;
                if (cfg == null) return;

                var name = string.IsNullOrWhiteSpace(cfg.Name) ? "(unnamed)" : cfg.Name;
                var confirmOne = MessageBox.Show(
                    $"Do you really want to delete '{name}'?",
                    "Delete configuration",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirmOne != DialogResult.Yes) return;

                // purge profiles dir
                try
                {
                    string prof = cfg?.ProfilesFolder ?? "";
                    if (!string.IsNullOrWhiteSpace(prof) && Directory.Exists(prof))
                        Directory.Delete(prof, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to delete profiles folder:\n{ex.Message}", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                _spConfigs.Remove(cfg);
            }

            SaveSpConfigsToJson();
            RefreshSpListView();
        }


        private void tsbHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
              "Documentation link will be added here.\n\n",
              "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsbSettings_Click(object sender, EventArgs e)
        {
            using var dlg = new PathsSetupForm(PathSettingsService.Current);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {

                // po uložení můžeš refreshnout cokoliv navázaného na cesty
                // ReloadWorkshopList();
                // RefreshSymlinksStatus();
            }
            ApplyShortcutsToButtons();
        }

        private void tsTop_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void tsbEditChangeLog_Click(object sender, EventArgs e)
        {
            // lazy-load + seed (první běh vytvoří hash pro "adminPH")
            _changelogAdminCfg ??= ChangelogAuthStorage.LoadOrSeed("adminPH");

            if (!_isChangelogAdmin)
            {
                using var login = new AdminLoginForm();
                if (login.ShowDialog(this) != DialogResult.OK) return;

                if (!ChangelogAuthStorage.Verify(login.EnteredPassword, _changelogAdminCfg))
                {
                    MessageBox.Show(this, "Invalid password.", "Access denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _isChangelogAdmin = true;
            }

            using var dlg = new ChangeLogEditorForm();
            dlg.VersionSaved += ver => BeginInvoke((Action)(() =>
            {
                _currentVersion = ver;
                lblVersion.Text = $"v{_currentVersion} (Changelog)";
            }));
            dlg.ShowDialog(this);
            UpdateFooterVersionFromJson(); // jistota po zavření
        }

        private void UpdateFooterVersionFromJson()
        {
            try
            {
                _changeLog = Bohemia_Solutions.Services.ChangeLogStorage.LoadOrCreate();

                var latest = _changeLog.Versions
                    .OrderByDescending(v => v.Date)
                    .ThenByDescending(v => v.Version, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

                if (latest != null)
                {
                    _currentVersion = latest.Version;
                    lblVersion.Text = $"v{_currentVersion} (Changelog)";
                }
                else
                {
                    // fallback – pokud by changelog neměl žádné verze
                    lblVersion.Text = $"v{_currentVersion} (Changelog)";
                }
            }
            catch
            {
                // fallback – když se JSON nepodaří načíst
                lblVersion.Text = $"v{_currentVersion} (Changelog)";
            }
        }


        private void tp_Multiplayer_Click(object sender, EventArgs e)
        {

        }
    }
}
