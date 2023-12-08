using Cowain_AutoDispenser.Flow;
using Cowain_Form.FormView;
using Cowain_Machine.Flow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using ToolTotal;
using ToolTotal_1;

namespace Post
{
    public class LXPOSTClass
    {
        private static object locker1 = new object();
        private static LXPOSTClass instace;
        private string updataTime = "";
        private static object lock1 = new object();
        /// <summary>
        /// 左流道对应的pdca网络
        /// </summary>
        public static NetClientPort socketLeft;
        /// <summary>
        ///右流道对应的pdca网络
        /// </summary>
        public static NetClientPort socketRight;

        public static LXPOSTClass CreateInstance()
        {
            lock (locker1)
            {
                if (instace == null)
                {
                    instace = new LXPOSTClass();

                }
                return instace;
            }
        }

        MESLXData mESLXData;

        public string StrPdcaresultL = "";
        public string StrPdcaresultR = "";

        HttpWebRequest Web = null;
        HttpWebResponse Res = null;
        IniFile myIniFile;
        /// <summary>
        /// 判断前后胶阀是否屏蔽[0]前龙门[1]后龙门
        /// </summary>
        public bool[] bDisableAlarmFrontOrBack = new bool[2] { false, false };

        private int id = 0;



        /// <summary>
        ///延时
        /// </summary>
        public System.Timers.Timer POSTm_tmDelay = new System.Timers.Timer();
        private void OnTimedEvent_DelayTimeOut(object source, System.Timers.ElapsedEventArgs e)
        {
            POSTm_tmDelay.Enabled = false;
        }
        ~LXPOSTClass()
        { }


        private LXPOSTClass()
        {

            POSTm_tmDelay.Elapsed += OnTimedEvent_DelayTimeOut;
            mESLXData = (MESLXData)MESDataDefine.MESDatas;
            socketLeft = new NetClientPort();
            socketRight = new NetClientPort();
            string ip = MESDataDefine.MESDatas.StrMiniIP;
            string port = MESDataDefine.MESDatas.StrMiniPort;
            if (!MachineDataDefine.ChkStatus.IsDisablePDCA)
            {
                Task.Run(() =>
                {
                    socketLeft.Open(ip, Convert.ToInt32(port));
                    socketLeft.receiveDoneSocketEvent += SocketEventL;

                    socketRight.Open(ip, Convert.ToInt32(port));
                    socketRight.receiveDoneSocketEvent += SocketEventR;
                });
            }
        }
        public  void SocketEventL(string msgStr)
        {
            StrPdcaresultL = StrPdcaresultL + msgStr.Trim();
            SaveDateMesL("_PDCA反馈信息:" + msgStr + "\r\n");
        }

        public void SocketEventR(string msgStr)
        {
            StrPdcaresultR = StrPdcaresultR + msgStr.Trim();
            SaveDateMesR("_PDCA反馈信息:" + msgStr + "\r\n");
        }

