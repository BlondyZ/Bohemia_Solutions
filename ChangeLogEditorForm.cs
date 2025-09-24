using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Bohemia_Solutions.Models;
using Bohemia_Solutions.Services;
using System.Diagnostics;

namespace Bohemia_Solutions
{
    public partial class ChangeLogEditorForm : Form
    {
        private ChangeLog _log;
        private ChangeLogVersion _current;
        private BindingList<ChangeItem> _items = new();
        public event Action<string> VersionSaved;  // poslední uložená/nejnovější verze


        public ChangeLogEditorForm()
        {
            InitializeComponent();
            this.Load += ChangeLogEditorForm_Load;
            dtpDate.Format = DateTimePickerFormat.Custom;
            dtpDate.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpDate.ShowUpDown = true; // místo dropdown kalendáře (pohodlnější pro čas)

            // 1) DataGridView pojistky
            gridItems.DataError += (s, e) => e.ThrowException = false;
            gridItems.DefaultValuesNeeded += GridItems_DefaultValuesNeeded;
            gridItems.AutoGenerateColumns = false;
            gridItems.AllowUserToAddRows = true;
            gridItems.AllowUserToDeleteRows = true;
            gridItems.EditMode = DataGridViewEditMode.EditOnEnter; // ← doplněno

            // 2) Sloupce: DOPLŇ Name, ValueType už máš dobře
            var colType = new DataGridViewComboBoxColumn
            {
                Name = "Type", // ← DŮLEŽITÉ!
                HeaderText = "Type",
                DataPropertyName = "Type",
                ValueType = typeof(ChangeType),
                DataSource = Enum.GetValues(typeof(ChangeType)),
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Standard,
                Width = 140
            };

            var colText = new DataGridViewTextBoxColumn
            {
                Name = "Text", // ← kvůli přehlednosti
                HeaderText = "Text",
                DataPropertyName = "Text",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            gridItems.Columns.Clear();
            gridItems.Columns.Add(colType);
            gridItems.Columns.Add(colText);

            btnAddVersion.Click += (_, __) => AddVersion();
            btnDeleteVersion.Click += (_, __) => DeleteSelectedVersion();
            btnSave.Click += (_, __) => Save();
            lstVersions.SelectedIndexChanged += (_, __) => LoadSelectedVersion();
        }

        private void ChangeLogEditorForm_Load(object sender, EventArgs e)
        {
            _log = ChangeLogStorage.LoadOrCreate();
            BindVersionsList();
        }

        private void BindVersionsList()
        {
            lstVersions.DisplayMember = "Version";
            lstVersions.ValueMember = "Version";
            lstVersions.DataSource = _log.Versions
                                         .OrderByDescending(v => v.Date)
                                         .ToList();

            if (lstVersions.Items.Count > 0)
                lstVersions.SelectedIndex = 0;
            else
                AddVersion();
        }

        private void LoadSelectedVersion()
        {
            _current = lstVersions.SelectedItem as ChangeLogVersion;
            if (_current == null)
            {
                _items = new BindingList<ChangeItem>();
                gridItems.DataSource = _items;
                return;
            }

            txtVersion.Text = _current.Version;
            dtpDate.Value = _current.Date == default ? DateTime.Now : _current.Date; // žádné .Date


            // Kritické: binduj přímo na BindingList<ChangeItem>
            _items = new BindingList<ChangeItem>(_current.Items.ToList());
            gridItems.DataSource = _items;
        }

        private void GridItems_DefaultValuesNeeded(object? sender, DataGridViewRowEventArgs e)
        {
            // nový řádek = platná výchozí hodnota enumu
            if (e.Row.DataGridView.Columns.Contains("Type"))
                e.Row.Cells["Type"].Value = ChangeType.CHANGED;
            else
                e.Row.Cells[0].Value = ChangeType.CHANGED; // fallback, kdyby někdo přejmenoval sloupec
        }

        private void AddVersion()
        {
            _log ??= new ChangeLog(); // ← pojistka, kdyby někdo klikl před Load

            var v = new ChangeLogVersion
            {
                Version = "0.0.0",
                Date = DateTime.Today
            };
            _log.Versions.Add(v);
            BindVersionsList();
            lstVersions.SelectedItem = _log.Versions.Last();
        }

        private void DeleteSelectedVersion()
        {
            var v = lstVersions.SelectedItem as ChangeLogVersion;
            if (v == null) return;

            if (MessageBox.Show(this, $"Delete version {v.Version}?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            _log.Versions.Remove(v);
            ChangeLogStorage.Save(_log);
            BindVersionsList();
        }

        /// <summary>
        /// Najde nejbližší nadřazenou složku, která obsahuje podsložku "Content".
        /// Typicky to je kořen projektu (Bohemia_Solutions\Content).
        /// </summary>
        private static string? FindProjectRootWithContent()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var content = Path.Combine(dir.FullName, "Content");
                if (Directory.Exists(content))
                    return dir.FullName;

                // bonus: zkusíme kořen řešení, když app běží z bin\Debug\...
                // (tj. o 3–4 úrovně výš může být složka projektu)
                dir = dir.Parent;
            }
            return null;
        }

        /// <summary>
        /// Zkopíruje changelog.json do kořene projektu a do Content.
        /// Bezpečně přepíše stávající soubory.
        /// </summary>
        private static void CopyChangelogToProjectLocations(string sourceFile)
        {
            if (!File.Exists(sourceFile)) return;

            var root = FindProjectRootWithContent();
            if (root == null) return;

            var destRoot = Path.Combine(root, "changelog.json");
            var destContent = Path.Combine(root, "Content", "changelog.json");

            try
            {
                // ujisti se, že Content existuje
                Directory.CreateDirectory(Path.Combine(root, "Content"));

                File.Copy(sourceFile, destRoot, overwrite: true);
                File.Copy(sourceFile, destContent, overwrite: true);
            }
            catch
            {
                // nechceme blokovat Save kvůli kopírování – případné chyby ignorujeme
                // (pokud chceš, můžeš sem dát Log/MessageBox)
            }
        }


        private void Save()
        {
            if (_current == null) return;

            _current.Version = txtVersion.Text.Trim();
            _current.Date = dtpDate.Value; // ponech čas!

            // Přepiš Items z BindingListu (vynech prázdné řádky)
            _current.Items.Clear();
            foreach (var it in _items.Where(i => i != null && !string.IsNullOrWhiteSpace(i.Text)))
            {
                _current.Items.Add(new ChangeItem { Type = it.Type, Text = it.Text.Trim() });
            }

            // 1) Ulož JSON
            ChangeLogStorage.Save(_log);

            // 2) Najdi nejnovější verzi (kvůli eventu)
            var latest = _log.Versions
                .OrderByDescending(v => v.Date)
                .ThenByDescending(v => v.Version, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            // 3) Oznám Form1
            VersionSaved?.Invoke(latest?.Version ?? _current.Version);

            // 4a) JSON -> projekt / Content
            var savedJson = Path.GetFullPath("changelog.json");
            CopyChangelogToProjectLocations(savedJson);

            // 4b) TXT pro GitHub (aktuálně ukládaná verze)
            var ghTxt = BuildGitHubChangelogText(_current.Version, _current.Date, _current.Items);

            // 4c) TXT -> projekt / Content
            WriteGitHubChangelogToProjectLocations(ghTxt);

            // 5) Info uživateli
            MessageBox.Show(this, "Changelog saved and propagated (JSON + GitHub TXT).", "OK",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 6) Po uložení a potvrzení otevřít TXT v nastaveném editoru (dle asociace systému)
            try
            {
                // Otevřeme ten v kořeni projektu (Content je kopie pro updater)
                var projRoot = FindProjectRootWithContent();
                if (projRoot != null)
                {
                    var txtPath = Path.Combine(projRoot, "CHANGELOG_GitHub.txt");
                    if (File.Exists(txtPath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = txtPath,
                            UseShellExecute = true // => otevře se v uživatelsky nastaveném editoru pro .txt
                        });
                    }
                }
            }
            catch
            {
                // neblokuj – klidně si sem dej Log.Debug(...)
            }

            // 7) Refresh listboxu a reselect
            BindVersionsList();
            var sel = _log.Versions.FirstOrDefault(v => v.Version == _current.Version && v.Date == _current.Date);
            if (sel != null) lstVersions.SelectedItem = sel;
        }



        // Postaví GitHub-friendly TXT pro jednu verzi (Added / Changed / Fixed / Removed).
        // GitHub-friendly TXT pro jednu verzi – každá kategorie má svou sekci:
        // Added, Changed, Fixed, Removed, Improved, Stability
        private static string BuildGitHubChangelogText(string version, DateTime date, IEnumerable<ChangeItem> items)
        {
            var added = new List<string>();
            var changed = new List<string>();
            var fixed_ = new List<string>();
            var removed = new List<string>();
            var improved = new List<string>();
            var stability = new List<string>();

            foreach (var it in items ?? Enumerable.Empty<ChangeItem>())
            {
                if (it == null || string.IsNullOrWhiteSpace(it.Text)) continue;

                var t = it.Type.ToString().Trim().ToUpperInvariant();
                var line = it.Text.Trim();

                switch (t)
                {
                    case "ADDED": added.Add(line); break;
                    case "CHANGED": changed.Add(line); break;
                    case "FIXED": fixed_.Add(line); break;
                    case "REMOVED": removed.Add(line); break;
                    case "IMPROVED": improved.Add(line); break;
                    case "STABILITY": stability.Add(line); break;
                    default:
                        // neznámé typy ignoruj, nebo zde přidej mapování
                        break;
                }
            }

            var nl = Environment.NewLine;
            var sb = new System.Text.StringBuilder();
          //  sb.Append($"## {version}{nl}{nl}");

            if (added.Count > 0)
            {
                sb.Append("### Added").Append(nl);
                foreach (var s in added) sb.Append("- ").Append(s).Append(nl);
                sb.Append(nl);
            }
            if (changed.Count > 0)
            {
                sb.Append("### Changed").Append(nl);
                foreach (var s in changed) sb.Append("- ").Append(s).Append(nl);
                sb.Append(nl);
            }
            if (fixed_.Count > 0)
            {
                sb.Append("### Fixed").Append(nl);
                foreach (var s in fixed_) sb.Append("- ").Append(s).Append(nl);
                sb.Append(nl);
            }
            if (removed.Count > 0)
            {
                sb.Append("### Removed").Append(nl);
                foreach (var s in removed) sb.Append("- ").Append(s).Append(nl);
                sb.Append(nl);
            }
            if (improved.Count > 0)
            {
                sb.Append("### Improved").Append(nl);
                foreach (var s in improved) sb.Append("- ").Append(s).Append(nl);
                sb.Append(nl);
            }
            if (stability.Count > 0)
            {
                sb.Append("### Stability").Append(nl);
                foreach (var s in stability) sb.Append("- ").Append(s).Append(nl);
                sb.Append(nl);
            }

            return sb.ToString();
        }



        // Zapíše TXT do kořene projektu i do Content (overwrite)
        private static void WriteGitHubChangelogToProjectLocations(string text)
        {
            var root = FindProjectRootWithContent();
            if (root == null) return;

            var destRoot = Path.Combine(root, "CHANGELOG_GitHub.txt");
            var destContentDir = Path.Combine(root, "Content");
            var destContent = Path.Combine(destContentDir, "CHANGELOG_GitHub.txt");

            try
            {
                Directory.CreateDirectory(destContentDir);
                File.WriteAllText(destRoot, text, System.Text.Encoding.UTF8);
                File.WriteAllText(destContent, text, System.Text.Encoding.UTF8);
            }
            catch
            {
                // neblokujeme uložení – případně sem dej log/MessageBox dle preferencí
            }
        }


        private void ChangeLogEditorForm_Load_1(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {

        }
    }
}
