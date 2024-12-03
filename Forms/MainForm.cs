using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PipeWorkshopApp.Models;
using PipeWorkshopApp.Services;
using System.Net.Http;
using PipeWorkshopApp.Properties;
using System.Threading;
using System.ComponentModel;

namespace PipeWorkshopApp
{
    public partial class MainForm : Form
    {
        private HttpServerService _httpServerService;
        private List<SectionQueue> _sectionQueues;
        private List<PipeData> rejectedPipes = new List<PipeData>(); // Список бракованных труб
        private CancellationTokenSource _cancellationTokenSource;

        // Словарь Modbus-сервисов для триггеров
        private Dictionary<string, ModbusService> _modbusServices = new Dictionary<string, ModbusService>();

        public MainForm()
        {
            InitializeComponent();

            // Инициализация сервиса HTTP-сервера
            _httpServerService = new HttpServerService();
            _httpServerService.MarkingDataReceived += HttpServerService_MarkingDataReceived;

            // Инициализация участков
            InitializeSections();

            // Загрузка настроек и инициализация Modbus-сервисов
            LoadSettings();
            InitializeModbusServices();

            // Запуск HTTP-сервера
            StartHttpServer();

            listBoxSharoshka.ContextMenuStrip = contextMenu;
            listBoxNK.ContextMenuStrip = contextMenu;
            listBoxTokarka.ContextMenuStrip = contextMenu;
            listBoxOtvrot.ContextMenuStrip = contextMenu;
            listBoxOpressovka.ContextMenuStrip = contextMenu;
            listBoxMarkirovka.ContextMenuStrip = contextMenu;
            listBoxRejectedPipes.ContextMenuStrip = contextMenu;


        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Останавливаем основной цикл
            StopMainLoop();

            // Останавливаем HTTP-сервер
            _httpServerService.StopServer();

            // Отключаем все Modbus-сервисы
            foreach (var modbusService in _modbusServices.Values)
            {
                modbusService.Disconnect();
            }
        }

        private void InitializeSections()
        {
            _sectionQueues = new List<SectionQueue>
            {
                new SectionQueue("Шарошка"),
                new SectionQueue("НК"),
                new SectionQueue("Токарка"),
                new SectionQueue("Отворот"),
                new SectionQueue("Опрессовка"),
                new SectionQueue("Маркировка"),
                new SectionQueue("Карманы")
            };

        }

        private void AddNewPipe()
        {
            var pipeData = new PipeData();
            var sharoshkaSection = _sectionQueues.Find(sq => sq.SectionName == "Шарошка");
            sharoshkaSection.Pipes.Enqueue(pipeData);

            // Обновляем интерфейс
            Invoke(new Action(() => UpdateListBoxes()));
        }