        /// <summary>
        ///  mes 获取SN
        /// </summary>
        public string MES_SendUCGetSn(string ucsn, int headid)
        {
            lock (lock1)
            {
                try
                {

                    string sendMessage = mESLXData.StrGetSNDat + ucsn;

                    if (headid == 0)
                        SaveDateMesL("发送UC获取SN:" + sendMessage, false);
                    else
                        SaveDateMesR("发送UC获取SN:" + sendMessage, false);

                    Web = (HttpWebRequest)WebRequest.Create(mESLXData.StrURL);
                    byte[] data = Encoding.UTF8.GetBytes(sendMessage);
                    Web.Method = "POST";
                    Web.ContentType = "application/x-www-form-urlencoded";
                    Web.ContentLength = data.Length;
                    using (var stream = Web.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    Res = (HttpWebResponse)Web.GetResponse();
                    string str = "";
                    using (StreamReader sr = new StreamReader(Res.GetResponseStream(), Encoding.UTF8))
                    {
                        str = sr.ReadToEnd();
                    }
                    if (headid == 0)
                        SaveDateMesL("获取SN:" + str);
                    else
                        SaveDateMesR("获取SN:" + str);

                    return str;
                }
                catch (Exception EX)
                {

                    return "";
                }
            }
        }
        /// <summary>
        /// check Sn
        /// </summary>
        public string MesUOP(string sn, string uc, int gantryId)
        {
            lock (lock1)
            {
                try
                {

                    string GlueSN = "";
                    MSystemDateDefine.GantryParm pGantryParm1 = MSystemDateDefine.SystemParameter.Gantry1Parm;
                    MSystemDateDefine.GantryParm pGantryParm2 = MSystemDateDefine.SystemParameter.Gantry2Parm;
                    if (gantryId == 0)
                    {
                        if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                            GlueSN = mESLXData.StrGlueSNFront + "," + mESLXData.StrGlueSNBack;
                        else if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.JustFrontStation)
                            GlueSN = mESLXData.StrGlueSNFront;
                        else
                            GlueSN = mESLXData.StrGlueSNBack;

                    }
                    if (gantryId == 1)
                    {
                        if (pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                            GlueSN = mESLXData.StrGlueSNFront + "," + mESLXData.StrGlueSNBack;
                        else if (pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.JustFrontStation)
                            GlueSN = mESLXData.StrGlueSNFront;
                        else
                            GlueSN = mESLXData.StrGlueSNBack;
                    }

                    GlueSN = GlueSN.Replace("+", "%2B");
                    string sendMessage = "cmd=UOP&sn=" + sn + "&Station_iD=" + mESLXData.StrStationID + "&op=" + mESLXData.StrOp + "&lotno=" + GlueSN + "&Fixture_ID=" + uc + "&kpsn=";


                    int num = gantryId;
                    String RL = num == 1 ? "Right" : "Left";


                    String str =  "&wo=&"
                                  //+ "sn=" + sn                         //CMD=REC&wo=&sn=//myIniFile.IniReadValue("WebMes", "RecDat1")
                                  //+ "&op=" + mESLXData.StrOp + "&"
                                  //+ "kpsn=" + "" + "&"
                                  //+ "lotno=" + GlueSN + "&"//胶水sn
                                  //+ "Fixture_ID=" + uc + "&"
                                  + "machine=" + mESLXData.StrMachineID + "&"
                                  + "Cavity=" + RL + "&"
                                  //+ "Station_ID=" + mESLXData.StrStationID + "&"
                                  ;
                    String testdata = "testdata=test_result=PASS" + "$"
                                       + "unit_sn=" + sn + "$"
                                       + "uut_start=" + MESDataDefine.StrStartTimeDelayS[num] + "$"
                                       + "uut_stop=" + MESDataDefine.StrStopTime[num] + "$"
                                       + "limits_version=" + "$"
                                       + "software_name =" + "Cowain_AutoDispenser" + "$"
                                       + "software_version=" + mESLXData.StrVersion + "$"
                                       + "station_id=" + mESLXData.StrStationID + "$"
                                       + "fixture_id=" + uc + "$"
                                       + "Head_ID=" + RL + "&";
                    sendMessage = sendMessage + str + testdata;

                    if (gantryId == 0)
                        SaveDateMesL("发送SN_check_mes:" + sendMessage);
                    else
                        SaveDateMesR("发送SN_check_mes:" + sendMessage);
                    Web = (HttpWebRequest)WebRequest.Create(mESLXData.StrURL);
                    byte[] data = Encoding.UTF8.GetBytes(sendMessage);
                    Web.Method = "POST";
                    Web.ContentType = "application/x-www-form-urlencoded";
                    Web.ContentLength = data.Length;
                    using (var stream = Web.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    Res = (HttpWebResponse)Web.GetResponse();
                    string strResult = "";
                    using (StreamReader sr = new StreamReader(Res.GetResponseStream(), Encoding.UTF8))
                    {
                        strResult = sr.ReadToEnd();
                    }

                    if (gantryId == 0)
                        SaveDateMesL("check_mes_SN结果:" + strResult);
                    else
                        SaveDateMesR("check_mes_SN结果:" + strResult);

                    return str;
                }
                catch { return ""; }
            }

        }

        /// <summary>
        /// Link Sn
        /// </summary>
        public string MesSendADD(string sn, string uc, int RL, int No, string addORrec)
        {
            lock (lock1)
            {
                try
                {
                    if (addORrec == "ADD")
                        mESLXData.StrLinkSNDat = "CMD=ADD";
                    else
                        mESLXData.StrLinkSNDat = "CMD=REC";

                    string RLName = "";

                    if (RL == 0)
                        RLName = "Left";
                    else
                        RLName = "Right";


                    string sendMessage = mESLXData.StrLinkSNDat + PostBady(sn, uc, RLName, No);//为空的是否可以不填写
                    if (RL == 0)
                        SaveDateMesL("发送Link Sn结果:" + sendMessage);
                    else
                        SaveDateMesR("发送Link Sn结果:" + sendMessage);
                    Web = (HttpWebRequest)WebRequest.Create(mESLXData.StrURL);
                    byte[] data = Encoding.UTF8.GetBytes(sendMessage);
                    Web.Method = "POST";
                    Web.ContentType = "application/x-www-form-urlencoded";
                    Web.ContentLength = data.Length;
                    using (var stream = Web.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }

                    Res = (HttpWebResponse)Web.GetResponse();
                    string str = "";
                    using (StreamReader sr = new StreamReader(Res.GetResponseStream(), Encoding.UTF8))
                    {
                        str = sr.ReadToEnd();
                    }
                    if (RL == 0)
                        SaveDateMesL("收到Link Sn反馈:" + str);
                    else
                        SaveDateMesR("收到Link Sn反馈:" + str);

                    return str;
                }
                catch (Exception ex)
                {
                    if (RL == 0)
                        SaveDateMesL("收到Link Sn反馈:" + ex.ToString());
                    else
                        SaveDateMesR("收到Link Sn反馈:" + ex.ToString());
                    return "";

                }
            }
        }
        public string PostBady(string sn, string uc, string RL, int No)
        {

            string str = "";
            string testdata = "";
            string results1 = "";
            string results2 = "";
            string GlueSN = "";
            int num = 0;
            if (RL == "Right")
                num = 1;
            MSystemDateDefine.GantryParm pGantryParm1 = MSystemDateDefine.SystemParameter.Gantry1Parm;
            MSystemDateDefine.GantryParm pGantryParm2 = MSystemDateDefine.SystemParameter.Gantry2Parm;
            //先判断哪个流道，再判断当前流道的点胶模式，然后选择胶水SN
            if (No == 0)
            {
                if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                    GlueSN = mESLXData.StrGlueSNFront + "," + mESLXData.StrGlueSNBack;
                else if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.JustFrontStation)
                    GlueSN = mESLXData.StrGlueSNFront;
                else
                    GlueSN = mESLXData.StrGlueSNBack;

            }
            if (No == 1)
            {
                if (pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                    GlueSN = mESLXData.StrGlueSNFront + "," + mESLXData.StrGlueSNBack;
                else if (pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.JustFrontStation)
                    GlueSN = mESLXData.StrGlueSNFront;
                else
                    GlueSN = mESLXData.StrGlueSNBack;
            }
            MSystemDateDefine.DispenserDataClass m_enGantryDispenserType1 = MSystemDateDefine.SystemParameter.Gantry1Parm.dispenserDataClass;
            MSystemDateDefine.DispenserDataClass m_enGantryDispenserType2 = MSystemDateDefine.SystemParameter.Gantry2Parm.dispenserDataClass;

            GlueSN = GlueSN.Replace("+", "%2B");
            str =
                   "&wo=&" +
                   "sn=" + sn +                        //CMD=REC&wo=&sn=//myIniFile.IniReadValue("WebMes", "RecDat1")
                  "&op=" + mESLXData.StrOp + "&"
                   + "kpsn=" + "" + "&"
                   + "lotno=" + GlueSN + "&"//胶水sn
                   + "Fixture_ID=" + uc + "&"
                   + "machine=" + mESLXData.StrMachineID + "&"
                   + "Cavity=" + RL + "&"
                   + "Station_ID=" + mESLXData.StrStationID + "&";
            testdata = "testdata=test_result=PASS" + "$"
                              + "unit_sn=" + sn + "$"
                              + "uut_start=" + MESDataDefine.StrStartTimeDelayS[num] + "$"
                              + "uut_stop=" + MESDataDefine.StrStopTime[num] + "$"
                              + "limits_version=" + "$"
                              + "software_name =" + "Cowain_AutoDispenser" + "$"
                              + "software_version=" + mESLXData.StrVersion + "$"
                              + "station_id=" + mESLXData.StrStationID + "$"
                              + "fixture_id=" + uc + "$"
                              + "Head_ID=" + RL + "&";
            results1 = getMesValue(m_enGantryDispenserType1, 0); //生成Mes胶阀数据
            results2 = getMesValue(m_enGantryDispenserType2, 1);//生成Mes胶阀数据
            str = str + testdata;
            if (No == 0)
            {
                if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                    str = str + "results=" + results1 + "$," + results2;
                else if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.JustFrontStation)
                    str = str + "results=" + results1;
                else
                    str = str + "results=" + results2;

            }
            if (No == 1)
            {
                if (pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                    str = str + "results=" + results1 + "$," + results2;
                else if (pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.JustFrontStation)
                    str = str + "results=" + results1;
                else
                    str = str + "results=" + results2;

            }

            return str;
        }

        /// <summary>
        /// 生成Mes 需要上传的值
        /// </summary>
        private string getMesValue(MSystemDateDefine.DispenserDataClass m_enGantryDispenserType, int gantryId)
        {
            string results1 = "";
            string err = "";

            if (gantryId == 0)    //判断前后胶阀是否屏蔽                 
                bDisableAlarmFrontOrBack[0] = MachineDataDefine.ChkStatus.IsDisableAlarmFront ? true : false;
            else
                bDisableAlarmFrontOrBack[1] = MachineDataDefine.ChkStatus.IsDisableAlarmBack ? true : false;
            #region 汉高 A,B压力，AB混合比
            if (m_enGantryDispenserType.dispenserType == MSystemDateDefine.enDispenserHeadType.Loctite)//汉高
            {
                double ABRate = 0;
                double APress = 0;
                double BPressure = 0;
                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    ABRate = Convert.ToDouble(MESDataDefine.MESDatas.StrABRateS[gantryId]);
                    APress = Convert.ToDouble(MESDataDefine.MESDatas.StrAPressureS[gantryId]);
                    BPressure = Convert.ToDouble(MESDataDefine.MESDatas.StrBPressureS[gantryId]);
                }
                else
                {
                    ABRate = frm_Main.myDispenserController[gantryId].controller_ABrate;
                    APress = frm_Main.myDispenserController[gantryId].controller_APress;
                    BPressure = frm_Main.myDispenserController[gantryId].controller_BPress;
                }
                #endregion
                #region ABRate结果
                string str_ABRate = "PASS";
                try
                {
                    double ABRateLimit0 = Convert.ToDouble(MESDataDefine.MESDatas.StrABRateLimitS[gantryId][0]);
                    double ABRateLimit1 = Convert.ToDouble(MESDataDefine.MESDatas.StrABRateLimitS[gantryId][1]);
                    if ((ABRate >= ABRateLimit0) && (ABRate <= ABRateLimit1))
                    {
                        str_ABRate = "PASS";
                    }
                    else
                    {
                        str_ABRate = "FAIL";
                    }
                }
                catch (Exception)
                {
                    str_ABRate = "FAIL";
                }
                #endregion
                #region APressure结果
                string str_APressure = "PASS";
                try
                {
                    double APressureLimit0 = Convert.ToDouble(MESDataDefine.MESDatas.StrAPressureLimitS[gantryId][0]);
                    double APressureLimit1 = Convert.ToDouble(MESDataDefine.MESDatas.StrAPressureLimitS[gantryId][1]);
                    //  double APress = 5;// frm_Main.myDispenserController[0].controller_APress;
                    if ((APress >= APressureLimit0) && (APress <= APressureLimit1))
                    {
                        str_APressure = "PASS";
                    }
                    else
                    {
                        str_APressure = "FAIL";
                    }
                }
                catch (Exception)
                {
                    str_APressure = "FAIL";
                }
                #endregion
                #region BPressure结果
                string str_BPressure = "PASS";
                try
                {
                    double BPressureLimit0 = Convert.ToDouble(MESDataDefine.MESDatas.StrBPressureLimitS[gantryId][0]);
                    double BPressureLimit1 = Convert.ToDouble(MESDataDefine.MESDatas.StrBPressureLimitS[gantryId][1]);
                    if ((BPressure >= BPressureLimit0) && (BPressure <= BPressureLimit1))
                    {
                        str_BPressure = "PASS";
                    }
                    else
                    {
                        str_BPressure = "FAIL";
                    }
                }
                catch (Exception)
                {
                    str_BPressure = "FAIL";
                }
                #endregion
                results1 = "lower_limit=" + MESDataDefine.MESDatas.StrABRateLimitS[gantryId][0] +
                                 "$parametric_key=$priority=$result=" + str_ABRate + "$sub_sub_test=$sub_test=$test=Mix_Ratio$Message=" + err +
                                 "$units=$upper_limit=" + MESDataDefine.MESDatas.StrABRateLimitS[gantryId][1] +
                                 "$value=" + ABRate +  //ABRate参数

                                 "$,lower_limit=" + MESDataDefine.MESDatas.StrAPressureLimitS[gantryId][0] +
                                 "$parametric_key=$priority=$result=" + str_APressure + "$sub_sub_test=$sub_test=$test=AGlue_Pressure$Message=" + err +
                                 "$units=$upper_limit=" + MESDataDefine.MESDatas.StrAPressureLimitS[gantryId][1] +
                                 "$value=" + APress + //APress参数

                                 "$,lower_limit=" + MESDataDefine.MESDatas.StrBPressureLimitS[gantryId][0] +
                                 "$parametric_key=$priority=$result=" + str_BPressure + "$sub_sub_test=$sub_test=$test=BGlue_Pressure$Message=" + err +
                                 "$units=$upper_limit=" + MESDataDefine.MESDatas.StrBPressureLimitS[gantryId][1] +
                                 "$value=" + BPressure; //BPressure参数

                //屏蔽 换胶时间，自动排胶时间
                //+"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=ABReplace_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrGlueReplceTime[0] +
                //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=ABPurge_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrPurgeTime[0];
            }

            #endregion
            #region 高凯
            if (m_enGantryDispenserType.dispenserType == MSystemDateDefine.enDispenserHeadType.高凯)
            {
                double NordsonTemp1 = 0;
                double NordsonTemp2 = 0;
                double NordPressure = 0;
                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    NordsonTemp1 = Convert.ToDouble(mESLXData.StrNordsonTempS[gantryId]);
                    NordsonTemp2 = Convert.ToDouble(mESLXData.StrTubeNordsonTempS[gantryId]);
                    NordPressure = Convert.ToDouble(mESLXData.StrNordPressureS[gantryId]);
                }
                else
                {
                    NordsonTemp1 = frm_Main.myDispenserController[gantryId].controller_T1;
                    NordsonTemp2 = frm_Main.myDispenserController[gantryId].controller_T2;
                    NordPressure = frm_Main.myDispenserController[gantryId].controller_Presure;
                }
                #endregion
                #region NordsonTemp-T1,T2结果
                string str_NordsonTemp1 = "PASS", str_NordsonTemp2 = "PASS";
                try
                {
                    double NordsonTempLimit0 = Convert.ToDouble(mESLXData.StrNordsonTempLimitS[gantryId][0]);
                    double NordsonTempLimit1 = Convert.ToDouble(mESLXData.StrNordsonTempLimitS[gantryId][1]);
                    if ((NordsonTemp1 >= NordsonTempLimit0) && (NordsonTemp1 <= NordsonTempLimit1))
                    {
                        str_NordsonTemp1 = "PASS";
                    }
                    else
                    {
                        str_NordsonTemp1 = "FAIL";
                    }
                    NordsonTempLimit0 = Convert.ToDouble(mESLXData.StrTubeNordsonTempLimitS[gantryId][0]);
                    NordsonTempLimit1 = Convert.ToDouble(mESLXData.StrTubeNordsonTempLimitS[gantryId][1]);
                    if ((NordsonTemp2 >= NordsonTempLimit0) && (NordsonTemp2 <= NordsonTempLimit1))
                    {
                        str_NordsonTemp2 = "PASS";
                    }
                    else
                    {
                        str_NordsonTemp2 = "FAIL";
                    }
                }
                catch (Exception)
                {
                    str_NordsonTemp1 = "FAIL";
                    str_NordsonTemp2 = "FAIL";
                }
                #endregion
                #region NordPressure结果
                string str_NordPressure = "PASS";
                try
                {
                    double NordPressureLimit0 = Convert.ToDouble(mESLXData.StrNordPressureLimitS[gantryId][0]);
                    double NordPressureLimit1 = Convert.ToDouble(mESLXData.StrNordPressureLimitS[gantryId][1]);
                    //   double NordPressure = 2;// frm_Main.myDispenserController[1].controller_Presure;

                    if ((NordPressure >= NordPressureLimit0) && (NordPressure <= NordPressureLimit1))
                    {
                        str_NordPressure = "PASS";
                    }
                    else
                    {
                        str_NordPressure = "FAIL";
                    }

                }
                catch (Exception)
                {
                    str_NordPressure = "FAIL";
                }
                #endregion
                if (m_enGantryDispenserType.communication == MSystemDateDefine.Communication.串口)
                {

                    results1 = "lower_limit=" + mESLXData.StrNordsonTempLimitS[gantryId][0] +
                             "$parametric_key=$priority=$result=" + str_NordsonTemp1 + "$sub_sub_test=$sub_test=$test=HMNozze_Temp$Message=" + err +
                             "$units=$upper_limit=" + mESLXData.StrNordsonTempLimitS[gantryId][1] +
                             "$value=" + NordsonTemp1 +  //温度T1参数

                             "$,lower_limit=" + mESLXData.StrTubeNordsonTempLimitS[gantryId][0] +
                             "$parametric_key=$priority=$result=" + str_NordsonTemp2 + "$sub_sub_test=$sub_test=$test=HMTube_temp$Message=" + err +
                             "$units=$upper_limit=" + mESLXData.StrTubeNordsonTempLimitS[gantryId][1] +
                             "$value=" + NordsonTemp2 + //温度T2参数

                             "$,lower_limit=" + mESLXData.StrNordPressureLimitS[gantryId][0] +
                             "$parametric_key=$priority=$result=" + str_NordPressure + "$sub_sub_test=$sub_test=$test=HMGlue_Pressure$Message=" + err +
                             "$units=$upper_limit=" + mESLXData.StrNordPressureLimitS[gantryId][1] +
                             "$value=" + NordPressure;// + //压力参数

                    //屏蔽 换胶时间，自动排胶时间
                    //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMReplace_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrGlueReplceTime[0] +
                    //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMPurge_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrPurgeTime[0];
                }

            }

            #endregion
            # region 诺信_Type1_热胶 && ZZT_Type1_热胶_NEW
            if (m_enGantryDispenserType.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type1_热胶 ||
                m_enGantryDispenserType.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type1_热胶_NEW)
            {
                double NordsonTemp1 = 0;
                double NordsonTemp2 = 0;
                double NordPressure = 0;
                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    NordsonTemp1 = Convert.ToDouble(mESLXData.StrNordsonTempS[gantryId]);
                    NordsonTemp2 = Convert.ToDouble(mESLXData.StrTubeNordsonTempS[gantryId]);
                    NordPressure = Convert.ToDouble(mESLXData.StrNordPressureS[gantryId]);
                }
                else
                {
                    NordsonTemp1 = frm_Main.myDispenserController[gantryId].controller_T1;
                    NordsonTemp2 = frm_Main.myDispenserController[gantryId].controller_T2;
                    NordPressure = frm_Main.myDispenserController[gantryId].controller_Presure;
                }
                #endregion
                #region NordsonTemp-T1,T2结果
                string str_NordsonTemp1 = "PASS", str_NordsonTemp2 = "PASS";
                try
                {
                    double NordsonTempLimit0 = Convert.ToDouble(mESLXData.StrNordsonTempLimitS[gantryId][0]);
                    double NordsonTempLimit1 = Convert.ToDouble(mESLXData.StrNordsonTempLimitS[gantryId][1]);
                    if ((NordsonTemp1 >= NordsonTempLimit0) && (NordsonTemp1 <= NordsonTempLimit1))
                    {
                        str_NordsonTemp1 = "PASS";
                    }
                    else
                    {
                        str_NordsonTemp1 = "FAIL";
                    }
                    NordsonTempLimit0 = Convert.ToDouble(mESLXData.StrTubeNordsonTempLimitS[gantryId][0]);
                    NordsonTempLimit1 = Convert.ToDouble(mESLXData.StrTubeNordsonTempLimitS[gantryId][1]);
                    if ((NordsonTemp2 >= NordsonTempLimit0) && (NordsonTemp2 <= NordsonTempLimit1))
                    {
                        str_NordsonTemp2 = "PASS";
                    }
                    else
                    {
                        str_NordsonTemp2 = "FAIL";
                    }
                }
                catch (Exception)
                {
                    str_NordsonTemp1 = "FAIL";
                    str_NordsonTemp2 = "FAIL";
                }
                #endregion
                #region NordPressure结果
                string str_NordPressure = "PASS";
                try
                {
                    double NordPressureLimit0 = Convert.ToDouble(mESLXData.StrNordPressureLimitS[gantryId][0]);
                    double NordPressureLimit1 = Convert.ToDouble(mESLXData.StrNordPressureLimitS[gantryId][1]);
                    //   double NordPressure = 2;// frm_Main.myDispenserController[1].controller_Presure;

                    if ((NordPressure >= NordPressureLimit0) && (NordPressure <= NordPressureLimit1))
                    {
                        str_NordPressure = "PASS";
                    }
                    else
                    {
                        str_NordPressure = "FAIL";
                    }

                }
                catch (Exception)
                {
                    str_NordPressure = "FAIL";
                }
                #endregion
                if (m_enGantryDispenserType.communication == MSystemDateDefine.Communication.串口)
                {

                    results1 = "lower_limit=" + mESLXData.StrNordsonTempLimitS[gantryId][0] +
                         "$parametric_key=$priority=$result=" + str_NordsonTemp1 + "$sub_sub_test=$sub_test=$test=HMNozze_Temp$Message=" + err +
                         "$units=$upper_limit=" + mESLXData.StrNordsonTempLimitS[gantryId][1] +
                         "$value=" + NordsonTemp1 +   //温度T1参数

                         "$,lower_limit=" + mESLXData.StrNordPressureLimitS[gantryId][0] +
                         "$parametric_key=$priority=$result=" + str_NordPressure + "$sub_sub_test=$sub_test=$test=HMGlue_Pressure$Message=" + err +
                         "$units=$upper_limit=" + mESLXData.StrNordPressureLimitS[gantryId][1] +
                         "$value=" + NordPressure;// +  //温度T2参数

                    //屏蔽 换胶时间，自动排胶时间
                    // "$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMReplace_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrGlueReplceTime[0] +
                    // "$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMPurge_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrPurgeTime[0];

                }
                else
                {
                    results1 = "lower_limit=" + mESLXData.StrNordsonTempLimitS[gantryId][0] +
                             "$parametric_key=$priority=$result=" + str_NordsonTemp1 + "$sub_sub_test=$sub_test=$test=HMNozze_Temp$Message=" + err +
                             "$units=$upper_limit=" + mESLXData.StrNordsonTempLimitS[gantryId][1] +
                             "$value=" + NordsonTemp1 +//温度T1参数

                             "$,lower_limit=" + mESLXData.StrTubeNordsonTempLimitS[gantryId][0] +
                             "$parametric_key=$priority=$result=" + str_NordsonTemp2 + "$sub_sub_test=$sub_test=$test=HMTube_temp$Message=" + err +
                             "$units=$upper_limit=" + mESLXData.StrTubeNordsonTempLimitS[gantryId][1] +
                             "$value=" + NordsonTemp2 +//温度T2参数

                             "$,lower_limit=" + mESLXData.StrNordPressureLimitS[gantryId][0] +
                             "$parametric_key=$priority=$result=" + str_NordPressure + "$sub_sub_test=$sub_test=$test=HMGlue_Pressure$Message=" + err +
                             "$units=$upper_limit=" + mESLXData.StrNordPressureLimitS[gantryId][1] +
                             "$value=" + NordPressure; //+//压力参数
                    //屏蔽 换胶时间，自动排胶时间
                    //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMReplace_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrGlueReplceTime[0] +
                    //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMPurge_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrPurgeTime[0];
                }

            }

