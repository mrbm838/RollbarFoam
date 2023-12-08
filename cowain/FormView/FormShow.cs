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

namespace cowain.FormView
{
    public partial class FormShow : Form
    {
        string fileName = String.Format("{0}.txt", DateTime.Now.ToString("yyyy_MM_dd"));
        string filePath = @"D:\Data\HIVE记录";
        string file = string.Empty;

        public FormShow()
        {
            InitializeComponent();
            file = Path.Combine(filePath, fileName);
            ShowOutMESSavedData();
            timerAlarm.Enabled = true;
        }
        
        private void ShowOutMESSavedData()
        {
            tB_MESInfo.Clear();
            lock (cowain.MES.LXPOSTClass.locker)
            {
                if (!File.Exists(file)) return;
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    bool flag = true;
                    int readByte = -10000;
                    do
                    {
                        try
                        {
                            fs.Seek(readByte, SeekOrigin.End);
                            flag = false;
                        }
                        catch
                        {
                            readByte += 1000;
                        }
                    } while (flag);
                    using (StreamReader sr = new StreamReader(fs, Encoding.Default))
                    {
                        while (!sr.EndOfStream)
                        {
                            tB_MESInfo.Text += sr.ReadLine() + "\r\n";
                        }
                        tB_MESInfo.Text += "The number of characters is : " + tB_MESInfo.TextLength;
                    }
                }
            }
        }

        private void tB_MESInfo_Click(object sender, EventArgs e)
        {
            ShowOutMESSavedData();
        }

        private void timerAlarm_Tick(object sender, EventArgs e)
        {
            string result17 = PLCQueueClass.GetResultFromList(OmronPLC.plc.plcAddresses[17].address);
            if (result17 == "1")
            {

            }
            string result18 = PLCQueueClass.GetResultFromListOther(OmronPLC.plc.plcAddresses[18].address);
            if (result18 == "1")
            {

            }
        }

        private void SaveAlarmMSG(string content)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
            File.WriteAllText(file, content);
        }
    }
}