        private void InitializeModbusServices()
        {
            // Инициализируем Modbus-сервисы, используя настройки из Properties.Settings.Default
            try
            {
                // Создание
                _modbusServices["Создание"] = new ModbusService(
                    Properties.Settings.Default["Создание_IP"] as string,
                    (int)Properties.Settings.Default["Создание_Port"],
                    (int)Properties.Settings.Default["Создание_Register"]
                );

                // Шарошка - триггер годной трубы
                _modbusServices["Шарошка_Good"] = new ModbusService(
                    Properties.Settings.Default["Шарошка_Good_IP"] as string,
                    (int)Properties.Settings.Default["Шарошка_Good_Port"],
                    (int)Properties.Settings.Default["Шарошка_Good_Register"]
                );

                // Шарошка - триггер брака
                _modbusServices["Шарошка_Reject"] = new ModbusService(
                    Properties.Settings.Default["Шарошка_Reject_IP"] as string,
                    (int)Properties.Settings.Default["Шарошка_Reject_Port"],
                    (int)Properties.Settings.Default["Шарошка_Reject_Register"]
                );

                // НК
                _modbusServices["НК"] = new ModbusService(
                    Properties.Settings.Default["НК_IP"] as string,
                    (int)Properties.Settings.Default["НК_Port"],
                    (int)Properties.Settings.Default["НК_Register"]
                );

                // Токарка
                _modbusServices["Токарка"] = new ModbusService(
                    Properties.Settings.Default["Токарка_IP"] as string,
                    (int)Properties.Settings.Default["Токарка_Port"],
                    (int)Properties.Settings.Default["Токарка_Register"]
                );

                // Отворот
                _modbusServices["Отворот"] = new ModbusService(
                    Properties.Settings.Default["Отворот_IP"] as string,
                    (int)Properties.Settings.Default["Отворот_Port"],
                    (int)Properties.Settings.Default["Отворот_Register"]
                );

                // Опрессовка - триггер годной трубы
                _modbusServices["Опрессовка_Good"] = new ModbusService(
                    Properties.Settings.Default["Опрессовка_Good_IP"] as string,
                    (int)Properties.Settings.Default["Опрессовка_Good_Port"],
                    (int)Properties.Settings.Default["Опрессовка_Good_Register"]
                );

                // Опрессовка - триггер брака
                _modbusServices["Опрессовка_Reject"] = new ModbusService(
                    Properties.Settings.Default["Опрессовка_Reject_IP"] as string,
                    (int)Properties.Settings.Default["Опрессовка_Reject_Port"],
                    (int)Properties.Settings.Default["Опрессовка_Reject_Register"]
                );

                // Маркировка
                _modbusServices["Маркировка"] = new ModbusService(
                    Properties.Settings.Default["Маркировка_IP"] as string,
                    (int)Properties.Settings.Default["Маркировка_Port"],
                    (int)Properties.Settings.Default["Маркировка_Register"]
                );

                // Карманы
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


        private void StartHttpServer()
        {
            string ipAddress = Properties.Settings.Default.ServerIP;
            int port = Properties.Settings.Default.ServerPort;

            _httpServerService.StartServer(ipAddress, port);
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
                        // Участок "Создание"
                        if (_modbusServices["Создание"].CheckTrigger())
                        {
                            AddNewPipe();
                        }

                        // "Шарошка" - триггер годной трубы
                        if (_modbusServices["Шарошка_Good"].CheckTrigger())
                        {
                            var pipe = GetCurrentPipe("Шарошка");
                            if (pipe != null)
                            {
                                MovePipeToNextQueue(pipe, "Шарошка");
                            }
                        }

                        // "Шарошка" - триггер брака
                        if (_modbusServices["Шарошка_Reject"].CheckTrigger())
                        {
                            var pipe = GetCurrentPipe("Шарошка");
                            if (pipe != null)
                            {
                                RejectPipe(pipe, "Шарошка");
                            }
                        }

                        // Участок "НК"
                        if (_modbusServices["НК"].CheckTrigger())
                        {
                            var pipe = GetCurrentPipe("НК");
                            if (pipe != null)
                            {
                                // TODO: выполнить групповой запрос к 3 компьютерам
                                var urls = new[]
                                {
                                    Properties.Settings.Default["NDT_DeviceURL1"] as string,
                                    Properties.Settings.Default["NDT_DeviceURL2"] as string,
                                    Properties.Settings.Default["NDT_DeviceURL3"] as string,
                                };

                                var tasks = urls.Select(url => SendGetRequest(url)).ToArray();
                                bool[] results = await Task.WhenAll(tasks);

                                bool isPipeGood = results.All(r => r);

                                if (isPipeGood)
                                {
                                    MovePipeToNextQueue(pipe, "НК");
                                }
                                else
                                {
                                    RejectPipe(pipe, "НК");
                                }
                            }
                        }

                        // Участок "Токарка"
                        if (_modbusServices["Токарка"].CheckTrigger())
                        {
                            // Логика для участка "Токарка"
                            var pipe = GetCurrentPipe("Токарка");
                            if (pipe != null)
                            {
                                MovePipeToNextQueue(pipe, "Токарка");
                            }
                        }

                        // Участок "Отворот"
                        if (_modbusServices["Отворот"].CheckTrigger())
                        {
                            var pipe = GetCurrentPipe("Отворот");
                            if (pipe != null)
                            {
                                string url = Properties.Settings.Default["Otvorot_DeviceURL4"] as string;
                                bool isPipeGood = await SendGetRequest(url);

                                if (isPipeGood)
                                {
                                    MovePipeToNextQueue(pipe, "Отворот");
                                }
                                else
                                {
                                    RejectPipe(pipe, "Отворот");
                                }
                            }
                        }

                        // Участок "Опрессовка" годная
                        if (_modbusServices["Опрессовка_Good"].CheckTrigger())
                        {
                            // Логика для участка "Опрессовка"
                            var pipe = GetCurrentPipe("Опрессовка");
                            if (pipe != null)
                            {
                                MovePipeToNextQueue(pipe, "Опрессовка");
                            }
                        }
                        // Участок "Опрессовка" брак
                        if (_modbusServices["Опрессовка_Reject"].CheckTrigger())
                        {
                            // Логика для участка "Опрессовка"
                            var pipe = GetCurrentPipe("Опрессовка");
                            if (pipe != null)
                            {
                                RejectPipe(pipe, "Опрессовка");
                            }
                        }

                        // Участок "Маркировка"
                        if (_modbusServices["Маркировка"].CheckTrigger())
                        {
                            // Логика для участка "Маркировка"
                            var pipe = GetCurrentPipe("Маркировка");
                            if (pipe != null)
                            {
                                //todo: тут нужно реализовать логику получения данных от маркиратора. Здесь только годная труба. Наша задача записать данные в базу данных.
                            }
                        }

                        if (_modbusServices["Карманы"].CheckTrigger())
                        {
                            //todo: тут мы будем решать в какой карман попадать трубе. И будет сохранять инфу в базу данных именно что по карманам.
                        }

                        await Task.Delay(500); // Пауза между итерациями цикла
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Ошибка в основном цикле: {ex.Message}");
                        // Обработка ошибок
                    }

                    LogMessage("Основной цикл остановлен.");
                }
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

        // Метод для перемещения трубы в следующую очередь
        private void MovePipeToNextQueue(PipeData pipe, string currentSectionName)
        {
            int currentIndex = _sectionQueues.FindIndex(sq => sq.SectionName == currentSectionName);
            if (currentIndex >= 0 && currentIndex < _sectionQueues.Count - 1)
            {
                var nextSection = _sectionQueues[currentIndex + 1];
                nextSection.Pipes.Enqueue(pipe);

                // Обновляем интерфейс
                Invoke(new Action(() => UpdateListBoxes()));
            }
            else if (currentIndex == _sectionQueues.Count - 1)
            {
                // Последний этап (Карманы)
                // Здесь можно реализовать дополнительную логику
            }
        }

        // Метод для перемещения трубы в брак
        private void RejectPipe(PipeData pipe, string currentSectionName)
        {
            pipe.IsRejected = true;
            pipe.RejectionStage = currentSectionName;
            rejectedPipes.Add(pipe);

            // Обновляем интерфейс
            Invoke(new Action(() => UpdateListBoxes()));
        }

        // Метод для отправки GET-запроса по указанному адресу
        private async Task<bool> SendGetRequest(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseData = await response.Content.ReadAsStringAsync();

                    // Предполагаем, что ответ содержит "good" или "reject"
                    return responseData.Trim().ToLower() == "good";
                }
                catch (Exception ex)
                {
                    LogMessage($"Ошибка при отправке GET-запроса: {ex.Message}");
                    // Решите, как обрабатывать ошибки связи с устройством
                    return false;
                }
            }
        }