            #endregion
            # region 诺信_Type2_冷胶  &&无胶阀                   
            if (m_enGantryDispenserType.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type2_冷胶 ||
                m_enGantryDispenserType.dispenserType == MSystemDateDefine.enDispenserHeadType.NonUse)//诺信热胶   + sn + "@pdata@Glue Pressure@" + 0.4 + "@0.3@0.5" + "\r\n"
            {
                string str1 = "SiliconReplace_Time";
                string str2 = "SiliconPurge_Time";
                switch ((int)m_enGantryDispenserType.GlueTypeEumn)
                {
                    case 0:
                        str1 = "SiliconReplace_Time";
                        str2 = "SiliconPurge_Time";
                        break;
                    case 1:
                        str1 = "UVReplace_Time";
                        str2 = "UVPurge_Time";
                        break;
                    case 2:
                        str1 = "Conductive_Replace_Time";
                        str2 = "Conductive_Purge_Time";
                        break;
                    default: break;
                }
                results1 = "lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=" + str1 + "$Message=$units=$upper_limit=NA$value=" + mESLXData.StrGlueReplceTime[0] +
                          "$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=" + str2 + "$Message=$units=$upper_limit=NA$value=" + mESLXData.StrPurgeTime[0];
            }
            #endregion
            #region ZZT AB_NEW A,B压力，AB混合比，喷嘴温度
            if (m_enGantryDispenserType.dispenserType == MSystemDateDefine.enDispenserHeadType.Loctite)//汉高
            {
                double ABRate = 0;
                double APress = 0;
                double BPressure = 0;
                double NordsonTemp1 = 0;
                // double NordsonTemp2 = 0;

                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    ABRate = Convert.ToDouble(MESDataDefine.MESDatas.StrABRateS[gantryId]);
                    APress = Convert.ToDouble(MESDataDefine.MESDatas.StrAPressureS[gantryId]);
                    BPressure = Convert.ToDouble(MESDataDefine.MESDatas.StrBPressureS[gantryId]);
                    NordsonTemp1 = Convert.ToDouble(mESLXData.StrNordsonTempS[gantryId]);
                    //  NordsonTemp2 = Convert.ToDouble(mESLXData.StrTubeNordsonTempS[gantryId]);
                }
                else
                {
                    ABRate = frm_Main.myDispenserController[gantryId].controller_ABrate;
                    APress = frm_Main.myDispenserController[gantryId].controller_APress;
                    BPressure = frm_Main.myDispenserController[gantryId].controller_BPress;
                    NordsonTemp1 = frm_Main.myDispenserController[gantryId].controller_T1;
                    // NordsonTemp2 = frm_Main.myDispenserController[gantryId].controller_T2;
                }
                #endregion
                #region ABRate结果
                string str_ABRate = "PASS";
                try
                {
                    double ABRateLimit0 = Convert.ToDouble(MESDataDefine.MESDatas.StrABRateLimitS[gantryId][0]);
                    double ABRateLimit1 = Convert.ToDouble(MESDataDefine.MESDatas.StrABRateLimitS[gantryId][1]);
                    if ((ABRate >= ABRateLimit0) && (ABRate <= ABRateLimit1))
                    {
                        str_ABRate = "PASS";
                    }
                    else
                    {
                        str_ABRate = "FAIL";
                    }
                }
                catch (Exception)
                {
                    str_ABRate = "FAIL";
                }
                #endregion
                #region APressure结果
                string str_APressure = "PASS";
                try
                {
                    double APressureLimit0 = Convert.ToDouble(MESDataDefine.MESDatas.StrAPressureLimitS[gantryId][0]);
                    double APressureLimit1 = Convert.ToDouble(MESDataDefine.MESDatas.StrAPressureLimitS[gantryId][1]);
                    //  double APress = 5;// frm_Main.myDispenserController[0].controller_APress;
                    if ((APress >= APressureLimit0) && (APress <= APressureLimit1))
                    {
                        str_APressure = "PASS";
                    }
                    else
                    {
                        str_APressure = "FAIL";
                    }
                }
                catch (Exception)
                {
                    str_APressure = "FAIL";
                }
                #endregion
                #region BPressure结果
                string str_BPressure = "PASS";
                try
                {
                    double BPressureLimit0 = Convert.ToDouble(MESDataDefine.MESDatas.StrBPressureLimitS[gantryId][0]);
                    double BPressureLimit1 = Convert.ToDouble(MESDataDefine.MESDatas.StrBPressureLimitS[gantryId][1]);
                    if ((BPressure >= BPressureLimit0) && (BPressure <= BPressureLimit1))
                    {
                        str_BPressure = "PASS";
                    }
                    else
                    {
                        str_BPressure = "FAIL";
                    }
                }
                catch (Exception)
                {
                    str_BPressure = "FAIL";
                }
                #endregion
                #region NordsonTemp-T1,T2结果
                string str_NordsonTemp1 = "PASS", str_NordsonTemp2 = "PASS";
                try
                {
                    double NordsonTempLimit0 = Convert.ToDouble(mESLXData.StrNordsonTempLimitS[gantryId][0]);
                    double NordsonTempLimit1 = Convert.ToDouble(mESLXData.StrNordsonTempLimitS[gantryId][1]);
                    if ((NordsonTemp1 >= NordsonTempLimit0) && (NordsonTemp1 <= NordsonTempLimit1))
                    {
                        str_NordsonTemp1 = "PASS";
                    }
                    else
                    {
                        str_NordsonTemp1 = "FAIL";
                    }
                    //NordsonTempLimit0 = Convert.ToDouble(mESLXData.StrTubeNordsonTempLimitS[gantryId][0]);
                    //NordsonTempLimit1 = Convert.ToDouble(mESLXData.StrTubeNordsonTempLimitS[gantryId][1]);
                    //if ((NordsonTemp2 >= NordsonTempLimit0) && (NordsonTemp2 <= NordsonTempLimit1))
                    //{
                    //    str_NordsonTemp2 = "PASS";
                    //}
                    //else
                    //{
                    //    str_NordsonTemp2 = "FAIL";
                    //}
                }
                catch (Exception)
                {
                    str_NordsonTemp1 = "FAIL";
                    str_NordsonTemp2 = "FAIL";
                }
                #endregion              
                results1 = "lower_limit=" + MESDataDefine.MESDatas.StrABRateLimitS[gantryId][0] +
                                 "$parametric_key=$priority=$result=" + str_ABRate + "$sub_sub_test=$sub_test=$test=Mix_Ratio$Message=" + err +
                                 "$units=$upper_limit=" + MESDataDefine.MESDatas.StrABRateLimitS[gantryId][1] +
                                 "$value=" + ABRate +  //ABRate参数

