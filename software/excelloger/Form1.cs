using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace excelloger
{
    public partial class Form1 : Form
    {
        String sPort;
        String sBaud;
        String sData;
        int length = 512;
        byte[] buf0 = new byte[4096];
        byte[] buf = new byte[4096];
        char[] arbeg = {'B','e','g','i','n',':'};
        ushort lengthr = 0;
        ushort pos;     //текущая позиция в буфере
        int state = 0;// 0-определение начала; 1-заполнение буфера
        ushort[] buf2 = new ushort[4096];
        bool bPortOpen = false;
        bool NewData = false;
        ushort TrigerPosS;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < SerialPort.GetPortNames().Length; i++)
            {
                comboBox_ports.Items.Add(SerialPort.GetPortNames()[i]);
            }
//            comboBox_ports.Text = "COM7";// SerialPort.GetPortNames()[0];

            comboBox_baud.Items.Add("9600");
            comboBox_baud.Items.Add("19200");
            comboBox_baud.Items.Add("38400");
            comboBox_baud.Items.Add("57600");
            comboBox_baud.Items.Add("115200");
            comboBox_baud.Items.Add("128000");
            comboBox_baud.Items.Add("256000");
            comboBox_baud.Items.Add("512000");
            comboBox_baud.Items.Add("1024000");
//            comboBox_baud.Text = ("115200");

            RegLoad();

            Graphics g = pictureBox1.CreateGraphics();
            pos = 0;
//            Open();
            pictureBox1.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
/*            char[] buf = new char[4096];
            buf[0] = 'B';
            buf[1] = 'e';
            buf[2] = 'g';
            buf[3] = 'i';
            buf[4] = 'n';
            buf[5] = ':';
            for (int i = 0; i < length; i++)
            {
                double angle    = Math.PI*4 * i / 1021;
                double sinAngle = Math.Sin(angle)*512+512;
//                int d = (int)sinAngle;
                int d = i;
                buf[i * 2 + 5] = (char)(d >> 8);
                buf[i * 2 + 4] = (char)(d & 0xFF);
            }
            buf[1033] = '\0';
            buf[1034] = 'e';
            buf[1035] = 'n';
            buf[1036] = 'd';
*/
            Point[] p = new Point[length];
            for (int i = 0; i < length; i++ )
            {
                p[i].X = (int)(((float)(e.ClipRectangle.Size.Width - 1 ) / (float)(length)) * (float)i)+1;
                p[i].Y = (int)(((float)(e.ClipRectangle.Size.Height - 2) / (float)1024) * (float)(1023 - (int)(BitConverter.ToInt16(buf, i*2)))) + 1;
//                p[i].Y = (int)(((float)(e.ClipRectangle.Size.Height - 2) / (float)1024) * (float)(1023 - (int)((buf[i * 2 + 1] << 8) + buf[i * 2]))) + 1;
//                p[i].Y = (int)(((float)(e.ClipRectangle.Size.Height - 2) / (float)1024) * (float)(1023 - (1024 / length * i))) + 1;//диагональная линия
            }

            Pen pen = new Pen(Color.FromArgb(255, 0, 0, 0), 2);
            pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
            e.Graphics.DrawRectangle(pen, 1, 1, e.ClipRectangle.Width - 2, e.ClipRectangle.Height - 2);
            int s;
            for (int i = 1; i < 14; i++)
            {
                s = (int)((float)(e.ClipRectangle.Size.Width-2) / 14.0 * (float)i)+1;
                e.Graphics.DrawLine(System.Drawing.Pens.Gray, s, 0, s, e.ClipRectangle.Size.Height);
            }
            s = e.ClipRectangle.Size.Width / 2;
            e.Graphics.DrawLine(pen, s, 0, s, e.ClipRectangle.Size.Height);

            for (int i = 1; i < 10; i++)
            {
                s = (int)((float)(e.ClipRectangle.Size.Height - 2) / 10.0 * (float)i) + 1;
                e.Graphics.DrawLine(System.Drawing.Pens.Gray, 0, s, e.ClipRectangle.Size.Width, s);
            }
            s = e.ClipRectangle.Size.Height / 2;
            e.Graphics.DrawLine(pen, 0, s, e.ClipRectangle.Size.Width, s);

            Pen pen2 = new Pen(Color.FromArgb(255, 255, 0, 0), 2);
//            Pen pen3 = new Pen(Color.FromArgb(255, 0, 255, 0), 1);
            if (length != 0)
            {
                e.Graphics.DrawLines(pen2, p);
//                e.Graphics.DrawLines(pen3, p);
            }

            int SizeF = e.ClipRectangle.Size.Width / 50;
            Font drawFont = new Font("Arial", SizeF);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            e.Graphics.DrawString(String.Format("{0}", TrigerPosS), drawFont, System.Drawing.Brushes.Black, 2, 3);
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
//            if (!bPortOpen)
            if (!serialPort1.IsOpen)
                return;

            while (serialPort1.BytesToRead > 0)
            {
                if (!serialPort1.IsOpen)
                    return;
                if (state == 0)
                {
                    int data = serialPort1.ReadByte();
                    if (data == arbeg[pos])
                    {
                        pos++;
                        if (pos == 6)
                        {
                            state = 1;
                            lengthr = 0;
                        }
                    }
                    else
                    {
                        pos = 0;
                    }
                }

                if (state == 1)
                {
                    int l = serialPort1.BytesToRead;
                    if (l > 1)
                    {
                        int data1 = serialPort1.ReadByte();
                        int data2 = serialPort1.ReadByte();
                        lengthr = (ushort)((data2 << 8) + data1);
                        state = 2;
                        pos = 0;
                    }
                }

                if (state == 2)
                {
                    int l = serialPort1.BytesToRead;
                    if ((l+pos) > (lengthr*2))
                        l = (lengthr*2) - pos;
                    if (l > 0)
                    {
                        serialPort1.Read(buf0, 0, l);
                        for (int i = pos; i < pos + l; i++)
                        {
                            buf[i] = buf0[i-pos];
                        }
                        pos = (ushort)(pos + l);
                        if (pos >= (lengthr*2))
                        {
                            pos = 0;
                            state = 3;
                        }
                    }
                }

                if (state == 3)
                {
                    int l = serialPort1.BytesToRead;
                    if (l > 1)
                    {
                        int data1 = serialPort1.ReadByte();
                        int data2 = serialPort1.ReadByte();
                        TrigerPosS = (ushort)((data2 << 8) + data1);
                        state = 0;
                        pos = 0;

                        byte[] buf3 = new byte[1024];

                        Marshal.Copy(buf, TrigerPosS, new IntPtr(), lengthr - TrigerPosS);
                        Marshal.Copy(buf, TrigerPosS, new IntPtr(), lengthr - TrigerPosS);

                        NewData = true;
                        if (!serialPort1.IsOpen)
                            return;
                    }
                }

                if (!serialPort1.IsOpen)
                    return;
            }
        }

        private void comboBox_ports_SelectionChangeCommitted(object sender, EventArgs e)
        {
            sPort = comboBox_ports.SelectedItem.ToString();
        }

        private void Open()
        {
            if (SerialPort.GetPortNames().Length > 0)
            {
//                sPort = comboBox_ports.SelectedItem.ToString();
                serialPort1.PortName = comboBox_ports.SelectedItem.ToString(); //sPort;
                string sBaud = comboBox_baud.SelectedItem.ToString();
                serialPort1.BaudRate = int.Parse(sBaud);
                serialPort1.DataBits = 8;
                serialPort1.StopBits = StopBits.Two;
                serialPort1.ReadTimeout = 100;
                serialPort1.Open();
                bPortOpen = true;
                button1.Text = "Close";
            }
            for (int i = 0; i < length; i++)
            {
                buf[i * 2] = 0;
                buf[i * 2 + 1] = 2;
            }
            Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!bPortOpen)
            {
                Open();
            }
            else
            {
                serialPort1.Close();
                bPortOpen = false;
                button1.Text = "Open";
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            pictureBox1.SetBounds(pictureBox1.Bounds.Left, pictureBox1.Bounds.Top, this.Size.Width - (pictureBox1.Bounds.Left*3), Bottom - Top - 110);
            Refresh();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (NewData)
            {
                Refresh();
                NewData = false;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (bPortOpen)
            {
                serialPort1.Close();
                bPortOpen = false;
                button1.Text = "Open";
            }
            RegSave();
        }

        private void RegLoad()
        {
            RegistryKey RegKey = Registry.CurrentUser.CreateSubKey("software\\NanoScope");
            this.SetDesktopBounds(int.Parse((string)RegKey.GetValue("X1", "0")), int.Parse((string)RegKey.GetValue("Y1", "0")), int.Parse((string)RegKey.GetValue("W", "640")), int.Parse((string)RegKey.GetValue("H", "480")));
            string str = (string)RegKey.GetValue("State", "Normal");
            if(str == "Maximized")
                WindowState = FormWindowState.Maximized;
            string sPort = (string)RegKey.GetValue("PORT");
            comboBox_baud.Text = (string)RegKey.GetValue("BAUD", "9600");
            int i = -1;
            if(sPort != null)
                i = comboBox_ports.Items.IndexOf(sPort);
            if (i >= 0)
            {
                comboBox_ports.Text = sPort;
                Open();
            }
            RegKey.Close();
        }

        private void RegSave()
        {
            RegistryKey RegKey = Registry.CurrentUser.CreateSubKey("software\\NanoScope");
            if (WindowState != FormWindowState.Maximized)
            {
                RegKey.SetValue("X1", string.Format("{0}", this.Bounds.X));
                RegKey.SetValue("Y1", string.Format("{0}", this.Bounds.Y));
                RegKey.SetValue("W", string.Format("{0}", this.Bounds.Width));
                RegKey.SetValue("H", string.Format("{0}", this.Bounds.Height));
            }
            RegKey.SetValue("State", WindowState);
            RegKey.SetValue("PORT", comboBox_ports.Text);
            RegKey.SetValue("BAUD", comboBox_baud.Text);
            RegKey.Close();
        }


        public unsafe static void memcpy(void* dst, void* src, int count)
        {
            const int blockSize = 4096;
            byte[] block = new byte[blockSize];
            byte* d = (byte*)dst, s = (byte*)src;
            for (int i = 0, step; i < count; i += step, d += step, s += step)
            {
                step = count - i;
                if (step > blockSize)
                {
                    step = blockSize;
                }
                Marshal.Copy(new IntPtr(s), block, 0, step);
                Marshal.Copy(block, 0, new IntPtr(d), step);
            }
        }
    }
}
