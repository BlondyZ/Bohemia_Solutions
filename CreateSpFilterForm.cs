using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Bohemia_Solutions.Models; // SinglePlayerConfig

namespace Bohemia_Solutions
{
    internal sealed partial class CreateSpFilterForm : Form
    {
        private readonly List<SinglePlayerConfig> _configs;
        private TextBox txtName;
        private CheckedListBox clb;

        public string FilterName { get; private set; } = "";
        public List<string> SelectedConfigIds { get; private set; } = new();

        public CreateSpFilterForm(IEnumerable<SinglePlayerConfig> configs)
        {
            _configs = (configs ?? Enumerable.Empty<SinglePlayerConfig>()).ToList();
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Create Filter";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(470, 420);

            var lbl = new Label { Text = "Filter name:", Left = 8, Top = 12, AutoSize = true };
            txtName = new TextBox { Left = 8, Top = 30, Width = 450 };

            clb = new CheckedListBox
            {
                Left = 8,
                Top = 64,
                Width = 450,
                Height = 300,
                CheckOnClick = true,
                IntegralHeight = false
            };

            foreach (var c in _configs.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var label = string.IsNullOrWhiteSpace(c.Name) ? "(unnamed)" : c.Name;
                clb.Items.Add(new Item { Id = EnsureId(c), Label = label });
            }

            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 300, Top = 374, Width = 75 };
            var btnCreate = new Button { Text = "Create", Left = 383, Top = 374, Width = 75 };
            btnCreate.Click += (_, __) =>
            {
                var name = (txtName.Text ?? "").Trim();
                name = Regex.Replace(name, @"\s{2,}", " ");
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Filter name cannot be empty."); return;
                }
                FilterName = name;
                SelectedConfigIds = clb.CheckedItems.Cast<Item>().Select(i => i.Id).ToList();
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[] { lbl, txtName, clb, btnCancel, btnCreate });
            AcceptButton = btnCreate;
            CancelButton = btnCancel;
        }

        private static string EnsureId(SinglePlayerConfig c)
        {
            if (string.IsNullOrWhiteSpace(c.Id))
                c.Id = Guid.NewGuid().ToString("N");
            return c.Id;
        }

        private sealed class Item
        {
            public string Id;
            public string Label;
            public override string ToString() => Label;
        }
    }
}
