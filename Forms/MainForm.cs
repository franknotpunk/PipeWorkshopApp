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

        private Dictionary<string, Label> _sectionLabels; // Лейблы для участков
        private ContextMenuStrip _contextMenuSection;
        private string _currentRightClickSection;

        private Dictionary<string, ModbusService> _modbusServices = new Dictionary<string, ModbusService>();

        private string _stateFilePath = "state.json"; // Файл для сохранения состояния

        public MainForm()
        {
            InitializeComponent();

            _httpServerService = new HttpServerService();
            _httpServerService.LogMessageReceived += (sender, msg) =>
            {
                LogMessage(msg); // Вызов вашего метода LogMessage, который обновляет listViewLog в форме
            };
            _httpServerService.MarkingDataReceived += HttpServerService_MarkingDataReceived;

            InitializeCounters();
            InitializeManualTrackers();
            InitializeContextMenu();

            // Логи
            listViewLog.Columns.Add("Сообщение", -2);
            listViewLog.View = View.Details;

            // Бракованные трубы
            listViewRejected.Columns.Add("Время", 200);
            listViewRejected.Columns.Add("Участок", 200);
            listViewRejected.View = View.Details;

            CreateSectionLabels();

            listViewLog.KeyDown += listViewLog_KeyDown;

            LoadSettings();
            InitializeModbusServices();

            LoadState(); // Загружаем состояние из файла
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
            // Добавляем "Брак" тоже в список участков
            _sectionCounters = new Dictionary<string, int>
            {
                {"Шарошка", 0},
                {"НК", 0},
                {"Токарка", 0},
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
            string[] sections = { "Шарошка", "НК", "Токарка", "Отворот", "Опрессовка", "Маркировка", "Карманы", "Брак" };

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

                // Если это "Брак", меняем стиль
                if (section == "Брак")
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
                LogMessage($"Невозможно удалить {Math.Abs(delta)} трубу(ы) с '{section}', там всего {_sectionCounters[section]}.");
                return;
            }

            _sectionCounters[section] = newCount;
            if (delta > 0)
            {
                _manualAdditions[section] += delta;
                LogMessage($"{delta} труб добавлено на '{section}'. Всего: {_sectionCounters[section]}");
            }
            else
            {
                int absDelta = Math.Abs(delta);
                _manualRemovals[section] += absDelta;
                LogMessage($"{absDelta} труб удалено с '{section}'. Всего: {_sectionCounters[section]}");
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

            SaveState(); // Сохраняем состояние при закрытии
        }

        private void InitializeModbusServices()
        {
            try
            {
                _modbusServices.Clear();

                _modbusServices["Создание"] = new ModbusService(
                    Properties.Settings.Default["Создание_IP"] as string,
                    (int)Properties.Settings.Default["Создание_Port"],
                    (int)Properties.Settings.Default["Создание_Register"]
                );

                _modbusServices["Шарошка_Good"] = new ModbusService(
                    Properties.Settings.Default["Шарошка_Good_IP"] as string,
                    (int)Properties.Settings.Default["Шарошка_Good_Port"],
                    (int)Properties.Settings.Default["Шарошка_Good_Register"]
                );

                _modbusServices["Шарошка_Reject"] = new ModbusService(
                    Properties.Settings.Default["Шарошка_Reject_IP"] as string,
                    (int)Properties.Settings.Default["Шарошка_Reject_Port"],
                    (int)Properties.Settings.Default["Шарошка_Reject_Register"]
                );

                _modbusServices["НК_Good"] = new ModbusService(
                    Properties.Settings.Default["НК_Good_IP"] as string,
                    (int)Properties.Settings.Default["НК_Good_Port"],
                    (int)Properties.Settings.Default["НК_Good_Register"]
                );

                _modbusServices["НК_Reject"] = new ModbusService(
                    Properties.Settings.Default["НК_Reject_IP"] as string,
                    (int)Properties.Settings.Default["НК_Reject_Port"],
                    (int)Properties.Settings.Default["НК_Reject_Register"]
                );

                _modbusServices["Токарка"] = new ModbusService(
                    Properties.Settings.Default["Токарка_IP"] as string,
                    (int)Properties.Settings.Default["Токарка_Port"],
                    (int)Properties.Settings.Default["Токарка_Register"]
                );

                _modbusServices["Отворот"] = new ModbusService(
                    Properties.Settings.Default["Отворот_IP"] as string,
                    (int)Properties.Settings.Default["Отворот_Port"],
                    (int)Properties.Settings.Default["Отворот_Register"]
                );

                _modbusServices["Опрессовка_Good"] = new ModbusService(
                    Properties.Settings.Default["Опрессовка_Good_IP"] as string,
                    (int)Properties.Settings.Default["Опрессовка_Good_Port"],
                    (int)Properties.Settings.Default["Опрессовка_Good_Register"]
                );

                _modbusServices["Опрессовка_Reject"] = new ModbusService(
                    Properties.Settings.Default["Опрессовка_Reject_IP"] as string,
                    (int)Properties.Settings.Default["Опрессовка_Reject_Port"],
                    (int)Properties.Settings.Default["Опрессовка_Reject_Register"]
                );

                _modbusServices["Маркировка"] = new ModbusService(
                    Properties.Settings.Default["Маркировка_IP"] as string,
                    (int)Properties.Settings.Default["Маркировка_Port"],
                    (int)Properties.Settings.Default["Маркировка_Register"]
                );

                _modbusServices["Карманы"] = new ModbusService(
                    Properties.Settings.Default["Карманы_IP"] as string,
                    (int)Properties.Settings.Default["Карманы_Port"],
                    (int)Properties.Settings.Default["Карманы_Register"]
                );
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при инициализации Modbus-сервисов: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            textBoxTriggerDelay.Text = Properties.Settings.Default.TriggerDelay.ToString();

            textBoxServerIP.Text = Properties.Settings.Default.ServerIP;
            textBoxServerPort.Text = Properties.Settings.Default.ServerPort.ToString();

            textBoxCreation_IP.Text = Properties.Settings.Default["Создание_IP"] as string;
            textBoxCreation_Port.Text = Properties.Settings.Default["Создание_Port"].ToString();
            textBoxCreation_Register.Text = Properties.Settings.Default["Создание_Register"].ToString();

            textBoxSharoshkaGood_IP.Text = Properties.Settings.Default["Шарошка_Good_IP"] as string;
            textBoxSharoshkaGood_Port.Text = Properties.Settings.Default["Шарошка_Good_Port"].ToString();
            textBoxSharoshkaGood_Register.Text = Properties.Settings.Default["Шарошка_Good_Register"].ToString();

            textBoxSharoshkaReject_IP.Text = Properties.Settings.Default["Шарошка_Reject_IP"] as string;
            textBoxSharoshkaReject_Port.Text = Properties.Settings.Default["Шарошка_Reject_Port"].ToString();
            textBoxSharoshkaReject_Register.Text = Properties.Settings.Default["Шарошка_Reject_Register"].ToString();

            textBoxНКGood_IP.Text = Properties.Settings.Default["НК_Good_IP"] as string;
            textBoxНКGood_Port.Text = Properties.Settings.Default["НК_Good_Port"].ToString();
            textBoxНКGood_Register.Text = Properties.Settings.Default["НК_Good_Register"].ToString();

            textBoxНКReject_IP.Text = Properties.Settings.Default["НК_Reject_IP"] as string;
            textBoxНКReject_Port.Text = Properties.Settings.Default["НК_Reject_Port"].ToString();
            textBoxНКReject_Register.Text = Properties.Settings.Default["НК_Reject_Register"].ToString();

            textBoxTokarka_IP.Text = Properties.Settings.Default["Токарка_IP"] as string;
            textBoxTokarka_Port.Text = Properties.Settings.Default["Токарка_Port"].ToString();
            textBoxTokarka_Register.Text = Properties.Settings.Default["Токарка_Register"].ToString();

            textBoxOtvorot_IP.Text = Properties.Settings.Default["Отворот_IP"] as string;
            textBoxOtvorot_Port.Text = Properties.Settings.Default["Отворот_Port"].ToString();
            textBoxOtvorot_Register.Text = Properties.Settings.Default["Отворот_Register"].ToString();

            textBoxOpressovkaGood_IP.Text = Properties.Settings.Default["Опрессовка_Good_IP"] as string;
            textBoxOpressovkaGood_Port.Text = Properties.Settings.Default["Опрессовка_Good_Port"].ToString();
            textBoxOpressovkaGood_Register.Text = Properties.Settings.Default["Опрессовка_Good_Register"].ToString();

            textBoxOpressovkaReject_IP.Text = Properties.Settings.Default["Опрессовка_Reject_IP"] as string;
            textBoxOpressovkaReject_Port.Text = Properties.Settings.Default["Опрессовка_Reject_Port"].ToString();
            textBoxOpressovkaReject_Register.Text = Properties.Settings.Default["Опрессовка_Reject_Register"].ToString();

            textBoxMarkirovka_IP.Text = Properties.Settings.Default["Маркировка_IP"] as string;
            textBoxMarkirovka_Port.Text = Properties.Settings.Default["Маркировка_Port"].ToString();
            textBoxMarkirovka_Register.Text = Properties.Settings.Default["Маркировка_Register"].ToString();

            textBoxKarman_IP.Text = Properties.Settings.Default["Карманы_IP"] as string;
            textBoxKarman_Port.Text = Properties.Settings.Default["Карманы_Port"].ToString();
            textBoxKarman_Register.Text = Properties.Settings.Default["Карманы_Register"].ToString();
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.TriggerDelay = int.Parse(textBoxTriggerDelay.Text);

            Properties.Settings.Default.ServerIP = textBoxServerIP.Text;
            Properties.Settings.Default.ServerPort = int.Parse(textBoxServerPort.Text);

            Properties.Settings.Default["Создание_IP"] = textBoxCreation_IP.Text;
            Properties.Settings.Default["Создание_Port"] = int.Parse(textBoxCreation_Port.Text);
            Properties.Settings.Default["Создание_Register"] = int.Parse(textBoxCreation_Register.Text);

            Properties.Settings.Default["Шарошка_Good_IP"] = textBoxSharoshkaGood_IP.Text;
            Properties.Settings.Default["Шарошка_Good_Port"] = int.Parse(textBoxSharoshkaGood_Port.Text);
            Properties.Settings.Default["Шарошка_Good_Register"] = int.Parse(textBoxSharoshkaGood_Register.Text);

            Properties.Settings.Default["Шарошка_Reject_IP"] = textBoxSharoshkaReject_IP.Text;
            Properties.Settings.Default["Шарошка_Reject_Port"] = int.Parse(textBoxSharoshkaReject_Port.Text);
            Properties.Settings.Default["Шарошка_Reject_Register"] = int.Parse(textBoxSharoshkaReject_Register.Text);

            Properties.Settings.Default["НК_Good_IP"] = textBoxНКGood_IP.Text;
            Properties.Settings.Default["НК_Good_Port"] = int.Parse(textBoxНКGood_Port.Text);
            Properties.Settings.Default["НК_Good_Register"] = int.Parse(textBoxНКGood_Register.Text);

            Properties.Settings.Default["НК_Reject_IP"] = textBoxНКReject_IP.Text;
            Properties.Settings.Default["НК_Reject_Port"] = int.Parse(textBoxНКReject_Port.Text);
            Properties.Settings.Default["НК_Reject_Register"] = int.Parse(textBoxНКReject_Register.Text);

            Properties.Settings.Default["Токарка_IP"] = textBoxTokarka_IP.Text;
            Properties.Settings.Default["Токарка_Port"] = int.Parse(textBoxTokarka_Port.Text);
            Properties.Settings.Default["Токарка_Register"] = int.Parse(textBoxTokarka_Register.Text);

            Properties.Settings.Default["Отворот_IP"] = textBoxOtvorot_IP.Text;
            Properties.Settings.Default["Отворот_Port"] = int.Parse(textBoxOtvorot_Port.Text);
            Properties.Settings.Default["Отворот_Register"] = int.Parse(textBoxOtvorot_Register.Text);

            Properties.Settings.Default["Опрессовка_Good_IP"] = textBoxOpressovkaGood_IP.Text;
            Properties.Settings.Default["Опрессовка_Good_Port"] = int.Parse(textBoxOpressovkaGood_Port.Text);
            Properties.Settings.Default["Опрессовка_Good_Register"] = int.Parse(textBoxOpressovkaGood_Register.Text);

            Properties.Settings.Default["Опрессовка_Reject_IP"] = textBoxOpressovkaReject_IP.Text;
            Properties.Settings.Default["Опрессовка_Reject_Port"] = int.Parse(textBoxOpressovkaReject_Port.Text);
            Properties.Settings.Default["Опрессовка_Reject_Register"] = int.Parse(textBoxOpressovkaReject_Register.Text);

            Properties.Settings.Default["Маркировка_IP"] = textBoxMarkirovka_IP.Text;
            Properties.Settings.Default["Маркировка_Port"] = int.Parse(textBoxMarkirovka_Port.Text);
            Properties.Settings.Default["Маркировка_Register"] = int.Parse(textBoxMarkirovka_Register.Text);

            Properties.Settings.Default["Карманы_IP"] = textBoxKarman_IP.Text;
            Properties.Settings.Default["Карманы_Port"] = int.Parse(textBoxKarman_Port.Text);
            Properties.Settings.Default["Карманы_Register"] = int.Parse(textBoxKarman_Register.Text);

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
                        if (_modbusServices["Создание"].CheckTrigger())
                        {
                            _sectionCounters["Шарошка"]++;
                            LogMessage($"Труба добавлена на 'Шарошка'. Всего: {_sectionCounters["Шарошка"]}");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["Шарошка_Good"].CheckTrigger())
                        {
                            MovePipe("Шарошка", "НК");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["Шарошка_Reject"].CheckTrigger())
                        {
                            RejectPipe("Шарошка");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["НК_Good"].CheckTrigger())
                        {
                            MovePipe("НК", "Токарка");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["НК_Reject"].CheckTrigger())
                        {
                            RejectPipe("Шарошка");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["Токарка"].CheckTrigger())
                        {
                            MovePipe("Токарка", "Отворот");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["Отворот"].CheckTrigger())
                        {
                            MovePipe("Отворот", "Опрессовка");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["Опрессовка_Good"].CheckTrigger())
                        {
                            MovePipe("Опрессовка", "Маркировка");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }

                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["Опрессовка_Reject"].CheckTrigger())
                        {
                            RejectPipe("Опрессовка");
                            UpdateSectionLabels();
                            UpdateGlobalStats();
                        }


                        Thread.Sleep(Properties.Settings.Default.TriggerDelay);
                        if (_modbusServices["Карманы"].CheckTrigger())
                        {
                            LogMessage("Заглушка, логика карманов будет реализованна позже");
                            // todo: тут ебучая логика карманов, СДЕЛАТЬ!!!!
                        }

                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Ошибка в основном цикле: {ex.Message}");
                    }
                }
                LogMessage("Основной цикл остановлен.");
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
                LogMessage($"Труба перемещена из '{fromSection}' в '{toSection}'. " +
                           $"'{fromSection}': {_sectionCounters[fromSection]}, '{toSection}': {_sectionCounters[toSection]}");
            }
            else
            {
                LogMessage($"Нет труб в '{fromSection}' для перемещения в '{toSection}'.");
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

                LogMessage($"Труба забракована на участке '{sectionName}'. Всего бракованных: {_rejectedCount}");
            }
            else
            {
                LogMessage($"Нет труб на участке '{sectionName}' для брака.");
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
            if (_sectionCounters["Маркировка"] > 0)
            {
                _sectionCounters["Маркировка"]--;
                _sectionCounters["Карманы"]++;
                LogMessage($"Маркировка завершена для трубы {markingData.PipeNumber}. 'Маркировка': {_sectionCounters["Маркировка"]}, 'Карманы': {_sectionCounters["Карманы"]}");
                SaveMarkedPipeData(markingData);
            }
            else
            {
                LogMessage("Нет труб на участке 'Маркировка' для завершения маркировки.");
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

            LogMessage($"Данные о маркировке сохранены в БД для трубы {markingData.PipeNumber}.");
        }

        private void UpdateSectionLabels()
        {
            foreach (var section in _sectionLabels.Keys)
            {
                int count = _sectionCounters[section];
                int addCount = _manualAdditions[section];
                int removeCount = _manualRemovals[section];

                _sectionLabels[section].Text = $"{section}: {count}         (руками:{addCount - removeCount})";
            }
        }

        private void UpdateGlobalStats()
        {
            int totalAdd = _manualAdditions.Values.Sum();
            int totalRemove = _manualRemovals.Values.Sum();
            int totalInKarmany = _sectionCounters["Карманы"];
            int totalInBrak = _sectionCounters["Брак"];

            labelGlobalStats.Height = 30;
            labelGlobalStats.Font = new Font(labelGlobalStats.Font.FontFamily, 12.0f, FontStyle.Bold);

            // Допустим, у нас есть labelGlobalStats на форме
            labelGlobalStats.Text =
                $"Глобальная статистика:\n" +
                $"Ручное редактирование: {totalAdd - totalRemove}\n" +
                $"Всего в Карманы: {totalInKarmany}\n" +
                $"Ручное добавление в Брак: {totalInBrak}\n" +
                $"Всего бракованных: {_rejectedCount + totalInBrak}";
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

                    LogMessage("Состояние загружено.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при загрузке состояния: {ex.Message}");
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
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_stateFilePath, json);
                LogMessage("Состояние сохранено.");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при сохранении состояния: {ex.Message}");
            }
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
            listViewRejected.Items.Clear();

            UpdateSectionLabels();
            UpdateGlobalStats();
            LogMessage("Состояние сброшено.");
        }

        private async void button_start_Click(object sender, EventArgs e)
        {
            await _httpServerService.StartServer(Properties.Settings.Default.ServerIP, Properties.Settings.Default.ServerPort);
            // Сервер будет запущен, не блокируя UI
            StartMainLoop();
            LogMessage("Приложение запущено.");
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            StopMainLoop();
            LogMessage("Приложение остановлено.");
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            LoadSettings();
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
                LogMessage("Выбранные элементы скопированы в буфер обмена.");
            }
            else
            {
                LogMessage("Нет выбранных элементов для копирования.");
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
