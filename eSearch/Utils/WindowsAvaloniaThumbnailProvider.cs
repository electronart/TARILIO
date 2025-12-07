using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace eSearch
{
    public static class WindowsAvaloniaThumbnailProvider
    {
        /// <summary>
        /// Since this can hit the disk cache, be careful about where this is called.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Avalonia.Media.Imaging.Bitmap GetMediumThumbnail(string filePath)
        {
            using (var bitmapTmp = ShellThumbs.WindowsThumbnailProvider.GetThumbnail(filePath, 96, 96, ShellThumbs.ThumbnailOptions.BiggerSizeOk | ShellThumbs.ThumbnailOptions.Win8WideThumbnails))
            {
                var bitmapdata = bitmapTmp.LockBits(new Rectangle(0, 0, bitmapTmp.Width, bitmapTmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                Avalonia.Media.Imaging.Bitmap bitmap1 = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Unpremul,
                    bitmapdata.Scan0,
                    new Avalonia.PixelSize(bitmapdata.Width, bitmapdata.Height),
                    new Avalonia.Vector(96, 96),
                    bitmapdata.Stride);
                bitmapTmp.UnlockBits(bitmapdata);
                bitmapTmp.Dispose();
                return bitmap1;
            }
        }
    }
    
}