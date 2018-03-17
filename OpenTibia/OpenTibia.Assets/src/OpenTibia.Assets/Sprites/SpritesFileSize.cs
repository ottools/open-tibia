namespace OpenTibia.Assets
{
    public static class SpritesFileSize
    {
        /// <summary>
        /// The size for header without extended option enabled.
        /// </summary>
        public const int HeaderU16 = SpritesFilePosition.Length + 2;

        /// <summary>
        /// The size for header with extended option enabled.
        /// </summary>
        public const int HeaderU32 = SpritesFilePosition.Length + 4;

        /// <summary>
        /// The size of sprite address in bytes.
        /// </summary>
        public const byte Address = 4;
    }
}
