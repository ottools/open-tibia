using System;
using System.IO;

namespace OpenTibia.Assets
{
    public class SpritesFileReader : ISpritesFileReader
    {
        private FileStream m_stream;
        private BinaryReader m_reader;
        private int m_headerLength;
        private uint m_signature;
        private bool m_signatureRead;
        private uint m_count;
        private bool m_countRead;
        private bool m_extended;
        private bool m_transparent;
        private SpritePixelFormat m_format;
        private Sprite m_emptySprite;
        private bool m_disposed;

        public SpritesFileReader(AssetsFeatures features, SpritePixelFormat format)
        {
            m_extended = features.HasFlag(AssetsFeatures.Extended);
            m_transparent = features.HasFlag(AssetsFeatures.Transparency);
            m_headerLength = m_extended ? SpritesFileSize.HeaderU32 : SpritesFileSize.HeaderU16;
            m_format = format;
            m_emptySprite = new Sprite(0, m_transparent, m_format);
        }

        public long Length
        {
            get
            {
                if (m_disposed)
                {
                    throw new ObjectDisposedException(nameof(SpritesFileReader));
                }

                return m_reader.BaseStream.Length;
            }
        }

        public int HeaderLength => m_headerLength;

        public uint Signature
        {
            get
            {
                if (m_disposed)
                {
                    throw new ObjectDisposedException(nameof(SpritesFileReader));
                }

                if (m_signatureRead)
                {
                    return m_signature;
                }

                m_stream.Position = SpritesFilePosition.Signature;
                m_signature = m_reader.ReadUInt32();
                m_signatureRead = true;
                return m_signature;
            }
        }

        public uint SpritesCount
        {
            get
            {
                if (m_disposed)
                {
                    throw new ObjectDisposedException(nameof(SpritesFileReader));
                }

                if (m_countRead)
                {
                    return m_count;
                }

                m_stream.Position = SpritesFilePosition.Length;
                m_count = m_extended ? m_reader.ReadUInt32() : m_reader.ReadUInt16();
                m_countRead = true;
                return m_count;
            }
        }

        public bool Extended => m_extended;

        public bool Transparent => m_transparent;

        public SpritePixelFormat Format => m_format;

        public Sprite EmptySprite => m_emptySprite;

        public void Open(string path)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileReader));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            m_stream = new FileStream(path, FileMode.Open);
            m_reader = new BinaryReader(m_stream);
        }

        public bool HasSprite(uint id)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileReader));
            }

            return id >= 0 && id <= SpritesCount;
        }

        public bool IsEmptySprite(uint id)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileReader));
            }

            if (id == 0)
            {
                return true;
            }

            if (id > SpritesCount)
            {
                throw new IndexOutOfRangeException();
            }

            m_stream.Position = ((id - 1) * SpritesFileSize.Address) + m_headerLength;

            var address = m_reader.ReadUInt32();
            if (address == 0)
            {
                return true;
            }

            // skipping 3 bytes of key color.
            m_stream.Position = address + 3;

            return m_reader.ReadUInt16() == 0;
        }

        public Sprite ReadSprite(uint id)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileReader));
            }

            if (id == 0)
            {
                return m_emptySprite;
            }

            if (id > SpritesCount)
            {
                throw new IndexOutOfRangeException();
            }

            m_stream.Position = ((id - 1) * SpritesFileSize.Address) + m_headerLength;

            var address = m_reader.ReadUInt32();
            if (address == 0)
            {
                return new Sprite(id, m_transparent, m_format);
            }

            // skipping 3 bytes of color key.
            m_stream.Position = address + 3;

            Sprite sprite = new Sprite(id, m_transparent, m_format);

            ushort count = m_reader.ReadUInt16();
            if (count != 0)
            {
                sprite.Data = m_reader.ReadBytes(count);
            }

            return sprite;
        }

        public byte[] ReadSpritePixels(uint id)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileReader));
            }

            if (id == 0)
            {
                return new byte[Sprite.PixelsDataSize];
            }

            if (id > SpritesCount)
            {
                throw new IndexOutOfRangeException();
            }

            m_stream.Position = ((id - 1) * SpritesFileSize.Address) + m_headerLength;

            var address = m_reader.ReadUInt32();
            if (address == 0)
            {
                return new byte[Sprite.PixelsDataSize];
            }

            // skipping 3 bytes of color key.
            m_stream.Position = address + 3;

            ushort count = m_reader.ReadUInt16();
            if (count == 0)
            {
                return new byte[Sprite.PixelsDataSize];
            }

            byte[] data = m_reader.ReadBytes(count);
            byte[] pixels = new byte[Sprite.PixelsDataSize];
            Sprite.Uncompress(data, m_transparent, pixels, m_format);
            return pixels;
        }

        public byte[] ReadRawPixels(uint id)
        {
            if (id == 0)
            {
                return Sprite.EmptyData;
            }

            m_stream.Position = ((id - 1) * SpritesFileSize.Address) + m_headerLength;

            var address = m_reader.ReadUInt32();
            if (address == 0)
            {
                return Sprite.EmptyData;
            }

            // skipping 3 bytes of color key.
            m_stream.Position = address + 3;

            ushort count = m_reader.ReadUInt16();
            if (count != 0)
            {
                return m_reader.ReadBytes(count);
            }

            return Sprite.EmptyData;
        }

        public int ReadBytes(byte[] buffer, int offset, int count)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(SpritesFileReader));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return m_reader.Read(buffer, offset, count);
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

            if (m_reader != null)
            {
                m_reader.Dispose();
                m_reader = null;
            }

            m_emptySprite = null;
        }
    }
}
