using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp1.Models;

namespace Bohemia_Solutions
{
    internal sealed partial class ManageFiltersForm : Form
    {
        private readonly List<DayZConfig> _configs;
        private readonly ListBox _lbFilters = new ListBox();
        private readonly CheckedListBox _clbConfigs = new CheckedListBox();
        private readonly TextBox _txtRename = new TextBox();
        private readonly Button _btnRename = new Button();
        private readonly Button _btnDelete = new Button();
        private readonly Button _btnSaveMembership = new Button();
        private readonly Button _btnClose = new Button();
        private readonly List<string> _allKnownFilters; // ⬅️ NOVÉ
        private bool _changed = false;
        private const string AllItem = "All";
        public ManageFiltersForm(List<DayZConfig> configs, List<string>? allKnownFilters = null)
        {
            _configs = configs ?? new List<DayZConfig>();
            _allKnownFilters = (allKnownFilters ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(CleanTag)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Text = "Manage Filters";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 500);
    
            BuildUi();
            LoadFilters();
        }
        public ManageFiltersForm(List<DayZConfig> configs)
        {
            _configs = configs ?? new List<DayZConfig>();
            Text = "Manage Filters";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 500);

            BuildUi();
            LoadFilters();
        }

        private void BuildUi()
        {
            var left = new GroupBox { Text = "Filters", Dock = DockStyle.Left, Width = 260, Padding = new Padding(8) };
            var right = new GroupBox { Text = "Membership (configs in selected filter)", Dock = DockStyle.Fill, Padding = new Padding(8) };

            _lbFilters.Dock = DockStyle.Fill;
            _lbFilters.IntegralHeight = false;
            _lbFilters.SelectedIndexChanged += (_, __) => RefreshMembershipForSelected();
            left.Controls.Add(_lbFilters);

            _clbConfigs.Dock = DockStyle.Fill;
            _clbConfigs.CheckOnClick = true;
            _clbConfigs.IntegralHeight = false;

            var pnlTop = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _txtRename.Width = 260;
            _txtRename.PlaceholderText = "New name… (rename)";
            _btnRename.Text = "Rename";
            _btnDelete.Text = "Delete";

            _btnRename.Click += (_, __) => DoRename();
            _btnDelete.Click += (_, __) => DoDelete();

            pnlTop.Controls.Add(_txtRename, 0, 0);
            pnlTop.Controls.Add(_btnRename, 1, 0);
            pnlTop.Controls.Add(_btnDelete, 2, 0);

            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            _btnClose.Text = "Close";
            _btnClose.Click += (_, __) => { DialogResult = _changed ? DialogResult.OK : DialogResult.Cancel; Close(); };
            _btnSaveMembership.Text = "Save membership";
            _btnSaveMembership.Click += (_, __) => { SaveMembership(); };
            pnlBottom.Controls.AddRange(new Control[] { _btnClose, _btnSaveMembership });

            right.Controls.Add(_clbConfigs);
            right.Controls.Add(pnlTop);
            right.Controls.Add(pnlBottom);

            Controls.Add(right);
            Controls.Add(left);
        }

        // ===== data helpers =====
        private static string CleanTag(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var t = System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s{2,}", " ");
            return t;
        }

