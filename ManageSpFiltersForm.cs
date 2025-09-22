using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Bohemia_Solutions.Models; // SinglePlayerConfig

namespace Bohemia_Solutions
{
    internal sealed partial class ManageSpFiltersForm : Form
    {
        private readonly List<SinglePlayerConfig> _configs;
        private readonly List<string> _allKnownFilters; // může být null
        private ListBox lstFilters;
        private TextBox txtFilterName;
        private CheckedListBox clbMembership;
        private Button btnRename, btnDelete, btnSave;
        private bool _changed = false;
        public ManageSpFiltersForm(List<SinglePlayerConfig> configs, List<string>? allKnownFilters = null)
        {
            _configs = configs ?? new List<SinglePlayerConfig>();
            foreach (var c in _configs) if (string.IsNullOrWhiteSpace(c.Id)) c.Id = Guid.NewGuid().ToString("N");

            _allKnownFilters = (allKnownFilters ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => Regex.Replace(s.Trim(), @"\s{2,}", " "))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            BuildUi();              // ⬅️ místo InitializeComponent()
            LoadFiltersIntoUI();    // ⬅️ naplnění seznamů
        }

        private void LoadFiltersIntoUI()
        {
            // union: filtry z konfigurací ∪ registry (allKnown)
            var fromConfigs = _configs.SelectMany(c => c?.Filters ?? Enumerable.Empty<string>());
            var all = fromConfigs
                .Concat(_allKnownFilters)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => Regex.Replace(s.Trim(), @"\s{2,}", " "))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            lstFilters.BeginUpdate();
            lstFilters.Items.Clear();
            foreach (var f in all) lstFilters.Items.Add(f);
            lstFilters.EndUpdate();

            // naplň i membership list (jen jména, zaškrtávání udělá LoadMembership)
            clbMembership.Items.Clear();
            foreach (var c in _configs.OrderBy(c => c?.Name ?? "", StringComparer.OrdinalIgnoreCase))
                clbMembership.Items.Add(new CItem { Id = c.Id!, Label = c.Name ?? "(unnamed)" });

            if (lstFilters.Items.Count > 0)
                lstFilters.SelectedIndex = 0;
        }



        private void BuildUi()
        {
            // jistota, že ovládací prvky existují
            lstFilters ??= new ListBox();
            txtFilterName ??= new TextBox();
            clbMembership ??= new CheckedListBox();
            btnRename ??= new Button();
            btnDelete ??= new Button();
            btnSave ??= new Button();
            var btnClose = new Button();

            SuspendLayout();

            // === stejné “základy” jako MP ===
            Text = "Manage SP Filters";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;

            // DŮLEŽITÉ: sjednocení vzhledu a měřítka s MP
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            ClientSize = new Size(720, 500);   // klidně dej přesně stejné hodnoty, jaké má MP dialog

            // --- levý box: Filters (šířka 260 stejně jako MP) ---
            var left = new GroupBox { Text = "Filters", Dock = DockStyle.Left, Width = 260, Padding = new Padding(8) };
            lstFilters.Dock = DockStyle.Fill;
            lstFilters.IntegralHeight = false;
            lstFilters.SelectedIndexChanged += (_, __) => LoadMembership();
            left.Controls.Add(lstFilters);

            // --- pravý box: Membership ---
            var right = new GroupBox { Text = "Membership (configs in selected filter)", Dock = DockStyle.Fill, Padding = new Padding(8) };

            // horní řádek: textbox + Rename + Delete
            var pnlTop = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            txtFilterName.Width = 260;
            txtFilterName.PlaceholderText = "New name… (rename)";
            btnRename.Text = "Rename";
            btnDelete.Text = "Delete";
            btnRename.Click += (_, __) => DoRename();
            btnDelete.Click += (_, __) => DoDelete();

            pnlTop.Controls.Add(txtFilterName, 0, 0);
            pnlTop.Controls.Add(btnRename, 1, 0);
            pnlTop.Controls.Add(btnDelete, 2, 0);

            // list členství
            clbMembership.Dock = DockStyle.Fill;
            clbMembership.CheckOnClick = true;
            clbMembership.IntegralHeight = false;

            // spodní tlačítka
            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            btnClose.Text = "Close";
            btnClose.Click += (_, __) => { DialogResult = _changed ? DialogResult.OK : DialogResult.Cancel; Close(); };
            btnSave.Text = "Save";                       // stejné jako v MP okně
            btnSave.Click += (_, __) => SaveMembership();
            pnlBottom.Controls.AddRange(new Control[] { btnClose, btnSave });

            // pořadí Docků musí být stejné jako u MP: Fill -> Top -> Bottom
            right.Controls.Add(clbMembership);
            right.Controls.Add(pnlTop);
            right.Controls.Add(pnlBottom);

            Controls.Clear();
            Controls.Add(right);
            Controls.Add(left);

            // stejné „OK/Cancel“ chování
            AcceptButton = btnSave;
            CancelButton = btnClose;

            ResumeLayout();
        }




