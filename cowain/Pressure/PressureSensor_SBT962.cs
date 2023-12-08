using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cowain
{
  public  class PressureSensor_SBT962
    {
        /// <summary> 
        /// 消息委托
        /// </summary> 
        public delegate void MsgDelegate(string msg);
        /// <summary>
        /// 消息委托
        /// </summary>
        public event MsgDelegate ShowMsgEvent;
        /// <summary> 
        /// 串口通讯
        /// </summary> 
        SerialPortClass serial = new SerialPortClass();
        /// <summary> 
        /// 串口号
        /// </summary> 
        string portName = "";
        /// <summary> 
        /// 波特率
        /// </summary> 
        int baudRate = 115200;
        /// <summary> 
        /// 奇偶校验
        /// </summary> 
        Parity parity = Parity.None;
        /// <summary> 
        /// 停止位
        /// </summary> 
        StopBits stopBits = StopBits.One;
        /// <summary> 
        /// 数据位
        /// </summary> 
        int dataBits = 8;
        /// <summary> 
        ///
        /// </summary> 
        /// <param name="com">串口号</param>
        public PressureSensor_SBT962(string com)
        {
            try
            {
                portName = com;
                serial.ReceiveEvent += serialPortReceived;
                if (!serial.Open(portName, baudRate, parity, stopBits, dataBits))
                {
                    MessageBox.Show("打开串口失败！-" + portName);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        /// <summary>
        /// lock
        /// </summary>
        object lockObj = new object();
        /// <summary>
        /// 串口返回值
        /// </summary>
        string getstr = "";
        string getstr_temp = "";
        /// <summary>
        /// 等待_串口返回值
        /// </summary>
        EventWaitHandle _waitHandle = new AutoResetEvent(false);
        /// <summary>
        /// 串口数据接收处理
        /// </summary>
        void serialPortReceived(byte[] msg, string comName)
        {
            try
            {
                if (getstr_temp.Length < 10000)
                {
                    getstr_temp += System.Text.Encoding.Default.GetString(msg);
                }
                if (msg[msg.Count() - 1] == 0X0A || msg[msg.Count() - 1] == 0X0D)
                {
                    string[] Data = Regex.Split(getstr_temp, "\r\n", RegexOptions.IgnoreCase);
                    getstr = Data[Data.Count() - 2];
                    _waitHandle.Set();
                    getstr_temp = "";
                    return;
                }
            }
            catch (Exception ex) { ShowMsgEvent(ex.ToString()); }
        }
        /// <summary>
        /// 读取力值
        /// </summary>
        /// <param name="channel">通道号</param>
        public string RDGROSS(int channel)
        {
            try
            {
                lock (lockObj)
                {
                    _waitHandle.Reset();
                    serial.SendStringData("001RDGROSS=" + channel.ToString());
                    if (_waitHandle.WaitOne(1000))
                    {
                        string[] Data = getstr.Replace("\r\n", "").Replace("\n\r", "").Split(new char[] { ',' });
                        if (Data.Count() >= 2)
                        {
                            if (Data[1].Replace("001GS=", "") == channel.ToString())
                            {
                                return Data[1];
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { ShowMsgEvent(ex.ToString()); }
            return null;
        }
        /// <summary>
        /// 读取力值
        /// </summary>
        public string RDGROSS()
        {
            try
            {
                string[] Data = getstr.Replace("\r\n", "").Replace("\n\r", "").Replace("0-", "-").Replace("0+", "").Split(new char[] { ',' });
                if (Data.Count() >= 1)
                {
                    return Data[0];
                }
            }
            catch (Exception ex) { ShowMsgEvent(ex.ToString()); }
            return null;
        }
    }
}
