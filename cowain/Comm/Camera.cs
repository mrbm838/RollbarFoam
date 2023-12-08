using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cowain.FormView;

namespace cowain.Comm
{
    internal class Camera
    {
        public static CCDParams ccd = new CCDParams();

        public struct CCDParams
        {
            public string IP;
            public int PortOne;
            public int PortTwo;
            public bool enable;
            public string IPOther;
            public int PortOther;
            public bool enableOther;
        }

        public void LoadParams()
        {
            ccd.IP = "127.0.0.10";
            ccd.PortOne = 8008;
            ccd.PortTwo = 8009;
            ccd.enable = true;
            ccd.IPOther = "192.168.250.111";
            ccd.PortOther = 8008;
            ccd.enableOther = true;
            ccd = (CCDParams)Designer.json.ReadParams(ccd.GetType().Name, ccd);
        }

        public Camera()
        {
            LoadParams();
        }
    }
}
