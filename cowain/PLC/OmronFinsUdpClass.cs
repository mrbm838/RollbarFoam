using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication.Profinet.Omron;
using HslCommunication;
using HslCommunication.Core;

namespace cowain.Comm
{
    internal class OmronFinsUdpClass : IPLC
    {
        private OmronFinsUdp omronFinsUdp;

        public bool connected { get; set; } = false;

        public void Close() { }

        public void Connect() { }

        public bool Connect(string ip, int port, string format)
        {
            //using (PLCForm frm = new PLCForm())
            //{
                omronFinsUdp = new OmronFinsUdp(ip, port);
                //omronFinsUdp.ByteTransform.DataFormat = (DataFormat)Enum.Parse(typeof(DataFormat), format);
                //omronFinsUdp.ReceiveTimeout = Convert.ToInt32(frm.tbTimeOut.Text);
                connected = omronFinsUdp.ReadInt16("D3700").IsSuccess;
            //}

            // Here using default default parameter settings!!!
            //connected = ConnectServer(ip, port,
            //    Convert.ToByte(PLCForm.plcForm.tbSA1.Text),
            //    Convert.ToByte(PLCForm.plcForm.tbDA1.Text),
            //    Convert.ToByte(PLCForm.plcForm.tbDA2.Text),
            //    (DataFormat)Enum.Parse(typeof(DataFormat), format),
            //    Convert.ToInt32(PLCForm.plcForm.tbTimeOut.Text),
            //    PLCForm.plcForm.tbAddr.Text);

            return connected;
        }

        public bool ConnectServer(string ip, int port, byte SA1, byte DA1, byte DA2, DataFormat dataFormat, int timeOut, string address)
        {
            try
            {
                omronFinsUdp = new OmronFinsUdp();
                omronFinsUdp.ReceiveTimeout = timeOut;
                omronFinsUdp.IpAddress = ip;
                omronFinsUdp.Port = port;
                omronFinsUdp.SA1 = SA1;
                omronFinsUdp.DA1 = DA1;
                omronFinsUdp.DA2 = DA2;
                omronFinsUdp.ByteTransform.DataFormat = dataFormat;
                OperateResult connect = omronFinsUdp.ReadInt16(address);
                return connect.IsSuccess;
            }
            catch { return false; }
        }

        public string ReadBool(string address)
        {
            OperateResult<bool> result = omronFinsUdp.ReadBool(address);
            return result.IsSuccess ? result.Content.ToString() : "Fail";
        }

        public string ReadBool(string address, ushort length)
        {
            OperateResult<bool[]> result = omronFinsUdp.ReadBool(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        public string ReadShort(string address)
        {
            OperateResult<short> result = omronFinsUdp.ReadInt16(address);
            return result.IsSuccess ? result.Content.ToString() : "Fail";
        }

        public string ReadShort(string address, ushort length)
        {
            OperateResult<short[]> result = omronFinsUdp.ReadInt16(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        public string ReadInt32(string address)
        {
            OperateResult<int> result = omronFinsUdp.ReadInt32(address);
            return result.IsSuccess ? result.Content.ToString() : "Fail";
        }

        public string ReadInt32(string address, ushort length)
        {
            OperateResult<int[]> result = omronFinsUdp.ReadInt32(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        public string ReadLong(string address)
        {
            OperateResult<long> result = omronFinsUdp.ReadInt64(address);
            return result.IsSuccess ? result.Content.ToString() : "Fail";
        }

        public string ReadLong(string address, ushort length)
        {
            OperateResult<long[]> result = omronFinsUdp.ReadInt64(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        public string ReadFloat(string address)
        {
            OperateResult<float> result = omronFinsUdp.ReadFloat(address);
            return result.IsSuccess ? result.Content.ToString() : "Fail";
        }

        public string ReadFloat(string address, ushort length)
        {
            OperateResult<float[]> result = omronFinsUdp.ReadFloat(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        public string ReadDouble(string address)
        {
            OperateResult<double> result = omronFinsUdp.ReadDouble(address);
            return result.IsSuccess ? result.Content.ToString() : "Fail";
        }

        public string ReadDouble(string address, ushort length)
        {
            OperateResult<double[]> result = omronFinsUdp.ReadDouble(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        public string ReadString(string address, ushort length)
        {
            OperateResult<string> result = omronFinsUdp.ReadString(address, length);
            return result.IsSuccess ? result.Content : "Fail";
        }

        public bool Write(string address, object value)
        {
            bool bResult = false;
            if (value is Boolean)
            {
                bResult = omronFinsUdp.Write(address, (Boolean)value).IsSuccess;
            }
            else if (value is Boolean[])
            {
                bResult = omronFinsUdp.Write(address, (Boolean[])value).IsSuccess;
            }
            else if (value is Int16)
            {
                bResult = omronFinsUdp.Write(address, (Int16)value).IsSuccess;
            }
            else if (value is Int16[])
            {
                bResult = omronFinsUdp.Write(address, (Int16[])value).IsSuccess;
            }
            else if (value is Int32)
            {
                bResult = omronFinsUdp.Write(address, (Int32)value).IsSuccess;
            }
            else if (value is Int32[])
            {
                bResult = omronFinsUdp.Write(address, (Int32[])value).IsSuccess;
            }
            else if (value is Int64)
            {
                bResult = omronFinsUdp.Write(address, (Int64)value).IsSuccess;
            }
            else if (value is Int64[])
            {
                bResult = omronFinsUdp.Write(address, (Int64[])value).IsSuccess;
            }
            else if (value is Single)
            {
                bResult = omronFinsUdp.Write(address, (Single)value).IsSuccess;
            }
            else if (value is Single[])
            {
                bResult = omronFinsUdp.Write(address, (Single[])value).IsSuccess;
            }
            else if (value is Double)
            {
                bResult = omronFinsUdp.Write(address, (Double)value).IsSuccess;
            }
            else if (value is Double[])
            {
                bResult = omronFinsUdp.Write(address, (Double[])value).IsSuccess;
            }
            else if (value is String)
            {
                bResult = omronFinsUdp.Write(address, (String)value).IsSuccess;
            }
            return bResult;
        }
    }
}
