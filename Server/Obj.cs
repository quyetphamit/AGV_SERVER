using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Obj
    {
        public string customer { get; set; }
        public string wo { get; set; }
        public string model { get; set; }
        public string type { get; set; }
        //public string hostName { get; set; }
        public string status { get; set; }
        public string timeCall { get; set; }
        public string timeReponseStart { get; set; }
        public string timeResponseEnd { get; set; }
    }
    public class Comport
    {
        public string id { get; set; }
        public string Ghi_Chu { get; set; }
        public string Cong_com { get; set; }
        public int BaudRate { get; set; }
        public int So_bit { get; set; }
        public string Parity { get; set; }
        public int StopBits { get; set; }
    }
}
