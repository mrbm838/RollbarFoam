using Chart;
using Cowain_Form.FormView;
using Cowain_Machine.Flow;
using MotionBase;
using Post;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Cowain_Machine.Flow.clsDispenserAuto;

namespace Cowain_AutoDispenser.Flow
{
    public class clsGTKPDCAPushClass : Base
    {
        MESGTKData mESGTKData;
        public clsGTKPDCAPushClass(Base parent, int nStation, int nSubID, String strEName, String strCName, int ErrCodeBase)
           : base(parent, nStation, strEName, strCName, ErrCodeBase)
        {
            String strStation = nStation.ToString();
            m_iSubID = nSubID;
            Station = nStation;
            m_tmDelay = new System.Timers.Timer(1000);
            m_tmDelay.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent_DelayTimeOut);
            if (MachineDataDefine.MachineCfgS.MachineFactoryEumn == MachineFactory.歌尔)
                mESGTKData = (MESGTKData)MESDataDefine.MESDatas;
        }
        ~clsGTKPDCAPushClass()
        {
        }
        int Station = 0;
        int m_iSubID = 0;
        System.Timers.Timer m_tmDelay;
        bool b_Result = false;
        /// <summary>
        /// 载具获取多SN时使用
        /// </summary>
        int indexSN = 0;
        private void OnTimedEvent_DelayTimeOut(object source, System.Timers.ElapsedEventArgs e) { m_tmDelay.Enabled = false; }
        PDCAStep m_Step;
        int currentRunner = 0;
        MSystemDateDefine.GantryParm pGantryParm1 = MSystemDateDefine.SystemParameter.Gantry1Parm;
        MSystemDateDefine.GantryParm pGantryParm2 = MSystemDateDefine.SystemParameter.Gantry2Parm;
        MSystemDateDefine.GantryParm pSelectParm;
        LogExcel LogExcel = new LogExcel();
        public Error pError = null;
        /// <summary>
        /// 统计PDCA上传失败次数，当次数大于3时再报警
        /// </summary>
        int NGcount = 0;
        /// <summary>
        /// 在右流道使用前龙门点胶时,应该获取index为0 时的前龙门信息，比如胶水，胶阀参数
        /// </summary>
        int index = 0;
        public enum PDCAStep
        {
            MESAndPDCA_UpLoadMes_MachineBOM,
            MESAndPDCA_Action,
            MESAndPDCA_UpLoadMes_RecheckResult,
            MESAndPDCA_UpLoadMes_RecheckMSG,
            //MESAndPDCA_UpLoadMes_MachineParameter,
            MESAndPDCA_UpLoadMes_CheckMSG,
            MESAndPDCA_UpLoadMes_checkMSGCompleted,
            //MESAndPDCA_UpLoadMes_checkReJudgeCompleted,
            PDCA_Action,
            GetURL,
            MESPOST_Get_S_Build,
            MESPOST_Get_Build_Event,
            MESPOST_Get_Build_Martix_Config,
            MESPOST_Get_Build_Martix_Config_Wait,
            MESAndPDCA_FileExist,
            MESAndPDCA_UpLoadPDCA,
            MESAndPDCA_UpLoadPDCAResult,
            MESAndPDCA_SaveDatas,
            MESAndPDCA_WaitTimes,
            MESAndPDCA_WaitTimesCompleted,
            MESAndPDCACompleted,
            Judgecount,
            Completed,
        }
        public override void Stop()
        {
            m_Status = 狀態.待命;
            base.Stop();
        }
        public bool getResult()
        {
            return b_Result;
        }
        public override void HomeCycle(ref double dbTime)
        {
            m_Status = 狀態.待命;
            base.HomeCycle(ref dbTime);
        }

