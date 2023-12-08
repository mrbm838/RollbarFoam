using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ToolTotal;
using System.Threading;
using cowain.Comm;

namespace cowain.MES
{
    public class HIVE
    {

        //public static HIVE HIVEInstance = new HIVE();
        /// <summary>
        /// 记录进料时间  用于HIVE
        /// </summary>
        public string[] HIVEStartTime = new string[2];//记录进料时间  用于HIVE
        /// <summary>
        /// 记录出料料时间  用于HIVE
        /// </summary>
        public string[] HIVEStopTime = new string[2];//记录出料料时间  用于HIVE
        /// <summary>
        /// 用于hive---条码枪扫的SN或者是MES获取到的SN，在下次流道进料后会被覆盖
        /// </summary>
        public string[] StrSNS = new string[2] { "", "" };
        /// <summary>
        /// 用于hive---条码枪扫的UC，在下次流道进料后会被覆盖
        /// </summary>
        public string[] StrUCS = new string[2] { "", "" };

        HttpWebRequest Web = null;
        HttpWebResponse Res = null;
        public string SendStr_mes = "";
        private object locker = new object();
        private object locker1 = new object();

        private bool bErrorExist046 = false;
        private int index046 = -1;
        private bool bErrorExist047 = false;
        private int index047 = -1;

        public delegate void DelHIVEMsg(string message);
        public static DelHIVEMsg flashHIVE;

        private enum EnumAlarm_046
        {
            马达修正报警,
            马达刹车状态,
            马达增量报警,
            马达手动报警,
            马达找原OK,
            马达找原报警,
            马达报警,
            轴掉线报警,
            马达使能,
            压力1报警,
            压力2报警,
            压力3报警,
            机械手报警,
            飞达模组未到位报警,
            飞达送料报警,
            阻挡1气缸物料感应报警,
            贴膜飞达动态运行中,
            动静态测试时机械手未屏蔽,
            动静态测试时机械手未给出Ready信号,
            动静态测试时未在手动模式,
            动静态测试时运转准备未OK,
            贴膜飞达静态运行中,
            保压动态测试运行中,
            机械手真空吸报警,
            与OPT视觉通讯报警,
            与上位机通讯报警,
            上位机屏蔽,
            上位机给视觉SN码完成未收到,
            上位机压力2完成信号未收到,
            上位机压力3完成信号未收到,
            机械手1贴模完成未收到,
            机械手1在贴膜位信号未收到,
            真空_报警保持1,
            真空_报警保持2,
            真空_报警保持3,
            机器手1连续取料NG报警,
            机器手取膜NG报警,
        }
        private enum EnumAlarm_047
        {
            马达修正报警,
            马达刹车状态,
            马达增量报警,
            马达手动报警,
            马达找原OK,
            马达找原报警,
            马达报警,
            轴掉线报警,
            马达使能,
            压力1报警,
            压力2报警,
            压力3报警,
            机械手报警,
            飞达模组未到位报警,
            飞达送料报警,
            阻挡1气缸物料感应报警,
            贴膜飞达动态运行中,
            动静态测试时机械手未屏蔽,
            动静态测试时机械手未给出Ready信号,
            动静态测试时未在手动模式,
            动静态测试时运转准备未OK,
            贴膜飞达静态运行中,
            保压动态测试运行中,
            机械手真空吸报警,
            与OPT视觉通讯报警,
            与上位机通讯报警,
            上位机屏蔽,
            上位机给视觉SN码完成未收到,
            上位机压力2完成信号未收到,
            上位机压力3完成信号未收到,
            机械手1贴模完成未收到,
            机械手1在贴膜位信号未收到,
            真空_报警保持1,
            真空_报警保持2,
            真空_报警保持3,
            机器手1连续取料NG报警,
            机器手取膜NG报警,
        }

