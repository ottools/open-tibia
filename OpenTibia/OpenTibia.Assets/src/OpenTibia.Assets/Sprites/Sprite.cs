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

using System;
using System.IO;

namespace OpenTibia.Assets
{
    public class Sprite
    {
        private static readonly byte[] EmptyData = new byte[0];

        public const byte DefaultSize = 32;
        public const ushort PixelsDataSize = 4096; // 32*32*4

        private byte[] m_data;
        private byte[] m_pixels;
        private bool m_transparent;

        public Sprite(uint id, byte[] pixels, bool transparent, SpritePixelFormat format)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != PixelsDataSize)
            {
                throw new ArgumentException("Invalid pixels size.");
            }

            ID = id;
            Transparent = transparent;
            PixelFormat = format;

            m_pixels = pixels;
        }

        public Sprite(uint id, bool transparent, SpritePixelFormat format)
        {
            ID = id;
            Transparent = transparent;
            PixelFormat = format;

            m_pixels = new byte[PixelsDataSize];
        }

        public uint ID { get; set; }

        public byte[] Data
        {
            get => m_data;
            set
            {
                m_data = value;

                if (m_data == null || m_data.Length == 0)
                {
                    Array.Clear(m_pixels, 0, PixelsDataSize);
                }
                else
                {
                    Uncompress(m_data, m_transparent, m_pixels, PixelFormat);
                }
            }
        }

        public byte[] Pixels => m_pixels;

        public int Length => m_data != null ? m_data.Length : 0;

        public bool IsEmpty => m_data == null || m_data.Length == 0;

        public bool Transparent
        {
            get => m_transparent;
            set
            {
                if (m_transparent != value)
                {
                    m_transparent = value;

                    if (!IsEmpty)
                    {
                        m_data = Compress(m_pixels, m_transparent, PixelFormat);
                    }
                }
            }
        }

        public SpritePixelFormat PixelFormat { get; }

        public byte[] GetDataAs(SpritePixelFormat format)
        {
            if (IsEmpty)
            {
                return EmptyData;
            }

            if (format == PixelFormat)
            {
                return m_data;
            }

            return Compress(m_pixels, m_transparent, format);
        }

        public byte[] GetPixelsAs(SpritePixelFormat format)
        {
            if (format == PixelFormat || IsEmpty)
            {
                return m_pixels;
            }

            byte[] pixels = new byte[PixelsDataSize];
            Uncompress(m_data, m_transparent, pixels, format);

            return pixels;
        }

        public override string ToString()
        {
            return ID.ToString();
        }

        public Sprite Clone()
        {
            Sprite sprite = new Sprite(ID, m_transparent, PixelFormat);
            sprite.m_pixels = (byte[])m_pixels.Clone();
            sprite.m_data = m_data != null ? (byte[])m_data.Clone() : null;
            return sprite;
        }

        public static byte[] CompressBGRA(byte[] pixels, bool transparent)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid pixels data size");
            }

            byte[] data = null;

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

                data = ((MemoryStream)writer.BaseStream).ToArray();
            }

            return data;
        }

        public static byte[] CompressARGB(byte[] pixels, bool transparent)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid pixels data size");
            }

            byte[] data = null;

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

                data = ((MemoryStream)writer.BaseStream).ToArray();
            }

            return data;
        }

        public static byte[] Compress(byte[] pixels, bool transparent, SpritePixelFormat format)
        {
            if (format == SpritePixelFormat.Bgra)
            {
                return CompressBGRA(pixels, transparent);
            }
            else
            {
                return CompressARGB(pixels, transparent);
            }
        }

        public static void Uncompress(byte[] data, bool transparent, byte[] buffer, SpritePixelFormat format)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length != PixelsDataSize)
            {
                throw new ArgumentException("Invalid buffer size.");
            }

            Array.Clear(buffer, 0, PixelsDataSize);

            if (data.Length == 0)
            {
                return;
            }

            int write = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = data.Length;
            int bpp = transparent ? 4 : 3;

            if (format == SpritePixelFormat.Bgra)
            {
                for (int read = 0, pos = 0; read < length; read += 4 + (bpp * coloredPixels))
                {
                    transparentPixels = data[pos++] | data[pos++] << 8;
                    coloredPixels = data[pos++] | data[pos++] << 8;

                    if (transparentPixels != 0)
                    {
                        write += transparentPixels * 4;
                    }

                    if (transparent)
                    {
                        for (int i = 0; i < coloredPixels; i++)
                        {
                            buffer[write] = data[pos + 2];     // blue
                            buffer[write + 1] = data[pos + 1]; // green
                            buffer[write + 2] = data[pos];     // red
                            buffer[write + 3] = data[pos + 3]; // alpha
                            pos += 4;
                            write += 4;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < coloredPixels; i++)
                        {
                            buffer[write] = data[pos + 2];     // blue
                            buffer[write + 1] = data[pos + 1]; // green
                            buffer[write + 2] = data[pos];     // red
                            buffer[write + 3] = 0xFF;          // alpha
                            pos += 3;
                            write += 4;
                        }
                    }
                }
            }
            else
            {
                for (int read = 0, pos = 0; read < length; read += 4 + (bpp * coloredPixels))
                {
                    transparentPixels = data[pos++] | data[pos++] << 8;
                    coloredPixels = data[pos++] | data[pos++] << 8;

                    if (transparentPixels != 0)
                    {
                        write += transparentPixels * 4;
                    }

                    if (transparent)
                    {
                        for (int i = 0; i < coloredPixels; i++)
                        {
                            buffer[write] = data[pos + 3];     // alpha
                            buffer[write + 1] = data[pos];     // red
                            buffer[write + 2] = data[pos + 1]; // green
                            buffer[write + 3] = data[pos + 2]; // blue
                            pos += 4;
                            write += 4;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < coloredPixels; i++)
                        {
                            buffer[write] = 0xFF;              // alpha
                            buffer[write + 1] = data[pos];     // red
                            buffer[write + 2] = data[pos + 1]; // green
                            buffer[write + 3] = data[pos + 2]; // blue
                            pos += 3;
                            write += 4;
                        }
                    }
                }
            }
        }
    }
}
