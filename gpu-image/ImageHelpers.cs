// Basic code from:
// http://stackoverflow.com/questions/111345/getting-image-dimensions-without-reading-the-entire-file
// With updates at end from: 
// http://www.codeproject.com/KB/cs/ReadingImageHeaders.aspx?msg=3029709#xx3029709xx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace Images {
    /// <summary>
    /// This class contains GetDimensions() method for getting size of an image without reading/loading it all.
    /// </summary>
    public static class ImageHelpers {

        const string errorMessage = "Could not recognise image format.";

        private static Dictionary<byte[], Func<BinaryReader, Size>> imageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, Size>>()
        {
            { new byte[]{ 0x42, 0x4D }, DecodeBitmap},
            { new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, DecodeGif },
            { new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, DecodeGif },
            { new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng },
            { new byte[]{ 0xff, 0xd8 }, DecodeJfif },
        };

        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>    
        private static Size GetDimensions(BinaryReader binaryReader) {
            int maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;

            byte[] magicBytes = new byte[maxMagicBytesLength];

            for (int i = 0; i < maxMagicBytesLength; i += 1) {
                magicBytes[i] = binaryReader.ReadByte();

                foreach (var kvPair in imageFormatDecoders) {
                    if (magicBytes.StartsWith(kvPair.Key)) {
                        return kvPair.Value(binaryReader);
                    }
                }
            }
            throw new ArgumentException(errorMessage, "binaryReader");
        }

        private static bool StartsWith(this byte[] thisBytes, byte[] thatBytes) {
            for (int i = 0; i < thatBytes.Length; i += 1) {
                if (thisBytes[i] != thatBytes[i]) {
                    return false;
                }
            }
            return true;
        }

        private static short ReadLittleEndianInt16(this BinaryReader binaryReader) {
            byte[] bytes = new byte[sizeof(short)];
            for (int i = 0; i < sizeof(short); i += 1) {
                bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt16(bytes, 0);
        }

        private static int ReadLittleEndianInt32(this BinaryReader binaryReader) {
            byte[] bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i += 1) {
                bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        private static Size DecodeBitmap(BinaryReader binaryReader) {
            binaryReader.ReadBytes(16);
            int width = binaryReader.ReadInt32();
            int height = binaryReader.ReadInt32();
            return new Size(width, height);
        }

        private static Size DecodeGif(BinaryReader binaryReader) {
            int width = binaryReader.ReadInt16();
            int height = binaryReader.ReadInt16();
            return new Size(width, height);
        }

        private static Size DecodePng(BinaryReader binaryReader) {
            binaryReader.ReadBytes(8);
            int width = binaryReader.ReadLittleEndianInt32();
            int height = binaryReader.ReadLittleEndianInt32();
            return new Size(width, height);
        }

        //private static Size DecodeJfif(BinaryReader binaryReader) {
        //    while (binaryReader.ReadByte() == 0xff) {
        //        byte marker = binaryReader.ReadByte();
        //        short chunkLength = binaryReader.ReadLittleEndianInt16();

        //        if (marker == 0xc0) {
        //            binaryReader.ReadByte();

        //            int height = binaryReader.ReadLittleEndianInt16();
        //            int width = binaryReader.ReadLittleEndianInt16();
        //            return new Size(width, height);
        //        }

        //        binaryReader.ReadBytes(chunkLength - 2);
        //    }

        //    throw new ArgumentException(errorMessage);
        //}


        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>
        public static Size GetDimensions(string path) {
            try {
                using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path))) {
                    try {
                        return GetDimensions(binaryReader);
                    } catch (ArgumentException e) {
                        string newMessage = string.Format("{0} file: '{1}' ", errorMessage, path);

                        throw new ArgumentException(newMessage, "path", e);
                    }
                }
            } catch (ArgumentException) {
                //do it the old fashioned way

                using (Bitmap b = new Bitmap(path)) {
                    return b.Size;
                }
            }
        }

        private static Size DecodeJfif(BinaryReader binaryReader) {
            while (binaryReader.ReadByte() == 0xff) {
                byte marker = binaryReader.ReadByte();
                short chunkLength = ReadLittleEndianInt16(binaryReader);
                if (marker == 0xc0) {
                    binaryReader.ReadByte();
                    int height = ReadLittleEndianInt16(binaryReader);
                    int width = ReadLittleEndianInt16(binaryReader);
                    return new Size(width, height);
                }

                if (chunkLength < 0) {
                    ushort uchunkLength = (ushort)chunkLength;
                    binaryReader.ReadBytes(uchunkLength - 2);
                } else {
                    binaryReader.ReadBytes(chunkLength - 2);
                }
            }

            throw new ArgumentException(errorMessage);
        }

        //public void ShowTest(Image im2, float i) {
        //    float r = MathHelper.ToRadians(i);
        //    form1.XTran = new Matrix[] { Matrix.CreateRotationZ(r), Matrix.CreateRotationZ(-r) };
        //    form1.image2 = im2;
        //    form1.Xparms = new float[] { 1, 1, 1, 1, 1 };
        //    form1.Xtechnique = "tr2";
        //    Show();
        //}


    }
}
