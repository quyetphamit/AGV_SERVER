using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
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
        private const int PORT = 8888;
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
                catch
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
            try
            {
                if (client != null && data != string.Empty)
                {
                    client.Send(Serialize(data));
                }

            }
            catch
            {
                MessageBox.Show("Error");
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
                    result = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                clientList.Remove(client);
                client.Close();
            }

        }
        public byte[] Serialize(object obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                byte[] bytes = stream.ToArray();
                stream.Flush();
                return bytes;
            }

        }
        object Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                //BinaryFormatter formatter = new BinaryFormatter();
                //return formatter.Deserialize(stream);
                stream.Position = 0;
                object desObj = new BinaryFormatter().Deserialize(stream);
                stream.Flush();
                return desObj;
            }

        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Stop();
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
            //    //lblModel.Text = "";
            //}
            //else 
            if (result.Contains("CANCEL"))
            {
                string content = getBetween(result, "", "*");
                Button btn = GetButtonSelected(this, typeof(Button), content);
                btn.Text = btn.Text.Replace(Environment.NewLine + "calling...", null);
                btn.BackColor = Color.LightBlue;
                RemoveItemListView(content);

            }
            else if (result.Contains("FINISH"))
            {
                string content = getBetween(result, "", "*");
                Button btn = GetButtonSelected(this, typeof(Button), content);
                btn.Text = btn.Text.Replace(Environment.NewLine + "comming...", null);
                btn.BackColor = Color.LightBlue;
                btn.Enabled = true;
            }
            else
            {
                Obj obj = new Obj();
                obj.customer = getBetween(result, "", "@");
                obj.wo = getBetween(result, "@", "$");
                obj.model = getBetween(result, "$", "%");
                obj.type = getBetween(result, "%", "&");
                obj.status = getBetween(result, "&", "*");
                obj.timeCall = getBetween(result, "*", "#");
                listObj.Add(obj);
                ListViewItem itemContent = new ListViewItem(new[] { obj.customer, obj.wo, obj.model, obj.type, obj.timeCall });
                lvwView.Items.Add(itemContent);
                Button button = GetButtonSelected(this, typeof(Button), obj.customer);
                button.BackColor = Color.Red;
                button.Text += Environment.NewLine + "calling...";
                // Send 
                // Trường hợp nhiều client
                //foreach (var item in clientList)
                //{
                //    Send(item, result);
                //}

                //lblModel.Text = listObj[0].model;
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
                .Where(h => h.Text.Contains(content))
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
                .Where(h => !h.Text.Contains("Reset"));
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
            string host = getBetween(result, "*", "#");
            List<Obj> lst = listObj.FindAll(x => x.status == host);
            var buttons = GetAllButtons(this, typeof(Button));
            foreach (var button in buttons)
            {
                foreach (var item in lst)
                {
                    if (item.customer == button.Text)
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
                Button top = GetButtonSelected(this, typeof(Button), listObj[0].customer);
                top.BackColor = Color.Red;
                foreach (var item in clientList)
                {
                    Send(item, obj.customer + "DONE#");
                    Console.WriteLine("Send ok");
                }
                string content = obj.model + ", " + obj.customer;
                //lblModel.Text = listObj[0].model;
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
            lvwView.Columns.Add("Khách hàng", 300);
            lvwView.Columns.Add("Số WO", 250);
            lvwView.Columns.Add("Model", 300);
            lvwView.Columns.Add("Kiểu", 200);
            lvwView.Columns.Add("Thời gian gọi AGV", 350);
        }
        private void RemoveItemListView(string input)
        {
            for (int i = 0; i < lvwView.Items.Count; i++)
            {
                var item = lvwView.Items[i];
                if (item.Text.Contains(input))
                {
                    lvwView.Items.Remove(item);
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.BackColor == Color.Red)
            {
                button2.Text = button2.Text.Replace("calling", "comming");
                button2.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "TOYODENSO*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("TOYODENSO");
                button2.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.BackColor == Color.Red)
            {
                button3.Text = button3.Text.Replace("calling", "comming");
                button3.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "YOKOWO*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("YOKOWO");
                button3.Enabled = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.BackColor == Color.Red)
            {
                button4.Text = button4.Text.Replace("calling", "comming");
                button4.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "CANON*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("CANON");
                button4.Enabled = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.BackColor == Color.Red)
            {
                button5.Text = button5.Text.Replace("calling", "comming");
                button5.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "MURATA*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("MURATA");
                button5.Enabled = false;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (button6.BackColor == Color.Red)
            {
                button6.Text = button6.Text.Replace("calling", "comming");
                button6.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "FUJI*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("FUJI");
                button6.Enabled = false;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (button7.BackColor == Color.Red)
            {
                button7.Text = button7.Text.Replace("calling", "comming");
                button7.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "NICHICON*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("NICHICON");
                button7.Enabled = false;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (button8.BackColor == Color.Red)
            {
                button8.Text = button8.Text.Replace("calling", "comming");
                button8.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "HONDA*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("HONDA");
                button8.Enabled = false;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (button9.BackColor == Color.Red)
            {
                button9.Text = button9.Text.Replace("calling", "comming");
                button9.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "NIHON*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("NIHON");
                button9.Enabled = false;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (button10.BackColor == Color.Red)
            {
                button10.Text = button10.Text.Replace("calling", "comming");
                button10.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "BROTHER*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("BROTHER");
                button10.Enabled = false;
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (button11.BackColor == Color.Red)
            {
                button11.Text = button11.Text.Replace("calling", "comming");
                button11.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "SCHNEIDER*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("SCHNEIDER");
                button11.Enabled = false;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (button12.BackColor == Color.Red)
            {
                button12.Text = button12.Text.Replace("calling", "comming");
                button12.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "KYOCERA*" + sTime + "$" + "COMMING#");
                }
                RemoveItemListView("KYOCERA");
                button12.Enabled = false;
            }
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CloseConnect();
            this.Close();
        }
        public SerialPort getPort(string comId)
        {
            var portSetting = Common.Comport().FirstOrDefault(r => r.id == comId);
            if (portSetting != null)
            {
                SerialPort comPort = new SerialPort()
                {
                    PortName = portSetting.Cong_com,
                    BaudRate = portSetting.BaudRate,
                    DataBits = portSetting.So_bit,
                    Parity = portSetting.Parity == "None" ? Parity.None :
                             portSetting.Parity == "Even" ? Parity.Even :
                             portSetting.Parity == "Odd" ? Parity.Odd :
                             portSetting.Parity == "Mark" ? Parity.Mark : Parity.Space,
                    StopBits = portSetting.StopBits == 0 ? StopBits.None :
                               portSetting.StopBits == 1 ? StopBits.One : StopBits.Two

                };
                try
                {
                    comPort.Open();
                }
                catch (Exception e)
                {
                    if (MessageBox.Show(e.Message, "Information",MessageBoxButtons.OK,MessageBoxIcon.Error) == DialogResult.OK)
                    {
                        // quyetpham
                        Application.Exit();
                    }
                }
                return comPort;
            }
            else return null;
        }
    }
}