                                 "$,lower_limit=" + MESDataDefine.MESDatas.StrAPressureLimitS[gantryId][0] +
                                 "$parametric_key=$priority=$result=" + str_APressure + "$sub_sub_test=$sub_test=$test=AGlue_Pressure$Message=" + err +
                                 "$units=$upper_limit=" + MESDataDefine.MESDatas.StrAPressureLimitS[gantryId][1] +
                                 "$value=" + APress + //APress参数

                                 "$,lower_limit=" + MESDataDefine.MESDatas.StrBPressureLimitS[gantryId][0] +
                                 "$parametric_key=$priority=$result=" + str_BPressure + "$sub_sub_test=$sub_test=$test=BGlue_Pressure$Message=" + err +
                                 "$units=$upper_limit=" + MESDataDefine.MESDatas.StrBPressureLimitS[gantryId][1] +
                                 "$value=" + BPressure + //BPressure参数

                                 "$,lower_limit=" + mESLXData.StrNordsonTempLimitS[gantryId][0] +
                                 "$parametric_key=$priority=$result=" + str_NordsonTemp1 + "$sub_sub_test=$sub_test=$test=HMNozze_Temp$Message=" + err +
                                 "$units=$upper_limit=" + mESLXData.StrNordsonTempLimitS[gantryId][1] +
                                 "$value=" + NordsonTemp1;// +  ////温度T1参数

