using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication.Core;
using cowain.FormView;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace cowain.Comm
{
    public enum enMachine
    {
        M_046,
        M_047
    }

    public enum enOperate
    {
        Read,
        Write,
    }

    class PLCAddress
    {
        public enMachine machine = enMachine.M_046;
        public string address = string.Empty;
        public OmronPLC.enType type = OmronPLC.enType.Short;
        public string lengthOrData = string.Empty;
        public enOperate readOrWrite = enOperate.Read;
        public PLCAddress(enMachine machine, string address, OmronPLC.enType type, string lengthOrData, enOperate readOrWrite)
        {
            this.machine = machine;
            this.address = address;
            this.type = type;
            this.lengthOrData = lengthOrData;
            this.readOrWrite = readOrWrite;
        }
    }

    internal class OmronPLC
    {
        public static PLCParams plc = new PLCParams();
        public static bool connected = false;
        public static bool connectedOther = false;
        IPLC HSL_plc = null;
        private enMachine CurMachine = enMachine.M_046;
        private static bool bLoaded = false;

        public delegate void DelFlash(enMachine machine, string result);
        public static DelFlash flashRunner;
        public static string[] enAlarm046;
        public static string[] enAlarm047;

        public OmronPLC(enMachine machineNo)
        {
            string curDirectory = Directory.GetCurrentDirectory();
            string file046 = curDirectory.Replace("cowain\\bin\\Debug", "ErrorCode046.txt");
            enAlarm046 = File.ReadAllLines(file046);
            string file047 = curDirectory.Replace("cowain\\bin\\Debug", "ErrorCode047.txt");
            enAlarm047 = File.ReadAllLines(file047);
            CurMachine = machineNo;
            if (!bLoaded) LoadParams();
            bLoaded = true;
            if (String.Equals(plc.procotol, "OmronFinsTCP"))
            {
                HSL_plc = new OmronFinsTcpClass();
                HSL_plc = HSL_plc as OmronFinsTcpClass;
            }
            else if (String.Equals(plc.procotol, "OmronFinsUDP"))
            {
                HSL_plc = new OmronFinsUdpClass();
                HSL_plc = HSL_plc as OmronFinsUdpClass;
            }

            if (CurMachine == enMachine.M_046)
            {
                connected = HSL_plc.Connect(plc.IP, plc.Port, string.Empty);
                Thread thReadCycle = new Thread(ReadCycle);
                thReadCycle.IsBackground = true;
                thReadCycle.Start();
            }

            if (CurMachine == enMachine.M_047)
            {
                connectedOther = HSL_plc.Connect(plc.IPOther, plc.PortOther, string.Empty);
                Thread thReadCycleOther = new Thread(ReadCycleOther);
                thReadCycleOther.IsBackground = true;
                thReadCycleOther.Start();
            }
        }

        public enum enType
        {
            Bool,
            Short,
            Int32,
            Long,
            Float,
            Double,
            String,
        }

        public struct PLCParams
        {
            public string procotol;
            public string IP;
            public int Port;
            public string IPOther;
            public int PortOther;
            public byte SA1;                 //PC网络号
            public byte DA1, DA2;            //PLC网络号,PLC单元号
            public DataFormat dataFormat;
            public int connectTimeOut;
            public PLCAddress[] plcAddresses;
        }

        public void LoadParams()
        {
            plc.procotol = "OmronFinsUDP";
            plc.IP = "192.168.250.10";
            plc.Port = 9600;
            plc.IPOther = "192.168.250.20";
            plc.PortOther = 9600;
            plc.SA1 = 192;
            plc.DA1 = 33;
            plc.DA2 = 0;
            plc.dataFormat = DataFormat.CDAB;
            plc.connectTimeOut = 2000;
            plc.plcAddresses = new PLCAddress[40];
            plc.plcAddresses[0] = new PLCAddress(enMachine.M_046, "D3700", enType.Short, "1", enOperate.Read);   //读取 载具到位
            plc.plcAddresses[1] = new PLCAddress(enMachine.M_046, "D3712", enType.Short, "1", enOperate.Write);  //写入 读码完成
            plc.plcAddresses[2] = new PLCAddress(enMachine.M_046, "D3990", enType.Short, "1", enOperate.Write);  //写入 用户登录
            plc.plcAddresses[3] = new PLCAddress(enMachine.M_046, "D4000", enType.Short, "1", enOperate.Read);   //读取 作料完成
            plc.plcAddresses[4] = new PLCAddress(enMachine.M_046, "D4100", enType.Short, "1", enOperate.Write);  //写入 MES报警

            plc.plcAddresses[5] = new PLCAddress(enMachine.M_046, "D3900", enType.Short, "1", enOperate.Read);   //读取 压力1
            plc.plcAddresses[6] = new PLCAddress(enMachine.M_046, "D3904", enType.Short, "1", enOperate.Read);   //读取 压力2
            plc.plcAddresses[7] = new PLCAddress(enMachine.M_046, "D3908", enType.Short, "1", enOperate.Read);   //读取 压力3
            plc.plcAddresses[8] = new PLCAddress(enMachine.M_046, "D4000.00", enType.Bool, "1", enOperate.Read);   //读取 触发压力1
            plc.plcAddresses[9] = new PLCAddress(enMachine.M_046, "D4000.01", enType.Bool, "1", enOperate.Read);   //读取 触发压力2
            plc.plcAddresses[10] = new PLCAddress(enMachine.M_046, "D4000.02", enType.Bool, "1", enOperate.Read);  //读取 触发压力3

            plc.plcAddresses[11] = new PLCAddress(enMachine.M_047, "D3904", enType.Short, "1", enOperate.Read);  //读取 压力4
            plc.plcAddresses[12] = new PLCAddress(enMachine.M_047, "D3908", enType.Short, "1", enOperate.Read);  //读取 压力5
            plc.plcAddresses[13] = new PLCAddress(enMachine.M_047, "D4000.01", enType.Bool, "1", enOperate.Read);  //读取 触发压力4
            plc.plcAddresses[14] = new PLCAddress(enMachine.M_047, "D4000.02", enType.Bool, "1", enOperate.Read);  //读取 触发压力5

            plc.plcAddresses[15] = new PLCAddress(enMachine.M_046, "D4000.03", enType.Bool, "1", enOperate.Read);  //读取 工位2到位
            plc.plcAddresses[16] = new PLCAddress(enMachine.M_047, "D4000.03", enType.Bool, "1", enOperate.Read);  //读取 工位3到位

            plc.plcAddresses[17] = new PLCAddress(enMachine.M_046, "D3712", enType.Short, "2", enOperate.Write); //写入 读码失败
            plc.plcAddresses[18] = new PLCAddress(enMachine.M_046, "D4002.01", enType.Bool, "True", enOperate.Write); //写入 成功读取压力2
            plc.plcAddresses[19] = new PLCAddress(enMachine.M_046, "D4002.02", enType.Bool, "True", enOperate.Write); //写入 成功读取压力3
            plc.plcAddresses[20] = new PLCAddress(enMachine.M_047, "D4002.01", enType.Bool, "True", enOperate.Write); //写入 成功读取压力4
            plc.plcAddresses[21] = new PLCAddress(enMachine.M_047, "D4002.02", enType.Bool, "True", enOperate.Write); //写入 成功读取压力5

            plc.plcAddresses[22] = new PLCAddress(enMachine.M_046, "D4002.03", enType.Bool, "True", enOperate.Write);  //写入 成功读取相机2反馈
            plc.plcAddresses[23] = new PLCAddress(enMachine.M_047, "D4002.03", enType.Bool, "True", enOperate.Write);  //写入 成功读取相机3反馈
            plc.plcAddresses[24] = new PLCAddress(enMachine.M_046, "D4002.00", enType.Bool, "True", enOperate.Write); //写入 成功读取压力1

            plc.plcAddresses[25] = new PLCAddress(enMachine.M_046, "D3800.00", enType.Bool, enAlarm046.Length.ToString(), enOperate.Read);  //读取 046报警信号
            plc.plcAddresses[26] = new PLCAddress(enMachine.M_047, "D3800.00", enType.Bool, enAlarm047.Length.ToString(), enOperate.Read);  //读取 047报警信号

            plc.plcAddresses[27] = new PLCAddress(enMachine.M_046, "D3708", enType.Short, "1", enOperate.Write);  //写入 通讯信号
            plc.plcAddresses[28] = new PLCAddress(enMachine.M_047, "D3708", enType.Short, "1", enOperate.Write);  //写入 通讯信号

            plc.plcAddresses[29] = new PLCAddress(enMachine.M_046, "D4000.04", enType.Bool, "1", enOperate.Read);  //读取 046屏蔽信号
            plc.plcAddresses[30] = new PLCAddress(enMachine.M_047, "D4000.04", enType.Bool, "1", enOperate.Read);  //读取 047屏蔽信号

            plc.plcAddresses[31] = new PLCAddress(enMachine.M_046, "D4000.11", enType.Bool, "1", enOperate.Read);  //读取 保压1到位信号
            plc.plcAddresses[32] = new PLCAddress(enMachine.M_047, "D4000.11", enType.Bool, "1", enOperate.Read);  //读取 保压2到位信号

            plc.plcAddresses[33] = new PLCAddress(enMachine.M_046, "D3710", enType.Short, "0", enOperate.Write);  //写入 046是否保压信号
            plc.plcAddresses[34] = new PLCAddress(enMachine.M_047, "D3710", enType.Short, "0", enOperate.Write);  //写入 047是否保压信号

            plc.plcAddresses[35] = new PLCAddress(enMachine.M_046, "D4000.05", enType.Bool, "6", enOperate.Read);  //读取 046流道信号
            plc.plcAddresses[36] = new PLCAddress(enMachine.M_047, "D4000.05", enType.Bool, "6", enOperate.Read);  //读取 047流道信号

            plc = (PLCParams)Designer.json.ReadParams(plc.GetType().Name, plc);

            FindWriteAddress();
            FindReadAddress();                          //将要读的地址存入集合去循环读
        }

        private void FindWriteAddress()
        {
            foreach (PLCAddress item in plc.plcAddresses)
            {
                if (item != null && item.address == "D3710" && item.readOrWrite == enOperate.Write)
                {
                    //PLCAddress TempHP = new PLCAddress(item.machine, item.address, item.type, "0", item.readOrWrite);
                    //item.lengthOrData = "0";
                    if (item.machine == enMachine.M_046)
                        PLCQueueClass.AddToQueue(item);
                    else if (item.machine == enMachine.M_047)
                        PLCQueueClass.AddToQueueOther(item);
                    continue;
                }
                if (item != null && item.address != "D3800" && item.address != "D3990" && item.address != "D4100" && item.address != "D3708")
                {
                    if (item.readOrWrite == enOperate.Write)
                    {
                        PLCAddress Temp = new PLCAddress(item.machine, item.address, item.type, "False", item.readOrWrite);
                        if (item.machine == enMachine.M_046)
                            PLCQueueClass.AddToQueue(Temp);
                        else if (item.machine == enMachine.M_047)
                            PLCQueueClass.AddToQueueOther(Temp);
                    }
                }
            }
        }

        private void FindReadAddress()
        {
            foreach (PLCAddress item in plc.plcAddresses)
                if (item != null)
                    if (item.readOrWrite == enOperate.Read)
                    {
                        if (item.machine == enMachine.M_046)
                            PLCQueueClass.listRead.Add(item);
                        else if (item.machine == enMachine.M_047)
                            PLCQueueClass.listReadOther.Add(item);
                    }
        }

        private void ReadCycle()
        {
            while (true)
            {
                Thread.Sleep(1);
                string result = string.Empty;
                Stopwatch st = new Stopwatch();
                st.Start();
                foreach (PLCAddress item in PLCQueueClass.listRead)
                {
                    PLCAddress plcAddress = PLCQueueClass.RemoveFromQueue();
                    if (plcAddress != null)
                    {
                        result = WriteSignals(plcAddress.address, plcAddress.type, plcAddress.lengthOrData) ? "Success" : "Fail";
                        PLCQueueClass.AddWriteResultToList(plcAddress.address, result);
                    }
                    result = ReadSignals(item.address, item.type, item.lengthOrData);
                    if (item.address == "D4000.05" && flashRunner != null)
                    {
                        flashRunner(item.machine, result);
                    }
                PLCQueueClass.AddResultToList(item.address, result);
                }
                PLCQueueClass.AddToQueue(plc.plcAddresses[27]);
                st.Stop();
                TimeSpan ts = st.Elapsed;
            }
        }

        private void ReadCycleOther()
        {
            while (true)
            {
                Thread.Sleep(1);
                string result = string.Empty;
                Stopwatch st = new Stopwatch();
                st.Start();
                foreach (PLCAddress item in PLCQueueClass.listReadOther)
                {
                    PLCAddress plcAddress = PLCQueueClass.RemoveFromQueueOther();
                    if (plcAddress != null)
                    {
                        result = WriteSignals(plcAddress.address, plcAddress.type, plcAddress.lengthOrData) ? "Success" : "Fail";
                        PLCQueueClass.AddWriteResultToListOther(plcAddress.address, result);
                    }
                    result = ReadSignals(item.address, item.type, item.lengthOrData);
                    if (item.address == "D4000.05" && flashRunner != null)
                    {
                        flashRunner(item.machine, result);
                    }
                    PLCQueueClass.AddResultToListOther(item.address, result);
                }
                PLCQueueClass.AddToQueueOther(plc.plcAddresses[28]);
                st.Stop();
                TimeSpan ts = st.Elapsed;
            }
        }

        //private string Read()
        //    =>
        //    plc.dataType switch
        //    {
        //        enType.Bool => strings.Length == 1 ? HSL_plc.ReadBool(plc.Address) : HSL_plc.ReadBool(plc.Address, strings.Length),
        //        enType.Short => strings.Length == 1 ? HSL_plc.ReadShort(plc.Address) : HSL_plc.ReadShort(plc.Address, strings.Length),
        //        enType.Int32 => throw new NotImplementedException(),
        //        enType.Long => throw new NotImplementedException(),
        //        enType.Float => throw new NotImplementedException(),
        //        enType.Double => throw new NotImplementedException(),
        //        enType.String => throw new NotImplementedException(),
        //        _ => throw new NotImplementedException()
        //    };

        public string ReadSignals(string address, enType dataType, string strLength)
        {
            string back = "Fail";
            try
            {
                UInt16 length = UInt16.Parse(strLength);
                switch (dataType)
                {
                    case enType.Bool:
                        back = length == 1 ? HSL_plc.ReadBool(address) : HSL_plc.ReadBool(address, length);
                        break;
                    case enType.Short:
                        back = length == 1 ? HSL_plc.ReadShort(address) : HSL_plc.ReadShort(address, length);
                        break;
                    case enType.Int32:
                        back = length == 1 ? HSL_plc.ReadInt32(address) : HSL_plc.ReadInt32(address, length);
                        break;
                    case enType.Long:
                        back = length == 1 ? HSL_plc.ReadLong(address) : HSL_plc.ReadLong(address, length);
                        break;
                    case enType.Float:
                        back = length == 1 ? HSL_plc.ReadFloat(address) : HSL_plc.ReadFloat(address, length);
                        break;
                    case enType.Double:
                        back = length == 1 ? HSL_plc.ReadDouble(address) : HSL_plc.ReadDouble(address, length);
                        break;
                    case enType.String:
                        back = HSL_plc.ReadString(address, length);
                        break;
                }
            }
            catch { back = "Fail"; }
            return back;
        }

        public bool WriteSignals(string address, enType dataType, string signals)
        {
            string[] strings = signals.Split(new string[] { "，", ",", " ", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> list = strings.ToList<string>();
            bool flag = false;
            try
            {
                switch (dataType)
                {
                    case enType.Bool:
                        bool[] bools = list.ConvertAll<bool>(t => Convert.ToBoolean(t)).ToArray<bool>();
                        flag = strings.Length == 1 ? HSL_plc.Write(address, bools.First()) : HSL_plc.Write(address, bools);
                        break;
                    case enType.Short:
                        short[] shorts = list.ConvertAll<short>(t => Convert.ToInt16(t)).ToArray<short>();
                        flag = strings.Length == 1 ? HSL_plc.Write(address, shorts.First()) : HSL_plc.Write(address, shorts);
                        break;
                    case enType.Int32:
                        int[] ints = list.ConvertAll<int>(t => Convert.ToInt32(t)).ToArray<int>();
                        flag = strings.Length == 1 ? HSL_plc.Write(address, ints.First()) : HSL_plc.Write(address, ints);
                        break;
                    case enType.Long:
                        long[] longs = list.ConvertAll<long>(t => Convert.ToInt64(t)).ToArray<long>();
                        flag = strings.Length == 1 ? HSL_plc.Write(address, longs.First()) : HSL_plc.Write(address, longs);
                        break;
                    case enType.Float:
                        float[] floats = list.ConvertAll<float>(t => Convert.ToSingle(t)).ToArray<float>();
                        flag = strings.Length == 1 ? HSL_plc.Write(address, floats.First()) : HSL_plc.Write(address, floats);
                        break;
                    case enType.Double:
                        double[] doubles = list.ConvertAll<double>(t => Convert.ToDouble(t)).ToArray<double>();
                        flag = strings.Length == 1 ? HSL_plc.Write(address, doubles.First()) : HSL_plc.Write(address, doubles);
                        break;
                    case enType.String:
                        flag = HSL_plc.Write(address, strings.First());
                        break;
                }
            }
            catch { flag = false; }
            return flag;
        }
    }
}
