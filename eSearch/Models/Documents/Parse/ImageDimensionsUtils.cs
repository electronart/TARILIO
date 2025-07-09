
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    public static class ImageDimensionsUtils
    {
        public static bool TryGetImageDimensions(string filePath, string parseAsExtension, out Tuple<uint, uint> pixelWidthHeight)
        {
            try
            {
                //pixelWidthHeight = new Tuple<uint, uint>(10, 10);
                //return true; // temp - testing;

                var info = new MagickImageInfo(filePath);
                pixelWidthHeight = new Tuple<uint, uint>(info.Width, info.Height);
                return true;
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                pixelWidthHeight = new Tuple<uint, uint>(0, 0);
                return false;
            }
        }
    }
}
