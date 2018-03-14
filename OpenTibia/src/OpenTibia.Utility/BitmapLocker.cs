#region Licence
/**
* Copyright © 2015-2018 OTTools <https://github.com/ottools/open-tibia>
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

using OpenTibia.Utility;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenTibia.Utilities
{
    public class BitmapLocker : IDisposable
    {
        private Bitmap m_bitmap;
        private BitmapData m_bitmapData;
        private IntPtr m_address = IntPtr.Zero;

        public BitmapLocker(Bitmap bitmap)
        {
            m_bitmap = bitmap;
            Width = bitmap.Width;
            Height = bitmap.Height;
            Length = bitmap.Width * bitmap.Height * 4;
            Pixels = new byte[Length];
        }

        public byte[] Pixels { get; }

        public int Width { get; }

        public int Height { get; }

        public int Length { get; }

        public void LockBits()
        {
            // create rectangle to lock
            Rectangle rect = new Rectangle(0, 0, m_bitmap.Width, m_bitmap.Height);

            // lock bitmap and return bitmap data
            m_bitmapData = m_bitmap.LockBits(rect, ImageLockMode.ReadWrite, m_bitmap.PixelFormat);

            // gets the address of the first pixel data in the bitmap
            m_address = m_bitmapData.Scan0;

            // copy data from pointer to array
            Marshal.Copy(m_address, Pixels, 0, Length);
        }

        public void CopyPixels(Bitmap source, int px, int py)
        {
            int width = source.Width;
            int height = source.Height;
            int maxIndex = Length - 4;

            for (int y = 0; y < height; y++)
            {
                int row = ((py + y) * Width);

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

                        Pixels[i] = color.B;
                        Pixels[i + 1] = color.G;
                        Pixels[i + 2] = color.R;
                        Pixels[i + 3] = color.A;
                    }
                }
            }
        }

        public void CopyPixels(Bitmap source, int rx, int ry, int rw, int rh, int px, int py)
        {
            int maxIndex = Length - 4;

            for (int y = 0; y < rh; y++)
            {
                int row = ((py + y) * Width);

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

                        Pixels[i] = color.B;
                        Pixels[i + 1] = color.G;
                        Pixels[i + 2] = color.R;
                        Pixels[i + 3] = color.A;
                    }
                }
            }
        }

        public void ColorizePixels(byte[] grayPixels, byte[] blendPixels, byte yellowChannel, byte redChannel, byte greenChannel, byte blueChannel)
        {
            Color rgb = Color.Transparent;

            for (int y = 0; y < Height; y++)
            {
                int row = (y * Width);

                for (int x = 0; x < Width; x++)
                {
                    int bi = (row + x) * 4; // blue index
                    int gi = bi + 1;        // green index
                    int ri = bi + 2;        // red index
                    int ai = bi + 3;        // alpha index

                    if (grayPixels[ai] == 0 || blendPixels[ai] == 0)
                    {
                        Pixels[bi] = grayPixels[bi]; // blue
                        Pixels[gi] = grayPixels[gi]; // green
                        Pixels[ri] = grayPixels[ri]; // red
                        Pixels[ai] = grayPixels[ai]; // alpha
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

                    Pixels[bi] = (byte)(grayPixels[bi] * (rgb.B / 255f));
                    Pixels[gi] = (byte)(grayPixels[gi] * (rgb.G / 255f));
                    Pixels[ri] = (byte)(grayPixels[ri] * (rgb.R / 255f));
                    Pixels[ai] = grayPixels[ai];
                }
            }
        }

        public void UnlockBits()
        {
            // copy data from byte array to pointer
            Marshal.Copy(Pixels, 0, m_address, Length);

            // unlock bitmap data
            m_bitmap.UnlockBits(m_bitmapData);
        }

        public void Dispose()
        {
            m_bitmap = null;
            m_bitmapData = null;
        }
    }
}