        // Метод для получения текущей трубы на участке
        private PipeData GetCurrentPipe(string sectionName)
        {
            var currentSection = _sectionQueues.Find(sq => sq.SectionName == sectionName);
            if (currentSection != null && currentSection.Pipes.Count > 0)
            {
                var pipe = currentSection.Pipes.Dequeue(); // Извлекаем трубу из очереди
                return pipe;
            }
            else
            {
                string message = $"Нет трубы в очереди '{sectionName}'. Возможно, неправильное чтение или несрабатывание триггера.";
                LogMessage(message);
                return null;
            }
        }


        private void UpdateListBoxes()
        {
            foreach (var section in _sectionQueues)
            {
                // Предположим, у вас есть словарь, сопоставляющий названия участков с ListBox
                ListBox listBox = GetListBoxForSection(section.SectionName);
                if (listBox != null)
                {
                    listBox.DataSource = null;
                    listBox.DataSource = new List<PipeData>(section.Pipes);
                    listBox.DisplayMember = "Id"; // Или другое свойство для отображения
                }
            }

            // Обновление списка бракованных труб
            listBoxRejectedPipes.DataSource = null;
            listBoxRejectedPipes.DataSource = rejectedPipes;
            listBoxRejectedPipes.DisplayMember = "RejectionStage";
        }

        // Метод для получения соответствующего ListBox для участка
        private ListBox GetListBoxForSection(string sectionName)
        {
            switch (sectionName)
            {
                case "Шарошка":
                    return listBoxSharoshka;
                case "НК":
                    return listBoxNK;
                case "Токарка":
                    return listBoxTokarka;
                case "Отворот":
                    return listBoxOtvrot;
                case "Опрессовка":
                    return listBoxOpressovka;
                case "Маркировка":
                    return listBoxMarkirovka;
                default:
                    return null;
            }
        }

