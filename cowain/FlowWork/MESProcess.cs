using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using cowain.MES;
using cowain.Comm;
using System.IO;

namespace cowain.FlowWork
{
    public class MESProcess
    {
        public static ConcurrentQueue<Product> MESQueue = new ConcurrentQueue<Product>();
        public static Product Upload_Product = null;
        System.Timers.Timer timer = new System.Timers.Timer();
        Action<String, Color> ShowMSG = null;
        LXPOSTClass lxPostClass = LXPOSTClass.CreateInstance();
        private string strMesResult = String.Empty;

        string dirName = @"D:\DATA\压力";
        string fileName = string.Format(@"D:\DATA\压力\{0}.csv", DateTime.Now.ToString("yyyy-MM-dd"));

        public delegate void DelFlashSN(string UC, string SN);
        public static event DelFlashSN flashUCSN;
        private enum enMESWorkStep
        {
            JudgeQueueCount,
            PrintPressureToExcel,
            JudgeDisablePDCAorNot,
            UploadMESToPass,

            JudgeUploadType,
            UploadStationSingle,

            UploadMESWithoutPass,
            UploadPDCA,
            UploadPDCAResult,
        }
        enMESWorkStep enMESStep;

        public MESProcess(Action<String, Color> action)
        {
            ShowMSG = action;
            timer.Elapsed += Timer_Elapsed;
            Thread thHPress = new Thread(MESCycle);
            thHPress.IsBackground = true;
            thHPress.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
        }

        private void MESCycle()
        {
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(1);
                switch (enMESStep)
                {
                    case enMESWorkStep.JudgeQueueCount:
                        //enMESStep = enMESWorkStep.PrintPressureToExcel;
                        if (MESQueue.Count > 0)
                        {
                            MESQueue.TryDequeue(out Upload_Product);
                            //if (MESDataDefine.MESData.IsDisableSingle)
                                enMESStep = enMESWorkStep.PrintPressureToExcel;
                            //else
                            //    enMESStep = enMESWorkStep.JudgeUploadType;
                        }
                        break;
                    case enMESWorkStep.PrintPressureToExcel:
                        string data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}\n",
                                                    Upload_Product.EnterTime, Upload_Product.ExitTime,
                                                    Upload_Product.SN,
                                                    Upload_Product.PressOne, Upload_Product.PressTwo,
                                                    Upload_Product.PressThree, Upload_Product.PressFour,
                                                    Upload_Product.PressFive, Upload_Product.VisionResultOne,
                                                    Upload_Product.VisionResultTwo, Upload_Product.VisionResultThree);

                        SaveExcelFile(data);
                        enMESStep = enMESWorkStep.JudgeDisablePDCAorNot;
                        break;
                    case enMESWorkStep.JudgeDisablePDCAorNot:
                        flashUCSN(Upload_Product.UC, Upload_Product.SN);
                        if (MESDataDefine.MESData.IsDisableMES)
                        {
                            ShowMSG($"禁用MES，{Upload_Product.SN}流程结束", Color.Black);
                            enMESStep = enMESWorkStep.JudgeQueueCount;
                            break;
                        }
                        //if (MESDataDefine.MESData.IsDisablePDCA)
                        //{
                        enMESStep = enMESWorkStep.UploadMESToPass;                        // 上传MES过站
                        strMesResult = lxPostClass.MES_SendPressure(Upload_Product);
                        //}
                        //else
                        //{
                        //    enMESStep = enMESWorkStep.UploadMESWithoutPass;                   // 上传MES&PDCA
                        //    strMesResult = lxPostClass.MesSend(Upload_Product.SN, Upload_Product.UC, "REC");
                        //}
                        //timer.Interval = 2000;
                        //timer.Start();
                        break;
                    case enMESWorkStep.UploadMESToPass:
                        if (strMesResult.Contains("0 SFC_OK"))
                        {
                            //timer.Enabled = false;
                            ShowMSG($"{Upload_Product.SN}上传MES成功", Color.Black);
                            // Hive
                            //HIVE.HIVEInstance.HiveSendMACHINEDATA(Upload_Product.SN,
                            //                                      Upload_Product.UC,
                            //                                      Upload_Product.SN,
                            //                                      currentUpload,
                            //                                      true);
                            enMESStep = enMESWorkStep.JudgeQueueCount;                         // 返回，读取PLC信号
                            Upload_Product = null;
                        }
                        else// if (!timer.Enabled)
                        {
                            ShowMSG($"{Upload_Product.SN}上传MES失败，再次上传", Color.Red);
                            PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[4]);
                            enMESStep = enMESWorkStep.JudgeDisablePDCAorNot;
                        }
                        break;

