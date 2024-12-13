using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PipeWorkshopApp.Models;
using PipeWorkshopApp.Services;
using System.Net.Http;
using PipeWorkshopApp.Properties;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.IO;
using Microsoft.EntityFrameworkCore;
using EasyModbus;
using System.Net;
using EmbedIO.Security;

namespace PipeWorkshopApp
{
    public partial class MainForm : Form
    {
        private HttpServerService _httpServerService;
        private CancellationTokenSource _cancellationTokenSource;
        public event EventHandler<string> LogMessageReceived;

        private Dictionary<string, int> _sectionCounters;    // �������� ���� �� ��������
        private Dictionary<string, int> _manualAdditions;    // ������ ���������� �� ��������
        private Dictionary<string, int> _manualRemovals;     // ������ �������� �� ��������

        private int _rejectedCount = 0; // ���������� ����������� ����

        private Dictionary<string, Label> _sectionLabels; // ������ ��� ��������
        private ContextMenuStrip _contextMenuSection;
        private string _currentRightClickSection;

        private Dictionary<string, ModbusService> _modbusServices = new Dictionary<string, ModbusService>();

        private string _stateFilePath = "state.json"; // ���� ��� ���������� ���������

        private int _karman1BatchNumber;
        private int _karman1BatchCount;

        private int _karman2BatchNumber;
        private int _karman2BatchCount;

        private int _karman3BatchNumber;
        private int _karman3BatchCount;

        private int _karman4BatchNumber;
        private int _karman4BatchCount;

       





public MainForm()
        {
            InitializeComponent();

            comboBoxK1Diameter.Items.AddRange(new object[] { "60", "73", "89" });
            comboBoxK1Diameter.SelectedIndex = 0;

            comboBoxK1Material.Items.AddRange(new object[] { "CR", "��" });
            comboBoxK1Material.SelectedIndex = 0;

            comboBoxK1Group.Items.AddRange(new object[] { "E", "L" });
            comboBoxK1Group.SelectedIndex = 0;

            comboBoxK2Diameter.Items.AddRange(new object[] { "60", "73", "89" });
            comboBoxK2Diameter.SelectedIndex = 0;

            comboBoxK2Material.Items.AddRange(new object[] { "CR", "��" });
            comboBoxK2Material.SelectedIndex = 0;

            comboBoxK2Group.Items.AddRange(new object[] { "E", "L" });
            comboBoxK2Group.SelectedIndex = 0;

            comboBoxK3Diameter.Items.AddRange(new object[] { "60", "73", "89" });
            comboBoxK3Diameter.SelectedIndex = 0;

            comboBoxK3Material.Items.AddRange(new object[] { "CR", "��" });
            comboBoxK3Material.SelectedIndex = 0;

            comboBoxK3Group.Items.AddRange(new object[] { "E", "L" });
            comboBoxK3Group.SelectedIndex = 0;

            comboBoxK4Diameter.Items.AddRange(new object[] { "60", "73", "89" });
            comboBoxK4Diameter.SelectedIndex = 0;

            comboBoxK4Material.Items.AddRange(new object[] { "CR", "��" });
            comboBoxK4Material.SelectedIndex = 0;

            comboBoxK4Group.Items.AddRange(new object[] { "E", "L" });
            comboBoxK4Group.SelectedIndex = 0;






            _httpServerService = new HttpServerService();
            _httpServerService.LogMessageReceived += (sender, msg) =>
            {
                LogMessage(msg); // ����� ������ ������ LogMessage, ������� ��������� listViewLog � �����
            };
            _httpServerService.MarkingDataReceived += HttpServerService_MarkingDataReceived;

            InitializeCounters();
            InitializeManualTrackers();
            InitializeContextMenu();

            // ����
            listViewLog.Columns.Add("���������", -2);
            listViewLog.View = View.Details;

            // ����������� �����
            listViewRejected.Columns.Add("�����", 200);
            listViewRejected.Columns.Add("�������", 200);
            listViewRejected.View = View.Details;

            CreateSectionLabels();

            listViewLog.KeyDown += listViewLog_KeyDown;

            LoadSettings();
            LoadKarmanBatchSettings();
            InitializeModbusServices();

            LoadState(); // ��������� ��������� �� �����
            UpdateSectionLabels();
            UpdateGlobalStats();

            this.Resize += MainForm_Resize;

            using (var dbContext = new AppDbContext())
            {
                dbContext.Database.Migrate();
            }
        }

        private void InitializeCounters()
        {
            // ��������� "����" ���� � ������ ��������
            _sectionCounters = new Dictionary<string, int>
            {
                {"�������", 0},
                {"��", 0},
                {"�������", 0},
                {"�������", 0},
                {"����������", 0},
                {"����������", 0},
                {"�������", 0},
                {"����", 0}
            };
        }

        private void InitializeManualTrackers()
        {
            _manualAdditions = new Dictionary<string, int>();
            _manualRemovals = new Dictionary<string, int>();

            foreach (var section in _sectionCounters.Keys)
            {
                _manualAdditions[section] = 0;
                _manualRemovals[section] = 0;
            }
        }

