using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using cowain.FlowWork;
using ToolTotal;
using cowain.WebReferencePicture;

namespace cowain.MES
{
    public class LXPOSTClass
    {
        private static object locker1 = new object();
        private static object Copylocker = new object();
        private static object NGlocker = new object();

        private ABGService sendPicService;
        private static LXPOSTClass instace;
        private string updataTime = "";
        private static object lock1 = new object();
        /// <summary>
        /// PDCA网络
        /// </summary>
        public static NetSocket clientOfPDCA;
        public static object locker = new object();
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
        public string StrPdcaresult = "";
        HttpWebRequest Web = null;
        HttpWebResponse Res = null;
        IniFile myIniFile;

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
            clientOfPDCA = new NetSocket();
            sendPicService = new ABGService();
            string ip = MESDataDefine.MESData.StrMiniIP;
            string port = MESDataDefine.MESData.StrMiniPort;
            if (!MESDataDefine.MESData.IsDisablePDCA)
            {
                Task.Run(() =>
                {
                    clientOfPDCA.Open(ip, Convert.ToInt32(port));
                    clientOfPDCA.receiveDoneSocketEvent += SocketEvent;
                });
            }
        }
        public void SocketEvent(string msgStr)
        {
            StrPdcaresult = StrPdcaresult + msgStr.Trim();
            SaveDateMes("_PDCA反馈信息:" + msgStr + "\r\n");
        }

        #region LUXSAN
        public string MES_SendPressure(Product product)
        {
            try
            {
                string sendMessage = "c=ADD_ATTR&product=D63" +
                                       "&mac_address=" + MESDataDefine.MESData.Mac_Address +
                                       "&start_time=" + product.EnterTime +
                                       "&sn=" + product.SN +
                                       "&emp_id=" + "89014103212450982048" +
                                       "&Press1=" + product.PressOne +
                                       "&Press2=" + product.PressTwo +
                                       "&Press3=" + product.PressThree +
                                       "&Press4=" + product.PressFour +
                                       "&Press5=" + product.PressFive +
                                       "&VisionResult1=" + product.VisionResultOne +
                                       "&VisionResult2=" + product.VisionResultTwo +
                                       "&VisionResult3=" + product.VisionResultThree;

                SaveDateMes("发送压力和视觉结果:\r\n" + sendMessage);

                Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
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
                SaveDateMes("获取上传压力结果:\r\n" + strResult);

                return strResult;
            }
            catch (Exception EX)
            {
                return string.Empty;
            }
        }

        public string MES_SendPressureVisionResult(string sn, string pressure, string visionResult)
        {
            try
            {
                string sendMessage = "c=ADD_ATTR&product=D63" +
                                       "&mac_address=" + MESDataDefine.MESData.Mac_Address +
                                       "&start_time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + //product.EnterTime
                                       "&sn=" + sn +
                                       "&emp_id=" + "89014103212450982048" +
                                       "&Press=" + pressure +
                                       "&VisionResult1=" + visionResult;

                SaveDateMes("发送压力和视觉结果:\r\n" + sendMessage);

                Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
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
                SaveDateMes("获取上传压力结果:\r\n" + strResult);

                return strResult;
            }
            catch (Exception EX)
            {
                return string.Empty;
            }
        }

        public string MES_SendPressureSingle(string sn, string pressure)
        {
            try
            {
                string sendMessage = "c=ADD_ATTR&product=D63" +
                                       "&mac_address=" + MESDataDefine.MESData.Mac_Address +
                                       "&start_time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + //product.EnterTime
                                       "&sn=" + sn +
                                       "&emp_id=" + "89014103212450982048" +
                                       "&Press=" + pressure;

                SaveDateMes("发送压力:\r\n" + sendMessage);

                Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
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
                SaveDateMes("获取上传压力结果:\r\n" + strResult);

                return strResult;
            }
            catch (Exception EX)
            {
                return string.Empty;
            }
        }

        public string MES_SendUCGetSN(string UC)
        {
            try
            {
                string sendMessage = "c=QUERY_4_SFC&subcmd=get_test_record" +
                                       "&carrier_sn=" + UC +
                                       "&station_code=" + MESDataDefine.MESData.Station_Code +
                                       "&station_id=" + MESDataDefine.MESData.Station_ID;

                SaveDateMes("发送UC码:\r\n" + sendMessage);

                Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
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
                SaveDateMes("获取SN码:\r\n" + strResult);

                return strResult;
            }
            catch (Exception EX)
            {
                return string.Empty;
            }
        }

