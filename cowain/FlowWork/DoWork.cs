using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cowain.Comm;
using System.Threading;
using System.Drawing;
using cowain.MES;
using cowain.FormView;
using System.Diagnostics;
using System.IO;

namespace cowain.FlowWork
{
    public class DoWork
    {
        SHIJEReader spReader = new SHIJEReader();
        OmronPLC omron = new OmronPLC(enMachine.M_046);
        Camera camera = new Camera();
        public static NetSocket clientReader = new NetSocket();
        public static NetSocket clientCCDOne = new NetSocket();
        Action<String, Color> ShowMSG = null;
        System.Timers.Timer tmDelay;
        MESDataDefine mesData = new MESDataDefine();
        LXPOSTClass lxPostClass = LXPOSTClass.CreateInstance();
        CacheProcess cpClass = null;
        HoldPress hpClass = null;
        CacheProcessOther cpClassOther = null;
        HoldPressOther hpClassOther = null;
        MESProcess mesClass = null;

        private string strMesResult = String.Empty;
        public static bool isIDLE = false;
        public static bool bPLCTogether = false;

        public static Product SOne_Product = null;

        HIVE hive = new HIVE();

        private enum enWorkStep
        {
            Start,

            CheckRole,
            NoticePLCLogin,
            NoticePLCLoginResult,
            ReadPLCSignals,

            JudgeReader,
            ReaderSerialPort,
            OpenSeriaPort,
            JudgeSerialPortType,
            ReceiveSerialPortStrData,
            ReaderNetPort,
            ReceiveNetPortStrBack,

            CheckMESandPDCA,
            CheckOperator,
            SendUCToMES,
            ReceiveSNCode,
            UploadMesUOP,
            ReceiveMesUOPResult,

            WritePLCSignals,
            WritePLCSignalsResult,

            CheckCCD,
            ReceiveCCDFeedback,
            ReadPressureOne,
            ClearPOneTriggerSignal,
            ClearPOneTriggerSignalResult,
            EnterCacheQueue,

            Completed
        }
        enWorkStep enStep;

        public DoWork(Action<String, Color> action)
        {
            this.ShowMSG = new Action<String, Color>(action);
            this.tmDelay = new System.Timers.Timer();
            tmDelay.Elapsed += TmDelay_Elapsed;
            Task.Run(() =>
            {
                if (SHIJEReader.reader.enable)
                    clientReader.Open(SHIJEReader.reader.IP, SHIJEReader.reader.Port);
                if (Camera.ccd.enable)
                    clientCCDOne.Open(Camera.ccd.IP, Camera.ccd.PortOne);
            });
            Thread thWork = new Thread(WorkCycle)
            {
                IsBackground = true
            };
            thWork.Start();

            cpClass = new CacheProcess(action);
            hpClass = new HoldPress(action);
            cpClassOther = new CacheProcessOther(action);
            hpClassOther = new HoldPressOther(action);
            mesClass = new MESProcess(action);
        }

        private void TmDelay_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            tmDelay.Stop();
        }

