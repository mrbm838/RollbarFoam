using Cowain_Form.FormView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cowain_AutoDispenser.Flow
{
    /// <summary>
    /// 刷新胶阀的数据
    /// </summary>
    public class ReflashDispenserDataClass
    {
        /// <summary>
        /// 诺信热胶前后龙门喷嘴温度_动态数据
        /// </summary>
        public string[] NordsonTemps = new string[2] { "", "" };
        /// <summary>
        /// 诺信热胶前后龙门胶管温度_动态数据
        /// </summary>
        public string[] Tube_NordsonTemps = new string[2] { "", "" };
        /// <summary>
        /// 诺信压力前后龙门_动态数据
        /// </summary>
        public string[] NordPressures = new string[2] { "", "" };
        /// <summary>
        /// AB胶A压力前后龙门_动态数据
        /// </summary>
        public string[] APressures = new string[2] { "", "" };
        /// <summary>
        /// AB胶B压力前后龙门_动态数据
        /// </summary>
        public string[] BPressures = new string[2] { "", "" };
        /// <summary>
        ///  AB混合比前后龙门_动态数据
        /// </summary>
        public string[] ABRates = new string[2] { "", "" };
        public void reflashData()
        {
            if (frm_Main.myDispenserController[0] != null)
            {
                NordsonTemps[0] = frm_Main.myDispenserController[0].controller_T1.ToString();
                Tube_NordsonTemps[0] = frm_Main.myDispenserController[0].controller_T2.ToString();
                NordPressures[0] = frm_Main.myDispenserController[0].controller_Presure.ToString();
                APressures[0] = frm_Main.myDispenserController[0].controller_APress.ToString("f3");
                BPressures[0] = frm_Main.myDispenserController[0].controller_BPress.ToString("f3");
                ABRates[0] = frm_Main.myDispenserController[0].controller_ABrate.ToString();
            }
            if (frm_Main.myDispenserController[1] != null)
            {
                NordsonTemps[1] = frm_Main.myDispenserController[1].controller_T1.ToString();
                Tube_NordsonTemps[1] = frm_Main.myDispenserController[1].controller_T2.ToString();
                NordPressures[1] = frm_Main.myDispenserController[1].controller_Presure.ToString();
                APressures[1] = frm_Main.myDispenserController[1].controller_APress.ToString("f3");
                BPressures[1] = frm_Main.myDispenserController[1].controller_BPress.ToString("f3");
                ABRates[1] = frm_Main.myDispenserController[1].controller_ABrate.ToString();
            }
        }
    }
}
