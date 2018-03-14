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

        public Sprite(uint id, byte[] pixels, bool transparent)
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

            m_pixels = pixels;
        }

        public Sprite(uint id, bool transparent)
        {
            ID = id;
            Transparent = transparent;

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
                    Array.Clear(m_pixels, 0x00, PixelsDataSize);
                }
                else
                {
                    UncompressBGRA(m_data, m_pixels, m_transparent);
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

                    if (m_data != null && m_data.Length != 0)
                    {
                        CompressBGRA(m_pixels, out m_data, m_transparent);
                    }
                }
            }
        }

        public override string ToString()
        {
            return ID.ToString();
        }

        public Sprite Clone()
        {
            Sprite sprite = new Sprite(ID, m_transparent);
            sprite.m_pixels = (byte[])m_pixels.Clone();
            sprite.m_data = m_data != null ? (byte[])m_data.Clone() : null;
            return sprite;
        }

        public static void CompressBGRA(byte[] pixels, out byte[] data, bool transparent)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid pixels data size");
            }

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
        }

        /// <summary>
        /// Uncompress data to BGRA pixel format.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="buffer"></param>
        /// <param name="transparent"></param>
        public static void UncompressBGRA(byte[] data, byte[]buffer, bool transparent)
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

            int write = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = data.Length;
            int bpp = transparent ? 4 : 3;

            for (int read = 0, pos = 0; read < length; read += 4 + (bpp * coloredPixels))
            {
                transparentPixels = data[pos++] | data[pos++] << 8;
                coloredPixels = data[pos++] | data[pos++] << 8;

                for (int i = 0; i < transparentPixels; i++)
                {
                    buffer[write    ] = 0x00; // blue
                    buffer[write + 1] = 0x00; // green
                    buffer[write + 2] = 0x00; // red
                    buffer[write + 3] = 0x00; // alpha
                    write += 4;
                }

                for (int i = 0; i < coloredPixels; i++)
                {
                    byte red = data[pos++];
                    byte green = data[pos++];
                    byte blue = data[pos++];
                    byte alpha = transparent ? data[pos++] : (byte)0xFF;

                    buffer[write    ] = blue;
                    buffer[write + 1] = green;
                    buffer[write + 2] = red;
                    buffer[write + 3] = alpha;
                    write += 4;
                }
            }

            // fills the remaining pixels
            while (write < PixelsDataSize)
            {
                buffer[write    ] = 0x00; // blue
                buffer[write + 1] = 0x00; // green
                buffer[write + 2] = 0x00; // red
                buffer[write + 3] = 0x00; // alpha
                write += 4;
            }
        }
    }
}