        private static string Normalize(string s) => Regex.Replace((s ?? "").Trim(), @"\s{2,}", " ");

        // Build distinct list of filters across SP configs
        private List<string> CollectFilters() =>
            _configs.SelectMany(c => c.Filters ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(Normalize)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

        private void RefreshFiltersList()
        {
            var filters = CollectFilters();
            lstFilters.Items.Clear();
            foreach (var f in filters) lstFilters.Items.Add(f);
            if (lstFilters.Items.Count > 0) lstFilters.SelectedIndex = 0;
        }

        private void LoadMembership()
        {
            var filter = lstFilters.SelectedItem as string ?? "";
            txtFilterName.Text = filter;

            for (int i = 0; i < clbMembership.Items.Count; i++)
            {
                var ci = (CItem)clbMembership.Items[i];
                var cfg = _configs.First(x => string.Equals(x.Id, ci.Id, StringComparison.OrdinalIgnoreCase));
                bool has = (cfg.Filters ?? new List<string>())
                            .Any(f => string.Equals(f, filter, StringComparison.OrdinalIgnoreCase));
                clbMembership.SetItemChecked(i, has);
            }
        }

        private void SaveMembership()
        {
            var filter = Normalize(txtFilterName.Text);
            if (string.IsNullOrWhiteSpace(filter)) return;

            var selectedOld = lstFilters.SelectedItem?.ToString();

            // (1) odstranit starý název
            if (!string.IsNullOrWhiteSpace(selectedOld))
            {
                foreach (var c in _configs)
                    c.Filters = (c.Filters ?? new List<string>())
                        .Where(f => !string.Equals(f, selectedOld, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            // (2) přidat nové jméno podle zaškrtnutí
            var ids = new HashSet<string>(clbMembership.CheckedItems.Cast<CItem>().Select(i => i.Id),
                                          StringComparer.OrdinalIgnoreCase);
            foreach (var c in _configs)
            {
                if (ids.Contains(c.Id!))
                {
                    c.Filters ??= new List<string>();
                    if (!c.Filters.Any(f => string.Equals(f, filter, StringComparison.OrdinalIgnoreCase)))
                        c.Filters.Add(filter);
                }
            }

            RefreshFiltersList();

            int ix = lstFilters.Items.IndexOf(filter);
            // info jako v MP
            MessageBox.Show(
                string.Equals(selectedOld, filter, StringComparison.OrdinalIgnoreCase)
                    ? "Membership saved."
                    : "Filter renamed.",
                "Filters",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _changed = true; 
        }

        private void DoRename()
        {
            if (lstFilters.SelectedItem == null) return;

            var oldName = (lstFilters.SelectedItem as string) ?? "";
            var newName = Normalize(txtFilterName.Text);
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Enter new name.", "Rename",
                MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }
            if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return;

            // existuje stejné jméno?
            bool exists = lstFilters.Items.Cast<string>()
                .Any(x => string.Equals(x, newName, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                var r = MessageBox.Show(
                    $"Filter \"{newName}\" already exists.\nDo you want to MERGE \"{oldName}\" into \"{newName}\"?",
                    "Rename → Merge", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }

            // odeber staré jméno a přidej nové (nebo slouč)
            foreach (var c in _configs)
            {
                if (c.Filters == null) continue;

                bool hadOld = c.Filters.Any(f => string.Equals(f, oldName, StringComparison.OrdinalIgnoreCase));
                if (!hadOld) continue;

                c.Filters = c.Filters
                    .Where(f => !string.Equals(f, oldName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!c.Filters.Any(f => string.Equals(f, newName, StringComparison.OrdinalIgnoreCase)))
                    c.Filters.Add(newName);

                _changed = true;
            }

            RefreshFiltersList();
            int ix = lstFilters.Items.IndexOf(newName);
            if (ix >= 0) lstFilters.SelectedIndex = ix;

            MessageBox.Show(exists ? "Filters merged." : "Filter renamed.",
                "Filters", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoDelete()
        {
            if (lstFilters.SelectedItem == null) return;
            string filter = lstFilters.SelectedItem.ToString();

            var q = MessageBox.Show(
                $"Delete filter \"{filter}\" from all configurations?\n(This does NOT delete any configs.)",
                "Delete filter", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (q != DialogResult.OK) return;

            foreach (var c in _configs)
                c.Filters = (c.Filters ?? new List<string>())
                    .Where(f => !string.Equals(f, filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            RefreshFiltersList();
            MessageBox.Show("Filter deleted.", "Filters", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _changed = true;        
        }

        private sealed class CItem
        {
            public string Id;
            public string Label;
            public override string ToString() => Label;
        }


        private void ManageSpFiltersForm_Load(object sender, EventArgs e)
        {

        }

        private void ManageSpFiltersForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Křížek / Alt+F4 → chovej se jako Close (Cancel)
            if (e.CloseReason == CloseReason.UserClosing && this.DialogResult == DialogResult.None)
                this.DialogResult = DialogResult.Cancel;

            base.OnFormClosing(e);
        }
    }
}
