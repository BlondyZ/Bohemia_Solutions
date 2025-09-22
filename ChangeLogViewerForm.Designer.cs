namespace Bohemia_Solutions
{
    partial class ChangeLogViewerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.RichTextBox rtbOut;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            rtbOut = new RichTextBox();
            SuspendLayout();
            // 
            // rtbOut
            // 
            rtbOut.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbOut.Font = new Font("Consolas", 10F);
            rtbOut.Location = new Point(12, 12);
            rtbOut.Name = "rtbOut";
            rtbOut.Size = new Size(760, 416);
            rtbOut.TabIndex = 0;
            rtbOut.Text = "";
            // 
            // ChangeLogViewerForm
            // 
            ClientSize = new Size(784, 440);
            Controls.Add(rtbOut);
            MinimizeBox = false;
            MinimumSize = new Size(600, 400);
            Name = "ChangeLogViewerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Changelog";
            ResumeLayout(false);
        }
    }
}