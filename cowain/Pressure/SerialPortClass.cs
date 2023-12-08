using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace cowain
{
  public class SerialPortClass
    {
        /// <summary> 
        /// 波特率
        /// </summary> 
        int baudRate1 = 115200;
        /// <summary> 
        /// 奇偶校验
        /// </summary> 
        Parity parity1 = Parity.None;
        /// <summary> 
        /// 停止位
        /// </summary> 
        StopBits stopBits1 = StopBits.One;
        /// <summary> 
        /// 数据位
        /// </summary> 
        int dataBits1 = 8;
        /// <summary> 
        /// 串口通讯
        /// </summary> 
        public delegate void SerialPortDelegate(byte[] msgStr, string portName);
        /// <summary>
        /// 接收数据事件
        /// </summary>
        public event SerialPortDelegate ReceiveEvent;
        private string ComName = "";

        SerialPort serialPort;
        Thread th_ReceiveData;
        /// <summary>
        /// 判断串口是否存在
        /// </summary>
        bool isSerialPortName(string com)
        {
            try
            {
                string[] PortNames = System.IO.Ports.SerialPort.GetPortNames();
                int num_ = Array.IndexOf(PortNames, com);
                if (num_ >= 0) { return true; }
            }
            catch { }
            return false;
        }
        /// <summary>
        /// 打开串口
        /// </summary>
        /// <param name="portName">串口号</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">奇偶校验</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="dataBits">数据位</param>
        public bool Open(string portName,int baudRate, Parity parity,StopBits stopBits,int dataBits)
        {
            try
            {
                baudRate1 = baudRate;
                parity1 = parity;
                stopBits1 = stopBits;
                dataBits1 = dataBits;

                try { Close(); } catch { }
                if (isSerialPortName(portName))
                {
                    serialPort = new SerialPort();
                    serialPort.PortName = portName;
                    serialPort.BaudRate = baudRate;
                    serialPort.Parity = parity;
                    serialPort.StopBits = stopBits;
                    serialPort.DataBits = dataBits;
                    serialPort.WriteTimeout = 1000;
                    serialPort.ReadBufferSize = 2000;

                    serialPort.Open();
                    ComName = portName;
                    th_ReceiveData = new Thread(ReceiveData);
                    th_ReceiveData.IsBackground = true;
                    th_ReceiveData.Start();
                    return true;
                }
            }
            catch { }
            try { Close(); } catch { }
            return false;
        }
        /// <summary>
        /// 关闭串口
        /// </summary>
        public void Close()
        {
            try
            {
                //if (isSerialPortName(ComName))
                //{
                serialPort.Close();
                //}
            }
            catch { }

            try
            {
                try { Flag_Receive = false; } catch { }
                try { serialPort.ReadTimeout = 0; } catch { }
                //Thread.Sleep(10);
                try { th_ReceiveData.Abort(); } catch { }
            }
            catch { }
        }

        bool Flag_Receive = false;
        /// <summary>
        /// 接收数据
        /// </summary>
        private void ReceiveData()
        {
            try
            {
                Flag_Receive = true;
                serialPort.ReadTimeout = -1;
                //string getStr = "";
                while (Flag_Receive)
                {
                    try
                    {
                        byte firstByte = Convert.ToByte(serialPort.ReadByte());
                        try
                        {
                            int bytesRead = serialPort.BytesToRead;
                            byte[] bytesData = new byte[bytesRead + 1];
                            bytesData[0] = firstByte;
                            for (int i = 1; i <= bytesRead; i++)
                            {
                                bytesData[i] = Convert.ToByte(serialPort.ReadByte());
                            }

                            //if (getStr.Length < 10000)
                            //{
                            //    getStr += System.Text.Encoding.Default.GetString(bytesData);
                            //}

                            //if (bytesData[bytesRead] == 0X0A)
                            //{
                            //    ReceiveEvent(getStr, ComName);
                            //    getStr = "";
                            //}

                            ReceiveEvent(bytesData, ComName);
                            if (Flag_Receive) { Thread.Sleep(10); }
                        }
                        catch { }
                    }
                    catch
                    {
                        Flag_Receive = false;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 发送字符串数据
        /// </summary>
        public void SendStringData(string str)
        {
            try
            {
                if (isSerialPortName(ComName))
                {
                    serialPort.Write(str);
                    return;
                }
            }
            catch { }
            //try
            //{
            //    if (Open(ComName, baudRate1, parity1, stopBits1,dataBits1))
            //    {
            //        serialPort.Write(str);
            //        return;
            //    }
            //}
         //   catch { }
        }

        /// <summary>
        /// 发送二进制数据
        /// </summary>
        public void SendBytesData(string str)
        {
            try
            {
                if (isSerialPortName(ComName))
                {
                    byte[] bytesSend = System.Text.Encoding.Default.GetBytes(str);
                    serialPort.Write(bytesSend, 0, bytesSend.Length);
                }
            }
            catch { }
        }

        /// <summary>
        /// 发送16进制数据
        /// </summary>
        public void SendHexBytesData(string str)
        {
            try
            {
                if (isSerialPortName(ComName))
                {
                    byte[] bytesSend = strToToHexByte(str);
                    serialPort.WriteTimeout = 1000;
                    serialPort.Write(bytesSend, 0, bytesSend.Length);
                    return;
                }
            }
            catch { }
            //try
            //{
            //    if (Open(ComName, baudRate1, parity1, stopBits1, dataBits1))
            //    {
            //        byte[] bytesSend = strToToHexByte(str);
            //        serialPort.WriteTimeout = 1000;
            //        serialPort.Write(bytesSend, 0, bytesSend.Length);
            //        return;
            //    }
            //}
            //catch { }
        }

        /// <summary> 
        /// 字符串转16进制字节数组 
        /// </summary> 
        /// <param name="hexString"></param> 
        /// <returns></returns> 
        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if (hexString.Substring(0, 2) == "0x" || hexString.Substring(0, 2) == "0X")
                hexString = hexString.Substring(2, hexString.Length - 2);
            byte[] returnBytes = new byte[hexString.Length / 2];
            try
            {
                for (int i = 0; i < returnBytes.Length; i++)
                    returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                //returnBytes[returnBytes.Length - i - 1] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            catch
            {
                return null;
            }
            return returnBytes;
        }

        public string[] check()
        {
           return System.IO.Ports.SerialPort.GetPortNames();
        }
    }
}
