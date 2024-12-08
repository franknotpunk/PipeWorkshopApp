namespace PipeWorkshopApp
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            textBoxServerIP = new TextBox();
            textBoxServerPort = new TextBox();
            textBoxCreation_Register = new TextBox();
            textBoxCreation_IP = new TextBox();
            textBoxCreation_Port = new TextBox();
            groupBox_sharoska = new GroupBox();
            groupBox_NK = new GroupBox();
            textBoxSharoshkaGood_Port = new TextBox();
            textBoxSharoshkaGood_IP = new TextBox();
            textBoxSharoshkaGood_Register = new TextBox();
            groupBox_Tokarka = new GroupBox();
            textBoxSharoshkaReject_Port = new TextBox();
            textBoxSharoshkaReject_IP = new TextBox();
            textBoxSharoshkaReject_Register = new TextBox();
            groupBox_Otvorot = new GroupBox();
            textBoxНКGood_Port = new TextBox();
            textBoxНКGood_IP = new TextBox();
            textBoxНКGood_Register = new TextBox();
            groupBox_Pressed = new GroupBox();
            textBoxTokarka_Port = new TextBox();
            textBoxTokarka_IP = new TextBox();
            textBoxTokarka_Register = new TextBox();
            groupBox_Marker = new GroupBox();
            textBoxOtvorot_Port = new TextBox();
            textBoxOtvorot_IP = new TextBox();
            textBoxOtvorot_Register = new TextBox();
            button_save = new Button();
            button_load = new Button();
            groupBox1 = new GroupBox();
            textBoxOpressovkaGood_Port = new TextBox();
            textBoxOpressovkaGood_IP = new TextBox();
            textBoxOpressovkaGood_Register = new TextBox();
            groupBox2 = new GroupBox();
            textBoxMarkirovka_Port = new TextBox();
            textBoxMarkirovka_IP = new TextBox();
            textBoxMarkirovka_Register = new TextBox();
            groupBox3 = new GroupBox();
            textBoxKarman_Port = new TextBox();
            textBoxKarman_IP = new TextBox();
            textBoxKarman_Register = new TextBox();
            groupBox4 = new GroupBox();
            textBoxOpressovkaReject_Port = new TextBox();
            textBoxOpressovkaReject_IP = new TextBox();
            textBoxOpressovkaReject_Register = new TextBox();
            button_start = new Button();
            button_stop = new Button();
            contextMenu = new ContextMenuStrip(components);
            addPipeToolStripMenuItem = new ToolStripMenuItem();
            deletePipeToolStripMenuItem = new ToolStripMenuItem();
            listViewLog = new ListView();
            listViewRejected = new ListView();
            panelCounters = new FlowLayoutPanel();
            buttonResetState = new Button();
            labelGlobalStats = new Label();
            НК_Reject = new GroupBox();
            textBoxНКReject_Port = new TextBox();
            textBoxНКReject_IP = new TextBox();
            textBoxНКReject_Register = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            groupBox_sharoska.SuspendLayout();
            groupBox_NK.SuspendLayout();
            groupBox_Tokarka.SuspendLayout();
            groupBox_Otvorot.SuspendLayout();
            groupBox_Pressed.SuspendLayout();
            groupBox_Marker.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            contextMenu.SuspendLayout();
            НК_Reject.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxServerIP
            // 
            textBoxServerIP.Location = new Point(357, 54);
            textBoxServerIP.Name = "textBoxServerIP";
            textBoxServerIP.Size = new Size(332, 23);
            textBoxServerIP.TabIndex = 7;
            // 
            // textBoxServerPort
            // 
            textBoxServerPort.Location = new Point(12, 54);
            textBoxServerPort.Name = "textBoxServerPort";
            textBoxServerPort.Size = new Size(332, 23);
            textBoxServerPort.TabIndex = 8;
            // 
            // textBoxCreation_Register
            // 
            textBoxCreation_Register.Location = new Point(254, 22);
            textBoxCreation_Register.Name = "textBoxCreation_Register";
            textBoxCreation_Register.Size = new Size(64, 23);
            textBoxCreation_Register.TabIndex = 2;
            // 
            // textBoxCreation_IP
            // 
            textBoxCreation_IP.Location = new Point(6, 22);
            textBoxCreation_IP.Name = "textBoxCreation_IP";
            textBoxCreation_IP.Size = new Size(172, 23);
            textBoxCreation_IP.TabIndex = 0;
            // 
            // textBoxCreation_Port
            // 
            textBoxCreation_Port.Location = new Point(184, 22);
            textBoxCreation_Port.Name = "textBoxCreation_Port";
            textBoxCreation_Port.Size = new Size(64, 23);
            textBoxCreation_Port.TabIndex = 1;
            // 
            // groupBox_sharoska
            // 
            groupBox_sharoska.Controls.Add(textBoxCreation_Port);
            groupBox_sharoska.Controls.Add(textBoxCreation_IP);
            groupBox_sharoska.Controls.Add(textBoxCreation_Register);
            groupBox_sharoska.Location = new Point(896, 32);
            groupBox_sharoska.Name = "groupBox_sharoska";
            groupBox_sharoska.Size = new Size(332, 57);
            groupBox_sharoska.TabIndex = 10;
            groupBox_sharoska.TabStop = false;
            groupBox_sharoska.Text = "Создание";
            // 
            // groupBox_NK
            // 
            groupBox_NK.Controls.Add(textBoxSharoshkaGood_Port);
            groupBox_NK.Controls.Add(textBoxSharoshkaGood_IP);
            groupBox_NK.Controls.Add(textBoxSharoshkaGood_Register);
            groupBox_NK.Location = new Point(896, 95);
            groupBox_NK.Name = "groupBox_NK";
            groupBox_NK.Size = new Size(332, 57);
            groupBox_NK.TabIndex = 11;
            groupBox_NK.TabStop = false;
            groupBox_NK.Text = "Шарошка_Good";
            // 
            // textBoxSharoshkaGood_Port
            // 
            textBoxSharoshkaGood_Port.Location = new Point(184, 22);
            textBoxSharoshkaGood_Port.Name = "textBoxSharoshkaGood_Port";
            textBoxSharoshkaGood_Port.Size = new Size(64, 23);
            textBoxSharoshkaGood_Port.TabIndex = 1;
            // 
            // textBoxSharoshkaGood_IP
            // 
            textBoxSharoshkaGood_IP.Location = new Point(6, 22);
            textBoxSharoshkaGood_IP.Name = "textBoxSharoshkaGood_IP";
            textBoxSharoshkaGood_IP.Size = new Size(172, 23);
            textBoxSharoshkaGood_IP.TabIndex = 0;
            // 
            // textBoxSharoshkaGood_Register
            // 
            textBoxSharoshkaGood_Register.Location = new Point(254, 22);
            textBoxSharoshkaGood_Register.Name = "textBoxSharoshkaGood_Register";
            textBoxSharoshkaGood_Register.Size = new Size(64, 23);
            textBoxSharoshkaGood_Register.TabIndex = 2;
            // 
            // groupBox_Tokarka
            // 
            groupBox_Tokarka.Controls.Add(textBoxSharoshkaReject_Port);
            groupBox_Tokarka.Controls.Add(textBoxSharoshkaReject_IP);
            groupBox_Tokarka.Controls.Add(textBoxSharoshkaReject_Register);
            groupBox_Tokarka.Location = new Point(896, 158);
            groupBox_Tokarka.Name = "groupBox_Tokarka";
            groupBox_Tokarka.Size = new Size(332, 57);
            groupBox_Tokarka.TabIndex = 11;
            groupBox_Tokarka.TabStop = false;
            groupBox_Tokarka.Text = "Шарошка_Reject";
            // 
            // textBoxSharoshkaReject_Port
            // 
            textBoxSharoshkaReject_Port.Location = new Point(184, 22);
            textBoxSharoshkaReject_Port.Name = "textBoxSharoshkaReject_Port";
            textBoxSharoshkaReject_Port.Size = new Size(64, 23);
            textBoxSharoshkaReject_Port.TabIndex = 1;
            // 
            // textBoxSharoshkaReject_IP
            // 
            textBoxSharoshkaReject_IP.Location = new Point(6, 22);
            textBoxSharoshkaReject_IP.Name = "textBoxSharoshkaReject_IP";
            textBoxSharoshkaReject_IP.Size = new Size(172, 23);
            textBoxSharoshkaReject_IP.TabIndex = 0;
            // 
            // textBoxSharoshkaReject_Register
            // 
            textBoxSharoshkaReject_Register.Location = new Point(254, 22);
            textBoxSharoshkaReject_Register.Name = "textBoxSharoshkaReject_Register";
            textBoxSharoshkaReject_Register.Size = new Size(64, 23);
            textBoxSharoshkaReject_Register.TabIndex = 2;
            // 
            // groupBox_Otvorot
            // 
            groupBox_Otvorot.Controls.Add(textBoxНКGood_Port);
            groupBox_Otvorot.Controls.Add(textBoxНКGood_IP);
            groupBox_Otvorot.Controls.Add(textBoxНКGood_Register);
            groupBox_Otvorot.Location = new Point(896, 221);
            groupBox_Otvorot.Name = "groupBox_Otvorot";
            groupBox_Otvorot.Size = new Size(332, 57);
            groupBox_Otvorot.TabIndex = 11;
            groupBox_Otvorot.TabStop = false;
            groupBox_Otvorot.Text = "НК_Good";
            // 
            // textBoxНКGood_Port
            // 
            textBoxНКGood_Port.Location = new Point(184, 22);
            textBoxНКGood_Port.Name = "textBoxНКGood_Port";
            textBoxНКGood_Port.Size = new Size(64, 23);
            textBoxНКGood_Port.TabIndex = 1;
            // 
            // textBoxНКGood_IP
            // 
            textBoxНКGood_IP.Location = new Point(6, 22);
            textBoxНКGood_IP.Name = "textBoxНКGood_IP";
            textBoxНКGood_IP.Size = new Size(172, 23);
            textBoxНКGood_IP.TabIndex = 0;
            // 
            // textBoxНКGood_Register
            // 
            textBoxНКGood_Register.Location = new Point(254, 22);
            textBoxНКGood_Register.Name = "textBoxНКGood_Register";
            textBoxНКGood_Register.Size = new Size(64, 23);
            textBoxНКGood_Register.TabIndex = 2;
            // 
            // groupBox_Pressed
            // 
            groupBox_Pressed.Controls.Add(textBoxTokarka_Port);
            groupBox_Pressed.Controls.Add(textBoxTokarka_IP);
            groupBox_Pressed.Controls.Add(textBoxTokarka_Register);
            groupBox_Pressed.Location = new Point(896, 347);
            groupBox_Pressed.Name = "groupBox_Pressed";
            groupBox_Pressed.Size = new Size(332, 57);
            groupBox_Pressed.TabIndex = 11;
            groupBox_Pressed.TabStop = false;
            groupBox_Pressed.Text = "Токарка";
            // 
            // textBoxTokarka_Port
            // 
            textBoxTokarka_Port.Location = new Point(184, 22);
            textBoxTokarka_Port.Name = "textBoxTokarka_Port";
            textBoxTokarka_Port.Size = new Size(64, 23);
            textBoxTokarka_Port.TabIndex = 1;
            // 
            // textBoxTokarka_IP
            // 
            textBoxTokarka_IP.Location = new Point(6, 22);
            textBoxTokarka_IP.Name = "textBoxTokarka_IP";
            textBoxTokarka_IP.Size = new Size(172, 23);
            textBoxTokarka_IP.TabIndex = 0;
            // 
            // textBoxTokarka_Register
            // 
            textBoxTokarka_Register.Location = new Point(254, 22);
            textBoxTokarka_Register.Name = "textBoxTokarka_Register";
            textBoxTokarka_Register.Size = new Size(64, 23);
            textBoxTokarka_Register.TabIndex = 2;
            // 
            // groupBox_Marker
            // 
            groupBox_Marker.Controls.Add(textBoxOtvorot_Port);
            groupBox_Marker.Controls.Add(textBoxOtvorot_IP);
            groupBox_Marker.Controls.Add(textBoxOtvorot_Register);
            groupBox_Marker.Location = new Point(896, 410);
            groupBox_Marker.Name = "groupBox_Marker";
            groupBox_Marker.Size = new Size(332, 57);
            groupBox_Marker.TabIndex = 11;
            groupBox_Marker.TabStop = false;
            groupBox_Marker.Text = "Отворот";
            // 
            // textBoxOtvorot_Port
            // 
            textBoxOtvorot_Port.Location = new Point(184, 22);
            textBoxOtvorot_Port.Name = "textBoxOtvorot_Port";
            textBoxOtvorot_Port.Size = new Size(64, 23);
            textBoxOtvorot_Port.TabIndex = 1;
            // 
            // textBoxOtvorot_IP
            // 
            textBoxOtvorot_IP.Location = new Point(6, 22);
            textBoxOtvorot_IP.Name = "textBoxOtvorot_IP";
            textBoxOtvorot_IP.Size = new Size(172, 23);
            textBoxOtvorot_IP.TabIndex = 0;
            // 
            // textBoxOtvorot_Register
            // 
            textBoxOtvorot_Register.Location = new Point(254, 22);
            textBoxOtvorot_Register.Name = "textBoxOtvorot_Register";
            textBoxOtvorot_Register.Size = new Size(64, 23);
            textBoxOtvorot_Register.TabIndex = 2;
            // 
            // button_save
            // 
            button_save.Location = new Point(900, 725);
            button_save.Name = "button_save";
            button_save.Size = new Size(154, 54);
            button_save.TabIndex = 12;
            button_save.Text = "SAVE";
            button_save.UseVisualStyleBackColor = true;
            button_save.Click += button_save_Click;
            // 
            // button_load
            // 
            button_load.Location = new Point(1060, 725);
            button_load.Name = "button_load";
            button_load.Size = new Size(154, 54);
            button_load.TabIndex = 13;
            button_load.Text = "LOAD";
            button_load.UseVisualStyleBackColor = true;
            button_load.Click += button_load_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textBoxOpressovkaGood_Port);
            groupBox1.Controls.Add(textBoxOpressovkaGood_IP);
            groupBox1.Controls.Add(textBoxOpressovkaGood_Register);
            groupBox1.Location = new Point(896, 473);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(332, 57);
            groupBox1.TabIndex = 12;
            groupBox1.TabStop = false;
            groupBox1.Text = "Опрессовка_Good";
            // 
            // textBoxOpressovkaGood_Port
            // 
            textBoxOpressovkaGood_Port.Location = new Point(184, 22);
            textBoxOpressovkaGood_Port.Name = "textBoxOpressovkaGood_Port";
            textBoxOpressovkaGood_Port.Size = new Size(64, 23);
            textBoxOpressovkaGood_Port.TabIndex = 1;
            // 
            // textBoxOpressovkaGood_IP
            // 
            textBoxOpressovkaGood_IP.Location = new Point(6, 22);
            textBoxOpressovkaGood_IP.Name = "textBoxOpressovkaGood_IP";
            textBoxOpressovkaGood_IP.Size = new Size(172, 23);
            textBoxOpressovkaGood_IP.TabIndex = 0;
            // 
            // textBoxOpressovkaGood_Register
            // 
            textBoxOpressovkaGood_Register.Location = new Point(254, 22);
            textBoxOpressovkaGood_Register.Name = "textBoxOpressovkaGood_Register";
            textBoxOpressovkaGood_Register.Size = new Size(64, 23);
            textBoxOpressovkaGood_Register.TabIndex = 2;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textBoxMarkirovka_Port);
            groupBox2.Controls.Add(textBoxMarkirovka_IP);
            groupBox2.Controls.Add(textBoxMarkirovka_Register);
            groupBox2.Location = new Point(896, 599);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(332, 57);
            groupBox2.TabIndex = 12;
            groupBox2.TabStop = false;
            groupBox2.Text = "Маркировка";
            // 
            // textBoxMarkirovka_Port
            // 
            textBoxMarkirovka_Port.Location = new Point(184, 22);
            textBoxMarkirovka_Port.Name = "textBoxMarkirovka_Port";
            textBoxMarkirovka_Port.Size = new Size(64, 23);
            textBoxMarkirovka_Port.TabIndex = 1;
            // 
            // textBoxMarkirovka_IP
            // 
            textBoxMarkirovka_IP.Location = new Point(6, 22);
            textBoxMarkirovka_IP.Name = "textBoxMarkirovka_IP";
            textBoxMarkirovka_IP.Size = new Size(172, 23);
            textBoxMarkirovka_IP.TabIndex = 0;
            // 
            // textBoxMarkirovka_Register
            // 
            textBoxMarkirovka_Register.Location = new Point(254, 22);
            textBoxMarkirovka_Register.Name = "textBoxMarkirovka_Register";
            textBoxMarkirovka_Register.Size = new Size(64, 23);
            textBoxMarkirovka_Register.TabIndex = 2;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(textBoxKarman_Port);
            groupBox3.Controls.Add(textBoxKarman_IP);
            groupBox3.Controls.Add(textBoxKarman_Register);
            groupBox3.Location = new Point(896, 662);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(332, 57);
            groupBox3.TabIndex = 12;
            groupBox3.TabStop = false;
            groupBox3.Text = "Карманы";
            // 
            // textBoxKarman_Port
            // 
            textBoxKarman_Port.Location = new Point(184, 22);
            textBoxKarman_Port.Name = "textBoxKarman_Port";
            textBoxKarman_Port.Size = new Size(64, 23);
            textBoxKarman_Port.TabIndex = 1;
            // 
            // textBoxKarman_IP
            // 
            textBoxKarman_IP.Location = new Point(6, 22);
            textBoxKarman_IP.Name = "textBoxKarman_IP";
            textBoxKarman_IP.Size = new Size(172, 23);
            textBoxKarman_IP.TabIndex = 0;
            // 
            // textBoxKarman_Register
            // 
            textBoxKarman_Register.Location = new Point(254, 22);
            textBoxKarman_Register.Name = "textBoxKarman_Register";
            textBoxKarman_Register.Size = new Size(64, 23);
            textBoxKarman_Register.TabIndex = 2;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(textBoxOpressovkaReject_Port);
            groupBox4.Controls.Add(textBoxOpressovkaReject_IP);
            groupBox4.Controls.Add(textBoxOpressovkaReject_Register);
            groupBox4.Location = new Point(896, 536);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(332, 57);
            groupBox4.TabIndex = 13;
            groupBox4.TabStop = false;
            groupBox4.Text = "Опрессовка_Reject";
            // 
            // textBoxOpressovkaReject_Port
            // 
            textBoxOpressovkaReject_Port.Location = new Point(184, 22);
            textBoxOpressovkaReject_Port.Name = "textBoxOpressovkaReject_Port";
            textBoxOpressovkaReject_Port.Size = new Size(64, 23);
            textBoxOpressovkaReject_Port.TabIndex = 1;
            // 
            // textBoxOpressovkaReject_IP
            // 
            textBoxOpressovkaReject_IP.Location = new Point(6, 22);
            textBoxOpressovkaReject_IP.Name = "textBoxOpressovkaReject_IP";
            textBoxOpressovkaReject_IP.Size = new Size(172, 23);
            textBoxOpressovkaReject_IP.TabIndex = 0;
            // 
            // textBoxOpressovkaReject_Register
            // 
            textBoxOpressovkaReject_Register.Location = new Point(254, 22);
            textBoxOpressovkaReject_Register.Name = "textBoxOpressovkaReject_Register";
            textBoxOpressovkaReject_Register.Size = new Size(64, 23);
            textBoxOpressovkaReject_Register.TabIndex = 2;
            // 
            // button_start
            // 
            button_start.Location = new Point(901, 785);
            button_start.Name = "button_start";
            button_start.Size = new Size(153, 53);
            button_start.TabIndex = 23;
            button_start.Text = "START";
            button_start.UseVisualStyleBackColor = true;
            button_start.Click += button_start_Click;
            // 
            // button_stop
            // 
            button_stop.Location = new Point(1060, 785);
            button_stop.Name = "button_stop";
            button_stop.Size = new Size(153, 53);
            button_stop.TabIndex = 24;
            button_stop.Text = "STOP";
            button_stop.UseVisualStyleBackColor = true;
            button_stop.Click += button_stop_Click;
            // 
            // contextMenu
            // 
            contextMenu.Items.AddRange(new ToolStripItem[] { addPipeToolStripMenuItem, deletePipeToolStripMenuItem });
            contextMenu.Name = "contextMenuStrip1";
            contextMenu.Size = new Size(131, 48);
            // 
            // addPipeToolStripMenuItem
            // 
            addPipeToolStripMenuItem.Name = "addPipeToolStripMenuItem";
            addPipeToolStripMenuItem.Size = new Size(130, 22);
            addPipeToolStripMenuItem.Text = "AddPipe";
            // 
            // deletePipeToolStripMenuItem
            // 
            deletePipeToolStripMenuItem.Name = "deletePipeToolStripMenuItem";
            deletePipeToolStripMenuItem.Size = new Size(130, 22);
            deletePipeToolStripMenuItem.Text = "DeletePipe";
            // 
            // listViewLog
            // 
            listViewLog.Location = new Point(453, 459);
            listViewLog.Name = "listViewLog";
            listViewLog.Size = new Size(437, 379);
            listViewLog.TabIndex = 25;
            listViewLog.UseCompatibleStateImageBehavior = false;
            // 
            // listViewRejected
            // 
            listViewRejected.Location = new Point(12, 459);
            listViewRejected.Name = "listViewRejected";
            listViewRejected.Size = new Size(435, 379);
            listViewRejected.TabIndex = 27;
            listViewRejected.UseCompatibleStateImageBehavior = false;
            // 
            // panelCounters
            // 
            panelCounters.Location = new Point(12, 146);
            panelCounters.Name = "panelCounters";
            panelCounters.Size = new Size(633, 307);
            panelCounters.TabIndex = 28;
            // 
            // buttonResetState
            // 
            buttonResetState.Location = new Point(736, 399);
            buttonResetState.Name = "buttonResetState";
            buttonResetState.Size = new Size(154, 54);
            buttonResetState.TabIndex = 30;
            buttonResetState.Text = "RESET";
            buttonResetState.UseVisualStyleBackColor = true;
            buttonResetState.Click += buttonResetState_Click;
            // 
            // labelGlobalStats
            // 
            labelGlobalStats.AutoSize = true;
            labelGlobalStats.Location = new Point(651, 146);
            labelGlobalStats.Name = "labelGlobalStats";
            labelGlobalStats.Size = new Size(38, 15);
            labelGlobalStats.TabIndex = 31;
            labelGlobalStats.Text = "label1";
            // 
            // НК_Reject
            // 
            НК_Reject.Controls.Add(textBoxНКReject_Port);
            НК_Reject.Controls.Add(textBoxНКReject_IP);
            НК_Reject.Controls.Add(textBoxНКReject_Register);
            НК_Reject.Location = new Point(896, 284);
            НК_Reject.Name = "НК_Reject";
            НК_Reject.Size = new Size(332, 57);
            НК_Reject.TabIndex = 12;
            НК_Reject.TabStop = false;
            НК_Reject.Text = "НК";
            // 
            // textBoxНКReject_Port
            // 
            textBoxНКReject_Port.Location = new Point(184, 22);
            textBoxНКReject_Port.Name = "textBoxНКReject_Port";
            textBoxНКReject_Port.Size = new Size(64, 23);
            textBoxНКReject_Port.TabIndex = 1;
            // 
            // textBoxНКReject_IP
            // 
            textBoxНКReject_IP.Location = new Point(6, 22);
            textBoxНКReject_IP.Name = "textBoxНКReject_IP";
            textBoxНКReject_IP.Size = new Size(172, 23);
            textBoxНКReject_IP.TabIndex = 0;
            // 
            // textBoxНКReject_Register
            // 
            textBoxНКReject_Register.Location = new Point(254, 22);
            textBoxНКReject_Register.Name = "textBoxНКReject_Register";
            textBoxНКReject_Register.Size = new Size(64, 23);
            textBoxНКReject_Register.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(14, 29);
            label1.Name = "label1";
            label1.Size = new Size(215, 15);
            label1.TabIndex = 32;
            label1.Text = "IP на котором запущено приложение";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(357, 29);
            label2.Name = "label2";
            label2.Size = new Size(227, 15);
            label2.TabIndex = 33;
            label2.Text = "Port на котором запущено приложение";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 120);
            label3.Name = "label3";
            label3.Size = new Size(55, 15);
            label3.TabIndex = 34;
            label3.Text = "Очереди";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1243, 945);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(НК_Reject);
            Controls.Add(labelGlobalStats);
            Controls.Add(buttonResetState);
            Controls.Add(panelCounters);
            Controls.Add(listViewRejected);
            Controls.Add(listViewLog);
            Controls.Add(button_stop);
            Controls.Add(button_start);
            Controls.Add(groupBox4);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(button_load);
            Controls.Add(button_save);
            Controls.Add(groupBox_Marker);
            Controls.Add(groupBox_Pressed);
            Controls.Add(groupBox_Otvorot);
            Controls.Add(groupBox_Tokarka);
            Controls.Add(groupBox_NK);
            Controls.Add(groupBox_sharoska);
            Controls.Add(textBoxServerPort);
            Controls.Add(textBoxServerIP);
            Name = "MainForm";
            Text = "Form1";
            groupBox_sharoska.ResumeLayout(false);
            groupBox_sharoska.PerformLayout();
            groupBox_NK.ResumeLayout(false);
            groupBox_NK.PerformLayout();
            groupBox_Tokarka.ResumeLayout(false);
            groupBox_Tokarka.PerformLayout();
            groupBox_Otvorot.ResumeLayout(false);
            groupBox_Otvorot.PerformLayout();
            groupBox_Pressed.ResumeLayout(false);
            groupBox_Pressed.PerformLayout();
            groupBox_Marker.ResumeLayout(false);
            groupBox_Marker.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            contextMenu.ResumeLayout(false);
            НК_Reject.ResumeLayout(false);
            НК_Reject.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox textBoxServerIP;
        private TextBox textBoxServerPort;
        private TextBox textBoxCreation_Register;
        private TextBox textBoxCreation_IP;
        private TextBox textBoxCreation_Port;
        private GroupBox groupBox_sharoska;
        private GroupBox groupBox_NK;
        private TextBox textBoxSharoshkaGood_Port;
        private TextBox textBoxSharoshkaGood_IP;
        private TextBox textBoxSharoshkaGood_Register;
        private GroupBox groupBox_Tokarka;
        private TextBox textBoxSharoshkaReject_Port;
        private TextBox textBoxSharoshkaReject_IP;
        private TextBox textBoxSharoshkaReject_Register;
        private GroupBox groupBox_Otvorot;
        private TextBox textBoxНКGood_Port;
        private TextBox textBoxНКGood_IP;
        private TextBox textBoxНКGood_Register;
        private GroupBox groupBox_Pressed;
        private TextBox textBoxTokarka_Port;
        private TextBox textBoxTokarka_IP;
        private TextBox textBoxTokarka_Register;
        private GroupBox groupBox_Marker;
        private TextBox textBoxOtvorot_Port;
        private TextBox textBoxOtvorot_IP;
        private TextBox textBoxOtvorot_Register;
        private Button button_save;
        private Button button_load;
        private GroupBox groupBox1;
        private TextBox textBoxOpressovkaGood_Port;
        private TextBox textBoxOpressovkaGood_IP;
        private TextBox textBoxOpressovkaGood_Register;
        private GroupBox groupBox2;
        private TextBox textBoxMarkirovka_Port;
        private TextBox textBoxMarkirovka_IP;
        private TextBox textBoxMarkirovka_Register;
        private GroupBox groupBox3;
        private TextBox textBoxKarman_Port;
        private TextBox textBoxKarman_IP;
        private TextBox textBoxKarman_Register;
        private GroupBox groupBox4;
        private TextBox textBoxOpressovkaReject_Port;
        private TextBox textBoxOpressovkaReject_IP;
        private TextBox textBoxOpressovkaReject_Register;
        private Button button_start;
        private Button button_stop;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem addPipeToolStripMenuItem;
        private ToolStripMenuItem deletePipeToolStripMenuItem;
        private ListView listViewLog;
        private ListView listViewRejected;
        private FlowLayoutPanel panelCounters;
        private Button buttonResetState;
        private Label labelGlobalStats;
        private GroupBox НК_Reject;
        private TextBox textBoxНКReject_Port;
        private TextBox textBoxНКReject_IP;
        private TextBox textBoxНКReject_Register;
        private Label label1;
        private Label label2;
        private Label label3;
    }
}
