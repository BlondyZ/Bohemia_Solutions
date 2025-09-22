using System;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Bohemia_Solutions
{
    public partial class DirectoryCopyProgressDialog : Form
    {
        private readonly ProgressBar _bar = new() { Dock = DockStyle.Top, Height = 22 };
        private readonly Label _lbl = new() { Dock = DockStyle.Top, AutoSize = false, Height = 42, Padding = new(6), Text = "Preparing..." };
        private readonly Button _cancel = new() { Text = "Cancel", Dock = DockStyle.Right, Width = 90, Margin = new(6) };
        private CancellationTokenSource _cts = new();
        private Task _runner = Task.CompletedTask;

        public DirectoryCopyProgressDialog()
        {
            Text = "Copying files...";
            StartPosition = FormStartPosition.CenterParent;
            Width = 560;
            Height = 150;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(8) };
            panel.Controls.Add(_cancel);

            Controls.Add(panel);
            Controls.Add(_bar);
            Controls.Add(_lbl);

            _cancel.Click += (s, e) => _cts.Cancel();
            Shown += (s, e) => Activate();
        }

        /// <summary>Show modal and copy source → dest. Returns true if completed.</summary>
        public static bool ShowAndCopy(IWin32Window owner, string source, string dest)
        {
            using var dlg = new DirectoryCopyProgressDialog();
            try
            {
                dlg._runner = dlg.CopyAsync(source, dest, dlg._cts.Token);
                dlg.ShowDialog(owner);
                return dlg.DialogResult == DialogResult.OK;
            }
            catch
            {
                return false;
            }
        }

        private async Task CopyAsync(string source, string dest, CancellationToken ct)
        {
            try
            {
                if (!Directory.Exists(dest))
                    Directory.CreateDirectory(dest);

                var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).ToList();
                var total = files.Count;
                if (total == 0)
                {
                    _lbl.Text = "Nothing to copy.";
                    _bar.Minimum = 0; _bar.Maximum = 1; _bar.Value = 1;
                    await Task.Delay(300, ct);
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                _bar.Minimum = 0; _bar.Maximum = total; _bar.Step = 1;
                int done = 0;

                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();

                    var rel = Path.GetRelativePath(source, file);
                    var target = Path.Combine(dest, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                    _lbl.Text = rel;
                    await Task.Run(() => File.Copy(file, target, overwrite: true), ct);

                    done++;
                    _bar.Value = done;
                }

                _lbl.Text = "Completed.";
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (OperationCanceledException)
            {
                _lbl.Text = "Cancelled.";
                DialogResult = DialogResult.Cancel;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Copy failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Abort;
                Close();
            }
        }

        private void DirectoryCopyProgressDialog_Load(object sender, EventArgs e)
        {

        }
    }
}
