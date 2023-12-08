using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using cowain.MES;
using cowain.Comm;
using System.Drawing;
using cowain.FormView;

namespace cowain.FlowWork
{
    internal class MESPush
    {
        System.Timers.Timer tmDelay = new System.Timers.Timer();
        LXPOSTClass lxPostClass = LXPOSTClass.CreateInstance();
        OmronPLC omron = null;
        Action<string, Color> ShowMSG = null;
        NetSocket clientOfCCD = null;

        private string strMesResult = string.Empty;
        private string rwResult = string.Empty;
        private object locker = new object();
        public bool isIDLE = true;

        private string SN = string.Empty;
        private string Barcode = string.Empty;

        public MESPush(OmronPLC omron, Action<string, Color> action)
        {
            this.omron = omron;
            ShowMSG = action;
        }

        private enum enMESStep
        {
            Start,

            JudgeCodeType,
            CheckMESandPDCA,
            CheckOperator,

            SendUCToMES,
            ReceiveSNCode,

            CheckCCD,
            ReceiveCCDFeedback,
            UploadMesUOP,
            ReceiveMesUOPResult,

            ReadMachineCompletedSignal,
            ReadMachineCompletedSignalResult,

            JudgeDisablePDCAorNot,
            UploadMESToPass,
            UploadMESWithoutPass,
            UploadPDCA,

            Completed
        }
        enMESStep enStep;

        public void StartCycle(string barcode)
        {
            this.Barcode = barcode;
            Thread thPush = new Thread(MESCycle);
            thPush.IsBackground = true;
            thPush.Start();
            tmDelay.Elapsed += TmDelay_Elapsed;
        }

        private void TmDelay_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            tmDelay.Stop();
        }

