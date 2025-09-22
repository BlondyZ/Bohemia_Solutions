using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bohemia_Solutions
{
    public partial class AdminLoginForm : Form
    {
        private readonly TextBox txtPassword;
        private readonly CheckBox chkShow;
        private readonly Button btnOk;
        private readonly Button btnCancel;

        public string EnteredPassword => txtPassword.Text;

        public AdminLoginForm()
        {
            Text = "Changelog – Admin";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(300, 140);

            var lbl = new Label
            {
                Left = 12,
                Top = 12,
                Width = 260,
                Text = "Enter admin password:"
            };

            txtPassword = new TextBox
            {
                Left = 12,
                Top = 35,
                Width = 260,
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = false,
                AutoSize = false,
                Margin = Padding.Empty
            };
            txtPassword.Height = txtPassword.PreferredHeight;  // odstraní „šedý proužek“

            chkShow = new CheckBox { Left = 12, Top = 60, Text = "Show", AutoSize = true };
            chkShow.CheckedChanged += (s, e) => txtPassword.UseSystemPasswordChar = !chkShow.Checked;

            btnOk = new Button { Left = 116, Top = 90, Width = 75, Text = "OK", DialogResult = DialogResult.OK };
            btnCancel = new Button { Left = 197, Top = 90, Width = 75, Text = "Cancel", DialogResult = DialogResult.Cancel };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[] { lbl, txtPassword, chkShow, btnOk, btnCancel });
        }
    }
}
