using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp1.Models;

namespace Bohemia_Solutions
{
    public class CreateFilterForm : Form
    {
        private TextBox txtName;
        private CheckedListBox clbConfigs;
        private Button btnOk, btnCancel;

        public string FilterName => (txtName?.Text ?? string.Empty).Trim();
        public List<string> SelectedConfigIds { get; private set; } = new();

        public CreateFilterForm(IEnumerable<DayZConfig> configs)
        {
            Text = "Create Filter";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(500, 500);
            MaximizeBox = false;

            var lbl = new Label { Text = "Filter name:", AutoSize = true };
            txtName = new TextBox { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var pnlName = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            pnlName.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlName.Controls.Add(lbl, 0, 0);
            pnlName.Controls.Add(txtName, 1, 0);

            clbConfigs = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false
            };

            foreach (var c in configs.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase))
                clbConfigs.Items.Add(new ComboItem(c.Name, c.Id));

            btnOk = new Button { Text = "Create", DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(8)
            };
            pnlButtons.Controls.AddRange(new Control[] { btnOk, btnCancel });

            btnOk.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(FilterName))
                {
                    MessageBox.Show("Please enter a filter name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                SelectedConfigIds = clbConfigs.CheckedItems.Cast<object>()
                    .Select(i => ((ComboItem)i).Value)
                    .ToList();
            };

            Controls.Add(clbConfigs);
            Controls.Add(pnlButtons);
            Controls.Add(pnlName);
        }

        private class ComboItem
        {
            public string Text { get; }
            public string Value { get; }
            public ComboItem(string text, string value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}
