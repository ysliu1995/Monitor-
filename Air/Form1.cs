using System;
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
        }
        private void record(object number)
        {
            int[] buffer = new int[24];
            int index = 0;
            int n = Convert.ToInt32(number);
            String str = "";
            String date;
            while (true)
            {
                buffer[index] = serial_port[n].ReadByte();
                index += 1;
                if (index == 24)
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
    
                    Console.WriteLine(str);
                    Console.WriteLine(date);
                    Console.WriteLine("PM1 (CF=1) = " + buffer[5]);
                    Console.WriteLine("PM2.5 (CF=1) = " + buffer[7]);
                    Console.WriteLine("PM10 (CF=1) = " + buffer[9]);
                    Console.WriteLine("PM1 = " + buffer[11]);
                    Console.WriteLine("PM2.5 = " + buffer[13]);
                    Console.WriteLine("PM10 = " + buffer[15]);
                    Console.WriteLine("--------------------------------------");

                    
                    writer_list[n].Write(string.Format("{0}\t{1}\t{2}\t{3}\r\n", date, buffer[5].ToString(), buffer[7].ToString(), buffer[9].ToString()));

                    //initial 
                    str = "";
                    Array.Clear(buffer, 0, buffer.Length);
                    index = 0;
                    
                }
            }
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
        }
    }
}