        public override void StepCycle(ref double dbTime)
        {
            if (MESDataDefine.CurrentPDCAStepS[m_iSubID] != m_Step)
            {
                //LogAuto.SaveMachineLog("流道" + m_iSubID.ToString() + ",上传PDCA流程," + m_Step.ToString());
                MESDataDefine.CurrentPDCAStepS[m_iSubID] = m_Step;
            }
            m_Step = (PDCAStep)m_nStep;
            switch (m_Step)
            {
                case PDCAStep.MESAndPDCA_UpLoadMes_MachineBOM:
                    GTKPOSTClass.AddCMD(currentRunner, CMDStep.上传设备BOM);
                    m_nStep = (int)PDCAStep.MESAndPDCA_Action;
                    break;
                case PDCAStep.MESAndPDCA_Action:
                    MESDataDefine.myReflashDispenserDataClass.reflashData();
                    m_nStep = (int)PDCAStep.MESAndPDCA_UpLoadMes_RecheckResult;
                    break;
                case PDCAStep.MESAndPDCA_UpLoadMes_RecheckResult:
                    m_tmDelay.Interval = 2000;
                    m_tmDelay.Start();
                    GTKPOSTClass.AddCMD(currentRunner, CMDStep.上传复检结果);
                    m_nStep = (int)PDCAStep.MESAndPDCA_UpLoadMes_RecheckMSG;
                    break;
                case PDCAStep.MESAndPDCA_UpLoadMes_RecheckMSG:
                    string result1 = GTKPOSTClass.GetResult(currentRunner, CMDStep.上传复检结果);
                    if (result1 == "OK")
                    {
                        m_tmDelay.Enabled = false;
                        m_tmDelay.Interval = 2000;
                        m_tmDelay.Start();
                        GTKPOSTClass.AddCMD(currentRunner, CMDStep.上传参数信息);
                        m_nStep = (int)PDCAStep.MESAndPDCA_UpLoadMes_CheckMSG;
                    }
                    else if (result1 == "NG" || m_tmDelay.Enabled == false)
                    {
                        MESDataDefine.IsBongS[currentRunner] = true;
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.上传参数信息);
                        string strShowMessage = (currentRunner == 0) ? "左流道MES上传参数信息失败！" + "ERR:" + err : "右流道MES上传参数信息失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.MES上传参数信息失败);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.MESAndPDCA_UpLoadMes_MachineBOM);//Retry，再試一次
                        pError.AddErrSloution("OK (Ignore Upload PDCA)", (int)PDCAStep.Completed);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;
      
                case PDCAStep.MESAndPDCA_UpLoadMes_CheckMSG:
                    string result2 = GTKPOSTClass.GetResult(currentRunner, CMDStep.上传参数信息);
                    if (result2 == "OK")
                    {
                        m_tmDelay.Enabled = false;
                        m_tmDelay.Interval = 2000;
                        m_tmDelay.Start();
                        GTKPOSTClass.AddCMD(currentRunner, CMDStep.检查追溯信息);
                        m_nStep = (int)PDCAStep.MESAndPDCA_UpLoadMes_checkMSGCompleted;
                    }
                    else if (result2 == "NG" || m_tmDelay.Enabled == false)
                    {
                        MESDataDefine.IsBongS[currentRunner] = true;
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.上传参数信息);
                        string strShowMessage = (currentRunner == 0) ? "左流道MES上传参数信息失败！" + "ERR:" + err : "右流道MES上传参数信息失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.MES上传参数信息失败);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.MESAndPDCA_UpLoadMes_MachineBOM);//Retry，再試一次
                        pError.AddErrSloution("OK (Ignore Upload PDCA)", (int)PDCAStep.Completed);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;
                case PDCAStep.MESAndPDCA_UpLoadMes_checkMSGCompleted:
                    string result3 = GTKPOSTClass.GetResult(currentRunner, CMDStep.检查追溯信息);
                    if (result3 == "OK")
                    {
                        m_nStep = (int)PDCAStep.PDCA_Action;
                    }
                    else if (result3 == "NG" || m_tmDelay.Enabled == false)
                    {
                        MESDataDefine.IsBongS[currentRunner] = true;
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.检查追溯信息);
                        string strShowMessage = (currentRunner == 0) ? "左流道MES检查追溯信息失败！" + "ERR:" + err : "右流道MES检查追溯信息失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.MES检查追溯信息失败);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.MESAndPDCA_UpLoadMes_MachineBOM);//Retry，再試一次
                        pError.AddErrSloution("OK (Ignore Upload PDCA)", (int)PDCAStep.Completed);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;
                case PDCAStep.PDCA_Action:
                    m_tmDelay.Enabled = false;
                    m_tmDelay.Interval = 5000;
                    m_tmDelay.Start();
                    m_nStep = (int)PDCAStep.GetURL;
                    break;
                case PDCAStep.GetURL:
                    m_tmDelay.Interval = 6000;
                    m_tmDelay.Start();
                    GTKPOSTClass.AddCMD(currentRunner, CMDStep.PDCA获取URL);
                    m_nStep = (int)PDCAStep.MESPOST_Get_S_Build;
                    break;
                case PDCAStep.MESPOST_Get_S_Build:
                    string result4 = GTKPOSTClass.GetResult(currentRunner, CMDStep.PDCA获取URL);
                    if (result4 == "OK")
                    {
                        m_tmDelay.Enabled = false;
                        m_tmDelay.Interval = 1000;
                        m_tmDelay.Start();
                        GTKPOSTClass.AddCMD(currentRunner, CMDStep.MES获取S_BUILD);
                        m_nStep = (int)PDCAStep.MESPOST_Get_Build_Event;
                    }
                    else if (result4 == "NG" || m_tmDelay.Enabled == false)
                    {
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.PDCA获取URL);
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string strShowMessage = (currentRunner == 0) ? "左流道PDCA获取URL失败！" + "ERR:" + err : "右流道PDCA获取URL失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.PDCA获取URL失败);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.GetURL);//Retry，再試一次
                        pError.AddErrSloution("OK (Ignore Upload PDCA)", (int)PDCAStep.Completed);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;
                case PDCAStep.MESPOST_Get_Build_Event:
                    string result11 = GTKPOSTClass.GetResult(currentRunner, CMDStep.MES获取S_BUILD);
                    if (result11 == "OK")
                    {
                        m_tmDelay.Enabled = false;
                        m_tmDelay.Interval = 1000;
                        m_tmDelay.Start();
                        GTKPOSTClass.AddCMD(currentRunner, CMDStep.MES获取BUILD_EVENT);
                        m_nStep = (int)PDCAStep.MESPOST_Get_Build_Martix_Config;
                    }
                    else if (result11 == "NG" || m_tmDelay.Enabled == false)
                    {
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.MES获取S_BUILD);
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string strShowMessage = (currentRunner == 0) ? "左流道MES获取S_BUILD失败！" + "ERR:" + err : "右流道MES获取S_BUILD失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.MES获取S_BUILD失败);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.MESPOST_Get_S_Build);//Retry，再試一次
                        pError.AddErrSloution("OK (Ignore Upload PDCA)", (int)PDCAStep.Completed);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;
                case PDCAStep.MESPOST_Get_Build_Martix_Config:
                    string result22 = GTKPOSTClass.GetResult(currentRunner, CMDStep.MES获取BUILD_EVENT);
                    if (result22 == "OK")
                    {
                        m_tmDelay.Enabled = false;
                        m_tmDelay.Interval = 1000;
                        m_tmDelay.Start();
                        GTKPOSTClass.AddCMD(currentRunner, CMDStep.MES获取BUILD_MATRIX_CONFIG);
                        m_nStep = (int)PDCAStep.MESPOST_Get_Build_Martix_Config_Wait;
                    }
                    else if (result22 == "NG" || m_tmDelay.Enabled == false)
                    {
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.MES获取BUILD_EVENT);
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string strShowMessage = (currentRunner == 0) ? "左流道MES获取BUILD_EVENT失败！" + "ERR:" + err : "右流道MES获取BUILD_EVENT失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.MES获取BUILD_EVENT失败);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.MESPOST_Get_Build_Event);//Retry，再試一次
                        pError.AddErrSloution("OK (Ignore Upload PDCA)", (int)PDCAStep.Completed);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;
                case PDCAStep.MESPOST_Get_Build_Martix_Config_Wait:
                    string result33 = GTKPOSTClass.GetResult(currentRunner, CMDStep.MES获取BUILD_MATRIX_CONFIG);
                    if (result33 == "OK")
                    {
                        m_tmDelay.Enabled = false;
                        m_tmDelay.Interval = 1000;
                        m_tmDelay.Start();
                        m_nStep = (int)PDCAStep.MESAndPDCA_FileExist;
                    }
                    else if (result33 == "NG" || m_tmDelay.Enabled == false)
                    {
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.MES获取BUILD_MATRIX_CONFIG);
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string strShowMessage = (currentRunner == 0) ? "左流道MES获取BUILD_MATRIX_CONFIG失败！" + "ERR:" + err : "右流道MES获取BUILD_MATRIX_CONFIG失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.MES获取BUILD_MATRIX_CONFIG失败);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.MESPOST_Get_Build_Martix_Config);//Retry，再試一次
                        pError.AddErrSloution("OK (Ignore Upload PDCA)", (int)PDCAStep.Completed);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;
                case PDCAStep.MESAndPDCA_FileExist:
                    string time = MESDataDefine.StrImagetimeDelayS[currentRunner].Substring(0, 4) + "-" + MESDataDefine.StrImagetimeDelayS[currentRunner].Substring(4, 2) + "-" + MESDataDefine.StrImagetimeDelayS[currentRunner].Substring(6, 2);
                    string imagePath = "E:\\IMG\\" + time + "\\" + MESDataDefine.StrSNDelayS[currentRunner] + "_" + MESDataDefine.StrImagetimeDelayS[currentRunner] + ".zip";
                    bool b_Exist = File.Exists(imagePath);
                    if (b_Exist != true && m_tmDelay.Enabled == false)
                    {
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string strShowMessage = (currentRunner == 0) ? "左流道压缩图片不存在！" : "右流道压缩图片不存在！";
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.压缩图片不存在);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.PDCA_Action);//Retry，再試一次
                        pError.AddErrSloution("OK (Ignore Upload PDCA)", (int)PDCAStep.Completed);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    else if (b_Exist)
                    {
                        //在一个载具多个SN模式下，需要复制一个文件
                        if (pSelectParm.b_UCGetSNs)
                        {
                            if (indexSN < 1)
                            {
                                string newImagePath = "E:\\IMG\\" + time + "\\" + MESDataDefine.StrSNFormMESDelayS[currentRunner][indexSN + 1] + "_" + MESDataDefine.StrImagetimeDelayS[currentRunner] + ".zip";
                                File.Copy(imagePath, newImagePath);
                            }
                        }
                        m_nStep = (int)PDCAStep.MESAndPDCA_UpLoadPDCA;
                    }
                    break;
                case PDCAStep.MESAndPDCA_UpLoadPDCA:
                    m_tmDelay.Enabled = false;
                    m_tmDelay.Interval = 11000;
                    m_tmDelay.Start();
                    GTKPOSTClass.AddCMD(currentRunner, CMDStep.上传PDCA, indexSN);
                    m_nStep = (int)PDCAStep.MESAndPDCA_UpLoadPDCAResult;
                    break;
                case PDCAStep.MESAndPDCA_UpLoadPDCAResult:
                    string result44 = GTKPOSTClass.GetResult(currentRunner, CMDStep.上传PDCA);
                    if (result44 == "OK")
                    {
                        m_tmDelay.Enabled = false;
                        MESDataDefine.StrCurrentSN = MESDataDefine.StrSNDelayS[currentRunner];
                        m_nStep = (int)PDCAStep.MESAndPDCA_SaveDatas;
                    }
                    else if (result44 == "NG" || m_tmDelay.Enabled == false)
                    {
                        string str = GTKPOSTClass.getReturnMSG(currentRunner, CMDStep.上传PDCA);
                        //判断PDCA是否连接上，如果没有连接上，就自动连接
                        if (!GTKPOSTClass.socket.ClientSocket.Connected || !GTKPOSTClass.socket.connectOk || str == "")
                        {
                            if (NGcount < 3)
                            {
                                NGcount++;
                                GTKPOSTClass.AddCMD(currentRunner, CMDStep.上传PDCA, indexSN);
                                m_nStep = (int)PDCAStep.MESAndPDCA_WaitTimes;
                                break;
                            }
                        }
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.上传PDCA);
                        string strShowMessage = (currentRunner == 0) ? "左流道PDCA上传失败！" + "ERR:" + err : "右流道PDCA上传失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.PDCA上传失败);
                        pError.AddErrSloution("Retry ", (int)PDCAStep.MESAndPDCA_UpLoadPDCA);//Retry，再試一次
                        pError.AddErrSloution("OK (Save Data To LogExcel)", (int)PDCAStep.MESAndPDCA_SaveDatas);
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;
                case PDCAStep.MESAndPDCA_WaitTimes:
                    m_tmDelay.Enabled = false;
                    m_tmDelay.Interval = 2000;
                    m_tmDelay.Start();
                    m_nStep = (int)PDCAStep.MESAndPDCA_WaitTimesCompleted;
                    break;
                case PDCAStep.MESAndPDCA_WaitTimesCompleted:
                    if (m_tmDelay.Enabled == false)
                    {
                        m_tmDelay.Interval = 8000;
                        m_tmDelay.Start();
                        m_nStep = (int)PDCAStep.MESAndPDCA_UpLoadPDCAResult;
                    }
                    break;
                case PDCAStep.MESAndPDCA_SaveDatas:
                    #region 新增内容
                    try
                    {
                       
                        string Log_Date = DateTime.Now.ToString("yyyy_MM_dd");
                        string Log_Time = DateTime.Now.ToString("HH:mm:ss");
                        string Log_Result_Mes = "OK";
                        if (MachineDataDefine.MachineControlS.IsRecheckNGs[currentRunner])
                        {
                            Log_Result_Mes = "NG";
                        }
                        string Log_UC_SN = MESDataDefine.StrCarryBarCodeDelayS[currentRunner];
                        string Log_MixRatio = "10:1";
                        string Log_Dir = "";
                        if (currentRunner == 0)
                        {
                            Log_Dir = "left";
                        }
                        else
                        {
                            Log_Dir = "right";
                        }
                        string head = "";
                        string strs = "";
                        MesData mesdata = new MesData(currentRunner, 0, "");
                        head = "Date,Time,OP ID,Mes State,Software version, UC SN,SN,Dir,CT,MESGlueSN,";
                        strs = $"{Log_Date},{Log_Time},{MESDataDefine.StrOPID},{Log_Result_Mes},{MESDataDefine.MESDatas.StrVersion},{Log_UC_SN},{ MESDataDefine.StrSNDelayS[currentRunner]},{Log_Dir}, { MESDataDefine.StrLogCT },{MESDataDefine.StrMESGlueS[mesdata.IntIndex][0]}";

                        string[] Nozzle_Temperatures = new string[] { "0", "0" };
                        string[] Tube_Temperatures = new string[] { "0", "0" };
                        string[] Pressures = new string[] { "0", "0" };
                        string[] Mixratios = new string[] { "0", "0" };
                        string[] Apressures = new string[] { "0", "0" };
                        string[] Bpressures = new string[] { "0", "0" };
                        Pressures[0] = MESDataDefine.MESDatas.StrNordPressureS[0];
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmFront)
                        {
                            Nozzle_Temperatures[0] = MESDataDefine.MESDatas.StrNordsonTempS[0];
                            Tube_Temperatures[0] = MESDataDefine.MESDatas.StrTubeNordsonTempS[0];
                            Mixratios[0] = MESDataDefine.MESDatas.StrABRateS[0];
                            Apressures[0] = MESDataDefine.MESDatas.StrAPressureS[0];
                            Bpressures[0] = MESDataDefine.MESDatas.StrBPressureS[0];
                        }
                        else
                        {
                            Nozzle_Temperatures[0] = frm_Main.myDispenserController[0].controller_T1.ToString();
                            Tube_Temperatures[0] = frm_Main.myDispenserController[0].controller_T2.ToString();
                            // Pressures[0] = frm_Main.myDispenserController[0].controller_Presure.ToString();
                            Mixratios[0] = frm_Main.myDispenserController[0].controller_ABrate.ToString();
                            Apressures[0] = frm_Main.myDispenserController[0].controller_APress.ToString();
                            Bpressures[0] = frm_Main.myDispenserController[0].controller_BPress.ToString();
                        }
                        Pressures[1] = MESDataDefine.MESDatas.StrNordPressureS[1];
                        if (MachineDataDefine.ChkStatus.IsDisableAlarmBack)
                        {
                            Nozzle_Temperatures[1] = MESDataDefine.MESDatas.StrNordsonTempS[1];
                            Tube_Temperatures[1] = MESDataDefine.MESDatas.StrTubeNordsonTempS[1];
                            Mixratios[1] = MESDataDefine.MESDatas.StrABRateS[1];
                            Apressures[1] = MESDataDefine.MESDatas.StrAPressureS[1];
                            Bpressures[1] = MESDataDefine.MESDatas.StrBPressureS[1];
                        }
                        else
                        {
                            Nozzle_Temperatures[1] = frm_Main.myDispenserController[1].controller_T1.ToString();
                            Tube_Temperatures[1] = frm_Main.myDispenserController[1].controller_T2.ToString();
                            /// Pressures[1] = frm_Main.myDispenserController[1].controller_Presure.ToString();
                            Mixratios[1] = frm_Main.myDispenserController[1].controller_ABrate.ToString();
                            Apressures[1] = frm_Main.myDispenserController[1].controller_APress.ToString();
                            Bpressures[1] = frm_Main.myDispenserController[1].controller_BPress.ToString();
                        }

                        //判断是单龙门还是双龙门做不同料的模式
                        enGantryType m_enGantryType = (enGantryType)MSystemDateDefine.SystemParameter.MachineParmeter.GantryType;
                        if (m_enGantryType == enGantryType.DualGantry && pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode && pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                        {
                            //胶阀1
                            if (pGantryParm1.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type1_热胶 || pGantryParm1.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type1_热胶_NEW)
                            {
                                head += "NozzleTemperature,Pressure,TubeTemperature,";
                                strs += $",{Nozzle_Temperatures[0]},{Pressures[0]},{Tube_Temperatures[0]}";
                            }
                            else if (pGantryParm1.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type2_冷胶 || pGantryParm1.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type2_冷胶_NEW)
                            {
                                head += "Temperature,Pressure,";
                                strs += $",{Nozzle_Temperatures[0]},{Pressures[0]}";
                            }
                            else if (pGantryParm1.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Loctite || pGantryParm1.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type3_AB胶_NEW)
                            {
                                head += "Mix ratio,A pressure,B pressure,NozzleTemperature,ColdTemperature,";
                                strs += $",{Mixratios[0]},{Apressures[0]},{Bpressures[0]},{Nozzle_Temperatures[0]},{Tube_Temperatures[0]}";
                            }
                            //胶阀2
                            if (pGantryParm2.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type1_热胶 || pGantryParm2.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type1_热胶_NEW)
                            {
                                head += "NozzleTemperature,Pressure,TubeTemperature,";
                                strs += $",{Nozzle_Temperatures[1]},{Pressures[1]},{Tube_Temperatures[1]}";
                            }
                            else if (pGantryParm2.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type2_冷胶 || pGantryParm2.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type2_冷胶_NEW)
                            {
                                head += "Temperature,Pressure,";
                                strs += $",{Nozzle_Temperatures[1]},{Pressures[1]}";
                            }
                            else if (pGantryParm2.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Loctite || pGantryParm2.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type3_AB胶_NEW)
                            {
                                head += "Mix ratio,A pressure,B pressure,NozzleTemperature,ColdTemperature,";
                                strs += $",{Mixratios[1]},{Apressures[1]},{Bpressures[1]},{Nozzle_Temperatures[1]},{Tube_Temperatures[1]}";
                            }

                            //
                            head += "Section,Station,ErrCode,Machine Judge,Manul\r\n";
                
                            strs += $",{mESGTKData.StrSection},{mESGTKData.StrStation},{MESDataDefine.MES_ErrorCode[m_iSubID]},{MachineDataDefine.StrRecheckResult},{MachineDataDefine.StrManulStatus}";
                        }
                        else
                        {
                            //当前胶阀
                            if (pSelectParm.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type1_热胶 || pSelectParm.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type1_热胶_NEW)
                            {
                                head += "NozzleTemperature,Pressure,TubeTemperature,";
                                strs += $",{Nozzle_Temperatures[index]},{Pressures[index]},{Tube_Temperatures[index]}";
                            }
                            else if (pSelectParm.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type2_冷胶 || pSelectParm.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type2_冷胶_NEW)
                            {
                                head += "Temperature,Pressure,";
                                strs += $",{Nozzle_Temperatures[index]},{Pressures[index]}";
                            }
                            else if (pSelectParm.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.Loctite || pSelectParm.dispenserDataClass.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type3_AB胶_NEW)
                            {
                                head += "Mix ratio,A pressure,B pressure,NozzleTemperature,ColdTemperature,";
                                strs += $",{Mixratios[index]},{Apressures[index]},{Bpressures[index]},{Nozzle_Temperatures[index]},{Tube_Temperatures[index]}";
                            }
                            head += "Section,Station,ErrCode,Machine Judge,Manul\r\n";
                            
                            strs += $",{mESGTKData.StrSection},{mESGTKData.StrStation},{MESDataDefine.MES_ErrorCode[m_iSubID]},{MachineDataDefine.StrRecheckResult},{MachineDataDefine.StrManulStatus}";
                        }
                        if (MachineDataDefine.MachineControlS.IsRecheckNGs[currentRunner] != true)
                        {
                            MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesOK;
                        }
                        else
                        {
                            MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        }
                        LogExcel.saveCsvData(head, strs);
                        if (currentRunner == 0)
                        {
                            MachineDataDefine.isLeft = false;
                        }
                        else if (currentRunner == 1)
                        {
                            MachineDataDefine.isRight = false;
                        }
                    }
                    catch (Exception)
                    { }
                    #endregion
                  
                    m_nStep = (int)PDCAStep.MESAndPDCACompleted;
                    break;
                case PDCAStep.MESAndPDCACompleted:
                    m_nStep = (int)PDCAStep.Judgecount;
                    break;
                case PDCAStep.Judgecount:
                    if (indexSN < 1 && pSelectParm.b_UCGetSNs)
                    {
                        indexSN++;
                        MESDataDefine.StrSNDelayS[currentRunner] = MESDataDefine.StrSNFormMESDelayS[currentRunner][indexSN]; //SN缓存
                        m_nStep = (int)PDCAStep.MESAndPDCA_UpLoadPDCA;
                    }
                    else
                        m_nStep = (int)PDCAStep.Completed;
                    break;
                case PDCAStep.Completed:
                    MachineDataDefine.StrRecheckResult = "OK";
                    MachineDataDefine.StrManulStatus = "0";
                    MESDataDefine.MES_ErrorCode[currentRunner] = "NA";
                    m_Status = 狀態.待命;
                    break;
            }
        }
        public bool PDCAAction(int currentRunner1, MSystemDateDefine.GantryParm pSelectParm1)
        {
            currentRunner = currentRunner1;
            pSelectParm = pSelectParm1;
            indexSN = 0;
            MSystemDateDefine.GantryParm pGantryParm1 = MSystemDateDefine.SystemParameter.Gantry1Parm;
            if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.JustBackStation)
            {
                if (currentRunner == 0)
                {
                    index = 1;
                }
                else
                {
                    index = 0;
                }
            }
            else
            {
                index = currentRunner;
            }
            //单龙门点双流道胶
            enGantryType m_enGantryType = (enGantryType)MSystemDateDefine.SystemParameter.MachineParmeter.GantryType;
            if (m_enGantryType == enGantryType.SigleGantry)
            {
                index = 0;
            }
            NGcount = 0;
            int doStep = (int)PDCAStep.MESAndPDCA_Action;
            bool bRet = DoStep(doStep);
            return bRet;
        }
    }
}
