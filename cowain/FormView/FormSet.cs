using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using cowain.Comm;
using System.IO.Ports;
using HslCommunication.Core;
using cowain.MES;

namespace cowain.FormView
{
    public partial class FormSet : Form
    {
        public FormSet()
        {
            InitializeComponent();
        }

        private void FormSet_Load(object sender, EventArgs e)
        {
            cbB_Type.Items.AddRange(new string[] { "视界", "新大陆" });
            cbB_Comm.Items.AddRange(new string[] { "串口", "网口" });
            cbB_QRCode.Items.AddRange(new string[] { "工装码", "产品码" });
            cbB_StopBits.DataSource = Enum.GetNames(typeof(StopBits));
            cbB_Parity.DataSource = Enum.GetNames(typeof(Parity));
            //cbB_Type.SelectedIndex = 0;
            cbB_Procotols.Items.AddRange(new string[] { "OmronFinsTCP", "OmronFinsUDP" });
            //cbB_Procotols.SelectedIndex = 1;
        }

        private void FormSet_Paint(object sender, PaintEventArgs e)
        {
            if (FormLogin.bLoginSuccess && !Designer.lockClicked)
                this.Enabled = true;
            else
                this.Enabled = false;
        }

        private void cbB_Comm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbB_Comm.Text == "串口")
                panel_ReaderNet.Visible = false;
            else if (cbB_Comm.Text == "网口")
                panel_ReaderNet.Visible = true;
        }

        public void ShowContent()
        {
            cbB_Type.Text = SHIJEReader.reader.type;
            cbB_Comm.Text = SHIJEReader.reader.communication;
            cbB_QRCode.Text = SHIJEReader.reader.QRCode;
            tB_PortName.Text = SHIJEReader.reader.portName;
            tB_BaudRate.Text = SHIJEReader.reader.baudRate.ToString();
            tB_DataBits.Text = SHIJEReader.reader.dataBits.ToString();
            cbB_StopBits.Text = SHIJEReader.reader.stopBits.ToString();
            cbB_Parity.Text = SHIJEReader.reader.parity.ToString();
            tB_Reader_IP.Text = SHIJEReader.reader.IP;
            tB_Reader_Port.Text = SHIJEReader.reader.Port.ToString();
            cB_EnableReader.Checked = SHIJEReader.reader.enable;

            cbB_Procotols.Text = OmronPLC.plc.procotol;
            tB_PLC_IP.Text = OmronPLC.plc.IP;
            tB_PLC_Port.Text = OmronPLC.plc.Port.ToString();
            tB_PLC_IPOther.Text = OmronPLC.plc.IPOther;
            tB_PLC_PortOther.Text = OmronPLC.plc.PortOther.ToString();

            tB_CCD_IP.Text = Camera.ccd.IP;
            tB_CCD_PortOne.Text = Camera.ccd.PortOne.ToString();
            tB_CCD_PortTwo.Text = Camera.ccd.PortTwo.ToString();
            cB_EnableCCD.Checked = Camera.ccd.enable;
            tB_CCD_IPOther.Text = Camera.ccd.IPOther;
            tB_CCD_PortOther.Text = Camera.ccd.PortOther.ToString();
            cB_EnableCCDOther.Checked = Camera.ccd.enableOther;

            tB_MES_SWVersion.Text = MESDataDefine.MESData.SW_Version;
            tB_MES_StationCode.Text = MESDataDefine.MESData.Station_Code;
            tB_MES_StationID.Text = MESDataDefine.MESData.Station_ID;
            tB_MES_MacAddress.Text = MESDataDefine.MESData.Mac_Address;
            tB_MES_URL.Text = MESDataDefine.MESData.StrURL;
            cB_EnableMES.Checked = MESDataDefine.MESData.IsDisableMES;
            tB_HIVE_IP.Text = MESDataDefine.MESData.StrURL_HIVE;
            //tB_PDCA_Port.Text = MESDataDefine.MESData.StrMiniPort;
            cB_EnableHIVE.Checked = MESDataDefine.MESData.IsDisableHIVE;
        }

        private void Popup(string text, string caption, MessageBoxIcon mbIcon)
        {
            MessageBox.Show(text, caption, MessageBoxButtons.OK, mbIcon, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        private void bt_SaveReader_Click(object sender, EventArgs e)
        {
            try
            {
                SHIJEReader.reader.type = cbB_Type.Text;
                SHIJEReader.reader.communication = cbB_Comm.Text;
                SHIJEReader.reader.QRCode = cbB_QRCode.Text;
                SHIJEReader.reader.portName = tB_PortName.Text;
                SHIJEReader.reader.baudRate = Convert.ToInt32(tB_BaudRate.Text);
                SHIJEReader.reader.dataBits = Convert.ToInt32(tB_DataBits.Text);
                SHIJEReader.reader.stopBits = (StopBits)Enum.Parse(typeof(StopBits), cbB_StopBits.Text);
                SHIJEReader.reader.parity = (Parity)Enum.Parse(typeof(Parity), cbB_Parity.Text);
                SHIJEReader.reader.IP = tB_Reader_IP.Text;
                SHIJEReader.reader.Port = Convert.ToInt32(tB_Reader_Port.Text);
                SHIJEReader.reader.enable = cB_EnableReader.Checked;
                Designer.json.WriteParams(SHIJEReader.reader.GetType().Name, SHIJEReader.reader);
                Popup("读码器参数保存成功", "Info", MessageBoxIcon.Information);
            }
            catch (FormatException)
            {
                Popup("输入格式错误", "Error", MessageBoxIcon.Error);
            }
            finally { this.Activate(); }
        }

        private void bt_SavePLC_Click(object sender, EventArgs e)
        {
            try
            {
                OmronPLC.plc.procotol = cbB_Procotols.Text;
                OmronPLC.plc.IP = tB_PLC_IP.Text;
                OmronPLC.plc.Port = Convert.ToInt32(tB_PLC_Port.Text);
                OmronPLC.plc.IPOther = tB_PLC_IPOther.Text;
                OmronPLC.plc.PortOther = Convert.ToInt32(tB_PLC_PortOther.Text);
                Designer.json.WriteParams(OmronPLC.plc.GetType().Name, OmronPLC.plc);
                Popup("PLC参数保存成功", "Info", MessageBoxIcon.Information);
            }
            catch (FormatException)
            {
                Popup("输入格式错误", "Error", MessageBoxIcon.Error);
            }
            finally { this.Activate(); }
        }

        private void bt_SaveCCD_Click(object sender, EventArgs e)
        {
            try
            {
                Camera.ccd.IP = tB_CCD_IP.Text;
                Camera.ccd.PortOne = Convert.ToInt32(tB_CCD_PortOne.Text);
                Camera.ccd.PortTwo = Convert.ToInt32(tB_CCD_PortTwo.Text);
                Camera.ccd.enable = cB_EnableCCD.Checked;
                Camera.ccd.IPOther = tB_CCD_IPOther.Text;
                Camera.ccd.PortOther = Convert.ToInt32(tB_CCD_PortOther.Text);
                Camera.ccd.enableOther = cB_EnableCCDOther.Checked;
                Designer.json.WriteParams(Camera.ccd.GetType().Name, Camera.ccd);
                Popup("相机参数保存成功", "Info", MessageBoxIcon.Information);
            }
            catch (FormatException)
            {
                Popup("输入格式错误", "Error", MessageBoxIcon.Error);
            }
            finally { this.Activate(); }
        }

        private void bt_SaveMES_Click(object sender, EventArgs e)
        {
            try
            {
                MESDataDefine.MESData.SW_Version = tB_MES_SWVersion.Text;
                MESDataDefine.MESData.Station_Code = tB_MES_StationCode.Text;
                MESDataDefine.MESData.Station_ID = tB_MES_StationID.Text;
                MESDataDefine.MESData.Mac_Address = tB_MES_MacAddress.Text;
                MESDataDefine.MESData.StrURL = tB_MES_URL.Text;
                MESDataDefine.MESData.IsDisableMES = cB_EnableMES.Checked;
                MESDataDefine.MESData.StrURL_HIVE = tB_HIVE_IP.Text;
                //MESDataDefine.MESData.StrMiniPort = tB_PDCA_Port.Text;
                MESDataDefine.MESData.IsDisableHIVE = cB_EnableHIVE.Checked;
                Designer.json.WriteParams(MESDataDefine.MESData.GetType().Name, MESDataDefine.MESData);
                Popup("MES参数保存成功", "Info", MessageBoxIcon.Information);
            }
            catch (FormatException)
            {
                Popup("输入格式错误", "Error", MessageBoxIcon.Error);
            }
            finally { this.Activate(); }
        }

    }
}