        private void InitializeContextMenu()
        {
            _contextMenuSection = new ContextMenuStrip();
            _contextMenuSection.Opening += _contextMenuSection_Opening;

            var addItem = new ToolStripMenuItem("�������� �����");
            addItem.Click += (s, e) => ChangePipeCountForSection(1);

            var removeItem = new ToolStripMenuItem("������� �����");
            removeItem.Click += (s, e) => ChangePipeCountForSection(-1);

            _contextMenuSection.Items.Add(addItem);
            _contextMenuSection.Items.Add(removeItem);
        }

        private void CreateSectionLabels()
        {
            _sectionLabels = new Dictionary<string, Label>();
            string[] sections = { "�������", "��", "�������", "�������", "����������", "����������", "�������", "����" };

            panelCounters.FlowDirection = FlowDirection.TopDown;
            panelCounters.WrapContents = true;
            panelCounters.AutoSize = true;
            panelCounters.AutoScroll = true;

            foreach (var section in sections)
            {
                var lbl = new Label();
                lbl.Tag = section;
                lbl.AutoSize = false;
                lbl.Height = 30;
                lbl.Font = new Font(lbl.Font.FontFamily, 12.0f, FontStyle.Bold);
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                lbl.ContextMenuStrip = _contextMenuSection;

                // ���� ��� "����", ������ �����
                if (section == "����")
                {
                    lbl.BackColor = Color.LightCoral;
                    lbl.ForeColor = Color.White;
                }

                panelCounters.Controls.Add(lbl);
                _sectionLabels[section] = lbl;
            }

            AdjustLabelWidths();
        }

