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
using OpenTibia.Utils;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
#endregion

namespace OpenTibia.Utilities
{
    public class BitmapLocker : IDisposable
    {
        #region Private Properties

        private Bitmap bitmap;
        private BitmapData bitmapData;
        private IntPtr address = IntPtr.Zero;

        #endregion

        #region Constructor

        public BitmapLocker(Bitmap bitmap)
        {
            this.bitmap = bitmap;
            this.Width = bitmap.Width;
            this.Height = bitmap.Height;
            this.Length = bitmap.Width * bitmap.Height * 4;
            this.Pixels = new byte[this.Length];
        }

        #endregion

        #region Public Properties

        public byte[] Pixels { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Length { get; private set; }

        #endregion

        #region Public Methods

        public void LockBits()
        {
            // create rectangle to lock
            Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);

            // lock bitmap and return bitmap data
            this.bitmapData = this.bitmap.LockBits(rect, ImageLockMode.ReadWrite, this.bitmap.PixelFormat);

            // gets the address of the first pixel data in the bitmap
            this.address = this.bitmapData.Scan0;

            // copy data from pointer to array
            Marshal.Copy(this.address, this.Pixels, 0, this.Length);
        }

        public void CopyPixels(Bitmap source, int px, int py)
        {
            int width = source.Width;
            int height = source.Height;
            int maxIndex = this.Length - 4;

            for (int y = 0; y < height; y++)
            {
                int row = ((py + y) * this.Width);

                for (int x = 0; x < width; x++)
                {
                    Color color = source.GetPixel(x, y);
                    if (color.A != 0)
                    {
                        int i = (row + (px + x)) * 4;
                        if (i > maxIndex)
                        {
                            continue;
                        }

                        this.Pixels[i] = color.B;
                        this.Pixels[i + 1] = color.G;
                        this.Pixels[i + 2] = color.R;
                        this.Pixels[i + 3] = color.A;
                    }
                }
            }
        }

        public void CopyPixels(Bitmap source, int rx, int ry, int rw, int rh, int px, int py)
        {
            int maxIndex = this.Length - 4;

            for (int y = 0; y < rh; y++)
            {
                int row = ((py + y) * this.Width);

                for (int x = 0; x < rw; x++)
                {
                    Color color = source.GetPixel(rx + x, ry + y);
                    if (color.A != 0)
                    {
                        int i = (row + (px + x)) * 4;
                        if (i > maxIndex)
                        {
                            continue;
                        }

                        this.Pixels[i] = color.B;
                        this.Pixels[i + 1] = color.G;
                        this.Pixels[i + 2] = color.R;
                        this.Pixels[i + 3] = color.A;
                    }
                }
            }
        }

        public void ColorizePixels(byte[] grayPixels, byte[] blendPixels, byte yellowChannel, byte redChannel, byte greenChannel, byte blueChannel)
        {
            Color rgb = Color.Transparent;

            for (int y = 0; y < this.Height; y++)
            {
                int row = (y * this.Width);

                for (int x = 0; x < this.Width; x++)
                {
                    int bi = (row + x) * 4; // blue index
                    int gi = bi + 1;        // green index
                    int ri = bi + 2;        // red index
                    int ai = bi + 3;        // alpha index

                    if (grayPixels[ai] == 0 || blendPixels[ai] == 0)
                    {
                        this.Pixels[bi] = grayPixels[bi]; // blue
                        this.Pixels[gi] = grayPixels[gi]; // green
                        this.Pixels[ri] = grayPixels[ri]; // red
                        this.Pixels[ai] = grayPixels[ai]; // alpha
                        continue;
                    }

                    byte blendBlue = blendPixels[bi];
                    byte blendGreen = blendPixels[gi];
                    byte blendRed = blendPixels[ri];

                    if (blendRed != 0 && blendGreen != 0 && blendBlue == 0)
                    {
                        rgb = ColorUtils.HsiToRgb(yellowChannel);
                    }
                    else if (blendRed != 0 && blendGreen == 0 && blendBlue == 0)
                    {
                        rgb = ColorUtils.HsiToRgb(redChannel);
                    }
                    else if (blendRed == 0 && blendGreen != 0 && blendBlue == 0)
                    {
                        rgb = ColorUtils.HsiToRgb(greenChannel);
                    }
                    else if (blendRed == 0 && blendGreen == 0 && blendBlue != 0)
                    {
                        rgb = ColorUtils.HsiToRgb(blueChannel);
                    }

                    this.Pixels[bi] = (byte)(grayPixels[bi] * (rgb.B / 255f));
                    this.Pixels[gi] = (byte)(grayPixels[gi] * (rgb.G / 255f));
                    this.Pixels[ri] = (byte)(grayPixels[ri] * (rgb.R / 255f));
                    this.Pixels[ai] = grayPixels[ai];
                }
            }
        }

        public void UnlockBits()
        {
            // copy data from byte array to pointer
            Marshal.Copy(this.Pixels, 0, this.address, this.Length);

            // unlock bitmap data
            this.bitmap.UnlockBits(this.bitmapData);
        }

        public void Dispose()
        {
            this.bitmap = null;
            this.bitmapData = null;
            this.Pixels = null;
            this.Width = 0;
            this.Height = 0;
            this.Length = 0;
        }

        #endregion
    }
}
