using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cowain.MES;
using System.Drawing;
using System.Threading;
using cowain.Comm;

namespace cowain.FlowWork
{
    public class HPressProductClass
    {
        public static Product TempSTO_Product = null;
        private Product HPress_Product = null;
        System.Timers.Timer timer = new System.Timers.Timer();
        Action<String, Color> ShowMSG = null;

        private enum enHPWorkStep
        {
            ReadArriveSignal,
            ReadSignalHPOfTrigger,
            ReadMaxPressureThree,
            EnterMESQueue
        }
        enHPWorkStep enHPStep;

        public HPressProductClass(Action<String, Color> action)
        {
            ShowMSG = action;
            timer.Elapsed += Timer_Elapsed;
            Thread thHPress = new Thread(HPressCycle);
            thHPress.IsBackground = true;
            thHPress.Start();

        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
        }

        private void HPressCycle()
        {
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(1);
                switch (enHPStep)
                {
                    default:
                    case enHPWorkStep.ReadArriveSignal:
                        string result12 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[12].address);
                        if (result12 == "1")
                        {
                            HPress_Product = TempSTO_Product.DeepCopy(TempSTO_Product);
                            TempSTO_Product = null;
                            if (HPress_Product != null)
                            {
                                ShowMSG($"{HPress_Product.SN}已到保压位", Color.Black);
                                enHPStep = enHPWorkStep.ReadSignalHPOfTrigger;
                            }
                            else
                            {
                                ShowMSG($"未读取到到达保压位的HSG信息！", Color.Black);
                                Thread.Sleep(1000);
                            }
                        }
                        break;
                    case enHPWorkStep.ReadSignalHPOfTrigger:
                        string result10 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[10].address);
                        if (result10 == "1")
                        {
                            OmronPLC.bRecodeT = true;
                            ShowMSG($"{HPress_Product.SN}开始读取压力3的信号", Color.Black);
                            enHPStep = enHPWorkStep.ReadMaxPressureThree;
                        }
                        break;
                    case enHPWorkStep.ReadMaxPressureThree:
                        string resultStop10 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[10].address);
                        if (resultStop10 == "0")
                        {
                            OmronPLC.bRecodeT = false;
                            HPress_Product.PressT = OmronPLC.MaxPressT;
                            ShowMSG($"{HPress_Product.SN}读取到压力3最大值为：{HPress_Product.PressT}", Color.Black);
                            enHPStep = enHPWorkStep.EnterMESQueue;
                        }
                        break;
                    case enHPWorkStep.EnterMESQueue:
                        OmronPLC.MaxPressT = 0;
                        //HPress_Product.MESStep = enMESStep.UploadPressure;
                        MESProcess.MESQueue.Enqueue(HPress_Product);
                        //Product ULoad_Product = HPress_Product.DeepCopy(HPress_Product);
                        //ULoad_Product.MESStep = enMESStep.UploadPictures;
                        //MESQueueClass.MESQueue.Enqueue(ULoad_Product);
                        ShowMSG($"{HPress_Product.SN}准备上传MES", Color.Black);
                        enHPStep = enHPWorkStep.ReadArriveSignal;
                        break;
                }
            }
        }
    }
}
