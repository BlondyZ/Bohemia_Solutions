using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Bohemia_Solutions.Models;
using System.Diagnostics;

namespace Bohemia_Solutions
{
    public partial class PathsSetupForm : Form
    {
        private readonly Dictionary<TextBox, (PictureBox Pic, bool IsFile)> _map = new();

        private TextBox txtWorkshop = new();
        private TextBox txtLocalWorkshop = new();
        private TextBox txtStable = new();
        private TextBox txtExp = new();
        private TextBox txtServerStable = new();
        private TextBox txtServerExp = new();
        private TextBox txtInternal = new();
        private TextBox txtServerInternal = new();
        private int _nextRow = 0;
        private Button btnSave = new() { Text = "Save and continue", AutoSize = true };
        private Button btnCancel = new() { Text = "Cancel", AutoSize = true };
        private Button btnOpenConfig = new() { Text = "Open config folder", AutoSize = true };
        private TextBox txtBdsRoot = new();
        private TextBox txtWorkDrive = new();
        private TextBox txtMikeroExtract = new();
        private TextBox txtVsCode = new();

        private TextBox txtEditor = new();
        private TextBox[] shortcutTbs = new TextBox[10];
        private PictureBox[] shortcutIcons = new PictureBox[10];


        private bool _localWorkshopPromptShown = false;
        private System.Windows.Forms.Timer liveValidateTimer = new() { Interval = 400 };
        private bool dirty = false;

        private const string MikeroDefaultExtractPbo =@"C:\Program Files (x86)\Mikero\DePboTools\bin\ExtractPbo.exe";


        private void EnsureMikeroExtractIfMissing()
        {
            var current = (txtMikeroExtract.Text ?? "").Trim();

            // 1) Pokud textbox prázdný nebo neplatný, zkus výchozí cestu
            if (string.IsNullOrWhiteSpace(current) || !File.Exists(current))
            {
                if (File.Exists(MikeroDefaultExtractPbo))
                {
                    txtMikeroExtract.Text = MikeroDefaultExtractPbo;
                    dirty = true;
                    ValidateAll();
                    return;
                }
            }
            else
            {
                // už je vyplněno a soubor existuje
                return;
            }

            // 2) Pořád nic — nabídni stažení
            var msg =
                "ExtractPbo.exe was not found.\n\n" +
                "I tried the default installation path:\n" +
                MikeroDefaultExtractPbo + "\n\n" +
                "Do you want to open the Mikero Tools website now?\n\n" +
                "On the website, click \"Download AIO\" and during the installer simply press \"Ignore License\".\n\n" +
                "Expected path after installation:\n" +
                MikeroDefaultExtractPbo;

            var res = MessageBox.Show(this, msg, "Mikero Tools required",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (res == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://www.mikero.tools/") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Unable to open website:\n" + ex.Message,
                        "Open URL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        public PathsSetupForm(PathSettings initial)
        {
            Text = "Setting up paths";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 850);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            SizeGripStyle = SizeGripStyle.Hide;
            MaximizeBox = false;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // obsah
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); // tlačítka
            Controls.Add(root);

            // === Obsah (Table) ===
            // === Obsah (vertikální stack řádků) ===
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 0,
                Padding = new Padding(12, 12, 12, 0),
                AutoScroll = true,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // jediný sloupec přes celou šířku
            root.Controls.Add(stack, 0, 0);

            BuildShortcutsUI();
            void AddRow(string label, TextBox tb, bool pickFile = false)
            {
                var rowPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 4,
                    RowCount = 1,
                    Height = 32,
                    AutoSize = false,
                    Margin = new Padding(0, 0, 0, 6),
                    Padding = new Padding(0)
                };
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                var lbl = new Label
                {
                    Text = label,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 6, 6, 6),
                    MaximumSize = new Size(190, 0)
                };

                tb.Dock = DockStyle.Fill;
                tb.Margin = new Padding(0, 3, 6, 3);
                tb.TextChanged += (s, e) => { dirty = true; };

                var btn = new Button
                {
                    Text = "Browse...",
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(0, 3, 6, 3),
                    Anchor = AnchorStyles.Left,
                    Width = 80,
                    Height = 23,
                };

                // 👉 pro soubor použij OpenFileDialog, pro složku FolderBrowser
                btn.Click += (s, e) =>
                {
                    if (pickFile) BrowseExeInto(tb);
                    else Browse(tb);
                };

                var pic = new PictureBox
                {
                    Size = new Size(18, 18),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Margin = new Padding(0, 7, 0, 0)
                };

                rowPanel.Controls.Add(lbl, 0, 0);
                rowPanel.Controls.Add(tb, 1, 0);
                rowPanel.Controls.Add(btn, 2, 0);
                rowPanel.Controls.Add(pic, 3, 0);

                stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
                stack.Controls.Add(rowPanel);

                // 💡 zapamatuj si, jestli je to soubor nebo složka
                _map[tb] = (pic, pickFile);
            }
            // Naplnění
            txtWorkshop.Text = initial.WorkshopRoot;
            txtLocalWorkshop.Text = initial.LocalWorkshopRoot;
            txtStable.Text = initial.DayZStableDir;
            txtExp.Text = initial.DayZExperimentalDir;
            txtServerStable.Text = initial.DayZServerStableRoot;
            txtServerExp.Text = initial.DayZServerExpRoot;
            txtInternal.Text = initial.DayZInternalDir;
            txtServerInternal.Text = initial.DayZServerInternalRoot;
            txtBdsRoot.Text = initial.BdsRoot;
            txtWorkDrive.Text = initial.WorkDriveRoot;
            txtMikeroExtract.Text = initial.MikeroExtractPbo;


