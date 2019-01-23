﻿using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
using AsyncRAT_Sharp.Handle_Packet;
using Microsoft.VisualBasic;
using System.Diagnostics;

namespace AsyncRAT_Sharp.Sockets
{
    class Clients
    {
        public Socket client;
        public byte[] Buffer;
        public long Buffersize;
        public bool BufferRecevied;
        public MemoryStream MS;
        public ListViewItem LV;
        public event ReadEventHandler Read;
        public delegate void ReadEventHandler(Clients client, byte[] data);

        public void InitializeClient(Socket CLIENT)
        {
            client = CLIENT;
            client.ReceiveBufferSize = 50 * 1024;
            client.SendBufferSize = 50 * 1024;
            client.ReceiveTimeout = -1;
            client.SendTimeout = -1;
            Buffer = new byte[1];
            Buffersize = 0;
            BufferRecevied = false;
            MS = new MemoryStream();
            LV = null;
            Read += HandlePacket.Read;
            client.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, ReadClientData, null);

        }

        public async void ReadClientData(IAsyncResult ar)
        {

            try
            {
                int Recevied = client.EndReceive(ar);

                if (Recevied > 0)
                {

                    if (BufferRecevied == false)
                    {
                        if (Buffer[0] == 0)
                        {
                            Buffersize = Convert.ToInt64(Encoding.UTF8.GetString(MS.ToArray()));
                            MS.Dispose();
                            MS = new MemoryStream();
                            if (Buffersize > 0)
                            {
                                Buffer = new byte[Buffersize - 1];
                                BufferRecevied = true;
                            }
                        }
                        else
                        {
                            await MS.WriteAsync(Buffer, 0, Buffer.Length);
                        }
                    }
                    else
                    {
                        await MS.WriteAsync(Buffer, 0, Recevied);
                        if (MS.Length == Buffersize)
                        {

                            Read?.BeginInvoke(this, MS.ToArray(), null, null);
                            Buffer = new byte[1];
                            Buffersize = 0;
                            BufferRecevied = false;
                            MS.Dispose();
                            MS = new MemoryStream();
                        }
                    }
                }
                else
                {
                    Disconnected();
                }
                client.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, ReadClientData, null);
            }
            catch
            {
                Disconnected();
            }
        }

        delegate void _isDisconnected();
        public void Disconnected()
        {
            if (Program.form1.listView1.InvokeRequired)
                Program.form1.listView1.Invoke(new _isDisconnected(Disconnected));
            else
            {
                LV.Remove();
            }
            try
            {
                MS.Dispose();
                client.Close();
                client.Dispose();
            }
            catch { }
        }

        public async void BeginSend(byte[] Msgs)
        {
            if (client.Connected)
            {
                try
                {
                    using (MemoryStream MS = new MemoryStream())
                    {
                        byte[] buffer = Msgs;
                        byte[] buffersize = Encoding.UTF8.GetBytes(buffer.Length.ToString() + Strings.ChrW(0));
                        await MS.WriteAsync(buffersize, 0, buffersize.Length);
                        await MS.WriteAsync(buffer, 0, buffer.Length);

                        client.Poll(-1, SelectMode.SelectWrite);
                        client.BeginSend(MS.ToArray(), 0, Convert.ToInt32(MS.Length), SocketFlags.None, new AsyncCallback(EndSend), null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("BeginSend " + ex.Message);
                    Disconnected();
                }
            }
        }

        public void EndSend(IAsyncResult ar)
        {
            try
            {
                client.EndSend(ar);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EndSend " + ex.Message);
                Disconnected();
            }
        }
    }
}
