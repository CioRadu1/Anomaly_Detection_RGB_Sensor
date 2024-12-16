using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace SSCGUI
{
    public partial class Form1 : Form
    {
        private SerialPort serialPort;
        private Timer updateTimer;
        private Panel resultColorPanel;

        public Form1()
        {
            InitializeComponent();
            InitializeSerialPort();
            InitializeCharts();
            InitializeResultingColorPanel();
        }

        private void InitializeSerialPort()
        {
            serialPort = new SerialPort("COM6", 9600);
            serialPort.DataReceived += SerialPort_DataReceived;
            try
            {
                serialPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening serial port: {ex.Message}");
            }
        }

        private void InitializeCharts()
        {
            InitializeSingleChart(chart4, "Red", Color.Red);

            InitializeSingleChart(chart5, "Green", Color.Green);

            InitializeSingleChart(chart6, "Blue", Color.Blue);

            InitializeSingleChart(chart7, "Light", Color.Yellow);

            InitializeZValueChart(chart1);

            InitializeCUSUMChart(chart2);
        }

        private void InitializeSingleChart(Chart chart, string title, Color color)
        {
            chart.Series.Clear();
            chart.Titles.Clear();

            chart.Titles.Add(title);

            var series = new Series
            {
                Name = title,
                ChartType = SeriesChartType.Column,
                Color = color
            };

            chart.Series.Add(series);

            chart.ChartAreas[0].AxisY.Minimum = 0;
            if (title == "Light")
            {
                chart.ChartAreas[0].AxisY.Maximum = 1024;
            }
            else
            {
                chart.ChartAreas[0].AxisY.Maximum = 255;
            }
            chart.ChartAreas[0].AxisX.Minimum = 0;
            chart.ChartAreas[0].AxisX.Maximum = 10;
            chart.ChartAreas[0].AxisX.Interval = 1;
        }

        private void InitializeResultingColorPanel()
        {
            panel1.BackColor = Color.Black;
        }

        private void InitializeCUSUMChart(Chart chart)
        {
            chart.Series.Clear();
            chart.Titles.Clear();

            chart.Titles.Add("CUSUM Chart");

            chart.Series.Add(CreateCUSUMSeries("Red CUSUM", Color.Red));
            chart.Series.Add(CreateCUSUMSeries("Green CUSUM", Color.Green));
            chart.Series.Add(CreateCUSUMSeries("Blue CUSUM", Color.Blue));
            chart.Series.Add(CreateCUSUMSeries("Light CUSUM", Color.Orange));

            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisY.Maximum = 50;
        }

        private Series CreateCUSUMSeries(string name, Color color)
        {
            return new Series
            {
                Name = name,
                ChartType = SeriesChartType.Line,
                Color = color,
                BorderWidth = 2
            };
        }

        private void InitializeZValueChart(Chart chart)
        {
            chart.Series.Clear();
            chart.Titles.Clear();

            chart.Titles.Add("Z Values");

            var redZSeries = new Series
            {
                Name = "Red Z",
                ChartType = SeriesChartType.Line,
                Color = Color.DarkRed,
                BorderWidth = 2
            };

            var greenZSeries = new Series
            {
                Name = "Green Z",
                ChartType = SeriesChartType.Line,
                Color = Color.DarkGreen,
                BorderWidth = 2
            };

            var blueZSeries = new Series
            {
                Name = "Blue Z",
                ChartType = SeriesChartType.Line,
                Color = Color.DarkBlue,
                BorderWidth = 2
            };

            var lightZSeries = new Series
            {
                Name = "Light Z",
                ChartType = SeriesChartType.Line,
                Color = Color.Orange,
                BorderWidth = 2
            };

            chart.Series.Add(redZSeries);
            chart.Series.Add(greenZSeries);
            chart.Series.Add(blueZSeries);
            chart.Series.Add(lightZSeries);


            chart.ChartAreas[0].AxisY.Minimum = -5;
            chart.ChartAreas[0].AxisY.Maximum = 5;
            chart.ChartAreas[0].AxisX.Minimum = 0;
            chart.ChartAreas[0].AxisX.Maximum = 10;
            chart.ChartAreas[0].AxisX.Interval = 1;
        }
        int readingCount = 0;
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = serialPort.ReadLine();
            var values = data.Split(',');

            if (values.Length == 12 &&
                float.TryParse(values[0], out float red) &&
                float.TryParse(values[1], out float green) &&
                float.TryParse(values[2], out float blue) &&
                float.TryParse(values[3], out float light) &&
                float.TryParse(values[4], out float redZ) &&
                float.TryParse(values[5], out float greenZ) &&
                float.TryParse(values[6], out float blueZ) &&
                float.TryParse(values[7], out float lightZ) &&
                float.TryParse(values[8], out float redCUSUM) &&
                float.TryParse(values[9], out float greenCUSUM) &&
                float.TryParse(values[10], out float blueCUSUM) &&
                float.TryParse(values[11], out float lightCUSUM))
            {
                if (readingCount < 10)
                {
                    AddDataToChart(chart4, red);
                    AddDataToChart(chart5, green);
                    AddDataToChart(chart6, blue);
                    AddDataToChart(chart7, light);
                    UpdateResultingColor((int)red, (int)green, (int)blue);

                    AddDataToZChart(chart1, "Red Z", redZ);
                    AddDataToZChart(chart1, "Green Z", greenZ);
                    AddDataToZChart(chart1, "Blue Z", blueZ);
                    AddDataToZChart(chart1, "Light Z", lightZ);

                    AddDataToCUSUMChart(chart2, "Red CUSUM", redCUSUM, 2.0f);
                    AddDataToCUSUMChart(chart2, "Green CUSUM", greenCUSUM, 2.0f);
                    AddDataToCUSUMChart(chart2, "Blue CUSUM", blueCUSUM, 2.0f);
                    AddDataToCUSUMChart(chart2, "Light CUSUM", lightCUSUM, 2.0f);

                    readingCount++;
                }

                if (readingCount == 10)
                {
                    string data2 = serialPort.ReadLine();
                    var values2 = data2.Split(',');

                    if (values2.Length == 2 &&
                        float.TryParse(values2[0], out float execTime) &&
                        int.TryParse(values2[1], out int memoryUsage))
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                textBox1.Text = execTime.ToString() + " ms";
                                textBox2.Text = memoryUsage.ToString() + " bytes";
                            });
                        }

                        readingCount = 0;
                    }
                }
            }
        }

        private void AddDataToCUSUMChart(Chart chart, string seriesName, float value, float threshold)
        {
            if (chart.InvokeRequired)
            {
                chart.BeginInvoke((MethodInvoker)(() => AddDataToCUSUMChart(chart, seriesName, value, threshold)));
            }
            else
            {
                var series = chart.Series[seriesName];

                if (series.Points.Count > 10)
                    series.Points.RemoveAt(0);

                var pointIndex = series.Points.AddY(value);

                if (Math.Abs(value) > threshold)
                {
                    series.Points[pointIndex].MarkerStyle = MarkerStyle.Circle;
                    series.Points[pointIndex].MarkerSize = 8;
                    series.Points[pointIndex].MarkerColor = Color.Red;
                }
                else
                {
                    series.Points[pointIndex].MarkerStyle = MarkerStyle.None;
                }
            }
        }



        private void AddDataToZChart(Chart chart, string seriesName, float value)
        {
            if (chart.InvokeRequired)
            {
                chart.BeginInvoke((MethodInvoker)(() => AddDataToZChart(chart, seriesName, value)));
            }
            else
            {
                var series = chart.Series[seriesName];

                if (series.Points.Count > 10)
                    series.Points.RemoveAt(0);

                series.Points.AddY(value);
            }
        }


        private void AddDataToChart(Chart chart, float value)
        {
            if (chart.InvokeRequired)
            {
                chart.BeginInvoke((MethodInvoker)(() => AddDataToChart(chart, value)));
            }
            else
            {
                if (chart.Series[0].Points.Count > 10)
                    chart.Series[0].Points.RemoveAt(0);

                chart.Series[0].Points.AddY(value);
            }
        }

        private void UpdateResultingColor(int red, int green, int blue)
        {
            red = Math.Max(0, Math.Min(255, red));
            green = Math.Max(0, Math.Min(255, green));
            blue = Math.Max(0, Math.Min(255, blue));

            panel1.BackColor = Color.FromArgb(red, green, blue);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
                serialPort.Close();
        }

        private void chart4_Click(object sender, EventArgs e) { }
        private void chart5_Click(object sender, EventArgs e) { }
        private void chart6_Click(object sender, EventArgs e) { }
        private void chart7_Click(object sender, EventArgs e) { }
        private void chart1_Click(object sender, EventArgs e)  { }

        private void chart2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