        private void AdjustLabelWidths()
        {
            int width = panelCounters.ClientSize.Width - panelCounters.Padding.Left - panelCounters.Padding.Right;
            foreach (var lbl in _sectionLabels.Values)
            {
                lbl.Width = width;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            AdjustLabelWidths();
        }

        private void _contextMenuSection_Opening(object sender, CancelEventArgs e)
        {
            if (_contextMenuSection.SourceControl is Label lbl && lbl.Tag is string sectionName)
            {
                _currentRightClickSection = sectionName;
            }
            else
            {
                _currentRightClickSection = null;
                e.Cancel = true;
            }
        }

        private void ChangePipeCountForSection(int delta)
        {
            if (string.IsNullOrEmpty(_currentRightClickSection)) return;

            string section = _currentRightClickSection;
            int newCount = _sectionCounters[section] + delta;
            if (newCount < 0)
            {
                LogMessage($"���������� ������� {Math.Abs(delta)} �����(�) � '{section}', ��� ����� {_sectionCounters[section]}.");
                return;
            }

            _sectionCounters[section] = newCount;
            if (delta > 0)
            {
                _manualAdditions[section] += delta;
                LogMessage($"{delta} ���� ��������� �� '{section}'. �����: {_sectionCounters[section]}");
            }
            else
            {
                int absDelta = Math.Abs(delta);
                _manualRemovals[section] += absDelta;
                LogMessage($"{absDelta} ���� ������� � '{section}'. �����: {_sectionCounters[section]}");
            }

            UpdateSectionLabels();
            UpdateGlobalStats();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopMainLoop();
            _httpServerService.StopServer();
            foreach (var modbusService in _modbusServices.Values)
                modbusService.Disconnect();

            SaveState(); // ��������� ��������� ��� ��������
        }

        private void InitializeModbusServices()
        {
            try
            {
                _modbusServices.Clear();

                _modbusServices["��������"] = new ModbusService(
                    Properties.Settings.Default["��������_IP"] as string,
                    (int)Properties.Settings.Default["��������_Port"],
                    (int)Properties.Settings.Default["��������_Register"]
                );

                _modbusServices["�������_Good"] = new ModbusService(
                    Properties.Settings.Default["�������_Good_IP"] as string,
                    (int)Properties.Settings.Default["�������_Good_Port"],
                    (int)Properties.Settings.Default["�������_Good_Register"]
                );

                _modbusServices["�������_Reject"] = new ModbusService(
                    Properties.Settings.Default["�������_Reject_IP"] as string,
                    (int)Properties.Settings.Default["�������_Reject_Port"],
                    (int)Properties.Settings.Default["�������_Reject_Register"]
                );

                _modbusServices["��_Good"] = new ModbusService(
                    Properties.Settings.Default["��_Good_IP"] as string,
                    (int)Properties.Settings.Default["��_Good_Port"],
                    (int)Properties.Settings.Default["��_Good_Register"]
                );

                _modbusServices["��_Reject"] = new ModbusService(
                    Properties.Settings.Default["��_Reject_IP"] as string,
                    (int)Properties.Settings.Default["��_Reject_Port"],
                    (int)Properties.Settings.Default["��_Reject_Register"]
                );

                _modbusServices["�������"] = new ModbusService(
                    Properties.Settings.Default["�������_IP"] as string,
                    (int)Properties.Settings.Default["�������_Port"],
                    (int)Properties.Settings.Default["�������_Register"]
                );

                _modbusServices["�������"] = new ModbusService(
                    Properties.Settings.Default["�������_IP"] as string,
                    (int)Properties.Settings.Default["�������_Port"],
                    (int)Properties.Settings.Default["�������_Register"]
                );

                _modbusServices["����������_Good"] = new ModbusService(
                    Properties.Settings.Default["����������_Good_IP"] as string,
                    (int)Properties.Settings.Default["����������_Good_Port"],
                    (int)Properties.Settings.Default["����������_Good_Register"]
                );

                _modbusServices["����������_Reject"] = new ModbusService(
                    Properties.Settings.Default["����������_Reject_IP"] as string,
                    (int)Properties.Settings.Default["����������_Reject_Port"],
                    (int)Properties.Settings.Default["����������_Reject_Register"]
                );

                _modbusServices["����������"] = new ModbusService(
                    Properties.Settings.Default["����������_IP"] as string,
                    (int)Properties.Settings.Default["����������_Port"],
                    (int)Properties.Settings.Default["����������_Register"]
                );

                _modbusServices["�������"] = new ModbusService(
                    Properties.Settings.Default["�������_IP"] as string,
                    (int)Properties.Settings.Default["�������_Port"],
                    (int)Properties.Settings.Default["�������_Register"]
                );
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ������������� Modbus-��������: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            textBoxTriggerDelay.Text = Properties.Settings.Default.TriggerDelay.ToString();

            textBoxServerIP.Text = Properties.Settings.Default.ServerIP;
            textBoxServerPort.Text = Properties.Settings.Default.ServerPort.ToString();

            textBoxCreation_IP.Text = Properties.Settings.Default["��������_IP"] as string;
            textBoxCreation_Port.Text = Properties.Settings.Default["��������_Port"].ToString();
            textBoxCreation_Register.Text = Properties.Settings.Default["��������_Register"].ToString();

            textBoxSharoshkaGood_IP.Text = Properties.Settings.Default["�������_Good_IP"] as string;
            textBoxSharoshkaGood_Port.Text = Properties.Settings.Default["�������_Good_Port"].ToString();
            textBoxSharoshkaGood_Register.Text = Properties.Settings.Default["�������_Good_Register"].ToString();

            textBoxSharoshkaReject_IP.Text = Properties.Settings.Default["�������_Reject_IP"] as string;
            textBoxSharoshkaReject_Port.Text = Properties.Settings.Default["�������_Reject_Port"].ToString();
            textBoxSharoshkaReject_Register.Text = Properties.Settings.Default["�������_Reject_Register"].ToString();

            textBox��Good_IP.Text = Properties.Settings.Default["��_Good_IP"] as string;
            textBox��Good_Port.Text = Properties.Settings.Default["��_Good_Port"].ToString();
            textBox��Good_Register.Text = Properties.Settings.Default["��_Good_Register"].ToString();

            textBox��Reject_IP.Text = Properties.Settings.Default["��_Reject_IP"] as string;
            textBox��Reject_Port.Text = Properties.Settings.Default["��_Reject_Port"].ToString();
            textBox��Reject_Register.Text = Properties.Settings.Default["��_Reject_Register"].ToString();

            textBoxOtvorot_IP.Text = Properties.Settings.Default["�������_IP"] as string;
            textBoxOtvorot_Port.Text = Properties.Settings.Default["�������_Port"].ToString();
            textBoxOtvorot_Register.Text = Properties.Settings.Default["�������_Register"].ToString();

            textBoxOpressovkaGood_IP.Text = Properties.Settings.Default["����������_Good_IP"] as string;
            textBoxOpressovkaGood_Port.Text = Properties.Settings.Default["����������_Good_Port"].ToString();
            textBoxOpressovkaGood_Register.Text = Properties.Settings.Default["����������_Good_Register"].ToString();

            textBoxOpressovkaReject_IP.Text = Properties.Settings.Default["����������_Reject_IP"] as string;
            textBoxOpressovkaReject_Port.Text = Properties.Settings.Default["����������_Reject_Port"].ToString();
            textBoxOpressovkaReject_Register.Text = Properties.Settings.Default["����������_Reject_Register"].ToString();


            textBoxKarman_IP.Text = Properties.Settings.Default["�������_IP"] as string;
            textBoxKarman_Port.Text = Properties.Settings.Default["�������_Port"].ToString();
            textBoxKarman_Register.Text = Properties.Settings.Default["�������_Register"].ToString();





            textBoxKarmanIp1.Text = Properties.Settings.Default["textBoxKarmanIp1"] as string;
            textBoxKarmanPort1.Text = Properties.Settings.Default["textBoxKarmanPort1"].ToString();
            textBoxKarmanRegister1.Text = Properties.Settings.Default["textBoxKarmanRegister1"].ToString();

            textBoxKarmanIp2.Text = Properties.Settings.Default["textBoxKarmanIp2"] as string;
            textBoxKarmanPort2.Text = Properties.Settings.Default["textBoxKarmanPort2"].ToString();
            textBoxKarmanRegister2.Text = Properties.Settings.Default["textBoxKarmanRegister2"].ToString();

            textBoxKarmanIp3.Text = Properties.Settings.Default["textBoxKarmanIp3"] as string;
            textBoxKarmanPort3.Text = Properties.Settings.Default["textBoxKarmanPort3"].ToString();
            textBoxKarmanRegister3.Text = Properties.Settings.Default["textBoxKarmanRegister3"].ToString();

            textBoxKarmanIp4.Text = Properties.Settings.Default["textBoxKarmanIp4"] as string;
            textBoxKarmanPort4.Text = Properties.Settings.Default["textBoxKarmanPort4"].ToString();
            textBoxKarmanRegister4.Text = Properties.Settings.Default["textBoxKarmanRegister4"].ToString();


        }

        private void SaveSettings()
        {
            Properties.Settings.Default.TriggerDelay = int.Parse(textBoxTriggerDelay.Text);

            Properties.Settings.Default.ServerIP = textBoxServerIP.Text;
            Properties.Settings.Default.ServerPort = int.Parse(textBoxServerPort.Text);

            Properties.Settings.Default["��������_IP"] = textBoxCreation_IP.Text;
            Properties.Settings.Default["��������_Port"] = int.Parse(textBoxCreation_Port.Text);
            Properties.Settings.Default["��������_Register"] = int.Parse(textBoxCreation_Register.Text);

            Properties.Settings.Default["�������_Good_IP"] = textBoxSharoshkaGood_IP.Text;
            Properties.Settings.Default["�������_Good_Port"] = int.Parse(textBoxSharoshkaGood_Port.Text);
            Properties.Settings.Default["�������_Good_Register"] = int.Parse(textBoxSharoshkaGood_Register.Text);

            Properties.Settings.Default["�������_Reject_IP"] = textBoxSharoshkaReject_IP.Text;
            Properties.Settings.Default["�������_Reject_Port"] = int.Parse(textBoxSharoshkaReject_Port.Text);
            Properties.Settings.Default["�������_Reject_Register"] = int.Parse(textBoxSharoshkaReject_Register.Text);

            Properties.Settings.Default["��_Good_IP"] = textBox��Good_IP.Text;
            Properties.Settings.Default["��_Good_Port"] = int.Parse(textBox��Good_Port.Text);
            Properties.Settings.Default["��_Good_Register"] = int.Parse(textBox��Good_Register.Text);

            Properties.Settings.Default["��_Reject_IP"] = textBox��Reject_IP.Text;
            Properties.Settings.Default["��_Reject_Port"] = int.Parse(textBox��Reject_Port.Text);
            Properties.Settings.Default["��_Reject_Register"] = int.Parse(textBox��Reject_Register.Text);

            Properties.Settings.Default["�������_IP"] = textBoxOtvorot_IP.Text;
            Properties.Settings.Default["�������_Port"] = int.Parse(textBoxOtvorot_Port.Text);
            Properties.Settings.Default["�������_Register"] = int.Parse(textBoxOtvorot_Register.Text);

            Properties.Settings.Default["����������_Good_IP"] = textBoxOpressovkaGood_IP.Text;
            Properties.Settings.Default["����������_Good_Port"] = int.Parse(textBoxOpressovkaGood_Port.Text);
            Properties.Settings.Default["����������_Good_Register"] = int.Parse(textBoxOpressovkaGood_Register.Text);

            Properties.Settings.Default["����������_Reject_IP"] = textBoxOpressovkaReject_IP.Text;
            Properties.Settings.Default["����������_Reject_Port"] = int.Parse(textBoxOpressovkaReject_Port.Text);
            Properties.Settings.Default["����������_Reject_Register"] = int.Parse(textBoxOpressovkaReject_Register.Text);



            Properties.Settings.Default["�������_IP"] = textBoxKarman_IP.Text;
            Properties.Settings.Default["�������_Port"] = int.Parse(textBoxKarman_Port.Text);
            Properties.Settings.Default["�������_Register"] = int.Parse(textBoxKarman_Register.Text);



            Properties.Settings.Default["textBoxKarmanIp1"] = textBoxKarmanIp1.Text;
            Properties.Settings.Default["textBoxKarmanPort1"] = int.Parse(textBoxKarmanPort1.Text);
            Properties.Settings.Default["textBoxKarmanRegister1"] = int.Parse(textBoxKarmanRegister1.Text);

            Properties.Settings.Default["textBoxKarmanIp2"] = textBoxKarmanIp2.Text;
            Properties.Settings.Default["textBoxKarmanPort2"] = int.Parse(textBoxKarmanPort2.Text);
            Properties.Settings.Default["textBoxKarmanRegister2"] = int.Parse(textBoxKarmanRegister2.Text);

            Properties.Settings.Default["textBoxKarmanIp3"] = textBoxKarmanIp3.Text;
            Properties.Settings.Default["textBoxKarmanPort3"] = int.Parse(textBoxKarmanPort3.Text);
            Properties.Settings.Default["textBoxKarmanRegister3"] = int.Parse(textBoxKarmanRegister3.Text);

            Properties.Settings.Default["textBoxKarmanIp4"] = textBoxKarmanIp4.Text;
            Properties.Settings.Default["textBoxKarmanPort4"] = int.Parse(textBoxKarmanPort4.Text);
            Properties.Settings.Default["textBoxKarmanRegister4"] = int.Parse(textBoxKarmanRegister4.Text);

            Properties.Settings.Default.Save();
        }


        private void StartMainLoop()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["��������"].CheckTrigger())
                        {
                            _sectionCounters["�������"]++;
                            LogMessage($"����� ��������� �� '�������'. �����: {_sectionCounters["�������"]}");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["�������_Good"].CheckTrigger())
                        {
                            MovePipe("�������", "��");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["�������_Reject"].CheckTrigger())
                        {
                            RejectPipe("�������");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["��_Good"].CheckTrigger())
                        {
                            MovePipe("��", "�������");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["��_Reject"].CheckTrigger())
                        {
                            RejectPipe("�������");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["�������"].CheckTrigger())
                        {
                            MovePipe("�������", "�������");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["�������"].CheckTrigger())
                        {
                            MovePipe("�������", "����������");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["����������_Good"].CheckTrigger())
                        {
                            MovePipe("����������", "����������");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["����������_Reject"].CheckTrigger())
                        {
                            RejectPipe("����������");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }


                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["�������"].CheckTrigger())
                        {
                            KarmanFunction();
                        }

                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"������ � �������� �����: {ex.Message}");
                    }
                }
                LogMessage("�������� ���� ����������.");
            }, token);
        }


        private void StopMainLoop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void MovePipe(string fromSection, string toSection)
        {
            if (_sectionCounters[fromSection] > 0)
            {
                _sectionCounters[fromSection]--;
                _sectionCounters[toSection]++;
                LogMessage($"����� ���������� �� '{fromSection}' � '{toSection}'. " +
                           $"'{fromSection}': {_sectionCounters[fromSection]}, '{toSection}': {_sectionCounters[toSection]}");
            }
            else
            {
                LogMessage($"��� ���� � '{fromSection}' ��� ����������� � '{toSection}'.");
            }
        }

        private void RejectPipe(string sectionName)
        {
            if (_sectionCounters[sectionName] > 0)
            {
                _sectionCounters[sectionName]--;
                _rejectedCount++;

                var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
                item.SubItems.Add(sectionName);
                listViewRejected.Items.Add(item);

                LogMessage($"����� ����������� �� ������� '{sectionName}'. ����� �����������: {_rejectedCount}");
            }
            else
            {
                LogMessage($"��� ���� �� ������� '{sectionName}' ��� �����.");
            }
        }


        private void HttpServerService_MarkingDataReceived(object sender, MarkingData markingData)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    ProcessMarkingDataOnUIThread(markingData);
                }));
            }
            else
            {
                ProcessMarkingDataOnUIThread(markingData);
            }
        }

        private void ProcessMarkingDataOnUIThread(MarkingData markingData)
        {
            if (_sectionCounters["����������"] > 0)
            {
                _sectionCounters["����������"]--;
                _sectionCounters["�������"]++;
                LogMessage($"���������� ��������� ��� ����� {markingData.PipeNumber}. '����������': {_sectionCounters["����������"]}, '�������': {_sectionCounters["�������"]}");
                SaveMarkedPipeData(markingData);
            }
            else
            {
                LogMessage("��� ���� �� ������� '����������' ��� ���������� ����������.");
            }
            UpdateSectionLabels();
            UpdateGlobalStats();
        }


        private void SaveMarkedPipeData(MarkingData markingData)
        {
            using var dbContext = new AppDbContext();
            var pipeData = new PipeData
            {
                PipeNumber = markingData.PipeNumber,
                Diameter = markingData.Diameter,
                Material = markingData.Material,
                Group = markingData.Group,
                PipeLength = markingData.PipeLength,
                Thickness = markingData.Thickness,
            };
            dbContext.Pipes.Add(pipeData);
            dbContext.SaveChanges();

            LogMessage($"������ � ���������� ��������� � �� ��� ����� {markingData.PipeNumber}.");
        }

        private void UpdateSectionLabels()
        {
            foreach (var section in _sectionLabels.Keys)
            {
                int count = _sectionCounters[section];
                int addCount = _manualAdditions[section];
                int removeCount = _manualRemovals[section];

                _sectionLabels[section].Text = $"{section}: {count}         (������:{addCount - removeCount})";
            }
        }

        private void UpdateGlobalStats()
        {
            int totalAdd = _manualAdditions.Values.Sum();
            int totalRemove = _manualRemovals.Values.Sum();
            int totalInKarmany = _sectionCounters["�������"];
            int totalInBrak = _sectionCounters["����"];

            labelGlobalStats.Height = 30;
            labelGlobalStats.Font = new Font(labelGlobalStats.Font.FontFamily, 12.0f, FontStyle.Bold);

            // ��������, � ��� ���� labelGlobalStats �� �����
            labelGlobalStats.Text =
                $"���������� ����������:\n" +
                $"������ ��������������: {totalAdd - totalRemove}\n" +
                $"����� � �������: {totalInKarmany}\n" +
                $"������ ���������� � ����: {totalInBrak}\n" +
                $"����� �����������: {_rejectedCount + totalInBrak}";
        }

        private void LoadState()
        {
            if (!File.Exists(_stateFilePath)) return;

            try
            {
                var json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<SaveState>(json);

                if (state != null)
                {
                    _sectionCounters = state.SectionCounters;
                    _manualAdditions = state.ManualAdditions;
                    _manualRemovals = state.ManualRemovals;
                    _rejectedCount = state.RejectedCount;

                    listViewRejected.Items.Clear();
                    foreach (var rec in state.RejectedRecords)
                    {
                        var item = new ListViewItem(rec.Time);
                        item.SubItems.Add(rec.Section);
                        listViewRejected.Items.Add(item);
                    }

                    LogMessage("��������� ���������.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� �������� ���������: {ex.Message}");
            }
        }

        private void SaveState()
        {
            var state = new SaveState
            {
                SectionCounters = _sectionCounters,
                ManualAdditions = _manualAdditions,
                ManualRemovals = _manualRemovals,
                RejectedCount = _rejectedCount,
                RejectedRecords = listViewRejected.Items.Cast<ListViewItem>()
                    .Select(it => new RejectedRecord { Time = it.Text, Section = it.SubItems[1].Text })
                    .ToList()
            };

            try
            {
                SaveKarmanBatchSettings();
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_stateFilePath, json);
                LogMessage("��������� ���������.");
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ���������� ���������: {ex.Message}");
            }
        }

        private void buttonResetState_Click(object sender, EventArgs e)
        {
            // ����� ���������
            foreach (var key in _sectionCounters.Keys.ToList())
                _sectionCounters[key] = 0;

            foreach (var key in _manualAdditions.Keys.ToList())
                _manualAdditions[key] = 0;

            foreach (var key in _manualRemovals.Keys.ToList())
                _manualRemovals[key] = 0;

            _rejectedCount = 0;
            listViewRejected.Items.Clear();

            UpdateSectionLabels();
            UpdateGlobalStats();
            LogMessage("��������� ��������.");
        }

        private async void button_start_Click(object sender, EventArgs e)
        {
            await _httpServerService.StartServer(Properties.Settings.Default.ServerIP, Properties.Settings.Default.ServerPort);
            // ������ ����� �������, �� �������� UI
            StartMainLoop();
            LogMessage("���������� ��������.");
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            StopMainLoop();
            LogMessage("���������� �����������.");
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            SaveSettings();
            SaveKarmanBatchSettings();
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            LoadSettings();
            LoadKarmanBatchSettings();
            InitializeModbusServices();
        }

        private void listViewLog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedItemsToClipboard();
                e.SuppressKeyPress = true;
            }
        }

        private void CopySelectedItemsToClipboard()
        {
            if (listViewLog.SelectedItems.Count > 0)
            {
                var lines = listViewLog.SelectedItems.Cast<ListViewItem>()
                              .Select(item => item.Text);
                string clipboardText = string.Join(Environment.NewLine, lines);
                Clipboard.SetText(clipboardText);
                LogMessage("��������� �������� ����������� � ����� ������.");
            }
            else
            {
                LogMessage("��� ��������� ��������� ��� �����������.");
            }
        }

        private void LogMessage(string message)
        {
            if (listViewLog.InvokeRequired)
            {
                listViewLog.Invoke(new Action(() =>
                {
                    var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss") + " - " + message);
                    listViewLog.Items.Add(item);
                    listViewLog.EnsureVisible(listViewLog.Items.Count - 1);
                }));
            }
            else
            {
                var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss") + " - " + message);
                listViewLog.Items.Add(item);
                listViewLog.EnsureVisible(listViewLog.Items.Count - 1);
            }
        }

        private void SaveKarmanBatchSettings()
        {
            Properties.Settings.Default.Karman1BatchNumber = _karman1BatchNumber;
            Properties.Settings.Default.Karman1BatchCount = _karman1BatchCount;

            Properties.Settings.Default.Karman2BatchNumber = _karman2BatchNumber;
            Properties.Settings.Default.Karman2BatchCount = _karman2BatchCount;

            Properties.Settings.Default.Karman3BatchNumber = _karman3BatchNumber;
            Properties.Settings.Default.Karman3BatchCount = _karman3BatchCount;

            Properties.Settings.Default.Karman4BatchNumber = _karman4BatchNumber;
            Properties.Settings.Default.Karman4BatchCount = _karman4BatchCount;

            Properties.Settings.Default.Save();
        }

        private void LoadKarmanBatchSettings()
        {
            _karman1BatchNumber = Properties.Settings.Default.Karman1BatchNumber;
            _karman1BatchCount = Properties.Settings.Default.Karman1BatchCount;

            _karman2BatchNumber = Properties.Settings.Default.Karman2BatchNumber;
            _karman2BatchCount = Properties.Settings.Default.Karman2BatchCount;

            _karman3BatchNumber = Properties.Settings.Default.Karman3BatchNumber;
            _karman3BatchCount = Properties.Settings.Default.Karman3BatchCount;

            _karman4BatchNumber = Properties.Settings.Default.Karman4BatchNumber;
            _karman4BatchCount = Properties.Settings.Default.Karman4BatchCount;

            UpdateKarmanUI(); // ������� ���������
        }

        private void UpdateKarmanUI()
        {
            textBoxK1CurrentBatch.Text = _karman1BatchNumber.ToString();
            textBoxK1CurrentCount.Text = _karman1BatchCount.ToString();

            textBoxK2CurrentBatch.Text = _karman2BatchNumber.ToString();
            textBoxK2CurrentCount.Text = _karman2BatchCount.ToString();

            textBoxK3CurrentBatch.Text = _karman3BatchNumber.ToString();
            textBoxK3CurrentCount.Text = _karman3BatchCount.ToString();

            textBoxK4CurrentBatch.Text = _karman4BatchNumber.ToString();
            textBoxK4CurrentCount.Text = _karman4BatchCount.ToString();
        }

        private void buttonCloseBatch1_Click(object sender, EventArgs e)
        {
            CloseBatch(1);
        }

        private void buttonCloseBatch2_Click(object sender, EventArgs e)
        {
            CloseBatch(2);
        }

        private void buttonCloseBatch3_Click(object sender, EventArgs e)
        {
            CloseBatch(3);
        }

        private void buttonCloseBatch4_Click(object sender, EventArgs e)
        {
            CloseBatch(4);
        }

        private void GenerateDocumentForBatch(int karmanNumber, int batchNumber)
        {
            LogMessage($"��������� �������� ��� ������� {karmanNumber}, ������ {batchNumber}...");
            // ����� ����� ������ ������������ ���������, ���� �����.
        }

        private void CloseBatch(int karmanNumber)
        {
            // ������ "��������" �����: �������� GenerateDocumentForBatch, 
            // �������������� ����� ������, ���������� �������
            switch (karmanNumber)
            {
                case 1:
                    GenerateDocumentForBatch(1, _karman1BatchNumber);
                    _karman1BatchNumber++;
                    _karman1BatchCount = 0;
                    break;
                case 2:
                    GenerateDocumentForBatch(2, _karman2BatchNumber);
                    _karman2BatchNumber++;
                    _karman2BatchCount = 0;
                    break;
                case 3:
                    GenerateDocumentForBatch(3, _karman3BatchNumber);
                    _karman3BatchNumber++;
                    _karman3BatchCount = 0;
                    break;
                case 4:
                    GenerateDocumentForBatch(4, _karman4BatchNumber);
                    _karman4BatchNumber++;
                    _karman4BatchCount = 0;
                    break;
            }

            UpdateKarmanUI();
            SaveKarmanBatchSettings();
        }

        private void KarmanFunction()
        {
            using (var dbContext = new AppDbContext())
            {
                var pipe = dbContext.Pipes
                    .Where(p => p.BatchNumber == 0)
                    .OrderBy(p => p.Id)
                    .FirstOrDefault();

                if (pipe == null)
                {
                    LogMessage("��� ���� ��� ������������� �� ��������.");
                    return;
                }

                // ��������� ��������� �������� �� ComboBox � TextBox
                string k1Diameter = comboBoxK1Diameter.SelectedItem.ToString();
                string k1Material = comboBoxK1Material.SelectedItem.ToString();
                string k1Group = comboBoxK1Group.SelectedItem.ToString();
                int k1BatchSize = int.Parse(textBoxK1BatchSize.Text);

                string k2Diameter = comboBoxK2Diameter.SelectedItem.ToString();
                string k2Material = comboBoxK2Material.SelectedItem.ToString();
                string k2Group = comboBoxK2Group.SelectedItem.ToString();
                int k2BatchSize = int.Parse(textBoxK2BatchSize.Text);

                string k3Diameter = comboBoxK3Diameter.SelectedItem.ToString();
                string k3Material = comboBoxK3Material.SelectedItem.ToString();
                string k3Group = comboBoxK3Group.SelectedItem.ToString();
                int k3BatchSize = int.Parse(textBoxK3BatchSize.Text);

                string k4Diameter = comboBoxK4Diameter.SelectedItem.ToString();
                string k4Material = comboBoxK4Material.SelectedItem.ToString();
                string k4Group = comboBoxK4Group.SelectedItem.ToString();
                int k4BatchSize = int.Parse(textBoxK4BatchSize.Text);

                int chosenKarman = 0;

                // ������ if-else ��� �������� ���������� ����������
                if (pipe.Diameter == k1Diameter && pipe.Material == k1Material && pipe.Group == k1Group)
                {
                    chosenKarman = 1;
                    // ������������� Modbus-������� ��� ������� 1

                    var ip = Properties.Settings.Default["textBoxKarmanIp1"].ToString();
                    var port = int.Parse(Properties.Settings.Default["textBoxKarmanPort1"].ToString());
                    var register = int.Parse(Properties.Settings.Default["textBoxKarmanRegister1"].ToString());


                    SetKarmanModbusRegister(ip, port, register);

                    var maxBatchNumber = dbContext.Pipes.Max(p => (int?)p.BatchNumber) ?? 0;
                    _karman1BatchNumber = maxBatchNumber + 1;

                    _karman1BatchCount++;
                    pipe.BatchNumber = _karman1BatchNumber;
                    dbContext.SaveChanges();

                    UpdateKarmanUI();     // ��������� ���������
                    SaveKarmanBatchSettings(); // ��������� ���������

                    LogMessage($"����� {pipe.PipeNumber} -> ������ 1, ������ {pipe.BatchNumber}, � ������ {_karman1BatchCount}/{k1BatchSize}.");

                    // ���� �������� ������� ������ - ��������� �
                    if (_karman1BatchCount == k1BatchSize)
                    {
                        GenerateDocumentForBatch(1, _karman1BatchNumber);
                        _karman1BatchNumber++;
                        _karman1BatchCount = 0;
                        UpdateKarmanUI();     // ��������� ���������
                        SaveKarmanBatchSettings(); // ��������� ���������
                    }
                }
                else if (pipe.Diameter == k2Diameter && pipe.Material == k2Material && pipe.Group == k2Group)
                {
                    chosenKarman = 2;

                    var ip = Properties.Settings.Default["textBoxKarmanIp2"].ToString();
                    var port = int.Parse(Properties.Settings.Default["textBoxKarmanPort2"].ToString());
                    var register = int.Parse(Properties.Settings.Default["textBoxKarmanRegister2"].ToString());
                    SetKarmanModbusRegister(ip, port, register);

                    var maxBatchNumber = dbContext.Pipes.Max(p => (int?)p.BatchNumber) ?? 0;
                    _karman2BatchNumber = maxBatchNumber + 1;

                    _karman2BatchCount++;
                    pipe.BatchNumber = _karman2BatchNumber;
                    dbContext.SaveChanges();
                    UpdateKarmanUI();     // ��������� ���������
                    SaveKarmanBatchSettings(); // ��������� ���������

                    LogMessage($"����� {pipe.PipeNumber} -> ������ 2, ������ {pipe.BatchNumber}, � ������ {_karman2BatchCount}/{k2BatchSize}.");

                    if (_karman2BatchCount == k2BatchSize)
                    {
                        GenerateDocumentForBatch(2, _karman2BatchNumber);
                        _karman2BatchNumber++;
                        _karman2BatchCount = 0;
                        UpdateKarmanUI();     // ��������� ���������
                        SaveKarmanBatchSettings(); // ��������� ���������
                    }
                }
                else if (pipe.Diameter == k3Diameter && pipe.Material == k3Material && pipe.Group == k3Group)
                {
                    chosenKarman = 3;


                    var ip = Properties.Settings.Default["textBoxKarmanIp3"].ToString();
                    var port = int.Parse(Properties.Settings.Default["textBoxKarmanPort3"].ToString());
                    var register = int.Parse(Properties.Settings.Default["textBoxKarmanRegister3"].ToString());
                    SetKarmanModbusRegister(ip, port, register);

                    var maxBatchNumber = dbContext.Pipes.Max(p => (int?)p.BatchNumber) ?? 0;
                    _karman3BatchNumber = maxBatchNumber + 1;

                    _karman3BatchCount++;
                    pipe.BatchNumber = _karman3BatchNumber;
                    dbContext.SaveChanges();
                    UpdateKarmanUI();     // ��������� ���������
                    SaveKarmanBatchSettings(); // ��������� ���������

                    LogMessage($"����� {pipe.PipeNumber} -> ������ 3, ������ {pipe.BatchNumber}, � ������ {_karman3BatchCount}/{k3BatchSize}.");

                    if (_karman3BatchCount == k3BatchSize)
                    {
                        GenerateDocumentForBatch(3, _karman3BatchNumber);
                        _karman3BatchNumber++;
                        _karman3BatchCount = 0;
                        UpdateKarmanUI();     // ��������� ���������
                        SaveKarmanBatchSettings(); // ��������� ���������
                    }
                }
                else if (pipe.Diameter == k4Diameter && pipe.Material == k4Material && pipe.Group == k4Group)
                {
                    chosenKarman = 4;



                    var ip = Properties.Settings.Default["textBoxKarmanIp4"].ToString();
                    var port = int.Parse(Properties.Settings.Default["textBoxKarmanPort4"].ToString());
                    var register = int.Parse(Properties.Settings.Default["textBoxKarmanRegister4"].ToString());
                    SetKarmanModbusRegister(ip, port, register);

                    var maxBatchNumber = dbContext.Pipes.Max(p => (int?)p.BatchNumber) ?? 0;
                    _karman4BatchNumber = maxBatchNumber + 1;

                    _karman4BatchCount++;
                    pipe.BatchNumber = _karman4BatchNumber;
                    dbContext.SaveChanges();
                    UpdateKarmanUI();     // ��������� ���������
                    SaveKarmanBatchSettings(); // ��������� ���������

                    LogMessage($"����� {pipe.PipeNumber} -> ������ 4, ������ {pipe.BatchNumber}, � ������ {_karman4BatchCount}/{k4BatchSize}.");

                    if (_karman4BatchCount == k4BatchSize)
                    {
                        GenerateDocumentForBatch(4, _karman4BatchNumber);
                        _karman4BatchNumber++;
                        _karman4BatchCount = 0;
                        UpdateKarmanUI();     // ��������� ���������
                        SaveKarmanBatchSettings(); // ��������� ���������
                    }
                }
                else
                {
                    LogMessage("�� ������� ����������� ����� �� ������ �������.");
                }
            }
        }

        private void SetKarmanModbusRegister(string ipAddress, int port, int register)
        {
            
            try
            {
                var client = new ModbusClient(ipAddress, port);

                client.Connect();

                client.WriteSingleRegister(register, 1);

                client.Disconnect();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ������ Modbus: {ex.Message}");
                // �������������� ��������� ��� ����������� ������
            }
       
         }
    }

    public class SaveState
    {
        public Dictionary<string, int> SectionCounters { get; set; }
        public Dictionary<string, int> ManualAdditions { get; set; }
        public Dictionary<string, int> ManualRemovals { get; set; }
        public int RejectedCount { get; set; }
        public List<RejectedRecord> RejectedRecords { get; set; } = new List<RejectedRecord>();
    }

    public class RejectedRecord
    {
        public string Time { get; set; }
        public string Section { get; set; }
    }
}
