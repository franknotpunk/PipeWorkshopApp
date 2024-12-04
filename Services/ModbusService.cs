using EasyModbus;
using System;
using System.Net;

namespace PipeWorkshopApp.Services
{
    public class ModbusService
    {
        private ModbusClient _modbusClient;
        private int _registerAddress;
        private string ipAddress;
        private int port;
        private int registerAddress;

        public ModbusService(string ipAddress, int port, int registerAddress)
        {
            this.port = port;
            this.ipAddress = ipAddress;
            this.registerAddress = registerAddress;
            _modbusClient = new ModbusClient(ipAddress, port);
            _registerAddress = registerAddress;

            Connect();
        }

        private void Connect()
        {
            try
            {
                if (!_modbusClient.Connected)
                    _modbusClient.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения Modbus: {ex.Message}");
                // Обработка ошибки подключения
            }
        }
        public void Disconnect()
        {
            try
            {
                if (_modbusClient.Connected)
                {
                    _modbusClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отключения Modbus клиента: {ex.Message}");
            }
        }

        public bool CheckTrigger()
        {
            try
            {
                if (!_modbusClient.Connected)
                    _modbusClient.Connect();

                // Чтение значения регистра
                int[] values = _modbusClient.ReadHoldingRegisters(_registerAddress, 1);
                int value = values[0];

                if (value == 1)
                {
                    // Зануляем регистр
                    _modbusClient.WriteSingleRegister(_registerAddress, 0);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка Modbus: {ex.Message} {this.ipAddress}:{this.port} {this.registerAddress}");
                // Обработка ошибок чтения/записи регистра
                return false;
            }
        }
    }
}
