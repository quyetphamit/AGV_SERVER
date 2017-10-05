using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form
    {
        private const int PORT = 9999;
        private IPEndPoint IP;
        private Socket server;
        private List<Socket> clientList;
        private List<Obj> listObj = new List<Obj>();
        private string result;
        private string path = "LogFile\\Log.txt";
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            OpenConnect();
        }
        public void OpenConnect()
        {
            clientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Any, PORT);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            server.Bind(IP);
            Thread listen = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        clientList.Add(client);
                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch (Exception)
                {
                    IP = new IPEndPoint(IPAddress.Any, PORT);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }
            });
            listen.IsBackground = true;
            listen.Start();
        }
        public void CloseConnect()
        {
            server.Close();
        }
        public void Send(Socket client, string data)
        {
            if (client != null && data != string.Empty)
            {
                client.Send(Serialize(data));
            }
        }
        public void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    result = Deserialize(data).ToString();
                    View();
                }
            }
            catch (Exception)
            {
                clientList.Remove(client);
                client.Close();
            }

        }
        public byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }
        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (var item in clientList)
            {
                Send(item, "SERVER CLOSE#");
            }
            CloseConnect();
        }
        public void View()
        {
            //if (result.Contains("RESET#"))
            //{
            //    Reset();
            //    listObj = new List<Obj>();
            //    lblModel.Text = "";
            //}
            //else 

            if (result.Contains("CLOSE#"))
            {
                ResetClient();
            }
            else
            {
                Obj obj = new Obj();
                obj.line = getBetween(result, "*", "#");
                obj.model = getBetween(result, "&", "*");
                obj.hostName = getBetween(result, "", "&");
                listObj.Add(obj);
                Button button = GetButtonSelected(this, typeof(Button), obj.line);
                // Send 
                foreach (var item in clientList)
                {
                    Send(item, result);
                }
                if (listObj.Count == 1)
                {
                    button.BackColor = Color.Red;
                }
                else
                {
                    button.BackColor = Color.Orange;
                }
                lblModel.Text = listObj[0].model;
            }
        }
        public string getBetween(string input, string from, string to)
        {
            int iFrom = input.IndexOf(from) + from.Length;
            int iTo = input.LastIndexOf(to);
            return input.Substring(iFrom, iTo - iFrom);
        }
        public Button GetButtonSelected(Control control, Type type, string content)
        {
            var controls = control.Controls.Cast<Control>();
            return (Button)controls.SelectMany(r => GetAllButtons(r, type))
                .Concat(controls)
                .Where(c => c.GetType() == type)
                .Where(h => h.Text.Equals(content))
                .FirstOrDefault();
        }
        public Button GetButtonSelected(Control control, Type type, Color color)
        {
            var controls = control.Controls.Cast<Control>();
            return (Button)controls.SelectMany(r => GetAllButtons(r, type))
                .Concat(controls)
                .Where(c => c.GetType() == type)
                .Where(h => h.BackColor.Equals(color))
                .FirstOrDefault();
        }
        public IEnumerable<Control> GetAllButtons(Control control, Type type)
        {
            var controls = control.Controls.Cast<Control>();
            return controls.SelectMany(r => GetAllButtons(r, type))
                .Concat(controls)
                .Where(c => c.GetType() == type)
                .Where(h => h.Text.Contains("LINE"));
        }

        public void Reset()
        {
            var buttons = GetAllButtons(this, typeof(Button));
            foreach (var button in buttons)
            {
                button.BackColor = Color.LightBlue;
                button.Enabled = true;
            }
        }
        public void ResetClient()
        {
            string host = getBetween(result, "", "*");
            List<Obj> lst = listObj.FindAll(x => x.hostName == host);
            var buttons = GetAllButtons(this, typeof(Button));
            foreach (var button in buttons)
            {
                foreach (var item in lst)
                {
                    if (item.line == button.Text)
                    {
                        button.BackColor = Color.LightBlue;
                    }
                }
            }
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            if (listObj.Count > 1)
            {
                Obj obj = listObj[0];
                listObj.RemoveAt(0);
                Button button = GetButtonSelected(this, typeof(Button), Color.Red);
                button.BackColor = Color.LightBlue;
                Button top = GetButtonSelected(this, typeof(Button), listObj[0].line);
                top.BackColor = Color.Red;
                foreach (var item in clientList)
                {
                    Send(item, obj.line + "DONE#");
                }
                string content = obj.model + ", " + obj.line;
                lblModel.Text = listObj[0].model;
                Common.WriteLog(path, content);
            }
            else
            {
                Reset();
                listObj = new List<Obj>();
                foreach (var item in clientList)
                {
                    Send(item, "FINISH#");
                }
                lblModel.Text = "";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblDatetime.Text = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start();
            if (!Directory.Exists("LogFile"))
            {
                Directory.CreateDirectory("LogFile");
            }
        }
    }
}
