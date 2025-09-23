using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Bohemia_Solutions.Models;
using Bohemia_Solutions.Services;

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

            ChangeLogStorage.Save(_log);
            // Najdi "nejnovější" verzi – primárně podle Date, sekundárně podle Version
            var latest = _log.Versions
                .OrderByDescending(v => v.Date)
                .ThenByDescending(v => v.Version, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            // pošli informaci Form1 (pokud je odběratel)
            VersionSaved?.Invoke(latest?.Version ?? _current.Version);
            MessageBox.Show(this, "Changelog saved.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // refresh listboxu (kvůli řazení podle datumu)
            BindVersionsList();
            // znovu vyber právě editovanou verzi (podle čísla)
            var sel = _log.Versions.FirstOrDefault(v => v.Version == _current.Version && v.Date == _current.Date);
            if (sel != null) lstVersions.SelectedItem = sel;
        }

        private void ChangeLogEditorForm_Load_1(object sender, EventArgs e)
        {

        }
    }
}