        private void LoadSettings()
        {
            // Загрузка настроек в элементы управления формы
            textBoxServerIP.Text = Properties.Settings.Default.ServerIP;
            textBoxServerPort.Text = Properties.Settings.Default.ServerPort.ToString();

            textBoxNTD1.Text = Properties.Settings.Default["NDT_DeviceURL1"] as string;
            textBoxNTD2.Text = Properties.Settings.Default["NDT_DeviceURL2"] as string;
            textBoxNTD3.Text = Properties.Settings.Default["NDT_DeviceURL3"] as string;
            textBoxNTD4.Text = Properties.Settings.Default["Otvorot_DeviceURL4"] as string;


            // Загрузка настроек для Modbus-сервисов
            textBoxCreation_IP.Text = Properties.Settings.Default["Создание_IP"] as string;
            textBoxCreation_Port.Text = Properties.Settings.Default["Создание_Port"].ToString();
            textBoxCreation_Register.Text = Properties.Settings.Default["Создание_Register"].ToString();

            textBoxSharoshkaGood_IP.Text = Properties.Settings.Default["Шарошка_Good_IP"] as string;
            textBoxSharoshkaGood_Port.Text = Properties.Settings.Default["Шарошка_Good_Port"].ToString();
            textBoxSharoshkaGood_Register.Text = Properties.Settings.Default["Шарошка_Good_Register"].ToString();

            textBoxSharoshkaReject_IP.Text = Properties.Settings.Default["Шарошка_Reject_IP"] as string;
            textBoxSharoshkaReject_Port.Text = Properties.Settings.Default["Шарошка_Reject_Port"].ToString();
            textBoxSharoshkaReject_Register.Text = Properties.Settings.Default["Шарошка_Reject_Register"].ToString();

            textBoxНК_IP.Text = Properties.Settings.Default["НК_IP"] as string;
            textBoxНК_Port.Text = Properties.Settings.Default["НК_Port"].ToString();
            textBoxНК_Register.Text = Properties.Settings.Default["НК_Register"].ToString();

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

            // Повторите для других участков
        }

        private void SaveSettings()
        {
            // Сохранение настроек из элементов управления формы
            Properties.Settings.Default.ServerIP = textBoxServerIP.Text;
            Properties.Settings.Default.ServerPort = int.Parse(textBoxServerPort.Text);

            Properties.Settings.Default["NDT_DeviceURL1"] = textBoxNTD1.Text;
            Properties.Settings.Default["NDT_DeviceURL2"] = textBoxNTD2.Text;
            Properties.Settings.Default["NDT_DeviceURL3"] = textBoxNTD3.Text;
            Properties.Settings.Default["Otvorot_DeviceURL4"] = textBoxNTD4.Text;

            // Сохранение настроек для Modbus-сервисов
            Properties.Settings.Default["Создание_IP"] = textBoxCreation_IP.Text;
            Properties.Settings.Default["Создание_Port"] = int.Parse(textBoxCreation_Port.Text);
            Properties.Settings.Default["Создание_Register"] = int.Parse(textBoxCreation_Register.Text);

            Properties.Settings.Default["Шарошка_Good_IP"] = textBoxSharoshkaGood_IP.Text;
            Properties.Settings.Default["Шарошка_Good_Port"] = int.Parse(textBoxSharoshkaGood_Port.Text);
            Properties.Settings.Default["Шарошка_Good_Register"] = int.Parse(textBoxSharoshkaGood_Register.Text);

            Properties.Settings.Default["Шарошка_Reject_IP"] = textBoxSharoshkaReject_IP.Text;
            Properties.Settings.Default["Шарошка_Reject_Port"] = int.Parse(textBoxSharoshkaReject_Port.Text);
            Properties.Settings.Default["Шарошка_Reject_Register"] = int.Parse(textBoxSharoshkaReject_Register.Text);

            Properties.Settings.Default["НК_IP"] = textBoxНК_IP.Text;
            Properties.Settings.Default["НК_Port"] = int.Parse(textBoxНК_Port.Text);
            Properties.Settings.Default["НК_Register"] = int.Parse(textBoxНК_Register.Text);

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

            // Повторите для других участков

            // Сохранение настроек для постоянства между сессиями
            Properties.Settings.Default.Save();
        }


