using cowain.Comm;
using cowain.MES;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cowain.FlowWork
{
    class HoldPressOther
    {
        public static Product[] Temp_ProductOther = new Product[2];
        System.Timers.Timer timer = new System.Timers.Timer();
        Action<String, Color> ShowMSG = null;

        public static bool isIDLE = false;
        public static Product HPTwo_Product = null;

        private enum enHoldPressStepOther
        {
            ReadArriveSignal,
            ReadPressureFive,
            ClearPFiveTriggerSignal,
            ClearPFiveTriggerSignalResult,
            EnterMESQueue
        }
        enHoldPressStepOther enHPStepOther;

        public HoldPressOther(Action<String, Color> action)
        {
            ShowMSG = action;
            timer.Elapsed += Timer_Elapsed;
            Thread thCache = new Thread(HoldPressCycleOther);
            thCache.IsBackground = true;
            thCache.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
        }

        private void HoldPressCycleOther()
        {
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(1);
                string result30 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[30].address);
                if (result30 == "True") continue;
                switch (enHPStepOther)
                {
                    default:
                        ShowMSG($"{enHPStepOther}跳转异常", Color.Black);
                        Thread.Sleep(2000);
                        break;
                    case enHoldPressStepOther.ReadArriveSignal:
                        if (!CacheProcessOther.isIDLE) break;
                        string result32 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[32].address);
                        if (result32 == "True")
                        {
                            if (Temp_ProductOther[0] == null)
                            {
                                ShowMSG("未读取到到达保压位2的HSG信息！", Color.Red);
                                enHPStepOther = enHoldPressStepOther.ReadArriveSignal;
                                Thread.Sleep(2000);
                            }
                            else
                            {
                                HPTwo_Product = Temp_ProductOther[0].DeepCopy(Temp_ProductOther[0]);
                                Temp_ProductOther[0] = null;
                                if (Temp_ProductOther[1] != null)
                                {
                                    Temp_ProductOther[0] = Temp_ProductOther[1];
                                    Temp_ProductOther[1] = null;
                                }
                                ShowMSG($"{HPTwo_Product.SN}已到保压位2", Color.Black);
                                if (HPTwo_Product.VisionResultThree)
                                {
                                    OmronPLC.plc.plcAddresses[34].lengthOrData = "1";
                                    PLCQueueClass.AddToQueueOther(OmronPLC.plc.plcAddresses[34]);
                                    ShowMSG($"{HPTwo_Product.SN}保压位2发送可保压信号·1", Color.Black);
                                    enHPStepOther = enHoldPressStepOther.ReadPressureFive;
                                }
                                else
                                {
                                    OmronPLC.plc.plcAddresses[34].lengthOrData = "2";
                                    PLCQueueClass.AddToQueueOther(OmronPLC.plc.plcAddresses[34]);
                                    ShowMSG($"{HPTwo_Product.SN}保压位2发送不保压信号·2", Color.Black);
                                    enHPStepOther = enHoldPressStepOther.EnterMESQueue;
                                }
                            }
                        }
                        break;
                    case enHoldPressStepOther.ReadPressureFive:
                        string result14 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[14].address);
                        if (result14 == "True")
                        {
                            string pressure = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[12].address);
                            try
                            {
                                HPTwo_Product.PressFive = float.Parse(pressure) / 100;
                                ShowMSG($"{HPTwo_Product.SN}读取到压力5：{HPTwo_Product.PressFive}", Color.Black);
                                File.AppendAllText($"D:\\DATA\\压力\\{HPTwo_Product.SendCCDTime.Substring(0, 8)}.txt",
                                    HPTwo_Product.SN + "  压力5:" + HPTwo_Product.PressFive.ToString() + "\r\n");
                                enHPStepOther = enHoldPressStepOther.ClearPFiveTriggerSignal;
                            }
                            catch
                            {
                                ShowMSG($"{HPTwo_Product.SN}读取压力5异常值：{pressure}！重新读取", Color.Red);
                                Thread.Sleep(2000);
                            }
                        }
                        //else
                        //{
                        //    enHPStepOther = turnStepOther;
                        //}
                        break;
                    case enHoldPressStepOther.ClearPFiveTriggerSignal:
                        PLCQueueClass.AddToQueueOther(OmronPLC.plc.plcAddresses[21]);
                        enHPStepOther = enHoldPressStepOther.ClearPFiveTriggerSignalResult;
                        break;
                    case enHoldPressStepOther.ClearPFiveTriggerSignalResult:
                        string result21 = PLCQueueClass.GetWriteResultFromListOther(OmronPLC.plc.plcAddresses[21].address);
                        if (result21 == "Success")
                        {
                            ShowMSG($"{HPTwo_Product.SN}写入PLC读取压力5完成信号", Color.Black);
                            enHPStepOther = enHoldPressStepOther.EnterMESQueue;
                        }
                        else if (result21 == "Fail")
                        {
                            ShowMSG($"{HPTwo_Product.SN}未能写入PLC读取压力5完成信号！重新写入", Color.Red);
                            enHPStepOther = enHoldPressStepOther.ClearPFiveTriggerSignal;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enHoldPressStepOther.EnterMESQueue:
                        HPTwo_Product.ExitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        Product newProduct = HPTwo_Product.DeepCopy(HPTwo_Product);
                        MESProcess.MESQueue.Enqueue(newProduct);
                        ShowMSG($"{HPTwo_Product.SN}进入MES队列", Color.Black);
                        enHPStepOther = enHoldPressStepOther.ReadArriveSignal;
                        //if (!MESDataDefine.MESData.IsDisableSingle)
                        //{
                        //    HPTwo_Product.CurrentStation = 5;
                        //    MESProcess.MESQueue.Enqueue(HPTwo_Product);
                        //}
                        HPTwo_Product = null;
                        isIDLE = true;
                        Thread.Sleep(200);
                        break;
                }
            }
        }

    }
}
