
namespace Bohemia_Solutions
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            btn_add = new Button();
            btn_edit = new Button();
            panel1 = new Panel();
            btn_remove = new Button();
            listViewConfigs = new ListView();
            btn_execute = new Button();
            pnl_Server_Info = new Panel();
            lbl_ip_adress = new Label();
            label2 = new Label();
            lblRunningServer = new Label();
            btnStopServer = new Button();
            pb_red = new PictureBox();
            pb_green = new PictureBox();
            pbButton6 = new PictureBox();
            pbButton5 = new PictureBox();
            pbButton4 = new PictureBox();
            label1 = new Label();
            pbButton1 = new PictureBox();
            pbButton2 = new PictureBox();
            pbButton3 = new PictureBox();
            btnRefreshBaseDayzServer = new Button();
            tbControllAll = new TabControl();
            tp_SinglePlayer = new TabPage();
            panel_SP_info = new Panel();
            label5 = new Label();
            button4 = new Button();
            button5 = new Button();
            pictureBox1 = new PictureBox();
            pictureBox2 = new PictureBox();
            panel2 = new Panel();
            btn_remove_sp = new Button();
            listViewConfigsSP = new ListView();
            btn_edit_sp = new Button();
            btn_add_sp = new Button();
            tp_Multiplayer = new TabPage();
            pbButton10 = new PictureBox();
            pbButton9 = new PictureBox();
            pbButton8 = new PictureBox();
            pbButton7 = new PictureBox();
            tsTop = new ToolStrip();
            tsbSettings = new ToolStripButton();
            tsbHelp = new ToolStripButton();
            tsbEditChangeLog = new ToolStripButton();
            statusStrip1 = new StatusStrip();
            lblActualVersion = new ToolStripStatusLabel();
            lblVersion = new ToolStripStatusLabel();
            lblBuildTime = new ToolStripStatusLabel();
            lbl_downloading_text = new Label();
            pnl_loading_update = new Panel();
            picLoading = new PictureBox();
            panel1.SuspendLayout();
            pnl_Server_Info.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pb_red).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pb_green).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton6).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton5).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton3).BeginInit();
            tbControllAll.SuspendLayout();
            tp_SinglePlayer.SuspendLayout();
            panel_SP_info.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            panel2.SuspendLayout();
            tp_Multiplayer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbButton10).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton9).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton8).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbButton7).BeginInit();
            tsTop.SuspendLayout();
            statusStrip1.SuspendLayout();
            pnl_loading_update.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLoading).BeginInit();
            SuspendLayout();
            // 
            // btn_add
            // 
            btn_add.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_add.Location = new Point(12, 743);
            btn_add.Name = "btn_add";
            btn_add.Size = new Size(75, 23);
            btn_add.TabIndex = 3;
            btn_add.Text = "Add";
            btn_add.UseVisualStyleBackColor = true;
            btn_add.Click += btn_add_Click;
            // 
            // btn_edit
            // 
            btn_edit.Location = new Point(93, 743);
            btn_edit.Name = "btn_edit";
            btn_edit.Size = new Size(101, 23);
            btn_edit.TabIndex = 4;
            btn_edit.Text = "Configurate";
            btn_edit.UseVisualStyleBackColor = true;
            btn_edit.Click += btn_edit_Click;
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.Fixed3D;
            panel1.Controls.Add(btn_remove);
            panel1.Controls.Add(listViewConfigs);
            panel1.Controls.Add(btn_edit);
            panel1.Controls.Add(btn_add);
            panel1.Location = new Point(8, 14);
            panel1.Name = "panel1";
            panel1.Size = new Size(674, 773);
            panel1.TabIndex = 10;
            // 
            // btn_remove
            // 
            btn_remove.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_remove.Location = new Point(200, 743);
            btn_remove.Name = "btn_remove";
            btn_remove.Size = new Size(75, 23);
            btn_remove.TabIndex = 14;
            btn_remove.Text = "Delete";
            btn_remove.UseVisualStyleBackColor = true;
            btn_remove.Click += btn_remove_Click;
            // 
            // listViewConfigs
            // 
            listViewConfigs.Location = new Point(13, 17);
            listViewConfigs.Name = "listViewConfigs";
            listViewConfigs.Size = new Size(651, 720);
            listViewConfigs.TabIndex = 12;
            listViewConfigs.UseCompatibleStateImageBehavior = false;
            listViewConfigs.View = View.Details;
            listViewConfigs.SelectedIndexChanged += listViewConfigs_SelectedIndexChanged;
            // 
            // btn_execute
            // 
            btn_execute.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_execute.Location = new Point(12, 149);
            btn_execute.Name = "btn_execute";
            btn_execute.Size = new Size(99, 23);
            btn_execute.TabIndex = 11;
            btn_execute.Text = "Run server !";
            btn_execute.UseVisualStyleBackColor = true;
            btn_execute.Click += btn_execute_Click;
            // 
            // pnl_Server_Info
            // 
            pnl_Server_Info.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnl_Server_Info.AutoScroll = true;
            pnl_Server_Info.BorderStyle = BorderStyle.Fixed3D;
            pnl_Server_Info.Controls.Add(lbl_ip_adress);
            pnl_Server_Info.Controls.Add(label2);
            pnl_Server_Info.Controls.Add(lblRunningServer);
            pnl_Server_Info.Controls.Add(btn_execute);
            pnl_Server_Info.Controls.Add(btnStopServer);
            pnl_Server_Info.Controls.Add(pb_red);
            pnl_Server_Info.Controls.Add(pb_green);
            pnl_Server_Info.Location = new Point(688, 14);
            pnl_Server_Info.Name = "pnl_Server_Info";
            pnl_Server_Info.Size = new Size(1016, 773);
            pnl_Server_Info.TabIndex = 11;
            // 
            // lbl_ip_adress
            // 
            lbl_ip_adress.AutoSize = true;
            lbl_ip_adress.Location = new Point(213, 17);
            lbl_ip_adress.Name = "lbl_ip_adress";
            lbl_ip_adress.Size = new Size(124, 15);
            lbl_ip_adress.TabIndex = 22;
            lbl_ip_adress.Text = ".......................................";
            lbl_ip_adress.Click += lbl_ip_adress_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(119, 17);
            label2.Name = "label2";
            label2.Size = new Size(92, 15);
            label2.TabIndex = 21;
            label2.Text = " IPv4 Address.  : ";
            // 
            // lblRunningServer
            // 
            lblRunningServer.AutoSize = true;
            lblRunningServer.Location = new Point(40, 186);
            lblRunningServer.Name = "lblRunningServer";
            lblRunningServer.Size = new Size(73, 15);
            lblRunningServer.TabIndex = 3;
            lblRunningServer.Text = "......................";
            // 
            // btnStopServer
            // 
            btnStopServer.Location = new Point(12, 212);
            btnStopServer.Name = "btnStopServer";
            btnStopServer.Size = new Size(99, 23);
            btnStopServer.TabIndex = 2;
            btnStopServer.Text = "Stop server !";
            btnStopServer.UseVisualStyleBackColor = true;
            btnStopServer.Click += btnStopServer_Click;
            // 
            // pb_red
            // 
            pb_red.BackgroundImageLayout = ImageLayout.Stretch;
            pb_red.BorderStyle = BorderStyle.Fixed3D;
            pb_red.Image = (Image)resources.GetObject("pb_red.Image");
            pb_red.Location = new Point(14, 186);
            pb_red.Name = "pb_red";
            pb_red.Size = new Size(20, 20);
            pb_red.SizeMode = PictureBoxSizeMode.StretchImage;
            pb_red.TabIndex = 1;
            pb_red.TabStop = false;
            // 
            // pb_green
            // 
            pb_green.BackgroundImageLayout = ImageLayout.Stretch;
            pb_green.BorderStyle = BorderStyle.Fixed3D;
            pb_green.Image = (Image)resources.GetObject("pb_green.Image");
            pb_green.Location = new Point(14, 186);
            pb_green.Name = "pb_green";
            pb_green.Size = new Size(20, 20);
            pb_green.SizeMode = PictureBoxSizeMode.StretchImage;
            pb_green.TabIndex = 0;
            pb_green.TabStop = false;
            // 
            // pbButton6
            // 
            pbButton6.BorderStyle = BorderStyle.Fixed3D;
            pbButton6.Location = new Point(290, 33);
            pbButton6.Name = "pbButton6";
            pbButton6.Size = new Size(50, 50);
            pbButton6.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton6.TabIndex = 20;
            pbButton6.TabStop = false;
            pbButton6.Visible = false;
            // 
            // pbButton5
            // 
            pbButton5.BorderStyle = BorderStyle.Fixed3D;
            pbButton5.Location = new Point(234, 33);
            pbButton5.Name = "pbButton5";
            pbButton5.Size = new Size(50, 50);
            pbButton5.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton5.TabIndex = 19;
            pbButton5.TabStop = false;
            pbButton5.Visible = false;
            // 
            // pbButton4
            // 
            pbButton4.BorderStyle = BorderStyle.Fixed3D;
            pbButton4.Location = new Point(178, 33);
            pbButton4.Name = "pbButton4";
            pbButton4.Size = new Size(50, 50);
            pbButton4.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton4.TabIndex = 18;
            pbButton4.TabStop = false;
            pbButton4.Visible = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 1);
            label1.Name = "label1";
            label1.Size = new Size(35, 15);
            label1.TabIndex = 17;
            label1.Text = "Tools";
            // 
            // pbButton1
            // 
            pbButton1.BorderStyle = BorderStyle.Fixed3D;
            pbButton1.Location = new Point(10, 33);
            pbButton1.Name = "pbButton1";
            pbButton1.Size = new Size(50, 50);
            pbButton1.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton1.TabIndex = 16;
            pbButton1.TabStop = false;
            pbButton1.Visible = false;
            // 
            // pbButton2
            // 
            pbButton2.BorderStyle = BorderStyle.Fixed3D;
            pbButton2.Location = new Point(66, 33);
            pbButton2.Name = "pbButton2";
            pbButton2.Size = new Size(50, 50);
            pbButton2.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton2.TabIndex = 15;
            pbButton2.TabStop = false;
            pbButton2.Visible = false;
            // 
            // pbButton3
            // 
            pbButton3.BorderStyle = BorderStyle.Fixed3D;
            pbButton3.Location = new Point(122, 33);
            pbButton3.Name = "pbButton3";
            pbButton3.Size = new Size(50, 50);
            pbButton3.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton3.TabIndex = 14;
            pbButton3.TabStop = false;
            pbButton3.Visible = false;
            // 
            // btnRefreshBaseDayzServer
            // 
            btnRefreshBaseDayzServer.BackColor = Color.Transparent;
            btnRefreshBaseDayzServer.BackgroundImage = (Image)resources.GetObject("btnRefreshBaseDayzServer.BackgroundImage");
            btnRefreshBaseDayzServer.BackgroundImageLayout = ImageLayout.Stretch;
            btnRefreshBaseDayzServer.Location = new Point(1674, 54);
            btnRefreshBaseDayzServer.Name = "btnRefreshBaseDayzServer";
            btnRefreshBaseDayzServer.Size = new Size(50, 50);
            btnRefreshBaseDayzServer.TabIndex = 21;
            btnRefreshBaseDayzServer.UseVisualStyleBackColor = false;
            btnRefreshBaseDayzServer.Visible = false;
            btnRefreshBaseDayzServer.Click += btnRefreshBaseDayzServer_Click;
            // 
            // tbControllAll
            // 
            tbControllAll.Controls.Add(tp_SinglePlayer);
            tbControllAll.Controls.Add(tp_Multiplayer);
            tbControllAll.Location = new Point(10, 86);
            tbControllAll.Name = "tbControllAll";
            tbControllAll.SelectedIndex = 0;
            tbControllAll.Size = new Size(1718, 821);
            tbControllAll.TabIndex = 22;
            // 
            // tp_SinglePlayer
            // 
            tp_SinglePlayer.Controls.Add(panel_SP_info);
            tp_SinglePlayer.Controls.Add(panel2);
            tp_SinglePlayer.Location = new Point(4, 24);
            tp_SinglePlayer.Name = "tp_SinglePlayer";
            tp_SinglePlayer.Padding = new Padding(3);
            tp_SinglePlayer.Size = new Size(1710, 793);
            tp_SinglePlayer.TabIndex = 1;
            tp_SinglePlayer.Text = "Single-player";
            tp_SinglePlayer.UseVisualStyleBackColor = true;
            // 
            // panel_SP_info
            // 
            panel_SP_info.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel_SP_info.AutoScroll = true;
            panel_SP_info.BorderStyle = BorderStyle.Fixed3D;
            panel_SP_info.Controls.Add(label5);
            panel_SP_info.Controls.Add(button4);
            panel_SP_info.Controls.Add(button5);
            panel_SP_info.Controls.Add(pictureBox1);
            panel_SP_info.Controls.Add(pictureBox2);
            panel_SP_info.Location = new Point(688, 14);
            panel_SP_info.Name = "panel_SP_info";
            panel_SP_info.Size = new Size(1016, 773);
            panel_SP_info.TabIndex = 12;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(40, 186);
            label5.Name = "label5";
            label5.Size = new Size(73, 15);
            label5.TabIndex = 3;
            label5.Text = "......................";
            // 
            // button4
            // 
            button4.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button4.Location = new Point(12, 149);
            button4.Name = "button4";
            button4.Size = new Size(99, 23);
            button4.TabIndex = 11;
            button4.Text = "Run client !";
            button4.UseVisualStyleBackColor = true;
            // 
            // button5
            // 
            button5.Location = new Point(12, 212);
            button5.Name = "button5";
            button5.Size = new Size(99, 23);
            button5.TabIndex = 2;
            button5.Text = "Stop client !";
            button5.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.BorderStyle = BorderStyle.Fixed3D;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(14, 186);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(20, 20);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox2.BorderStyle = BorderStyle.Fixed3D;
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(14, 186);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(20, 20);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 0;
            pictureBox2.TabStop = false;
            // 
            // panel2
            // 
            panel2.BorderStyle = BorderStyle.Fixed3D;
            panel2.Controls.Add(btn_remove_sp);
            panel2.Controls.Add(listViewConfigsSP);
            panel2.Controls.Add(btn_edit_sp);
            panel2.Controls.Add(btn_add_sp);
            panel2.Location = new Point(8, 14);
            panel2.Name = "panel2";
            panel2.Size = new Size(674, 773);
            panel2.TabIndex = 11;
            // 
            // btn_remove_sp
            // 
            btn_remove_sp.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_remove_sp.Location = new Point(201, 743);
            btn_remove_sp.Name = "btn_remove_sp";
            btn_remove_sp.Size = new Size(75, 23);
            btn_remove_sp.TabIndex = 14;
            btn_remove_sp.Text = "Delete";
            btn_remove_sp.UseVisualStyleBackColor = true;
            btn_remove_sp.Click += btn_remove_sp_Click;
            // 
            // listViewConfigsSP
            // 
            listViewConfigsSP.Location = new Point(13, 17);
            listViewConfigsSP.Name = "listViewConfigsSP";
            listViewConfigsSP.Size = new Size(651, 720);
            listViewConfigsSP.TabIndex = 12;
            listViewConfigsSP.UseCompatibleStateImageBehavior = false;
            listViewConfigsSP.View = View.Details;
            // 
            // btn_edit_sp
            // 
            btn_edit_sp.Location = new Point(94, 743);
            btn_edit_sp.Name = "btn_edit_sp";
            btn_edit_sp.Size = new Size(101, 23);
            btn_edit_sp.TabIndex = 4;
            btn_edit_sp.Text = "Configurate";
            btn_edit_sp.UseVisualStyleBackColor = true;
            btn_edit_sp.Click += btn_edit_sp_Click;
            // 
            // btn_add_sp
            // 
            btn_add_sp.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_add_sp.Location = new Point(13, 743);
            btn_add_sp.Name = "btn_add_sp";
            btn_add_sp.Size = new Size(75, 23);
            btn_add_sp.TabIndex = 3;
            btn_add_sp.Text = "Add";
            btn_add_sp.UseVisualStyleBackColor = true;
            btn_add_sp.Click += btn_add_sp_Click;
            // 
            // tp_Multiplayer
            // 
            tp_Multiplayer.Controls.Add(panel1);
            tp_Multiplayer.Controls.Add(pnl_Server_Info);
            tp_Multiplayer.Location = new Point(4, 24);
            tp_Multiplayer.Name = "tp_Multiplayer";
            tp_Multiplayer.Padding = new Padding(3);
            tp_Multiplayer.Size = new Size(1710, 793);
            tp_Multiplayer.TabIndex = 0;
            tp_Multiplayer.Text = "Multi-player";
            tp_Multiplayer.UseVisualStyleBackColor = true;
            tp_Multiplayer.Click += tp_Multiplayer_Click;
            // 
            // pbButton10
            // 
            pbButton10.BorderStyle = BorderStyle.Fixed3D;
            pbButton10.Location = new Point(514, 33);
            pbButton10.Name = "pbButton10";
            pbButton10.Size = new Size(50, 50);
            pbButton10.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton10.TabIndex = 27;
            pbButton10.TabStop = false;
            pbButton10.Visible = false;
            // 
            // pbButton9
            // 
            pbButton9.BorderStyle = BorderStyle.Fixed3D;
            pbButton9.Location = new Point(458, 33);
            pbButton9.Name = "pbButton9";
            pbButton9.Size = new Size(50, 50);
            pbButton9.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton9.TabIndex = 26;
            pbButton9.TabStop = false;
            pbButton9.Visible = false;
            // 
            // pbButton8
            // 
            pbButton8.BorderStyle = BorderStyle.Fixed3D;
            pbButton8.Location = new Point(402, 33);
            pbButton8.Name = "pbButton8";
            pbButton8.Size = new Size(50, 50);
            pbButton8.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton8.TabIndex = 25;
            pbButton8.TabStop = false;
            pbButton8.Visible = false;
            // 
            // pbButton7
            // 
            pbButton7.BorderStyle = BorderStyle.Fixed3D;
            pbButton7.Location = new Point(346, 33);
            pbButton7.Name = "pbButton7";
            pbButton7.Size = new Size(50, 50);
            pbButton7.SizeMode = PictureBoxSizeMode.StretchImage;
            pbButton7.TabIndex = 24;
            pbButton7.TabStop = false;
            pbButton7.Visible = false;
            // 
            // tsTop
            // 
            tsTop.BackColor = Color.RoyalBlue;
            tsTop.GripMargin = new Padding(0);
            tsTop.GripStyle = ToolStripGripStyle.Hidden;
            tsTop.ImageScalingSize = new Size(25, 25);
            tsTop.Items.AddRange(new ToolStripItem[] { tsbSettings, tsbHelp, tsbEditChangeLog });
            tsTop.Location = new Point(0, 0);
            tsTop.Name = "tsTop";
            tsTop.Padding = new Padding(0, 1, 0, 1);
            tsTop.Size = new Size(1734, 29);
            tsTop.TabIndex = 28;
            tsTop.Text = "toolStrip1";
            tsTop.ItemClicked += tsTop_ItemClicked;
            // 
            // tsbSettings
            // 
            tsbSettings.Alignment = ToolStripItemAlignment.Right;
            tsbSettings.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSettings.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tsbSettings.ForeColor = Color.White;
            tsbSettings.Image = (Image)resources.GetObject("tsbSettings.Image");
            tsbSettings.ImageTransparentColor = Color.Magenta;
            tsbSettings.Name = "tsbSettings";
            tsbSettings.Size = new Size(34, 24);
            tsbSettings.Text = "⚙";
            tsbSettings.ToolTipText = "Settings (coming soon)";
            tsbSettings.Click += tsbSettings_Click;
            // 
            // tsbHelp
            // 
            tsbHelp.Alignment = ToolStripItemAlignment.Right;
            tsbHelp.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbHelp.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tsbHelp.ForeColor = Color.White;
            tsbHelp.Image = (Image)resources.GetObject("tsbHelp.Image");
            tsbHelp.ImageTransparentColor = Color.Magenta;
            tsbHelp.Name = "tsbHelp";
            tsbHelp.Size = new Size(34, 24);
            tsbHelp.Text = "❓";
            tsbHelp.ToolTipText = "Documentation (coming soon)";
            tsbHelp.Click += tsbHelp_Click;
            // 
            // tsbEditChangeLog
            // 
            tsbEditChangeLog.Alignment = ToolStripItemAlignment.Right;
            tsbEditChangeLog.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbEditChangeLog.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tsbEditChangeLog.ForeColor = Color.White;
            tsbEditChangeLog.Image = (Image)resources.GetObject("tsbEditChangeLog.Image");
            tsbEditChangeLog.ImageTransparentColor = Color.Magenta;
            tsbEditChangeLog.Name = "tsbEditChangeLog";
            tsbEditChangeLog.Size = new Size(34, 24);
            tsbEditChangeLog.Text = "🔑";
            tsbEditChangeLog.ToolTipText = "Settings (coming soon)";
            tsbEditChangeLog.Click += tsbEditChangeLog_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblActualVersion, lblVersion, lblBuildTime });
            statusStrip1.Location = new Point(0, 902);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1734, 30);
            statusStrip1.TabIndex = 29;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblActualVersion
            // 
            lblActualVersion.Name = "lblActualVersion";
            lblActualVersion.Size = new Size(1407, 25);
            lblActualVersion.Spring = true;
            lblActualVersion.Text = "toolStripStatusLabel1";
            lblActualVersion.Visible = false;
            // 
            // lblVersion
            // 
            lblVersion.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblVersion.IsLink = true;
            lblVersion.Margin = new Padding(0, 5, 0, 5);
            lblVersion.Name = "lblVersion";
            lblVersion.Padding = new Padding(10, 0, 0, 0);
            lblVersion.Size = new Size(161, 20);
            lblVersion.Text = "toolStripStatusLabel1";
            // 
            // lblBuildTime
            // 
            lblBuildTime.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblBuildTime.Margin = new Padding(0, 5, 0, 5);
            lblBuildTime.Name = "lblBuildTime";
            lblBuildTime.Size = new Size(151, 20);
            lblBuildTime.Text = "toolStripStatusLabel1";
            // 
            // lbl_downloading_text
            // 
            lbl_downloading_text.AutoSize = true;
            lbl_downloading_text.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbl_downloading_text.Location = new Point(650, 443);
            lbl_downloading_text.Name = "lbl_downloading_text";
            lbl_downloading_text.Size = new Size(507, 25);
            lbl_downloading_text.TabIndex = 31;
            lbl_downloading_text.Text = "Downloading and installing new version, please wait......";
            // 
            // pnl_loading_update
            // 
            pnl_loading_update.BackgroundImageLayout = ImageLayout.None;
            pnl_loading_update.Controls.Add(lbl_downloading_text);
            pnl_loading_update.Controls.Add(picLoading);
            pnl_loading_update.Location = new Point(0, 32);
            pnl_loading_update.Name = "pnl_loading_update";
            pnl_loading_update.Size = new Size(1734, 867);
            pnl_loading_update.TabIndex = 30;
            // 
            // picLoading
            // 
            picLoading.Image = (Image)resources.GetObject("picLoading.Image");
            picLoading.Location = new Point(0, 24);
            picLoading.Name = "picLoading";
            picLoading.Size = new Size(1734, 369);
            picLoading.SizeMode = PictureBoxSizeMode.CenterImage;
            picLoading.TabIndex = 30;
            picLoading.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(1734, 932);
            Controls.Add(pnl_loading_update);
            Controls.Add(statusStrip1);
            Controls.Add(tsTop);
            Controls.Add(pbButton10);
            Controls.Add(pbButton9);
            Controls.Add(pbButton8);
            Controls.Add(pbButton7);
            Controls.Add(btnRefreshBaseDayzServer);
            Controls.Add(pbButton6);
            Controls.Add(pbButton5);
            Controls.Add(pbButton4);
            Controls.Add(label1);
            Controls.Add(pbButton3);
            Controls.Add(pbButton1);
            Controls.Add(pbButton2);
            Controls.Add(tbControllAll);
            Name = "Form1";
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Bohemia Solutions";
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            pnl_Server_Info.ResumeLayout(false);
            pnl_Server_Info.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pb_red).EndInit();
            ((System.ComponentModel.ISupportInitialize)pb_green).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton6).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton5).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton4).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton3).EndInit();
            tbControllAll.ResumeLayout(false);
            tp_SinglePlayer.ResumeLayout(false);
            panel_SP_info.ResumeLayout(false);
            panel_SP_info.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            panel2.ResumeLayout(false);
            tp_Multiplayer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbButton10).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton9).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton8).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbButton7).EndInit();
            tsTop.ResumeLayout(false);
            tsTop.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            pnl_loading_update.ResumeLayout(false);
            pnl_loading_update.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picLoading).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button btn_add;
        private Button btn_edit;
        private Panel panel1;
        private Button btn_execute;
        private ListView listViewConfigs;
        private Button btn_remove;
        private Panel pnl_Server_Info;
        private PictureBox pb_green;
        private PictureBox pb_red;
        private Button btnStopServer;
        private Label lblRunningServer;
        private PictureBox pbButton3;
        private PictureBox pbButton2;
        private PictureBox pbButton1;
        private Label label1;
        private PictureBox pbButton4;
        private PictureBox pbButton5;
        private PictureBox pbButton6;
        private Label label2;
        private Label lbl_ip_adress;
        private Button btnRefreshBaseDayzServer;
        private TabControl tbControllAll;
        private TabPage tp_Multiplayer;
        private TabPage tp_SinglePlayer;
        private Panel panel_SP_info;
        private Label label5;
        private Button button4;
        private Button button5;
        private PictureBox pictureBox1;
        private PictureBox pictureBox2;
        private Panel panel2;
        private Button btn_remove_sp;
        private ListView listViewConfigsSP;
        private Button btn_edit_sp;
        private Button btn_add_sp;
        private PictureBox pbButton10;
        private PictureBox pbButton9;
        private PictureBox pbButton8;
        private PictureBox pbButton7;
        private ToolStrip tsTop;
        private ToolStripButton tsbSettings;
        private ToolStripButton tsbHelp;
        private ToolStripButton tsbEditChangeLog;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblActualVersion;
        private ToolStripStatusLabel lblVersion;
        private ToolStripStatusLabel lblBuildTime;
        private Label lbl_downloading_text;
        private Panel pnl_loading_update;
        private PictureBox picLoading;
    }
}
