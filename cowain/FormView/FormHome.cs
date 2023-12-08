using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using cowain.Comm;
using cowain.FlowWork;
using cowain.MES;
using System.Diagnostics;

namespace cowain.FormView
{
    public partial class FormHome : Form
    {
        public static bool b_PDCAOnline = false;
        public static bool b_MESOnline = false;
        Ping ping = new Ping();
        PingReply reply = null;
        Uri uri = null;
        //public static bool ProgramStart = false;
        Func<string, DialogResult> FuncPopup = null;
        private object locker = new object();

        public FormHome(Func<string, DialogResult> func)
        {
            InitializeComponent();
            SHIJEReader.flashUCcode += SHIJEReader_flashUCcode;
            MESProcess.flashUCSN += MESProcess_flashSN;
            OmronPLC.flashRunner = OmronPLC_flashRunner;
            HIVE.flashHIVE = HIVE_flashHIVE;
            FuncPopup = func;
        }

        private void FormHome_Load(object sender, EventArgs e)
        {
            bt_MES.Enabled = false;
            //bt_PDCA.Enabled = false;
            timer_FlashStatus.Enabled = true;
            lb_HIVEMSG.Text = string.Empty;
        }
        
        private void HIVE_flashHIVE(string message)
        {
            Invoke((EventHandler)delegate
            {
                lb_HIVEMSG.Text = message;
            });
        }

        private void MESProcess_flashSN(string UC, string SN)
        {
            Invoke((EventHandler)delegate
            {
                tB_UC.Text = UC;
                tB_SN.Text = SN;
            });
            //if(tB_SN.InvokeRequired)
            //{
            //    tB_SN.Invoke(Action<string> )
            //}
        }

        private void SHIJEReader_flashUCcode(string strCode)
        {
            tB_UC.Text = strCode;
        }

