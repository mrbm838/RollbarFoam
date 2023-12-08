using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using cowain.Comm;
using cowain.FlowWork;
using System.Diagnostics;
using cowain.MES;
using System.Collections.Concurrent;

namespace cowain.FormView
{
    public partial class Designer : Form
    {
        private float formHeight;
        private float formWidth;
        private bool bResize = false;

        public static JsonHelper json = new JsonHelper();
        private FormHome fHome = null; //new FormHome(Popup);
        private FormSet fSet = null;
        private FormLogin fLogin = null;
        private FormShow fSHow = null;
        public DoWork doWork = null;
        public static bool lockClicked = false;
        private object locker = new object();

        private ToolStripMenuItem MI_Clear = new ToolStripMenuItem("清屏");

        public Designer()
        {
            InitializeComponent();
            doWork = new DoWork(ShowInRTB);

            formHeight = this.Height;
            formWidth = this.Width;
            SaveToTag(this);
        }

        private void Designer_Load(object sender, EventArgs e)
        {
            rTB_Message.Text = String.Empty;
            tsBtHome_Click(tsBtHome, e);

            // Set false if resize is not need
            bResize = false;

            contextMenuStrip.Items.Add(MI_Clear);
            rTB_Message.ContextMenuStrip = contextMenuStrip;
            MI_Clear.Click += MI_Clear_Click;
            MI_Clear.Enabled = false;
        }

        private void MI_Clear_Click(object sender, EventArgs e)
        {
            if (MI_Clear.Enabled)
            {
                rTB_Message.Clear();
                MI_Clear.Enabled = false;
            }
        }

        #region 控件大小自适应
        void SaveToTag(Control control)
        {
            foreach (Control item in control.Controls)
            {
                item.Tag = item.Height + "," + item.Width + "," + item.Top + "," + item.Left;
                if (item.Controls.Count > 0)
                {
                    SaveToTag(item);
                }
            }
        }

        private void Designer_Resize(object sender, EventArgs e)
        {
            if (!bResize) return;
            float PHeight = this.Height / formHeight;
            float PWidth = this.Width / formWidth;
            ResetSize(PHeight, PWidth, this);
        }

        private void ResetSize(float PHeight, float PWidth, Control control)
        {
            foreach (Control item in control.Controls)
            {
                string[] strings = item.Tag.ToString().Split(',');
                item.Height = (int)(Convert.ToSingle(strings[0]) * PHeight);
                item.Width = (int)(Convert.ToSingle(strings[1]) * PWidth);
                item.Top = (int)(Convert.ToSingle(strings[2]) * PHeight);
                item.Left = (int)(Convert.ToSingle(strings[3]) * PWidth);
                if (item.Controls.Count > 0)
                {
                    ResetSize(PHeight, PWidth, item);
                }
            }
        }
        #endregion

        private void OpenForm(Form form)
        {
            form.TopLevel = false;
            form.WindowState = FormWindowState.Normal;
            form.FormBorderStyle = FormBorderStyle.None;
            //form.Parent = panel;
            panel.Controls.Add(form);
            form.Dock = DockStyle.Fill;
            form.Show();

            //this.IsMdiContainer = true; // Set parent form as container
            //form.MdiParent = this; // Set parent-child relationship
            //form.Parent = this.panel; // Set the parent formof the form as the container
            //form.Show();
        }

        private void HideForm()
        {
            foreach (Control item in panel.Controls)
            {
                if (item is Form)
                {
                    ((Form)item).Hide();
                }
                //if (item == richTextBox1)
                //{
                //    break;
                //}
                //item.Hide();
            }
        }

        private void tsBtHome_Click(object sender, EventArgs e)
        {
            HideForm();
            if (fHome == null) fHome = new FormHome(Popup);
            OpenForm(fHome);
            //fHome.ShowContent();
        }

