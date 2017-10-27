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
using System.Windows.Forms.DataVisualization.Charting;

namespace Server
{
    public partial class frmReport : Form
    {
        private string path = Application.StartupPath + "\\Setup";
        public frmReport()
        {
            InitializeComponent();
        }

        private void Report_Load(object sender, EventArgs e)
        {
            LoadSetting();
        }
        public void LoadSetting()
        {
            List<string> listCustomer = Directory.GetFiles(path, "*.txt")
                                    .Select(Path.GetFileNameWithoutExtension)
                                    .Where(r => !r.Contains("type") && !r.Contains("Comport"))
                                    .ToList();
            cbbCustomer.DataSource = listCustomer;
            cbbStatus.Items.Add("Hoàn thành");
            cbbStatus.Items.Add("Hủy");
            cbbStatus.SelectedIndex = 0;
            this.MaximizeBox = false;
        }

        private void cbbCustomer_SelectedIndexChanged(object sender, EventArgs e)
        {
            string customer = cbbCustomer.Text;
            cbbModel.DataSource = File.ReadLines(path + "\\" + customer + ".txt").ToList();
        }
        public string ConvertStatus(string oldStatus)
        {
            string newStatus = string.Empty;
            if (oldStatus.Contains("Hoàn thành"))
            {
                newStatus = "Finish";
            }
            else
            {
                newStatus = "Cancel";
            }
            return newStatus;
        }
        public void Search()
        {
            List<Obj> lst = new List<Obj>();
            string date = dtpTime.Value.ToString("ddMMyy");
            string fileSearch = Application.StartupPath + "\\Logfile\\" + date + ".txt";
            string customer = cbbCustomer.Text;
            string model = cbbModel.Text;
            string status = ConvertStatus(cbbStatus.Text).ToUpper();
            if (File.Exists(fileSearch))
            {
                File.ReadLines(fileSearch)
                    .Where(r => r.Contains(customer))
                    .Where(h => h.Contains(model))
                    .Where(t => t.ToUpper().Contains(status))
                    .ToList().ForEach(u =>
                    {
                        string[] col = u.Split(',');
                        Obj obj = new Obj() { customer = col[0], wo = col[1], model = col[2], type = col[3], timeCall = col[4], timeReponseStart = col[5], timeResponseEnd = col[6], status = col[7] };
                        lst.Add(obj);
                    });
            }
            //else
            //{
            //    Console.WriteLine("Hehe");
            //}

            dvSearch.DataSource = lst;
        }
        public void DrawChart()
        {
            this.chart1.Series.Clear();
            this.chart1.Titles.Clear();
            string[] seriesArray = { "Thành công", "Hủy" };
            string date = dtpTime.Value.ToString("ddMMyy");
            string fileSearch = Application.StartupPath + "\\Logfile\\" + date + ".txt";
            int finish = File.Exists(fileSearch) ?
                File.ReadLines(fileSearch)
                .Where(r => r.Contains(cbbModel.Text))
                .Where(h => h.ToUpper().Contains("FINISH"))
                .Count()
                : 0;
            int cancel = File.Exists(fileSearch) ?
                File.ReadLines(fileSearch)
               .Where(r => r.Contains(cbbModel.Text))
               .Where(h => h.Contains("#NA"))
               .Count()
               : 0;
            if (finish > 0 || cancel > 0)
            {
                int[] pointsArray = { finish, cancel };
                this.chart1.Titles.Add(cbbCustomer.Text);
                // Add series.
                for (int i = 0; i < seriesArray.Length; i++)
                {
                    // Add series.
                    Series series = this.chart1.Series.Add(seriesArray[i]);

                    // Add point.
                    series.Points.Add(pointsArray[i]);
                }
            }
        }
        public void ExportCSV()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                InitialDirectory = Application.StartupPath,
                FileName = DateTime.Now.ToString("ddMMyy") + ".csv",
                Filter = "csv File(.csv)|*.csv|All files(*.*)|*.*",
                Title = "Save CSV file"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                //string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(dialog.FileName);
                var sb = new StringBuilder();

                var headers = dvSearch.Columns.Cast<DataGridViewColumn>();
                sb.AppendLine(string.Join(",", headers.Select(column => "\"" + column.HeaderText + "\"").ToArray()));

                foreach (DataGridViewRow row in dvSearch.Rows)
                {
                    var cells = row.Cells.Cast<DataGridViewCell>();
                    sb.AppendLine(string.Join(",", cells.Select(cell => "\"" + cell.Value + "\"").ToArray()));
                }
                using (StreamWriter writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8))
                {
                    writer.WriteLine(sb.ToString());
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ExportCSV();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Search();
            DrawChart();
        }
    }
}
