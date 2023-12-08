

using Cowain_AutoDispenser.Flow;
using Cowain_Form.FormView;
using Cowain_Machine.Flow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToolTotal_1;
using static Cowain_Machine.Flow.clsDispenserAuto;

namespace Post
{
    public enum CMDStep
    {
        用户登录,
        上传软件版本,
        获取胶水配置,
        设置胶水信息,
        载具获取SN,
        载具获取多个SN,
        检查过站,
        上传参数信息,
        上传复检结果,
        上传设备BOM,
        检查追溯信息,
        上传PDCA,
        提交过站,
        waitTime,
        CheckPDCAReciveMSG,
        MES获取工位,
        PDCA获取工位,
        清除临时表,
        Datacpmplete,
        MES获取BUILD_EVENT,
        MES获取BUILD_MATRIX_CONFIG,
        MES获取S_BUILD,
        PDCA获取URL,
     
    }

    public class MesData
    {
        /// <summary>
        /// 前后龙门
        /// </summary>
        public int IntStation;
        /// <summary>
        /// 当前步骤
        /// </summary>
        public CMDStep CmdStep;
        /// <summary>
        /// 是否上传成功
        /// </summary>
        public string StrResult;
        /// <summary>
        /// NG时返回的错误信息
        /// </summary>
        public string StrErrMSG = "";
        /// <summary>
        /// 发送的全部信息
        /// </summary>
        public string StrSendMSG = "";
        /// <summary>
        /// 返回的全部信息
        /// </summary>
        public string StrReturnMSG = "";
        /// <summary>
        /// 在右流道使用前龙门点胶时,应该获取index为0 时的前龙门信息，比如胶水，胶阀参数
        /// </summary>
        public int IntIndex = 0;
        /// <summary>
        /// 当一个载具对应多个产品时，要提前上传MES，所以 attr的参数就需要缓存，这个参数用于区分缓存位置
        /// </summary>
        public int IntIndexSN = 0;
        public MesData(int station1, CMDStep cmdStep1, string Result1)
        {
            IntStation = station1;
            CmdStep = cmdStep1;
            StrResult = Result1;
            MSystemDateDefine.GantryParm pGantryParm1 = MSystemDateDefine.SystemParameter.Gantry1Parm;
            if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.JustBackStation)
            {
                if (IntStation == 0)
                {
                    IntIndex = 1;
                }
                else
                {
                    IntIndex = 0;
                }
            }
            else
            {
                IntIndex = station1;
            }
            //单龙门点双流道胶
            enGantryType m_enGantryType = (enGantryType)MSystemDateDefine.SystemParameter.MachineParmeter.GantryType;
            if (m_enGantryType == enGantryType.SigleGantry)
            {
                IntIndex = 0;
            }
        }
    }
    public class GTKPOSTClass
    {
        private static object locker1 = new object();
        private static List<MesData> myListResult = new List<MesData>();
        private CMDStep currentCMDStep;
        /// <summary>
        /// 存储MES的指令
        /// </summary>
        private static Queue<MesData> myQueueCMD = new Queue<MesData>();
        /// <summary>
        /// 存储PDCA的指令
        /// </summary>
        private static Queue<MesData> myQueueCMDForPDCA = new Queue<MesData>();
        /// <summary>
        /// 线程退出
        /// </summary>
        public bool b_Close = false;
        private static GTKPOSTClass instace;
        public string Pdcaresult = "";
        HttpWebRequest Web = null;
        HttpWebResponse Res = null;
        /// <summary>
        ///延时
        /// </summary>
        public System.Timers.Timer POSTm_tmDelay = new System.Timers.Timer();
        Thread threadForMES;
        Thread threadForPDCA;

        private  HttpWebRequest webRequest = null;//关于mes的类
        private HttpWebResponse response = null;//关于mes的类
        //------------------------- 

        //mini参数-----------------------
        public static NetClientPort socket;

        MESGTKData mESGTKData;

        //--------------------------------
        private GTKPOSTClass()
        {
            if (MachineDataDefine.MachineCfgS.MachineFactoryEumn == MachineFactory.歌尔)
            {
                threadForMES = new Thread(CircleForMES);
                threadForMES.IsBackground = true;
                threadForMES.Start();
                threadForPDCA = new Thread(CircleForPDCA);
                threadForPDCA.IsBackground = true;
                threadForPDCA.Start();
                socket = new NetClientPort();
                string ip = MESDataDefine.MESDatas.StrMiniIP;
                string port = MESDataDefine.MESDatas.StrMiniPort;
                Task.Run(() =>
                {
                    socket.Open(ip, Convert.ToInt32(port));
                    socket.receiveDoneSocketEvent += SocketEvent;
                });
                POSTm_tmDelay = new System.Timers.Timer(500);
                POSTm_tmDelay.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent_DelayTimeOut);
              
                 mESGTKData = (MESGTKData)MESDataDefine.MESDatas;
            }
        }
        public void SocketEvent(string msgStr)
        {
            Pdcaresult = msgStr.Trim();
            SaveDateMes("_PDCA反馈信息:" + msgStr + "\r\n");
        }
        public static GTKPOSTClass CreateInstance()
        {
            lock (locker1)
            {
                if (instace == null)
                {
                    instace = new GTKPOSTClass();
                }
                return instace;
            }
        }

        private void OnTimedEvent_DelayTimeOut(object source, System.Timers.ElapsedEventArgs e)
        {
            POSTm_tmDelay.Enabled = false;
        }
        public static void AddCMD(int station, CMDStep cmdStep)
        {
            lock (locker1)
            {
                MesData mesData = new MesData(station, cmdStep, "");
                if (mesData.CmdStep == CMDStep.上传PDCA || mesData.CmdStep == CMDStep.PDCA获取工位 || mesData.CmdStep == CMDStep.PDCA获取URL)
                {
                    myQueueCMDForPDCA.Enqueue(mesData);
                }
                else
                {
                    myQueueCMD.Enqueue(mesData);

                }
                bool b_Exist = false;
                for (int i = 0; i < myListResult.Count; i++)
                {
                    if (myListResult[i].IntStation == station && myListResult[i].CmdStep == cmdStep)
                    {
                        myListResult[i].StrResult = "";
                        myListResult[i].StrErrMSG = "";
                        myListResult[i].StrReturnMSG = "";
                        myListResult[i].StrSendMSG = "";
                        b_Exist = true;
                        break;
                    }
                }
                if (b_Exist == false)
                {
                    myListResult.Add(mesData);
                }
            }
            if (cmdStep == CMDStep.用户登录)
            {
                AddCMD(0, CMDStep.上传软件版本);
                AddCMD(0, CMDStep.获取胶水配置);
                AddCMD(0, CMDStep.MES获取工位);
            }
        }
        public static void AddCMD(int station, CMDStep cmdStep, int indexSN)
        {
            lock (locker1)
            {
                MesData mesData = new MesData(station, cmdStep, "");
                mesData.IntIndexSN = indexSN;
                if (mesData.CmdStep == CMDStep.上传PDCA || mesData.CmdStep == CMDStep.PDCA获取工位 || mesData.CmdStep == CMDStep.PDCA获取URL)
                {
                    myQueueCMDForPDCA.Enqueue(mesData);
                }
                else
                {
                    myQueueCMD.Enqueue(mesData);

                }
                bool b_Exist = false;
                for (int i = 0; i < myListResult.Count; i++)
                {
                    if (myListResult[i].IntStation == station && myListResult[i].CmdStep == cmdStep)
                    {
                        myListResult[i].StrResult = "";
                        myListResult[i].StrErrMSG = "";
                        myListResult[i].StrReturnMSG = "";
                        myListResult[i].StrSendMSG = "";
                        b_Exist = true;
                        break;
                    }
                }
                if (b_Exist == false)
                {
                    myListResult.Add(mesData);
                }
            }
            if (cmdStep == CMDStep.用户登录)
            {
                AddCMD(0, CMDStep.上传软件版本);
                AddCMD(0, CMDStep.获取胶水配置);
                AddCMD(0, CMDStep.MES获取工位);
            }
        }
        /// <summary>
        /// 如果拿到结果则返回true,
        /// </summary>
        public static string GetResult(int station, CMDStep cmdStep)
        {
            string result = "";
            lock (locker1)
            {
                for (int i = 0; i < myListResult.Count; i++)
                {
                    if (myListResult[i].IntStation == station && myListResult[i].CmdStep == cmdStep)
                    {
                        if (myListResult[i].StrResult == "OK")
                        {
                            result = "OK";
                        }
                        else if (myListResult[i].StrResult == "NG")
                        {
                            result = "NG";
                        }
                        else
                        {
                            result = "";
                        }
                        break;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 拿到发送的数据
        /// </summary>
        public static string GetSendMSG(int station, CMDStep cmdStep)
        {
            string sendMSG = "";
            lock (locker1)
            {
                for (int i = 0; i < myListResult.Count; i++)
                {
                    if (myListResult[i].IntStation == station && myListResult[i].CmdStep == cmdStep)
                    {
                        sendMSG = myListResult[i].StrSendMSG;
                        break;
                    }
                }
            }
            return sendMSG;
        }
        /// <summary>
        /// 拿到接收的数据
        /// </summary>
        public static string getReturnMSG(int station, CMDStep cmdStep)
        {
            string sendMSG = "";
            lock (locker1)
            {
                for (int i = 0; i < myListResult.Count; i++)
                {
                    if (myListResult[i].IntStation == station && myListResult[i].CmdStep == cmdStep)
                    {
                        sendMSG = myListResult[i].StrReturnMSG;
                        break;
                    }
                }
            }
            return sendMSG;
        }
        /// <summary>
        /// 拿到Err的数据
        /// </summary>
        public static string GetErrMSG(int station, CMDStep cmdStep)
        {
            string sendMSG = "";
            lock (locker1)
            {
                for (int i = 0; i < myListResult.Count; i++)
                {
                    if (myListResult[i].IntStation == station && myListResult[i].CmdStep == cmdStep)
                    {
                        sendMSG = myListResult[i].StrErrMSG;
                        break;
                    }
                }
            }
            return sendMSG;
        }
        public void clear()
        {
            lock (locker1)
            {
                myListResult.Clear();
                myQueueCMD.Clear();
            }
        }
        private void AddResult(MesData mesData, bool result)
        {
            lock (locker1)
            {
                for (int j = 0; j < myListResult.Count; j++)
                {
                    if (myListResult[j].IntStation == mesData.IntStation && myListResult[j].CmdStep == mesData.CmdStep)
                    {
                        if (result)
                        {
                            myListResult[j].StrResult = "OK";
                        }
                        else
                        {
                            myListResult[j].StrResult = "NG";
                        }
                        myListResult[j].StrSendMSG = mesData.StrSendMSG;
                        myListResult[j].StrReturnMSG = mesData.StrReturnMSG;
                        myListResult[j].StrErrMSG = mesData.StrErrMSG;
                    }
                }
            }
        }
        public void CircleForMES()
        {
            while (true)
            {
                if (b_Close)
                {
                    break;
                }
                Thread.Sleep(1);
                MesData mesData = null;
                if (myQueueCMD.Count > 0)
                {
                    lock (locker1)
                    {
                        mesData = myQueueCMD.Dequeue();
                    }
                }
                else
                {
                    continue;
                }
           
                //dnagiq不存再
                if (MESDataDefine.StrMyMesMsgDic[mesData.IntIndex].ContainsKey(mesData.CmdStep.ToString()) != true)
                {
                    AddResult(mesData, true);
                    continue;
                }
                if (mesData.CmdStep != CMDStep.用户登录)
                {
                    if (MESDataDefine.StrCMD == "")
                    {
                        continue;
                    }
                }
                CMDStep currentCMDStep = mesData.CmdStep;
                if (currentCMDStep == CMDStep.MES获取S_BUILD || currentCMDStep == CMDStep.MES获取BUILD_EVENT || currentCMDStep == CMDStep.MES获取BUILD_MATRIX_CONFIG)
                {
                    currentCMDStep = CMDStep.MES获取S_BUILD;
                }
                string mesStr = "";
                switch (currentCMDStep)
                {
                    case CMDStep.MES获取S_BUILD:
                        mesStr = formatMESData(mesData);//对要发送的字符串进行截取，以适应一个文件传多次的情况
                        string sendStr = getValue(mesData, mesStr);
                        SaveDateMes(mesData.CmdStep.ToString() + "\r\n" + "send\r\n" + sendStr);
                        mesData.StrSendMSG = sendStr;
                        string getStr11 = MES_GTK(sendStr);
                        mesData.StrReturnMSG = getStr11;
                        bool result1 = JudgeResult(getStr11, ref mesData, 0);
                        AddResult(mesData, result1);
                        SaveDateMes(mesData.CmdStep.ToString() + "\r\n" + "get\r\n" + getStr11 + "\r\n");
                        break;
                    default:
                        mesStr = formatMESData(mesData);//对要发送的字符串进行截取，以适应一个文件传多次的情况
                        string[] mesStrs = mesStr.Split(new string[] { "{\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        string returnStr = "";
                        for (int i = 0; i < mesStrs.Length; i++)
                        {
                            if (mesStrs[i].Length > 1)
                            {
                                mesStrs[i] = "{\r\n" + mesStrs[i];
                                SaveDateMes(mesData.CmdStep.ToString() + "\r\n" + "send\r\n" + mesStrs[i]);
                                mesData.StrSendMSG = mesStrs[i];
                                try
                                {
                                    string getStr = push(mesStrs[i], ref mesData);
                                    returnStr += getStr;
                                    mesData.StrReturnMSG = returnStr;
                                    bool result = JudgeResult(getStr, ref mesData, i);
                                    SaveDateMes(mesData.CmdStep.ToString() + "\r\n" + "get\r\n" + getStr + "\r\n");
                                    if (result != true)
                                    {
                                        AddResult(mesData, result);
                                        break;
                                    }
                                    else if (i == mesStrs.Length - 1)
                                    {
                                        AddResult(mesData, result);
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                        break;
                }
            }
        }
        public void CircleForPDCA()
        {
            while (true)
            {
                if (b_Close)
                {
                    break;
                }
                Thread.Sleep(1);
                MesData mesData = null;
                if (myQueueCMDForPDCA.Count > 0)
                {
                    lock (locker1)
                    {
                        mesData = myQueueCMDForPDCA.Dequeue();
                    }
                }
                else
                {
                    continue;
                }
                //如果本地文件不存在，则直接返回true
                if (MESDataDefine.StrMyMesMsgDic[mesData.IntIndex].ContainsKey(mesData.CmdStep.ToString()) != true)
                {
                    AddResult(mesData, true);
                    continue;
                }
                string mesStr = "";
                string getStr = "";
                bool b_Circle = true;
                //if (mesData.cmdStep == CMDStep.上传PDCA)
                //{
                //    currentCMDStep = mesData.cmdStep;
                //}
                currentCMDStep = CMDStep.上传PDCA;
                while (b_Circle)
                {
                    switch (currentCMDStep)
                    {
                        case CMDStep.上传PDCA:
                            mesStr = formatMESData(mesData);
                            SaveDateMes(mesData.CmdStep.ToString() + "\r\n" + "send\r\n" + mesStr);
                            mesData.StrSendMSG = mesStr;
                            if (socket.connectOk != true)
                            {
                                string ip = MESDataDefine.MESDatas.StrMiniIP;
                                string port = MESDataDefine.MESDatas.StrMiniPort;
                                socket.Open(ip, Convert.ToInt32(port));
                            }
                            socket.SendMsg(mesStr);
                            currentCMDStep = CMDStep.waitTime;
                            if (mesData.CmdStep == CMDStep.上传PDCA)
                            {
                                POSTm_tmDelay.Interval = 5000;
                            }
                            else
                            {
                                POSTm_tmDelay.Interval = 500;
                            }
                            POSTm_tmDelay.Start();
                            break;
                        case CMDStep.waitTime:
                            string strGet = socket.StrBack;
                            if (strGet.Length > 0)
                            {
                                getStr += strGet;
                                socket.StrBack = "";
                            }
                            if (POSTm_tmDelay.Enabled == false)
                            {
                                currentCMDStep = CMDStep.CheckPDCAReciveMSG;
                            }
                            break;
                        case CMDStep.CheckPDCAReciveMSG://判断PDCA返回的值是否符合要求
                            mesData.StrReturnMSG = getStr;
                            SaveDateMes(mesData.CmdStep.ToString() + "\r\n" + "get\r\n" + getStr);
                            bool result = JudgeResult(mesData.StrReturnMSG, ref mesData, mesData.IntIndexSN);
                            AddResult(mesData, result);
                            b_Circle = false;
                            break;
                    }
                }
            }
        }
        public enum data
        {
            令牌,
            线体,
            工站,
            工位,
            产品条码,
            产品条码L,
            产品条码R,
            作业员用户名,
            作业员密码,
            胶水码ID,
            MES胶水码,//分前后龙门，前龙门MES胶水码，后龙门MES胶水码
            新胶水码,
            工装码,
            压力,//分前后龙门--
            最小压力,
            最大压力,
            温度,
            最小温度,
            最大温度,
            混合比,
            最小混合比,
            最大混合比,
            A压力,
            最小A压力,
            最大A压力,
            B压力,
            最小B压力,
            最大B压力,//分前后龙门--
            通道,
            开始时间,
            结束时间,
            机台编号,
            站别名,//类似AOI-1
            压缩图片路径,
            电脑账户,
            电脑密码,
            软件版本,
            胶管温度,
            胶管最小温度,
            胶管最大温度,
            MES错误代码,
            胶路速度,
            S_Build值,
            BUILD_EVENT值,
            BUILD_MARTIX_CONFIG值,
        }
        private string getValue(MesData mesData, string mesStr)
        {
            string gantry = "前龙门";
            int station = mesData.IntStation;
            string currentStr = mesStr;
            currentStr = currentStr.Replace('{', ' ');
            currentStr = currentStr.Replace('}', ' ');
            currentStr = currentStr.Replace('\\', ' ');
            currentStr = currentStr.Replace('"', ' ');
            currentStr = currentStr.Replace(',', ' ');
            currentStr = currentStr.Replace(':', ' ');
            currentStr = currentStr.Replace('\r', ' ');
            currentStr = currentStr.Replace('\n', ' ');
            currentStr = currentStr.Replace('=', ' ');
            currentStr = currentStr.Replace('@', ' ');
            string[] currentStrs = currentStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            List<string> myList = new List<string>();
            for (int i = 0; i < currentStrs.Length; i++)
            {
                if (currentStrs[i].Trim() != "")
                {
                    myList.Add(currentStrs[i]);
                }
            }
            for (int i = 0; i < myList.Count; i++)
            {
                int st = 0;
                if (myList[i].Contains("前龙门"))
                {
                    st = 0;
                }
                else if (myList[i].Contains("后龙门"))
                {
                    st = 1;
                }
                if (myList[i] == data.令牌.ToString())
                {
                    mesStr = mesStr.Replace(data.令牌.ToString(), MESDataDefine.StrCMD);
                }
                if (myList[i] == data.线体.ToString())
                {
                    mesStr = mesStr.Replace(data.线体.ToString(), mESGTKData.StrLineNo);
                }
                if (myList[i] == data.工站.ToString())
                {
                    mesStr = mesStr.Replace(data.工站.ToString(), mESGTKData.StrSection);
                }
                if (myList[i] == data.工位.ToString())
                {
                    mesStr = mesStr.Replace(data.工位.ToString(), mESGTKData.StrStation);
                }
                if (myList[i] == data.产品条码.ToString())
                {
                    if (mesData.CmdStep == CMDStep.上传PDCA || mesData.CmdStep == CMDStep.提交过站 || mesData.CmdStep == CMDStep.上传复检结果 || mesData.CmdStep == CMDStep.上传参数信息 || mesData.CmdStep == CMDStep.检查追溯信息)//检查过站时SN还没有给SNDelay赋值 
                    {
                        mesStr = mesStr.Replace(data.产品条码.ToString(), MESDataDefine.StrSNDelayS[station]);
                    }
                    else
                    {
                        mesStr = mesStr.Replace(data.产品条码.ToString(), MESDataDefine.StrSNS[station]);
                    }
                    if (MESDataDefine.IsBongS[station] == true)
                    {
                        MESDataDefine.IsBongS[station] = false;
                        mesStr = mesStr.Replace(data.产品条码.ToString(), MESDataDefine.StrSNDelayS[station]);
                    }
                }
                if (myList[i] == data.产品条码L.ToString())
                {
                    if (mesData.CmdStep == CMDStep.检查过站 || mesData.CmdStep == CMDStep.上传设备BOM || mesData.CmdStep == CMDStep.清除临时表)//检查过站时给SN赋值
                    {
                        mesStr = mesStr.Replace(data.产品条码L.ToString(), MESDataDefine.StrSNFormMesS[station][0]);
                    }
                    else
                    {
                        mesStr = mesStr.Replace(data.产品条码L.ToString(), MESDataDefine.StrSNFormMESDelayS[station][0]);
                    }
                }
                if (myList[i] == data.产品条码R.ToString())
                {
                    if (mesData.CmdStep == CMDStep.检查过站 || mesData.CmdStep == CMDStep.上传设备BOM || mesData.CmdStep == CMDStep.清除临时表)//检查过站时给SN赋值
                    {
                        mesStr = mesStr.Replace(data.产品条码R.ToString(), MESDataDefine.StrSNFormMesS[station][1]);
                    }
                    else
                    {
                        mesStr = mesStr.Replace(data.产品条码R.ToString(), MESDataDefine.StrSNFormMESDelayS[station][1]);
                    }
                }
                if (myList[i] == data.作业员用户名.ToString())
                {
                    mesStr = mesStr.Replace(data.作业员用户名.ToString(), MESDataDefine.StrOPID);
                }
                if (myList[i] == data.作业员密码.ToString())
                {
                    mesStr = mesStr.Replace(data.作业员密码.ToString(), MESDataDefine.StrOPPassWord);
                }
                if (myList[i] == data.胶水码ID.ToString())
                {
                    mesStr = mesStr.Replace(data.胶水码ID.ToString(), MESDataDefine.StrMESIdS[station][0]);
                }
                if (myList[i] == gantry + data.MES胶水码.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.MES胶水码.ToString(), MESDataDefine.StrMESGlueS[st][0]);
                }
                if (myList[i] == data.新胶水码.ToString())
                {
                    mesStr = mesStr.Replace(data.新胶水码.ToString(), MESDataDefine.StrNewMESGlueS[station][0]);
                }
                if (myList[i] == data.工装码.ToString())
                {
                    if (mesData.CmdStep != CMDStep.上传PDCA)//检查过站时SN还没有给SNDelay赋值
                    {
                        mesStr = mesStr.Replace(data.工装码.ToString(), MESDataDefine.StrCarryBarCodeS[station]);//在载具进料时使用这个变量，不能使用m_strCarryBarCodeDelay
                    }
                    else
                    {
                        mesStr = mesStr.Replace(data.工装码.ToString(), MESDataDefine.StrCarryBarCodeDelayS[station]);
                    }
                }
                if (myList[i] == gantry + data.压力.ToString())
                {
                    string press = "0";
                    if (gantry == "前龙门")
                    {
                        press = MESDataDefine.MESDatas.StrNordPressureS[0];
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmFront)
                        {
                            press = MESDataDefine.MESDatas.StrNordPressureS[0];
                        }
                        else
                        {
                            // press = frm_Main.myDispenserController[st].controller_Presure.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.NordPressures[st];
                        }
                    }
                    else
                    {
                        press = MESDataDefine.MESDatas.StrNordPressureS[1];
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmBack)
                        {
                            press = MESDataDefine.MESDatas.StrNordPressureS[1];
                        }
                        else
                        {
                            // press = frm_Main.myDispenserController[st].controller_Presure.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.NordPressures[st];
                        }
                    }
                    mesStr = mesStr.Replace(gantry + data.压力.ToString(), press);
                }
                if (myList[i] == gantry + data.最小压力.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最小压力.ToString(), MESDataDefine.MESDatas.StrNordPressureLimitS[st][0]);
                }
                if (myList[i] == gantry + data.最大压力.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最大压力.ToString(), MESDataDefine.MESDatas.StrNordPressureLimitS[st][1]);
                }
                if (myList[i] == gantry + data.温度.ToString())
                {
                    string press = "0";
                    if (gantry == "前龙门")
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmFront)
                        {
                            press = MESDataDefine.MESDatas.StrNordsonTempS[0];
                        }
                        else
                        {
                           
                            press = MESDataDefine.myReflashDispenserDataClass.NordsonTemps[st];
                        }
                    }
                    else
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmBack)
                        {
                            press = MESDataDefine.MESDatas.StrNordsonTempS[1];
                        }
                        else
                        {
                            // press = frm_Main.myDispenserController[st].controller_T1.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.NordsonTemps[st];
                        }
                    }
                    mesStr = mesStr.Replace(gantry + data.温度.ToString(), press);
                }
                if (myList[i] == gantry + data.胶管温度.ToString())
                {
                    string press = "0";
                    if (gantry == "前龙门")
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmFront)
                        {
                            press = MESDataDefine.MESDatas.StrTubeNordsonTempS[0];
                        }
                        else
                        {
                            // press = frm_Main.myDispenserController[st].controller_T2.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.Tube_NordsonTemps[st];
                        }
                    }
                    else
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmBack)
                        {
                            press = MESDataDefine.MESDatas.StrTubeNordsonTempS[1];
                        }
                        else
                        {
                            // press = frm_Main.myDispenserController[st].controller_T2.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.Tube_NordsonTemps[st];
                        }
                    }
                    mesStr = mesStr.Replace(gantry + data.胶管温度.ToString(), press);
                }
                if (myList[i] == gantry + data.最小温度.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最小温度.ToString(), MESDataDefine.MESDatas.StrNordsonTempLimitS[st][0]);
                }
                if (myList[i] == gantry + data.最大温度.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最大温度.ToString(), MESDataDefine.MESDatas.StrNordsonTempLimitS[st][1]);
                }
                if (myList[i] == gantry + data.胶管最小温度.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.胶管最小温度.ToString(), MESDataDefine.MESDatas.StrTubeNordsonTempLimitS[st][0]);
                }
                if (myList[i] == gantry + data.胶管最大温度.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.胶管最大温度.ToString(), MESDataDefine.MESDatas.StrTubeNordsonTempLimitS[st][1]);
                }
                if (myList[i] == gantry + data.混合比.ToString())
                {
                    string press = "0";
                    if (gantry == "前龙门")
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmFront)
                        {
                            press = MESDataDefine.MESDatas.StrABRateS[0];
                        }
                        else
                        {
                            // press = frm_Main.myDispenserController[st].controller_ABrate.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.ABRates[st];
                        }
                    }
                    else
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmBack)
                        {
                            press = MESDataDefine.MESDatas.StrABRateS[1];
                        }
                        else
                        {
                            // press = frm_Main.myDispenserController[st].controller_ABrate.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.ABRates[st];
                        }
                    }
                    mesStr = mesStr.Replace(gantry + data.混合比.ToString(), press);
                }
                if (myList[i] == gantry + data.最小混合比.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最小混合比.ToString(), MESDataDefine.MESDatas.StrABRateLimitS[st][0]);
                }
                if (myList[i] == gantry + data.最大混合比.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最大混合比.ToString(), MESDataDefine.MESDatas.StrABRateLimitS[st][1]);
                }
                if (myList[i] == gantry + data.A压力.ToString())
                {
                    string press = "0";
                    if (gantry == "前龙门")
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmFront)
                        {
                            press = MESDataDefine.MESDatas.StrAPressureS[0];
                        }
                        else
                        {
                            //  press = frm_Main.myDispenserController[st].controller_APress.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.APressures[st];
                        }
                    }
                    else
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmBack)
                        {
                            press = MESDataDefine.MESDatas.StrAPressureS[1];
                        }
                        else
                        {
                            //  press = frm_Main.myDispenserController[st].controller_APress.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.APressures[st];
                        }
                    }
                    mesStr = mesStr.Replace(gantry + data.A压力.ToString(), press);
                }
                if (myList[i] == gantry + data.最小A压力.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最小A压力.ToString(), MESDataDefine.MESDatas.StrAPressureLimitS[st][0]);
                }
                if (myList[i] == gantry + data.最大A压力.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最大A压力.ToString(), MESDataDefine.MESDatas.StrAPressureLimitS[st][1]);
                }
                if (myList[i] == gantry + data.B压力.ToString())
                {
                    string press = "0";
                    if (gantry == "前龙门")
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmFront)
                        {
                            press = MESDataDefine.MESDatas.StrBPressureS[0];
                        }
                        else
                        {
                            //  press = frm_Main.myDispenserController[st].controller_BPress.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.BPressures[st];
                        }
                    }
                    else
                    {
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmBack)
                        {
                            press = MESDataDefine.MESDatas.StrBPressureS[1];
                        }
                        else
                        {
                            //  press = frm_Main.myDispenserController[st].controller_BPress.ToString();
                            press = MESDataDefine.myReflashDispenserDataClass.BPressures[st];
                        }
                    }
                    mesStr = mesStr.Replace(gantry + data.B压力.ToString(), press);
                }
                if (myList[i] == gantry + data.最小B压力.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最小B压力.ToString(), MESDataDefine.MESDatas.StrBPressureLimitS[st][0]);
                }
                if (myList[i] == gantry + data.最大B压力.ToString())
                {
                    mesStr = mesStr.Replace(gantry + data.最大B压力.ToString(), MESDataDefine.MESDatas.StrBPressureLimitS[st][1]);
                }
                if (myList[i] == data.开始时间.ToString())
                {
                    mesStr = mesStr.Replace(data.开始时间.ToString(), MESDataDefine.StrStartTimeDelayS[mesData.IntStation]);
                }
                if (myList[i] == data.结束时间.ToString())
                {
                    mesStr = mesStr.Replace(data.结束时间.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                if (myList[i] == data.站别名.ToString())
                {
                    mesStr = mesStr.Replace(data.站别名.ToString(), mESGTKData.StrMachineStation);
                }
                if (myList[i] == data.机台编号.ToString())
                {
                    mesStr = mesStr.Replace(data.机台编号.ToString(), mESGTKData.StrMachineNO);
                }
                if (myList[i] == data.压缩图片路径.ToString())
                {
                    string imagePath = MESDataDefine.StrImagetimeDelayS[mesData.IntStation].Substring(0, 4) + "-" + MESDataDefine.StrImagetimeDelayS[mesData.IntStation].Substring(4, 2) + "-" + MESDataDefine.StrImagetimeDelayS[mesData.IntStation].Substring(6, 2);
                    mesStr = mesStr.Replace(data.压缩图片路径.ToString(), MESDataDefine.MESDatas.StrPDCAImagePath + imagePath + "/" + MESDataDefine.StrSNDelayS[mesData.IntStation] + "_" + MESDataDefine.StrImagetimeDelayS[mesData.IntStation] + ".zip");
                }
                if (myList[i] == data.电脑账户.ToString())
                {
                    mesStr = mesStr.Replace(data.电脑账户.ToString(), MESDataDefine.MESDatas.StrUser);
                }
                if (myList[i] == data.电脑密码.ToString())
                {
                    mesStr = mesStr.Replace(data.电脑密码.ToString(), MESDataDefine.MESDatas.StrPassWord);
                }
                if (myList[i] == data.通道.ToString())
                {
                    mesStr = mesStr.Replace(data.通道.ToString(), (station + 1).ToString());
                }
                if (myList[i] == data.软件版本.ToString())
                {
                    mesStr = mesStr.Replace(data.软件版本.ToString(), MESDataDefine.MESDatas.StrVersion.ToString());
                }
                if (myList[i] == data.MES错误代码.ToString())
                {
                    string errCode = MESDataDefine.MES_ErrorCode[mesData.IntStation];
                    if (mesData.CmdStep == CMDStep.上传PDCA)
                    {
                        errCode = "ERRORCODE_" + errCode;
                    }
                    mesStr = mesStr.Replace(data.MES错误代码.ToString(), errCode);
                }
                if (myList[i] == data.胶路速度.ToString())
                {
                    mesStr = mesStr.Replace(data.胶路速度.ToString(), MESDataDefine.getGlueSpeedStr(station));
                }
                if (myList[i] == data.S_Build值.ToString())
                {
                    mesStr = mesStr.Replace(data.S_Build值.ToString(), MESDataDefine.StrBulid.ToString());
                }
                if (myList[i] == data.BUILD_EVENT值.ToString())
                {
                    mesStr = mesStr.Replace(data.BUILD_EVENT值.ToString(), MESDataDefine.StrBulidEvent.ToString());
                }
                if (myList[i] == data.BUILD_MARTIX_CONFIG值.ToString())
                {
                    mesStr = mesStr.Replace(data.BUILD_MARTIX_CONFIG值.ToString(), MESDataDefine.StrBulidMartixConfig.ToString());
                }
                if (gantry == "前龙门")
                {
                    gantry = "后龙门";
                    i--;
                }
                else
                {
                    gantry = "前龙门";
                }
            }
            if (mesData.CmdStep == CMDStep.上传PDCA)//产品条码@attr@属性@值
            {
                mesStr = mesStr.Trim().Substring(0, mesStr.Trim().Length - 2).Trim() + "\r\n";
                if (MESDataDefine.MES_ErrorCode[mesData.IntStation].Contains("NA") || MESDataDefine.MES_ErrorCode[mesData.IntStation].Contains("Manually_Recheck_OK"))
                {
                    int n = mesStr.Trim().LastIndexOf('\n');
                    mesStr = mesStr.Substring(0, n) + "\r\n";
                }
                string attrs = "";
                foreach (KeyValuePair<string, string> item in MESDataDefine.StrAttributeDic[mesData.IntStation][mesData.IntIndexSN])
                {
                    attrs += MESDataDefine.StrSNDelayS[mesData.IntStation] + "@attr@" + item.Key + "@" + item.Value + "\r\n";
                }
                string submit = MESDataDefine.StrSNDelayS[mesData.IntStation] + "@submit@" + MESDataDefine.MESDatas.StrVersion + "\r\n";
                mesStr = mesStr + attrs + submit + "}\r\n";
            }
            return mesStr;
        }
        private string formatMESData(MesData mesData11)
        {
            string currentStr = MESDataDefine.StrMyMesMsgDic[mesData11.IntIndex][mesData11.CmdStep.ToString()];
            string mesData = getValue(mesData11, currentStr);
            return mesData;
        }
        private string push(string mesStr, ref MesData mesData)
        {

            string str = "";
            try
            {
                if (mesData.CmdStep == CMDStep.用户登录)
                {
                    Web = (HttpWebRequest)WebRequest.Create(mESGTKData.StrGetHwdURL);
                }
                else
                {
                    Web = (HttpWebRequest)WebRequest.Create(mESGTKData.StrGetMESDataURL);
                }
                byte[] data = Encoding.UTF8.GetBytes(mesStr);
                Web.Method = "POST";
                Web.ContentType = "application/json"; //"application/x-www-form-urlencoded";// Web.ContentType = "application/json";

                Web.ContentLength = data.Length;
                using (var stream = Web.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                Res = (HttpWebResponse)Web.GetResponse();

                using (StreamReader sr = new StreamReader(Res.GetResponseStream(), Encoding.UTF8))
                {
                    str = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                lock (locker1)
                {
                    for (int i = 0; i < myListResult.Count; i++)
                    {
                        if (myListResult[i].IntStation == mesData.IntStation && myListResult[i].CmdStep == mesData.CmdStep)
                        {
                            myListResult[i].StrResult = "NG";
                            myListResult[i].StrErrMSG = e.ToString();
                        }
                    }
                }
            }
            return str;
        }
        private void getIDAndValue(int currentIndex, List<string> datas, ref int index)
        {
            string id = "";
            string value = "";
            int station = 0;
            currentIndex = currentIndex + 1;
            for (int i = currentIndex; i < datas.Count; i++)
            {
                if (datas[i] == "ID")
                {
                    id = datas[i + 1];
                }
                if (datas[i] == "G_RAW_PN_TYPE")
                {
                    if (datas[i + 1] == "Glue_Front")
                    {
                        station = 0;
                    }
                    else if (datas[i + 1] == "Glue_Back")
                    {
                        station = 1;
                    }
                }
                if (datas[i] == "G_RAW_PN_VALUE")
                {
                    value = datas[i + 1];
                }
                if (id != "" && value != "")
                {
                    MESDataDefine.StrMESIdS[station][0] = id;
                    MESDataDefine.StrMESGlueS[station][0] = value;//"20210318-1040";// 
                    index = i;
                    break;
                }
            }
        }
        private bool JudgeResult(string currentStr1, ref MesData mesData, int count)
        {
            bool b_Result = false;
            string currentStr = currentStr1;
            currentStr = currentStr.Replace('{', '^');
            currentStr = currentStr.Replace('}', '^');
            currentStr = currentStr.Replace('\\', '^');
            currentStr = currentStr.Replace('"', '^');
            currentStr = currentStr.Replace(',', '^');
            currentStr = currentStr.Replace(':', '^');
            currentStr = currentStr.Replace('\r', '^');
            currentStr = currentStr.Replace('@', '^');
            currentStr = currentStr.Replace('\n', '^');
            currentStr = currentStr.Replace(';', '^');
            string[] currentStrs = currentStr.Split('^');
            List<string> myList = new List<string>();
            for (int i = 0; i < currentStrs.Length; i++)
            {
                if (currentStrs[i].Trim() != "")
                {
                    myList.Add(currentStrs[i]);
                }
            }
            for (int i = 0; i < myList.Count; i++)
            {
                switch (mesData.CmdStep)
                {
                    case CMDStep.用户登录:
                        if (myList[i] == "cmd")
                        {
                            if (myList[i + 1] == "4")
                            {
                                b_Result = true;
                            }
                            else
                            {
                                b_Result = false;
                                MESDataDefine.StrCMD = "";
                                return b_Result;
                            }
                        }
                        if (myList[i] == "Hwd")
                        {
                            MESDataDefine.StrCMD = myList[i + 1];
                        }
                        break;
                    case CMDStep.上传软件版本:
                        if (myList[i] == "Result")
                        {
                            if (myList[i + 1] == "OK")
                            {
                                b_Result = true;
                            }
                            else
                            {
                                int n = currentStr.IndexOf("Result");
                                string err = currentStr.Substring(n, currentStr.Length - n - 1);
                                mesData.StrErrMSG = err;
                                b_Result = false;
                            }
                        }
                        break;
                    case CMDStep.获取胶水配置:
                        int index = 0;
                        getIDAndValue(0, myList, ref index);
                        getIDAndValue(index, myList, ref index);
                        b_Result = true;
                        break;
                    case CMDStep.设置胶水信息:
                        if (myList[i] == "Result")
                        {
                            if (myList[i + 1] == "OK")
                            {
                                b_Result = true;
                            }
                            else
                            {
                                int n = currentStr.IndexOf("Result");
                                string err = currentStr.Substring(n, currentStr.Length - n - 1);
                                mesData.StrErrMSG = err;
                                b_Result = false;
                            }
                        }
                        break;
                    case CMDStep.载具获取SN:
                        if (myList[i] == "SN")
                        {
                            MESDataDefine.StrSNS[mesData.IntStation] = myList[i + 1];//此处要赋值SN，而不是SNDelay
                            b_Result = true;
                        }
                        break;
                    case CMDStep.载具获取多个SN:
                        if (myList[i] == "SNLIST")
                        {
                            MESDataDefine.StrSNS[mesData.IntStation] = myList[i + 1];//默认把左边的赋值给SN
                            MESDataDefine.StrSNFormMesS[mesData.IntStation][0] = myList[i + 1];
                            MESDataDefine.StrSNFormMesS[mesData.IntStation][1] = myList[i + 2];
                            b_Result = true;
                        }
                        break;
                    case CMDStep.检查过站:
                        if (myList[i] == "Result")
                        {
                            if (myList[i + 1] == "OK")
                            {
                                b_Result = true;
                            }
                            else
                            {
                                int n = currentStr.IndexOf("Result");
                                string err = currentStr.Substring(n, currentStr.Length - n - 1);
                                mesData.StrErrMSG = err;
                                b_Result = false;
                            }
                        }
                        break;
                    case CMDStep.上传参数信息:
                        if (myList[i] == "Result")
                        {
                            if (myList[i + 1] == "OK")
                            {
                                b_Result = true;
                            }
                            else
                            {
                                int n = currentStr.IndexOf("Result");
                                string err = currentStr.Substring(n, currentStr.Length - n - 1);
                                mesData.StrErrMSG = err;
                                b_Result = false;
                                AddCMD(mesData.IntStation, CMDStep.清除临时表);
                            }
                        }
                        break;
                    case CMDStep.上传复检结果:
                        if (myList[i] == "Result")
                        {
                            if (myList[i + 1] == "OK")
                            {
                                b_Result = true;
                            }
                            else
                            {
                                int n = currentStr.IndexOf("Result");
                                string err = currentStr.Substring(n, currentStr.Length - n - 1);
                                mesData.StrErrMSG = err;
                                b_Result = false;
                                AddCMD(mesData.IntStation, CMDStep.清除临时表);
                            }
                        }
                        break;
                    case CMDStep.上传设备BOM:
                        if (myList[i] == "Result")
                        {
                            if (myList[i + 1] == "OK")
                            {
                                b_Result = true;
                            }
                            else
                            {
                                int n = currentStr.IndexOf("Result");
                                string err = currentStr.Substring(n, currentStr.Length - n - 1);
                                mesData.StrErrMSG = err;
                                b_Result = false;
                                AddCMD(mesData.IntStation, CMDStep.清除临时表);
                            }
                        }
                        break;
                    case CMDStep.检查追溯信息:
                        if (myList[i] == "SerializeData")
                        {
                            if (myList[i + 1].Contains("NG"))
                            {
                                int n = currentStr.IndexOf("SerializeData");
                                string err = currentStr.Substring(n, currentStr.Length - n - 1);
                                mesData.StrErrMSG = err;
                                b_Result = false;
                                AddCMD(mesData.IntStation, CMDStep.清除临时表);
                                return b_Result;
                            }
                        }
                        if (myList[i] == "cmd")
                        {
                            if (myList[i + 1] != "-1")
                            {
                                b_Result = true;
                                //获取参数
                                for (int j = 0; j < myList.Count; j++)
                                {
                                    if (myList[j] == "Data")
                                    {
                                        MESDataDefine.StrAttributeDic[mesData.IntStation][count].Clear();
                                        for (int m = j + 1; m < myList.Count; m++)
                                        {
                                            if (myList[m] == "Display")
                                            {
                                                return b_Result;
                                            }
                                            else
                                            {
                                                //如果是载具条码或者通道，则不再添加
                                                if (myList[m] == "Cavity_No" || myList[m].Contains("Glue_Pressure") || myList[m].Contains("Glue_Nozzle_Temp") || 
                                                    myList[m].Contains("Glue_Tube_Temp") || myList[m].Contains("Glue_Ratio")|| myList[m].Contains("Condensation_Temp"))
                                                {

                                                }
                                                else if (myList[m].Contains("FIXTURE") || myList[m].Contains("UC"))// "FIXTURE-UC1"
                                                {
                                                    MESDataDefine.StrCarryBarCodeS[mesData.IntStation] = myList[m + 1];
                                                    MESDataDefine.StrCarryBarCodeDelayS[mesData.IntStation] = myList[m + 1];
                                                }
                                                else
                                                {
                                                    MESDataDefine.StrAttributeDic[mesData.IntStation][count].Add(myList[m], myList[m + 1]);
                                                }
                                                m++;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                b_Result = false;
                            }
                        }
                        break;
                    case CMDStep.提交过站:
                        b_Result = true;
                        break;
                    case CMDStep.上传PDCA:
                        if (currentStr.Length > 10)
                        {
                            if (currentStr.Contains("bad"))
                            {
                                mesData.StrResult = "NG";
                                int index1 = currentStr.IndexOf("bad");
                                mesData.StrErrMSG = currentStr.Substring(index1, currentStr.Length - index1 - 1);
                            }
                            else if (currentStr.Contains("err"))
                            {
                                mesData.StrResult = "NG";
                                int index1 = currentStr.IndexOf("err");
                                mesData.StrErrMSG = currentStr.Substring(index1, currentStr.Length - index1 - 1);
                            }
                            else if (currentStr.Contains("FAIL"))
                            {
                                mesData.StrResult = "NG";
                                int index1 = currentStr.IndexOf("FAIL");
                                mesData.StrErrMSG = currentStr.Substring(index1, currentStr.Length - index1 - 1);
                            }
                            else
                            {
                                mesData.StrResult = "OK";
                                b_Result = true;
                            }
                        }
                        else
                        {
                            mesData.StrErrMSG = "获取数据超时";
                        }
                        break;
                    case CMDStep.MES获取工位:
                        if (myList[i] == "STATIONNAME")
                        {
                            MESDataDefine.StrMESStationName = myList[i + 1];
                            b_Result = true;
                        }
                        break;
                    case CMDStep.PDCA获取工位:
                        if (myList[i].ToUpper() == "OK")
                        {
                            for (int j = i + 1; j < myList.Count; j++)
                            {
                                if (myList[j].ToUpper() == "OK")
                                {
                                    MESDataDefine.StrPDCAStationName = myList[j + 1];
                                    b_Result = true;
                                    return b_Result;
                                }
                            }
                        }
                        break;
                    case CMDStep.清除临时表:
                        if (myList[i] == "Result")
                        {
                            if (myList[i + 1] == "OK")
                            {
                                b_Result = true;
                            }
                            else
                            {
                                int n = currentStr.IndexOf("Result");
                                string err = currentStr.Substring(n, currentStr.Length - n - 1);
                                mesData.StrErrMSG = err;
                                b_Result = false;
                            }
                        }
                        break;
                    case CMDStep.MES获取S_BUILD:
                        string[] strs = currentStr1.Split('=');
                        if (strs.Length > 1)
                        {
                            MESDataDefine.StrBulid = strs[1];
                            b_Result = true;
                            return b_Result;
                        }
                        break;
                    case CMDStep.MES获取BUILD_EVENT:
                        string[] strs1 = currentStr1.Split('=');
                        if (strs1.Length > 1)
                        {
                            MESDataDefine.StrBulidEvent = strs1[1];
                            b_Result = true;
                            return b_Result;
                        }
                        break;
                    case CMDStep.MES获取BUILD_MATRIX_CONFIG:
                        string[] strs2 = currentStr1.Split('=');
                        if (strs2.Length > 1)
                        {
                            MESDataDefine.StrBulidMartixConfig = strs2[1];
                            b_Result = true;
                            return b_Result;
                        }
                        break;
                    case CMDStep.PDCA获取URL:
                        if (currentStr1.Contains("http"))
                        {
                            string[] strs4 = currentStr1.Split('\n');
                            for (int a = 0; a < strs4.Length; a++)
                            {
                                if (strs4[a].Contains("bobcat"))
                                {
                                    string[] lastStrs = strs4[a].Split(new char[] { '{', '}' });
                                    MESDataDefine.StrGetBulidURL = lastStrs[1];
                                    b_Result = true;
                                    return b_Result;
                                }
                            }
                        }
                        else
                        {
                            b_Result = false;
                            return b_Result;
                        }
                        break;
                }
            }
            return b_Result;
        }
        public void SaveDateMes(string mes)
        {
            try
            {
                string fileName;
                fileName = string.Format("{0}.txt", DateTime.Now.ToString("yyyy_MM_dd"));
                string outputPath = @"D:\DATA\MES记录";
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                string fullFileName = Path.Combine(outputPath, fileName);
                System.IO.FileStream fs;
                StreamWriter sw;
                if (!File.Exists(fullFileName))
                {
                    fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                    sw = new StreamWriter(fs, System.Text.Encoding.Default);
                    sw.WriteLine(DateTime.Now.ToString("HH:mm:ss:fff") + "__" + mes);
                    sw.Close();
                    fs.Close();

                }
                else
                {
                    fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                    sw = new StreamWriter(fs, System.Text.Encoding.Default);
                    sw.WriteLine(DateTime.Now.ToString("HH:mm:ss:fff") + "__" + mes);
                    sw.Close();
                    fs.Close();
                }
            }
            catch
            {


            }
        }
        private string MES_GTK(string cmd)
        {
            //    string cmd = "c=QUERY_RECORD&sn=" + sn + "&p=BUILD_MATRIX_CONFIG";
          webRequest = (HttpWebRequest)WebRequest.Create(MESDataDefine.StrGetBulidURL);
            byte[] data = Encoding.UTF8.GetBytes(cmd);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = data.Length;
            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            response = (HttpWebResponse)webRequest.GetResponse();
            string str = "";
            using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                str += sr.ReadToEnd();
            }
            return str;
            //下面是MES反馈的结果
            //0 SFC_OK
            // tsid::::unit_process_check = UNIT OUT OF PROCESS NG; TERMINAL NOT FOUND
            //     SaveDateMes(sn + "->" + "SN Get Config：" + cmd, LogType.Message);
            //   SaveDateMes(sn + "->" + "SN Get Config：" + str, LogType.Message);
            //string[] strs = str.Split('=');
            //if (strs.Length > 1)
            //{
            //    if (strs[1] != "")
            //    {
            //        if (txtConfig.InvokeRequired)
            //        {
            //            txtConfig.Invoke(new Action(() => { txtConfig.Text = ; }));
            //        }
            //        else
            //        {
            //            txtConfig.Text = strs[1];
            //        }
            //    }
            //}
            //else
            //{
            //    //
            //}
        }
    }


}
