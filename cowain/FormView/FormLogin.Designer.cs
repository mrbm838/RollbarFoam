namespace cowain.FormView
{
    partial class FormLogin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel3 = new System.Windows.Forms.Panel();
            this.lb_LoginStatus = new System.Windows.Forms.Label();
            this.panel_LoginPart = new System.Windows.Forms.Panel();
            this.panel_Login = new System.Windows.Forms.Panel();
            this.btn_Modify = new System.Windows.Forms.Button();
            this.btn_Login = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tB_Password = new System.Windows.Forms.TextBox();
            this.panel_Modify = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.tB_CheckPassword = new System.Windows.Forms.TextBox();
            this.bt_Back = new System.Windows.Forms.Button();
            this.bt_ModifyPwd = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.tB_NewPassword = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lb_Title = new System.Windows.Forms.Label();
            this.tB_Account = new System.Windows.Forms.TextBox();
            this.panel3.SuspendLayout();
            this.panel_LoginPart.SuspendLayout();
            this.panel_Login.SuspendLayout();
            this.panel_Modify.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.panel3.Controls.Add(this.lb_LoginStatus);
            this.panel3.Location = new System.Drawing.Point(155, 562);
            this.panel3.Margin = new System.Windows.Forms.Padding(4);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(604, 100);
            this.panel3.TabIndex = 44;
            // 
            // lb_LoginStatus
            // 
            this.lb_LoginStatus.AutoSize = true;
            this.lb_LoginStatus.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lb_LoginStatus.Location = new System.Drawing.Point(193, 4);
            this.lb_LoginStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lb_LoginStatus.Name = "lb_LoginStatus";
            this.lb_LoginStatus.Size = new System.Drawing.Size(218, 42);
            this.lb_LoginStatus.TabIndex = 1;
            this.lb_LoginStatus.Text = "Login Status";
            // 
            // panel_LoginPart
            // 
            this.panel_LoginPart.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.panel_LoginPart.Controls.Add(this.panel_Login);
            this.panel_LoginPart.Controls.Add(this.label1);
            this.panel_LoginPart.Controls.Add(this.lb_Title);
            this.panel_LoginPart.Controls.Add(this.tB_Account);
            this.panel_LoginPart.Controls.Add(this.panel_Modify);
            this.panel_LoginPart.Location = new System.Drawing.Point(155, 15);
            this.panel_LoginPart.Margin = new System.Windows.Forms.Padding(4);
            this.panel_LoginPart.Name = "panel_LoginPart";
            this.panel_LoginPart.Size = new System.Drawing.Size(604, 517);
            this.panel_LoginPart.TabIndex = 43;
            // 
            // panel_Login
            // 
            this.panel_Login.Controls.Add(this.btn_Modify);
            this.panel_Login.Controls.Add(this.btn_Login);
            this.panel_Login.Controls.Add(this.label2);
            this.panel_Login.Controls.Add(this.tB_Password);
            this.panel_Login.Location = new System.Drawing.Point(37, 202);
            this.panel_Login.Name = "panel_Login";
            this.panel_Login.Size = new System.Drawing.Size(546, 295);
            this.panel_Login.TabIndex = 39;
            // 
            // btn_Modify
            // 
            this.btn_Modify.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Modify.Enabled = false;
            this.btn_Modify.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Modify.Location = new System.Drawing.Point(291, 161);
            this.btn_Modify.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Modify.Name = "btn_Modify";
            this.btn_Modify.Size = new System.Drawing.Size(146, 48);
            this.btn_Modify.TabIndex = 39;
            this.btn_Modify.Text = "修改密码";
            this.btn_Modify.UseVisualStyleBackColor = true;
            this.btn_Modify.Click += new System.EventHandler(this.btn_Modify_Click);
            // 
            // btn_Login
            // 
            this.btn_Login.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Login.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Login.Location = new System.Drawing.Point(86, 161);
            this.btn_Login.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Login.Name = "btn_Login";
            this.btn_Login.Size = new System.Drawing.Size(153, 48);
            this.btn_Login.TabIndex = 38;
            this.btn_Login.Text = "登陆";
            this.btn_Login.UseVisualStyleBackColor = true;
            this.btn_Login.Click += new System.EventHandler(this.btn_Login_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(81, 38);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(106, 30);
            this.label2.TabIndex = 36;
            this.label2.Text = "密码：";
            // 
            // tB_Password
            // 
            this.tB_Password.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tB_Password.Location = new System.Drawing.Point(226, 26);
            this.tB_Password.Margin = new System.Windows.Forms.Padding(4);
            this.tB_Password.Multiline = true;
            this.tB_Password.Name = "tB_Password";
            this.tB_Password.PasswordChar = '*';
            this.tB_Password.Size = new System.Drawing.Size(212, 49);
            this.tB_Password.TabIndex = 37;
            // 
            // panel_Modify
            // 
            this.panel_Modify.Controls.Add(this.label4);
            this.panel_Modify.Controls.Add(this.tB_CheckPassword);
            this.panel_Modify.Controls.Add(this.bt_Back);
            this.panel_Modify.Controls.Add(this.bt_ModifyPwd);
            this.panel_Modify.Controls.Add(this.label3);
            this.panel_Modify.Controls.Add(this.tB_NewPassword);
            this.panel_Modify.Location = new System.Drawing.Point(86, 179);
            this.panel_Modify.Margin = new System.Windows.Forms.Padding(4);
            this.panel_Modify.Name = "panel_Modify";
            this.panel_Modify.Size = new System.Drawing.Size(450, 332);
            this.panel_Modify.TabIndex = 38;
            this.panel_Modify.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(4, 129);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(168, 30);
            this.label4.TabIndex = 41;
            this.label4.Text = "确认密码：";
            // 
            // tB_CheckPassword
            // 
            this.tB_CheckPassword.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tB_CheckPassword.Location = new System.Drawing.Point(176, 117);
            this.tB_CheckPassword.Margin = new System.Windows.Forms.Padding(4);
            this.tB_CheckPassword.Multiline = true;
            this.tB_CheckPassword.Name = "tB_CheckPassword";
            this.tB_CheckPassword.PasswordChar = '*';
            this.tB_CheckPassword.Size = new System.Drawing.Size(212, 49);
            this.tB_CheckPassword.TabIndex = 42;
            // 
            // bt_Back
            // 
            this.bt_Back.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bt_Back.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bt_Back.Location = new System.Drawing.Point(36, 261);
            this.bt_Back.Margin = new System.Windows.Forms.Padding(4);
            this.bt_Back.Name = "bt_Back";
            this.bt_Back.Size = new System.Drawing.Size(153, 48);
            this.bt_Back.TabIndex = 40;
            this.bt_Back.Text = "返回";
            this.bt_Back.UseVisualStyleBackColor = true;
            this.bt_Back.Click += new System.EventHandler(this.bt_Back_Click);
            // 
            // bt_ModifyPwd
            // 
            this.bt_ModifyPwd.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bt_ModifyPwd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bt_ModifyPwd.Location = new System.Drawing.Point(241, 261);
            this.bt_ModifyPwd.Margin = new System.Windows.Forms.Padding(4);
            this.bt_ModifyPwd.Name = "bt_ModifyPwd";
            this.bt_ModifyPwd.Size = new System.Drawing.Size(146, 48);
            this.bt_ModifyPwd.TabIndex = 38;
            this.bt_ModifyPwd.Text = "确认修改";
            this.bt_ModifyPwd.UseVisualStyleBackColor = true;
            this.bt_ModifyPwd.Click += new System.EventHandler(this.bt_ModifyPwd_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(16, 43);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(137, 30);
            this.label3.TabIndex = 36;
            this.label3.Text = "新密码：";
            // 
            // tB_NewPassword
            // 
            this.tB_NewPassword.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tB_NewPassword.Location = new System.Drawing.Point(176, 31);
            this.tB_NewPassword.Margin = new System.Windows.Forms.Padding(4);
            this.tB_NewPassword.Multiline = true;
            this.tB_NewPassword.Name = "tB_NewPassword";
            this.tB_NewPassword.PasswordChar = '*';
            this.tB_NewPassword.Size = new System.Drawing.Size(212, 49);
            this.tB_NewPassword.TabIndex = 37;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(118, 134);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 30);
            this.label1.TabIndex = 34;
            this.label1.Text = "账号：";
            // 
            // lb_Title
            // 
            this.lb_Title.AutoSize = true;
            this.lb_Title.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.lb_Title.Font = new System.Drawing.Font("微软雅黑", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lb_Title.Location = new System.Drawing.Point(30, 45);
            this.lb_Title.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lb_Title.Name = "lb_Title";
            this.lb_Title.Size = new System.Drawing.Size(137, 40);
            this.lb_Title.TabIndex = 1;
            this.lb_Title.Text = "用户登录";
            // 
            // tB_Account
            // 
            this.tB_Account.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tB_Account.Location = new System.Drawing.Point(262, 125);
            this.tB_Account.Margin = new System.Windows.Forms.Padding(4);
            this.tB_Account.Multiline = true;
            this.tB_Account.Name = "tB_Account";
            this.tB_Account.Size = new System.Drawing.Size(212, 46);
            this.tB_Account.TabIndex = 33;
            this.tB_Account.TextChanged += new System.EventHandler(this.tB_Account_TextChanged);
            // 
            // FormLogin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(925, 729);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel_LoginPart);
            this.Name = "FormLogin";
            this.Text = "FormLogin";
            this.Load += new System.EventHandler(this.FormLogin_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.FormLogin_Paint);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel_LoginPart.ResumeLayout(false);
            this.panel_LoginPart.PerformLayout();
            this.panel_Login.ResumeLayout(false);
            this.panel_Login.PerformLayout();
            this.panel_Modify.ResumeLayout(false);
            this.panel_Modify.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label lb_LoginStatus;
        private System.Windows.Forms.Panel panel_LoginPart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lb_Title;
        private System.Windows.Forms.TextBox tB_Account;
        private System.Windows.Forms.Panel panel_Modify;
        private System.Windows.Forms.Button bt_ModifyPwd;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tB_NewPassword;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tB_CheckPassword;
        private System.Windows.Forms.Button bt_Back;
        private System.Windows.Forms.Panel panel_Login;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tB_Password;
        public System.Windows.Forms.Button btn_Login;
        public System.Windows.Forms.Button btn_Modify;
    }
}