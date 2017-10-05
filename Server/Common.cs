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
    }
}
