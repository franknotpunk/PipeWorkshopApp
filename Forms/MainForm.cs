using PipeWorkshopApp.Models;
using PipeWorkshopApp.Services;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using EasyModbus;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using Xceed.Words.NET;
using Xceed.Document.NET;

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
        private int _rejectedCountShablon = 0; // ���������� ����������� ����
        private int _rejectedCountNK = 0; // ���������� ����������� ����
        private int _rejectedCountPressed = 0; // ���������� ����������� ����


        private bool _isRunning = false;

        private Dictionary<string, Label> _sectionLabels; // ������ ��� ��������
        private ContextMenuStrip _contextMenuSection;
        private string _currentRightClickSection;

        private Dictionary<string, ModbusService> _modbusServices = new Dictionary<string, ModbusService>();

        private string _stateFilePath = "state.json"; // ���� ��� ���������� ���������

        private int _karman1BatchNumber;
        private int _karman1BatchCount;
        private int _karman1BatchSize;

        private int _karman2BatchNumber;
        private int _karman2BatchCount;
        private int _karman2BatchSize;

        private int _karman3BatchNumber;
        private int _karman3BatchCount;
        private int _karman3BatchSize;

        private int _karman4BatchNumber;
        private int _karman4BatchCount;
        private int _karman4BatchSize;

        public MainForm()
        {
            InitializeComponent();

            InitCombobox();

            InitLogs();
            InitLogsReceived();

            InitializeCounters();

            InitializeManualTrackers();
            InitializeContextMenu();

            CreateSectionLabels();

            // ����������� ������������ ������� ��� ��������� �������� ��������
            AttachKarmanSettingsEventHandlers();

            LoadSettings();
            LoadKarmanBatchSettings();
            InitializeModbusServices();

            LoadState();
            UpdateSectionLabels();
            UpdateGlobalStats();

            InitMigration();
            this.Resize += MainForm_Resize;
            button_start.Enabled = true;
            button_stop.Enabled = false;
        }

        #region Initialization Methods

        private void AttachKarmanSettingsEventHandlers()
        {
            // ������ 1
            textBoxKarmanIp1.TextChanged += (s, e) => SaveKarmanStringSetting("KarmanIp1", textBoxKarmanIp1.Text);
            textBoxKarmanPort1.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanPort1.Text, out int port))
                {
                    SaveKarmanIntSetting("KarmanPort1", port);
                }
                else
                {
                    LogMessage("������������ ���� ��� KarmanPort1. ��������� ����� �����.");
                }
            };
            textBoxKarmanRegister1.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanRegister1.Text, out int register))
                {
                    SaveKarmanIntSetting("KarmanRegister1", register);
                }
                else
                {
                    LogMessage("������������ ���� ��� KarmanRegister1. ��������� ����� �����.");
                }
            };

            // ������ 2
            textBoxKarmanIp2.TextChanged += (s, e) => SaveKarmanStringSetting("KarmanIp2", textBoxKarmanIp2.Text);
            textBoxKarmanPort2.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanPort2.Text, out int port))
                {
                    SaveKarmanIntSetting("KarmanPort2", port);
                }
                else
                {
                    LogMessage("������������ ���� ��� KarmanPort2. ��������� ����� �����.");
                }
            };
            textBoxKarmanRegister2.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanRegister2.Text, out int register))
                {
                    SaveKarmanIntSetting("KarmanRegister2", register);
                }
                else
                {
                    LogMessage("������������ ���� ��� KarmanRegister2. ��������� ����� �����.");
                }
            };

            // ������ 3
            textBoxKarmanIp3.TextChanged += (s, e) => SaveKarmanStringSetting("KarmanIp3", textBoxKarmanIp3.Text);
            textBoxKarmanPort3.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanPort3.Text, out int port))
                {
                    SaveKarmanIntSetting("KarmanPort3", port);
                }
                else
                {
                    LogMessage("������������ ���� ��� KarmanPort3. ��������� ����� �����.");
                }
            };
            textBoxKarmanRegister3.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanRegister3.Text, out int register))
                {
                    SaveKarmanIntSetting("KarmanRegister3", register);
                }
                else
                {
                    LogMessage("������������ ���� ��� KarmanRegister3. ��������� ����� �����.");
                }
            };

            // ������ 4
            textBoxKarmanIp4.TextChanged += (s, e) => SaveKarmanStringSetting("KarmanIp4", textBoxKarmanIp4.Text);
            textBoxKarmanPort4.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanPort4.Text, out int port))
                {
                    SaveKarmanIntSetting("KarmanPort4", port);
                }
                else
                {
                    LogMessage("������������ ���� ��� KarmanPort4. ��������� ����� �����.");
                }
            };
            textBoxKarmanRegister4.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanRegister4.Text, out int register))
                {
                    SaveKarmanIntSetting("KarmanRegister4", register);
                }
                else
                {
                    LogMessage("������������ ���� ��� KarmanRegister4. ��������� ����� �����.");
                }
            };
        }

        private void SaveKarmanStringSetting(string key, string value)
        {
            Properties.Settings.Default[key] = value;
            Properties.Settings.Default.Save();
        }

        private void SaveKarmanIntSetting(string key, int value)
        {
            Properties.Settings.Default[key] = value;
            Properties.Settings.Default.Save();
        }

        private void InitCombobox() // �������������� ��������� � ComboBox ��������
        {
            string[] diameters = { "60", "73", "89" };
            string[] materials = { "CR", "��" };
            string[] groups = { "E", "L" };

            // ������ 1
            comboBoxK1Diameter.Items.AddRange(diameters);
            comboBoxK1Diameter.SelectedIndex = 0;

            comboBoxK1Material.Items.AddRange(materials);
            comboBoxK1Material.SelectedIndex = 0;

            comboBoxK1Group.Items.AddRange(groups);
            comboBoxK1Group.SelectedIndex = 0;

            // ������ 2
            comboBoxK2Diameter.Items.AddRange(diameters);
            comboBoxK2Diameter.SelectedIndex = 0;

            comboBoxK2Material.Items.AddRange(materials);
            comboBoxK2Material.SelectedIndex = 0;

            comboBoxK2Group.Items.AddRange(groups);
            comboBoxK2Group.SelectedIndex = 0;

            // ������ 3
            comboBoxK3Diameter.Items.AddRange(diameters);
            comboBoxK3Diameter.SelectedIndex = 0;

            comboBoxK3Material.Items.AddRange(materials);
            comboBoxK3Material.SelectedIndex = 0;

            comboBoxK3Group.Items.AddRange(groups);
            comboBoxK3Group.SelectedIndex = 0;

            // ������ 4
            comboBoxK4Diameter.Items.AddRange(diameters);
            comboBoxK4Diameter.SelectedIndex = 0;

            comboBoxK4Material.Items.AddRange(materials);
            comboBoxK4Material.SelectedIndex = 0;

            comboBoxK4Group.Items.AddRange(groups);
            comboBoxK4Group.SelectedIndex = 0;
        }

        private void InitLogs() // ������������� ��� ����� � ����������� ����
        {
            // ����
            listViewLog.Columns.Add("���������", -2);
            listViewLog.View = System.Windows.Forms.View.Details;

            // ����������� �����
            listViewRejected.Columns.Add("�����", 200);
            listViewRejected.Columns.Add("�������", 200);
            listViewRejected.View = System.Windows.Forms.View.Details;
            listViewLog.KeyDown += listViewLog_KeyDown;
        }

        private void InitLogsReceived()
        {
            _httpServerService = new HttpServerService();
            _httpServerService.LogMessageReceived += (sender, msg) =>
            {
                LogMessage(msg); // ����� ������ ������ LogMessage, ������� ��������� listViewLog � �����
            };

            _httpServerService.MarkingDataReceived += HttpServerService_MarkingDataReceived;
        }

        private void InitializeCounters()
        {
            // ��������� "����" ���� � ������ ��������
            _sectionCounters = new Dictionary<string, int>
            {
                {"�������", 0},
                {"��", 0},
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
            string[] sections = { "�������", "��", "�������", "����������", "����������", "�������", "����" };

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
                lbl.Font = new System.Drawing.Font(lbl.Font.FontFamily, 12.0f, FontStyle.Bold);
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                lbl.ContextMenuStrip = _contextMenuSection;

                // ���� ��� "����", ������ �����
                if (section == "����")
                {
                    lbl.BackColor = System.Drawing.Color.LightCoral;
                    lbl.ForeColor = System.Drawing.Color.White;
                }

                panelCounters.Controls.Add(lbl);
                _sectionLabels[section] = lbl;
            }

            AdjustLabelWidths();
        }

        #endregion

        #region Event Handlers

        private void MainForm_Resize(object sender, EventArgs e)
        {
            AdjustLabelWidths();
        }

        private void AdjustLabelWidths()
        {
            // ��������� ��������� ������ ������ ������, �������� �������
            int width = panelCounters.ClientSize.Width - panelCounters.Padding.Left - panelCounters.Padding.Right;

            // ������������� ������ ������ ����� � ������������ � ����������� �������
            foreach (var lbl in _sectionLabels.Values)
            {
                lbl.Width = width;
            }
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

        private void buttonCloseBatch1_Click(object sender, EventArgs e)
        {
            CloseBatch(1);
            SendGetRequestAsync().ConfigureAwait(false);
        }

        private void buttonCloseBatch2_Click(object sender, EventArgs e)
        {
            CloseBatch(2);
            SendGetRequestAsync().ConfigureAwait(false);
        }

        private void buttonCloseBatch3_Click(object sender, EventArgs e)
        {
            CloseBatch(3);
            SendGetRequestAsync().ConfigureAwait(false);
        }

        private void buttonCloseBatch4_Click(object sender, EventArgs e)
        {
            CloseBatch(4);
            SendGetRequestAsync().ConfigureAwait(false);
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
            _rejectedCountShablon = 0;
            _rejectedCountNK = 0;
            _rejectedCountPressed = 0;
            listViewRejected.Items.Clear();

            UpdateSectionLabels();
            UpdateGlobalStats();
            LogMessage("��������� ��������.");
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                LogMessage("���������� ��� ��������.");
                return;
            }

            try
            {
                // ��������� HTTP ������
                _httpServerService.StartServer(Properties.Settings.Default.ServerIP, Properties.Settings.Default.ServerPort);

                // ��������� �������� ����
                StartMainLoop();
                LogMessage("�������� ���� �������.");

                _isRunning = true;

                // ��������� ��������� ������
                button_start.Enabled = false;
                button_stop.Enabled = true;
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ������� ����������: {ex.Message}");
            }
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                LogMessage("���������� �� ��������.");
                return;
            }

            try
            {
                // ������������� �������� ����
                StopMainLoop();
                LogMessage("�������� ���� ����������.");

                // ������������� HTTP ������
                _httpServerService.StopServer();

                _isRunning = false;

                // ��������� ��������� ������
                button_start.Enabled = true;
                button_stop.Enabled = false;
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ��������� ����������: {ex.Message}");
            }
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            SaveSettings();
            SaveKarmanSettings();
            LogMessage("��������� ���������");
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            LoadSettings();
            LoadKarmanBatchSettings();
            InitializeModbusServices();
            LogMessage("�������� ���������");
        }


        private void listViewLog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedItemsToClipboard();
                e.SuppressKeyPress = true;
            }
        }

        #endregion

        #region Karman Methods

        private void SaveKarmanSettings()
        {
            try
            {
                // ���������� ������� � ��������� ����� �� ��������� ����� �����
                Properties.Settings.Default["Karman1BatchNumber"] = int.Parse(textBoxK1CurrentBatch.Text);
                Properties.Settings.Default["Karman1BatchCount"] = int.Parse(textBoxK1CurrentCount.Text);
                Properties.Settings.Default["Karman1BatchSize"] = int.Parse(textBoxK1BatchSize.Text);

                Properties.Settings.Default["Karman2BatchNumber"] = int.Parse(textBoxK2CurrentBatch.Text);
                Properties.Settings.Default["Karman2BatchCount"] = int.Parse(textBoxK2CurrentCount.Text);
                Properties.Settings.Default["Karman2BatchSize"] = int.Parse(textBoxK2BatchSize.Text);

                Properties.Settings.Default["Karman3BatchNumber"] = int.Parse(textBoxK3CurrentBatch.Text);
                Properties.Settings.Default["Karman3BatchCount"] = int.Parse(textBoxK3CurrentCount.Text);
                Properties.Settings.Default["Karman3BatchSize"] = int.Parse(textBoxK3BatchSize.Text);

                Properties.Settings.Default["Karman4BatchNumber"] = int.Parse(textBoxK4CurrentBatch.Text);
                Properties.Settings.Default["Karman4BatchCount"] = int.Parse(textBoxK4CurrentCount.Text);
                Properties.Settings.Default["Karman4BatchSize"] = int.Parse(textBoxK4BatchSize.Text);

                // ���������� �������� �������� �� ��������� ����� �����
                SaveKarmanStringSetting("KarmanIp1", textBoxKarmanIp1.Text);
                SaveKarmanIntSetting("KarmanPort1", int.Parse(textBoxKarmanPort1.Text));
                SaveKarmanIntSetting("KarmanRegister1", int.Parse(textBoxKarmanRegister1.Text));

                SaveKarmanStringSetting("KarmanIp2", textBoxKarmanIp2.Text);
                SaveKarmanIntSetting("KarmanPort2", int.Parse(textBoxKarmanPort2.Text));
                SaveKarmanIntSetting("KarmanRegister2", int.Parse(textBoxKarmanRegister2.Text));

                SaveKarmanStringSetting("KarmanIp3", textBoxKarmanIp3.Text);
                SaveKarmanIntSetting("KarmanPort3", int.Parse(textBoxKarmanPort3.Text));
                SaveKarmanIntSetting("KarmanRegister3", int.Parse(textBoxKarmanRegister3.Text));

                SaveKarmanStringSetting("KarmanIp4", textBoxKarmanIp4.Text);
                SaveKarmanIntSetting("KarmanPort4", int.Parse(textBoxKarmanPort4.Text));
                SaveKarmanIntSetting("KarmanRegister4", int.Parse(textBoxKarmanRegister4.Text));

                Properties.Settings.Default.Karman1_Diameter = comboBoxK1Diameter.SelectedItem?.ToString() ?? "60";
                Properties.Settings.Default.Karman1_Material = comboBoxK1Material.SelectedItem?.ToString() ?? "CR";
                Properties.Settings.Default.Karman1_Group = comboBoxK1Group.SelectedItem?.ToString() ?? "E";

                Properties.Settings.Default.Karman2_Diameter = comboBoxK2Diameter.SelectedItem?.ToString() ?? "60";
                Properties.Settings.Default.Karman2_Material = comboBoxK2Material.SelectedItem?.ToString() ?? "CR";
                Properties.Settings.Default.Karman2_Group = comboBoxK2Group.SelectedItem?.ToString() ?? "E";

                Properties.Settings.Default.Karman3_Diameter = comboBoxK3Diameter.SelectedItem?.ToString() ?? "60";
                Properties.Settings.Default.Karman3_Material = comboBoxK3Material.SelectedItem?.ToString() ?? "CR";
                Properties.Settings.Default.Karman3_Group = comboBoxK3Group.SelectedItem?.ToString() ?? "E";

                Properties.Settings.Default.Karman4_Diameter = comboBoxK4Diameter.SelectedItem?.ToString() ?? "60";
                Properties.Settings.Default.Karman4_Material = comboBoxK4Material.SelectedItem?.ToString() ?? "CR";
                Properties.Settings.Default.Karman4_Group = comboBoxK4Group.SelectedItem?.ToString() ?? "E";


                Properties.Settings.Default.ServerRejectAddres = textBoxServerReject.Text;


               Properties.Settings.Default.Save();
            }
            catch (FormatException ex)
            {
                LogMessage($"������ ������� ������ ��� ���������� ���������� ��������: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ���������� ���������� ��������: {ex.Message}");
            }
        }

        private void LoadKarmanBatchSettings()
        {
            try
            {
                // �������� ������� � ��������� ����� ��� �������� �����
                _karman1BatchNumber = int.Parse(Properties.Settings.Default["Karman1BatchNumber"].ToString());
                _karman1BatchCount = int.Parse(Properties.Settings.Default["Karman1BatchCount"].ToString());
                _karman1BatchSize = int.Parse(Properties.Settings.Default["Karman1BatchSize"].ToString());

                _karman2BatchNumber = int.Parse(Properties.Settings.Default["Karman2BatchNumber"].ToString());
                _karman2BatchCount = int.Parse(Properties.Settings.Default["Karman2BatchCount"].ToString());
                _karman2BatchSize = int.Parse(Properties.Settings.Default["Karman2BatchSize"].ToString());

                _karman3BatchNumber = int.Parse(Properties.Settings.Default["Karman3BatchNumber"].ToString());
                _karman3BatchCount = int.Parse(Properties.Settings.Default["Karman3BatchCount"].ToString());
                _karman3BatchSize = int.Parse(Properties.Settings.Default["Karman3BatchSize"].ToString());

                _karman4BatchNumber = int.Parse(Properties.Settings.Default["Karman4BatchNumber"].ToString());
                _karman4BatchCount = int.Parse(Properties.Settings.Default["Karman4BatchCount"].ToString());
                _karman4BatchSize = int.Parse(Properties.Settings.Default["Karman4BatchSize"].ToString());

                // �������� �������� �������� �� �������� � ��������� ���� �����
                textBoxKarmanIp1.Text = Properties.Settings.Default["KarmanIp1"].ToString();
                textBoxKarmanPort1.Text = Properties.Settings.Default["KarmanPort1"].ToString();
                textBoxKarmanRegister1.Text = Properties.Settings.Default["KarmanRegister1"].ToString();

                textBoxKarmanIp2.Text = Properties.Settings.Default["KarmanIp2"].ToString();
                textBoxKarmanPort2.Text = Properties.Settings.Default["KarmanPort2"].ToString();
                textBoxKarmanRegister2.Text = Properties.Settings.Default["KarmanRegister2"].ToString();

                textBoxKarmanIp3.Text = Properties.Settings.Default["KarmanIp3"].ToString();
                textBoxKarmanPort3.Text = Properties.Settings.Default["KarmanPort3"].ToString();
                textBoxKarmanRegister3.Text = Properties.Settings.Default["KarmanRegister3"].ToString();

                textBoxKarmanIp4.Text = Properties.Settings.Default["KarmanIp4"].ToString();
                textBoxKarmanPort4.Text = Properties.Settings.Default["KarmanPort4"].ToString();
                textBoxKarmanRegister4.Text = Properties.Settings.Default["KarmanRegister4"].ToString();

                SetComboBoxSelectedItem(comboBoxK1Diameter, Properties.Settings.Default.Karman1_Diameter);
                SetComboBoxSelectedItem(comboBoxK1Material, Properties.Settings.Default.Karman1_Material);
                SetComboBoxSelectedItem(comboBoxK1Group, Properties.Settings.Default.Karman1_Group);

                SetComboBoxSelectedItem(comboBoxK2Diameter, Properties.Settings.Default.Karman2_Diameter);
                SetComboBoxSelectedItem(comboBoxK2Material, Properties.Settings.Default.Karman2_Material);
                SetComboBoxSelectedItem(comboBoxK2Group, Properties.Settings.Default.Karman2_Group);

                SetComboBoxSelectedItem(comboBoxK3Diameter, Properties.Settings.Default.Karman3_Diameter);
                SetComboBoxSelectedItem(comboBoxK3Material, Properties.Settings.Default.Karman3_Material);
                SetComboBoxSelectedItem(comboBoxK3Group, Properties.Settings.Default.Karman3_Group);

                SetComboBoxSelectedItem(comboBoxK4Diameter, Properties.Settings.Default.Karman4_Diameter);
                SetComboBoxSelectedItem(comboBoxK4Material, Properties.Settings.Default.Karman4_Material);
                SetComboBoxSelectedItem(comboBoxK4Group, Properties.Settings.Default.Karman4_Group);

                textBoxServerReject.Text = Properties.Settings.Default.ServerRejectAddres;

                UpdateKarmanUI(); // ��������� ���������

            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� �������� ���������� ��������: {ex.Message}");
            }
        }

        private void SetComboBoxSelectedItem(ComboBox comboBox, string selectedItem)
        {
            if (comboBox.Items.Contains(selectedItem))
            {
                comboBox.SelectedItem = selectedItem;
            }
            else
            {
                // ���������� �������� �� ���������, ���� ����������� �������� ����������� � ������
                comboBox.SelectedIndex = 0;
                LogMessage($"����������� ������� '{selectedItem}' �� ������ � ComboBox '{comboBox.Name}'. ���������� ������� �� ���������.");
            }
        }

        private void UpdateKarmanUI()
        {
            try
            {
                // ������ 1
                textBoxK1CurrentBatch.Text = _karman1BatchNumber.ToString();
                textBoxK1CurrentCount.Text = _karman1BatchCount.ToString();
                textBoxK1BatchSize.Text = _karman1BatchSize.ToString();

                // ������ 2
                textBoxK2CurrentBatch.Text = _karman2BatchNumber.ToString();
                textBoxK2CurrentCount.Text = _karman2BatchCount.ToString();
                textBoxK2BatchSize.Text = _karman2BatchSize.ToString();

                // ������ 3
                textBoxK3CurrentBatch.Text = _karman3BatchNumber.ToString();
                textBoxK3CurrentCount.Text = _karman3BatchCount.ToString();
                textBoxK3BatchSize.Text = _karman3BatchSize.ToString();

                // ������ 4
                textBoxK4CurrentBatch.Text = _karman4BatchNumber.ToString();
                textBoxK4CurrentCount.Text = _karman4BatchCount.ToString();
                textBoxK4BatchSize.Text = _karman4BatchSize.ToString();
            }
            catch (FormatException ex)
            {
                LogMessage($"������ ������� ������ ��� ���������� UI ��������: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ���������� UI ��������: {ex.Message}");
            }
        }

        private void CloseBatch(int karmanNumber)
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    // ���������� �������� ��� ������� �����
                    GenerateDocumentForBatch(karmanNumber, GetKarmanBatchNumber(karmanNumber));

                    // �������� ������������ ����� ����� �� ���� ������
                    var dbMaxBatchNumber = dbContext.Pipes.Max(p => (int?)p.BatchNumber) ?? 0;

                    // �������� ������� ������ ����� ���� �������� �� �����
                    var currentKarmanBatchNumbers = new List<int>
                    {
                        _karman1BatchNumber,
                        _karman2BatchNumber,
                        _karman3BatchNumber,
                        _karman4BatchNumber
                    };

                    var karmanMaxBatchNumber = currentKarmanBatchNumbers.Max();

                    // ���������� ����� ���������� ����� �����
                    var newBatchNumber = Math.Max(dbMaxBatchNumber, karmanMaxBatchNumber) + 1;

                    // ����������� ����� ����� � ���������� ������� ��� ���������������� �������
                    switch (karmanNumber)
                    {
                        case 1:
                            _karman1BatchNumber = newBatchNumber;
                            _karman1BatchCount = 0;
                            break;
                        case 2:
                            _karman2BatchNumber = newBatchNumber;
                            _karman2BatchCount = 0;
                            break;
                        case 3:
                            _karman3BatchNumber = newBatchNumber;
                            _karman3BatchCount = 0;
                            break;
                        case 4:
                            _karman4BatchNumber = newBatchNumber;
                            _karman4BatchCount = 0;
                            break;
                        default:
                            LogMessage("�������� ����� ������� ��� �������� �����.");
                            return;
                    }

                    UpdateKarmanUI();             // ��������� ���������
                    SaveKarmanSettings();        // ��������� ���������

                }
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� �������� ����� {karmanNumber}: {ex.Message}");
            }
        }

        public void KarmanFunction()
        {
            using (var dbContext = new AppDbContext())
            {
                var pipe = dbContext.Pipes
                    .Where(p => p.BatchNumber == 0)
                    .OrderBy(p => p.Id)
                    .FirstOrDefault();

                if (pipe == null)
                {
                    LogMessage("��� ���� � ���� ������ ��� ������������� �� ��������.");
                    return;
                }

                // ��������� ��������� �������� �� ComboBox � TextBox �� �����
                string k1Diameter = comboBoxK1Diameter.SelectedItem?.ToString();
                string k1Material = comboBoxK1Material.SelectedItem?.ToString();
                string k1Group = comboBoxK1Group.SelectedItem?.ToString();
                int k1BatchSize = int.TryParse(textBoxK1BatchSize.Text, out int temp1) ? temp1 : 100;

                string k2Diameter = comboBoxK2Diameter.SelectedItem?.ToString();
                string k2Material = comboBoxK2Material.SelectedItem?.ToString();
                string k2Group = comboBoxK2Group.SelectedItem?.ToString();
                int k2BatchSize = int.TryParse(textBoxK2BatchSize.Text, out int temp2) ? temp2 : 100;

                string k3Diameter = comboBoxK3Diameter.SelectedItem?.ToString();
                string k3Material = comboBoxK3Material.SelectedItem?.ToString();
                string k3Group = comboBoxK3Group.SelectedItem?.ToString();
                int k3BatchSize = int.TryParse(textBoxK3BatchSize.Text, out int temp3) ? temp3 : 100;

                string k4Diameter = comboBoxK4Diameter.SelectedItem?.ToString();
                string k4Material = comboBoxK4Material.SelectedItem?.ToString();
                string k4Group = comboBoxK4Group.SelectedItem?.ToString();
                int k4BatchSize = int.TryParse(textBoxK4BatchSize.Text, out int temp4) ? temp4 : 100;


                bool assigned = false;

                if (pipe.Diameter == k1Diameter && pipe.Material == k1Material && pipe.Group == k1Group &&  !checkBox1.Checked)
                {
                    AssignPipeToKarman(dbContext, pipe, 1, k1BatchSize);
                    assigned = true;
                }
                else if (pipe.Diameter == k2Diameter && pipe.Material == k2Material && pipe.Group == k2Group && !checkBox2.Checked)
                {
                    AssignPipeToKarman(dbContext, pipe, 2, k2BatchSize);
                    assigned = true;
                }
                else if (pipe.Diameter == k3Diameter && pipe.Material == k3Material && pipe.Group == k3Group && !checkBox3.Checked)
                {
                    AssignPipeToKarman(dbContext, pipe, 3, k3BatchSize);
                    assigned = true;
                }
                else if (pipe.Diameter == k4Diameter && pipe.Material == k4Material && pipe.Group == k4Group && !checkBox4.Checked)
                {
                    AssignPipeToKarman(dbContext, pipe, 4, k4BatchSize);
                    assigned = true;
                }

                if (!assigned)
                {

                    LogMessage("�� ������� ����������� ����� �� ������ �������.");
                    SendGetRequestAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// ���������� GET-������ �� ������ ��� ��������� ������� ������������� �����.
        /// </summary>
        /// <param name="pipe">������ �����, ������� �� ���� ������������.</param>
        private async Task SendGetRequestAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // ��������� URL. ��� ������������� �������� ��������� �������.
                    string url =  $"http://{Properties.Settings.Default.ServerRejectAddres}/"; // �������� �� ������ URL, ���� ����������

                    
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        LogMessage("��� ���������� �������������� ������������!");
                    }
                    else
                    {
                        LogMessage($"������ ��� �������� �������������� ������������. ������ ���: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� �������� GET-�������: {ex.Message}");
            }
        }

        private void AssignPipeToKarman(AppDbContext dbContext, PipeData pipe, int karmanNumber, int batchSize)
        {
            // ��������� �������� ������� �� ��������������� ��������� ����� �����
            string ip = "";
            int port = 0;
            int register = 0;

            switch (karmanNumber)
            {
                case 1:
                    ip = textBoxKarmanIp1.Text;
                    if (!int.TryParse(textBoxKarmanPort1.Text, out port))
                    {
                        LogMessage("������������ ���� ��� Karman1.");
                        return;
                    }
                    if (!int.TryParse(textBoxKarmanRegister1.Text, out register))
                    {
                        LogMessage("������������ ������� ��� Karman1.");
                        return;
                    }
                    break;
                case 2:
                    ip = textBoxKarmanIp2.Text;
                    if (!int.TryParse(textBoxKarmanPort2.Text, out port))
                    {
                        LogMessage("������������ ���� ��� Karman2.");
                        return;
                    }
                    if (!int.TryParse(textBoxKarmanRegister2.Text, out register))
                    {
                        LogMessage("������������ ������� ��� Karman2.");
                        return;
                    }
                    break;
                case 3:
                    ip = textBoxKarmanIp3.Text;
                    if (!int.TryParse(textBoxKarmanPort3.Text, out port))
                    {
                        LogMessage("������������ ���� ��� Karman3.");
                        return;
                    }
                    if (!int.TryParse(textBoxKarmanRegister3.Text, out register))
                    {
                        LogMessage("������������ ������� ��� Karman3.");
                        return;
                    }
                    break;
                case 4:
                    ip = textBoxKarmanIp4.Text;
                    if (!int.TryParse(textBoxKarmanPort4.Text, out port))
                    {
                        LogMessage("������������ ���� ��� Karman4.");
                        return;
                    }
                    if (!int.TryParse(textBoxKarmanRegister4.Text, out register))
                    {
                        LogMessage("������������ ������� ��� Karman4.");
                        return;
                    }
                    break;
                default:
                    LogMessage("�������� ����� ������� ��� ���������� �����.");
                    return;
            }

            // ������������� Modbus-������� ��� ���������� �������
            SetKarmanModbusRegister(ip, port, register);

            // ����������� ������� ����� ������ � ����������� �������
            switch (karmanNumber)
            {
                case 1:
                    pipe.BatchNumber = _karman1BatchNumber;
                    _karman1BatchCount++;
                    break;
                case 2:
                    pipe.BatchNumber = _karman2BatchNumber;
                    _karman2BatchCount++;
                    break;
                case 3:
                    pipe.BatchNumber = _karman3BatchNumber;
                    _karman3BatchCount++;
                    break;
                case 4:
                    pipe.BatchNumber = _karman4BatchNumber;
                    _karman4BatchCount++;
                    break;
            }

            dbContext.SaveChanges();

            UpdateKarmanUI();             // ��������� ���������
            SaveKarmanSettings();        // ��������� ���������

            //LogMessage($"����� {pipe.PipeNumber} -> ������ {karmanNumber}, ������ {GetKarmanBatchNumber(karmanNumber)}, � ������ {GetKarmanBatchCount(karmanNumber)}/{batchSize}.");

            // ���� �������� ������� ������ - ��������� �
            if (GetKarmanBatchCount(karmanNumber) >= batchSize)
            {
                CloseBatch(karmanNumber);
                SendGetRequestAsync().ConfigureAwait(false);
            }
        }


        public static void ReplacePlaceholders(string templatePath, string outputPath, Dictionary<string, string> replacements)
        {
            try
            {
                // �������� ������ � ����� ����
                File.Copy(templatePath, outputPath, overwrite: true);

                // ��������� ������������� ��������
                using (DocX document = DocX.Load(outputPath))
                {
                    foreach (var placeholder in replacements)
                    {
                        // �������� ��� ��������� ��������� ����� �� ��������
                        document.ReplaceText(placeholder.Key, placeholder.Value, false, RegexOptions.IgnoreCase);
                    }

                    // ��������� ���������
                    document.Save();
                }

            }
            catch (Exception ex)
            {
                // ����������� ������ (�����������)
                // LogMessage($"������ ��� ������ �������������: {ex.Message}");
                throw; // ��� ����������� ������ ��������������� �������
            }
        }

        public void InsertTableAtPlaceholder(string documentPath, List<PipeData> pipes)
        {
            try
            {
                using (DocX document = DocX.Load(documentPath))
                {
                    // ����� �������� � ������������� "�������"
                    var tableParagraph = document.Paragraphs.FirstOrDefault(p => p.Text.Contains("�������"));

                    if (tableParagraph == null)
                    {
                        LogMessage("�� ������ ����������� '�������' � ���������.");
                        return;
                    }

                    // ������ ������� � ������ ����������� �������
                    int numberOfColumns = 7;
                    var table = document.AddTable(pipes.Count + 1, numberOfColumns);

                    // ��������� ������ ������� (�����������)
                    table.Design = TableDesign.LightShadingAccent1;

                    // ���������� ����������
                    table.Rows[0].Cells[0].Paragraphs[0].Append("� �/�");
                    table.Rows[0].Cells[1].Paragraphs[0].Append("�-���");
                    table.Rows[0].Cells[2].Paragraphs[0].Append("�������");
                    table.Rows[0].Cells[3].Paragraphs[0].Append("������ ��������� ���� 633-80");
                    table.Rows[0].Cells[4].Paragraphs[0].Append("����������� ������� ������, � ��");
                    table.Rows[0].Cells[5].Paragraphs[0].Append("����� � ��� �������");
                    table.Rows[0].Cells[6].Paragraphs[0].Append("�����, � ��");

                    // ���������� ������
                    for (int i = 0; i < pipes.Count; i++)
                    {
                        var pipe = pipes[i];
                        table.Rows[i + 1].Cells[0].Paragraphs[0].Append((i + 1).ToString());
                        table.Rows[i + 1].Cells[1].Paragraphs[0].Append(pipe.PipeNumber.ToString());
                        table.Rows[i + 1].Cells[2].Paragraphs[0].Append(pipe.Diameter);
                        table.Rows[i + 1].Cells[3].Paragraphs[0].Append(pipe.Group);
                        table.Rows[i + 1].Cells[4].Paragraphs[0].Append(pipe.Thickness.ToString(CultureInfo.InvariantCulture));
                        table.Rows[i + 1].Cells[5].Paragraphs[0].Append(DateTime.Now.ToString("MM.yyyy"));
                        var length = (double.Parse(pipe.PipeLength) * 10).ToString();
                        table.Rows[i + 1].Cells[6].Paragraphs[0].Append(length);
                    }


                    // �������� ������� ����� ���������
                    tableParagraph.InsertTableAfterSelf(table);
                    // ������� ����� "�������" �� ���������
                    tableParagraph.ReplaceText("�������", string.Empty, false, RegexOptions.None);

                    // ������� ��������, ���� �� ������ ����� �������� ������������
                    //if (string.IsNullOrWhiteSpace(tableParagraph.Text))
                    //{
                    //    tableParagraph.Remove(false);
                    //}

                    // ��������� ���������
                    document.Save();
                }

                // ����������� �������� ������� ������� (�����������)
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ������� �������: {ex.Message}");
            }
        }

        private void GenerateDocumentForBatch(int karmanNumber, int batchNumber)
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    // �������� ��� ����� � �������� ������� �����
                    var pipes = dbContext.Pipes
                        .Where(p => p.BatchNumber == batchNumber)
                        .OrderBy(p => p.PipeNumber)
                        .ToList();

                    if (pipes.Count == 0)
                    {
                        LogMessage($"��� ���� � ������� ����� {batchNumber}.");
                        return;
                    }

                    // ��������������, ��� ��� ����� � ����� ����� ���������� ��������
                    var firstPipe = pipes.First();

                    // ��������� ����� ����������
                    int totalCount = pipes.Count;
                    double totalLength = Math.Round(pipes.Sum(p => double.Parse(p.PipeLength) / 100), 2);
                    double totalTonnage = 0;
                    if (firstPipe.Diameter == "73")
                    {
                        totalTonnage = totalLength * 9.48 / 1000;
                    }
                    else
                    {
                        totalTonnage = totalLength * 13.62 / 1000;
                    }
                    totalTonnage = Math.Round(totalTonnage, 2);
                    double thickness = double.Parse(firstPipe.Thickness, CultureInfo.InvariantCulture); // ���������, ��� Thickness - string, ����� �������� ���
                    string steel;
                    if(firstPipe.Material == "CR")
                    {
                        steel = "CS (����)";
                    }
                    else
                    {
                        steel = "30�2";
                    }

                    string group = firstPipe.Group;
                    string date = DateTime.Now.ToString("dd.MM.yyyy");

                    // ����������� ������� �����
                    var replacements1 = new Dictionary<string, string>
                    {
                        { "�����", batchNumber.ToString() },
                        { "��������", steel },
                        { "������", group },
                        { "count", totalCount.ToString() },
                        { "�����", totalLength.ToString("F2", CultureInfo.InvariantCulture) },
                        { "������", totalTonnage.ToString("F2", CultureInfo.InvariantCulture) },
                        { "�������", thickness.ToString("F1", CultureInfo.InvariantCulture) },
                        { "����", date }
                    };

                    string diam = firstPipe.Diameter;
                    var min = pipes.Min(p => int.Parse(p.PipeNumber)).ToString();
                    var max = pipes.Max(p => int.Parse(p.PipeNumber)).ToString();
                    var replacements2 = new Dictionary<string, string>
                    {
                        { "�����", batchNumber.ToString() },
                        { "��������", steel },
                        { "������", group },
                        { "���", min },
                        { "����", max },
                        { "count", totalCount.ToString() },
                        { "�������", thickness.ToString("F1", CultureInfo.InvariantCulture) },
                        { "diam", diam },
                        { "date", date },
                        { "length", totalLength.ToString() }
                    };

                    string projectRoot = AppContext.BaseDirectory;

                    // �������� ���� � ������� � ��������� �����
                    string templatePath1 = Path.Combine(projectRoot, "templates", "template1.docx"); // ����� "templates" � ����� �������
                    string templatePath2 = Path.Combine(projectRoot, "templates", "template2.docx"); // ����� "templates" � ����� �������
                    string outputPath1 = Path.Combine(projectRoot, "output", $"{batchNumber}.docx"); // ����� "output" � ����� �������
                    string outputPath2 = Path.Combine(projectRoot, "output", $"{batchNumber}-�����.docx"); // ����� "output" � ����� �������

                    // �������� ������������
                    ReplacePlaceholders(templatePath1, outputPath1, replacements1);
                    InsertTableAtPlaceholder(outputPath1, pipes);

                    ReplacePlaceholders(templatePath2, outputPath2, replacements2);

                    LogMessage($"�������� ��� ����� {batchNumber} ������� ������: {Path.Combine(projectRoot, "templates")}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ��������� ��������� ��� ����� {batchNumber}: {ex.Message}");
            }
        }

        private int GetKarmanBatchNumber(int karmanNumber)
        {
            return karmanNumber switch
            {
                1 => _karman1BatchNumber,
                2 => _karman2BatchNumber,
                3 => _karman3BatchNumber,
                4 => _karman4BatchNumber,
                _ => throw new ArgumentException("�������� ����� �������.")
            };
        }

        private int GetKarmanBatchCount(int karmanNumber)
        {
            return karmanNumber switch
            {
                1 => _karman1BatchCount,
                2 => _karman2BatchCount,
                3 => _karman3BatchCount,
                4 => _karman4BatchCount,
                _ => throw new ArgumentException("�������� ����� �������.")
            };
        }

        #endregion

        #region Modbus Methods

        private void InitializeModbusServices()
        {
            try
            {
                _modbusServices.Clear();

                _modbusServices["��������"] = new ModbusService(
                    textBoxCreation_IP.Text,
                    int.TryParse(textBoxCreation_Port.Text, out int creationPort) ? creationPort : 502,
                    int.TryParse(textBoxCreation_Register.Text, out int creationRegister) ? creationRegister : 100
                );

                _modbusServices["�������_Good"] = new ModbusService(
                    textBoxSharoshkaGood_IP.Text,
                    int.TryParse(textBoxSharoshkaGood_Port.Text, out int sharoshkaGoodPort) ? sharoshkaGoodPort : 502,
                    int.TryParse(textBoxSharoshkaGood_Register.Text, out int sharoshkaGoodRegister) ? sharoshkaGoodRegister : 101
                );

                _modbusServices["�������_Reject"] = new ModbusService(
                    textBoxSharoshkaReject_IP.Text,
                    int.TryParse(textBoxSharoshkaReject_Port.Text, out int sharoshkaRejectPort) ? sharoshkaRejectPort : 502,
                    int.TryParse(textBoxSharoshkaReject_Register.Text, out int sharoshkaRejectRegister) ? sharoshkaRejectRegister : 102
                );

                _modbusServices["��_Good"] = new ModbusService(
                    textBox��Good_IP.Text,
                    int.TryParse(textBox��Good_Port.Text, out int nkGoodPort) ? nkGoodPort : 502,
                    int.TryParse(textBox��Good_Register.Text, out int nkGoodRegister) ? nkGoodRegister : 103
                );

                _modbusServices["��_Reject"] = new ModbusService(
                    textBox��Reject_IP.Text,
                    int.TryParse(textBox��Reject_Port.Text, out int nkRejectPort) ? nkRejectPort : 502,
                    int.TryParse(textBox��Reject_Register.Text, out int nkRejectRegister) ? nkRejectRegister : 104
                );

                _modbusServices["�������"] = new ModbusService(
                    textBoxOtvorot_IP.Text,
                    int.TryParse(textBoxOtvorot_Port.Text, out int otvorotPort) ? otvorotPort : 502,
                    int.TryParse(textBoxOtvorot_Register.Text, out int otvorotRegister) ? otvorotRegister : 105
                );

                _modbusServices["����������_Good"] = new ModbusService(
                    textBoxOpressovkaGood_IP.Text,
                    int.TryParse(textBoxOpressovkaGood_Port.Text, out int opressovkaGoodPort) ? opressovkaGoodPort : 502,
                    int.TryParse(textBoxOpressovkaGood_Register.Text, out int opressovkaGoodRegister) ? opressovkaGoodRegister : 106
                );

                _modbusServices["����������_Reject"] = new ModbusService(
                    textBoxOpressovkaReject_IP.Text,
                    int.TryParse(textBoxOpressovkaReject_Port.Text, out int opressovkaRejectPort) ? opressovkaRejectPort : 502,
                    int.TryParse(textBoxOpressovkaReject_Register.Text, out int opressovkaRejectRegister) ? opressovkaRejectRegister : 107
                );

                _modbusServices["�������"] = new ModbusService(
                    textBoxKarman_IP.Text,
                    int.TryParse(textBoxKarman_Port.Text, out int karmanyPort) ? karmanyPort : 502,
                    int.TryParse(textBoxKarman_Register.Text, out int karmanyRegister) ? karmanyRegister : 109
                );

            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ������������� Modbus-��������: {ex.Message}");
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
                LogMessage($"������ ������ Modbus: {ex.Message}");
                // �������������� ��������� ��� ����������� ������
            }
        }

        #endregion

        #region Main Loop Methods

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
                        await Task.Delay(GetTriggerDelay(), token);

                        // �������� ��������� Modbus-��������
                        foreach (var serviceName in _modbusServices.Keys)
                        {
                            if (_modbusServices[serviceName].CheckTrigger())
                            {
                                HandleModbusTrigger(serviceName);
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // ������� ������ ������
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"������ � �������� �����: {ex.Message}");
                    }
                }
                LogMessage("�������� ���� ����������.");
            }, token);
        }

        private int GetTriggerDelay()
        {
            return int.TryParse(textBoxTriggerDelay.Text, out int delay) ? delay : 1000;
        }

        private void HandleModbusTrigger(string serviceName)
        {
            switch (serviceName)
            {
                case "��������":
                    _sectionCounters["�������"]++;
                    break;

                case "�������_Good":
                    MovePipe("�������", "��");
                    break;

                case "�������_Reject":
                    RejectPipe("�������");
                    break;

                case "��_Good":
                    MovePipe("��", "�������");
                    break;

                case "��_Reject":
                    RejectPipe("��");
                    break;

                case "�������":
                    MovePipe("�������", "����������");
                    break;

                case "����������_Good":
                    MovePipe("����������", "����������");
                    break;

                case "����������_Reject":
                    RejectPipe("����������");
                    break;

                case "�������":
                    KarmanFunction();
                    break;

                case "����������":
                    // ��������� ���������� ����� ���� ����� ��� � ������ ������
                    break;

                default:
                    LogMessage($"����������� Modbus-������: {serviceName}");
                    break;
            }

            UpdateSectionLabels();
            UpdateGlobalStats();
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

        #endregion

        #region Pipe Handling Methods

        private void MovePipe(string fromSection, string toSection)
        {
            if (_sectionCounters[fromSection] > 0)
            {
                _sectionCounters[fromSection]--;
                _sectionCounters[toSection]++;
                
            }
            else
            {
                LogMessage($"��� ���� � '{fromSection}' ��� ����������� � '{toSection}'.");
            }
        }

        private void RejectPipe(string sectionName)
        {
            if (_sectionCounters.ContainsKey(sectionName) && _sectionCounters[sectionName] > 0)
            {
                _sectionCounters[sectionName]--;

                switch (sectionName)
                {
                    case "�������":
                        _rejectedCountShablon++;
                        break;
                    case "��":
                        _rejectedCountNK++;
                        break;
                    case "����������":
                        _rejectedCountPressed++;
                        break;
                    default:
                        // ���� ���� ������ ������, ����� �������� ��������� �����
                        break;
                }

                _rejectedCount++; // ����� ���������� �����

                var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
                item.SubItems.Add(sectionName);
                listViewRejected.Items.Add(item);

                
            }
            else
            {
                LogMessage($"��� ���� �� ������� '{sectionName}' ��� �����.");
            }
        }

        #endregion

        #region HTTP Server Handlers

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
                // ���� ����� � ������� "����������", ������������ ��� ������
                _sectionCounters["����������"]--;
                _sectionCounters["�������"]++;

                SaveMarkedPipeData(markingData); // ��������� ������ � �����
            }
            else
            {
                // ��� ���� � ������� "����������", ��������� ����� �������
                LogMessage("��� ���� �� ������� '����������' ��� ���������� ����������. ��������� ����� �������.");
                AddPipeToSectionProgrammatically("����������");

                // ������ ������������ ����������� �����
                _sectionCounters["����������"]--;
                _sectionCounters["�������"]++;

                SaveMarkedPipeData(markingData); // ��������� ������ � �����
            }

            // ��������� ������ � ������ ����������� ����
            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add("����������");
            listViewRejected.Items.Add(item);

            UpdateSectionLabels();
            UpdateGlobalStats();
        }

        private void AddPipeToSectionProgrammatically(string sectionName)
        {
            if (!_sectionCounters.ContainsKey(sectionName))
            {
                LogMessage($"������ '{sectionName}' �� ����������.");
                return;
            }

            _sectionCounters[sectionName]++;
            _manualAdditions[sectionName]++;

            // ��������� ��������� � ����������
            UpdateSectionLabels();
            UpdateGlobalStats();

            LogMessage($"����� ��������� ������� � ������ '{sectionName}'.");
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

            
        }

        #endregion

        #region State Management

        private void SaveStateFunk()
        {
            var state = new SaveState
            {
                SectionCounters = new Dictionary<string, int>(_sectionCounters),
                ManualAdditions = new Dictionary<string, int>(_manualAdditions),
                ManualRemovals = new Dictionary<string, int>(_manualRemovals),
                RejectedCount = _rejectedCount,
                RejectedCountShablon = _rejectedCountShablon,
                RejectedCountNK = _rejectedCountNK,
                RejectedCountPressed = _rejectedCountPressed,

                RejectedRecords = listViewRejected.Items.Cast<ListViewItem>()
                    .Select(it => new RejectedRecord { Time = it.Text, Section = it.SubItems[1].Text })
                    .ToList()
            };

            try
            {
                SaveKarmanSettings();
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_stateFilePath, json);
                
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ���������� ���������: {ex.Message}");
            }
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
                    _sectionCounters = new Dictionary<string, int>(state.SectionCounters);
                    _manualAdditions = new Dictionary<string, int>(state.ManualAdditions);
                    _manualRemovals = new Dictionary<string, int>(state.ManualRemovals);
                    _rejectedCount = state.RejectedCount;
                    _rejectedCountShablon = state.RejectedCountShablon;
                    _rejectedCountNK = state.RejectedCountNK;
                    _rejectedCountPressed = state.RejectedCountPressed;

                    listViewRejected.Items.Clear();
                    foreach (var rec in state.RejectedRecords)
                    {
                        var item = new ListViewItem(rec.Time);
                        item.SubItems.Add(rec.Section);
                        listViewRejected.Items.Add(item);
                    }

                }
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� �������� ���������: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                Properties.Settings.Default["TriggerDelay"] = int.TryParse(textBoxTriggerDelay.Text, out int delay) ? delay : 1000;

                Properties.Settings.Default["ServerIP"] = textBoxServerIP.Text;
                Properties.Settings.Default["ServerPort"] = int.TryParse(textBoxServerPort.Text, out int serverPort) ? serverPort : 8080;

                Properties.Settings.Default["��������_IP"] = textBoxCreation_IP.Text;
                Properties.Settings.Default["��������_Port"] = int.TryParse(textBoxCreation_Port.Text, out int creationPort) ? creationPort : 502;
                Properties.Settings.Default["��������_Register"] = int.TryParse(textBoxCreation_Register.Text, out int creationRegister) ? creationRegister : 100;

                Properties.Settings.Default["�������_Good_IP"] = textBoxSharoshkaGood_IP.Text;
                Properties.Settings.Default["�������_Good_Port"] = int.TryParse(textBoxSharoshkaGood_Port.Text, out int sharoshkaGoodPort) ? sharoshkaGoodPort : 502;
                Properties.Settings.Default["�������_Good_Register"] = int.TryParse(textBoxSharoshkaGood_Register.Text, out int sharoshkaGoodRegister) ? sharoshkaGoodRegister : 101;

                Properties.Settings.Default["�������_Reject_IP"] = textBoxSharoshkaReject_IP.Text;
                Properties.Settings.Default["�������_Reject_Port"] = int.TryParse(textBoxSharoshkaReject_Port.Text, out int sharoshkaRejectPort) ? sharoshkaRejectPort : 502;
                Properties.Settings.Default["�������_Reject_Register"] = int.TryParse(textBoxSharoshkaReject_Register.Text, out int sharoshkaRejectRegister) ? sharoshkaRejectRegister : 102;

                Properties.Settings.Default["��_Good_IP"] = textBox��Good_IP.Text;
                Properties.Settings.Default["��_Good_Port"] = int.TryParse(textBox��Good_Port.Text, out int nkGoodPort) ? nkGoodPort : 502;
                Properties.Settings.Default["��_Good_Register"] = int.TryParse(textBox��Good_Register.Text, out int nkGoodRegister) ? nkGoodRegister : 103;

                Properties.Settings.Default["��_Reject_IP"] = textBox��Reject_IP.Text;
                Properties.Settings.Default["��_Reject_Port"] = int.TryParse(textBox��Reject_Port.Text, out int nkRejectPort) ? nkRejectPort : 502;
                Properties.Settings.Default["��_Reject_Register"] = int.TryParse(textBox��Reject_Register.Text, out int nkRejectRegister) ? nkRejectRegister : 104;

                Properties.Settings.Default["�������_IP"] = textBoxOtvorot_IP.Text;
                Properties.Settings.Default["�������_Port"] = int.TryParse(textBoxOtvorot_Port.Text, out int otvorotPort) ? otvorotPort : 502;
                Properties.Settings.Default["�������_Register"] = int.TryParse(textBoxOtvorot_Register.Text, out int otvorotRegister) ? otvorotRegister : 105;

                Properties.Settings.Default["����������_Good_IP"] = textBoxOpressovkaGood_IP.Text;
                Properties.Settings.Default["����������_Good_Port"] = int.TryParse(textBoxOpressovkaGood_Port.Text, out int opressovkaGoodPort) ? opressovkaGoodPort : 502;
                Properties.Settings.Default["����������_Good_Register"] = int.TryParse(textBoxOpressovkaGood_Register.Text, out int opressovkaGoodRegister) ? opressovkaGoodRegister : 106;

                Properties.Settings.Default["����������_Reject_IP"] = textBoxOpressovkaReject_IP.Text;
                Properties.Settings.Default["����������_Reject_Port"] = int.TryParse(textBoxOpressovkaReject_Port.Text, out int opressovkaRejectPort) ? opressovkaRejectPort : 502;
                Properties.Settings.Default["����������_Reject_Register"] = int.TryParse(textBoxOpressovkaReject_Register.Text, out int opressovkaRejectRegister) ? opressovkaRejectRegister : 107;

                Properties.Settings.Default["�������_IP"] = textBoxKarman_IP.Text;
                Properties.Settings.Default["�������_Port"] = int.TryParse(textBoxKarman_Port.Text, out int karmanyPort) ? karmanyPort : 502;
                Properties.Settings.Default["�������_Register"] = int.TryParse(textBoxKarman_Register.Text, out int karmanyRegister) ? karmanyRegister : 109;

                // ��������� �������� 1-4 ����������� ������������� ����� ����������� �������

                Properties.Settings.Default.Save();
                
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� ���������� ��������: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                textBoxTriggerDelay.Text = Properties.Settings.Default["TriggerDelay"]?.ToString() ?? "1000";

                textBoxServerIP.Text = Properties.Settings.Default["ServerIP"]?.ToString() ?? "127.0.0.1";
                textBoxServerPort.Text = Properties.Settings.Default["ServerPort"]?.ToString() ?? "8080";

                textBoxCreation_IP.Text = Properties.Settings.Default["��������_IP"]?.ToString() ?? "127.0.0.1";
                textBoxCreation_Port.Text = Properties.Settings.Default["��������_Port"]?.ToString() ?? "502";
                textBoxCreation_Register.Text = Properties.Settings.Default["��������_Register"]?.ToString() ?? "100";

                textBoxSharoshkaGood_IP.Text = Properties.Settings.Default["�������_Good_IP"]?.ToString() ?? "127.0.0.1";
                textBoxSharoshkaGood_Port.Text = Properties.Settings.Default["�������_Good_Port"]?.ToString() ?? "502";
                textBoxSharoshkaGood_Register.Text = Properties.Settings.Default["�������_Good_Register"]?.ToString() ?? "101";

                textBoxSharoshkaReject_IP.Text = Properties.Settings.Default["�������_Reject_IP"]?.ToString() ?? "127.0.0.1";
                textBoxSharoshkaReject_Port.Text = Properties.Settings.Default["�������_Reject_Port"]?.ToString() ?? "502";
                textBoxSharoshkaReject_Register.Text = Properties.Settings.Default["�������_Reject_Register"]?.ToString() ?? "102";

                textBox��Good_IP.Text = Properties.Settings.Default["��_Good_IP"]?.ToString() ?? "127.0.0.1";
                textBox��Good_Port.Text = Properties.Settings.Default["��_Good_Port"]?.ToString() ?? "502";
                textBox��Good_Register.Text = Properties.Settings.Default["��_Good_Register"]?.ToString() ?? "103";

                textBox��Reject_IP.Text = Properties.Settings.Default["��_Reject_IP"]?.ToString() ?? "127.0.0.1";
                textBox��Reject_Port.Text = Properties.Settings.Default["��_Reject_Port"]?.ToString() ?? "502";
                textBox��Reject_Register.Text = Properties.Settings.Default["��_Reject_Register"]?.ToString() ?? "104";

                textBoxOtvorot_IP.Text = Properties.Settings.Default["�������_IP"]?.ToString() ?? "127.0.0.1";
                textBoxOtvorot_Port.Text = Properties.Settings.Default["�������_Port"]?.ToString() ?? "502";
                textBoxOtvorot_Register.Text = Properties.Settings.Default["�������_Register"]?.ToString() ?? "105";

                textBoxOpressovkaGood_IP.Text = Properties.Settings.Default["����������_Good_IP"]?.ToString() ?? "127.0.0.1";
                textBoxOpressovkaGood_Port.Text = Properties.Settings.Default["����������_Good_Port"]?.ToString() ?? "502";
                textBoxOpressovkaGood_Register.Text = Properties.Settings.Default["����������_Good_Register"]?.ToString() ?? "106";

                textBoxOpressovkaReject_IP.Text = Properties.Settings.Default["����������_Reject_IP"]?.ToString() ?? "127.0.0.1";
                textBoxOpressovkaReject_Port.Text = Properties.Settings.Default["����������_Reject_Port"]?.ToString() ?? "502";
                textBoxOpressovkaReject_Register.Text = Properties.Settings.Default["����������_Reject_Register"]?.ToString() ?? "107";

                textBoxKarman_IP.Text = Properties.Settings.Default["�������_IP"]?.ToString() ?? "127.0.0.1";
                textBoxKarman_Port.Text = Properties.Settings.Default["�������_Port"]?.ToString() ?? "502";
                textBoxKarman_Register.Text = Properties.Settings.Default["�������_Register"]?.ToString() ?? "109";

                LoadKarmanBatchSettings();

                
            }
            catch (Exception ex)
            {
                LogMessage($"������ ��� �������� ��������: {ex.Message}");
            }
        }

        #endregion

        #region State Update Methods

        private void UpdateSectionLabels()
        {
            foreach (var section in _sectionLabels.Keys)
            {
                int count = _sectionCounters[section];
                int addCount = _manualAdditions[section];
                int removeCount = _manualRemovals[section];

                _sectionLabels[section].Text = $"{section}: {count}         (������: {addCount - removeCount})";
            }
        }

        private void UpdateGlobalStats()
        {
            int totalAdd = _manualAdditions.Values.Sum();
            int totalRemove = _manualRemovals.Values.Sum();
            int totalInKarmany = _sectionCounters.ContainsKey("�������") ? _sectionCounters["�������"] : 0;
            int totalInBrak = _sectionCounters.ContainsKey("����") ? _sectionCounters["����"] : 0;

            labelGlobalStats.Height = 60;
            labelGlobalStats.Font = new System.Drawing.Font(labelGlobalStats.Font.FontFamily, 12.0f, FontStyle.Bold);

            // ���������� ������ � ������ ����� ���������� �����
            labelGlobalStats.Text =
                $"���������� ����������:\n" +
                $"������ ��������������: {totalAdd - totalRemove}\n" +
                $"������� ����: {totalInKarmany}\n" +
                $"������ ���������� � ����: {totalInBrak}\n" +
                $"���� �� �������: {_rejectedCountShablon}\n" +
                $"���� �� ��: {_rejectedCountNK}\n" +
                $"���� �� ����������: {_rejectedCountPressed}\n" +
                $"����� �����������: {_rejectedCount + totalInBrak}";
        }

        #endregion

        #region Logging Methods

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

        #endregion

        #region Context Menu Methods

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
            }
            else
            {
                int absDelta = Math.Abs(delta);
                _manualRemovals[section] += absDelta;
            }

            UpdateSectionLabels();
            UpdateGlobalStats();
        }

        #endregion

        #region Form Events

        private void Form1_Load(object sender, EventArgs e)
        {
            // �������������� ������������� ��� �������� �����, ���� ����������
        }

        #endregion

        #region Migration

        private void InitMigration()
        {
            using (var dbContext = new AppDbContext())
            {
                dbContext.Database.Migrate();
            }
        }

        #endregion

        #region SaveState Classes

        public class SaveState
        {
            public Dictionary<string, int> SectionCounters { get; set; }
            public Dictionary<string, int> ManualAdditions { get; set; }
            public Dictionary<string, int> ManualRemovals { get; set; }
            public int RejectedCount { get; set; }
            public int RejectedCountShablon { get; set; }
            public int RejectedCountNK { get; set; }
            public int RejectedCountPressed { get; set; }

            public List<RejectedRecord> RejectedRecords { get; set; } = new List<RejectedRecord>();
        }

        public class RejectedRecord
        {
            public string Time { get; set; }
            public string Section { get; set; }
        }

        #endregion

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_isRunning)
            {
                StopMainLoop();
                _httpServerService.StopServer();
                LogMessage("���������� ����������� ��� �������� �����.");
            }

            foreach (var modbusService in _modbusServices.Values)
                modbusService.Disconnect();

            SaveStateFunk(); // ��������� ��������� ��� ��������
        }
    }
}
