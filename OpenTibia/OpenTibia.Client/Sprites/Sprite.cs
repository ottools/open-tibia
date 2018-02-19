#region Licence
/**
* Copyright (C) 2015 Open Tibia Tools <https://github.com/ottools/open-tibia>
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
#endregion

#region Using Statements
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
#endregion

namespace OpenTibia.Client.Sprites
{
    public class Sprite
    {
        #region Constants

        public const byte DefaultSize = 32;
        public const ushort PixelsDataSize = 4096; // 32*32*4

        #endregion

        #region Private Properties

        private bool transparent = false;
        private Bitmap bitmap = null;

        #endregion

        #region Contructor

        public Sprite(uint id, bool transparent)
        {
            this.ID = id;
            this.Transparent = transparent;
        }

        public Sprite(uint id) : this(id, false)
        {
            ////
        }

        public Sprite() : this(0, false)
        {
            ////
        }

        #endregion

        #region Public Properties

        public uint ID { get; set; }

        public byte[] CompressedPixels { get; internal set; }

        public int Length
        {
            get { return this.CompressedPixels == null ? 0 : this.CompressedPixels.Length; }
        }

        public bool Transparent
        {
            get
            {
                return this.transparent;
            }

            set
            {
                if (this.transparent != value)
                {
                    if (this.Length != 0)
                    {
                        byte[] pixels = UncompressPixelsBGRA(this.CompressedPixels, this.transparent);

                        this.transparent = value;
                        this.CompressedPixels = CompressPixelsBGRA(pixels, this.transparent);
                        this.bitmap = null;
                    }
                    else
                    {
                        this.transparent = value;
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return this.ID.ToString();
        }

        /// <summary>
        /// Used for C# Bitmap
        /// </summary>
        /// <returns></returns>
        public byte[] GetBGRAPixels()
        {
            byte[] bytes = this.CompressedPixels != null ? this.CompressedPixels : EmptyArray;
            return UncompressPixelsBGRA(bytes, this.transparent);
        }

        /// <summary>
        /// Used for AS3 Bitmap
        /// </summary>
        /// <returns></returns>
        public byte[] GetARGBPixels()
        {
            byte[] bytes = this.CompressedPixels != null ? this.CompressedPixels : EmptyArray;
            return UncompressPixelsARGB(bytes, this.transparent);
        }

        /// <summary>
        /// Used for C# bitmaps.
        /// </summary>
        /// <param name="pixels"></param>
        public void SetPixelsBGRA(byte[] pixels)
        {
            if (pixels != null && pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid sprite pixels length");
            }

            this.CompressedPixels = CompressPixelsBGRA(pixels, this.transparent);
            this.bitmap = null;
        }

        /// <summary>
        /// Used for AS3 bitmaps.
        /// </summary>
        /// <param name="pixels"></param>
        public void SetPixelsARGB(byte[] pixels)
        {
            if (pixels != null && pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid sprite pixels length");
            }

            this.CompressedPixels = CompressPixelsARGB(pixels, this.transparent);
            this.bitmap = null;
        }

        public Bitmap GetBitmap()
        {
            if (this.bitmap != null)
            {
                return this.bitmap;
            }

            this.bitmap = new Bitmap(DefaultSize, DefaultSize, PixelFormat.Format32bppArgb);

            byte[] pixels = this.GetBGRAPixels();
            if (pixels != null)
            {
                BitmapData bitmapData = this.bitmap.LockBits(Rectangle, ImageLockMode.ReadWrite, this.bitmap.PixelFormat);
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                this.bitmap.UnlockBits(bitmapData);
            }

            return this.bitmap;
        }

        public void SetBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            if (bitmap.Width != DefaultSize || bitmap.Height != DefaultSize)
            {
                throw new ArgumentException("Invalid bitmap size", nameof(bitmap));
            }

            this.CompressedPixels = CompressBitmap(bitmap, this.transparent);
            this.bitmap = bitmap;
        }

        public Sprite Clone()
        {
            Sprite clone = new Sprite(this.ID, this.transparent);
            clone.CompressedPixels = this.CompressedPixels != null ? (byte[])this.CompressedPixels.Clone() : null;
            return clone;
        }

        #endregion

        #region Class Properties

        private static readonly byte[] EmptyArray = new byte[0];

        #endregion

        #region Class Methods

        public static byte[] CompressBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            BitmapData bitmapData = bitmap.LockBits(Rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[PixelsDataSize];
            Marshal.Copy(bitmapData.Scan0, pixels, 0, PixelsDataSize);
            bitmap.UnlockBits(bitmapData);

            return CompressPixelsBGRA(pixels, false);
        }

        public static byte[] CompressBitmap(Bitmap bitmap, bool transparent)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            BitmapData bitmapData = bitmap.LockBits(Rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[PixelsDataSize];
            Marshal.Copy(bitmapData.Scan0, pixels, 0, PixelsDataSize);
            bitmap.UnlockBits(bitmapData);

            return CompressPixelsBGRA(pixels, transparent);
        }

        public static byte[] CompressPixelsBGRA(byte[] pixels, bool transparent)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid pixels data size");
            }

            byte[] compressedPixels;

            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                int read = 0;
                int alphaCount = 0;
                ushort chunkSize = 0;
                long coloredPos = 0;
                long finishOffset = 0;
                int length = pixels.Length / 4;
                int index = 0;

                while (index < length)
                {
                    chunkSize = 0;

                    // Read transparent pixels
                    while (index < length)
                    {
                        read = (index * 4) + 3;

                        // alpha
                        if (pixels[read++] != 0)
                        {
                            break;
                        }

                        alphaCount++;
                        chunkSize++;
                        index++;
                    }

                    // Read colored pixels
                    if (alphaCount < length && index < length)
                    {
                        writer.Write(chunkSize); // Writes the length of the transparent pixels
                        coloredPos = writer.BaseStream.Position; // Save colored position 
                        writer.BaseStream.Seek(2, SeekOrigin.Current); // Skip colored position
                        chunkSize = 0;

                        while (index < length)
                        {
                            read = index * 4;

                            byte blue = pixels[read++];
                            byte green = pixels[read++];
                            byte red = pixels[read++];
                            byte alpha = pixels[read++];

                            if (alpha == 0)
                            {
                                break;
                            }

                            writer.Write(red);
                            writer.Write(green);
                            writer.Write(blue);

                            if (transparent)
                            {
                                writer.Write(alpha);
                            }

                            chunkSize++;
                            index++;
                        }

                        finishOffset = writer.BaseStream.Position;
                        writer.BaseStream.Seek(coloredPos, SeekOrigin.Begin); // Go back to chunksize indicator
                        writer.Write(chunkSize); // Writes the length of he colored pixels
                        writer.BaseStream.Seek(finishOffset, SeekOrigin.Begin);
                    }
                }

                compressedPixels = ((MemoryStream)writer.BaseStream).ToArray();
            }

            return compressedPixels;
        }

        public static byte[] CompressPixelsBGRA(byte[] pixels)
        {
            return CompressPixelsBGRA(pixels, false);
        }

        public static byte[] CompressPixelsARGB(byte[] pixels, bool transparent)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid pixels data size");
            }

            byte[] compressedPixels;

            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                int read = 0;
                int alphaCount = 0;
                ushort chunkSize = 0;
                long coloredPos = 0;
                long finishOffset = 0;
                int length = pixels.Length / 4;
                int index = 0;

                while (index < length)
                {
                    chunkSize = 0;

                    // Read transparent pixels
                    while (index < length)
                    {
                        read = index * 4;

                        byte alpha = pixels[read++];
                        read += 3;

                        if (alpha != 0)
                        {
                            break;
                        }

                        alphaCount++;
                        chunkSize++;
                        index++;
                    }

                    // Read colored pixels
                    if (alphaCount < length && index < length)
                    {
                        writer.Write(chunkSize); // Writes the length of the transparent pixels
                        coloredPos = writer.BaseStream.Position; // Save colored position 
                        writer.BaseStream.Seek(2, SeekOrigin.Current); // Skip colored position
                        chunkSize = 0;

                        while (index < length)
                        {
                            read = index * 4;

                            byte alpha = pixels[read++];
                            byte red = pixels[read++];
                            byte green = pixels[read++];
                            byte blue = pixels[read++];

                            if (alpha == 0)
                            {
                                break;
                            }

                            writer.Write(red);
                            writer.Write(green);
                            writer.Write(blue);

                            if (transparent)
                            {
                                writer.Write(alpha);
                            }

                            chunkSize++;
                            index++;
                        }

                        finishOffset = writer.BaseStream.Position;
                        writer.BaseStream.Seek(coloredPos, SeekOrigin.Begin); // Go back to chunksize indicator
                        writer.Write(chunkSize); // Writes the length of he colored pixels
                        writer.BaseStream.Seek(finishOffset, SeekOrigin.Begin);
                    }
                }

                compressedPixels = ((MemoryStream)writer.BaseStream).ToArray();
            }

            return compressedPixels;
        }

        public static byte[] CompressPixelsARGB(byte[] pixels)
        {
            return CompressPixelsARGB(pixels, false);
        }

        public static Bitmap UncompressBitmap(byte[] compressedPixels)
        {
            return UncompressBitmap(compressedPixels, false);
        }

        public static Bitmap UncompressBitmap(byte[] compressedPixels, bool transparent)
        {
            if (compressedPixels == null)
            {
                throw new ArgumentNullException(nameof(compressedPixels));
            }

            Bitmap bitmap = new Bitmap(DefaultSize, DefaultSize, PixelFormat.Format32bppArgb);
            byte[] pixels = UncompressPixelsBGRA(compressedPixels, transparent);
            BitmapData bitmapData = bitmap.LockBits(Rectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(pixels, 0, bitmapData.Scan0, PixelsDataSize);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        /// <summary>
        /// Used for C# Bitmap
        /// </summary>
        /// <param name="compressedPixels"></param>
        /// <param name="transparent"></param>
        /// <returns></returns>
        public static byte[] UncompressPixelsBGRA(byte[] compressedPixels, bool transparent)
        {
            if (compressedPixels == null)
            {
                throw new ArgumentNullException(nameof(compressedPixels));
            }

            int read = 0;
            int write = 0;
            int pos = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = compressedPixels.Length;
            byte bitPerPixel = (byte)(transparent ? 4 : 3);
            byte[] pixels = new byte[PixelsDataSize];

            for (read = 0; read < length; read += 4 + (bitPerPixel * coloredPixels))
            {
                transparentPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;
                coloredPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;

                for (int i = 0; i < transparentPixels; i++)
                {
                    pixels[write++] = 0x00; // Blue
                    pixels[write++] = 0x00; // Green
                    pixels[write++] = 0x00; // Red
                    pixels[write++] = 0x00; // Alpha
                }

                for (int i = 0; i < coloredPixels; i++)
                {
                    byte red = compressedPixels[pos++];
                    byte green = compressedPixels[pos++];
                    byte blue = compressedPixels[pos++];
                    byte alpha = transparent ? compressedPixels[pos++] : (byte)0xFF;

                    pixels[write++] = blue;
                    pixels[write++] = green;
                    pixels[write++] = red;
                    pixels[write++] = alpha;
                }
            }

            // Fills the remaining pixels
            while (write < PixelsDataSize)
            {
                pixels[write++] = 0x00; // Blue
                pixels[write++] = 0x00; // Green
                pixels[write++] = 0x00; // Red
                pixels[write++] = 0x00; // Alpha
            }

            return pixels;
        }

        /// <summary>
        /// Used for c# bitmap.
        /// </summary>
        /// <param name="compressedPixels"></param>
        /// <returns></returns>
        public static byte[] UncompressPixelsBGRA(byte[] compressedPixels)
        {
            return UncompressPixelsBGRA(compressedPixels, false);
        }

        /// <summary>
        /// Used for AS3 Bitmap
        /// </summary>
        /// <param name="compressedPixels"></param>
        /// <param name="transparent"></param>
        /// <returns></returns>
        public static byte[] UncompressPixelsARGB(byte[] compressedPixels, bool transparent)
        {
            if (compressedPixels == null)
            {
                throw new ArgumentNullException(nameof(compressedPixels));
            }

            int read = 0;
            int write = 0;
            int pos = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = compressedPixels.Length;
            byte bitPerPixel = (byte)(transparent ? 4 : 3);
            byte[] pixels = new byte[PixelsDataSize];

            for (read = 0; read < length; read += 4 + (bitPerPixel * coloredPixels))
            {
                transparentPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;
                coloredPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;

                for (int i = 0; i < transparentPixels; i++)
                {
                    pixels[write++] = 0x00; // alpha
                    pixels[write++] = 0x00; // red
                    pixels[write++] = 0x00; // green
                    pixels[write++] = 0x00; // blue
                }

                for (int i = 0; i < coloredPixels; i++)
                {
                    byte red = compressedPixels[pos++];
                    byte green = compressedPixels[pos++];
                    byte blue = compressedPixels[pos++];
                    byte alpha = transparent ? compressedPixels[pos++] : (byte)0xFF;

                    pixels[write++] = alpha;
                    pixels[write++] = red;
                    pixels[write++] = green;
                    pixels[write++] = blue;
                }
            }

            // Fills the remaining pixels
            while (write < PixelsDataSize)
            {
                pixels[write++] = 0x00; // alpha
                pixels[write++] = 0x00; // red
                pixels[write++] = 0x00; // green
                pixels[write++] = 0x00; // blue
            }

            return pixels;
        }

        /// <summary>
        /// Used for AS3 bitmap.
        /// </summary>
        /// <param name="compressedPixels"></param>
        /// <returns></returns>
        public static byte[] UncompressPixelsARGB(byte[] compressedPixels)
        {
            return UncompressPixelsARGB(compressedPixels, false);
        }

        #endregion

        #region Class Properties

        public static readonly Rectangle Rectangle = new Rectangle(0, 0, DefaultSize, DefaultSize);

        #endregion
    }
}
