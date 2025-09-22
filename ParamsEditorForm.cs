using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Bohemia_Solutions
{
    internal sealed partial class ParamsEditorForm : Form
    {
        private readonly DataGridView dgv = new DataGridView();
        private readonly Button btnAdd = new Button();
        private readonly Button btnEdit = new Button();
        private readonly Button btnDelete = new Button();
        private readonly Button btnOk = new Button();
        private readonly Button btnCancel = new Button();

        private readonly BindingList<Row> binding;
        public List<Row> Result { get; private set; } = new List<Row>();

        // Vstupní řádek
        public sealed class Row
        {
            public bool Enabled { get; set; }
            public string Parameter { get; set; } = "";
        }

        // Přijme existující položky z ConfigForm (ParamItem -> Row)
        public ParamsEditorForm(IEnumerable<Row> initial, string title)

        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Width = 700;
            Height = 480;

            // Data
            binding = new BindingList<Row>(
                 (initial ?? Enumerable.Empty<Row>())
                     .Select(x => new Row
                     {
                         Enabled = x.Enabled,
                         Parameter = x.Parameter ?? ""
                     })
                     .ToList()
             );

            // DataGridView
            dgv.Dock = DockStyle.Top;
            dgv.Height = 360;
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = true;
            dgv.DataSource = binding;

            var colEnabled = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Enabled",
                DataPropertyName = nameof(Row.Enabled),
                Width = 90
            };
            var colParam = new DataGridViewTextBoxColumn
            {
                HeaderText = "Parameter",
                DataPropertyName = nameof(Row.Parameter),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dgv.Columns.Add(colEnabled);
            dgv.Columns.Add(colParam);

            // Buttons
            btnAdd.Text = "Add";
            btnEdit.Text = "Edit";
            btnDelete.Text = "Delete";
            btnOk.Text = "OK";
            btnCancel.Text = "Cancel";

            btnAdd.SetBounds(10, 370, 80, 28);
            btnEdit.SetBounds(100, 370, 80, 28);
            btnDelete.SetBounds(190, 370, 80, 28);
            btnOk.SetBounds(Width - 200, 370, 80, 28);
            btnCancel.SetBounds(Width - 110, 370, 80, 28);

            btnAdd.Click += (s, e) => AddParam();
            btnEdit.Click += (s, e) => EditParam();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnOk.Click += (s, e) => { CommitAndClose(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; }
                if (e.Control && e.KeyCode == Keys.E) { EditParam(); e.Handled = true; }
                if (e.Control && e.KeyCode == Keys.N) { AddParam(); e.Handled = true; }
            };

            Controls.Add(dgv);
            Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnOk, btnCancel });
        }

        private void AddParam()
        {
            var s = Microsoft.VisualBasic.Interaction.InputBox("Enter new parameter:", "Add parameter", "");
            s = (s ?? "").Trim();
            if (string.IsNullOrEmpty(s)) return;

            if (binding.Any(r => string.Equals(r.Parameter, s, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This parameter already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            binding.Add(new Row { Enabled = true, Parameter = s });
        }

        private void EditParam()
        {
            if (dgv.CurrentRow == null) return;
            var row = dgv.CurrentRow.DataBoundItem as Row;
            if (row == null) return;

            var s = Microsoft.VisualBasic.Interaction.InputBox("Edit parameter:", "Edit parameter", row.Parameter);
            s = (s ?? "").Trim();
            if (string.IsNullOrEmpty(s)) return;

            if (!string.Equals(s, row.Parameter, StringComparison.OrdinalIgnoreCase) &&
                binding.Any(r => string.Equals(r.Parameter, s, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This parameter already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            row.Parameter = s;
            dgv.Refresh();
        }

        private void DeleteSelected()
        {
            if (dgv.SelectedRows.Count == 0) return;
            if (MessageBox.Show("Delete selected item(s)?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // Smazat odzadu kvůli indexům
            var rows = dgv.SelectedRows.Cast<DataGridViewRow>()
                        .OrderByDescending(r => r.Index)
                        .ToList();

            foreach (var r in rows)
            {
                if (r.DataBoundItem is Row rr)
                    binding.Remove(rr);
            }
        }

        private void CommitAndClose()
        {
            // Uložit výsledky
            Result = binding
                .Where(r => !string.IsNullOrWhiteSpace(r.Parameter))
                .Select(r => new Row { Enabled = r.Enabled, Parameter = r.Parameter.Trim() })
                .ToList();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
