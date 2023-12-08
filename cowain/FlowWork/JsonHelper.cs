using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace cowain.FlowWork
{
    public class JsonHelper
    {
        private object locker = new object();

        private string strBasePath = Directory.GetCurrentDirectory().Replace("cowain\\bin\\Debug", "Data");

        private string CheckDirectory(string fileName)
        {
            if (!Directory.Exists(strBasePath))
            {
                Directory.CreateDirectory(strBasePath);
            }
            return strBasePath + "\\" + fileName + ".json";
        }

        public void WriteParams(string fileName, object objTemp)
        {
            string path = CheckDirectory(fileName);
            lock (locker)
            {
                ObjectToJsonFile(path, objTemp);
            }
        }

        public void ObjectToJsonFile(string path, object obj)
        {
            string str = JsonConvert.SerializeObject(obj);
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(str);
                    sw.Flush();
                    fs.Flush(true);
                }
            }
        }

        public object ReadParams(string fileName, object objTemp)
        {
            string strPath = CheckDirectory(fileName);
            if (File.Exists(strPath))
            {
                objTemp = JsonFileToObject(strPath, objTemp);
            }
            else
            {
                ObjectToJsonFile(strPath, objTemp);
            }
            return objTemp;
        }

        public object JsonFileToObject(string path, object obj)
        {
            string strBack = string.Empty;
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    strBack = sr.ReadToEnd();
                }
            }
            return JsonConvert.DeserializeObject(strBack, obj.GetType());
        }
    }
}