        private List<string> GetDistinctFilters()
        {
            return _configs
                .Where(c => c?.Filters != null)
                .SelectMany(c => c.Filters)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(CleanTag)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<string> GetAllKnownFiltersDistinct()
        {
            var fromConfigs = _configs
                .Where(c => c?.Filters != null)
                .SelectMany(c => c.Filters!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(CleanTag);

            return fromConfigs
                .Concat(_allKnownFilters ?? Enumerable.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void LoadFilters()
        {
            _lbFilters.BeginUpdate();
            _lbFilters.Items.Clear();

            var items = GetAllKnownFiltersDistinct();   // ⬅️ místo GetDistinctFilters()
            foreach (var f in items) _lbFilters.Items.Add(f);

            _lbFilters.EndUpdate();

            _clbConfigs.Items.Clear();
            foreach (var c in _configs.OrderBy(c => c?.Name ?? "", StringComparer.OrdinalIgnoreCase))
                _clbConfigs.Items.Add(c?.Name ?? "(unnamed)");

            if (_lbFilters.Items.Count > 0)
                _lbFilters.SelectedIndex = 0;
        }

        private string? SelectedFilterName()
        {
            var s = _lbFilters.SelectedItem as string;
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private void RefreshMembershipForSelected()
        {
            var f = SelectedFilterName();
            if (f == null)
            {
                for (int i = 0; i < _clbConfigs.Items.Count; i++)
                    _clbConfigs.SetItemChecked(i, false);
                return;
            }

            // Nastav checkboxy podle toho, které konfigurace obsahují filter f
            for (int i = 0; i < _clbConfigs.Items.Count; i++)
            {
                var cfg = _configs.OrderBy(c => c?.Name ?? "", StringComparer.OrdinalIgnoreCase).ElementAt(i);
                bool has = (cfg?.Filters ?? new List<string>()).Any(x =>
                    string.Equals(x, f, StringComparison.OrdinalIgnoreCase));
                _clbConfigs.SetItemChecked(i, has);
            }

            _txtRename.Text = f;
        }

        private void SaveMembership()
        {
            var f = SelectedFilterName();
            if (f == null) return;

            var orderedConfigs = _configs.OrderBy(c => c?.Name ?? "", StringComparer.OrdinalIgnoreCase).ToList();

            for (int i = 0; i < orderedConfigs.Count; i++)
            {
                var cfg = orderedConfigs[i];
                cfg.Filters ??= new List<string>();

                bool shouldHave = _clbConfigs.GetItemChecked(i);
                bool hasNow = cfg.Filters.Any(x => string.Equals(x, f, StringComparison.OrdinalIgnoreCase));

                if (shouldHave && !hasNow)
                {
                    cfg.Filters.Add(f);
                    _changed = true;
                }
                else if (!shouldHave && hasNow)
                {
                    // odstranit case-insensitive
                    cfg.Filters = cfg.Filters
                        .Where(x => !string.Equals(x, f, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    _changed = true;
                }
            }

            if (_changed)
                MessageBox.Show("Membership saved.", "Filters", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoRename()
        {
           
            var oldName = SelectedFilterName();
            if (oldName == null) return;

            var newName = CleanTag(_txtRename.Text);
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Enter new name.", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.Equals(newName, AllItem, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("\"All\" is reserved.", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.Equals(newName, oldName, StringComparison.OrdinalIgnoreCase))
                return;


            // kolize?
            bool exists = GetAllKnownFiltersDistinct()
                .Any(x => string.Equals(x, newName, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                var r = MessageBox.Show(
                    $"Filter \"{newName}\" already exists.\nDo you want to MERGE \"{oldName}\" into \"{newName}\"?",
                    "Rename → Merge", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.Cancel) return;

                if (r == DialogResult.No) return; // nechceš kolidovat
            }

            foreach (var cfg in _configs)
            {
                if (cfg?.Filters == null) continue;

                bool hadOld = cfg.Filters.Any(x => string.Equals(x, oldName, StringComparison.OrdinalIgnoreCase));
                if (!hadOld) continue;

                // odeber staré
                cfg.Filters = cfg.Filters
                    .Where(x => !string.Equals(x, oldName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // přidej nové (pokud už není)
                if (!cfg.Filters.Any(x => string.Equals(x, newName, StringComparison.OrdinalIgnoreCase)))
                    cfg.Filters.Add(newName);

                _changed = true;
            }

            _allKnownFilters.RemoveAll(x => string.Equals(x, oldName, StringComparison.OrdinalIgnoreCase));
            if (!_allKnownFilters.Any(x => string.Equals(x, newName, StringComparison.OrdinalIgnoreCase)))
                _allKnownFilters.Add(newName);
            LoadFilters();
            // vyber přejmenovaný / zmergovaný
            int ix = _lbFilters.Items.IndexOf(newName);
            if (ix >= 0) _lbFilters.SelectedIndex = ix;
            MessageBox.Show("Filter renamed.", "Filters", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoDelete()
        {
            var f = SelectedFilterName();
            if (f == null) return;

            var q = MessageBox.Show(
                $"Delete filter \"{f}\" from all configurations?\n(This does NOT delete any configs.)",
                "Delete filter", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (q != DialogResult.OK) return;

            foreach (var cfg in _configs)
            {
                if (cfg?.Filters == null) continue;
                cfg.Filters = cfg.Filters
                    .Where(x => !string.Equals(x, f, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _allKnownFilters.RemoveAll(x => string.Equals(x, f, StringComparison.OrdinalIgnoreCase));

            _changed = true;                   // ⬅️ DŮLEŽITÉ – ať parent dostane DialogResult.OK
            LoadFilters();
            MessageBox.Show("Filter deleted.", "Filters", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ManageFiltersForm_Load(object sender, EventArgs e)
        {

        }
    }
}