        private void timer_FlashStatus_Tick(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                Invoke((EventHandler)delegate
                {
                    //Stopwatch st = new Stopwatch();
                    //st.Start();

                    #region UC,SN
                    //tB_UC.Text = MESDataDefine.StrCurrentUC;
                    //tB_SN.Text = MESDataDefine.StrCurrentSN;
                    #endregion

                    #region 用户
                    if (FormLogin.bLoginSuccess)
                        bt_Login.BackColor = Color.Lime;
                    else
                        bt_Login.BackColor = Color.Red;
                    #endregion

                    #region PDCA网络
                    //if (!MESDataDefine.MESData.IsDisablePDCA)
                    //{
                    //    ToolTotal.AOIMethod.checkmes(MESDataDefine.MESData.StrMiniIP, ref b_PDCAOnline);
                    //    if (!b_PDCAOnline)
                    //    {
                    //        bt_PDCA.BackColor = Color.Red;
                    //        bt_PDCA.Text = "离线";
                    //        int timesOfPDCA = 0;
                    //        while (timesOfPDCA < 3 && !LXPOSTClass.clientOfPDCA.connectOk)
                    //        {
                    //            LXPOSTClass.clientOfPDCA.Open(MESDataDefine.MESData.StrMiniIP, Convert.ToInt32(MESDataDefine.MESData.StrMiniPort));
                    //            timesOfPDCA++;
                    //        }
                    //        if (!LXPOSTClass.clientOfPDCA.connectOk)
                    //        {
                    //            MessageBox.Show("PDCA重连3次失败！", "Warnning");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        bt_PDCA.BackColor = Color.Lime;
                    //        bt_PDCA.Text = "在线";
                    //    }
                    //}
                    //else
                    //{
                    //    bt_PDCA.BackColor = Color.Gray;
                    //    bt_PDCA.Text = "";
                    //}
                    #endregion

                    #region MES网络
                    if (!MESDataDefine.MESData.IsDisableMES)
                    {
                        uri = new Uri(MESDataDefine.MESData.StrURL);
                        try
                        {
                            int timesOfMES = 0;
                            while (timesOfMES < 3)
                            {
                                reply = ping.Send(uri.Host, timeout: 3000);
                                if (reply.Status.ToString() == "Success")
                                {
                                    b_MESOnline = true;
                                    break;
                                }
                                else
                                {
                                    b_MESOnline = false;
                                    timesOfMES++;
                                }
                            }
                        }
                        catch { b_MESOnline = false; }
                        if (b_MESOnline)
                        {
                            bt_MES.BackColor = Color.Lime;
                            bt_MES.Text = "在线";
                        }
                        else
                        {
                            bt_MES.BackColor = Color.Red;
                            bt_MES.Text = "离线";
                        }
                    }
                    else
                    {
                        bt_MES.BackColor = Color.Gray;
                        bt_MES.Text = "";
                    }
                    #endregion

                    #region 读码器
                    if (!SHIJEReader.reader.enable)
                    {
                        bt_Reader.BackColor = Color.Gray;
                    }
                    else if (SHIJEReader.hasOpened || DoWork.clientReader.connectOk)
                    {
                        bt_Reader.BackColor = Color.Lime;
                    }
                    else
                    {
                        bt_Reader.BackColor = Color.Red;
                    }
                    #endregion

                    #region 相机
                    if (!Camera.ccd.enable)
                    {
                        bt_CameraOne.BackColor = Color.Gray;
                    }
                    else if (DoWork.clientCCDOne.connectOk)
                    {
                        bt_CameraOne.BackColor = Color.Lime;
                    }
                    else
                    {
                        bt_CameraOne.BackColor = Color.Red;
                    }

                    if (!Camera.ccd.enable)
                    {
                        bt_CameraTwo.BackColor = Color.Gray;
                    }
                    else if (CacheProcess.clientCCDTwo.connectOk)
                    {
                        bt_CameraTwo.BackColor = Color.Lime;
                    }
                    else
                    {
                        bt_CameraTwo.BackColor = Color.Red;
                    }

                    if (!Camera.ccd.enableOther)
                    {
                        bt_CameraOther.BackColor = Color.Gray;
                    }
                    else if (CacheProcessOther.clientCCDOther.connectOk)
                    {
                        bt_CameraOther.BackColor = Color.Lime;
                    }
                    else
                    {
                        bt_CameraOther.BackColor = Color.Red;
                    }
                    #endregion

                    #region PLC
                    if (OmronPLC.connected)
                    {
                        bt_PLC.BackColor = Color.Lime;
                    }
                    else
                    {
                        bt_PLC.BackColor = Color.Red;
                    }
                    if (OmronPLC.connectedOther)
                    {
                        bt_PLCOther.BackColor = Color.Lime;
                    }
                    else
                    {
                        bt_PLCOther.BackColor = Color.Red;
                    }
                    #endregion

                    #region PLC联动
                    if (DoWork.bPLCTogether)
                    {
                        bt_PLCTogether.BackColor = Color.Lime;
                    }
                    else
                    {
                        bt_PLCTogether.BackColor = Color.Red;
                    }
                    if (CacheProcessOther.bPLCTogetherOther)
                    {
                        bt_PLCTogetherOther.BackColor = Color.Lime;
                    }
                    else
                    {
                        bt_PLCTogetherOther.BackColor = Color.Red;
                    }
                    #endregion

                    //st.Stop();
                    //TimeSpan ts = st.Elapsed;
                });
            });
        }

        private void bt_CameraOther_Click(object sender, EventArgs e)
        {
            if (Camera.ccd.enableOther && !CacheProcessOther.clientCCDOther.connectOk)
                if (FuncPopup("是否重连047相机？") == DialogResult.OK)
                {
                    this.Activate();
                    CacheProcessOther.clientCCDOther.Open(Camera.ccd.IPOther, Camera.ccd.PortOther);
                }
                else
                {
                    this.Activate();
                }
        }

        private void bt_CameraOne_Click(object sender, EventArgs e)
        {
            if (Camera.ccd.enable && !DoWork.clientCCDOne.connectOk)
                if (FuncPopup("是否重连046相机1？") == DialogResult.OK)
                {
                    this.Activate();
                    DoWork.clientCCDOne.Open(Camera.ccd.IP, Camera.ccd.PortOne);
                }
                else
                {
                    this.Activate();
                }
        }

        private void bt_CameraTwo_Click(object sender, EventArgs e)
        {
            if (Camera.ccd.enable && !CacheProcess.clientCCDTwo.connectOk)
                if (FuncPopup("是否重连046相机2？") == DialogResult.OK)
                {
                    this.Activate();
                    CacheProcess.clientCCDTwo.Open(Camera.ccd.IP, Camera.ccd.PortTwo);
                }
                else
                {
                    this.Activate();
                }
        }

        private void bt_Reader_Click(object sender, EventArgs e)
        {
            if (SHIJEReader.reader.enable && !DoWork.clientReader.connectOk)
                if (FuncPopup("是否重连读码器？") == DialogResult.OK)
                {
                    this.Activate();
                    DoWork.clientReader.Open(SHIJEReader.reader.IP, SHIJEReader.reader.Port);
                }
                else
                {
                    this.Activate();
                }
        }

        private void timer_FlashProducts_Tick(object sender, EventArgs e)
        {
            if (DoWork.SOne_Product == null)
                lb_SOne_Product.Text = "工位1：0";
            else
                lb_SOne_Product.Text = "工位1：1";
            lb_CacheQueue.Text = "046缓存：" + CacheProcess.CacheQueue.Count();
            if (CacheProcess.STwo_Product== null)
                lb_STwo_Product.Text = "工位2：0";
            else
                lb_STwo_Product.Text = "工位2：1";
            if (HoldPress.Temp_Product[0] == null)// && HoldPress.Temp_Product[1] == null)
                lb_Temp_Product.Text = "保压1缓存：0";
            else if (HoldPress.Temp_Product[0] != null && HoldPress.Temp_Product[1] != null)
                lb_Temp_Product.Text = "保压1缓存：2";
            else
                lb_Temp_Product.Text = "保压1缓存：1";
            if (HoldPress.HPOne_Product == null)
                lb_HPOne_Product.Text = "保压位1：0";
            else
                lb_HPOne_Product.Text = "保压位1：1";


            lb_CacheQueueOther.Text = "047缓存：" + CacheProcessOther.CacheQueueOther.Count();
            if (CacheProcessOther.SThree_Product == null)
                lb_SThree_Product.Text = "工位3：0";
            else
                lb_SThree_Product.Text = "工位3：1";
            if (HoldPressOther.Temp_ProductOther[0] == null)// && HoldPress.Temp_Product[1] == null)
                lb_Temp_ProductOther.Text = "保压2缓存：0";
            else if (HoldPressOther.Temp_ProductOther[0] != null && HoldPressOther.Temp_ProductOther[1] != null)
                lb_Temp_ProductOther.Text = "保压2缓存：2";
            else
                lb_Temp_ProductOther.Text = "保压2缓存：1";
            if (HoldPressOther.HPTwo_Product == null)
                lb_HPTwo_Product.Text = "保压位2：0";
            else
                lb_HPTwo_Product.Text = "保压位2：1";
            lb_MESQueue.Text = "MES缓存：" + MESProcess.MESQueue.Count();

            //string[] things046 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[35].address).Split(',');
            //for (int i = 0; i < groupBox1.Controls.Count; i++)
            //{
            //    groupBox1.Controls[i].BackColor = things046[i] == "True" ? Color.Crimson : Color.Gray;
            //}
            //string[] things047 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[36].address).Split(',');
            //for (int i = 0; i < groupBox2.Controls.Count; i++)
            //{
            //    groupBox2.Controls[i].BackColor = things047[i] == "True" ? Color.Crimson : Color.Gray;
            //}
        }

        private void OmronPLC_flashRunner(enMachine machine, string result)
        {
            lock(locker)
            {
                try
                {
                    string[] strings = result.Split(',');
                    Invoke((EventHandler)delegate
                    {
                        if (machine == enMachine.M_046)
                        {
                            button1.BackColor = strings[0] == "True" ? Color.LimeGreen : Color.Gold;
                            button2.BackColor = strings[1] == "True" ? Color.LimeGreen : Color.Orange;
                            button3.BackColor = strings[2] == "True" ? Color.LimeGreen : Color.Gray;
                            button4.BackColor = strings[3] == "True" ? Color.LimeGreen : Color.Gray;
                            button5.BackColor = strings[4] == "True" ? Color.LimeGreen : Color.Orange;
                            button6.BackColor = strings[5] == "True" ? Color.LimeGreen : Color.Yellow;
                        }
                        if (machine == enMachine.M_047)
                        {
                            button7.BackColor = strings[0] == "True" ? Color.LimeGreen : Color.Gray;
                            button8.BackColor = strings[1] == "True" ? Color.LimeGreen : Color.SlateGray;
                            button9.BackColor = strings[2] == "True" ? Color.LimeGreen : Color.Gray;
                            button10.BackColor = strings[3] == "True" ? Color.LimeGreen : Color.Gray;
                            button11.BackColor = strings[4] == "True" ? Color.LimeGreen : Color.Orange;
                            button12.BackColor = strings[5] == "True" ? Color.LimeGreen : Color.Yellow;
                        }
                    });
                }
                catch{ }
            }
        }
    }
}
