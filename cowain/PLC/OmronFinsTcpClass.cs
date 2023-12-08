using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Profinet.Omron;
using HslCommunication.Core;

namespace cowain.Comm
{
    class OmronFinsTcpClass : IPLC
    {
        private OmronFinsNet omronFinsNet;
        public bool connected { get; set; }

        public void Connect() { }

        public bool Connect(string ip, int port, string format)
        {
            //using (PLCForm frm = new PLCForm())
            //{
            //    connected = ConnectServer(ip, port,
            //        Convert.ToByte(frm.tbSA1.Text),
            //        Convert.ToByte(frm.tbDA1.Text),
            //        Convert.ToByte(frm.tbDA2.Text),
            //        (DataFormat)Enum.Parse(typeof(DataFormat), format),
            //        Convert.ToInt32(frm.tbTimeOut.Text));
            //}
            return connected;
        }

        /// <summary>
        /// 连接PLC
        ///  连接成功返回true
        /// </summary>
        /// <param name="ip">PLC的IP地址</param>
        /// <param name="port">PLC的端口</param>
        /// <param name="SA1">PC网络号，PC的IP地址的最后一个数</param>
        /// <param name="DA1">PLC网络号，PLC的IP地址的最后一个数</param>
        /// <param name="DA2">PLC单元号，通常为0</param>
        /// <param name="dataFormat">字节转换的数据格式</param>
        /// <param name="connectTimeOut">连接超时时间</param>
        public bool ConnectServer(string ip, int port, byte SA1, byte DA1, byte DA2, DataFormat dataFormat, int connectTimeOut)
        {
            try
            {
                omronFinsNet = new OmronFinsNet();
                omronFinsNet.ConnectTimeOut = connectTimeOut;
                omronFinsNet.IpAddress = ip;
                omronFinsNet.Port = port;
                omronFinsNet.SA1 = SA1;
                omronFinsNet.DA1 = DA1;
                omronFinsNet.DA2 = DA2;
                omronFinsNet.ByteTransform.DataFormat = dataFormat;
                OperateResult connect = omronFinsNet.ConnectServer();
                if (connect.IsSuccess)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 断开PLC
        /// </summary>
        public void Close()
        {
            try
            {
                omronFinsNet.ConnectClose();
                connected = false;
            }
            catch { }
        }

        /// <summary>
        /// bool读取
        ///  读取成功返回True或False
        ///  读取失败返回Fail
        /// </summary>
        /// <param name="address">PLC的寄存器地址</param>
        public string ReadBool(string address)
        {
            try
            {
                OperateResult<bool> result = omronFinsNet.ReadBool(address);
                if (result.IsSuccess)
                {
                    return result.Content.ToString();
                }
                else
                {
                    return "Fail";
                }
            }
            catch { return "Fail"; }
        }
        /// <summary>
        /// bool批量读取
        ///  数值首个元素代表读取是否成功
        /// </summary>
        /// <param name="address">PLC的寄存器首地址</param>
        /// <param name="length">读取个数</param>
        public string ReadBool(string address, ushort length)
        {
            OperateResult<bool[]> result = omronFinsNet.ReadBool(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }
        /// <summary>
        /// short读取
        ///  读取成功返回寄存器数值
        ///  读取失败返回Fail
        /// </summary>
        /// <param name="address">PLC的寄存器地址</param>
        public string ReadShort(string address)
        {
            try
            {
                OperateResult<short> result = omronFinsNet.ReadInt16(address);
                if (result.IsSuccess)
                {
                    return result.Content.ToString();
                }
                else
                {
                    return "Fail";
                }
            }
            catch { return "Fail"; }
        }

        public string ReadShort(string address, ushort length)
        {
            OperateResult<short[]> result = omronFinsNet.ReadInt16(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        /// <summary>
        /// int32读取
        ///  读取成功返回寄存器数值
        ///  读取失败返回Fail
        /// </summary>
        /// <param name="address">PLC的寄存器地址</param>
        public string ReadInt32(string address)
        {
            try
            {
                OperateResult<int> result = omronFinsNet.ReadInt32(address);
                if (result.IsSuccess)
                {
                    return result.Content.ToString();
                }
                else
                {
                    return "Fail";
                }
            }
            catch { return "Fail"; }
        }

        public string ReadInt32(string address, ushort length)
        {
            OperateResult<int[]> result = omronFinsNet.ReadInt32(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        /// <summary>
        /// long读取
        ///  读取成功返回寄存器数值
        ///  读取失败返回Fail
        /// </summary>
        /// <param name="address">PLC的寄存器地址</param>
        public string ReadLong(string address)
        {
            try
            {
                OperateResult<long> result = omronFinsNet.ReadInt64(address);
                if (result.IsSuccess)
                {
                    return result.Content.ToString();
                }
                else
                {
                    return "Fail";
                }
            }
            catch { return "Fail"; }
        }

        public string ReadLong(string address, ushort length)
        {
            OperateResult<long[]> result = omronFinsNet.ReadInt64(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        /// <summary>
        /// Float读取
        ///  读取成功返回寄存器数值
        ///  读取失败返回Fail
        /// </summary>
        /// <param name="address">PLC的寄存器地址</param>
        public string ReadFloat(string address)
        {
            try
            {
                OperateResult<float> result = omronFinsNet.ReadFloat(address);
                if (result.IsSuccess)
                {
                    return result.Content.ToString();
                }
                else
                {
                    return "Fail";
                }
            }
            catch { return "Fail"; }
        }

        public string ReadFloat(string address, ushort length)
        {
            OperateResult<float[]> result = omronFinsNet.ReadFloat(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        /// <summary>
        /// Double读取
        ///  读取成功返回寄存器数值
        ///  读取失败返回Fail
        /// </summary>
        /// <param name="address">PLC的寄存器地址</param>
        public string ReadDouble(string address)
        {
            try
            {
                OperateResult<double> result = omronFinsNet.ReadDouble(address);
                if (result.IsSuccess)
                {
                    return result.Content.ToString();
                }
                else
                {
                    return "Fail";
                }
            }
            catch { return "Fail"; }
        }

        public string ReadDouble(string address, ushort length)
        {
            OperateResult<double[]> result = omronFinsNet.ReadDouble(address, length);
            return result.IsSuccess ? String.Join(",", result.Content) : "Fail";
        }

        /// <summary>
        /// string读取
        ///  读取成功返回寄存器数值
        ///  读取失败返回Fail
        /// </summary>
        /// <param name="address">PLC的寄存器地址</param>
        /// <param name="length">字符串长度</param>
        public string ReadString(string address, ushort length)
        {
            try
            {
                OperateResult<string> result = omronFinsNet.ReadString(address, length);
                if (result.IsSuccess)
                {
                    return result.Content.ToString();
                }
                else
                {
                    return "Fail";
                }
            }
            catch { return "Fail"; }
        }

        public bool Write(string address, object value)
        {
            bool bResult = false;
            Type type = value.GetType();

            if (value is Boolean)
            {
                bResult = omronFinsNet.Write(address, (Boolean)value).IsSuccess;
            }
            else if (value is Boolean[])
            {
                bResult = omronFinsNet.Write(address, (Boolean[])value).IsSuccess;
            }
            else if (value is Int16)
            {
                bResult = omronFinsNet.Write(address, (Int16)value).IsSuccess;
            }
            else if (value is Int16[])
            {
                bResult = omronFinsNet.Write(address, (Int16[])value).IsSuccess;
            }
            else if (value is Int32)
            {
                bResult = omronFinsNet.Write(address, (Int32)value).IsSuccess;
            }
            else if (value is Int32[])
            {
                bResult = omronFinsNet.Write(address, (Int32[])value).IsSuccess;
            }
            else if (value is Int64)
            {
                bResult = omronFinsNet.Write(address, (Int64)value).IsSuccess;
            }
            else if (value is Int64[])
            {
                bResult = omronFinsNet.Write(address, (Int64[])value).IsSuccess;
            }
            else if (value is Single)
            {
                bResult = omronFinsNet.Write(address, (Single)value).IsSuccess;
            }
            else if (value is Single[])
            {
                bResult = omronFinsNet.Write(address, (Single[])value).IsSuccess;
            }
            else if (value is Double)
            {
                bResult = omronFinsNet.Write(address, (Double)value).IsSuccess;
            }
            else if (value is Double[])
            {
                bResult = omronFinsNet.Write(address, (Double[])value).IsSuccess;
            }
            else if (value is String)
            {
                bResult = omronFinsNet.Write(address, (String)value).IsSuccess;
            }
            return bResult;
        }

    }
}