        public string MES_TestRecode(string SN)
        {
            try
            {
                string sendMessage = "result=PASS&c=ADD_RECORD&product=D63" +
                                       "&test_station_name=" + MESDataDefine.MESData.Station_Code +
                                       "&station_id=" + MESDataDefine.MESData.Station_ID +
                                       "&mac_address=" + MESDataDefine.MESData.Mac_Address +
                                       "&sw_version=" + MESDataDefine.MESData.SW_Version +
                                       "&start_time=" + "2021-05-07 13:02:25" +
                                       "&stop_time=" + "2021-05-07 13:02:25" +
                                       "&sn=" + "MLB SN";

                SaveDateMes("发送测试数据:\r\n" + sendMessage);

                Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
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
                SaveDateMes("获取测试数据结果:\r\n" + strResult);

                return strResult;
            }
            catch (Exception EX)
            {
                return string.Empty;
            }
        }

        #endregion

        #region MES       

        /// <summary>
        /// mes 获取SN
        /// </summary>
        /// <param name="ucsn">UC码</param>
        /// <param name="headid">左右流道</param>
        /// <returns></returns>
        public string MES_SendUCGetSn(string ucsn)
        {
            lock (lock1)
            {
                try
                {
                    string sendMessage = MESDataDefine.MESData.StrGetSNDat + ucsn;

                    SaveDateMes("发送UC获取SN:\r\n" + sendMessage, false);

                    Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
                    byte[] data = Encoding.UTF8.GetBytes(sendMessage);
                    Web.Method = "POST";
                    Web.ContentType = "application/x-www-form-urlencoded";
                    Web.ContentLength = data.Length;
                    using (var stream = Web.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    Web.KeepAlive = false;

                    Web.AllowAutoRedirect = true;
                    Web.CookieContainer = new System.Net.CookieContainer();
                    ServicePointManager.Expect100Continue = false;
                    ServicePointManager.MaxServicePointIdleTime = 2000;
                    Res = (HttpWebResponse)Web.GetResponse();
                    string str = "";
                    using (StreamReader sr = new StreamReader(Res.GetResponseStream(), Encoding.UTF8))
                    {
                        str = sr.ReadToEnd();
                    }
                    SaveDateMes("获取SN:\r\n" + str);

                    return str;
                }
                catch (Exception EX)
                {

                    return "";
                }
            }
        }

        /// <summary>
        /// check胶水码 2022.02.21新增
        /// </summary>
        /// <param name="frontOrBack">0:前龙门/1:后龙门</param>
        /// <param name="m_iRunnerID">流道</param>
        /// <returns></returns>
        public string MesCheckGlue(int frontOrBack, int m_iRunnerID)
        {
            lock (lock1)
            {
                try
                {
                    string sendMessage = "CMD=ATT&P=GET_GLUE_USE_STATUS&SN=";// + (frontOrBack == 0 ? MESDataDefine.MESData.StrGlueSNFront.Replace("+", "%2b") : MESDataDefine.MESData.StrGlueSNBack.Replace("+", "%2b")) +"&Station_ID=" + MESDataDefine.MESData.StrStationID;

                    SaveDateMes("发送CheckGlue数据:\r\n" + sendMessage, false);

                    Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
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
                    SaveDateMes("获取CheckGlue结果:\r\n" + str);

                    return str;
                }
                catch (Exception EX)
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// check胶水码 SN OP UC
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="uc"></param>
        /// <param name="gantryId"></param>
        /// <returns></returns>
        public string MesUOP(string sn, string uc)
        {
            lock (lock1)
            {
                try
                {
                    string GlueSN = "";

                    GlueSN = GlueSN.Replace("+", "%2B");
                    string sendMessage = "cmd=UOP&sn=" + sn + "&Station_iD=" + MESDataDefine.MESData.StrStationID + "&op=" + MESDataDefine.MESData.StrOp + "&lotno=" + GlueSN + "&Fixture_ID=" + uc + "&kpsn=";

                    String RL = "Left";

                    String str = "&wo=&"
                                  //+ "sn=" + sn                         //CMD=REC&wo=&sn=//myIniFile.IniReadValue("WebMes", "RecDat1")
                                  //+ "&op=" + MESDataDefine.MESData.StrOp + "&"
                                  //+ "kpsn=" + "" + "&"
                                  //+ "lotno=" + GlueSN + "&"//胶水sn
                                  //+ "Fixture_ID=" + uc + "&"
                                  + "machine=" + MESDataDefine.MESData.StrMachineID + "&"
                                  + "Cavity=" + RL + "&"
                                  //+ "Station_ID=" + MESDataDefine.MESData.StrStationID + "&"
                                  ;
                    String testdata = "testdata=test_result=PASS" + "$"
                                       + "unit_sn=" + sn + "$"
                                       + "uut_start=" + MESDataDefine.StrStartTimeDelayS[0] + "$"
                                       + "uut_stop=" + MESDataDefine.StrStopTime[0] + "$"
                                       + "limits_version=" + "$"
                                       + "software_name =" + "Cowain_AutoDispenser" + "$"
                                       + "software_version=" + MESDataDefine.MESData.SW_Version + "$"
                                       + "station_id=" + MESDataDefine.MESData.StrStationID + "$"
                                       + "fixture_id=" + uc + "$"
                                       + "Head_ID=" + RL + "&";
                    sendMessage = sendMessage + str + testdata;

                    SaveDateMes("发送SN_check_mes:\r\n" + sendMessage);
                    Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
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

                    SaveDateMes("check_mes_SN结果:\r\n" + strResult);

                    return strResult;
                }
                catch { return ""; }
            }

        }

        /// <summary>
        /// REC模式启用pdca和mes，开始REC上传MES
        /// ADD模式启用mes禁用pdca，开始ADD上传MES
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="uc"></param>
        /// <param name="RL"></param>
        /// <param name="No"></param>
        /// <param name="addORrec"></param>
        /// <returns></returns>
        public string MesSend(string sn, string uc, string addORrec)
        {
            lock (lock1)
            {
                try
                {
                    if (addORrec == "ADD")
                        MESDataDefine.MESData.StrLinkSNDat = "CMD=ADD";
                    else
                        MESDataDefine.MESData.StrLinkSNDat = "CMD=REC";

                    string sendMessage = MESDataDefine.MESData.StrLinkSNDat + PostBody(sn, uc);//为空的是否可以不填写

                    SaveDateMes("发送Link Sn结果:\r\n" + sendMessage);
                    Web = (HttpWebRequest)WebRequest.Create(MESDataDefine.MESData.StrURL);
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
                    SaveDateMes("收到Link Sn反馈:\r\n" + str);

                    return str;
                }
                catch (Exception ex)
                {
                    SaveDateMes("收到Link Sn反馈:\r\n" + ex.ToString());
                    return "";
                }
            }
        }
        /// <summary>
        /// MES上传的信息体
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="uc"></param>
        /// <param name="RL"></param>
        /// <param name="No"></param>
        /// <returns></returns>
        public string PostBody(string sn, string uc)
        {
            string str = "";
            string testdata = "";
            string results = "";
            string GlueSN = "";
            string RL = "Left";
            GlueSN = GlueSN.Replace("+", "%2B");
            str =
                   "&wo=&" +
                   "sn=" + sn +                        //CMD=REC&wo=&sn=//myIniFile.IniReadValue("WebMes", "RecDat1")
                  "&op=" + MESDataDefine.MESData.StrOp + "&"
                   + "kpsn=" + "" + "&"
                   + "lotno=" + GlueSN + "&"//胶水sn
                   + "Fixture_ID=" + uc + "&"
                   + "machine=" + MESDataDefine.MESData.StrMachineID + "&"
                   + "Cavity=" + RL + "&"
                   + "Station_ID=" + MESDataDefine.MESData.StrStationID + "&";
            testdata = "testdata=test_result=PASS" + "$"
                              + "unit_sn=" + sn + "$"
                              + "uut_start=" + MESDataDefine.StrStartTimeDelayS[0] + "$"
                              + "uut_stop=" + MESDataDefine.StrStopTime[0] + "$"
                              + "limits_version=" + "$"
                              + "software_name =" + "Cowain_AutoDispenser" + "$"
                              + "software_version=" + MESDataDefine.MESData.SW_Version + "$"
                              + "station_id=" + MESDataDefine.MESData.StrStationID + "$"
                              + "fixture_id=" + uc + "$"
                              + "Head_ID=" + RL + "&";

            str = str + testdata;
            str = str + "results=" + results;

            return str;
        }
        #endregion

        #region PDCA

        /// <summary>
        /// 产品SN/UC SN/图片名称
        /// </summary>
        public string PDCASend(string sn, string uc, string imageTime)
        {
            string HeadID = "";
            StrPdcaresult = "";
            HeadID = "Left";

            string strPdcaBegin = "{\r\n" +
                                    sn + "@start\r\n" +
                                    sn + "@start_time@" + MESDataDefine.StrStartTimeDelayS[0] + "\r\n" +
                                    sn + "@stop_time@" + MESDataDefine.StrStopTime[0] + "\r\n";

            string strPdca = "";
            String GlueSN = "";


            string strPdcaEnd = sn + "@dut_pos@" + uc + "@" + HeadID + "\r\n"
                              + sn + "@log_file@" + MESDataDefine.MESData.StrPDCAImagePath + DateTime.Now.ToString("yyyy-MM-dd") + "/" + sn + "_" + imageTime + ".zip@" + MESDataDefine.MESData.StrUser + "@" + MESDataDefine.MESData.StrPassWord + "\r\n"////uc+str
                              + sn + "@submit@" + MESDataDefine.MESData.SW_Version + "\r\n}\r\n";

            string strSend = "";
            #region 选用 前后龙门胶水信息组合+胶水SN
            strSend = strPdcaBegin + GlueSN + strPdca + strPdcaEnd;
            #endregion

            #region 发送PDCA数据  SendMsg
            if (clientOfPDCA.ClientSocket != null) //如果实例化过，则只连接
            {
                if (!clientOfPDCA.ClientSocket.Connected || !clientOfPDCA.connectOk)
                {
                    clientOfPDCA.Open(MESDataDefine.MESData.StrMiniIP, Convert.ToInt32(MESDataDefine.MESData.StrMiniPort));
                }
                else if (clientOfPDCA.ClientSocket.Connected && clientOfPDCA.connectOk)
                    clientOfPDCA.SendMsg(strSend);
            }
            else//如果 没有 实例化过，连接并绑定
            {
                clientOfPDCA.Open(MESDataDefine.MESData.StrMiniIP, Convert.ToInt32(MESDataDefine.MESData.StrMiniPort));
                clientOfPDCA.receiveDoneSocketEvent += SocketEvent;
                SaveDateMes("-----------PDCA没有实例化！----现实例化并连接-----");
                clientOfPDCA.SendMsg(strSend);

            }

            #endregion

            SaveDateMes("发送PDCA:\r\n" + strSend);
            return strSend;
        }
        #endregion

        #region MES log
        public void SaveDateMes(string mes, bool iswrite = true)
        {
            try
            {
                lock (locker)
                {
                    string fileName;
                    fileName = string.Format("{0}.txt", DateTime.Now.ToString("yyyy_MM_dd"));
                    string outputPath = @"D:\DATA\MES";
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
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        if (iswrite)
                            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "——" + mes + "\r\n");
                        else
                            sw.WriteLine(DateTime.Now.ToString("\r\nHH:mm:ss") + "——" + mes + "\r\n");
                        sw.Close();
                        fs.Close();

                    }
                    else
                    {
                        fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                        //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        if (iswrite)
                            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "——" + mes + "\r\n");
                        else
                            sw.WriteLine(DateTime.Now.ToString("\r\nHH:mm:ss") + "——" + mes + "\r\n");
                        sw.Close();
                        fs.Close();
                    }
                }
            }
            catch
            {

            }
        }
        #endregion

