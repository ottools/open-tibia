using System;

namespace OpenTibia.Assets
{
    public interface ISpritesFileWriter : IDisposable
    {
        void Open(string path);
        void WriteSignature(uint signature);
        void WriteCount(uint count);
        void WriteSprite(Sprite sprite);
        void WriteSpritePixels(ReadOnlySpan<byte> pixels);
        void WriteRawPixels(ReadOnlySpan<byte> pixels);
    }
}
