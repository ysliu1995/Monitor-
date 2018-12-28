using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Air
{
    public partial class Form1 : Form
    {
        private List<SerialPort> serial_port;
        private List<String> comport_list;
        private List<GroupBox> groupbox_list;
        private List<Label> label_list;
        private List<StreamWriter> writer_list;
        private List<Thread> thread_list;
        private List<Chart> chart_list;
        private List<Queue> queue_list;

        //queue input size
        private static int num = 1;

        public Form1()
        {
            InitializeComponent();
        }
        private void GetPortName()
        {
            comboBox1.Items.Clear();

            string[] serialPorts = SerialPort.GetPortNames();
            foreach (string serialPort in serialPorts)
            {
                comboBox1.Items.Add(serialPort);
                if (comboBox1.Items.Count > 0)
                {
                    comboBox1.SelectedIndex = 0;
                }
            }
        }
        private void CreateComport()
        {
            SerialPort port;
            foreach (String comport in comport_list)
            {
                port = new SerialPort(comport, 9600, Parity.None, 8, StopBits.One);
                serial_port.Add(port);
            }
        }
        private void OpenComport()
        {
            for (int i = 0; i < serial_port.Count; i++)
            {
                try
                {
                    if ((serial_port[i] != null) && (!serial_port[i].IsOpen))
                    {
                        serial_port[i].Open();

                        ParameterizedThreadStart record_T = new ParameterizedThreadStart(record);
                        Thread record_Thread = new Thread(record_T);

                        String store = DateTime.Now.ToString("yyyyMMddhhmm") + "-" + comport_list[i] + ".txt";
                        StreamWriter writer = new StreamWriter(store, false);

                        Queue q1 = new Queue();
                        Queue q2 = new Queue();
                        Queue q3 = new Queue();
                        queue_list.Add(q1);
                        queue_list.Add(q2);
                        queue_list.Add(q3);
                        InitChart(3 * i);
                        InitChart(3 * i + 1);
                        InitChart(3 * i + 2);

                        writer.Write("\t\t\tPM1\tPM2.5\tPM10\r\n");
                        writer_list.Add(writer);
                        thread_list.Add(record_Thread);
                        record_Thread.Start(i);

                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(String.Format("出問題啦:{0}", e.ToString()));
                }
            }
            label16.Text = "Connected";
            timer1.Start();
        }
        private void record(object number)
        {
            int[] buffer = new int[32];
            int index = 0;
            int n = Convert.ToInt32(number);
            Boolean flag = false;
            String str = "";
            String date;
            String pre = "";
            while (true)
            {
                buffer[index] = serial_port[n].ReadByte();
                index += 1;
                if (index == 32)
                {
                    date = (DateTime.Now.Year) + "/" + (DateTime.Now.Month) + "/" + (byte)(DateTime.Now.Day) + " " + (DateTime.Now.Hour) + ":" + (DateTime.Now.Minute) + ":" + (DateTime.Now.Second);
                    if (date != pre)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            str += Convert.ToString(buffer[i], 16) + " ";
                        }
                        Console.WriteLine("This is " + comport_list[n]);
                        label_list[n * 3].Text = "PM1 : " + buffer[5].ToString() + " ug/m3";
                        label_list[n * 3 + 1].Text = "PM2.5 : " + buffer[7].ToString() + " ug/m3";
                        label_list[n * 3 + 2].Text = "PM10 : " + buffer[9].ToString() + " ug/m3";
                        date = (DateTime.Now.Year) + "/" + (DateTime.Now.Month) + "/" + (byte)(DateTime.Now.Day) + " " + (DateTime.Now.Hour) + ":" + (DateTime.Now.Minute) + ":" + (DateTime.Now.Second);
                        label17.Text = date;
                        pre = date;
                        Console.WriteLine(str);
                        Console.WriteLine(date);
                        Console.WriteLine("PM1 (CF=1) = " + buffer[5]);
                        Console.WriteLine("PM2.5 (CF=1) = " + buffer[7]);
                        Console.WriteLine("PM10 (CF=1) = " + buffer[9]);
                        Console.WriteLine("PM1 = " + buffer[11]);
                        Console.WriteLine("PM2.5 = " + buffer[13]);
                        Console.WriteLine("PM10 = " + buffer[15]);
                        Console.WriteLine("--------------------------------------");

                        queue_list[n * 3].Enqueue(buffer[5]);
                        queue_list[n * 3 + 1].Enqueue(buffer[7]);
                        queue_list[n * 3 + 2].Enqueue(buffer[9]);


                        writer_list[n].Write(string.Format("{0}\t{1}\t{2}\t{3}\r\n", date, buffer[5].ToString(), buffer[7].ToString(), buffer[9].ToString()));
                    }
                    //initial 
                    str = "";
                    Array.Clear(buffer, 0, buffer.Length);
                    index = 0;
                    
                }
            }
        }
        private void InitChart(int i) {
            chart_list[i].Series.Clear();
            Series series = new Series();
            series.ChartType = SeriesChartType.Spline;
            series.IsVisibleInLegend = false;
            chart_list[i].Series.Add(series);
            chart_list[i].ChartAreas[0].AxisY.Minimum = 0;           
            chart_list[i].ChartAreas[0].AxisY.Maximum = 30;           
            chart_list[i].ChartAreas[0].AxisX.Minimum = 0;           
            chart_list[i].ChartAreas[0].AxisX.Maximum = 100;           
            chart_list[i].ChartAreas[0].AxisX.Interval = 10;          
            chart_list[i].ChartAreas[0].AxisY.Interval = 15;
            //chart1.ChartAreas[0].AxisX.Title = "time";              //設置下方橫座標名稱
            //chart1.ChartAreas[0].AxisY.Title = "ug/m3";          //設置左邊縱座標的名稱
            chart_list[i].Series[0].Color = Color.Red;               
        }
        private void UpdateQueueValue(Queue q) {
            //Console.WriteLine(q.Count);
            if (q.Count > 100) {
                for (int i = 0; i < num; i++)
                {
                    q.Dequeue();
                }
            }
        }
        private int FindQueueMax(Queue q)
        {
            int max = -1;
            foreach (int n in q)
            {
                if (n > max)
                {
                    max = n;
                }
            }
            return max;
        }
        private int FindQueueMean(Queue q)
        {
            int cnt = 0;
            foreach (int n in q)
            {
                cnt += n;
            }
            return (int)(cnt / q.Count + 1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Bug
            Form1.CheckForIllegalCrossThreadCalls = false;

            //initial
            comport_list = new List<String>();
            groupbox_list = new List<GroupBox>();
            label_list = new List<Label>();
            serial_port = new List<SerialPort>();
            writer_list = new List<StreamWriter>();
            thread_list = new List<Thread>();
            chart_list = new List<Chart>();
            queue_list = new List<Queue>();

            groupbox_list.Add(groupBox1);
            groupbox_list.Add(groupBox2);
            groupbox_list.Add(groupBox3);
            groupbox_list.Add(groupBox4);
            groupbox_list.Add(groupBox5);

            label_list.Add(label1);
            label_list.Add(label2);
            label_list.Add(label3);
            label_list.Add(label4);
            label_list.Add(label5);
            label_list.Add(label6);
            label_list.Add(label7);
            label_list.Add(label8);
            label_list.Add(label9);
            label_list.Add(label10);
            label_list.Add(label11);
            label_list.Add(label12);
            label_list.Add(label13);
            label_list.Add(label14);
            label_list.Add(label15);

            chart_list.Add(chart1);
            chart_list.Add(chart2);
            chart_list.Add(chart3);
            chart_list.Add(chart4);
            chart_list.Add(chart5);
            chart_list.Add(chart6);
            chart_list.Add(chart7);
            chart_list.Add(chart8);
            chart_list.Add(chart9);
            chart_list.Add(chart10);
            chart_list.Add(chart11);
            chart_list.Add(chart12);
            chart_list.Add(chart13);
            chart_list.Add(chart14);
            chart_list.Add(chart15);


            GetPortName();

            
        }
        private void button2_Click(object sender, EventArgs e)
        {
            comport_list.Add((String)comboBox1.SelectedItem);
            for (int i = 0; i < comport_list.Count; i++)
            {
                groupbox_list[i].Text = comport_list[i];
                groupbox_list[i].Visible = true;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            GetPortName();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            CreateComport();
            OpenComport();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            //close writer
            foreach (StreamWriter sw in writer_list)
            {
                sw.Close();
            }
            //close thread
            foreach (Thread t in thread_list)
            {
                if (t.IsAlive)
                {
                    t.Suspend();
                }
            }

            timer1.Stop();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < queue_list.Count/3; i++)
            {
                UpdateQueueValue(queue_list[3 * i]);
                UpdateQueueValue(queue_list[3 * i + 1]);
                UpdateQueueValue(queue_list[3 * i + 2]);

                chart_list[3 * i].Series[0].Points.Clear();
                chart_list[3 * i + 1].Series[0].Points.Clear();
                chart_list[3 * i + 2].Series[0].Points.Clear();

                for (int j = 0; j < queue_list[3 * i].Count; j++)
                {
                    chart_list[3 * i].Series[0].Points.AddXY((j), queue_list[3 * i].ToArray()[j]);
                    chart_list[3 * i].ChartAreas[0].AxisY.Maximum = FindQueueMax(queue_list[3 * i]) + 5;
                    chart_list[3 * i].ChartAreas[0].AxisY.Interval = (int)(FindQueueMean(queue_list[3 * i]));
                }
                for (int j = 0; j < queue_list[3 * i + 1].Count; j++)
                {
                    chart_list[3 * i + 1].Series[0].Points.AddXY((j), queue_list[3 * i + 1].ToArray()[j]);
                    chart_list[3 * i + 1].ChartAreas[0].AxisY.Maximum = FindQueueMax(queue_list[3 * i + 1]) + 5;
                    chart_list[3 * i + 1].ChartAreas[0].AxisY.Interval = (int)(FindQueueMean(queue_list[3 * i + 1]));
                }
                for (int j = 0; j < queue_list[3 * i + 2].Count; j++)
                {
                    chart_list[3 * i + 2].Series[0].Points.AddXY((j), queue_list[3 * i + 2].ToArray()[j]);
                    chart_list[3 * i + 2].ChartAreas[0].AxisY.Maximum = FindQueueMax(queue_list[3 * i + 2]) + 5;
                    chart_list[3 * i + 2].ChartAreas[0].AxisY.Interval = (int)(FindQueueMean(queue_list[3 * i + 2]));
                }
            }
        }
    }
}
