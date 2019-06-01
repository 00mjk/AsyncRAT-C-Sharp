﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace AsyncRAT_Sharp.Handle_Packet
{
    public class HandleLogs
    {
        public void Addmsg(string Msg, Color color)
        {
            try
            {
                if (Program.form1.listView2.InvokeRequired)
                {
                    Program.form1.listView2.BeginInvoke((MethodInvoker)(() =>
                    {
                        ListViewItem LV = new ListViewItem();
                        LV.Text = DateTime.Now.ToLongTimeString();
                        LV.SubItems.Add(Msg);
                        LV.ForeColor = color;
                        lock (Settings.Listview2Lock)
                        {
                            Program.form1.listView2.Items.Insert(0, LV);
                        }
                    }));
                }
                else
                {
                    ListViewItem LV = new ListViewItem();
                    LV.Text = DateTime.Now.ToLongTimeString();
                    LV.SubItems.Add(Msg);
                    LV.ForeColor = color;
                    Program.form1.listView2.Items.Insert(0, LV);
                }
            }
            catch { }
        }
    }
}