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
using static Cowain_Machine.Flow.clsDispenserAuto;

namespace Cowain_AutoDispenser.Flow
{
    public class clsGTKMESPushClass : Base
    {
        public clsGTKMESPushClass(Base parent, int nStation, int nSubID, String strEName, String strCName, int ErrCodeBase)
           : base(parent, nStation, strEName, strCName, ErrCodeBase)
        {
            String strStation = nStation.ToString();
            m_iSubID = nSubID;
            Station = nStation;
            m_tmDelay = new System.Timers.Timer(1000);
            m_tmDelay.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent_DelayTimeOut);
        }
        ~clsGTKMESPushClass()
        {
        }
        int Station = 0;
        int m_iSubID = 0;
        System.Timers.Timer m_tmDelay;
        bool b_Result = false;
        private void OnTimedEvent_DelayTimeOut(object source, System.Timers.ElapsedEventArgs e) { m_tmDelay.Enabled = false; }
        MESAndPDCAStep m_Step;
        int currentRunner = 0;
        MSystemDateDefine.GantryParm pGantryParm1 = MSystemDateDefine.SystemParameter.Gantry1Parm;
        MSystemDateDefine.GantryParm pGantryParm2 = MSystemDateDefine.SystemParameter.Gantry2Parm;
        MSystemDateDefine.GantryParm pSelectParm;
        LogExcel LogExcel = new LogExcel();
        public Error pError = null;
        /// <summary>
        /// 在右流道使用前龙门点胶时,应该获取index为0 时的前龙门信息，比如胶水，胶阀参数
        /// </summary>
        int index = 0;
        public enum MESAndPDCAStep
        {
            MESAndPDCAStart,
            MESAndPDCA_UpLoadMes_MachineBOM,
            MESAndPDCA_UpLoadMes_checkMSGCompleted,
            MESAndPDCACompleted,
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
            m_nStep = (int)MESAndPDCAStep.MESAndPDCACompleted;
            m_Status = 狀態.待命;
            base.HomeCycle(ref dbTime);
        }

        public override void StepCycle(ref double dbTime)
        {
            if (MESDataDefine.currentMESAndPDCASteps[m_iSubID] != m_Step)
            {
                //LogAuto.SaveMachineLog("流道" + m_iSubID.ToString() + ",上传MES流程," + m_Step.ToString());
                MESDataDefine.currentMESAndPDCASteps[m_iSubID] = m_Step;
            }
            m_Step = (MESAndPDCAStep)m_nStep;
            switch (m_Step)
            {
                case MESAndPDCAStep.MESAndPDCAStart:
                    MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.CheckSN;
                    m_nStep = (int)MESAndPDCAStep.MESAndPDCA_UpLoadMes_MachineBOM;
                    break;
                case MESAndPDCAStep.MESAndPDCA_UpLoadMes_MachineBOM:
                    m_tmDelay.Interval = 2000;
                    m_tmDelay.Start();
                    GTKPOSTClass.AddCMD(currentRunner, CMDStep.上传设备BOM);
                    m_nStep = (int)MESAndPDCAStep.MESAndPDCA_UpLoadMes_checkMSGCompleted;
                    break;
                case MESAndPDCAStep.MESAndPDCA_UpLoadMes_checkMSGCompleted:
                    string result2 = GTKPOSTClass.GetResult(currentRunner, CMDStep.上传设备BOM);
                    if (result2 == "OK")
                    {
                        m_nStep = (int)MESAndPDCAStep.MESAndPDCACompleted;
                    }
                    else if (result2 == "NG" || m_tmDelay.Enabled == false)
                    {
                        MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesNG;
                        string err = GTKPOSTClass.GetErrMSG(currentRunner, CMDStep.上传设备BOM);
                        string strShowMessage = (currentRunner == 0) ? "左流道MES上传设备BOM失败！" + "ERR:" + err : "右流道MES上传设备BOM失败！" + "ERR:" + err;
                        pError = new Error(ref this.m_NowAddress, strShowMessage, "", (int)MErrorDefine.MErrorCode.MES上传BOM失败);
                        pError.AddErrSloution("Retry ", (int)MESAndPDCAStep.MESAndPDCA_UpLoadMes_MachineBOM);//Retry，再試一次
                        pError.ErrorHappen(ref pError, Error.ErrorType.錯誤);
                    }
                    break;  
                case MESAndPDCAStep.MESAndPDCACompleted:
                    MESDataDefine.MesUploadStatusS[currentRunner] = MesStatus.UpLoadMesOK;
                    m_Status = 狀態.待命;
                    break;
            }
        }
        public bool MESAction(int currentRunner1, MSystemDateDefine.GantryParm pSelectParm1)
        {
            currentRunner = currentRunner1;
            pSelectParm = pSelectParm1;
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
            int doStep = (int)MESAndPDCAStep.MESAndPDCAStart;
            bool bRet = DoStep(doStep);
            return bRet;
        }
    }
}
