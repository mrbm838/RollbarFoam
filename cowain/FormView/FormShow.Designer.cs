namespace cowain.FormView
{
    partial class FormShow
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
            this.components = new System.ComponentModel.Container();
            this.tB_MESInfo = new System.Windows.Forms.TextBox();
            this.timerAlarm = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // tB_MESInfo
            // 
            this.tB_MESInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tB_MESInfo.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tB_MESInfo.Location = new System.Drawing.Point(13, 13);
            this.tB_MESInfo.Multiline = true;
            this.tB_MESInfo.Name = "tB_MESInfo";
            this.tB_MESInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tB_MESInfo.Size = new System.Drawing.Size(968, 786);
            this.tB_MESInfo.TabIndex = 0;
            this.tB_MESInfo.Click += new System.EventHandler(this.tB_MESInfo_Click);
            // 
            // timerAlarm
            // 
            this.timerAlarm.Interval = 2000;
            this.timerAlarm.Tick += new System.EventHandler(this.timerAlarm_Tick);
            // 
            // FormShow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1013, 804);
            this.Controls.Add(this.tB_MESInfo);
            this.Name = "FormShow";
            this.Text = "FormMES";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tB_MESInfo;
        private System.Windows.Forms.Timer timerAlarm;
    }
}