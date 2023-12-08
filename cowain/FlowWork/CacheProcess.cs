using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cowain.MES;
using cowain.Comm;
using System.Drawing;
using System.IO;

namespace cowain.FlowWork
{
    public class CacheProcess
    {
        public static ConcurrentQueue<Product> CacheQueue = new ConcurrentQueue<Product>();
        System.Timers.Timer timer = new System.Timers.Timer();
        public static NetSocket clientCCDTwo = new NetSocket();
        Action<String, Color> ShowMSG = null;
        public static Product STwo_Product = null;

        //enSubWorkStep turnStep;
        //enSubWorkStep oldStep;
        public static bool isIDLE = false;
        private List<Product> list = new List<Product>();

        private enum enSubWorkStep
        {
            ReadArriveSignal,
            CheckCCD,
            ReceiveCCDFeedback,
            ClearArriveSignal,
            ClearArriveSignalResult,
            ReadPressureTwo,
            ClearPTwoTriggerSignal,
            ClearPTwoTriggerSignalResult,
            TemporaryStorage,
        }
        enSubWorkStep enSubStep;

        public CacheProcess(Action<String, Color> action)
        {
            ShowMSG = action;
            timer.Elapsed += Timer_Elapsed;
            Task.Run(() =>
            {
                if (Camera.ccd.enable)
                    clientCCDTwo.Open(Camera.ccd.IP, Camera.ccd.PortTwo);
            });
            Thread thCache = new Thread(SubWorkCycle);
            thCache.IsBackground = true;
            thCache.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
        }