                    #region 单个工位上传
                    case enMESWorkStep.JudgeUploadType:
                        switch (Upload_Product.CurrentStation)
                        {
                            case 1:
                                strMesResult = lxPostClass.MES_SendPressureVisionResult(Upload_Product.SN, Upload_Product.PressOne.ToString("f2"), Upload_Product.VisionResultOne.ToString());
                                break;
                            case 2:
                                strMesResult = lxPostClass.MES_SendPressureVisionResult(Upload_Product.SN, Upload_Product.PressTwo.ToString("f2"), Upload_Product.VisionResultTwo.ToString());
                                break;
                            case 3:
                                strMesResult = lxPostClass.MES_SendPressureSingle(Upload_Product.SN, Upload_Product.PressThree.ToString("f2"));
                                break;
                            case 4:
                                strMesResult = lxPostClass.MES_SendPressureVisionResult(Upload_Product.SN, Upload_Product.PressFour.ToString("f2"), Upload_Product.VisionResultThree.ToString());
                                break;
                            case 5:
                                strMesResult = lxPostClass.MES_SendPressureSingle(Upload_Product.SN, Upload_Product.PressFive.ToString("f2"));
                                break;
                        }
                        enMESStep = enMESWorkStep.UploadStationSingle;
                        break;
                    case enMESWorkStep.UploadStationSingle:
                        if (strMesResult.Contains("0 SFC_OK"))
                        {
                            //timer.Enabled = false;
                            ShowMSG($"{Upload_Product.SN}工位{Upload_Product.CurrentStation}上传MES成功", Color.Black);
                            enMESStep = enMESWorkStep.JudgeQueueCount;                         // 返回，读取PLC信号
                            Upload_Product = null;
                        }
                        else// if (!timer.Enabled)
                        {
                            ShowMSG($"{Upload_Product.SN}工位{Upload_Product.CurrentStation}上传MES失败，再次上传", Color.Red);
                            PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[4]);
                            enMESStep = enMESWorkStep.JudgeDisablePDCAorNot;
                        }
                        break;
                    #endregion
                    #region 上传PDCA
                    case enMESWorkStep.UploadMESWithoutPass:
                        if (strMesResult.Contains("0 SFC_OK"))
                        {
                            timer.Enabled = false;
                            ShowMSG($"{Upload_Product.SN}上传MES成功（未过站），开始上传PDCA", Color.Black);
                            enMESStep = enMESWorkStep.UploadPDCA;
                        }
                        else if (!timer.Enabled)
                        {
                            ShowMSG($"{Upload_Product.SN}上传MES失败，再次上传", Color.Red);
                            PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[4]);
                            enMESStep = enMESWorkStep.JudgeDisablePDCAorNot;
                        }
                        break;
                    case enMESWorkStep.UploadPDCA:
                        lxPostClass.PDCASend("1111", "2222", "2022-05");
                        //lxPostClass.PDCASend(Upload_Product.SN, Upload_Product.UC, MESDataDefine.StrImagetimeS[0]);
                        enMESStep = enMESWorkStep.UploadPDCAResult;
                        timer.Interval = 4000;
                        timer.Start();
                        break;
                    case enMESWorkStep.UploadPDCAResult:
                        string strPDCAResult = LXPOSTClass.clientOfPDCA.StrBack;
                        if (strPDCAResult.Contains("copy") && !strPDCAResult.Contains("err") && !strPDCAResult.Contains("bad"))
                        {
                            timer.Enabled = false;
                            ShowMSG($"{Upload_Product.SN}上传PDCA成功", Color.Black);
                            // Hive
                            //HIVE.HIVEInstance.HiveSendMACHINEDATA(Upload_Product.SN,
                            //                                      Upload_Product.UC,
                            //                                      Upload_Product.SN,
                            //                                      currentUpload,
                            //                                      true);
                            enMESStep = enMESWorkStep.JudgeQueueCount;                         // 返回，读取PLC信号
                        }
                        else if (!timer.Enabled || strPDCAResult.Contains("err") || strPDCAResult.Contains("bad"))
                        {
                            timer.Enabled = false;
                            string ErrorCode = String.Empty;
                            if (timer.Enabled == false) { ErrorCode = "PDCA反馈超时，"; }
                            if (strPDCAResult.Contains("err")) { ErrorCode += "PDCA反馈包含【err】，"; }
                            if (strPDCAResult.Contains("bad")) { ErrorCode += "PDCA反馈包含【bad】"; }
                            ShowMSG($"{Upload_Product.SN}上传PDCA失败，错误信息：{ErrorCode}，再次上传", Color.Red);
                            PLCQueueClass.AddToQueue(OmronPLC.plc.plcAddresses[4]);
                            enMESStep = enMESWorkStep.UploadPDCA;
                        }
                        break;
                        #endregion

                }
            }
        }

        private void SaveExcelFile(string content)
        {
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            if (!File.Exists(fileName))
            {
                string title = string.Format("入料时间,出料时间,SN,压力1,压力2,压力3,压力4,压力5,视觉结果1,视觉结果2,视觉结果3\n");
                byte[] bytesTitle = System.Text.Encoding.UTF8.GetBytes(title);
                using (FileStream fs = new FileStream(fileName, FileMode.Append))
                {
                    fs.Write(bytesTitle, 0, bytesTitle.Length);
                }
            }
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            using (FileStream fs = new FileStream(fileName, FileMode.Append))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        //public static void AddToQueue(Product product, enMESStep step)
        //{
        //    Product newProduct = new Product()
        //    {
        //        UC = product.UC,
        //        SN = product.SN,
        //        MESStep = step
        //    };
        //    MESQueue.Enqueue(newProduct);
        //}

        //public static string RemoveFromQueue(LXPOSTClass lxPostClass, string order)
        //{
        //    //Product product = null;
        //    MESQueue.TryDequeue(out currentProduct);
        //    string StrReturn = string.Empty;
        //    switch (currentProduct.MESStep)
        //    {
        //        case enMESStep.GetSN:
        //            StrReturn =  lxPostClass.MES_SendUCGetSn(currentProduct.UC);
        //            break;
        //        case enMESStep.CheckUOP:
        //            StrReturn = lxPostClass.MesUOP(currentProduct.SN, currentProduct.UC);
        //            break;
        //        case enMESStep.UploadMES:
        //            StrReturn = lxPostClass.MesSend(currentProduct.SN, currentProduct.UC, order);
        //            break;
        //        case enMESStep.UploadPDCA:
        //            lxPostClass.PDCASend(currentProduct.SN, currentProduct.UC, order);
        //            break;
        //    }
        //    return StrReturn;
        //}
    }
}