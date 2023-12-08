using cowain.FlowWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//using ToolTotal;
//using static Cowain_AutoDispenser.Flow.clsGTKMESPushClass;
//using static Cowain_AutoDispenser.Flow.clsGTKPDCAPushClass;
//using static Cowain_AutoDispenser.Flow.MESDataDefine;

namespace cowain.MES
{
    public class MESDataDefine
    {
        public MESDataDefine()
        {
            ReadParams();
        }

        /// <summary>
        /// mes参数 
        /// </summary>
        public static MESBase MESData = new MESBase();
        public enum MesStatus
        {
            UpLoadMesOK,
            UpLoadMesNG,
            CheckSN,
        }

        public static void ReadParams()
        {
            MESData = (MESBase)MESData.ReadParams(MESData.GetType().Name, MESData);
        }

        #region 公共的静态 MES 参数
        /// <summary>
        /// 图片的时间缓存
        /// </summary>
        public static string[] StrImagetimeS = new string[2];
        /// <summary>
        /// 图片的时间缓存
        /// </summary>
        public static string[] StrImagetimeDelayS = new string[2];
        /// <summary>
        /// 开始作料时的时间缓存
        /// </summary>
        public static string[] StrStartTimeDelayS = new string[2];
        /// <summary>
        /// 存储MES上传相关信息（从文件中读出来）
        /// </summary>
        public static Dictionary<string, string>[] StrMyMesMsgDic = new Dictionary<string, string>[2];

        /// <summary>
        /// 从MES获取到的工站名
        /// </summary>
        public static string StrMESStationName = "";

        //--------------------------新增
        public static string StrBulid = "";
        public static string StrBulidEvent = "";
        public static string StrBulidMartixConfig = "";
        public static string StrGetBulidURL = "";

        /// <summary>
        /// 复检NG的错误代码
        /// </summary>
        public static string[] ErrorCode = new string[2] { "NA", "NA" };

        //----------------------------------
        public static string StrLogCT;
        //--------------------------------MES Time Data

        public static string[] StrStartTime = new string[2];
        public static string[] StrStopTime = new string[2];

        /// <summary>
        /// 已经获取到结果的SN，用于显示结果
        /// </summary>
        public static string StrCurrentSN = "";
        /// <summary>
        /// 已经获取到结果的UC，用于显示结果
        /// </summary>
        public static string StrCurrentUC = "";
        /// <summary>
        /// mes上传状态
        /// </summary>
        public static MesStatus[] MesUploadStatusS = new MesStatus[2] { 0, 0 };

        //public static Product[] Products = new Product[2] { new Product(), new Product() };
        //public static List<Product> Products = new List<Product>();
        //public static Dictionary<string, Product> ProductsDic = new Dictionary<string, Product>();
        public static ConcurrentQueue<Product> MESQueue = new ConcurrentQueue<Product>();
        #endregion

    }

    public enum enMES
    {
        GetSN,
        CheckUOP,
        UploadSN,
        UploadPressure,
        UploadPictures,
    }

    public class Product
    {
        public string SN = string.Empty;
        public string UC = string.Empty;
        public string EnterTime = string.Empty;
        public string SendCCDTime = string.Empty;
        public string ExitTime = string.Empty;

        public float PressOne = 0;
        public float PressTwo = 0;
        public float PressThree = 0;
        public float PressFour = 0;
        public float PressFive = 0;

        public bool VisionResultOne = false;
        public bool VisionResultTwo = false;
        public bool VisionResultThree = false;

        public int CurrentStation = 0;
        //public enMES MESStep = enMES.UploadSN;

        public Product DeepCopy(Product product)
        {
            Type type = product.GetType();
            object obj = Activator.CreateInstance(type);
            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo field in fields)
            {
                field.SetValue(obj, field.GetValue(product));
            }
            return (Product)obj;
        }
    }

    /// <summary>
    /// 基类  歌尔和立讯公共参数
    /// </summary>
    public class MESBase : JsonHelper
    {
        /// <summary>
        /// 软件的版本号，会上传
        /// </summary>
        public string SW_Version = "RollbarFoam V2.6";

        /// <summary>
        /// Mini  IP
        /// </summary>
        public string StrMiniIP = "127.0.0.10";//"169.254.1.10";

        /// <summary>
        /// Mini 端口号
        /// </summary>
        public string StrMiniPort = "8080";//"1111"

        /// <summary>
        /// 电脑的账号
        /// </summary>
        public string StrUser = "user";
        /// <summary>
        /// 电脑的密码
        /// </summary>
        public string StrPassWord = "123";

        /// <summary>
        /// 存储发给PDCA的图片的路径
        /// </summary>
        public string StrPDCAImagePath = @"smb://169.254.1.12/IMG/";
        /// <summary>
        /// OP用户
        /// </summary>
        public string StrOp = "0123456";

        /// <summary>
        /// 禁用PDCA
        /// </summary>
        public bool IsDisablePDCA = true;
        /// <summary>
        /// 禁用MES
        /// </summary>
        public bool IsDisableMES = false;
        /// <summary>
        /// 禁用HIVE
        /// </summary>
        public bool IsDisableHIVE = true;
        /// <summary>
        /// 禁用单个工位上传
        /// </summary>
        public bool IsDisableSingle = true;

        /// <summary>
        /// 上站设备ID
        /// </summary>
        public string Station_Code = "RBF01";
        /// <summary>
        /// 本站设备ID
        /// </summary>
        public string Station_ID = "RBF01";

        /// <summary>
        /// 设备号（设备自己的名称）
        /// </summary>
        public string StrStationID = "KSH26NMPLH365M0100005";
        /// <summary>
        /// 站号（设备在流水线上的名称）
        /// </summary>
        public string StrMachineID = "ITJX_A07-4FT-01B_1_STATION9";
        /// <summary>
        /// MES的URL
        /// </summary>
        public string StrURL = "http://127.0.0.100:8080/";//"http://172.30.170.190/MABCATA01/Bobcat.aspx";
        /// <summary>
        /// MES的URL
        /// </summary>
        public string StrURL_HIVE = "http://127.0.0.100:8080/";//"http://10.0.0.2:5008/v5/capture/errordata";
        /// <summary>
        /// 获取SN的dat
        /// </summary>
        public string StrGetSNDat = "CMD=ATT&P=RFID_GET_SN";
        /// <summary>
        /// Check SN的dat
        /// </summary>
        public string StrCheckSNDat = "CMD=UOP";
        /// <summary>
        /// 上传SN的dat
        /// </summary>
        public string StrLinkSNDat = "CMD=ADD";

        /// <summary>
        ///Line
        /// </summary>
        public string StrLine = "D1-3F-H26-OFF-L02";

        /// <summary>
        /// 站别
        /// </summary>
        public string Mac_Address = "LXKS_A01-2FA-01_1_RollBarFoam";

        /// <summary>
        ///本地图片存储路径
        /// </summary>
        public string StrLocalPath = @"E:\\IMG";

        #region 2022.2.25 上传图片新增
        public string StrRemotepath = "abg_pic";
        public string StrImgUrl = "http://10.103.6.30/UploadFileData/abgService?wsdl";
        public string StrStationName = "AE_38";
        public string StrCopyLoPath = @"E:\IMG";
        public string StrCopyRePath = "Y:";
        #endregion
    }

}
