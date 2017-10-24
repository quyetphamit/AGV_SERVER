using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Common
    {
        public static void WriteLog(string path, string content)
        {
            using (StreamWriter writer = new StreamWriter(path, true, Encoding.Unicode))
            {
                try
                {
                    writer.WriteLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ", " + content);
                }
                catch (Exception)
                {

                }
            }
        }
        public static List<Comport> ReadComport()
        {
            List<Comport> comport = new List<Server.Comport>();
            File.ReadAllLines("Setup\\Comport.txt").ToList().ForEach(r =>
            {
                string[] col = r.Split(',');
                comport.Add(new Comport
                {
                    Ghi_Chu = col[0],
                    Cong_com = col[1],
                    BaudRate = Convert.ToInt32(col[2]),
                    So_bit = Convert.ToInt32(col[3]),
                    Parity = col[4],
                    StopBits = Convert.ToInt32(col[5]),
                    id = col[6]
                });
            });
            return comport;
        }

    }
}
