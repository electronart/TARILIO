using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Documents.Parse
{
    public static class ImageDimensionsUtils
    {
        public static bool TryGetImageDimensions(string filePath, string parseAsExtension, out Tuple<uint, uint> pixelWidthHeight)
        {
            pixelWidthHeight = new Tuple<uint, uint>(0, 0);

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    // Auto-detect format via magic bytes, or use extension hint
                    string format = string.IsNullOrEmpty(parseAsExtension) ? DetectFormat(br) : parseAsExtension.ToLowerInvariant().TrimStart('.');

                    uint width = 0;
                    uint height = 0;

                    switch (format)
                    {
                        case "jpg":
                        case "jpeg":
                            if (GetJpegDimensions(br, out width, out height)) return SetAndReturn(ref pixelWidthHeight, width, height);
                            break;
                        case "png":
                            if (GetPngDimensions(br, out width, out height)) return SetAndReturn(ref pixelWidthHeight, width, height);
                            break;
                        case "gif":
                            if (GetGifDimensions(br, out width, out height)) return SetAndReturn(ref pixelWidthHeight, width, height);
                            break;
                        case "bmp":
                            if (GetBmpDimensions(br, out width, out height)) return SetAndReturn(ref pixelWidthHeight, width, height);
                            break;
                    }

                    var info = new MagickImageInfo(filePath);
                    return SetAndReturn(ref pixelWidthHeight, (uint)info.Width, (uint)info.Height);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return false;
        }

        private static bool SetAndReturn(ref Tuple<uint, uint> pixelWidthHeight, uint width, uint height)
        {
            pixelWidthHeight = new Tuple<uint, uint>(width, height);
            return true;
        }

        private static string DetectFormat(BinaryReader br)
        {
            br.BaseStream.Position = 0;
            byte[] header = br.ReadBytes(8);

            if (header[0] == 0xFF && header[1] == 0xD8) return "jpg";
            if (header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "png";
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46) return "gif";
            if (header[0] == 0x42 && header[1] == 0x4D) return "bmp";
            // Add more detections, e.g., WebP: if header starts with "RIFF" then "WEBP"

            return string.Empty;
        }

        private static bool GetJpegDimensions(BinaryReader br, out uint width, out uint height)
        {
            width = height = 0;
            br.BaseStream.Position = 0;

            if (br.ReadUInt16() != 0xD8FF) return false; // Big-endian for 0xFFD8 SOI marker

            while (true)
            {
                ushort marker = br.ReadUInt16();
                if (marker == 0xD9FF) return false; // EOI, no dimensions found

                ushort len = br.ReadUInt16(); // Big-endian
                if (marker >= 0xC0FF && marker <= 0xCFFF && marker != 0xC4FF && marker != 0xC8FF && marker != 0xCCFF) // SOF markers
                {
                    br.ReadByte(); // Precision
                    height = SwapEndian(br.ReadUInt16());
                    width = SwapEndian(br.ReadUInt16());
                    return true;
                }
                br.BaseStream.Position += len - 2; // Skip segment
            }
        }

        private static bool GetPngDimensions(BinaryReader br, out uint width, out uint height)
        {
            width = height = 0;
            br.BaseStream.Position = 0;

            if (br.ReadUInt64() != 0x0A1A0A0D474E5089) return false; // PNG signature (big-endian)

            br.BaseStream.Position = 16; // Skip length + IHDR
            width = SwapEndian(br.ReadUInt32());
            height = SwapEndian(br.ReadUInt32());
            return true;
        }

        private static bool GetGifDimensions(BinaryReader br, out uint width, out uint height)
        {
            width = height = 0;
            br.BaseStream.Position = 0;

            if (br.ReadUInt32() != 0x38464947 && br.ReadUInt32() != 0x39464947) return false; // GIF87a or GIF89a

            br.BaseStream.Position = 6;
            width = br.ReadUInt16(); // Little-endian
            height = br.ReadUInt16();
            return true;
        }

        private static bool GetBmpDimensions(BinaryReader br, out uint width, out uint height)
        {
            width = height = 0;
            br.BaseStream.Position = 0;

            if (br.ReadUInt16() != 0x4D42) return false; // BM signature

            br.BaseStream.Position = 18;
            width = br.ReadUInt32(); // Little-endian
            height = br.ReadUInt32();
            return true;
        }

        private static uint SwapEndian(uint value) => ((value & 0x000000FF) << 24) | ((value & 0x0000FF00) << 8) | ((value & 0x00FF0000) >> 8) | ((value & 0xFF000000) >> 24);
        private static ushort SwapEndian(ushort value) => (ushort)(((value & 0x00FF) << 8) | ((value & 0xFF00) >> 8));
    }
}