        private void SubWorkCycle()
        {
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(1);
                string result29 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[29].address);
                if (result29 == "True") continue;
                //if (enSubStep != oldStep)
                //{
                //    File.AppendAllText(@"D:\DATA\CacheProcess.txt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + enSubStep.ToString() + "\r\n");
                //    oldStep = enSubStep;
                //}
                switch (enSubStep)
                {
                    default:
                        ShowMSG($"{enSubStep}跳转异常", Color.Black);
                        Thread.Sleep(2000);
                        break;
                    case enSubWorkStep.ReadArriveSignal:
                        if (!DoWork.isIDLE) break;
                        string result15 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[15].address);
                        if (result15 == "True")
                        {
                            //STwo_Product = new Product();
                            CacheQueue.TryDequeue(out STwo_Product);
                            if (STwo_Product != null)
                            {
                                ShowMSG($"{STwo_Product.SN}已到工位2", Color.Black);
                                enSubStep = enSubWorkStep.CheckCCD;
                            }
                            else
                            {
                                ShowMSG("未读取到到达工位2的HSG信息！", Color.Red);
                                Thread.Sleep(2000);
                            }
                        }
                        //else
                        //{
                        //    turnStep = enSubWorkStep.ReadArriveSignal;
                        //    enSubStep = enSubWorkStep.ReadPressureThree;
                        //}
                        break;
                    case enSubWorkStep.CheckCCD:
                        if (Camera.ccd.enableOther)
                        {
                            //ShowMSG($"向046相机2发送：SN", Color.Black);
                            //enSubStep = enSubWorkStep.ReceiveCCDFeedback;
                            if (!clientCCDTwo.connectOk)
                            {
                                clientCCDTwo.Open(Camera.ccd.IP, Camera.ccd.PortTwo);
                            }
                            if (clientCCDTwo.connectOk)
                            {
                                timer.Enabled = false;
                                ShowMSG($"向046相机2发送:{STwo_Product.SendCCDTime}_{STwo_Product.SN}", Color.Black);
                                //MESDataDefine.StrImagetimeS[0] = DateTime.Now.ToString("yyyyMMddHHmmss");
                                enSubStep = enSubWorkStep.ReceiveCCDFeedback;
                                timer.Interval = 6000;
                                timer.Start();
                                clientCCDTwo.SendMsg(STwo_Product.SendCCDTime + "_" + STwo_Product.SN);
                            }
                            else if (!timer.Enabled)
                            {
                                ShowMSG("未能成功连接到046相机2！重新连接", Color.Red);
                                timer.Interval = 4000;
                                timer.Start();
                                break;
                            }
                        }
                        else
                        {
                            enSubStep = enSubWorkStep.ReadPressureTwo;
                        }
                        break;
                    case enSubWorkStep.ReceiveCCDFeedback:
                        if (clientCCDTwo.StrBack != "")
                        {
                            //timer.Enabled = false;
                            ShowMSG($"{STwo_Product.SN}接收到046相机2反馈：{clientCCDTwo.StrBack}", Color.Black);
                            if (clientCCDTwo.StrBack == "1")
                            {
                                STwo_Product.VisionResultTwo = true;
                                //enSubStep = enSubWorkStep.ClearArriveSignal;
                            }
                            else if (clientCCDTwo.StrBack == "2")
                            {
                                STwo_Product.VisionResultTwo = false;
                                STwo_Product.PressTwo = 0;
                                ShowMSG($"{STwo_Product.SN}读取到压力2：{STwo_Product.PressTwo}", Color.Black);
                                //enSubStep = enSubWorkStep.TemporaryStorage;
                            }
                            else// if (clientCCDTwo.StrBack == "err")
                            {
                                ShowMSG($"046相机2断开连接,无法发送{STwo_Product.SN}！", Color.Red);
                                enSubStep = enSubWorkStep.CheckCCD;
                                break;
                            }
                            enSubStep = enSubWorkStep.ClearArriveSignal;
                        }
                        else if (!timer.Enabled)
                        {
                            ShowMSG($"{STwo_Product.SN}未收到相机2反馈！再次监听到位信号", Color.Red);
                            enSubStep = enSubWorkStep.ReadArriveSignal;
                            while (!CacheQueue.IsEmpty)
                            {
                                Product product = null;
                                CacheQueue.TryDequeue(out product);
                                list.Add(product);
                            }
                            CacheQueue.Enqueue(STwo_Product);
                            list.ForEach(t => CacheQueue.Enqueue(t));
                            list.Clear();
                            timer.Interval = 6000;
                            timer.Start();
                        }
                        break;
                    case enSubWorkStep.ClearArriveSignal:
                        PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[22]);
                        enSubStep = enSubWorkStep.ClearArriveSignalResult;
                        break;
                    case enSubWorkStep.ClearArriveSignalResult:
                        string result22 = PLCQueueClass.GetWriteResultFromList(OmronPLC.plc.plcAddresses[22].address);
                        if (result22 == "Success")
                        {
                            ShowMSG($"{STwo_Product.SN}写入PLC046相机2反馈信号", Color.Black);
                            enSubStep = STwo_Product.VisionResultTwo ? enSubWorkStep.ReadPressureTwo : enSubWorkStep.TemporaryStorage;
                            //if (STwo_Product.VisionResultTwo == true)
                            //{
                            //    enSubStep = enSubWorkStep.ReadPressureTwo;
                            //    ShowMSG($"{enSubStep}", Color.Black);
                            //}
                            //else
                            //{
                            //    enSubStep = enSubWorkStep.TemporaryStorage;
                            //    ShowMSG($"{enSubStep}", Color.Black);
                            //}
                        }
                        else if (result22 == "Fail")
                        {
                            ShowMSG($"{STwo_Product.SN}未能写入PLC读取046相机2反馈完成信号！重新写入", Color.Red);
                            enSubStep = enSubWorkStep.ClearArriveSignal;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enSubWorkStep.ReadPressureTwo:
                        string result9 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[9].address);
                        if (result9 == "True")
                        {
                            string pressure = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[6].address);
                            try
                            {
                                STwo_Product.PressTwo = float.Parse(pressure) / 100;
                                ShowMSG($"{STwo_Product.SN}读取到压力2：{STwo_Product.PressTwo}", Color.Black);
                                File.AppendAllText($"D:\\DATA\\压力\\{STwo_Product.SendCCDTime.Substring(0, 8)}.txt",
                                    STwo_Product.SN + "  压力2:" + STwo_Product.PressTwo.ToString() + "\r\n");
                                enSubStep = enSubWorkStep.ClearPTwoTriggerSignal;
                            }
                            catch
                            {
                                ShowMSG($"{STwo_Product.SN}读取压力2异常值：{pressure}！重新读取", Color.Red);
                                Thread.Sleep(2000);
                            }
                        }
                        //else
                        //{
                        //    turnStep = enSubWorkStep.ReadPressureTwo;
                        //    enSubStep = enSubWorkStep.ReadPressureThree;
                        //}
                        break;
                    case enSubWorkStep.ClearPTwoTriggerSignal:
                        PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[18]);
                        enSubStep = enSubWorkStep.ClearPTwoTriggerSignalResult;
                        break;
                    case enSubWorkStep.ClearPTwoTriggerSignalResult:
                        string result18 = PLCQueueClass.GetWriteResultFromList(OmronPLC.plc.plcAddresses[18].address);
                        if (result18 == "Success")
                        {
                            ShowMSG($"{STwo_Product.SN}写入PLC读取压力2完成信号", Color.Black);
                            enSubStep = enSubWorkStep.TemporaryStorage;
                        }
                        else if (result18 == "Fail")
                        {
                            ShowMSG($"{STwo_Product.SN}未能写入PLC读取压力2完成信号！重新写入", Color.Red);
                            enSubStep = enSubWorkStep.ClearPTwoTriggerSignal;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enSubWorkStep.TemporaryStorage:
                        int index = HoldPress.Temp_Product[0] == null ? 0 : 1;
                        HoldPress.Temp_Product[index] = STwo_Product.DeepCopy(STwo_Product);
                        ShowMSG($"{HoldPress.Temp_Product[index].SN}准备进入保压位1", Color.Black);
                        enSubStep = enSubWorkStep.ReadArriveSignal;
                        //if (!MESDataDefine.MESData.IsDisableSingle)
                        //{
                        //    STwo_Product.CurrentStation = 2;
                        //    MESProcess.MESQueue.Enqueue(STwo_Product);
                        //}
                        STwo_Product = null;
                        isIDLE = true;
                        Thread.Sleep(160);
                        break;

                }
            }
        }

    }
}