            var rows = new (string Label, TextBox Tb, bool IsFile)[] {
               ("!Workshop:",            txtWorkshop,       false),
               ("!LocalWorkshop:",       txtLocalWorkshop,  false),
               ("DayZ (Stable):",        txtStable,         false),
               ("DayZ (Experimental):",  txtExp,            false),
               ("DayZServer (Stable):",  txtServerStable,   false),
               ("DayZ Server Exp:",      txtServerExp,      false),
               ("DayZ Internal:",        txtInternal,       false),
               ("DayZ Server Internal:", txtServerInternal, false),
               ("BDS root:",             txtBdsRoot,        false),
               ("Work drive root:",      txtWorkDrive,      false),
               // 👉 tady je změna: Mikero je SOUBOR
               ("Mikero ExtractPbo:",    txtMikeroExtract,  true),
            };

            stack.SuspendLayout();
            foreach (var r in rows) AddRow(r.Label, r.Tb, r.IsFile);
            stack.ResumeLayout(true);




            // === Panel s tlačítky ===
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12)
            };

            buttons.Controls.Add(btnOpenConfig);
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
           
            root.Controls.Add(buttons, 0, 1);

            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnSave.Click += (s, e) => OnSave();
            btnOpenConfig.Click += (s, e) => OpenConfigFolder();
            // Live validace
            liveValidateTimer.Tick += (s, e) =>
            {
                if (!dirty) return;
                dirty = false;
                ValidateAll();
                EnsureLocalWorkshopIfMissing(); // ⬅️ už existuje
                EnsureMikeroExtractIfMissing(); // ⬅️ PŘIDAT
            };

            liveValidateTimer.Start();

            // Inicializační validace po načtení defaultů
            Shown += (s, e) => ValidateAll();
        }

        private void BuildShortcutsUI()
        {
            // Skupina pro Editor + Shortcuts
            var group = new GroupBox
            {
                Text = "Quick shortcuts & Editor",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10, 6, 10, 10)
            };

            var grid = new TableLayoutPanel
            {
                ColumnCount = 4,
                AutoSize = true,
                Dock = DockStyle.Top
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // #
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));         // path
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // browse
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // icon/poznámka

            // Editor řádek
            grid.RowStyles.Add(new RowStyle());
            grid.Controls.Add(new Label { Text = "Editor:", AutoSize = true }, 0, 0);
            txtEditor.Width = 480;
            grid.Controls.Add(txtEditor, 1, 0);
            var btnEd = new Button { Text = "Browse…" };
            btnEd.Click += (_, __) => BrowseExeInto(txtEditor);
            grid.Controls.Add(btnEd, 2, 0);
           // grid.Controls.Add(new Label { Text = "Eg. VS Code, Notepad++…", AutoSize = true }, 3, 0);

            // Shortcuts 1..10
            var s = PathSettingsService.Current;
            for (int i = 0; i < 10; i++)
            {
                grid.RowStyles.Add(new RowStyle());

                var tb = new TextBox { Width = 660 };
                var pb = new PictureBox
                {
                    Width = 24,
                    Height = 24,
                    BorderStyle = BorderStyle.None,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Margin = new Padding(6, 3, 3, 3),

                };

                int row = i + 1;
                grid.Controls.Add(new Label { Text = $"Shortcut - {i + 1}:", AutoSize = true }, 0, row);
                grid.Controls.Add(tb, 1, row);
                var btn = new Button { Text = "Browse…" };
                btn.Click += (_, __) => { BrowseExeInto(tb); UpdateIcon(tb.Text, pb); };
                grid.Controls.Add(btn, 2, row);
                grid.Controls.Add(pb, 3, row);

                shortcutTbs[i] = tb;
                shortcutIcons[i] = pb;

                tb.TextChanged += (_, __) => UpdateIcon(tb.Text, pb);

                // předvyplnění z konfigurace
                if (i < (s.Shortcuts?.Count ?? 0))
                    tb.Text = s.Shortcuts[i]?.ExePath ?? "";
            }

            txtEditor.Text = s.EditorExePath ?? "";

            group.Controls.Add(grid);
            Controls.Add(group);
        }

        private void BrowseExeInto(TextBox tb)
        {
            using var ofd = new OpenFileDialog { Filter = "Executable (*.exe)|*.exe|All files|*.*" };
            if (File.Exists(tb.Text)) ofd.FileName = tb.Text;
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                tb.Text = ofd.FileName;
                dirty = true;
            }
        }

        private void UpdateIcon(string exePath, PictureBox pb)
        {
            try
            {
                if (File.Exists(exePath))
                {
                    var ico = Icon.ExtractAssociatedIcon(exePath);
                    pb.Image = ico?.ToBitmap();
                }
                else pb.Image = null;
            }
            catch { pb.Image = null; }
        }


        private void Browse(TextBox tb)
        {
            using var dlg = new FolderBrowserDialog();
            if (Directory.Exists(tb.Text)) dlg.SelectedPath = tb.Text;
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                tb.Text = dlg.SelectedPath;
                dirty = true;
            }
        }

        private void OnSave()
        {
            var s = new PathSettings
            {
                WorkshopRoot = txtWorkshop.Text.Trim(),
                LocalWorkshopRoot = txtLocalWorkshop.Text.Trim(),
                DayZStableDir = txtStable.Text.Trim(),
                DayZExperimentalDir = txtExp.Text.Trim(),
                DayZServerStableRoot = txtServerStable.Text.Trim(),
                DayZServerExpRoot = txtServerExp.Text.Trim(),
                DayZInternalDir = txtInternal.Text.Trim(),
                DayZServerInternalRoot = txtServerInternal.Text.Trim()
            };
            s.BdsRoot = txtBdsRoot.Text.Trim();
            s.WorkDriveRoot = txtWorkDrive.Text.Trim();
            s.MikeroExtractPbo = txtMikeroExtract.Text.Trim();

            s.EditorExePath = txtEditor.Text.Trim();
            s.Shortcuts = shortcutTbs
                .Select(tb => (tb.Text ?? "").Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Take(10)
                .Select(p => new ShortcutItem
                {
                    ExePath = p,
                    WorkingDir = Path.GetDirectoryName(p) ?? "",
                    Arguments = "" // necháváme volitelné do budoucna
                })
                .ToList();


            if (!PathSettingsService.IsComplete(s))
            {
                MessageBox.Show(this, "Please fill in all the paths.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Pokud LocalWorkshop chybí a Workshop existuje, nabídneme vytvoření & kopii
            if (!Directory.Exists(s.LocalWorkshopRoot) && Directory.Exists(s.WorkshopRoot))
            {
                _localWorkshopPromptShown = false; // dovol ještě jednou prompt při Save
                EnsureLocalWorkshopIfMissing();

                // po případném kopírování přenačti lokální proměnnou
                if (!Directory.Exists(s.LocalWorkshopRoot))
                {
                    MessageBox.Show(this, "LocalWorkshop folder was not created. Please correct the path.",
                        "Invalid paths", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }


            if (!PathSettingsService.AllExisting(s))
            {
                MessageBox.Show(this, "Some of the specified folders do not exist. Please correct the paths - the file will not be saved until they are all checked.",
                    "Invalid paths", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PathSettingsService.Save(s);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OpenConfigFolder()
        {
            try
            {
                var path = PathSettingsService.ConfigFilePath;         // kam se ukládá paths.json
                var dir = Path.GetDirectoryName(path)!;

                // složku případně vytvoř, ať Explorer vždy otevře něco smysluplného
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Pokud soubor už existuje, rovnou ho označ; jinak otevři jen složku
                if (File.Exists(path))
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                else
                    Process.Start("explorer.exe", dir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to open config folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ValidateAll()
        {
            foreach (var kv in _map)
            {
                var tb = kv.Key;
                var (pic, isFile) = kv.Value;

                bool ok = !string.IsNullOrWhiteSpace(tb.Text)
                          && (isFile ? File.Exists(tb.Text) : Directory.Exists(tb.Text));

                pic.Image = CreateStatusIcon(ok);
                tb.BackColor = ok ? SystemColors.Window : Color.MistyRose;
            }

            btnSave.Enabled = _map.All(kv =>
            {
                var tb = kv.Key;
                var (pic, isFile) = kv.Value;
                return !string.IsNullOrWhiteSpace(tb.Text)
                       && (isFile ? File.Exists(tb.Text) : Directory.Exists(tb.Text));
            });
        }



        private void EnsureLocalWorkshopIfMissing()
        {
            // čti aktuální hodnoty z textboxů
            var workshop = txtWorkshop.Text?.Trim();
            var local = txtLocalWorkshop.Text?.Trim();

            if (_localWorkshopPromptShown) return;
            if (string.IsNullOrWhiteSpace(local) || string.IsNullOrWhiteSpace(workshop)) return;

            if (Directory.Exists(local) && Directory.EnumerateFileSystemEntries(local).Any())
                return; // OK – něco tam je


            // pokud zdroj (!Workshop) neexistuje, stejně nepomůžeme
            if (!Directory.Exists(workshop)) return;

            _localWorkshopPromptShown = true; // dále už neotravovat (dokud znovu neotevřu dialog)

            var msg = $"Folder \"{local}\" was not found.\n\n" +
                      $"Do you want to create it and copy the content from:\n\"{workshop}\" ?";
            var res = MessageBox.Show(this, msg, "LocalWorkshop not found",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                // modal progress & copy
                var ok = DirectoryCopyProgressDialog.ShowAndCopy(this, workshop, local);

                // po dokončení znovu ověř a přebarvi
                if (ok)
                {
                    dirty = true;
                    ValidateAll();
                }
            }
        }



        // Vytvoří jednoduchou ✓ / ✗ ikonku (18x18) pro PictureBox
        private static Bitmap CreateStatusIcon(bool ok)
        {
            var bmp = new Bitmap(18, 18);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);

            var circle = ok ? Color.FromArgb(32, 178, 70) : Color.FromArgb(220, 0, 0);
            using var b = new SolidBrush(circle);
            using var penWhite = new Pen(Color.White, 2);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillEllipse(b, 1, 1, 16, 16);

            if (ok)
            {
                // ✓
                g.DrawLines(penWhite, new[]
                {
                    new Point(4,10),
                    new Point(8,14),
                    new Point(14,5)
                });
            }
            else
            {
                // ✗
                g.DrawLine(penWhite, 4, 4, 14, 14);
                g.DrawLine(penWhite, 14, 4, 4, 14);
            }
            return bmp;
        }

        /// <summary>
        /// Vrací true, pokud je vše nastaveno a můžeme pokračovat do aplikace.
        /// </summary>
        public static bool EnsureConfiguredAndShowIfNeeded()
        {
            var current = PathSettingsService.Current;
            var needDialog = !PathSettingsService.IsComplete(current) || !PathSettingsService.AllExisting(current);

            if (!needDialog) return true;

            using var form = new PathsSetupForm(current);
            var result = form.ShowDialog();
            return result == DialogResult.OK
                   && PathSettingsService.IsComplete(PathSettingsService.Current)
                   && PathSettingsService.AllExisting(PathSettingsService.Current);
        }

        private void PathsSetupForm_Load(object sender, EventArgs e)
        {
            Shown += (s, e) => { ValidateAll(); EnsureLocalWorkshopIfMissing(); EnsureMikeroExtractIfMissing(); };

        }
    }
}
