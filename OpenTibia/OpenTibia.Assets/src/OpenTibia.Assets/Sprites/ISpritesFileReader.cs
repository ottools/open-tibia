using System;

namespace OpenTibia.Assets
{
    public interface ISpritesFileReader : IDisposable
    {
        long Length { get; }
        int HeaderLength { get; }
        uint Signature { get; }
        uint SpritesCount { get; }
        bool Extended { get; }
        bool Transparent { get; }
        SpritePixelFormat Format { get; }

        void Open(string path);
        bool HasSprite(uint id);
        bool IsEmptySprite(uint id);
        Sprite ReadSprite(uint id);
        byte[] ReadSpritePixels(uint id);
        byte[] ReadRawPixels(uint id);
        int ReadBytes(byte[] buffer, int offset, int count);
    }
}
