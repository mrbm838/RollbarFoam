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
    public class CacheProcessOther
    {
        public static ConcurrentQueue<Product> CacheQueueOther = new ConcurrentQueue<Product>();
        public static NetSocket clientCCDOther = new NetSocket();
        OmronPLC omronOther = new OmronPLC(enMachine.M_047);
        System.Timers.Timer timer = new System.Timers.Timer();
        Action<String, Color> ShowMSG = null;
        public static Product SThree_Product = null;

        //enSubWorkStepOther turnStepOther;
        //enSubWorkStepOther oldStepOther;
        public static bool isIDLE = false;
        private List<Product> listOther = new List<Product>();
        public static bool bPLCTogetherOther = false;

        private enum enSubWorkStepOther
        {
            ReadArriveSignal,
            CheckCCDOther,
            ReceiveCCDOtherFeedback,
            ClearArriveSignal,
            ClearArriveSignalResult,
            ReadPressureFour,
            ClearPFourTriggerSignal,
            ClearPFourTriggerSignalResult,
            TemporaryStorage,
        }
        enSubWorkStepOther enSubStepOther;

        public CacheProcessOther(Action<String, Color> action)
        {
            ShowMSG = action;
            timer.Elapsed += Timer_Elapsed;
            Task.Run(() =>
            {
                if (Camera.ccd.enableOther)
                    clientCCDOther.Open(Camera.ccd.IPOther, Camera.ccd.PortOther);
                //clientCCDOther.SendMsg("abcdcd");
            });
            Thread thCache = new Thread(SubWorkCycleOther);
            thCache.IsBackground = true;
            thCache.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
        }

        private void SubWorkCycleOther()
        {
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(1);
                string result30 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[30].address);
                if (result30 == "True")
                {
                    bPLCTogetherOther = false;
                    continue;
                }
                else
                {
                    bPLCTogetherOther = true;
                }
                //if (enSubStepOther != oldStepOther)
                //{
                //File.AppendAllText(@"D:\DATA\CacheProcessOther.txt", enSubStepOther.ToString() + "\r\n");
                //    oldStepOther = enSubStepOther;
                //}
                switch (enSubStepOther)
                {
                    default:
                        ShowMSG($"{enSubStepOther}跳转异常", Color.Black);
                        Thread.Sleep(2000);
                        break;
                    case enSubWorkStepOther.ReadArriveSignal:
                        if (!HoldPress.isIDLE) break;
                        string result16 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[16].address);
                        if (result16 == "True")
                        {
                            //SThree_Product = new Product();
                            CacheQueueOther.TryDequeue(out SThree_Product);
                            if (SThree_Product != null)
                            {
                                ShowMSG($"{SThree_Product.SN}已到工位3", Color.Black);
                                enSubStepOther = enSubWorkStepOther.CheckCCDOther;
                            }
                            else
                            {
                                ShowMSG("未读取到到达工位3的HSG信息！", Color.Red);
                                Thread.Sleep(2000);
                            }
                        }
                        //else
                        //{
                        //    turnStepOther = enSubWorkStepOther.ReadArriveSignal;
                        //    enSubStepOther = enSubWorkStepOther.ReadPressureFive;
                        //}
                        break;
                    case enSubWorkStepOther.CheckCCDOther:
                        if (Camera.ccd.enableOther)
                        {
                            if (!clientCCDOther.connectOk)
                            {
                                clientCCDOther.Open(Camera.ccd.IPOther, Camera.ccd.PortOther);
                            }
                            if (clientCCDOther.connectOk)
                            {
                                timer.Enabled = false;
                                ShowMSG($"向047相机发送:{SThree_Product.SendCCDTime}_{SThree_Product.SN}", Color.Black);
                                //MESDataDefine.StrImagetimeS[0] = DateTime.Now.ToString("yyyyMMddHHmmss");
                                enSubStepOther = enSubWorkStepOther.ReceiveCCDOtherFeedback;
                                timer.Interval = 10000;
                                timer.Start();
                                clientCCDOther.SendMsg(SThree_Product.SendCCDTime + "_" + SThree_Product.SN);
                            }
                            else if (!timer.Enabled)
                            {
                                ShowMSG("未能成功连接到047相机！重新连接", Color.Red);
                                timer.Interval = 4000;
                                timer.Start();
                                break;
                            }
                        }
                        else
                        {
                            enSubStepOther = enSubWorkStepOther.ReadPressureFour;
                        }
                        break;
                    case enSubWorkStepOther.ReceiveCCDOtherFeedback:
                        if (clientCCDOther.StrBack != "")
                        {
                            //timer.Enabled = false;
                            ShowMSG($"{SThree_Product.SN}接收到047相机反馈：{clientCCDOther.StrBack}", Color.Black);
                            if (clientCCDOther.StrBack == "1")
                            {
                                SThree_Product.VisionResultThree = true;
                                //enSubStepOther = enSubWorkStepOther.ClearArriveSignal;
                            }
                            else if (clientCCDOther.StrBack == "2")
                            {
                                SThree_Product.VisionResultThree = false;
                                SThree_Product.PressFour = 0;
                                ShowMSG($"{SThree_Product.SN}读取到压力4：{SThree_Product.PressFour}", Color.Black);
                                //enSubStepOther = enSubWorkStepOther.TemporaryStorage;
                            }
                            else// if (clientCCDOther.StrBack == "err")
                            {
                                ShowMSG($"047相机断开连接,无法发送{SThree_Product.SN}！", Color.Red);
                                enSubStepOther = enSubWorkStepOther.CheckCCDOther;
                                break;
                            }
                            enSubStepOther = enSubWorkStepOther.ClearArriveSignal;
                        }
                        else if (!timer.Enabled)
                        {
                            ShowMSG($"{SThree_Product.SN}未收到相机反馈！再次监听到位信号", Color.Red);
                            enSubStepOther = enSubWorkStepOther.ReadArriveSignal;
                            while (!CacheQueueOther.IsEmpty)
                            {
                                Product product = null;
                                CacheQueueOther.TryDequeue(out product);
                                listOther.Add(product);
                            }
                            CacheQueueOther.Enqueue(SThree_Product);
                            listOther.ForEach(t => CacheQueueOther.Enqueue(t));
                            listOther.Clear();
                            timer.Interval = 10000;
                            timer.Start();
                        }
                        break;
                    case enSubWorkStepOther.ClearArriveSignal:
                        PLCQueueClass.AddToQueueOther(OmronPLC.plc.plcAddresses[23]);
                        enSubStepOther = enSubWorkStepOther.ClearArriveSignalResult;
                        break;
                    case enSubWorkStepOther.ClearArriveSignalResult:
                        string result23 = PLCQueueClass.GetWriteResultFromListOther(OmronPLC.plc.plcAddresses[23].address);
                        if (result23 == "Success")
                        {
                            ShowMSG($"{SThree_Product.SN}写入PLC047相机反馈信号", Color.Black);
                            enSubStepOther = SThree_Product.VisionResultThree ? enSubWorkStepOther.ReadPressureFour : enSubWorkStepOther.TemporaryStorage;
                        }
                        else if (result23 == "Fail")
                        {
                            ShowMSG($"{SThree_Product.SN}未能写入PLC读取047相机反馈完成信号！重新写入", Color.Red);
                            enSubStepOther = enSubWorkStepOther.ClearArriveSignal;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enSubWorkStepOther.ReadPressureFour:
                        string result13 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[13].address);
                        if (result13 == "True")
                        {
                            string pressure = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[11].address);
                            try
                            {
                                SThree_Product.PressFour = float.Parse(pressure) / 100;
                                ShowMSG($"{SThree_Product.SN}读取到压力4：{SThree_Product.PressFour}", Color.Black);
                                File.AppendAllText($"D:\\DATA\\压力\\{SThree_Product.SendCCDTime.Substring(0, 8)}.txt",
                                    SThree_Product.SN + "  压力4:" + SThree_Product.PressFour.ToString() + "\r\n");
                                enSubStepOther = enSubWorkStepOther.ClearPFourTriggerSignal;
                            }
                            catch
                            {
                                ShowMSG($"{SThree_Product.SN}读取压力4异常值：{pressure}！重新读取", Color.Red);
                                Thread.Sleep(2000);
                            }
                        }
                        //else
                        //{
                        //    turnStepOther = enSubWorkStepOther.ReadPressureFour;
                        //    enSubStepOther = enSubWorkStepOther.ReadPressureFive;
                        //}
                        break;
                    case enSubWorkStepOther.ClearPFourTriggerSignal:
                        PLCQueueClass.AddToQueueOther(OmronPLC.plc.plcAddresses[20]);
                        enSubStepOther = enSubWorkStepOther.ClearPFourTriggerSignalResult;
                        break;
                    case enSubWorkStepOther.ClearPFourTriggerSignalResult:
                        string result20 = PLCQueueClass.GetWriteResultFromListOther(OmronPLC.plc.plcAddresses[20].address);
                        if (result20 == "Success")
                        {
                            ShowMSG($"{SThree_Product.SN}写入PLC读取压力4完成信号", Color.Black);
                            enSubStepOther = enSubWorkStepOther.TemporaryStorage;
                        }
                        else if (result20 == "Fail")
                        {
                            ShowMSG($"{SThree_Product.SN}未能写入PLC读取压力4完成信号！重新写入", Color.Red);
                            enSubStepOther = enSubWorkStepOther.ClearPFourTriggerSignal;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enSubWorkStepOther.TemporaryStorage:
                        int indexOther = HoldPressOther.Temp_ProductOther[0] == null ? 0 : 1;
                        HoldPressOther.Temp_ProductOther[indexOther] = SThree_Product.DeepCopy(SThree_Product);
                        ShowMSG($"{HoldPressOther.Temp_ProductOther[indexOther].SN}准备进入保压位2", Color.Black);
                        enSubStepOther = enSubWorkStepOther.ReadArriveSignal;
                        //if (!MESDataDefine.MESData.IsDisableSingle)
                        //{
                        //    SThree_Product.CurrentStation = 4;
                        //    MESProcess.MESQueue.Enqueue(SThree_Product);
                        //}
                        SThree_Product = null;
                        isIDLE = true;
                        Thread.Sleep(160);
                        break;

                }
            }
        }
    }
}
