using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using cowain.FormView;

namespace cowain.Comm
{
    public class SHIJEReader
    {
        private SerialPort serialPort;
        private string _strData = String.Empty;
        public delegate void delFlashCode(string strCode);
        public static event delFlashCode flashUCcode;
        public static CodeReader reader = new CodeReader();
        public static bool hasOpened = false;

        public string StrData
        {
            get
            {
                return _strData;
            }
            set
            {
                _strData = value;
                flashUCcode(_strData);
            }
        }

        public struct CodeReader
        {
            public string type;
            public string QRCode;
            public string communication;
            public string portName;
            public int baudRate;
            public int dataBits;
            public StopBits stopBits;
            public Parity parity;
            public string IP;
            public int Port;
            public string triggerCommand;
            public string releaseCommand;
            public bool enable;
        }

        public void LoadParams()
        {
            reader.type = "视界";
            reader.QRCode = "产品码";
            reader.communication = "网口";
            reader.portName = "COM1";
            reader.baudRate = 9600;
            reader.dataBits = 8;
            reader.stopBits = StopBits.One;
            reader.parity = Parity.None;
            reader.IP = "192.168.1.100";
            reader.Port = 9102;
            reader.triggerCommand = "T"; // "0x0A"
            reader.releaseCommand = "R";
            reader.enable = true;
            
            reader = (CodeReader)Designer.json.ReadParams(reader.GetType().Name, reader);
            //json.WriteParams("CodeReader", reader);
        }

        public SHIJEReader()
        {
            LoadParams();
            //if (reader.enable)
            //{
            //    if (reader.communication == "串口")
            //    {
            //        Connect();
            //        if (String.Equals(reader.type, "视界"))
            //            SHIJEInstruction();
            //        else if (String.Equals(reader.type, "新大陆"))
            //            NewLandInstruction();
            //        DisConnect();
            //    }
            //    else if (reader.communication == "网口")
            //    {
                    //FlowWork.NetSocket startTest = new FlowWork.NetSocket();
                    //startTest.Open(reader.IP, reader.Port);
                    //startTest.SendMsg(reader.triggerCommand);
                    //startTest.SendMsg(reader.releaseCommand);
                    //startTest.StopConnect();
                    //startTest = null;
            //    }
            //}
        }

        public void ConfigReader(string portName, int baudRate, int dataBits,
            StopBits stopBits, Parity parity)
        {
            serialPort = new SerialPort();
            serialPort.PortName = portName;
            serialPort.BaudRate = baudRate;
            serialPort.DataBits = dataBits;
            serialPort.StopBits = stopBits;
            serialPort.Parity = parity;
            serialPort.ReadBufferSize = 128;
            serialPort.WriteBufferSize = 64;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            int byteLength = serialPort.BytesToRead;
            byte[] buffer = new byte[byteLength];
            serialPort.Read(buffer, 0, byteLength);
            StrData = Encoding.UTF8.GetString(buffer);
        }

        public void SHIJEInstruction()
        {
            try
            {
                StrData = String.Empty;
                serialPort.Write(reader.triggerCommand);
            }
            catch { }
        }

        public void NewLandInstruction()
        {
            try
            {
                StrData = String.Empty;
                byte[] bytes = new byte[] { Convert.ToByte(reader.triggerCommand, 16) };
                serialPort.Write(bytes, 0, bytes.Length);
            }
            catch { }
        }

        public bool Connect()
        {
            try
            {
                ConfigReader(
                    reader.portName,
                    reader.baudRate,
                    reader.dataBits,
                    reader.stopBits,
                    reader.parity);
                serialPort.Open();
                hasOpened = true;
                return true;
            }
            catch
            {
                hasOpened = false;
                return false;
            }
        }

        public void DisConnect()
        {
            try
            {
                serialPort.Close();
                hasOpened = false;
            }
            catch { hasOpened = false; };
        }
    }
}