                //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMReplace_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrGlueReplceTime[0] +
                //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMPurge_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrPurgeTime[0];
            }

            #endregion
            #region ZZT2_冷胶_NEW
            if (m_enGantryDispenserType.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type2_冷胶_NEW)
            {
                double NordPressure = 0;
                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    NordPressure = Convert.ToDouble(mESLXData.StrNordPressureS[gantryId]);
                }
                else
                {
                    NordPressure = frm_Main.myDispenserController[gantryId].controller_Presure;
                }
                #endregion             
                #region NordPressure结果
                string str_NordPressure = "PASS";
                try
                {
                    double NordPressureLimit0 = Convert.ToDouble(mESLXData.StrNordPressureLimitS[gantryId][0]);
                    double NordPressureLimit1 = Convert.ToDouble(mESLXData.StrNordPressureLimitS[gantryId][1]);
                    //   double NordPressure = 2;// frm_Main.myDispenserController[1].controller_Presure;

                    if ((NordPressure >= NordPressureLimit0) && (NordPressure <= NordPressureLimit1))
                    {
                        str_NordPressure = "PASS";
                    }
                    else
                    {
                        str_NordPressure = "FAIL";
                    }

                }
                catch (Exception)
                {
                    str_NordPressure = "FAIL";
                }
                #endregion

