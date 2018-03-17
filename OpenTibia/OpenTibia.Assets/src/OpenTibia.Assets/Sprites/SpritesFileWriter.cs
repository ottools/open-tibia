using System;
using System.IO;

namespace OpenTibia.Assets
{
    public class SpritesFileWriter : ISpritesFileWriter
    {
        private FileStream m_stream;
        private BinaryWriter m_writer;
        private bool m_extended;
        private bool m_transparent;
        private SpritePixelFormat m_format;
        private uint m_count;
        private bool m_signatureWrited;
        private bool m_countWrited;
        private int m_address;
        private int m_position;
        private bool m_disposed;

        public SpritesFileWriter(AssetsFeatures features, SpritePixelFormat format)
        {
            m_extended = features.HasFlag(AssetsFeatures.Extended);
            m_transparent = features.HasFlag(AssetsFeatures.Transparency);
            m_format = format;
        }

        public uint Count => m_count;

        public bool Extended => m_extended;

        public bool Transparent => m_transparent;

        public void Open(string path)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileWriter));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            m_stream = new FileStream(path, FileMode.Create);
            m_writer = new BinaryWriter(m_stream);
        }

        public void WriteSignature(uint signature)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileWriter));
            }

            if (m_signatureWrited)
            {
                throw new InvalidOperationException("Signature was already written.");
            }

            m_writer.Write(signature);
            m_signatureWrited = true;
        }

        public void WriteCount(uint count)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileWriter));
            }

            if (m_countWrited)
            {
                throw new InvalidOperationException("Count was already written.");
            }

            if (m_extended)
            {
                m_count = count;
                m_writer.Write(m_count);
                m_address = SpritesFileSize.HeaderU32;
                m_position = (int)(m_count * 4) + SpritesFileSize.HeaderU32;
            }
            else
            {
                m_count = count >= 0xFFFF ? 0xFFFF : count;
                m_writer.Write((ushort)m_count);
                m_address = SpritesFileSize.HeaderU16;
                m_position = (int)(m_count * 4) + SpritesFileSize.HeaderU16;
            }

            m_countWrited = true;
        }

        public void WriteSprite(Sprite sprite)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileWriter));
            }

            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            if (!m_signatureWrited)
            {
                throw new InvalidOperationException("You must write the file signature before.");
            }

            if (!m_countWrited)
            {
                throw new InvalidOperationException("You must write the sprites count before.");
            }

            m_writer.Seek(m_address, SeekOrigin.Begin);

            if (sprite.Length == 0)
            {
                // write adress
                m_writer.Write((uint)0);
            }
            else
            {
                // write address
                m_writer.Write((uint)m_position);
                m_writer.Seek(m_position, SeekOrigin.Begin);

                // write colorkey
                m_writer.Write((byte)0xFF); // red
                m_writer.Write((byte)0x00); // blue
                m_writer.Write((byte)0xFF); // green

                byte[] data = sprite.Data;

                if (sprite.Transparent != m_transparent)
                {
                    data = Sprite.Compress(sprite.Pixels, m_transparent, m_format);
                }

                // write sprite data size
                m_writer.Write((ushort)data.Length);

                // write sprite compressed pixels
                m_writer.Write(data);

                m_position = (int)m_writer.BaseStream.Position;
            }

            m_address += 4;
        }

        public void WriteSpritePixels(ReadOnlySpan<byte> pixels)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileWriter));
            }

            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != Sprite.PixelsDataSize)
            {
                throw new ArgumentException("Invalid pixels data size");
            }

            if (!m_signatureWrited)
            {
                throw new InvalidOperationException("You must write the file signature before.");
            }

            if (!m_countWrited)
            {
                throw new InvalidOperationException("You must write the sprites count before.");
            }

            byte[] data = Sprite.Compress(pixels, m_transparent, m_format);

            m_writer.Seek(m_address, SeekOrigin.Begin);

            if (data.Length == 0)
            {
                // write adress
                m_writer.Write((uint)0);
            }
            else
            {
                // write address
                m_writer.Write((uint)m_position);
                m_writer.Seek(m_position, SeekOrigin.Begin);

                // write colorkey
                m_writer.Write((byte)0xFF); // red
                m_writer.Write((byte)0x00); // blue
                m_writer.Write((byte)0xFF); // green

                // write sprite data size
                m_writer.Write((ushort)data.Length);

                // write sprite compressed pixels
                m_writer.Write(data);

                m_position = (int)m_writer.BaseStream.Position;
            }

            m_address += 4;
        }

        public void WriteRawPixels(ReadOnlySpan<byte> pixels)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileWriter));
            }

            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (!m_signatureWrited)
            {
                throw new InvalidOperationException("You must write the file signature before.");
            }

            if (!m_countWrited)
            {
                throw new InvalidOperationException("You must write the sprites count before.");
            }

            m_writer.Seek(m_address, SeekOrigin.Begin);

            if (pixels.Length == 0)
            {
                // write adress
                m_writer.Write((uint)0);
            }
            else
            {
                // write address
                m_writer.Write((uint)m_position);
                m_writer.Seek(m_position, SeekOrigin.Begin);

                // write colorkey
                m_writer.Write((byte)0xFF); // red
                m_writer.Write((byte)0x00); // blue
                m_writer.Write((byte)0xFF); // green

                // write sprite data size
                m_writer.Write((ushort)pixels.Length);

                // write sprite compressed pixels
                m_writer.Write(pixels.ToArray());

                m_position = (int)m_writer.BaseStream.Position;
            }

            m_address += 4;
        }

        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;

            if (m_stream != null)
            {
                m_stream.Dispose();
                m_stream = null;
            }

            if (m_writer != null)
            {
                m_writer.Dispose();
                m_writer = null;
            }
        }
    }
}