        private void HttpServerService_MarkingDataReceived(object sender, MarkingData markingData)
        {
            // Обработка данных от маркировщика
            // Ищем трубу по Id и обновляем ее данные
            var pipe = FindPipeById(markingData.PipeId);
            if (pipe != null)
            {
                pipe.MarkingInfo = markingData.Info;

                // Сохраняем данные в базе
                SavePipeData(pipe);
            }

            // Обновляем интерфейс
            Invoke(new Action(() => UpdateListBoxes()));
        }

        private PipeData FindPipeById(Guid pipeId)
        {
            // Ищем трубу в очередях и в бракованных трубах
            foreach (var section in _sectionQueues)
            {
                var pipe = section.Pipes.FirstOrDefault(p => p.Id == pipeId);
                if (pipe != null)
                    return pipe;
            }

            var rejectedPipe = rejectedPipes.FirstOrDefault(p => p.Id == pipeId);
            if (rejectedPipe != null)
                return rejectedPipe;

            return null;
        }

        private void SavePipeData(PipeData pipeData)
        {
            // Сохраняем данные трубы в базе данных
            using var dbContext = new AppDbContext();
            dbContext.Pipes.Add(pipeData);
            dbContext.SaveChanges();
        }

        private void LogMessage(string message)
        {
            if (listBoxLog.InvokeRequired)
            {
                listBoxLog.Invoke(new Action(() => listBoxLog.Items.Add(message)));
            }
            else
            {
                listBoxLog.Items.Add(message);
            }
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            StartMainLoop();
            LogMessage("Приложение запущено.");
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            StopMainLoop();
            LogMessage("Приложение остановлено.");
        }


        private void AddPipeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var listBox = contextMenu.SourceControl as ListBox;
            if (listBox != null)
            {
                string sectionName = GetSectionNameByListBox(listBox);
                AddPipeToSection(sectionName);
            }
        }
        private void DeletePipeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var listBox = contextMenu.SourceControl as ListBox;
            if (listBox != null)
            {
                string sectionName = GetSectionNameByListBox(listBox);
                RemoveSelectedPipeFromSection(sectionName, listBox);
            }
        }
        private void AddPipeToSection(string sectionName)
        {
            var pipeData = new PipeData();
            var section = _sectionQueues.Find(sq => sq.SectionName == sectionName);
            if (section != null)
            {
                section.Pipes.Enqueue(pipeData);
                LogMessage($"Труба добавлена в очередь '{sectionName}'.");
                UpdateListBoxes();
            }
            else
            {
                LogMessage($"Ошибка: очередь '{sectionName}' не найдена.");
            }
        }
        private void RemoveSelectedPipeFromSection(string sectionName, ListBox listBox)
        {
            var section = _sectionQueues.Find(sq => sq.SectionName == sectionName);
            if (section != null && listBox.SelectedItem is PipeData selectedPipe)
            {
                // Создаем новую очередь без удаляемой трубы
                section.Pipes = new Queue<PipeData>(section.Pipes.Where(p => p.Id != selectedPipe.Id));
                LogMessage($"Труба удалена из очереди '{sectionName}'.");
                UpdateListBoxes();
            }
            else
            {
                LogMessage($"Ошибка: не выбрана труба для удаления из очереди '{sectionName}'.");
            }
        }

        private string GetSectionNameByListBox(ListBox listBox)
        {
            if (listBox == listBoxSharoshka)
                return "Шарошка";
            else if (listBox == listBoxNK)
                return "НК";
            else if (listBox == listBoxTokarka)
                return "Токарка";
            else if (listBox == listBoxOtvrot)
                return "Отворот";
            else if (listBox == listBoxOpressovka)
                return "Опрессовка";
            else if (listBox == listBoxMarkirovka)
                return "Маркировка";
            else if (listBox == listBoxRejectedPipes)
                return "Карманы";
            else
                return null;
        }

        private void contextMenu_Opening(object sender, CancelEventArgs e)
        {
            var contextMenu = sender as ContextMenuStrip;
            var listBox = contextMenu.SourceControl as ListBox;

            if (listBox == null)
            {
                e.Cancel = true; // Нет связанного ListBox, отменяем меню
                return;
            }

            // Получаем позицию мыши относительно ListBox
            Point mousePos = listBox.PointToClient(Cursor.Position);
            int index = listBox.IndexFromPoint(mousePos);

            if (index != ListBox.NoMatches)
            {
                // Элемент выбран
                listBox.SelectedIndex = index;
                deletePipeToolStripMenuItem.Enabled = true;
            }
            else
            {
                // Элемент не выбран
                deletePipeToolStripMenuItem.Enabled = false;
                listBox.ClearSelected();
            }
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            LoadSettings();
        }
    }
}