        private void WorkCycle()
        {
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(1);
                string result29 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[29].address);
                if (result29 == "True")
                {
                    bPLCTogether = false;
                    continue;
                }
                else
                {
                    bPLCTogether = true;
                }
                switch (enStep)
                {
                    case enWorkStep.Start:
                        //Stopwatch st = new Stopwatch();
                        //st.Start();
                        //string dd;
                        ////for (int i = 0; i < 10; i++)
                        //    dd = omron.ReadSignals("D3700", OmronPLC.enType.Bool, "100");
                        //st.Stop();
                        //TimeSpan ts = st.Elapsed;

                        ShowMSG("流程开始", Color.Black);
                        enStep = enWorkStep.CheckRole;// UploadMesUOP; 
                        break;
                    case enWorkStep.CheckRole:
                        if (true)//FormLogin.bLoginSuccess
                        {
                            tmDelay.Enabled = false;
                            enStep = enWorkStep.NoticePLCLogin;
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("请先进行用户登录", Color.Brown);
                            tmDelay.Interval = 15000;
                            tmDelay.Start();
                        }
                        break;
                    case enWorkStep.NoticePLCLogin:
                        PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[2]);
                        enStep = enWorkStep.NoticePLCLoginResult;
                        //tmDelay.Interval = 4000;
                        //tmDelay.Start();
                        break;
                    case enWorkStep.NoticePLCLoginResult:
                        string result2 = PLCQueueClass.GetWriteResultFromList(OmronPLC.plc.plcAddresses[2].address);
                        if (result2 == "Success")
                        {
                            //tmDelay.Enabled = false;
                            ShowMSG("成功写入PLC登录信号，监听到位信号", Color.Black);
                            enStep = enWorkStep.ReadPLCSignals;
                        }
                        else if (result2 == "Fail")//(!tmDelay.Enabled)
                        {
                            ShowMSG("未能成功写入PLC登录信号！重新写入", Color.Red);
                            enStep = enWorkStep.NoticePLCLogin;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enWorkStep.ReadPLCSignals:
                        string result0 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[0].address);
                        if (result0 == "1")
                        {
                            ShowMSG("载具已到位，开始扫码", Color.Black);
                            enStep = enWorkStep.JudgeReader;
                        }
                        break;
                    case enWorkStep.JudgeReader:
                        if (SHIJEReader.reader.enable)
                        {
                            clientReader.StrBack = String.Empty;
                            if (SHIJEReader.reader.communication == "串口")
                            {
                                enStep = enWorkStep.ReaderSerialPort;
                            }
                            else if (SHIJEReader.reader.communication == "网口")
                            {
                                enStep = enWorkStep.ReaderNetPort;
                                tmDelay.Interval = 2000;
                                tmDelay.Start();
                            }
                        }
                        else
                        {
                            enStep = enWorkStep.ReadPLCSignals;
                        }
                        break;
                    #region 读码器串口通讯
                    case enWorkStep.ReaderSerialPort:
                        string[] ports = System.IO.Ports.SerialPort.GetPortNames();
                        if (ports.Contains(SHIJEReader.reader.portName))
                        {
                            ShowMSG("读码器串口在线，触发扫码", Color.Black);
                            enStep = enWorkStep.OpenSeriaPort;
                            break;
                        }
                        else
                        {
                            ShowMSG("未检测到读码器串口！再次检测", Color.Red);
                            enStep = enWorkStep.JudgeReader;
                            Thread.Sleep(2000);
                            break;
                        }
                    case enWorkStep.OpenSeriaPort:
                        if (!SHIJEReader.hasOpened)
                        {
                            bool bConn = spReader.Connect();
                            if (!bConn)
                            {
                                ShowMSG("打开读码器串口失败！重新打开", Color.Red);
                                enStep = enWorkStep.JudgeReader;
                                break;
                            }
                        }
                        enStep = enWorkStep.JudgeSerialPortType;
                        tmDelay.Interval = 4000;
                        tmDelay.Start();
                        break;
                    case enWorkStep.JudgeSerialPortType:
                        if (SHIJEReader.reader.type == "视界")
                        {
                            spReader.SHIJEInstruction();
                        }
                        else if (SHIJEReader.reader.type == "新大陆")
                        {
                            spReader.NewLandInstruction();
                        }
                        enStep = enWorkStep.ReceiveSerialPortStrData;
                        break;
                    case enWorkStep.ReceiveSerialPortStrData:
                        if (spReader.StrData != "")
                        {
                            tmDelay.Enabled = false;
                            SOne_Product = new Product();
                            SOne_Product.UC = spReader.StrData;
                            ShowMSG($"接收到UC码：{SOne_Product.UC}", Color.Black);
                            enStep = enWorkStep.WritePLCSignals;
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("未接收到UC码！再次扫码", Color.Red);
                            enStep = enWorkStep.JudgeReader;
                        }
                        break;
                    #endregion
                    case enWorkStep.ReaderNetPort:
                        if (!clientReader.connectOk)
                        {
                            clientReader.Open(SHIJEReader.reader.IP, SHIJEReader.reader.Port);
                        }
                        if (clientReader.connectOk)
                        {
                            tmDelay.Enabled = false;
                            ShowMSG("读码器网络在线，触发扫码", Color.Black);
                            enStep = enWorkStep.ReceiveNetPortStrBack;
                            tmDelay.Interval = 2000;
                            tmDelay.Start();
                            clientReader.SendMsg(SHIJEReader.reader.triggerCommand);
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("未能成功连接到读码器！重新连接", Color.Red);
                            enStep = enWorkStep.JudgeReader;
                        }
                        break;
                    case enWorkStep.ReceiveNetPortStrBack:
                        if (clientReader.StrBack != "")
                        {
                            tmDelay.Enabled = false;
                            //clientReader.SendMsg(SHIJEReader.reader.releaseCommand);
                            SOne_Product = new Product();
                            SOne_Product.UC = clientReader.StrBack;
                            SOne_Product.EnterTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            SOne_Product.SendCCDTime = String.Join("", SOne_Product.EnterTime.Split('-', ' ', ':'));
                            //SOne_Product.SendCCDTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                            //ShowMSG($"接收到UC码：{SOne_Product.UC}", Color.Black);
                            enStep = enWorkStep.CheckMESandPDCA;
                        }
                        else if (!tmDelay.Enabled)
                        {
                            clientReader.SendMsg(SHIJEReader.reader.releaseCommand);
                            ShowMSG("未接收到UC码！再次监听到位信号", Color.Red);
                            PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[17]);    //写入读码失败
                            enStep = enWorkStep.ReadPLCSignals;
                            Thread.Sleep(1000);
                        }
                        break;
                    case enWorkStep.CheckMESandPDCA:
                        if (MESDataDefine.MESData.IsDisableMES)
                        {
                            enStep = enWorkStep.CheckCCD;
                            break;
                        }
                        if (!MESDataDefine.MESData.IsDisableMES)
                        {
                            if (!FormHome.b_MESOnline)
                            {
                                ShowMSG("MES网络掉线！", Color.Red);
                                Thread.Sleep(2000);
                                break;
                            }
                        }
                        if (!MESDataDefine.MESData.IsDisablePDCA)
                        {
                            if (!FormHome.b_PDCAOnline)
                            {
                                ShowMSG("PDCA网络掉线！", Color.Red);
                                Thread.Sleep(2000);
                                break;
                            }
                        }
                        enStep = enWorkStep.CheckOperator;
                        break;
                    case enWorkStep.CheckOperator:
                        //if (!MESDataDefine.MESData.IsDisableMES)
                        //{
                        //    strMesResult = String.Empty;
                        // if
                        //ShowMSG("作业员已登录", Color.Black);
                        enStep = enWorkStep.SendUCToMES;
                        // else
                        //ShowMSG("作业员未登录！", Color.Black);
                        //enStep = enWorkStep.CheckMESandPDCA;
                        //}
                        //else
                        //{
                        //enStep = enWorkStep.CheckCCD;
                        //}
                        break;
                    case enWorkStep.SendUCToMES:
                        if (SHIJEReader.reader.QRCode == "产品码")
                        {
                            SOne_Product.SN = SOne_Product.UC;
                            ShowMSG($"获取到SN码：{SOne_Product.SN}", Color.Black);
                            enStep = enWorkStep.UploadMesUOP;
                        }
                        else if (SHIJEReader.reader.QRCode == "工装码")
                        {
                            enStep = enWorkStep.ReceiveSNCode;
                            tmDelay.Interval = 2000;
                            tmDelay.Start();
                            strMesResult = lxPostClass.MES_SendUCGetSn(SOne_Product.UC);
                        }
                        break;
                    case enWorkStep.ReceiveSNCode:
                        //string strSN = lxPostClass.MES_SendUCGetSn("12345566");
                        if (strMesResult != "")
                        {
                            tmDelay.Enabled = false;
                            SOne_Product.SN = SubSN(strMesResult);
                            ShowMSG($"接收到SN码：{SOne_Product.SN}", Color.Black);
                            enStep = enWorkStep.UploadMesUOP;
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("未接收到SN码！再次上传", Color.Red);
                            PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[4]);
                            enStep = enWorkStep.SendUCToMES;
                        }
                        break;
                    case enWorkStep.UploadMesUOP:
                        enStep = enWorkStep.ReceiveMesUOPResult;
                        strMesResult = lxPostClass.MesUOP(SOne_Product.SN, SOne_Product.UC);// ("sn", "uc");
                        break;
                    case enWorkStep.ReceiveMesUOPResult:
                        if (strMesResult.Contains("0 SFC_OK"))//check=OK
                        {
                            ShowMSG($"{SOne_Product.SN}上传MesUOP结果OK", Color.Black);
                            enStep = enWorkStep.WritePLCSignals;
                        }
                        else
                        {
                            ShowMSG($"{SOne_Product.SN}过站失败!，再次监听到位信号", Color.Red);
                            PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[4]);
                            enStep = enWorkStep.ReadPLCSignals;
                            Thread.Sleep(2000);                                         // 2秒后再次扫码
                        }
                        break;
                    case enWorkStep.WritePLCSignals:
                        enStep = enWorkStep.WritePLCSignalsResult;
                        //tmDelay.Interval = 1000;
                        //tmDelay.Start();
                        PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[1]);
                        break;
                    case enWorkStep.WritePLCSignalsResult:
                        string result1 = PLCQueueClass.GetWriteResultFromList(OmronPLC.plc.plcAddresses[1].address);
                        if (result1 == "Success")
                        {
                            //tmDelay.Enabled = false;
                            ShowMSG($"{SOne_Product.SN}成功写入PLC扫码完成信号", Color.Black);
                            enStep = enWorkStep.CheckCCD;
                        }
                        else if (result1 == "Fail") //(!tmDelay.Enabled)
                        {
                            ShowMSG("未能成功写入PLC扫码信号！再次写入", Color.Red);
                            enStep = enWorkStep.WritePLCSignals;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enWorkStep.CheckCCD:
                        if (Camera.ccd.enable)
                        {
                            if (!clientCCDOne.connectOk)
                            {
                                clientCCDOne.Open(Camera.ccd.IP, Camera.ccd.PortOne);
                            }
                            if (clientCCDOne.connectOk)
                            {
                                tmDelay.Enabled = false;
                                ShowMSG($"向046相机1发送:{SOne_Product.SendCCDTime}_{SOne_Product.SN}", Color.Black);
                                //MESDataDefine.StrImagetimeS[0] = DateTime.Now.ToString("yyyyMMddHHmmss");
                                enStep = enWorkStep.ReceiveCCDFeedback;
                                tmDelay.Interval = 10000;
                                tmDelay.Start();
                                clientCCDOne.SendMsg(SOne_Product.SendCCDTime + "_" + SOne_Product.SN);
                            }
                            else if (!tmDelay.Enabled)
                            {
                                ShowMSG("未能成功连接到046相机1！重新连接", Color.Red);
                                tmDelay.Interval = 4000;
                                tmDelay.Start();
                                break;
                            }
                        }
                        else
                        {
                            enStep = enWorkStep.ReadPressureOne;
                        }
                        break;
                    case enWorkStep.ReceiveCCDFeedback:
                        if (clientCCDOne.StrBack != "")
                        {
                            //tmDelay.Enabled = false;
                            ShowMSG($"{SOne_Product.SN}接收到046相机1反馈：{clientCCDOne.StrBack}", Color.Black);
                            if (clientCCDOne.StrBack == "1")
                            {
                                SOne_Product.VisionResultOne = true;
                                enStep = enWorkStep.ReadPressureOne;
                            }
                            else if (clientCCDOne.StrBack == "2")
                            {
                                SOne_Product.VisionResultOne = false;
                                SOne_Product.PressOne = 0;
                                ShowMSG($"{SOne_Product.SN}读取到压力1：{SOne_Product.PressOne}", Color.Black);
                                enStep = enWorkStep.EnterCacheQueue;
                            }
                            else// if (clientCCDOne.StrBack == "err")
                            {
                                ShowMSG($"046相机1断开连接,无法发送{SOne_Product.SN}！", Color.Red);
                                enStep = enWorkStep.CheckCCD;
                                break;
                            }
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG($"{SOne_Product.SN}未收到046相机1反馈！再次发送", Color.Red);
                            enStep = enWorkStep.CheckCCD;
                            tmDelay.Interval = 10000;
                            tmDelay.Start();
                        }
                        break;
                    case enWorkStep.ReadPressureOne:
                        string result8 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[8].address);//"1" 
                        if (result8 == "True")
                        {
                            string pressure = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[5].address);
                            try
                            {
                                SOne_Product.PressOne = float.Parse(pressure) / 100;
                                ShowMSG($"{SOne_Product.SN}读取到压力1：{SOne_Product.PressOne}", Color.Black);
                                File.AppendAllText($"D:\\DATA\\压力\\{SOne_Product.SendCCDTime.Substring(0, 8)}.txt",
                                    SOne_Product.SN + "  压力1:" + SOne_Product.PressOne.ToString() + "\r\n");
                                enStep = enWorkStep.ClearPOneTriggerSignal;
                            }
                            catch
                            {
                                ShowMSG($"{SOne_Product.SN}读取压力1异常值：{pressure}！重新读取", Color.Red);
                                Thread.Sleep(2000);
                            }
                        }
                        break;
                    case enWorkStep.ClearPOneTriggerSignal:
                        PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[24]);
                        enStep = enWorkStep.ClearPOneTriggerSignalResult;
                        break;
                    case enWorkStep.ClearPOneTriggerSignalResult:
                        string result24 = PLCQueueClass.GetWriteResultFromList(OmronPLC.plc.plcAddresses[24].address);
                        if (result24 == "Success")
                        {
                            ShowMSG($"{SOne_Product.SN}写入PLC读取压力1完成信号", Color.Black);
                            enStep = enWorkStep.EnterCacheQueue;
                        }
                        else if (result24 == "Fail")
                        {
                            ShowMSG($"{SOne_Product.SN}未能写入PLC读取压力1完成信号！重新写入", Color.Red);
                            enStep = enWorkStep.ClearPOneTriggerSignal;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enWorkStep.EnterCacheQueue:
                        Product newProduct = SOne_Product.DeepCopy(SOne_Product);
                        CacheProcess.CacheQueue.Enqueue(newProduct);
                        ShowMSG($"{SOne_Product.SN}进入046缓存队列", Color.Black);
                        enStep = enWorkStep.ReadPLCSignals;                             // 返回读取载具信号
                        //if (!MESDataDefine.MESData.IsDisableSingle)
                        //{
                        //    SOne_Product.CurrentStation = 1;
                        //    MESProcess.MESQueue.Enqueue(SOne_Product);
                        //}
                        SOne_Product = null;
                        isIDLE = true;
                        break;

                }
            }
        }

        private string SubSN(string str)
        {
            if (str.Contains("SN="))
            {
                int SNStart = str.IndexOf("SN=");
                string SNStr = str.Trim().Substring(SNStart + 3, str.Length - SNStart - 4);
                return SNStr;
            }
            else { return ""; }
        }

    }
}
