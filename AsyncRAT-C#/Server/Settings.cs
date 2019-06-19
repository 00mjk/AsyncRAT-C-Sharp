﻿using Server.Cryptography;
using Server.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace Server
{
    public static class Settings
    {
        public static List<Clients> Online = new List<Clients>();
        public static List<string> Blocked = new List<string>();
        public static long Sent { get; set; }
        public static long Received { get; set; }

        public static string CertificatePath = Application.StartupPath + "\\ServerCertificate.p12";
        public static X509Certificate2 ServerCertificate;
        public static readonly string Version = "AsyncRAT 0.5.1";
        public static object Listview1Lock = new object();
        public static object Listview2Lock = new object();
        public static object Listview3Lock = new object();
    }
}