        public HIVE()
        {//Enum.GetNames(typeof(EnumAlarm_046));//Enum.GetNames(typeof(EnumAlarm_047));
            Thread thFindError = new Thread(() =>
            {
                Thread.Sleep(1000);
                while (true)
                {
                    Thread.Sleep(1);
                    if (MESDataDefine.MESData.IsDisableHIVE) continue;
                    //OmronPLC.plc.plcAddresses[25].lengthOrData = enAlarm046.Length.ToString();
                    string result25 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[25].address);
                    //OmronPLC.plc.plcAddresses[26].lengthOrData = enAlarm047.Length.ToString();
                    string result26 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[26].address);
                    string[] result046 = result25.Split(',');
                    string[] result047 = result26.Split(',');
                    if (bErrorExist046)
                    {
                        if (result046[index046] == "True")
                            continue;
                        else
                        {
                            bErrorExist046 = false;
                            flashHIVE(string.Empty);
                        }
                    }
                    if (bErrorExist047)
                    {
                        if (result047[index047] == "True")
                            continue;
                        else
                        {
                            bErrorExist047 = false;
                            flashHIVE(string.Empty);
                        }
                    }
                    //List<int> listIndex046 = new List<int>();
                    //List<int> listIndex047 = new List<int>();
                    for (int i = 0; i < result046.Length; i++)
                    {
                        if (result046[i] == "True")
                        {
                            //listIndex046.Add(i);
                            bErrorExist046 = true;
                            index046 = i;
                            string[] strings = OmronPLC.enAlarm046[i].Split('_');
                            HiveSendERRORDATA(enMachine.M_046.ToString(), Enumerable.First(strings), DateTime.Now.ToString(), DateTime.Now.ToString(),
                                                 DateTime.Now.Ticks, DateTime.Now.Ticks, true);
                            flashHIVE("上传HIVE：" + enMachine.M_046.ToString() + Enumerable.First(strings));
                            break;
                        }
                    }
                    //if (bErrorExist046) continue;
                    for (int i = 0; i < result047.Length; i++)
                    {
                        if (result047[i] == "True")
                        {
                            //listIndex047.Add(i);
                            bErrorExist047 = true;
                            index047 = i;
                            string[] strings = OmronPLC.enAlarm047[i].Split('_');
                            HiveSendERRORDATA(enMachine.M_047.ToString(), Enumerable.First(strings), DateTime.Now.ToString(), DateTime.Now.ToString(),
                                                 DateTime.Now.Ticks, DateTime.Now.Ticks, true);
                            flashHIVE("上传HIVE：" + enMachine.M_047.ToString() + Enumerable.First(strings));
                            break;
                        }
                    }
                    //for (int i = 0; i < listIndex046.Count(); i++)
                    //{
                    //    string[] strings = enAlarm046[listIndex046[i]].Split('_');
                    //    HiveSendERRORDATA(Enumerable.Last(strings), Enumerable.First(strings), DateTime.Now.ToString(), DateTime.Now.ToString(),
                    //                         DateTime.Now.Ticks, DateTime.Now.Ticks, true);
                    //}
                    //listIndex046.Clear();

                    //if (listIndex.Count() == 0)//count > 0
                    //{
                    //    //if (!File.Exists(file)) File.Create(file);
                    //    string[] content =  new string[8];//= File.ReadAllLines(file);
                    //    string ss = "";
                    //    byte[] bytes = new byte[1024];
                    //    using (FileStream sr = new FileStream(file, FileMode.OpenOrCreate))
                    //    {
                    //        //if (!sr.EndOfStream)
                    //        //    ss += sr.ReadLine();
                    //        if (sr.Read(bytes, 0, bytes.Length) != 0)
                    //        {
                    //            ss += Encoding.ASCII.GetString(bytes);
                    //        }
                    //    }
                    //HiveSendERRORDATA();
                    //}
                }
            });
            thFindError.IsBackground = true;
            thFindError.Start();
        }



        /// <summary>
        /// 发送机台数据
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="top_sn">合成部件的第一部分的SN，不存在的允许放空</param>
        /// <param name="bottom_sn">合成部件的第二部分的SN，不存在的允许放空</param>
        /// <param name="no"></param>
        /// <param name="isauto"></param>
        /// <returns></returns>
        public string HiveSendMACHINEDATA(string sn, string uc, string bottom_sn, int no, bool isauto)
        {
            try
            {
                lock (locker1)
                {
                    SendStr_mes = "";

                    string value = "";
                    if (isauto)
                    {

                        value = "{" +
                                      "\"unit_sn\":" + "\"" + sn + "\"," +
                                      "\"serials\":" + "{ \"top_case\": \"" + uc + "\"," + "\"bottom_case\": \"" + bottom_sn + "\"}," +

                                      "\"pass\":\"true\"," +
                                      //"\"input_time\":" + "\"" + MESDataDefine.Products[no].EnterTime + "\"," + // HIVEStartTime[no]
                                      //"\"output_time\":" + "\"" + MESDataDefine.Products[no].ExitTime + "\"," + // HIVEStopTime[no]
                                      "\"data\":" + "{" + "}" +
                                "}";
                    }
                    else
                    {

                        value = "{" +
                                      "\"unit_sn\":" + "\"" + sn + "\"," +
                                      "\"serials\":" + "{ \"top_case\": \"" + uc + "\"," + "\"bottom_case\": \"" + bottom_sn + "\"}," +

                                      "\"pass\":\"true\"," +
                                      "\"input_time\":" + "\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ff+0800") + "\"," +
                                      "\"output_time\":" + "\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ff+0800") + "\"," +
                                      //结果
                                      "\"pass\":\"true\"," +
                                "}";
                    }

                    string url = "http://127.0.0.10:8080";//"http://10.0.0.2:5008/v5/capture/machinedata";
                    Web = (HttpWebRequest)WebRequest.Create(url);
                    Web.Method = "POST";
                    Web.ContentType = "multipart/form-data";
                    Web.Timeout = 200;
                    string dat = value/* + result + result1*/;
                    SaveDateHIVE("发送数据MACHINEDATA:\r\n" + dat);

                    SendStr_mes = dat;
                    byte[] data = Encoding.UTF8.GetBytes(dat);
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
                    SaveDateHIVE("接收数据MACHINEDATA:\r\n" + str);
                    return str;
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///  HIVE系统发送errorcode
        /// </summary>
        /// <param name="errorcode">报警码</param>
        /// <param name="messgae">报警信息</param>
        /// <param name="occurrence_time">报警开始时间</param>
        /// <param name="resolved_time">报警结束时间</param>
        /// <param name="isauto">true表示自动运行时调用此函数</param>
        /// <returns></returns>
        public string HiveSendERRORDATA(string errorcode, string messgae, string occurrence_time, string resolved_time, long Start_time, long Stop_time, bool isauto)
        {
            try
            {
                lock (locker1)
                {
                    string severity;
                    SendStr_mes = "";
                    string value = "";
                    severity = (Stop_time - Start_time) / 10000.0 / 1000 > 30 ? "warning" : "error";

                    if (isauto)
                    {
                        value = "{" +
                                      "\"message\":" + "\"" + messgae + "\"," +
                                      "\"code\":" + "\"" + errorcode + "\"," +
                                      "\"severity\":" + "\"" + severity + "\"," +
                                      "\"occurrence_time\":" + "\"" + occurrence_time + "\"," +
                                      "\"resolved_time\":" + "\"" + resolved_time + "\"," +
                                      "\"data\":" + "{" + "}" +
                                "}";
                    }
                    else
                    {
                        value = "{" +
                                      "\"message\":" + "\"" + messgae + "\"," +
                                      "\"code\":" + "\"" + errorcode + "\"," +
                                      "\"occurrence_time\":" + "\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff+0800") + "\"," +
                                      "\"resolved_time\":" + "\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff+0800") + "\"," +
                                      "\"data\":" + "{" + "}" +
                                "}";
                    }

                    string url = MESDataDefine.MESData.StrURL_HIVE;
                    Web = (HttpWebRequest)WebRequest.Create(url);
                    Web.Method = "POST";
                    Web.ContentType = "multipart/form-data";
                    Web.Timeout = 200;
                    string dat = value;
                    SaveDateHIVE("发送数据ERRORDATA:\r\n" + dat);

                    SendStr_mes = dat;
                    byte[] data = Encoding.UTF8.GetBytes(dat);
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
                    SaveDateHIVE("接收数据ERRORDATA:\r\n" + str);
                    return str;
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// HIVE系统发送设备状态
        /// </summary>
        /// <param name="sataus">1:running 2：idle 3：engineering 4：planned downtime 5：error_down</param>
        /// <param name="errorcode">sataus为5时传值</param>
        /// <param name="messgae">sataus为5时传值</param>
        /// <returns></returns>
        public string HiveSendMACHINESTATE(int status, string errorcode, string messgae)
        {
            try
            {
                lock (locker1)
                {

                    SendStr_mes = "";
                    string value = "";
                    if (status != 5)
                    {
                        value = "{" +
                                     "\"machine_state\":" + status + "," +
                                     "\"state_change_time\":" + "\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ff+0800") + "\"," +
                                     "\"data\":" + "{" + "}" +
                                "}";
                    }
                    else
                    {
                        value = "{" +
                                      "\"machine_state\":" + status + "," +
                                      "\"state_change_time\":" + "\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ff+0800") + "\"," +
                                      "\"data\":" + "{" +
                                                          "\"code\":" + "\"" + errorcode + "\"," +
                                                          "\"error_message\":" + "\"" + messgae + "\"" +
                                                    "}" +
                                 "}";
                    }

                    string url = "http://127.0.0.10:8080";//"http://10.0.0.2:5008/v5/capture/machinestate";
                    Web = (HttpWebRequest)WebRequest.Create(url);
                    Web.Method = "POST";
                    Web.ContentType = "multipart/form-data";
                    Web.Timeout = 200;
                    string dat = value;
                    SaveDateHIVE("发送数据MACHINESTATE:\r\n" + dat);

                    SendStr_mes = dat;
                    byte[] data = Encoding.UTF8.GetBytes(dat);
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
                    SaveDateHIVE("接收数据MACHINESTATE:\r\n" + str);
                    return str;
                }
            }
            catch
            {
                return "";
            }
        }

        public void SaveDateHIVE(string result)
        {
            try
            {
                lock (locker)
                {
                    string fileName;
                    fileName = string.Format("{0}.txt", DateTime.Now.ToString("yyyy_MM_dd"));
                    string outputPath = @"D:\DATA\HIVE记录";
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    string fullFileName = Path.Combine(outputPath, fileName);
                    System.IO.FileStream fs;
                    StreamWriter sw;
                    if (!File.Exists(fullFileName))
                    {
                        fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "————" + result + "\r\n");
                        sw.Close();
                        fs.Close();
                    }
                    else
                    {
                        fs = new System.IO.FileStream(fullFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, FileShare.Read);
                        sw = new StreamWriter(fs, System.Text.Encoding.Default);
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "————" + result + "\r\n");
                        sw.Close();
                        fs.Close();
                    }

                }
            }
            catch
            {

            }
        }

    }
}
