using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cowain.FormView
{
    public partial class MESLogin : Form
    {
        public MESLogin()
        {
            InitializeComponent();
            tBID.Focus();
        }

        private void tBID_TextChanged(object sender, EventArgs e)
        {
            if (tBID.Text.Contains("\r\n"))
            {
                tBPwd.Focus();
            }
        }

        private void tBPwd_TextChanged(object sender, EventArgs e)
        {
            if (tBPwd.Text.Contains("\r\n"))
            {
                btLogin.Focus();
            }
        }

        private void btLogin_Click(object sender, EventArgs e)
        {
            //PostClass.OPID = tBID.Text.Trim();
            //PostClass.OPPwd = tBPwd.Text.Trim();
            //PostClass.AddCMD(CMDStep.用户登录);
            timer_Login.Enabled = true;
        }

        private void timer_Login_Tick(object sender, EventArgs e)
        {
            //string rsLogin = PostClass.GetMESResult(CMDStep.用户登录);
            //if (rsLogin == "OK")
            //{
            //    timer_Login.Enabled = false;
            //    MessageBox.Show("登录成功！", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else if (rsLogin == "NG")
            //{
            //    timer_Login.Enabled = false;
            //    FormMain.ShowMSG("登录失败！", "Error");
            //    tBID.Text = "";
            //    tBPwd.Text = "";
            //    tBID.Focus();
            //}
        }
    }
}
