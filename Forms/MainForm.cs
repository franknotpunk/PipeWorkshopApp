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

        private Dictionary<string, int> _sectionCounters;    // Счётчики труб на участках
        private Dictionary<string, int> _manualAdditions;    // Ручные добавления по участкам
        private Dictionary<string, int> _manualRemovals;     // Ручные удаления по участкам

        private int _rejectedCount = 0; // Количество бракованных труб
        private int _rejectedCountShablon = 0; // Количество бракованных труб
        private int _rejectedCountNK = 0; // Количество бракованных труб
        private int _rejectedCountPressed = 0; // Количество бракованных труб


        private bool _isRunning = false;

        private Dictionary<string, Label> _sectionLabels; // Лейблы для участков
        private ContextMenuStrip _contextMenuSection;
        private string _currentRightClickSection;

        private Dictionary<string, ModbusService> _modbusServices = new Dictionary<string, ModbusService>();

        private string _stateFilePath = "state.json"; // Файл для сохранения состояния

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

            // Подключение обработчиков событий для изменения настроек карманов
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
            // Карман 1
            textBoxKarmanIp1.TextChanged += (s, e) => SaveKarmanStringSetting("KarmanIp1", textBoxKarmanIp1.Text);
            textBoxKarmanPort1.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanPort1.Text, out int port))
                {
                    SaveKarmanIntSetting("KarmanPort1", port);
                }
                else
                {
                    LogMessage("Некорректный ввод для KarmanPort1. Ожидается целое число.");
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
                    LogMessage("Некорректный ввод для KarmanRegister1. Ожидается целое число.");
                }
            };

            // Карман 2
            textBoxKarmanIp2.TextChanged += (s, e) => SaveKarmanStringSetting("KarmanIp2", textBoxKarmanIp2.Text);
            textBoxKarmanPort2.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanPort2.Text, out int port))
                {
                    SaveKarmanIntSetting("KarmanPort2", port);
                }
                else
                {
                    LogMessage("Некорректный ввод для KarmanPort2. Ожидается целое число.");
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
                    LogMessage("Некорректный ввод для KarmanRegister2. Ожидается целое число.");
                }
            };

            // Карман 3
            textBoxKarmanIp3.TextChanged += (s, e) => SaveKarmanStringSetting("KarmanIp3", textBoxKarmanIp3.Text);
            textBoxKarmanPort3.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanPort3.Text, out int port))
                {
                    SaveKarmanIntSetting("KarmanPort3", port);
                }
                else
                {
                    LogMessage("Некорректный ввод для KarmanPort3. Ожидается целое число.");
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
                    LogMessage("Некорректный ввод для KarmanRegister3. Ожидается целое число.");
                }
            };

            // Карман 4
            textBoxKarmanIp4.TextChanged += (s, e) => SaveKarmanStringSetting("KarmanIp4", textBoxKarmanIp4.Text);
            textBoxKarmanPort4.TextChanged += (s, e) =>
            {
                if (int.TryParse(textBoxKarmanPort4.Text, out int port))
                {
                    SaveKarmanIntSetting("KarmanPort4", port);
                }
                else
                {
                    LogMessage("Некорректный ввод для KarmanPort4. Ожидается целое число.");
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
                    LogMessage("Некорректный ввод для KarmanRegister4. Ожидается целое число.");
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

        private void InitCombobox() // Инициализируем настройки в ComboBox карманов
        {
            string[] diameters = { "60", "73", "89" };
            string[] materials = { "CR", "ГС" };
            string[] groups = { "E", "L" };

            // Карман 1
            comboBoxK1Diameter.Items.AddRange(diameters);
            comboBoxK1Diameter.SelectedIndex = 0;

            comboBoxK1Material.Items.AddRange(materials);
            comboBoxK1Material.SelectedIndex = 0;

            comboBoxK1Group.Items.AddRange(groups);
            comboBoxK1Group.SelectedIndex = 0;

            // Карман 2
            comboBoxK2Diameter.Items.AddRange(diameters);
            comboBoxK2Diameter.SelectedIndex = 0;

            comboBoxK2Material.Items.AddRange(materials);
            comboBoxK2Material.SelectedIndex = 0;

            comboBoxK2Group.Items.AddRange(groups);
            comboBoxK2Group.SelectedIndex = 0;

            // Карман 3
            comboBoxK3Diameter.Items.AddRange(diameters);
            comboBoxK3Diameter.SelectedIndex = 0;

            comboBoxK3Material.Items.AddRange(materials);
            comboBoxK3Material.SelectedIndex = 0;

            comboBoxK3Group.Items.AddRange(groups);
            comboBoxK3Group.SelectedIndex = 0;

            // Карман 4
            comboBoxK4Diameter.Items.AddRange(diameters);
            comboBoxK4Diameter.SelectedIndex = 0;

            comboBoxK4Material.Items.AddRange(materials);
            comboBoxK4Material.SelectedIndex = 0;

            comboBoxK4Group.Items.AddRange(groups);
            comboBoxK4Group.SelectedIndex = 0;
        }

        private void InitLogs() // Инициализация для логов и бракованных труб
        {
            // Логи
            listViewLog.Columns.Add("Сообщение", -2);
            listViewLog.View = System.Windows.Forms.View.Details;

            // Бракованные трубы
            listViewRejected.Columns.Add("Время", 200);
            listViewRejected.Columns.Add("Участок", 200);
            listViewRejected.View = System.Windows.Forms.View.Details;
            listViewLog.KeyDown += listViewLog_KeyDown;
        }

        private void InitLogsReceived()
        {
            _httpServerService = new HttpServerService();
            _httpServerService.LogMessageReceived += (sender, msg) =>
            {
                LogMessage(msg); // Вызов вашего метода LogMessage, который обновляет listViewLog в форме
            };

            _httpServerService.MarkingDataReceived += HttpServerService_MarkingDataReceived;
        }

        private void InitializeCounters()
        {
            // Добавляем "Брак" тоже в список участков
            _sectionCounters = new Dictionary<string, int>
            {
                {"Шарошка", 0},
                {"НК", 0},
                {"Отворот", 0},
                {"Опрессовка", 0},
                {"Маркировка", 0},
                {"Карманы", 0},
                {"Брак", 0}
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

            var addItem = new ToolStripMenuItem("Добавить трубу");
            addItem.Click += (s, e) => ChangePipeCountForSection(1);

            var removeItem = new ToolStripMenuItem("Удалить трубу");
            removeItem.Click += (s, e) => ChangePipeCountForSection(-1);

            _contextMenuSection.Items.Add(addItem);
            _contextMenuSection.Items.Add(removeItem);
        }

        private void CreateSectionLabels()
        {
            _sectionLabels = new Dictionary<string, Label>();
            string[] sections = { "Шарошка", "НК", "Отворот", "Опрессовка", "Маркировка", "Карманы", "Брак" };

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

                // Если это "Брак", меняем стиль
                if (section == "Брак")
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
            // Вычисляем доступную ширину внутри панели, учитывая отступы
            int width = panelCounters.ClientSize.Width - panelCounters.Padding.Left - panelCounters.Padding.Right;

            // Устанавливаем ширину каждой метки в соответствии с вычисленной шириной
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
            // Сброс состояния
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
            LogMessage("Состояние сброшено.");
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                LogMessage("Приложение уже запущено.");
                return;
            }

            try
            {
                // Запускаем HTTP сервер
                _httpServerService.StartServer(Properties.Settings.Default.ServerIP, Properties.Settings.Default.ServerPort);

                // Запускаем основной цикл
                StartMainLoop();
                LogMessage("Основной цикл запущен.");

                _isRunning = true;

                // Обновляем состояние кнопок
                button_start.Enabled = false;
                button_stop.Enabled = true;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при запуске приложения: {ex.Message}");
            }
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                LogMessage("Приложение не запущено.");
                return;
            }

            try
            {
                // Останавливаем основной цикл
                StopMainLoop();
                LogMessage("Основной цикл остановлен.");

                // Останавливаем HTTP сервер
                _httpServerService.StopServer();

                _isRunning = false;

                // Обновляем состояние кнопок
                button_start.Enabled = true;
                button_stop.Enabled = false;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при остановке приложения: {ex.Message}");
            }
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            SaveSettings();
            SaveKarmanSettings();
            LogMessage("Настройки сохранены");
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            LoadSettings();
            LoadKarmanBatchSettings();
            InitializeModbusServices();
            LogMessage("Загрузил настройки");
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
                // Сохранение номеров и счетчиков пачек из текстовых полей формы
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

                // Сохранение настроек карманов из текстовых полей формы
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
                LogMessage($"Ошибка формата данных при сохранении параметров карманов: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при сохранении параметров карманов: {ex.Message}");
            }
        }

        private void LoadKarmanBatchSettings()
        {
            try
            {
                // Загрузка номеров и счетчиков пачек без проверки типов
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

                // Загрузка настроек карманов из настроек в текстовые поля формы
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

                UpdateKarmanUI(); // Обновляем интерфейс

            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при загрузке параметров карманов: {ex.Message}");
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
                // Установите значение по умолчанию, если сохраненное значение отсутствует в списке
                comboBox.SelectedIndex = 0;
                LogMessage($"Сохраненный элемент '{selectedItem}' не найден в ComboBox '{comboBox.Name}'. Установлен элемент по умолчанию.");
            }
        }

        private void UpdateKarmanUI()
        {
            try
            {
                // Карман 1
                textBoxK1CurrentBatch.Text = _karman1BatchNumber.ToString();
                textBoxK1CurrentCount.Text = _karman1BatchCount.ToString();
                textBoxK1BatchSize.Text = _karman1BatchSize.ToString();

                // Карман 2
                textBoxK2CurrentBatch.Text = _karman2BatchNumber.ToString();
                textBoxK2CurrentCount.Text = _karman2BatchCount.ToString();
                textBoxK2BatchSize.Text = _karman2BatchSize.ToString();

                // Карман 3
                textBoxK3CurrentBatch.Text = _karman3BatchNumber.ToString();
                textBoxK3CurrentCount.Text = _karman3BatchCount.ToString();
                textBoxK3BatchSize.Text = _karman3BatchSize.ToString();

                // Карман 4
                textBoxK4CurrentBatch.Text = _karman4BatchNumber.ToString();
                textBoxK4CurrentCount.Text = _karman4BatchCount.ToString();
                textBoxK4BatchSize.Text = _karman4BatchSize.ToString();
            }
            catch (FormatException ex)
            {
                LogMessage($"Ошибка формата данных при обновлении UI карманов: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при обновлении UI карманов: {ex.Message}");
            }
        }

        private void CloseBatch(int karmanNumber)
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    // Генерируем документ для текущей пачки
                    GenerateDocumentForBatch(karmanNumber, GetKarmanBatchNumber(karmanNumber));

                    // Получаем максимальный номер пачки из базы данных
                    var dbMaxBatchNumber = dbContext.Pipes.Max(p => (int?)p.BatchNumber) ?? 0;

                    // Получаем текущие номера пачек всех карманов на форме
                    var currentKarmanBatchNumbers = new List<int>
                    {
                        _karman1BatchNumber,
                        _karman2BatchNumber,
                        _karman3BatchNumber,
                        _karman4BatchNumber
                    };

                    var karmanMaxBatchNumber = currentKarmanBatchNumbers.Max();

                    // Определяем новый уникальный номер пачки
                    var newBatchNumber = Math.Max(dbMaxBatchNumber, karmanMaxBatchNumber) + 1;

                    // Увеличиваем номер пачки и сбрасываем счетчик для соответствующего кармана
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
                            LogMessage("Неверный номер кармана при закрытии пачки.");
                            return;
                    }

                    UpdateKarmanUI();             // Обновляем интерфейс
                    SaveKarmanSettings();        // Сохраняем настройки

                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при закрытии пачки {karmanNumber}: {ex.Message}");
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
                    LogMessage("Нет труб в базе данных для распределения по карманам.");
                    return;
                }

                // Считываем настройки карманов из ComboBox и TextBox на форме
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

                    LogMessage("Не удалось сопоставить трубу ни одному карману.");
                    SendGetRequestAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Отправляет GET-запрос на сервер при неудачной попытке сопоставления трубы.
        /// </summary>
        /// <param name="pipe">Объект трубы, которая не была сопоставлена.</param>
        private async Task SendGetRequestAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Формируем URL. При необходимости добавьте параметры запроса.
                    string url =  $"http://{Properties.Settings.Default.ServerRejectAddres}/"; // Замените на нужный URL, если отличается

                    
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        LogMessage("Был отправлено предупреждение маркировщику!");
                    }
                    else
                    {
                        LogMessage($"Ошибка при отправке предупреждения маркировщику. Статус код: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при отправке GET-запроса: {ex.Message}");
            }
        }

        private void AssignPipeToKarman(AppDbContext dbContext, PipeData pipe, int karmanNumber, int batchSize)
        {
            // Получение настроек кармана из соответствующих текстовых полей формы
            string ip = "";
            int port = 0;
            int register = 0;

            switch (karmanNumber)
            {
                case 1:
                    ip = textBoxKarmanIp1.Text;
                    if (!int.TryParse(textBoxKarmanPort1.Text, out port))
                    {
                        LogMessage("Некорректный порт для Karman1.");
                        return;
                    }
                    if (!int.TryParse(textBoxKarmanRegister1.Text, out register))
                    {
                        LogMessage("Некорректный регистр для Karman1.");
                        return;
                    }
                    break;
                case 2:
                    ip = textBoxKarmanIp2.Text;
                    if (!int.TryParse(textBoxKarmanPort2.Text, out port))
                    {
                        LogMessage("Некорректный порт для Karman2.");
                        return;
                    }
                    if (!int.TryParse(textBoxKarmanRegister2.Text, out register))
                    {
                        LogMessage("Некорректный регистр для Karman2.");
                        return;
                    }
                    break;
                case 3:
                    ip = textBoxKarmanIp3.Text;
                    if (!int.TryParse(textBoxKarmanPort3.Text, out port))
                    {
                        LogMessage("Некорректный порт для Karman3.");
                        return;
                    }
                    if (!int.TryParse(textBoxKarmanRegister3.Text, out register))
                    {
                        LogMessage("Некорректный регистр для Karman3.");
                        return;
                    }
                    break;
                case 4:
                    ip = textBoxKarmanIp4.Text;
                    if (!int.TryParse(textBoxKarmanPort4.Text, out port))
                    {
                        LogMessage("Некорректный порт для Karman4.");
                        return;
                    }
                    if (!int.TryParse(textBoxKarmanRegister4.Text, out register))
                    {
                        LogMessage("Некорректный регистр для Karman4.");
                        return;
                    }
                    break;
                default:
                    LogMessage("Неверный номер кармана при назначении трубы.");
                    return;
            }

            // Устанавливаем Modbus-регистр для выбранного кармана
            SetKarmanModbusRegister(ip, port, register);

            // Присваиваем текущий номер партии и увеличиваем счетчик
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

            UpdateKarmanUI();             // Обновляем интерфейс
            SaveKarmanSettings();        // Сохраняем настройки

            //LogMessage($"Труба {pipe.PipeNumber} -> Карман {karmanNumber}, Партия {GetKarmanBatchNumber(karmanNumber)}, в партии {GetKarmanBatchCount(karmanNumber)}/{batchSize}.");

            // Если достигли размера партии - закрываем её
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
                // Копируем шаблон в новый файл
                File.Copy(templatePath, outputPath, overwrite: true);

                // Открываем скопированный документ
                using (DocX document = DocX.Load(outputPath))
                {
                    foreach (var placeholder in replacements)
                    {
                        // Заменяем все вхождения ключевого слова на значение
                        document.ReplaceText(placeholder.Key, placeholder.Value, false, RegexOptions.IgnoreCase);
                    }

                    // Сохраняем изменения
                    document.Save();
                }

            }
            catch (Exception ex)
            {
                // Логирование ошибки (опционально)
                // LogMessage($"Ошибка при замене плейсхолдеров: {ex.Message}");
                throw; // Или обработайте ошибку соответствующим образом
            }
        }

        public void InsertTableAtPlaceholder(string documentPath, List<PipeData> pipes)
        {
            try
            {
                using (DocX document = DocX.Load(documentPath))
                {
                    // Найти параграф с плейсхолдером "таблица"
                    var tableParagraph = document.Paragraphs.FirstOrDefault(p => p.Text.Contains("таблица"));

                    if (tableParagraph == null)
                    {
                        LogMessage("Не найден плейсхолдер 'таблица' в документе.");
                        return;
                    }

                    // Создаём таблицу с нужным количеством колонок
                    int numberOfColumns = 7;
                    var table = document.AddTable(pipes.Count + 1, numberOfColumns);

                    // Настройка стилей таблицы (опционально)
                    table.Design = TableDesign.LightShadingAccent1;

                    // Заполнение заголовков
                    table.Rows[0].Cells[0].Paragraphs[0].Append("№ п/п");
                    table.Rows[0].Cells[1].Paragraphs[0].Append("№-НКТ");
                    table.Rows[0].Cells[2].Paragraphs[0].Append("Диаметр");
                    table.Rows[0].Cells[3].Paragraphs[0].Append("Группа прочности ГОСТ 633-80");
                    table.Rows[0].Cells[4].Paragraphs[0].Append("Минимальная толщина стенки, в мм");
                    table.Rows[0].Cells[5].Paragraphs[0].Append("Месяц и год выпуска");
                    table.Rows[0].Cells[6].Paragraphs[0].Append("Длина, в мм");

                    // Заполнение данных
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


                    // Вставить таблицу после параграфа
                    tableParagraph.InsertTableAfterSelf(table);
                    // Удалить слово "таблица" из параграфа
                    tableParagraph.ReplaceText("таблица", string.Empty, false, RegexOptions.None);

                    // Удалить параграф, если он пустой после удаления плейсхолдера
                    //if (string.IsNullOrWhiteSpace(tableParagraph.Text))
                    //{
                    //    tableParagraph.Remove(false);
                    //}

                    // Сохранить изменения
                    document.Save();
                }

                // Логирование успешной вставки таблицы (опционально)
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при вставке таблицы: {ex.Message}");
            }
        }

        private void GenerateDocumentForBatch(int karmanNumber, int batchNumber)
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    // Получить все трубы с заданным номером пачки
                    var pipes = dbContext.Pipes
                        .Where(p => p.BatchNumber == batchNumber)
                        .OrderBy(p => p.PipeNumber)
                        .ToList();

                    if (pipes.Count == 0)
                    {
                        LogMessage($"Нет труб с номером пачки {batchNumber}.");
                        return;
                    }

                    // Предполагается, что все трубы в пачке имеют одинаковые свойства
                    var firstPipe = pipes.First();

                    // Вычислить общие показатели
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
                    double thickness = double.Parse(firstPipe.Thickness, CultureInfo.InvariantCulture); // Убедитесь, что Thickness - string, иначе измените тип
                    string steel;
                    if(firstPipe.Material == "CR")
                    {
                        steel = "CS (хром)";
                    }
                    else
                    {
                        steel = "30Г2";
                    }

                    string group = firstPipe.Group;
                    string date = DateTime.Now.ToString("dd.MM.yyyy");

                    // Подготовить словарь замен
                    var replacements1 = new Dictionary<string, string>
                    {
                        { "пачка", batchNumber.ToString() },
                        { "материал", steel },
                        { "группа", group },
                        { "count", totalCount.ToString() },
                        { "метры", totalLength.ToString("F2", CultureInfo.InvariantCulture) },
                        { "тоннаж", totalTonnage.ToString("F2", CultureInfo.InvariantCulture) },
                        { "толщина", thickness.ToString("F1", CultureInfo.InvariantCulture) },
                        { "дата", date }
                    };

                    string diam = firstPipe.Diameter;
                    var min = pipes.Min(p => int.Parse(p.PipeNumber)).ToString();
                    var max = pipes.Max(p => int.Parse(p.PipeNumber)).ToString();
                    var replacements2 = new Dictionary<string, string>
                    {
                        { "пачка", batchNumber.ToString() },
                        { "материал", steel },
                        { "группа", group },
                        { "мин", min },
                        { "макс", max },
                        { "count", totalCount.ToString() },
                        { "толщина", thickness.ToString("F1", CultureInfo.InvariantCulture) },
                        { "diam", diam },
                        { "date", date },
                        { "length", totalLength.ToString() }
                    };

                    string projectRoot = AppContext.BaseDirectory;

                    // Указание пути к шаблону и выходному файлу
                    string templatePath1 = Path.Combine(projectRoot, "templates", "template1.docx"); // Папка "templates" в корне проекта
                    string templatePath2 = Path.Combine(projectRoot, "templates", "template2.docx"); // Папка "templates" в корне проекта
                    string outputPath1 = Path.Combine(projectRoot, "output", $"{batchNumber}.docx"); // Папка "output" в корне проекта
                    string outputPath2 = Path.Combine(projectRoot, "output", $"{batchNumber}-бирка.docx"); // Папка "output" в корне проекта

                    // Заменить плейсхолдеры
                    ReplacePlaceholders(templatePath1, outputPath1, replacements1);
                    InsertTableAtPlaceholder(outputPath1, pipes);

                    ReplacePlaceholders(templatePath2, outputPath2, replacements2);

                    LogMessage($"Документ для пачки {batchNumber} успешно создан: {Path.Combine(projectRoot, "templates")}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при генерации документа для пачки {batchNumber}: {ex.Message}");
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
                _ => throw new ArgumentException("Неверный номер кармана.")
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
                _ => throw new ArgumentException("Неверный номер кармана.")
            };
        }

        #endregion

        #region Modbus Methods

        private void InitializeModbusServices()
        {
            try
            {
                _modbusServices.Clear();

                _modbusServices["Создание"] = new ModbusService(
                    textBoxCreation_IP.Text,
                    int.TryParse(textBoxCreation_Port.Text, out int creationPort) ? creationPort : 502,
                    int.TryParse(textBoxCreation_Register.Text, out int creationRegister) ? creationRegister : 100
                );

                _modbusServices["Шарошка_Good"] = new ModbusService(
                    textBoxSharoshkaGood_IP.Text,
                    int.TryParse(textBoxSharoshkaGood_Port.Text, out int sharoshkaGoodPort) ? sharoshkaGoodPort : 502,
                    int.TryParse(textBoxSharoshkaGood_Register.Text, out int sharoshkaGoodRegister) ? sharoshkaGoodRegister : 101
                );

                _modbusServices["Шарошка_Reject"] = new ModbusService(
                    textBoxSharoshkaReject_IP.Text,
                    int.TryParse(textBoxSharoshkaReject_Port.Text, out int sharoshkaRejectPort) ? sharoshkaRejectPort : 502,
                    int.TryParse(textBoxSharoshkaReject_Register.Text, out int sharoshkaRejectRegister) ? sharoshkaRejectRegister : 102
                );

                _modbusServices["НК_Good"] = new ModbusService(
                    textBoxНКGood_IP.Text,
                    int.TryParse(textBoxНКGood_Port.Text, out int nkGoodPort) ? nkGoodPort : 502,
                    int.TryParse(textBoxНКGood_Register.Text, out int nkGoodRegister) ? nkGoodRegister : 103
                );

                _modbusServices["НК_Reject"] = new ModbusService(
                    textBoxНКReject_IP.Text,
                    int.TryParse(textBoxНКReject_Port.Text, out int nkRejectPort) ? nkRejectPort : 502,
                    int.TryParse(textBoxНКReject_Register.Text, out int nkRejectRegister) ? nkRejectRegister : 104
                );

                _modbusServices["Отворот"] = new ModbusService(
                    textBoxOtvorot_IP.Text,
                    int.TryParse(textBoxOtvorot_Port.Text, out int otvorotPort) ? otvorotPort : 502,
                    int.TryParse(textBoxOtvorot_Register.Text, out int otvorotRegister) ? otvorotRegister : 105
                );

                _modbusServices["Опрессовка_Good"] = new ModbusService(
                    textBoxOpressovkaGood_IP.Text,
                    int.TryParse(textBoxOpressovkaGood_Port.Text, out int opressovkaGoodPort) ? opressovkaGoodPort : 502,
                    int.TryParse(textBoxOpressovkaGood_Register.Text, out int opressovkaGoodRegister) ? opressovkaGoodRegister : 106
                );

                _modbusServices["Опрессовка_Reject"] = new ModbusService(
                    textBoxOpressovkaReject_IP.Text,
                    int.TryParse(textBoxOpressovkaReject_Port.Text, out int opressovkaRejectPort) ? opressovkaRejectPort : 502,
                    int.TryParse(textBoxOpressovkaReject_Register.Text, out int opressovkaRejectRegister) ? opressovkaRejectRegister : 107
                );

                _modbusServices["Карманы"] = new ModbusService(
                    textBoxKarman_IP.Text,
                    int.TryParse(textBoxKarman_Port.Text, out int karmanyPort) ? karmanyPort : 502,
                    int.TryParse(textBoxKarman_Register.Text, out int karmanyRegister) ? karmanyRegister : 109
                );

            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при инициализации Modbus-сервисов: {ex.Message}");
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
                LogMessage($"Ошибка записи Modbus: {ex.Message}");
                // Дополнительная обработка или логирование ошибок
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

                        // Проверка триггеров Modbus-сервисов
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
                        // Ожидаем отмены задачи
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Ошибка в основном цикле: {ex.Message}");
                    }
                }
                LogMessage("Основной цикл остановлен.");
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
                case "Создание":
                    _sectionCounters["Шарошка"]++;
                    break;

                case "Шарошка_Good":
                    MovePipe("Шарошка", "НК");
                    break;

                case "Шарошка_Reject":
                    RejectPipe("Шарошка");
                    break;

                case "НК_Good":
                    MovePipe("НК", "Отворот");
                    break;

                case "НК_Reject":
                    RejectPipe("НК");
                    break;

                case "Отворот":
                    MovePipe("Отворот", "Опрессовка");
                    break;

                case "Опрессовка_Good":
                    MovePipe("Опрессовка", "Маркировка");
                    break;

                case "Опрессовка_Reject":
                    RejectPipe("Опрессовка");
                    break;

                case "Карманы":
                    KarmanFunction();
                    break;

                case "Маркировка":
                    // Обработка маркировки может быть здесь или в другом методе
                    break;

                default:
                    LogMessage($"Неизвестный Modbus-сервис: {serviceName}");
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
                LogMessage($"Нет труб в '{fromSection}' для перемещения в '{toSection}'.");
            }
        }

        private void RejectPipe(string sectionName)
        {
            if (_sectionCounters.ContainsKey(sectionName) && _sectionCounters[sectionName] > 0)
            {
                _sectionCounters[sectionName]--;

                switch (sectionName)
                {
                    case "Шарошка":
                        _rejectedCountShablon++;
                        break;
                    case "НК":
                        _rejectedCountNK++;
                        break;
                    case "Опрессовка":
                        _rejectedCountPressed++;
                        break;
                    default:
                        // Если есть другие секции, можно добавить обработку здесь
                        break;
                }

                _rejectedCount++; // Общее количество брака

                var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
                item.SubItems.Add(sectionName);
                listViewRejected.Items.Add(item);

                
            }
            else
            {
                LogMessage($"Нет труб на участке '{sectionName}' для брака.");
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
            if (_sectionCounters["Маркировка"] > 0)
            {
                // Есть трубы в разделе "Маркировка", обрабатываем как обычно
                _sectionCounters["Маркировка"]--;
                _sectionCounters["Карманы"]++;

                SaveMarkedPipeData(markingData); // Сохраняем данные о трубе
            }
            else
            {
                // Нет труб в разделе "Маркировка", добавляем трубу вручную
                LogMessage("Нет труб на участке 'Маркировка' для завершения маркировки. Добавляем трубу вручную.");
                AddPipeToSectionProgrammatically("Маркировка");

                // Теперь обрабатываем добавленную трубу
                _sectionCounters["Маркировка"]--;
                _sectionCounters["Карманы"]++;

                SaveMarkedPipeData(markingData); // Сохраняем данные о трубе
            }

            // Добавляем запись в список бракованных труб
            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add("Маркировка");
            listViewRejected.Items.Add(item);

            UpdateSectionLabels();
            UpdateGlobalStats();
        }

        private void AddPipeToSectionProgrammatically(string sectionName)
        {
            if (!_sectionCounters.ContainsKey(sectionName))
            {
                LogMessage($"Раздел '{sectionName}' не существует.");
                return;
            }

            _sectionCounters[sectionName]++;
            _manualAdditions[sectionName]++;

            // Обновляем интерфейс и статистику
            UpdateSectionLabels();
            UpdateGlobalStats();

            LogMessage($"Труба добавлена вручную в раздел '{sectionName}'.");
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
                LogMessage($"Ошибка при сохранении состояния: {ex.Message}");
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
                LogMessage($"Ошибка при загрузке состояния: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                Properties.Settings.Default["TriggerDelay"] = int.TryParse(textBoxTriggerDelay.Text, out int delay) ? delay : 1000;

                Properties.Settings.Default["ServerIP"] = textBoxServerIP.Text;
                Properties.Settings.Default["ServerPort"] = int.TryParse(textBoxServerPort.Text, out int serverPort) ? serverPort : 8080;

                Properties.Settings.Default["Создание_IP"] = textBoxCreation_IP.Text;
                Properties.Settings.Default["Создание_Port"] = int.TryParse(textBoxCreation_Port.Text, out int creationPort) ? creationPort : 502;
                Properties.Settings.Default["Создание_Register"] = int.TryParse(textBoxCreation_Register.Text, out int creationRegister) ? creationRegister : 100;

                Properties.Settings.Default["Шарошка_Good_IP"] = textBoxSharoshkaGood_IP.Text;
                Properties.Settings.Default["Шарошка_Good_Port"] = int.TryParse(textBoxSharoshkaGood_Port.Text, out int sharoshkaGoodPort) ? sharoshkaGoodPort : 502;
                Properties.Settings.Default["Шарошка_Good_Register"] = int.TryParse(textBoxSharoshkaGood_Register.Text, out int sharoshkaGoodRegister) ? sharoshkaGoodRegister : 101;

                Properties.Settings.Default["Шарошка_Reject_IP"] = textBoxSharoshkaReject_IP.Text;
                Properties.Settings.Default["Шарошка_Reject_Port"] = int.TryParse(textBoxSharoshkaReject_Port.Text, out int sharoshkaRejectPort) ? sharoshkaRejectPort : 502;
                Properties.Settings.Default["Шарошка_Reject_Register"] = int.TryParse(textBoxSharoshkaReject_Register.Text, out int sharoshkaRejectRegister) ? sharoshkaRejectRegister : 102;

                Properties.Settings.Default["НК_Good_IP"] = textBoxНКGood_IP.Text;
                Properties.Settings.Default["НК_Good_Port"] = int.TryParse(textBoxНКGood_Port.Text, out int nkGoodPort) ? nkGoodPort : 502;
                Properties.Settings.Default["НК_Good_Register"] = int.TryParse(textBoxНКGood_Register.Text, out int nkGoodRegister) ? nkGoodRegister : 103;

                Properties.Settings.Default["НК_Reject_IP"] = textBoxНКReject_IP.Text;
                Properties.Settings.Default["НК_Reject_Port"] = int.TryParse(textBoxНКReject_Port.Text, out int nkRejectPort) ? nkRejectPort : 502;
                Properties.Settings.Default["НК_Reject_Register"] = int.TryParse(textBoxНКReject_Register.Text, out int nkRejectRegister) ? nkRejectRegister : 104;

                Properties.Settings.Default["Отворот_IP"] = textBoxOtvorot_IP.Text;
                Properties.Settings.Default["Отворот_Port"] = int.TryParse(textBoxOtvorot_Port.Text, out int otvorotPort) ? otvorotPort : 502;
                Properties.Settings.Default["Отворот_Register"] = int.TryParse(textBoxOtvorot_Register.Text, out int otvorotRegister) ? otvorotRegister : 105;

                Properties.Settings.Default["Опрессовка_Good_IP"] = textBoxOpressovkaGood_IP.Text;
                Properties.Settings.Default["Опрессовка_Good_Port"] = int.TryParse(textBoxOpressovkaGood_Port.Text, out int opressovkaGoodPort) ? opressovkaGoodPort : 502;
                Properties.Settings.Default["Опрессовка_Good_Register"] = int.TryParse(textBoxOpressovkaGood_Register.Text, out int opressovkaGoodRegister) ? opressovkaGoodRegister : 106;

                Properties.Settings.Default["Опрессовка_Reject_IP"] = textBoxOpressovkaReject_IP.Text;
                Properties.Settings.Default["Опрессовка_Reject_Port"] = int.TryParse(textBoxOpressovkaReject_Port.Text, out int opressovkaRejectPort) ? opressovkaRejectPort : 502;
                Properties.Settings.Default["Опрессовка_Reject_Register"] = int.TryParse(textBoxOpressovkaReject_Register.Text, out int opressovkaRejectRegister) ? opressovkaRejectRegister : 107;

                Properties.Settings.Default["Карманы_IP"] = textBoxKarman_IP.Text;
                Properties.Settings.Default["Карманы_Port"] = int.TryParse(textBoxKarman_Port.Text, out int karmanyPort) ? karmanyPort : 502;
                Properties.Settings.Default["Карманы_Register"] = int.TryParse(textBoxKarman_Register.Text, out int karmanyRegister) ? karmanyRegister : 109;

                // Настройки карманов 1-4 сохраняются автоматически через обработчики событий

                Properties.Settings.Default.Save();
                
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при сохранении настроек: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                textBoxTriggerDelay.Text = Properties.Settings.Default["TriggerDelay"]?.ToString() ?? "1000";

                textBoxServerIP.Text = Properties.Settings.Default["ServerIP"]?.ToString() ?? "127.0.0.1";
                textBoxServerPort.Text = Properties.Settings.Default["ServerPort"]?.ToString() ?? "8080";

                textBoxCreation_IP.Text = Properties.Settings.Default["Создание_IP"]?.ToString() ?? "127.0.0.1";
                textBoxCreation_Port.Text = Properties.Settings.Default["Создание_Port"]?.ToString() ?? "502";
                textBoxCreation_Register.Text = Properties.Settings.Default["Создание_Register"]?.ToString() ?? "100";

                textBoxSharoshkaGood_IP.Text = Properties.Settings.Default["Шарошка_Good_IP"]?.ToString() ?? "127.0.0.1";
                textBoxSharoshkaGood_Port.Text = Properties.Settings.Default["Шарошка_Good_Port"]?.ToString() ?? "502";
                textBoxSharoshkaGood_Register.Text = Properties.Settings.Default["Шарошка_Good_Register"]?.ToString() ?? "101";

                textBoxSharoshkaReject_IP.Text = Properties.Settings.Default["Шарошка_Reject_IP"]?.ToString() ?? "127.0.0.1";
                textBoxSharoshkaReject_Port.Text = Properties.Settings.Default["Шарошка_Reject_Port"]?.ToString() ?? "502";
                textBoxSharoshkaReject_Register.Text = Properties.Settings.Default["Шарошка_Reject_Register"]?.ToString() ?? "102";

                textBoxНКGood_IP.Text = Properties.Settings.Default["НК_Good_IP"]?.ToString() ?? "127.0.0.1";
                textBoxНКGood_Port.Text = Properties.Settings.Default["НК_Good_Port"]?.ToString() ?? "502";
                textBoxНКGood_Register.Text = Properties.Settings.Default["НК_Good_Register"]?.ToString() ?? "103";

                textBoxНКReject_IP.Text = Properties.Settings.Default["НК_Reject_IP"]?.ToString() ?? "127.0.0.1";
                textBoxНКReject_Port.Text = Properties.Settings.Default["НК_Reject_Port"]?.ToString() ?? "502";
                textBoxНКReject_Register.Text = Properties.Settings.Default["НК_Reject_Register"]?.ToString() ?? "104";

                textBoxOtvorot_IP.Text = Properties.Settings.Default["Отворот_IP"]?.ToString() ?? "127.0.0.1";
                textBoxOtvorot_Port.Text = Properties.Settings.Default["Отворот_Port"]?.ToString() ?? "502";
                textBoxOtvorot_Register.Text = Properties.Settings.Default["Отворот_Register"]?.ToString() ?? "105";

                textBoxOpressovkaGood_IP.Text = Properties.Settings.Default["Опрессовка_Good_IP"]?.ToString() ?? "127.0.0.1";
                textBoxOpressovkaGood_Port.Text = Properties.Settings.Default["Опрессовка_Good_Port"]?.ToString() ?? "502";
                textBoxOpressovkaGood_Register.Text = Properties.Settings.Default["Опрессовка_Good_Register"]?.ToString() ?? "106";

                textBoxOpressovkaReject_IP.Text = Properties.Settings.Default["Опрессовка_Reject_IP"]?.ToString() ?? "127.0.0.1";
                textBoxOpressovkaReject_Port.Text = Properties.Settings.Default["Опрессовка_Reject_Port"]?.ToString() ?? "502";
                textBoxOpressovkaReject_Register.Text = Properties.Settings.Default["Опрессовка_Reject_Register"]?.ToString() ?? "107";

                textBoxKarman_IP.Text = Properties.Settings.Default["Карманы_IP"]?.ToString() ?? "127.0.0.1";
                textBoxKarman_Port.Text = Properties.Settings.Default["Карманы_Port"]?.ToString() ?? "502";
                textBoxKarman_Register.Text = Properties.Settings.Default["Карманы_Register"]?.ToString() ?? "109";

                LoadKarmanBatchSettings();

                
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при загрузке настроек: {ex.Message}");
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

                _sectionLabels[section].Text = $"{section}: {count}         (руками: {addCount - removeCount})";
            }
        }

        private void UpdateGlobalStats()
        {
            int totalAdd = _manualAdditions.Values.Sum();
            int totalRemove = _manualRemovals.Values.Sum();
            int totalInKarmany = _sectionCounters.ContainsKey("Карманы") ? _sectionCounters["Карманы"] : 0;
            int totalInBrak = _sectionCounters.ContainsKey("Брак") ? _sectionCounters["Брак"] : 0;

            labelGlobalStats.Height = 60;
            labelGlobalStats.Font = new System.Drawing.Font(labelGlobalStats.Font.FontFamily, 12.0f, FontStyle.Bold);

            // Обновление текста с учетом новых переменных брака
            labelGlobalStats.Text =
                $"Глобальная статистика:\n" +
                $"Ручное редактирование: {totalAdd - totalRemove}\n" +
                $"Готовых труб: {totalInKarmany}\n" +
                $"Ручное добавление в Брак: {totalInBrak}\n" +
                $"Брак по Шаблону: {_rejectedCountShablon}\n" +
                $"Брак по НК: {_rejectedCountNK}\n" +
                $"Брак по Опрессовке: {_rejectedCountPressed}\n" +
                $"Всего бракованных: {_rejectedCount + totalInBrak}";
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
                LogMessage("Выбранные элементы скопированы в буфер обмена.");
            }
            else
            {
                LogMessage("Нет выбранных элементов для копирования.");
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
                LogMessage($"Невозможно удалить {Math.Abs(delta)} трубу(ы) с '{section}', там всего {_sectionCounters[section]}.");
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
            // Дополнительные инициализации при загрузке формы, если необходимо
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
                LogMessage("Приложение остановлено при закрытии формы.");
            }

            foreach (var modbusService in _modbusServices.Values)
                modbusService.Disconnect();

            SaveStateFunk(); // Сохраняем состояние при закрытии
        }
    }
}
