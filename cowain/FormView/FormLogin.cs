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
    public partial class FormLogin : Form
    {
        public static Permission permission = new Permission();
        public static bool bLoginSuccess = false;
        public int indexOP = -1;
        Action<bool> ShowLockIcon = null;

        public struct Permission
        {
            public string[,] roles;
        }

        public void LoadParams()
        {
            permission.roles = new string[10, 3];
            permission.roles[0, 0] = "cowain";
            permission.roles[0, 1] = "123";
            permission.roles[0, 2] = "1";

            permission = (Permission)Designer.json.ReadParams(permission.GetType().Name, permission);
        }

        public FormLogin(Action<bool> action)
        {
            ShowLockIcon = new Action<bool>(action);
            InitializeComponent();
        }

        private void FormLogin_Load(object sender, EventArgs e)
        {

        }

        private void FormLogin_Paint(object sender, PaintEventArgs e)
        {
            //if (bLoginSuccess)
            //    btn_Login.Enabled = false;
            //else
            //    btn_Login.Enabled = true;
            lb_LoginStatus.Text = "Status";
            lb_LoginStatus.ForeColor = Color.Black;
            tB_Password.Text = "";
        }

        private void SearchRole()//(object button)
        {
            if (permission.roles == null) LoadParams();
            bool bAccount = false;
            bool bPassword = false;
            for (int i = 0; i < permission.roles.GetLength(0); i++)
            {
                if (permission.roles[i, 0] == null) continue;
                if (!String.Equals(permission.roles[i, 0], tB_Account.Text)) continue;
                bAccount = true;
                if (!String.Equals(permission.roles[i, 1], tB_Password.Text)) break;
                bPassword = true;
                indexOP = i;
            }
            if (!bAccount)
            {
                lb_LoginStatus.Text = "账号不存在";
                lb_LoginStatus.ForeColor = Color.OrangeRed;
            }
            else if (!bPassword)
            {
                lb_LoginStatus.Text = "密码错误";
                lb_LoginStatus.ForeColor = Color.OrangeRed;
            }
            //else if ((Button)button == btn_Login) return index;
            //else if ((Button)button == btn_Modify) return index;
            else
            {
                lb_LoginStatus.Text = "Unknow Error";
                lb_LoginStatus.ForeColor = Color.OrangeRed;
            }
        }

        private void bt_ModifyPwd_Click(object sender, EventArgs e)
        {
            lb_LoginStatus.Text = "";
            if (indexOP == -1 || !bLoginSuccess) return;
            if (String.Equals(tB_NewPassword.Text, tB_CheckPassword.Text))
            {
                permission.roles[indexOP, 1] = tB_NewPassword.Text;
                Designer.json.WriteParams("Operator", permission);
                lb_LoginStatus.Text = "密码修改成功";
                lb_LoginStatus.ForeColor = Color.ForestGreen;
            }
            else
            {
                lb_LoginStatus.Text = "密码不一致";
                lb_LoginStatus.ForeColor = Color.OrangeRed;
            }
        }

        private void btn_Modify_Click(object sender, EventArgs e)
        {
            if (indexOP != -1)
            {
                lb_LoginStatus.Text = "Status";
                panel_Login.Visible = false;
                panel_Modify.Visible = true;
                lb_Title.Text = "密码修改";
            }
        }

        private void btn_Login_Click(object sender, EventArgs e)
        {
            lb_LoginStatus.Text = "";
            SearchRole();
            if (indexOP != -1)
            {
                bLoginSuccess = true;
                lb_LoginStatus.Text = "登录成功";
                lb_LoginStatus.ForeColor = Color.ForestGreen;
                //btn_Login.Enabled = false;
                btn_Modify.Enabled = true;

                ShowLockIcon(true);
                Designer.lockClicked = false;
            }
        }

        private void bt_Back_Click(object sender, EventArgs e)
        {
            lb_LoginStatus.Text = "Status";
            panel_Modify.Visible = false;
            panel_Login.Visible = true;
            lb_Title.Text = "用户登录";
            tB_Password.Text = "";
        }

        private void tB_Account_TextChanged(object sender, EventArgs e)
        {
            if (tB_Account.TextLength > 10)
            {
                btn_Login_Click(sender, e);
            }
        }
    }
}