                results1 = "lower_limit=" + mESLXData.StrNordPressureLimitS[gantryId][0] +
                     "$parametric_key=$priority=$result=" + str_NordPressure + "$sub_sub_test=$sub_test=$test=HMGlue_Pressure$Message=" + err +
                     "$units=$upper_limit=" + mESLXData.StrNordPressureLimitS[gantryId][1] +
                     "$value=" + NordPressure;//+ 
                                              //屏蔽 换胶时间，自动排胶时间
                                              //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMReplace_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrGlueReplceTime[0] +
                                              //"$,lower_limit=NA$parametric_key=$priority=$result=PASS$sub_sub_test=$sub_test=$test=HMPurge_Time$Message=$units=$upper_limit=NA$value=" + mESLXData.StrPurgeTime[0];

            }

            #endregion         
            return results1;

        }

        /// <summary>
        /// 发送pdca
        /// </summary>
        public string PDCASend(string sn, string uc, string imageTime, int No)//产品SN/UC SN/图片名称/胶阀种类
        {
            MSystemDateDefine.GantryParm pGantryParm1 = MSystemDateDefine.SystemParameter.Gantry1Parm;
            MSystemDateDefine.GantryParm pGantryParm2 = MSystemDateDefine.SystemParameter.Gantry2Parm;
            id = No;
            string HeadID = "";
            if (No == 0)
            {
                StrPdcaresultL = "";
                HeadID = "Left";
            }
            else
            {
                StrPdcaresultR = "";
                HeadID = "Right";
            }

            string strPdcaBegin = "{\r\n" +
                                             sn + "@start\r\n" +
                                             sn + "@start_time@" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n"
                                            + sn + "@stop_time@" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n"
                                            ;
            string strPdca1 = "";
            string strPdca2 = "";


            MSystemDateDefine.DispenserDataClass m_enGantryDispenserType1 = MSystemDateDefine.SystemParameter.Gantry1Parm.dispenserDataClass;
            MSystemDateDefine.DispenserDataClass m_enGantryDispenserType2 = MSystemDateDefine.SystemParameter.Gantry2Parm.dispenserDataClass;



            strPdca1 = getPdcaValue(m_enGantryDispenserType1, sn, 0);
            strPdca2 = getPdcaValue(m_enGantryDispenserType1, sn, 1);


            string strPdcaEnd = sn + "@dut_pos@" + uc + "@" + HeadID + "\r\n"
                              + sn + "@log_file@" + mESLXData.StrPDCAImagePath + DateTime.Now.ToString("yyyy-MM-dd") + "/" + sn + "_" + imageTime + ".zip@" + mESLXData.StrUser + "@" + mESLXData.StrPassWord + "\r\n"////uc+str
                              + sn + "@submit@" + mESLXData.StrVersion + "\r\n}\r\n";

            string strSend = "";
            if (No == 0)
            {
                if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                    strSend = strPdcaBegin + strPdca1 + strPdca2 + strPdcaEnd;
                else if (pGantryParm1.enMatchMode == MSystemDateDefine.enMatchingMode.JustFrontStation)
                    strSend = strPdcaBegin + strPdca1 + strPdcaEnd;
                else
                    strSend = strPdcaBegin + strPdca2 + strPdcaEnd;

            }
            else
            {
                if (pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.Normalmode)
                    strSend = strPdcaBegin + strPdca1 + strPdca2 + strPdcaEnd;
                else if (pGantryParm2.enMatchMode == MSystemDateDefine.enMatchingMode.JustFrontStation)
                    strSend = strPdcaBegin + strPdca1 + strPdcaEnd;
                else
                    strSend = strPdcaBegin + strPdca2 + strPdcaEnd;
            }
            if (No == 0)
            {
                socketLeft.SendMsg(strSend);
            }
            else
            {
                socketRight.SendMsg(strSend);
            }
            if (No == 0)
                SaveDateMesL("发送PDCA:" + strSend);
            else
                SaveDateMesR("发送PDCA:" + strSend);
            return strSend;
        }

        /// <summary>
        /// 获取pdca的值
        /// </summary>
        private string getPdcaValue(MSystemDateDefine.DispenserDataClass m_enGantryDispenserType1, string sn, int gantryId)
        { //龙门1
            string strPdca1 = "";
            if (MESDataDefine.StrPurgeTime[gantryId] == "" || MESDataDefine.StrPurgeTime[gantryId] == null)
            {
                MESDataDefine.StrPurgeTime[gantryId] = DateTime.Now.ToString("yyyyMMddHHmmss");
            }


            if (gantryId == 0)    //判断前后胶阀是否屏蔽                 
                bDisableAlarmFrontOrBack[0] = MachineDataDefine.ChkStatus.IsDisableAlarmFront ? true : false;
            else
                bDisableAlarmFrontOrBack[1] = MachineDataDefine.ChkStatus.IsDisableAlarmBack ? true : false;


            #region 汉高 &&ZZT_Type3_AB胶  && ZZT_Type3_AB胶_NEW     
            if (m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.Loctite ||
                m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type3_AB胶||
                 m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type3_AB胶_NEW)//汉高,Zzt

            {
                double ABRate = 0;
                double APress = 0;
                double BPressure = 0;
                double NordsonTemp1 = 0;
                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    ABRate = Convert.ToDouble(MESDataDefine.MESDatas.StrABRateS[gantryId]);
                    APress = Convert.ToDouble(MESDataDefine.MESDatas.StrAPressureS[gantryId]);
                    BPressure = Convert.ToDouble(MESDataDefine.MESDatas.StrBPressureS[gantryId]);
                    if (m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type3_AB胶)
                        NordsonTemp1 = Convert.ToDouble(mESLXData.StrNordsonTempS[gantryId]);
                }
                else
                {
                    ABRate = frm_Main.myDispenserController[gantryId].controller_ABrate;
                    APress = frm_Main.myDispenserController[gantryId].controller_APress;
                    BPressure = frm_Main.myDispenserController[gantryId].controller_BPress;
                    if (m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type3_AB胶)
                        NordsonTemp1 = frm_Main.myDispenserController[gantryId].controller_T1;
                }
                #endregion
                if (m_enGantryDispenserType1.dispenserType != MSystemDateDefine.enDispenserHeadType.ZZT_Type3_AB胶)
                {
                    strPdca1 = sn + "@pdata@ABReplace_Time@" + DateTime.Now.ToString("yyyyMMddHHmmss") + "@NA@NA\r\n" +
                                                 sn + "@pdata@ABPurge_Time@" + DateTime.Now.ToString("yyyyMMddHHmmss") + "@NA@NA\r\n" +
                                                 sn + "@pdata@Mix_ratio@" + ABRate + "@" + mESLXData.StrABRateLimitS[gantryId][0] + "@" + mESLXData.StrABRateLimitS[gantryId][1] + "\r\n" +
                                                 sn + "@pdata@AGlue_Pressure@" + APress + "@" + mESLXData.StrAPressureLimitS[gantryId][0] + "@" + mESLXData.StrAPressureLimitS[gantryId][1] + "\r\n" +
                                                 sn + "@pdata@BGlue_Pressure@" + BPressure + "@" + mESLXData.StrBPressureLimitS[gantryId][0] + "@" + mESLXData.StrBPressureLimitS[gantryId][1] + "\r\n" +
                                                sn + "@pdata@SiliconReplace_Time@" + mESLXData.StrGlueReplceTime[gantryId] + "@NA@NA\r\n" +
                                                sn + "@pdata@SiliconPurge_Time@" + MESDataDefine.StrPurgeTime[gantryId] + "@NA@NA\r\n";
                }
                else
                    strPdca1 = sn + "@pdata@ABReplace_Time@" + DateTime.Now.ToString("yyyyMMddHHmmss") + "@NA@NA\r\n" +
                                                                     sn + "@pdata@ABPurge_Time@" + DateTime.Now.ToString("yyyyMMddHHmmss") + "@NA@NA\r\n" +
                                                                     sn + "@pdata@Mix_ratio@" + ABRate + "@" + mESLXData.StrABRateLimitS[gantryId][0] + "@" + mESLXData.StrABRateLimitS[gantryId][1] + "\r\n" +
                                                                     sn + "@pdata@AGlue_Pressure@" + APress + "@" + mESLXData.StrAPressureLimitS[gantryId][0] + "@" + mESLXData.StrAPressureLimitS[gantryId][1] + "\r\n" +
                                                                     sn + "@pdata@BGlue_Pressure@" + BPressure + "@" + mESLXData.StrBPressureLimitS[gantryId][0] + "@" + mESLXData.StrBPressureLimitS[gantryId][1] + "\r\n" +
                                                                     sn + "@pdata@AGlue_Pressure@" + NordsonTemp1 + "@" + mESLXData.StrNordsonTempLimitS[gantryId][0] + "@" + mESLXData.StrNordsonTempLimitS[gantryId][0] + "\r\n" +
                                                                     sn + "@pdata@SiliconReplace_Time@" + mESLXData.StrGlueReplceTime[gantryId] + "@NA@NA\r\n" +
                                                                     sn + "@pdata@SiliconPurge_Time@" + MESDataDefine.StrPurgeTime[gantryId] + "@NA@NA\r\n";

            }
            #endregion
            #region 高凯
            else if (m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.高凯)//诺信热胶
            {
                double NordsonTemp1 = 0;
                double NordsonTemp2 = 0;
                double NordPressure = 0;
                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    NordsonTemp1 = Convert.ToDouble(mESLXData.StrNordsonTempS[gantryId]);
                    NordsonTemp2 = Convert.ToDouble(mESLXData.StrTubeNordsonTempS[gantryId]);
                    NordPressure = Convert.ToDouble(mESLXData.StrNordPressureS[gantryId]);
                }
                else
                {
                    NordsonTemp1 = frm_Main.myDispenserController[gantryId].controller_T1;
                    NordsonTemp2 = frm_Main.myDispenserController[gantryId].controller_T2;
                    NordPressure = frm_Main.myDispenserController[gantryId].controller_Presure;
                }
                #endregion
                if (m_enGantryDispenserType1.communication == MSystemDateDefine.Communication.串口)
                {
                    strPdca1 =
                        sn + "@pdata@HMNozze_Temp@" + NordsonTemp1 + "@" + mESLXData.StrNordsonTempLimitS[gantryId][0] + "@" + mESLXData.StrNordsonTempLimitS[gantryId][1] + "\r\n" +
                        sn + "@pdata@HMTube_Temp@" + NordsonTemp2 + "@" + mESLXData.StrTubeNordsonTempLimitS[gantryId][0] + "@" + mESLXData.StrTubeNordsonTempLimitS[gantryId][1] + "\r\n" +
                        sn + "@pdata@HMGlue_Pressure@" + NordPressure + "@" + mESLXData.StrNordPressureLimitS[gantryId][0] + "@" + mESLXData.StrNordPressureLimitS[gantryId][1] + "\r\n" +
                        sn + "@pdata@SiliconReplace_Time@" + mESLXData.StrGlueReplceTime[gantryId] + "@NA@NA\r\n" +
                        sn + "@pdata@SiliconPurge_Time@" + MESDataDefine.StrPurgeTime[gantryId] + "@NA@NA\r\n";
                }

            }
            #endregion
            #region 诺信Type1_热胶 && ZZT_Type1_热胶_NEW
            else if (m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type1_热胶)//诺信热胶
            {
                double NordsonTemp1 = 0;
                double NordsonTemp2 = 0;
                double NordPressure = 0;
                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    NordsonTemp1 = Convert.ToDouble(mESLXData.StrNordsonTempS[gantryId]);
                    NordsonTemp2 = Convert.ToDouble(mESLXData.StrTubeNordsonTempS[gantryId]);
                    NordPressure = Convert.ToDouble(mESLXData.StrNordPressureS[gantryId]);
                }
                else
                {
                    NordsonTemp1 = frm_Main.myDispenserController[gantryId].controller_T1;
                    NordsonTemp2 = frm_Main.myDispenserController[gantryId].controller_T2;
                    NordPressure = frm_Main.myDispenserController[gantryId].controller_Presure;
                }
                #endregion
                if (m_enGantryDispenserType1.communication == MSystemDateDefine.Communication.串口)
                {

                    strPdca1 =
                               sn + "@pdata@HMNozze_Temp@" + NordsonTemp1 + "@" + mESLXData.StrNordsonTempLimitS[gantryId][0] + "@" + mESLXData.StrNordsonTempLimitS[gantryId][1] + "\r\n" +
                               sn + "@pdata@HMGlue_Pressure@" + frm_Main.myDispenserController[gantryId].controller_Presure + "@" + mESLXData.StrNordPressureLimitS[gantryId][0] + "@" + mESLXData.StrNordPressureLimitS[gantryId][1] + "\r\n" +
                               sn + "@pdata@SiliconReplace_Time@" + mESLXData.StrGlueReplceTime[gantryId] + "@NA@NA\r\n" +
                               sn + "@pdata@SiliconPurge_Time@" + MESDataDefine.StrPurgeTime[gantryId] + "@NA@NA\r\n";
                }
                if (m_enGantryDispenserType1.communication == MSystemDateDefine.Communication.网口)
                {
                    strPdca1 =
                               sn + "@pdata@HMNozze_Temp@" + NordsonTemp1 + "@" + mESLXData.StrNordsonTempLimitS[gantryId][0] + "@" + mESLXData.StrNordsonTempLimitS[gantryId][1] + "\r\n" +
                               sn + "@pdata@HMTube_Temp@" + NordsonTemp2 + "@" + mESLXData.StrTubeNordsonTempLimitS[gantryId][0] + "@" + mESLXData.StrTubeNordsonTempLimitS[gantryId][1] + "\r\n" +
                              sn + "@pdata@HMGlue_Pressure@" + frm_Main.myDispenserController[gantryId].controller_Presure + "@" + mESLXData.StrNordPressureLimitS[gantryId][0] + "@" + mESLXData.StrNordPressureLimitS[gantryId][1] + "\r\n" +
                              sn + "@pdata@SiliconReplace_Time@" + mESLXData.StrGlueReplceTime[gantryId] + "@NA@NA\r\n" +
                              sn + "@pdata@SiliconPurge_Time@" + MESDataDefine.StrPurgeTime[gantryId] + "@NA@NA\r\n";
                }

            }

            #endregion
            #region  ZZT2_冷胶&& ZZT2_冷胶_NEW
            if (m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type2_冷胶_NEW||
               m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.ZZT_Type2_冷胶 )
            {
                double NordPressure = 0;
                #region  固定值还是实时值
                if (bDisableAlarmFrontOrBack[gantryId])
                {
                    NordPressure = Convert.ToDouble(mESLXData.StrNordPressureS[gantryId]);
                }
                else
                {
                    NordPressure = frm_Main.myDispenserController[gantryId].controller_Presure;
                }
                #endregion
                strPdca1 = sn + "@pdata@HMGlue_Pressure@" + NordPressure + "@" + mESLXData.StrNordPressureLimitS[gantryId][0] + "@" + mESLXData.StrNordPressureLimitS[gantryId][1] + "\r\n" +
                            sn + "@pdata@SiliconReplace_Time@" + mESLXData.StrGlueReplceTime[gantryId] + "@NA@NA\r\n" +
                            sn + "@pdata@SiliconPurge_Time@" + MESDataDefine.StrPurgeTime[gantryId] + "@NA@NA\r\n";
            }

            #endregion
            #region Nordson_Type2_冷胶      
            else if (m_enGantryDispenserType1.dispenserType == MSystemDateDefine.enDispenserHeadType.Nordson_Type2_冷胶)//诺信冷胶
            {

                strPdca1 = sn + "@pdata@SiliconReplace_Time@" + mESLXData.StrGlueReplceTime[gantryId] + "@NA@NA\r\n" +
                           sn + "@pdata@SiliconPurge_Time@" + MESDataDefine.StrPurgeTime[gantryId] + "@NA@NA\r\n";
            }
            #endregion
            return strPdca1;

        }
        object locker = new object();
        public void SaveDateMesL(string mes, bool iswrite = true)
        {
            try
            {
                lock (locker)
                {
                    string fileName;
                    fileName = string.Format("{0}.txt", DateTime.Now.ToString("yyyy_MM_dd"));
                    string outputPath = @"D:\DATA\MES记录L";
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    string fullFileName = Path.Combine(outputPath, fileName);
                    System.IO.FileStream fs;
                    //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                    StreamWriter sw;
                    if (!File.Exists(fullFileName))
                    {
                        fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                        //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        if (iswrite)
                            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "__" + mes);
                        else
                            sw.WriteLine(DateTime.Now.ToString("\r\nHH:mm:ss") + "__" + mes);
                        sw.Close();
                        fs.Close();

                    }
                    else
                    {
                        fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                        //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        if (iswrite)
                            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "__" + mes);
                        else
                            sw.WriteLine(DateTime.Now.ToString("\r\nHH:mm:ss") + "__" + mes);
                        sw.Close();
                        fs.Close();
                    }

                }
            }
            catch
            {


            }
        }

        public void SaveDateMesR(string mes, bool iswrite = true)
        {
            try
            {
                lock (locker)
                {
                    string fileName;
                    fileName = string.Format("{0}.txt", DateTime.Now.ToString("yyyy_MM_dd"));
                    string outputPath = @"D:\DATA\MES记录R";
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    string fullFileName = Path.Combine(outputPath, fileName);
                    System.IO.FileStream fs;
                    //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                    StreamWriter sw;
                    if (!File.Exists(fullFileName))
                    {
                        fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                        //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        if (iswrite)
                            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + mes);
                        else
                            sw.WriteLine(DateTime.Now.ToString("\r\nHH:mm:ss") + mes);
                        sw.Close();
                        fs.Close();

                    }
                    else
                    {
                        fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                        //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        if (iswrite)
                            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + mes);
                        else
                            sw.WriteLine(DateTime.Now.ToString("\r\nHH:mm:ss") + mes);
                        sw.Close();
                        fs.Close();
                    }

                }
            }
            catch
            {


            }
        }





        #region  MES上传图片的部分

        private string[] MESImageOutputPathCopy = new string[2];
        private string[] MESImageOutputPath = new string[2];

        private string[] LocalImageFileCopy = new string[2];
        private string[] LocalImageFile = new string[2];
        public bool ConnectNetUse(string Zipname, int No)
        {
            //    string ImagePath = myIniFile.IniReadValue("Macmini", "ImagePath");
            bool result = false;
            try
            {
                updataTime = DateTime.Now.ToString("HH");
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                MESImageOutputPathCopy[No] = @"Y:\" + date + "\\" + updataTime + "\\" + mESLXData.StrLine + "\\" + mESLXData.StrShortStation + "\\" + mESLXData.StrStationID;
                MESImageOutputPath[No] = @"Y:\\" + date + "\\\\" + updataTime + "\\\\" + mESLXData.StrLine + "\\\\" + mESLXData.StrShortStation + "\\\\" + mESLXData.StrStationID;
                if (!Directory.Exists(MESImageOutputPathCopy[No]))
                {
                    Directory.CreateDirectory(MESImageOutputPathCopy[No]);
                }
                LocalImageFileCopy[No] = @"E:\IMG\" + date + "\\" + Zipname;
                LocalImageFile[No] = @mESLXData.StrLocalImage + date + @"\\" + Zipname;

                File.Copy(LocalImageFileCopy[No], MESImageOutputPathCopy[No] + "\\" + Zipname, true);
                result = true;

            }
            catch (Exception EX)
            {
                result = false;
            }
            return result;
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="uc"></param>
        /// <param name="Zipname">文件本地路径</param>
        /// <param name="RL"></param>
        /// <param name="updata">TRUE  上传</param>
        /// <returns></returns>
        public string MesSendImageREC(string sn, string uc, string Zipname, int RL, bool updata)
        {                            // SN          UC        文件名时间    流道标识     上传远程
            lock (lock1)
            {
                string dat = "";
                try
                {
                    if (updata)
                    {
                        dat = "{" + PostImageBady(sn, Zipname, LocalImageFile[RL], MESImageOutputPath[RL]) + "}";//为空的是否可以不填写
                        if (RL == 0)
                            SaveDateMesL("发送mes存图报文:" + dat);
                        else
                            SaveDateMesR("发送mes存图报文:" + dat);
                        string result = "";
                        //江西更新网络后后再开放
                        //WebServiceSoapClient WebClient = new WebServiceSoapClient();
                        //string result = WebClient.SendDataToMES(dat);
                        string value = "";
                        if (result.Contains("OK"))
                            value = "OK";
                        else
                            value = "NG";
                        if (RL == 0)
                            SaveDateMesL("收到mes存图反馈: " + value + "  " + result);
                        else
                            SaveDateMesR("收到mes存图反馈: " + value + "  " + result);

                        return result;
                    }
                    else
                    {
                        if (RL == 0)
                            SaveDateMesL("发送mes存图报文:" + "上传异常--存入DATA——左流道上传图片缓存");
                        else
                            SaveDateMesR("发送mes存图报文:" + "上传异常--存入DATA——右流道上传图片缓存");

                        dat = "{" + PostImageBady(sn, Zipname, LocalImageFile[RL], MESImageOutputPath[RL]) + "}";//为空的是否可以不填写
                        if (RL == 0)
                        {
                            string pdcaPath1 = @"D:\DATA\左流道上传图片缓存\";
                            string fullFileName = Path.Combine(pdcaPath1, Zipname.Substring(0, Zipname.Length - 4) + ".txt");
                            if (!Directory.Exists(pdcaPath1))
                            {
                                Directory.CreateDirectory(pdcaPath1);
                            }
                            File.WriteAllText(fullFileName, dat + "\r" + LocalImageFileCopy[RL] + "\r" + MESImageOutputPathCopy[RL]);
                            File.AppendText("\r" + LocalImageFileCopy[RL] + "\r" + MESImageOutputPathCopy[RL]);
                        }
                        else
                        {
                            string pdcaPath1 = @"D:\DATA\右流道上传图片缓存\";
                            string fullFileName = Path.Combine(pdcaPath1, Zipname.Substring(0, Zipname.Length - 4) + ".txt");
                            if (!Directory.Exists(pdcaPath1))
                            {
                                Directory.CreateDirectory(pdcaPath1);
                            }
                            File.WriteAllText(fullFileName, dat + "\r" + LocalImageFileCopy[RL] + "\r" + MESImageOutputPathCopy[RL]);
                        }
                        return "文件Copy异常";
                    }
                }
                catch (Exception ex)
                {
                    return "上传远程异常:" + ex.ToString();

                }
            }

        }

        /// </summary>
        public string PostImageBady(string sn, string zipname, string localImageFile, string MESImageOutputPath)
        {

            string COMMAND = "SendData";
            string FILENAME = zipname;
            string FILETYPE = "zip";
            string LOCALPATH = localImageFile;
            //去除分享盘中的Y:\\
            string REMOTEPATH = "abg_pic\\\\" + MESImageOutputPath.Substring(4);

            string result = "\"COMMAND\":\"" + COMMAND + "\"," + "\"SERIAL_NUMBER\":\"" + sn + "\"," + "\"LINE_NAME\":\"" + mESLXData.StrLine + "\"," +
                "\"TERMINAL_NAME\":\"" + mESLXData.StrStationID + "\"," + "\"EQUIPMENTNAME\":\"" + mESLXData.StrMachineID + "\"," + "\"FILENAME\":\"" + FILENAME + "\"," +
                "\"FILETYPE\":\"" + FILETYPE + "\"," + "\"LOCALPATH\":\"" + LOCALPATH + "\"," + "\"REMOTEPATH\":\"" + REMOTEPATH + "\"";

            return result;
        }



        #endregion

    }
}