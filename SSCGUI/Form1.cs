using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace SSCGUI
{
    public partial class Form1 : Form
    {
        private SerialPort serialPort;
        private Timer updateTimer;
        private Panel resultColorPanel;

        Dictionary<string, int> redMap = InitializeRangeMap();
        Dictionary<string, int> greenMap = InitializeRangeMap();
        Dictionary<string, int> blueMap = InitializeRangeMap();
        Dictionary<string, int> lightMap = InitializeRangeMapLight();

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

            InitializeSingleChart(chart7, "Light", Color.Gray);

            InitializeZValueChart(chart1);

            InitializeZValueChart(chart3);

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
                Color = color,
                BorderWidth = 2
            };

            chart.Series.Add(series);

            chart.ChartAreas[0].AxisX.Minimum = 0;
            if (title == "Light")
            {
                chart.ChartAreas[0].AxisX.Maximum = 1024;
                chart.ChartAreas[0].AxisX.Interval = 100;

            }
            else
            {
                chart.ChartAreas[0].AxisX.Maximum = 255;
                chart.ChartAreas[0].AxisX.Interval = 20;

            }
            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisY.Maximum = 100;
            chart.ChartAreas[0].AxisY.Interval = 20;
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

            if (values.Length == 16 &&
                float.TryParse(values[0], out float red) &&
                float.TryParse(values[1], out float green) &&
                float.TryParse(values[2], out float blue) &&
                float.TryParse(values[3], out float light) &&
                float.TryParse(values[4], out float redZ) &&
                float.TryParse(values[5], out float greenZ) &&
                float.TryParse(values[6], out float blueZ) &&
                float.TryParse(values[7], out float lightZ) &&
                float.TryParse(values[8], out float redZWf) &&
                float.TryParse(values[9], out float greenZWf) &&
                float.TryParse(values[10], out float blueZWf) &&
                float.TryParse(values[11], out float lightZWf) &&
                float.TryParse(values[12], out float redCUSUM) &&
                float.TryParse(values[13], out float greenCUSUM) &&
                float.TryParse(values[14], out float blueCUSUM) &&
                float.TryParse(values[15], out float lightCUSUM))
            {
                UpdateRangeMap(redMap, (int)red);
                UpdateRangeMap(greenMap, (int)green);
                UpdateRangeMap(blueMap, (int)blue);
                UpdateRangeMap(lightMap, (int)light);

                if (readingCount < 10)
                {
                    AddDataToChart(chart4, redMap);
                    AddDataToChart(chart5, greenMap);
                    AddDataToChart(chart6, blueMap);
                    AddDataToChart(chart7, lightMap);
                    UpdateResultingColor((int)red, (int)green, (int)blue);

                    AddDataToZChart(chart1, "Red Z", redZ);
                    AddDataToZChart(chart1, "Green Z", greenZ);
                    AddDataToZChart(chart1, "Blue Z", blueZ);
                    AddDataToZChart(chart1, "Light Z", lightZ);

                    AddDataToZChart(chart3, "Red Z", redZWf);
                    AddDataToZChart(chart3, "Green Z", greenZWf);
                    AddDataToZChart(chart3, "Blue Z", blueZWf);
                    AddDataToZChart(chart3, "Light Z", lightZWf);

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

                    if (values2.Length == 4 &&
                        float.TryParse(values2[0], out float execTimeOrg) &&
                        float.TryParse(values2[1], out float execTimeWf) &&
                        float.TryParse(values2[2], out float execTimeTotal) &&
                        int.TryParse(values2[3], out int memoryUsage))
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                textBox4.Text = execTimeOrg.ToString() + " μs";
                                textBox3.Text = execTimeWf.ToString() + " μs";
                                textBox1.Text = execTimeTotal.ToString() + " ms";
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


        private void AddDataToChart(Chart chart, Dictionary<string, int> map)
        {
            if (chart == null)
            {
                return;
            }
            if (chart.InvokeRequired)
            {
                chart.BeginInvoke((MethodInvoker)(() => AddDataToChart(chart, map)));
            }
            else
            {
                try
                {
                    chart.Series[0].Points.Clear();
                    foreach (var range in map)
                    {
                        var bounds = range.Key.Split('-');
                        int lowerBound = int.Parse(bounds[0]);
                        int upperBound = int.Parse(bounds[1]);

                        int xPosition = (lowerBound + upperBound) / 2;

                        chart.Series[0].Points.AddXY(xPosition, range.Value);
                    }

                    chart.Series[0].ChartType = SeriesChartType.Column;
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Error map: {ex.Message}");
                }
            }
        }

        public void ResetChartForMap(Dictionary<string, int> map)
        {
            if (map == redMap)
            {
                ResetChart(chart4, "Red");
            }
            else if (map == greenMap)
            {
                ResetChart(chart5, "Green");
            }
            else if (map == blueMap)
            {
                ResetChart(chart6, "Blue");
            }
            else if (map == lightMap)
            {
                ResetChart(chart7, "Light");
            }
        }

        public void ResetChart(Chart chart, string seriesName)
        {
            if (chart.InvokeRequired)
            {
                chart.BeginInvoke((MethodInvoker)(() => ResetChart(chart, seriesName)));
            }
            else
            {
                if (chart.Series.Count > 0)
                {
                    chart.Series[0].Points.Clear();

                }
            }
        }

        static Dictionary<string, int> InitializeRangeMap()
        {
            Dictionary<string, int> rangeMap = new Dictionary<string, int>();
            for (int i = 0; i <= 240; i += 20)
            {
                int upperBound = (i + 20 > 255) ? 255 : i + 20;
                string rangeKey = GetRangeKey(i, upperBound);
                rangeMap[rangeKey] = 0;
            }
            return rangeMap;
        }
        static Dictionary<string, int> InitializeRangeMapLight()
        {
            Dictionary<string, int> rangeMap = new Dictionary<string, int>();
            for (int i = 0; i <= 1000; i += 100)
            {
                int upperBound = (i + 100 > 1024) ? 1204 : i + 100;
                string rangeKey = GetRangeKey(i, upperBound);
                rangeMap[rangeKey] = 0;
            }
            return rangeMap;
        }
        private void UpdateRangeMap(Dictionary<string, int> map, int value)
        {
            foreach (var range in map.Keys)
            {
                var bounds = range.Split('-');
                int lowerBound = int.Parse(bounds[0]);
                int upperBound = int.Parse(bounds[1]);

                if (value >= lowerBound && value <= upperBound)
                {
                    map[range]++;

                    if (map[range] >= 100)
                    {
                        ResetChartForMap(map);
                        map[range] = 0;
                    }
                    break;
                }
            }
        }

        static string GetRangeKey(int lower, int upper)
        {
            return $"{lower}-{upper}";
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

    }
}
