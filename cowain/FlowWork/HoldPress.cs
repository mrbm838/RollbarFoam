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
    class HoldPress
    {
        public static Product[] Temp_Product = new Product[2];
        System.Timers.Timer timer = new System.Timers.Timer();
        Action<String, Color> ShowMSG = null;

        public static bool isIDLE = false;
        public static Product HPOne_Product = null;

        private enum enHoldPressStep
        {
            ReadArriveSignal,
            ReadPressureThree,
            ClearPThreeTriggerSignal,
            ClearPThreeTriggerSignalResult,
            EnterCacheQueueOther

        }
        enHoldPressStep enHPStep;

        public HoldPress(Action<String, Color> action)
        {
            ShowMSG = action;
            timer.Elapsed += Timer_Elapsed;
            Thread thCache = new Thread(HoldPressCycle);
            thCache.IsBackground = true;
            thCache.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
        }

        private void HoldPressCycle()
        {
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(1);
                string result29 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[29].address);
                if (result29 == "True") continue;
                switch (enHPStep)
                {
                    default:
                        ShowMSG($"{enHPStep}跳转异常", Color.Black);
                        Thread.Sleep(2000);
                        break;
                    case enHoldPressStep.ReadArriveSignal:
                        if (!CacheProcess.isIDLE) break;
                        string result31 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[31].address);
                        if (result31 == "True")
                        {
                            if (Temp_Product[0] == null)
                            {
                                ShowMSG("未读取到到达保压位1的HSG信息！", Color.Red);
                                enHPStep = enHoldPressStep.ReadArriveSignal;
                                Thread.Sleep(2000);
                            }
                            else
                            {
                                HPOne_Product = Temp_Product[0].DeepCopy(Temp_Product[0]);
                                Temp_Product[0] = null;
                                if (Temp_Product[1] != null)
                                {
                                    Temp_Product[0] = Temp_Product[1];
                                    Temp_Product[1] = null;
                                }
                                ShowMSG($"{HPOne_Product.SN}已到保压位1", Color.Black);
                                if (HPOne_Product.VisionResultOne || HPOne_Product.VisionResultTwo)
                                {
                                    OmronPLC.plc.plcAddresses[33].lengthOrData = "1";
                                    PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[33]);
                                    ShowMSG($"{HPOne_Product.SN}保压位1发送可保压信号·1", Color.Black);
                                    enHPStep = enHoldPressStep.ReadPressureThree;
                                }
                                else
                                {
                                    OmronPLC.plc.plcAddresses[33].lengthOrData = "2";
                                    PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[33]);
                                    ShowMSG($"{HPOne_Product.SN}保压位1发送不保压信号·2", Color.Black);
                                    enHPStep = enHoldPressStep.EnterCacheQueueOther;
                                }
                            }
                        }
                        break;
                    case enHoldPressStep.ReadPressureThree:
                        string result10 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[10].address);
                        if (result10 == "True")
                        {
                            string pressure = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[7].address);
                            try
                            {
                                HPOne_Product.PressThree = float.Parse(pressure) / 100;
                                ShowMSG($"{HPOne_Product.SN}读取到压力3：{HPOne_Product.PressThree}", Color.Black);
                                File.AppendAllText($"D:\\DATA\\压力\\{HPOne_Product.SendCCDTime.Substring(0, 8)}.txt",
                                    HPOne_Product.SN + "  压力3:" + HPOne_Product.PressThree.ToString() + "\r\n");
                                enHPStep = enHoldPressStep.ClearPThreeTriggerSignal;
                            }
                            catch
                            {
                                ShowMSG($"{HPOne_Product.SN}读取压力3异常值：{pressure}！重新读取", Color.Red);
                                Thread.Sleep(2000);
                            }
                        }
                        //else
                        //{
                        //    enHPStep = enHoldPressStep.ReadPressureThree;
                        //}
                        break;
                    case enHoldPressStep.ClearPThreeTriggerSignal:
                        PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[19]);
                        enHPStep = enHoldPressStep.ClearPThreeTriggerSignalResult;
                        break;
                    case enHoldPressStep.ClearPThreeTriggerSignalResult:
                        string result19 = PLCQueueClass.GetWriteResultFromList(OmronPLC.plc.plcAddresses[19].address);
                        if (result19 == "Success")
                        {
                            ShowMSG($"{HPOne_Product.SN}写入PLC读取压力3完成信号", Color.Black);
                            enHPStep = enHoldPressStep.EnterCacheQueueOther;
                        }
                        else if (result19 == "Fail")
                        {
                            ShowMSG($"{HPOne_Product.SN}未能写入PLC读取压力3完成信号！重新写入", Color.Red);
                            enHPStep = enHoldPressStep.ClearPThreeTriggerSignal;
                            Thread.Sleep(2000);
                        }
                        break;
                    case enHoldPressStep.EnterCacheQueueOther:
                        Product newProduct = HPOne_Product.DeepCopy(HPOne_Product);
                        CacheProcessOther.CacheQueueOther.Enqueue(newProduct);
                        ShowMSG($"{HPOne_Product.SN}进入047缓存队列", Color.Black);
                        enHPStep = enHoldPressStep.ReadArriveSignal;
                        //if (!MESDataDefine.MESData.IsDisableSingle)
                        //{
                        //    HPOne_Product.CurrentStation = 3;
                        //    MESProcess.MESQueue.Enqueue(HPOne_Product);
                        //}
                        HPOne_Product = null;
                        isIDLE = true;
                        Thread.Sleep(200);
                        break;
                }
            }
        }

    }
}