        private void MESCycle()
        {
            while (true)
            {
                Thread.Sleep(1);
                rwResult = string.Empty;
                if (omron.plcQueue.Count > 0)
                {
                    lock (locker)
                    {
                        rwResult = omron.RemoveFromQueue(omron.plcQueue.Dequeue());
                    }
                }
                switch (enStep)
                {
                    case enMESStep.Start:
                        isIDLE = false;
                        enStep = enMESStep.CheckMESandPDCA;
                        break;
                    case enMESStep.JudgeCodeType:
                        if (SHIJEReader.reader.QRCode == "产品码")
                        {
                            SN = Barcode;
                            enStep = enMESStep.CheckCCD;
                        }
                        else if (SHIJEReader.reader.QRCode == "工装码")
                        {
                            enStep = enMESStep.CheckMESandPDCA;
                        }
                        break;
                    case enMESStep.CheckMESandPDCA:
                        if (MESDataDefine.MESData.IsDisableMES && MESDataDefine.MESData.IsDisablePDCA)
                        {
                            enStep = enMESStep.CheckCCD;
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
                        enStep = enMESStep.CheckOperator;
                        break;
                    case enMESStep.CheckOperator:
                        //if (!MESDataDefine.MESData.IsDisableMES)
                        //{
                        //    strMesResult = String.Empty;
                        // if
                        //ShowMSG("作业员已登录", Color.Black);
                        enStep = enMESStep.SendUCToMES;
                        // else
                        //ShowMSG("作业员未登录！", Color.Black);
                        //enStep = enMESStep.CheckMESandPDCA;
                        //}
                        //else
                        //{
                        //enStep = enMESStep.CheckCCD;
                        //}
                        break;
                    case enMESStep.SendUCToMES:
                        enStep = enMESStep.ReceiveSNCode;
                        tmDelay.Interval = 2000;
                        tmDelay.Start();
                        break;
                    case enMESStep.ReceiveSNCode:
                        string strSN = lxPostClass.MES_SendUCGetSn(Barcode, 0);
                        if (strSN != "")
                        {
                            tmDelay.Enabled = false;
                            SN = SubSN(strSN);
                            ShowMSG($"接收到UC码：{SN}", Color.Black);
                            enStep = enMESStep.CheckCCD;
                            tmDelay.Interval = 2000;
                            tmDelay.Start();
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("未接收到SN码！再次上传", Color.Red);
                            omron.AddToQueue(OmronPLC.plc.addrs[4, 0]);
                            enStep = enMESStep.SendUCToMES;
                        }
                        break;
                    case enMESStep.CheckCCD:
                        if (Camera.ccd.enable)
                        {
                            if (clientOfCCD == null)
                            {
                                clientOfCCD = new NetSocket();
                            }
                            if (!clientOfCCD.connectOk)
                            {
                                clientOfCCD.Open(Camera.ccd.IP, Camera.ccd.Port);
                            }
                            if (clientOfCCD.connectOk)
                            {
                                tmDelay.Enabled = false;
                                ShowMSG("成功连接到相机", Color.Black);
                                //MESDataDefine.StrImagetimeS[0] = DateTime.Now.ToString("yyyyMMddHHmmss");
                                enStep = enMESStep.ReceiveCCDFeedback;
                                tmDelay.Interval = 2000;
                                tmDelay.Start();
                            }
                            else if (!tmDelay.Enabled)
                            {
                                ShowMSG("未能成功连接到相机！重新连接", Color.Red);
                                tmDelay.Interval = 4000;
                                tmDelay.Start();
                                break;
                            }
                        }
                        else
                        {
                            enStep = enMESStep.UploadMesUOP;
                        }
                        break;
                    case enMESStep.ReceiveCCDFeedback:
                        clientOfCCD.SendMsg(SN);
                        if (clientOfCCD.StrBack != "")
                        {
                            tmDelay.Enabled = false;
                            ShowMSG($"接收到相机反馈：{clientOfCCD.StrBack}", Color.Black);
                            enStep = enMESStep.UploadMesUOP;
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("未收到相机反馈！再次上传", Color.Red);
                            enStep = enMESStep.CheckCCD;
                            tmDelay.Interval = 4000;
                            tmDelay.Start();
                        }
                        break;
                    case enMESStep.UploadMesUOP:
                        if (MESDataDefine.MESData.IsDisableMES)
                        {
                            enStep = enMESStep.Completed;
                        }
                        else
                        {
                            enStep = enMESStep.ReceiveMesUOPResult;
                            tmDelay.Interval = 4000;
                            tmDelay.Start();
                        }
                        break;
                    case enMESStep.ReceiveMesUOPResult:
                        strMesResult = lxPostClass.MesUOP(SN, Barcode, 0);
                        if (strMesResult.Contains("check=OK"))
                        {
                            tmDelay.Enabled = false;
                            ShowMSG("上传MesUOP结果OK，监听作料完成信号", Color.Black);
                            enStep = enMESStep.ReadMachineCompletedSignal;
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("未收到上传MesUOP的结果！再次上传", Color.Red);
                            omron.AddToQueue(OmronPLC.plc.addrs[4, 0]);
                            enStep = enMESStep.ReceiveMesUOPResult;
                            tmDelay.Interval = 4000;
                            tmDelay.Start();
                        }
                        break;
                    case enMESStep.ReadMachineCompletedSignal:
                        omron.AddToQueue(OmronPLC.plc.addrs[3, 0]);
                        enStep = enMESStep.ReadMachineCompletedSignalResult;
                        break;
                    case enMESStep.ReadMachineCompletedSignalResult:
                        //string resultOfCompleted = omron.ReadSignals(OmronPLC.plc.addrs[3, 0], OmronPLC.plc.addrs[3, 1], OmronPLC.plc.addrs[3, 2]);
                        if (rwResult == "1")
                        {
                            ShowMSG("工作已完成，开始上传MES数据", Color.Black);
                            enStep = enMESStep.JudgeDisablePDCAorNot;
                            tmDelay.Interval = 4000;
                            tmDelay.Start();
                        }
                        else
                        {
                            enStep = enMESStep.ReadMachineCompletedSignal;
                            Thread.Sleep(500);
                        }
                        break;
                    case enMESStep.JudgeDisablePDCAorNot:
                        if (MESDataDefine.MESData.IsDisablePDCA)
                        {
                            enStep = enMESStep.UploadMESToPass;
                        }
                        else
                        {
                            enStep = enMESStep.UploadMESWithoutPass;
                        }
                        tmDelay.Interval = 4000;
                        tmDelay.Start();
                        break;
                    case enMESStep.UploadMESToPass:
                        strMesResult = lxPostClass.MesSendADD(SN, Barcode, 0, 0, "ADD");
                        if (strMesResult == "0 SFC_OK")
                        {
                            tmDelay.Enabled = false;
                            MESDataDefine.MesUploadStatusS[0] = MESDataDefine.MesStatus.UpLoadMesOK;
                            ShowMSG("上传MES成功", Color.Black);
                            enStep = enMESStep.Completed;
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("上传MES失败，再次上传", Color.Red);
                            omron.AddToQueue(OmronPLC.plc.addrs[4, 0]);
                            tmDelay.Interval = 4000;
                            tmDelay.Start();
                        }
                        break;
                    case enMESStep.UploadMESWithoutPass:
                        strMesResult = lxPostClass.MesSendADD(SN, Barcode, 0, 0, "REC");
                        if (strMesResult == "0 SFC_OK")
                        {
                            tmDelay.Enabled = false;
                            ShowMSG("上传MES成功（未过站）", Color.Black);
                            enStep = enMESStep.UploadPDCA;
                            tmDelay.Interval = 4000;
                            tmDelay.Start();
                        }
                        else if (!tmDelay.Enabled)
                        {
                            ShowMSG("上传MES失败，再次上传", Color.Red);
                            omron.AddToQueue(OmronPLC.plc.addrs[4, 0]);
                            tmDelay.Interval = 4000;
                            tmDelay.Start();
                        }
                        break;
                    case enMESStep.UploadPDCA:
                        ShowMSG("开始上传PDCA", Color.Black);
                        lxPostClass.PDCASend(SN, Barcode, MESDataDefine.StrImagetimeS[0], 0);
                        string strPDCAResult = LXPOSTClass.socketLeft.StrBack;
                        if (strPDCAResult.Contains("copy") && !strPDCAResult.Contains("err") && !strPDCAResult.Contains("bad"))
                        {
                            tmDelay.Enabled = false;
                            //MESDataDefine.MesUploadStatusS[0] = MESDataDefine.MesStatus.UpLoadMesOK;
                            ShowMSG("上传PDCA成功", Color.Black);
                            enStep = enMESStep.Completed;
                        }
                        else if (!tmDelay.Enabled || strPDCAResult.Contains("err") || strPDCAResult.Contains("bad"))
                        {
                            tmDelay.Enabled = false;
                            string ErrorCode = String.Empty;
                            if (tmDelay.Enabled == false) { ErrorCode = "PDCA反馈超时，"; }
                            if (strPDCAResult.Contains("err")) { ErrorCode += "PDCA反馈包含【err】，"; }
                            if (strPDCAResult.Contains("bad")) { ErrorCode += "PDCA反馈包含【bad】"; }
                            ShowMSG($"上传PDCA失败{ErrorCode}，再次上传", Color.Red);
                            omron.AddToQueue(OmronPLC.plc.addrs[4, 0]);
                            tmDelay.Interval = 4000;
                            tmDelay.Start();
                        }
                        break;
                    case enMESStep.Completed:
                        isIDLE = true;
                        enStep = enMESStep.Start;
                        return;

                }
            }
        }

        private string SubSN(string str)
        {
            if (str.Contains("SN="))
            {
                int SNStart = str.IndexOf("SN=");
                string SNStr = str.Trim().Substring(SNStart + 3, str.Length - SNStart - 3);
                return SNStr;
            }
            else { return ""; }
        }
    }
}