        private void tsBtSetting_Click(object sender, EventArgs e)
        {
            HideForm();
            if (fSet == null) fSet = new FormSet();
            OpenForm(fSet);
            fSet.ShowContent();
        }

        private void tsBtOperator_Click(object sender, EventArgs e)
        {
            HideForm();
            if (fLogin == null) fLogin = new FormLogin(ShowLockIcon);
            OpenForm(fLogin);
        }

        private void tsBt_Lock_Click(object sender, EventArgs e)
        {
            ShowLockIcon(false);
            lockClicked = true;
        }

        private void ShowLockIcon(bool bShow)
        {
            if (bShow)
                tsBt_Lock.Visible = true;
            else
                tsBt_Lock.Visible = false;
        }

        private void tsBtShow_Click(object sender, EventArgs e)
        {
            HideForm();
            if (fSHow == null) fSHow = new FormShow();
            OpenForm(fSHow);
        }

        //private void tsBt_LoginOut_Click(object sender, EventArgs e)
        //{
        //    if (!FormLogin.bLoginSuccess)
        //        return;
        //    if (DialogResult.Yes == Popup("是否退出登录？"))
        //    {
        //        FormLogin.bLoginSuccess = false;
        //        fLogin.indexOP = -1;
        //        fLogin.btn_Login.Enabled = true;
        //        fLogin.btn_Modify.Enabled = false;
        //        fSet.Enabled = false;
        //    }
        //}

        public void ShowInRTB(string text, Color color)
        {
            lock (locker)
            {
                string strPrint = string.Format("{0}：{1}\r\n", DateTime.Now.ToString("HH:mm:ss.fff"), text);
                SaveRunLog(strPrint);
                Invoke((EventHandler)delegate
                {
                    if (rTB_Message.Lines.Length > 500)
                        rTB_Message.Text = "";
                    rTB_Message.SelectionColor = color;
                    rTB_Message.AppendText(strPrint);
                });
            }
        }

        private void SaveRunLog(string text)
        {
            string dirSave = @"D:\Data\运行日志";
            if (!Directory.Exists(dirSave))
                Directory.CreateDirectory(dirSave);
            string fileSave = Path.Combine(dirSave, DateTime.Now.ToString("yyyy-MM-dd")) + ".txt";
            File.AppendAllText(fileSave, text);
        }

        private void Designer_FormClosing(object sender, FormClosingEventArgs e)
        {
            //try
            //{
            if (DialogResult.Cancel == Popup("是否关闭程序？"))
            {
                this.Activate();
                e.Cancel = true;
            }
            else
            {
                Process[] allProcesses = Process.GetProcesses();
                foreach (Process item in allProcesses)
                    if (item.ProcessName.Contains("cowain"))
                        item.Kill();
            }
            //Environment.Exit(0);
            //}
            //catch { }
        }

        private DialogResult Popup(string text)
        {
            return MessageBox.Show(text, "提示",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            //contextMenuStrip.BindingContext = BindingContext.;

            if (rTB_Message.TextLength > 0)
                MI_Clear.Enabled = true;
        }

        private void tSL_MachineName_DoubleClick(object sender, EventArgs e)
        {
            CacheProcessOther.CacheQueueOther = new ConcurrentQueue<Product>();
            CacheProcessOther.SThree_Product = null;
            CacheProcess.CacheQueue = new ConcurrentQueue<Product>();
            CacheProcess.STwo_Product = null;
            HoldPressOther.Temp_ProductOther[0] = null;
            HoldPressOther.Temp_ProductOther[1] = null;
            HoldPressOther.HPTwo_Product = null;
            HoldPress.Temp_Product[0]= null;
            HoldPress.Temp_Product[1] = null;
            HoldPress.HPOne_Product = null;
            DoWork.SOne_Product = null;
            rTB_Message.Clear();
            rTB_Message.SelectionColor = Color.Brown;
            rTB_Message.AppendText("已清除缓存！！！\r\n");
        }
    }
}
