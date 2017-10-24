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
        private string pathLog = Application.StartupPath + "\\logfile";
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
            catch
            {
                //Console.WriteLine(ex.Message);
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
            if (comControl.IsOpen)
            {
                comControl.Write("X");
                comControl.Close();
            }
            //CloseConnect();
        }
        public void View()
        {
            if (result.Contains("CANCEL"))
            {
                string nameFile = pathLog + "\\" + DateTime.Now.ToString("ddMMyy") + ".txt";
                string content = getBetween(result, "", "*");
                Button btn = GetButtonSelected(this, typeof(Button), content);
                btn.Text = btn.Text.Replace(Environment.NewLine + "Calling...", null);
                btn.BackColor = Color.LightBlue;
                ReplaceSubItemListView(content, 5, "#NA");
                ReplaceSubItemListView(content, 6, DateTime.Now.ToString("HH:mm:ss"));
                ReplaceSubItemListView(content, 7, "Cancel");
                SaveLog(nameFile, btn.Text);
                RemoveItemListView(content);

            }
            else if (result.Contains("FINISH"))
            {
                string content = getBetween(result, "", "*");
                Button btn = GetButtonSelected(this, typeof(Button), content);
                btn.Text = btn.Text.Replace(Environment.NewLine + "Comming...", null);
                ReplaceSubItemListView(content, 6, DateTime.Now.ToString("HH:mm:ss"));
                ReplaceSubItemListView(content, 7, "Finish");
                string nameFile = pathLog + "\\" + DateTime.Now.ToString("ddMMyy") + ".txt";
                SaveLog(nameFile, btn.Text);
                RemoveItemListView(content);
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
                ListViewItem itemContent = new ListViewItem(new[] { obj.customer, obj.wo, obj.model, obj.type, obj.timeCall, obj.timeReponseStart, obj.timeResponseEnd, obj.status });
                lvwView.Items.Add(itemContent);
                Button button = GetButtonSelected(this, typeof(Button), obj.customer);
                button.BackColor = Color.Red;
                button.Text += Environment.NewLine + "Calling...";
                ReplaceSubItemListView(obj.customer, 7, "Calling");
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
        private void timer1_Tick(object sender, EventArgs e)
        {
            lblDatetime.Text = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start();
            comControl = SetupComport("com01");
            LoadSetting();
        }
        public void LoadSetting()
        {
            if (!Directory.Exists("LogFile"))
            {
                Directory.CreateDirectory("LogFile");
            }
            lvwView.Columns.Add("Khách hàng", 200);
            lvwView.Columns.Add("Số WO", 200);
            lvwView.Columns.Add("Model", 200);
            lvwView.Columns.Add("Kiểu", 200);
            lvwView.Columns.Add("Thời gian gọi", 200);
            lvwView.Columns.Add("Thời gian trả", 200);
            lvwView.Columns.Add("Thời gian kết thúc", 250);
            lvwView.Columns.Add("Trạng thái", 200);
            if (!Directory.Exists(Application.StartupPath + "\\logfile"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\logfile");
            }
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
                string nameFile = pathLog + "\\" + DateTime.Now.ToString("ddMMyy") + ".txt";
                button2.Text = button2.Text.Replace("Calling", "Comming");
                button2.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "TOYODENSO*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("TOYODENSO", 5, sTime);
                ReplaceSubItemListView("TOYODENSO", 7, "Comming");
                //SaveLog(nameFile, button2.Text);
                button2.Enabled = false; ;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.BackColor == Color.Red)
            {
                string nameFile = pathLog + "\\" + DateTime.Now.ToString("ddMMyy") + ".txt";
                button3.Text = button3.Text.Replace("Calling", "Comming");
                button3.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "YOKOWO*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("YOKOWO", 5, sTime);
                ReplaceSubItemListView("YOKOWO", 7, "Comming");
                //SaveLog(nameFile, button3.Text);
                button3.Enabled = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.BackColor == Color.Red)
            {
                button4.Text = button4.Text.Replace("Calling", "Comming");
                button4.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "CANON*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("CANON", 5, sTime);
                ReplaceSubItemListView("CANON", 7, "Comming");
                button4.Enabled = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.BackColor == Color.Red)
            {
                button5.Text = button5.Text.Replace("Calling", "Comming");
                button5.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "MURATA*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("MURATA", 5, sTime);
                ReplaceSubItemListView("MURATA", 7, "Comming");
                button5.Enabled = false;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (button6.BackColor == Color.Red)
            {
                button6.Text = button6.Text.Replace("Calling", "Comming");
                button6.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "FUJI*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("FUJI", 5, sTime);
                ReplaceSubItemListView("FUJI", 7, "Comming");
                button6.Enabled = false;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (button7.BackColor == Color.Red)
            {
                button7.Text = button7.Text.Replace("Calling", "Comming");
                button7.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "NICHICON*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("NICHICON", 5, sTime);
                ReplaceSubItemListView("NICHICON", 7, "Comming");
                button7.Enabled = false;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (button8.BackColor == Color.Red)
            {
                button8.Text = button8.Text.Replace("Calling", "Comming");
                button8.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "HONDA*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("HONDA", 5, sTime);
                ReplaceSubItemListView("HONDA", 7, "Comming");
                button8.Enabled = false;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (button9.BackColor == Color.Red)
            {
                button9.Text = button9.Text.Replace("Calling", "Comming");
                button9.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "NIHON*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("NIHON", 5, sTime);
                ReplaceSubItemListView("NIHON", 7, "Comming");
                button9.Enabled = false;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (button10.BackColor == Color.Red)
            {
                button10.Text = button10.Text.Replace("Calling", "Comming");
                button10.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "BROTHER*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("BROTHER", 5, sTime);
                ReplaceSubItemListView("BROTHER", 7, "Comming");
                button10.Enabled = false;
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (button11.BackColor == Color.Red)
            {
                button11.Text = button11.Text.Replace("Calling", "Comming");
                button11.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "SCHNEIDER*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("SCHNEIDER", 5, sTime);
                ReplaceSubItemListView("SCHNEIDER", 7, "Comming");
                button11.Enabled = false;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (button12.BackColor == Color.Red)
            {
                button12.Text = button12.Text.Replace("Calling", "Comming");
                button12.BackColor = Color.Yellow;
                string sTime = DateTime.Now.ToString("HH:mm:ss");
                foreach (var item in clientList)
                {
                    Send(item, "KYOCERA*" + sTime + "$" + "COMMING#");
                }
                ReplaceSubItemListView("KYOCERA", 5, sTime);
                ReplaceSubItemListView("KYOCERA", 7, "Comming");
                button12.Enabled = false;
            }
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CloseConnect();
            this.Close();
        }
        public SerialPort SetupComport(string comId)
        {
            var portSetting = Common.ReadComport().FirstOrDefault(r => r.id == comId);
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
                    if (MessageBox.Show(e.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                    {
                        // quyetpham
                        Application.Exit();
                    }
                }
                return comPort;
            }
            else return null;
        }
        public void ReplaceSubItemListView(string customer, int index, string content)
        {
            for (int i = 0; i < lvwView.Items.Count; i++)
            {
                var item = lvwView.Items[i];
                if (item.Text.Contains(customer))
                {
                    item.SubItems[index].Text = content;
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAbout frmAbout = new frmAbout();
            frmAbout.ShowDialog();
        }
        public void SaveLog(string pathLog, string customer)
        {
            string content = string.Empty;
            using (StreamWriter writer = new StreamWriter(pathLog, true, Encoding.Unicode))
            {
                for (int i = 0; i < lvwView.Items.Count; i++)
                {
                    var item = lvwView.Items[i];
                    if (item.Text.Contains(customer))
                    {
                        content = item.SubItems[0].Text + "," + item.SubItems[1].Text + ","
                            + item.SubItems[2].Text + "," + item.SubItems[3].Text + ","
                            + item.SubItems[4].Text + "," + item.SubItems[5].Text + ","
                            + item.SubItems[6].Text + "," + item.SubItems[7].Text;
                    }
                }
                writer.WriteLine(content);
            }
        }

        private void reportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmReport frmReport = new frmReport();
            frmReport.ShowDialog();
        }
    }
}