        #region  MES上传图片的部分

        #region 图片文件拷贝---未使用
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
                MESImageOutputPathCopy[No] = @"Y:\" + date + "\\" + updataTime + "\\" + MESDataDefine.MESData.StrLine + "\\" + MESDataDefine.MESData.Mac_Address + "\\" + MESDataDefine.MESData.StrStationID;
                MESImageOutputPath[No] = @"Y:\\" + date + "\\\\" + updataTime + "\\\\" + MESDataDefine.MESData.StrLine + "\\\\" + MESDataDefine.MESData.Mac_Address + "\\\\" + MESDataDefine.MESData.StrStationID;
                if (!Directory.Exists(MESImageOutputPathCopy[No]))
                {
                    Directory.CreateDirectory(MESImageOutputPathCopy[No]);
                }
                LocalImageFileCopy[No] = @"E:\IMG\" + date + "\\" + Zipname;
                LocalImageFile[No] = @MESDataDefine.MESData.StrLocalPath + date + @"\\" + Zipname;

                File.Copy(LocalImageFileCopy[No], MESImageOutputPathCopy[No] + "\\" + Zipname, true);
                result = true;

            }
            catch (Exception EX)
            {
                result = false;
            }
            return result;
        }
        #endregion

        public string MesUpPicResult = "";

        public void SaveDatePic(string mes, string Date)
        {
            try
            {
                lock (locker)
                {
                    string fileName;
                    fileName = string.Format("{0}.txt", Date);
                    string outputPath = @"D:\DATA\上传图片记录";
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
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + mes);
                        sw.Close();
                        fs.Close();

                    }
                    else
                    {
                        fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                        //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + mes);
                        sw.Close();
                        fs.Close();
                    }

                }
            }
            catch
            {


            }
        }

        /// <summary>
        /// mes上传图片相关(获取zip路径名字)
        /// </summary>
        public static string getFiles_Path(string SN, string Time)
        {
            try
            {
                List<string> list_filename = new List<string>();
                //string str = DateTime.Now.ToString("yyyy-MM-dd");
                string dictionary = Path.Combine("E:\\IMG\\", Time);
                DirectoryInfo info = new DirectoryInfo(dictionary);
                FileInfo[] files = info.GetFiles();
                string filename = "";
                string filenameTmp = "";
                for (int i = 0; i < files.Length; i++)
                {
                    if (files[i].Name.Contains(SN))
                    {
                        filenameTmp = files[i].FullName;
                        list_filename.Add(filenameTmp);
                        continue;
                    }
                }

                if (list_filename.Count > 0)
                    filename = list_filename[list_filename.Count - 1];
                else
                    filename = "";
                return filename;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        /// <summary>
        /// mes上传图片相关(获取zip文件名字)
        /// </summary>
        public static string getFiles(string SN)
        {
            try
            {
                List<string> list_filename = new List<string>();
                string str = DateTime.Now.ToString("yyyy-MM-dd");
                string dictionary = Path.Combine("E:\\IMG\\", str);
                DirectoryInfo info = new DirectoryInfo(dictionary);
                FileInfo[] files = info.GetFiles();
                string filename = "";
                string filenameTmp = "";
                for (int i = 0; i < files.Length; i++)
                {
                    if (files[i].Name.Contains(SN))
                    {
                        filenameTmp = files[i].Name;
                        list_filename.Add(filenameTmp);
                        continue;
                    }
                }

                if (list_filename.Count > 0)
                    filename = list_filename[list_filename.Count - 1];
                else
                    filename = "";
                return filename;
            }
            catch (Exception ex)
            {
                return "";
            }
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
                        SaveDateMes("发送mes存图报文:\r\n" + dat);
                        string result = "";
                        //江西更新网络后后再开放
                        //WebServiceSoapClient WebClient = new WebServiceSoapClient();
                        //string result = WebClient.SendDataToMES(dat);
                        string value = "";
                        if (result.Contains("OK"))
                            value = "OK";
                        else
                            value = "NG";
                        SaveDateMes("收到mes存图反馈: " + value + "  " + result);

                        return result;
                    }
                    else
                    {
                        SaveDateMes("发送mes存图报文:" + "上传异常--存入DATA——左流道上传图片缓存");

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

            string result = "\"COMMAND\":\"" + COMMAND + "\","
                            + "\"SERIAL_NUMBER\":\"" + sn + "\","
                            + "\"LINE_NAME\":\"" + MESDataDefine.MESData.StrLine + "\","
                            + "\"TERMINAL_NAME\":\"" + MESDataDefine.MESData.StrStationID + "\","
                            + "\"EQUIPMENTNAME\":\"" + MESDataDefine.MESData.StrMachineID + "\","
                            + "\"FILENAME\":\"" + FILENAME + "\","
                            + "\"FILETYPE\":\"" + FILETYPE + "\","
                            + "\"LOCALPATH\":\"" + LOCALPATH + "\","
                            + "\"REMOTEPATH\":\"" + REMOTEPATH + "\"";

            return result;
        }
        #region 正常生产上传图片
        public string MesSendPic(string sn, string time)
        {
            lock (NGlocker)
            {
                try
                {
                    MesUpPicResult = "";
                    string re = "结果出错";
                    string tagInfo = StrToJson(sn, time);

                    SaveDatePic("上传图片发送mes结果:" + tagInfo, DateTime.Now.ToString("yyyy_MM_dd"));

                    #region 换接口
                    string picPath = getFiles_Path(sn, DateTime.Now.ToString("yyyy-MM-dd"));
                    if (picPath != "")
                    {
                        FileStream picZipFile_Stream = new FileStream(picPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        byte[] picByte = new byte[picZipFile_Stream.Length];
                        picZipFile_Stream.Read(picByte, 0, picByte.Length);
                        picZipFile_Stream.Dispose();
                        picZipFile_Stream.Close();
                        re = sendPicService.SendDataToMES(tagInfo, picByte);
                    }
                    #endregion

                    //string re = web1.SendDataToMES(tagInfo);

                    SaveDatePic("上传图片收到mes反馈:" + re, DateTime.Now.ToString("yyyy_MM_dd"));

                    MesUpPicResult = JsonToStr(re, sn);


                    return MesUpPicResult;
                }
                catch (Exception e)
                {
                    //MDataDefine.SaveMesError("Mes传图报错行数：" + e.StackTrace + "-----报错原因：" + e.Message);
                    return "";
                }
            }
        }
        public string MesSendPicManual(string sn, string time, int RL, ref string message, ref string remessage)
        {
            string tagInfo = StrToJson(sn, time);

            message = tagInfo;

            SaveDatePic("上传图片发送mes结果:" + tagInfo, DateTime.Now.ToString("yyyy_MM_dd"));

            #region 换接口，要怪怪27，
            string picPath = getFiles_Path(sn, DateTime.Now.ToString("yyyy-MM-dd"));
            FileStream picZipFile_Stream = new FileStream(picPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] picByte = new byte[picZipFile_Stream.Length];
            picZipFile_Stream.Read(picByte, 0, picByte.Length);
            picZipFile_Stream.Dispose();
            picZipFile_Stream.Close();
            string re = sendPicService.SendDataToMES(tagInfo, picByte);
            #endregion

            //string re = web1.SendDataToMES(tagInfo);

            remessage = re;
            SaveDatePic("上传图片收到mes反馈:" + re, DateTime.Now.ToString("yyyy_MM_dd"));

            string success = JsonToStr(re, sn);

            return success;
        }
        public string StrToJson(string sn, string time)
        {
            try
            {
                string[] key = new string[9];
                key[0] = "COMMAND";
                key[1] = "SERIAL_NUMBER";
                key[2] = "LINE_NAME";
                key[3] = "TERMINAL_NAME";
                key[4] = "EQUIPMENTNAME";
                key[5] = "FILENAME";
                key[6] = "FILETYPE";
                key[7] = "LOCALPATH";
                key[8] = "REMOTEPATH";

                string[] value = new string[9];
                value[0] = "SendData";
                value[1] = sn;


                string LineID = MESDataDefine.MESData.StrLine;//myIniFile.IniReadValue("WebMes", "LineID");
                value[2] = LineID;
                string StationID = MESDataDefine.MESData.StrStationID;//myIniFile.IniReadValue("WebMes", "StationID");
                value[3] = StationID;
                string MachineID = MESDataDefine.MESData.StrMachineID; //myIniFile.IniReadValue("Macmini", "MachineID");
                value[4] = MachineID;
                value[5] = sn + "_" + time + ".zip";
                value[6] = "zip";


                string FileName = MESDataDefine.MESData.StrLocalPath /*myIniFile.IniReadValue("WebMes", "LoPath")*/ + "\\\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\\\" + sn + "_" + time + ".zip";
                string CopyFileName = MESDataDefine.MESData.StrLocalPath /*myIniFile.IniReadValue("WebMes", "LoPath")*/ + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\" + sn + "_" + time + ".zip";
                value[7] = FileName;

                string Mac = MESDataDefine.MESData.Mac_Address;  // myIniFile.IniReadValue("WebMes", "MacName");
                string RePath = MESDataDefine.MESData.StrRemotepath; // myIniFile.IniReadValue("WebMes", "RePath");
                string CopyRePath = MESDataDefine.MESData.StrCopyRePath; // myIniFile.IniReadValue("WebMes", "CopyRePath ");
                string StationName = MESDataDefine.MESData.StrStationName; // myIniFile.IniReadValue("WebMes", "StationName");
                //string RemotePath =  RePath + "\\" + DateTime.Now.ToString("YYYY-MM-DD") + "\\" + DateTime.Now.ToString("HH") + "\\" + LineID + "\\" + StationName + "\\" + MachineID ;
                string RemotePath = RePath + "\\\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\\\" + DateTime.Now.ToString("HH") + "\\\\" + Mac + "\\\\" + StationName + "\\\\" + MachineID;
                string CopyRemotePath = CopyRePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\" + DateTime.Now.ToString("HH") + "\\" + Mac + "\\" + StationName + "\\" + MachineID;

                string zipFile = CopyRemotePath + "\\" + sn + "_" + time + ".zip";

                //if (!File.Exists(zipFile))
                //    CreatRemotePath(CopyRemotePath, CopyFileName);//27软件(DW)太菜，导致立讯换接口，这里只好更改

                value[8] = RemotePath;

                string tagInfo = "{\n\t";

                for (int i = 0; i < key.Length; i++)
                {
                    if (i <= 7)
                        tagInfo = tagInfo + "\"" + key[i] + "\":\"" + value[i] + "\",\n\t" /*+ "\"},"*/;
                    if (i == 8)
                        tagInfo = tagInfo + "\"" + key[i] + "\":\"" + value[i] + "\"\n";
                }
                // tagInfo = tagInfo.Remove(tagInfo.Length - 1, 1);
                tagInfo = tagInfo + "}";
                //tagInfo = tagInfo.Substring(0, tagInfo.Length - 1);
                //tagInfo += "]}";
                return tagInfo;
            }
            catch (Exception e)
            {
                //MDataDefine.SaveMesError("报错行数：" + e.StackTrace + "-----报错原因：" + e.Message);
                return "";
            }
        }
        public string JsonToStr(string a, string SN)
        {
            try
            {
                string result = string.Empty;
                string[] str = a.Split(',');
                foreach (var item in str)
                {
                    if (item.Contains("RESULT"))
                    {
                        if (item.Contains("OK"))
                        {
                            result = "OK";
                            return result;
                        }
                        else
                            result = a;
                    }
                }
                return result;
            }
            catch (Exception)
            {
                return a;
            }
        }


        /// <summary>
        /// 窗体手动上传大量图片
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="FileName"></param>
        /// <param name="data"></param>
        /// <param name="RL"></param>
        /// <returns></returns>
        public string Hand_MesSendPicManual(string sn, string FileName, string data, int RL)
        {
            string tagInfo = Hand_StrToJson(sn, FileName, data);

            data = data.Replace('-', '_');
            SaveDatePic("上传图片发送mes结果:" + tagInfo, data);

            #region 换接口
            string picPath = getFiles_Path(sn, data.Replace('_', '-'));
            FileStream picZipFile_Stream = new FileStream(picPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] picByte = new byte[picZipFile_Stream.Length];
            picZipFile_Stream.Read(picByte, 0, picByte.Length);
            picZipFile_Stream.Dispose();
            picZipFile_Stream.Close();
            string re = sendPicService.SendDataToMES(tagInfo, picByte);
            #endregion

            //string re = web1.SendDataToMES(tagInfo);
            SaveDatePic("上传图片收到mes反馈:" + re, data);

            string success = JsonToStr(re, sn);

            return success;
        }
        /// <summary>
        /// 窗体手动上传大量图片json
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string Hand_StrToJson(string sn, string fileName, string data)
        {
            try
            {
                string[] key = new string[9];
                key[0] = "COMMAND";
                key[1] = "SERIAL_NUMBER";
                key[2] = "LINE_NAME";
                key[3] = "TERMINAL_NAME";
                key[4] = "EQUIPMENTNAME";
                key[5] = "FILENAME";
                key[6] = "FILETYPE";
                key[7] = "LOCALPATH";
                key[8] = "REMOTEPATH";

                string[] value = new string[9];
                value[0] = "SendData";
                value[1] = sn;

                string LineID = MESDataDefine.MESData.StrLine;//myIniFile.IniReadValue("WebMes", "LineID");
                value[2] = LineID;
                string StationID = MESDataDefine.MESData.StrStationID;//myIniFile.IniReadValue("WebMes", "StationID");
                value[3] = StationID;
                string MachineID = MESDataDefine.MESData.StrMachineID; //myIniFile.IniReadValue("Macmini", "MachineID");
                value[4] = MachineID;
                value[5] = fileName + ".zip";
                value[6] = "zip";

                string FileName = MESDataDefine.MESData.StrLocalPath /*myIniFile.IniReadValue("WebMes", "LoPath")*/ + "\\\\" + data + "\\\\" + fileName + ".zip";
                string CopyFileName = MESDataDefine.MESData.StrLocalPath /*myIniFile.IniReadValue("WebMes", "LoPath")*/ + "\\" + data + "\\" + fileName + ".zip";
                value[7] = FileName;

                string Mac = MESDataDefine.MESData.Mac_Address;  // myIniFile.IniReadValue("WebMes", "MacName");
                string RePath = MESDataDefine.MESData.StrRemotepath; // myIniFile.IniReadValue("WebMes", "RePath");
                string CopyRePath = MESDataDefine.MESData.StrCopyRePath; // myIniFile.IniReadValue("WebMes", "CopyRePath ");
                string StationName = MESDataDefine.MESData.StrStationName; // myIniFile.IniReadValue("WebMes", "StationName");
                //string RemotePath =  RePath + "\\" + DateTime.Now.ToString("YYYY-MM-DD") + "\\" + DateTime.Now.ToString("HH") + "\\" + LineID + "\\" + StationName + "\\" + StationID ;
                string RemotePath = RePath + "\\\\" + data + "\\\\" + DateTime.Now.ToString("HH") + "\\\\" + Mac + "\\\\" + StationName + "\\\\" + MachineID;
                string CopyRemotePath = CopyRePath + "\\" + data + "\\" + DateTime.Now.ToString("HH") + "\\" + Mac + "\\" + StationName + "\\" + MachineID;

                //CreatRemotePath(CopyRemotePath, CopyFileName);  //27软件(DW)太菜，导致立讯换接口，这里只好更改

                FileInfo file = new FileInfo(FileName);
                value[8] = RemotePath;

                string tagInfo = "{\n\t";

                for (int i = 0; i < key.Length; i++)
                {
                    if (i <= 7)
                        tagInfo = tagInfo + "\"" + key[i] + "\":\"" + value[i] + "\",\n\t" /*+ "\"},"*/;
                    if (i == 8)
                        tagInfo = tagInfo + "\"" + key[i] + "\":\"" + value[i] + "\"\n";
                }
                // tagInfo = tagInfo.Remove(tagInfo.Length - 1, 1);
                tagInfo = tagInfo + "}";
                //tagInfo = tagInfo.Substring(0, tagInfo.Length - 1);
                //tagInfo += "]}";
                return tagInfo;
            }
            catch (Exception e)
            {
                //MDataDefine.SaveMesError("报错行数：" + e.StackTrace + "-----报错原因：" + e.Message);
                return "";
            }
        }

        public bool CreatRemotePath(string RePath, string FileName)
        {
            lock (Copylocker)
            {
                bool re = false;
                try
                {
                    if (!Directory.Exists(RePath))
                        Directory.CreateDirectory(RePath);

                    if (File.Exists(FileName))
                    {
                        FileInfo file = new FileInfo(FileName);
                        File.Copy(FileName, RePath + "\\" + file.Name);
                        re = true;
                    }
                }
                catch (Exception e)
                {
                    re = false;
                }
                return re;
            }
        }
        public bool Copy_CreatRemotePath(string sn, string time)
        {
            lock (Copylocker)
            {
                string CopyFileName = myIniFile.IniReadValue("WebMes", "CopyLoPath ") + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\" + sn + "_" + time + ".zip";
                string CopyRemotePath = myIniFile.IniReadValue("WebMes", "CopyRePath ") + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\" + DateTime.Now.ToString("HH")
                                        + "\\" + myIniFile.IniReadValue("WebMes", "MacName") + "\\" + myIniFile.IniReadValue("WebMes", "StationName") + "\\" + myIniFile.IniReadValue("WebMes", "StationID");

                bool re = false;
                try
                {
                    if (!Directory.Exists(CopyRemotePath))
                        Directory.CreateDirectory(CopyRemotePath);

                    if (File.Exists(CopyFileName))
                    {
                        FileInfo file = new FileInfo(CopyFileName);
                        File.Copy(CopyFileName, CopyRemotePath + "\\" + file.Name);
                        re = true;
                    }
                }
                catch (Exception e)
                {
                    re = false;
                }
                return re;
            }
        }
        public bool PathHavePicFile(string sn, string time)
        {
            lock (Copylocker)
            {
                bool re = false;

                string CopyFileName = myIniFile.IniReadValue("WebMes", "CopyLoPath ") + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\" + sn + "_" + time + ".zip";
                string CopyRemotePath = myIniFile.IniReadValue("WebMes", "CopyRePath ") + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\" + DateTime.Now.ToString("HH")
                                        + "\\" + myIniFile.IniReadValue("WebMes", "MacName") + "\\" + myIniFile.IniReadValue("WebMes", "StationName") + "\\" + myIniFile.IniReadValue("WebMes", "StationID");


                string zipFile = CopyRemotePath + "\\" + sn + "_" + time + ".zip";
                if (File.Exists(zipFile))
                    re = true;
                return re;
            }
        }
        #endregion

        #endregion
    }
}