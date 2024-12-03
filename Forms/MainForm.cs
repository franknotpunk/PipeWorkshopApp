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
        private List<PipeData> rejectedPipes = new List<PipeData>(); // ������ ����������� ����
        private CancellationTokenSource _cancellationTokenSource;

        // ������� Modbus-�������� ��� ���������
        private Dictionary<string, ModbusService> _modbusServices = new Dictionary<string, ModbusService>();

        public MainForm()
        {
            InitializeComponent();

            // ������������� ������� HTTP-�������
            _httpServerService = new HttpServerService();
            _httpServerService.MarkingDataReceived += HttpServerService_MarkingDataReceived;

            // ������������� ��������
            InitializeSections();

            // �������� �������� � ������������� Modbus-��������
            LoadSettings();
            InitializeModbusServices();

            // ������ HTTP-�������
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
            // ������������� �������� ����
            StopMainLoop();

            // ������������� HTTP-������
            _httpServerService.StopServer();

            // ��������� ��� Modbus-�������
            foreach (var modbusService in _modbusServices.Values)
            {
                modbusService.Disconnect();
            }
        }

        private void InitializeSections()
        {
            _sectionQueues = new List<SectionQueue>
            {
                new SectionQueue("�������"),
                new SectionQueue("��"),
                new SectionQueue("�������"),
                new SectionQueue("�������"),
                new SectionQueue("����������"),
                new SectionQueue("����������"),
                new SectionQueue("�������")
            };

        }

        private void AddNewPipe()
        {
            var pipeData = new PipeData();
            var sharoshkaSection = _sectionQueues.Find(sq => sq.SectionName == "�������");
            sharoshkaSection.Pipes.Enqueue(pipeData);

            // ��������� ���������
            Invoke(new Action(() => UpdateListBoxes()));
        }

        private void InitializeModbusServices()
        {
            // �������������� Modbus-�������, ��������� ��������� �� Properties.Settings.Default
            try
            {
                // ��������
                _modbusServices["��������"] = new ModbusService(
                    Properties.Settings.Default["��������_IP"] as string,
                    (int)Properties.Settings.Default["��������_Port"],
                    (int)Properties.Settings.Default["��������_Register"]
                );

                // ������� - ������� ������ �����
                _modbusServices["�������_Good"] = new ModbusService(
                    Properties.Settings.Default["�������_Good_IP"] as string,
                    (int)Properties.Settings.Default["�������_Good_Port"],
                    (int)Properties.Settings.Default["�������_Good_Register"]
                );

                // ������� - ������� �����
                _modbusServices["�������_Reject"] = new ModbusService(
                    Properties.Settings.Default["�������_Reject_IP"] as string,
                    (int)Properties.Settings.Default["�������_Reject_Port"],
                    (int)Properties.Settings.Default["�������_Reject_Register"]
                );

                // ��
                _modbusServices["��"] = new ModbusService(
                    Properties.Settings.Default["��_IP"] as string,
                    (int)Properties.Settings.Default["��_Port"],
                    (int)Properties.Settings.Default["��_Register"]
                );

                // �������
                _modbusServices["�������"] = new ModbusService(
                    Properties.Settings.Default["�������_IP"] as string,
                    (int)Properties.Settings.Default["�������_Port"],
                    (int)Properties.Settings.Default["�������_Register"]
                );

                // �������
                _modbusServices["�������"] = new ModbusService(
                    Properties.Settings.Default["�������_IP"] as string,
                    (int)Properties.Settings.Default["�������_Port"],
                    (int)Properties.Settings.Default["�������_Register"]
                );

                // ���������� - ������� ������ �����
                _modbusServices["����������_Good"] = new ModbusService(
                    Properties.Settings.Default["����������_Good_IP"] as string,
                    (int)Properties.Settings.Default["����������_Good_Port"],
                    (int)Properties.Settings.Default["����������_Good_Register"]
                );

                // ���������� - ������� �����
                _modbusServices["����������_Reject"] = new ModbusService(
                    Properties.Settings.Default["����������_Reject_IP"] as string,
                    (int)Properties.Settings.Default["����������_Reject_Port"],
                    (int)Properties.Settings.Default["����������_Reject_Register"]
                );

                // ����������
                _modbusServices["����������"] = new ModbusService(
                    Properties.Settings.Default["����������_IP"] as string,
                    (int)Properties.Settings.Default["����������_Port"],
                    (int)Properties.Settings.Default["����������_Register"]
                );

                // �������
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
                        // ������� "��������"
                        if (_modbusServices["��������"].CheckTrigger())
                        {
                            AddNewPipe();
                        }

                        // "�������" - ������� ������ �����
                        if (_modbusServices["�������_Good"].CheckTrigger())
                        {
                            var pipe = GetCurrentPipe("�������");
                            if (pipe != null)
                            {
                                MovePipeToNextQueue(pipe, "�������");
                            }
                        }

                        // "�������" - ������� �����
                        if (_modbusServices["�������_Reject"].CheckTrigger())
                        {
                            var pipe = GetCurrentPipe("�������");
                            if (pipe != null)
                            {
                                RejectPipe(pipe, "�������");
                            }
                        }

                        // ������� "��"
                        if (_modbusServices["��"].CheckTrigger())
                        {
                            var pipe = GetCurrentPipe("��");
                            if (pipe != null)
                            {
                                // TODO: ��������� ��������� ������ � 3 �����������
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
                                    MovePipeToNextQueue(pipe, "��");
                                }
                                else
                                {
                                    RejectPipe(pipe, "��");
                                }
                            }
                        }

                        // ������� "�������"
                        if (_modbusServices["�������"].CheckTrigger())
                        {
                            // ������ ��� ������� "�������"
                            var pipe = GetCurrentPipe("�������");
                            if (pipe != null)
                            {
                                MovePipeToNextQueue(pipe, "�������");
                            }
                        }

                        // ������� "�������"
                        if (_modbusServices["�������"].CheckTrigger())
                        {
                            var pipe = GetCurrentPipe("�������");
                            if (pipe != null)
                            {
                                string url = Properties.Settings.Default["Otvorot_DeviceURL4"] as string;
                                bool isPipeGood = await SendGetRequest(url);

                                if (isPipeGood)
                                {
                                    MovePipeToNextQueue(pipe, "�������");
                                }
                                else
                                {
                                    RejectPipe(pipe, "�������");
                                }
                            }
                        }

                        // ������� "����������" ������
                        if (_modbusServices["����������_Good"].CheckTrigger())
                        {
                            // ������ ��� ������� "����������"
                            var pipe = GetCurrentPipe("����������");
                            if (pipe != null)
                            {
                                MovePipeToNextQueue(pipe, "����������");
                            }
                        }
                        // ������� "����������" ����
                        if (_modbusServices["����������_Reject"].CheckTrigger())
                        {
                            // ������ ��� ������� "����������"
                            var pipe = GetCurrentPipe("����������");
                            if (pipe != null)
                            {
                                RejectPipe(pipe, "����������");
                            }
                        }

                        // ������� "����������"
                        if (_modbusServices["����������"].CheckTrigger())
                        {
                            // ������ ��� ������� "����������"
                            var pipe = GetCurrentPipe("����������");
                            if (pipe != null)
                            {
                                //todo: ��� ����� ����������� ������ ��������� ������ �� �����������. ����� ������ ������ �����. ���� ������ �������� ������ � ���� ������.
                            }
                        }

                        if (_modbusServices["�������"].CheckTrigger())
                        {
                            //todo: ��� �� ����� ������ � ����� ������ �������� �����. � ����� ��������� ���� � ���� ������ ������ ��� �� ��������.
                        }

                        await Task.Delay(500); // ����� ����� ���������� �����
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"������ � �������� �����: {ex.Message}");
                        // ��������� ������
                    }

                    LogMessage("�������� ���� ����������.");
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

        // ����� ��� ����������� ����� � ��������� �������
        private void MovePipeToNextQueue(PipeData pipe, string currentSectionName)
        {
            int currentIndex = _sectionQueues.FindIndex(sq => sq.SectionName == currentSectionName);
            if (currentIndex >= 0 && currentIndex < _sectionQueues.Count - 1)
            {
                var nextSection = _sectionQueues[currentIndex + 1];
                nextSection.Pipes.Enqueue(pipe);

                // ��������� ���������
                Invoke(new Action(() => UpdateListBoxes()));
            }
            else if (currentIndex == _sectionQueues.Count - 1)
            {
                // ��������� ���� (�������)
                // ����� ����� ����������� �������������� ������
            }
        }

        // ����� ��� ����������� ����� � ����
        private void RejectPipe(PipeData pipe, string currentSectionName)
        {
            pipe.IsRejected = true;
            pipe.RejectionStage = currentSectionName;
            rejectedPipes.Add(pipe);

            // ��������� ���������
            Invoke(new Action(() => UpdateListBoxes()));
        }

        // ����� ��� �������� GET-������� �� ���������� ������
        private async Task<bool> SendGetRequest(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseData = await response.Content.ReadAsStringAsync();

                    // ������������, ��� ����� �������� "good" ��� "reject"
                    return responseData.Trim().ToLower() == "good";
                }
                catch (Exception ex)
                {
                    LogMessage($"������ ��� �������� GET-�������: {ex.Message}");
                    // ������, ��� ������������ ������ ����� � �����������
                    return false;
                }
            }
        }

        // ����� ��� ��������� ������� ����� �� �������
        private PipeData GetCurrentPipe(string sectionName)
        {
            var currentSection = _sectionQueues.Find(sq => sq.SectionName == sectionName);
            if (currentSection != null && currentSection.Pipes.Count > 0)
            {
                var pipe = currentSection.Pipes.Dequeue(); // ��������� ����� �� �������
                return pipe;
            }
            else
            {
                string message = $"��� ����� � ������� '{sectionName}'. ��������, ������������ ������ ��� �������������� ��������.";
                LogMessage(message);
                return null;
            }
        }


        private void UpdateListBoxes()
        {
            foreach (var section in _sectionQueues)
            {
                // �����������, � ��� ���� �������, �������������� �������� �������� � ListBox
                ListBox listBox = GetListBoxForSection(section.SectionName);
                if (listBox != null)
                {
                    listBox.DataSource = null;
                    listBox.DataSource = new List<PipeData>(section.Pipes);
                    listBox.DisplayMember = "Id"; // ��� ������ �������� ��� �����������
                }
            }

            // ���������� ������ ����������� ����
            listBoxRejectedPipes.DataSource = null;
            listBoxRejectedPipes.DataSource = rejectedPipes;
            listBoxRejectedPipes.DisplayMember = "RejectionStage";
        }

        // ����� ��� ��������� ���������������� ListBox ��� �������
        private ListBox GetListBoxForSection(string sectionName)
        {
            switch (sectionName)
            {
                case "�������":
                    return listBoxSharoshka;
                case "��":
                    return listBoxNK;
                case "�������":
                    return listBoxTokarka;
                case "�������":
                    return listBoxOtvrot;
                case "����������":
                    return listBoxOpressovka;
                case "����������":
                    return listBoxMarkirovka;
                default:
                    return null;
            }
        }

        private void LoadSettings()
        {
            // �������� �������� � �������� ���������� �����
            textBoxServerIP.Text = Properties.Settings.Default.ServerIP;
            textBoxServerPort.Text = Properties.Settings.Default.ServerPort.ToString();

            textBoxNTD1.Text = Properties.Settings.Default["NDT_DeviceURL1"] as string;
            textBoxNTD2.Text = Properties.Settings.Default["NDT_DeviceURL2"] as string;
            textBoxNTD3.Text = Properties.Settings.Default["NDT_DeviceURL3"] as string;
            textBoxNTD4.Text = Properties.Settings.Default["Otvorot_DeviceURL4"] as string;


            // �������� �������� ��� Modbus-��������
            textBoxCreation_IP.Text = Properties.Settings.Default["��������_IP"] as string;
            textBoxCreation_Port.Text = Properties.Settings.Default["��������_Port"].ToString();
            textBoxCreation_Register.Text = Properties.Settings.Default["��������_Register"].ToString();

            textBoxSharoshkaGood_IP.Text = Properties.Settings.Default["�������_Good_IP"] as string;
            textBoxSharoshkaGood_Port.Text = Properties.Settings.Default["�������_Good_Port"].ToString();
            textBoxSharoshkaGood_Register.Text = Properties.Settings.Default["�������_Good_Register"].ToString();

            textBoxSharoshkaReject_IP.Text = Properties.Settings.Default["�������_Reject_IP"] as string;
            textBoxSharoshkaReject_Port.Text = Properties.Settings.Default["�������_Reject_Port"].ToString();
            textBoxSharoshkaReject_Register.Text = Properties.Settings.Default["�������_Reject_Register"].ToString();

            textBox��_IP.Text = Properties.Settings.Default["��_IP"] as string;
            textBox��_Port.Text = Properties.Settings.Default["��_Port"].ToString();
            textBox��_Register.Text = Properties.Settings.Default["��_Register"].ToString();

            textBoxTokarka_IP.Text = Properties.Settings.Default["�������_IP"] as string;
            textBoxTokarka_Port.Text = Properties.Settings.Default["�������_Port"].ToString();
            textBoxTokarka_Register.Text = Properties.Settings.Default["�������_Register"].ToString();

            textBoxOtvorot_IP.Text = Properties.Settings.Default["�������_IP"] as string;
            textBoxOtvorot_Port.Text = Properties.Settings.Default["�������_Port"].ToString();
            textBoxOtvorot_Register.Text = Properties.Settings.Default["�������_Register"].ToString();

            textBoxOpressovkaGood_IP.Text = Properties.Settings.Default["����������_Good_IP"] as string;
            textBoxOpressovkaGood_Port.Text = Properties.Settings.Default["����������_Good_Port"].ToString();
            textBoxOpressovkaGood_Register.Text = Properties.Settings.Default["����������_Good_Register"].ToString();

            textBoxOpressovkaReject_IP.Text = Properties.Settings.Default["����������_Reject_IP"] as string;
            textBoxOpressovkaReject_Port.Text = Properties.Settings.Default["����������_Reject_Port"].ToString();
            textBoxOpressovkaReject_Register.Text = Properties.Settings.Default["����������_Reject_Register"].ToString();

            textBoxMarkirovka_IP.Text = Properties.Settings.Default["����������_IP"] as string;
            textBoxMarkirovka_Port.Text = Properties.Settings.Default["����������_Port"].ToString();
            textBoxMarkirovka_Register.Text = Properties.Settings.Default["����������_Register"].ToString();

            textBoxKarman_IP.Text = Properties.Settings.Default["�������_IP"] as string;
            textBoxKarman_Port.Text = Properties.Settings.Default["�������_Port"].ToString();
            textBoxKarman_Register.Text = Properties.Settings.Default["�������_Register"].ToString();

            // ��������� ��� ������ ��������
        }

        private void SaveSettings()
        {
            // ���������� �������� �� ��������� ���������� �����
            Properties.Settings.Default.ServerIP = textBoxServerIP.Text;
            Properties.Settings.Default.ServerPort = int.Parse(textBoxServerPort.Text);

            Properties.Settings.Default["NDT_DeviceURL1"] = textBoxNTD1.Text;
            Properties.Settings.Default["NDT_DeviceURL2"] = textBoxNTD2.Text;
            Properties.Settings.Default["NDT_DeviceURL3"] = textBoxNTD3.Text;
            Properties.Settings.Default["Otvorot_DeviceURL4"] = textBoxNTD4.Text;

            // ���������� �������� ��� Modbus-��������
            Properties.Settings.Default["��������_IP"] = textBoxCreation_IP.Text;
            Properties.Settings.Default["��������_Port"] = int.Parse(textBoxCreation_Port.Text);
            Properties.Settings.Default["��������_Register"] = int.Parse(textBoxCreation_Register.Text);

            Properties.Settings.Default["�������_Good_IP"] = textBoxSharoshkaGood_IP.Text;
            Properties.Settings.Default["�������_Good_Port"] = int.Parse(textBoxSharoshkaGood_Port.Text);
            Properties.Settings.Default["�������_Good_Register"] = int.Parse(textBoxSharoshkaGood_Register.Text);

            Properties.Settings.Default["�������_Reject_IP"] = textBoxSharoshkaReject_IP.Text;
            Properties.Settings.Default["�������_Reject_Port"] = int.Parse(textBoxSharoshkaReject_Port.Text);
            Properties.Settings.Default["�������_Reject_Register"] = int.Parse(textBoxSharoshkaReject_Register.Text);

            Properties.Settings.Default["��_IP"] = textBox��_IP.Text;
            Properties.Settings.Default["��_Port"] = int.Parse(textBox��_Port.Text);
            Properties.Settings.Default["��_Register"] = int.Parse(textBox��_Register.Text);

            Properties.Settings.Default["�������_IP"] = textBoxTokarka_IP.Text;
            Properties.Settings.Default["�������_Port"] = int.Parse(textBoxTokarka_Port.Text);
            Properties.Settings.Default["�������_Register"] = int.Parse(textBoxTokarka_Register.Text);

            Properties.Settings.Default["�������_IP"] = textBoxOtvorot_IP.Text;
            Properties.Settings.Default["�������_Port"] = int.Parse(textBoxOtvorot_Port.Text);
            Properties.Settings.Default["�������_Register"] = int.Parse(textBoxOtvorot_Register.Text);

            Properties.Settings.Default["����������_Good_IP"] = textBoxOpressovkaGood_IP.Text;
            Properties.Settings.Default["����������_Good_Port"] = int.Parse(textBoxOpressovkaGood_Port.Text);
            Properties.Settings.Default["����������_Good_Register"] = int.Parse(textBoxOpressovkaGood_Register.Text);

            Properties.Settings.Default["����������_Reject_IP"] = textBoxOpressovkaReject_IP.Text;
            Properties.Settings.Default["����������_Reject_Port"] = int.Parse(textBoxOpressovkaReject_Port.Text);
            Properties.Settings.Default["����������_Reject_Register"] = int.Parse(textBoxOpressovkaReject_Register.Text);

            Properties.Settings.Default["����������_IP"] = textBoxMarkirovka_IP.Text;
            Properties.Settings.Default["����������_Port"] = int.Parse(textBoxMarkirovka_Port.Text);
            Properties.Settings.Default["����������_Register"] = int.Parse(textBoxMarkirovka_Register.Text);

            Properties.Settings.Default["�������_IP"] = textBoxKarman_IP.Text;
            Properties.Settings.Default["�������_Port"] = int.Parse(textBoxKarman_Port.Text);
            Properties.Settings.Default["�������_Register"] = int.Parse(textBoxKarman_Register.Text);

            // ��������� ��� ������ ��������

            // ���������� �������� ��� ����������� ����� ��������
            Properties.Settings.Default.Save();
        }


        private void HttpServerService_MarkingDataReceived(object sender, MarkingData markingData)
        {
            // ��������� ������ �� ������������
            // ���� ����� �� Id � ��������� �� ������
            var pipe = FindPipeById(markingData.PipeId);
            if (pipe != null)
            {
                pipe.MarkingInfo = markingData.Info;

                // ��������� ������ � ����
                SavePipeData(pipe);
            }

            // ��������� ���������
            Invoke(new Action(() => UpdateListBoxes()));
        }

        private PipeData FindPipeById(Guid pipeId)
        {
            // ���� ����� � �������� � � ����������� ������
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
            // ��������� ������ ����� � ���� ������
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
            LogMessage("���������� ��������.");
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            StopMainLoop();
            LogMessage("���������� �����������.");
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
                LogMessage($"����� ��������� � ������� '{sectionName}'.");
                UpdateListBoxes();
            }
            else
            {
                LogMessage($"������: ������� '{sectionName}' �� �������.");
            }
        }
        private void RemoveSelectedPipeFromSection(string sectionName, ListBox listBox)
        {
            var section = _sectionQueues.Find(sq => sq.SectionName == sectionName);
            if (section != null && listBox.SelectedItem is PipeData selectedPipe)
            {
                // ������� ����� ������� ��� ��������� �����
                section.Pipes = new Queue<PipeData>(section.Pipes.Where(p => p.Id != selectedPipe.Id));
                LogMessage($"����� ������� �� ������� '{sectionName}'.");
                UpdateListBoxes();
            }
            else
            {
                LogMessage($"������: �� ������� ����� ��� �������� �� ������� '{sectionName}'.");
            }
        }

        private string GetSectionNameByListBox(ListBox listBox)
        {
            if (listBox == listBoxSharoshka)
                return "�������";
            else if (listBox == listBoxNK)
                return "��";
            else if (listBox == listBoxTokarka)
                return "�������";
            else if (listBox == listBoxOtvrot)
                return "�������";
            else if (listBox == listBoxOpressovka)
                return "����������";
            else if (listBox == listBoxMarkirovka)
                return "����������";
            else if (listBox == listBoxRejectedPipes)
                return "�������";
            else
                return null;
        }

        private void contextMenu_Opening(object sender, CancelEventArgs e)
        {
            var contextMenu = sender as ContextMenuStrip;
            var listBox = contextMenu.SourceControl as ListBox;

            if (listBox == null)
            {
                e.Cancel = true; // ��� ���������� ListBox, �������� ����
                return;
            }

            // �������� ������� ���� ������������ ListBox
            Point mousePos = listBox.PointToClient(Cursor.Position);
            int index = listBox.IndexFromPoint(mousePos);

            if (index != ListBox.NoMatches)
            {
                // ������� ������
                listBox.SelectedIndex = index;
                deletePipeToolStripMenuItem.Enabled = true;
            }
            else
            {
                // ������� �� ������
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
