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

namespace OpenTibia.Animation
{
    public enum FrameGroupType : byte
    {
        Default = 0,
        Walking = 1
    }

    public class FrameGroup
    {
        public byte Width { get; set; }

        public byte Height { get; set; }

        public byte ExactSize { get; set; }

        public byte Layers { get; set; }

        public byte PatternsX { get; set; }

        public byte PatternsY { get; set; }

        public byte PatternsZ { get; set; }

        public byte Frames { get; set; }

        public uint[] SpriteIDs { get; set; }

        public bool IsAnimation { get; set; }

        public AnimationMode AnimationMode { get; set; }

        public int LoopCount { get; set; }

        public sbyte StartFrame { get; set; }

        public FrameDuration[] FrameDurations { get; set; }

        public int GetTotalSprites()
        {
            return Width * Height * PatternsX * PatternsY * PatternsZ * Frames * Layers;
        }

        public int GetSpriteIndex(int width, int height, int layers, int patternX, int patternY, int patternZ, int frames)
        {
            return ((((((frames % Frames) * PatternsZ + patternZ) * PatternsY + patternY) * PatternsX + patternX) * Layers + layers) * Height + height) * Width + width;
        }

        public int GetTextureIndex(int layer, int patternX, int patternY, int patternZ, int frame)
        {
            return (((frame % Frames * PatternsZ + patternZ) * PatternsY + patternY) * PatternsX + patternX) * Layers + layer;
        }

        public FrameGroup Clone()
        {
            FrameGroup group = new FrameGroup
            {
                Width = Width,
                Height = Height,
                Layers = Layers,
                Frames = Frames,
                PatternsX = PatternsX,
                PatternsY = PatternsY,
                PatternsZ = PatternsZ,
                ExactSize = ExactSize,
                SpriteIDs = (uint[])SpriteIDs.Clone(),
                AnimationMode = AnimationMode,
                LoopCount = LoopCount,
                StartFrame = StartFrame
            };

            if (Frames > 1)
            {
                group.IsAnimation = true;
                group.FrameDurations = new FrameDuration[Frames];

                for (int i = 0; i < Frames; i++)
                {
                    group.FrameDurations[i] = FrameDurations[i].Clone();
                }
            }

            return group;
        }

        public static FrameGroup Create()
        {
            FrameGroup group = new FrameGroup
            {
                Width = 1,
                Height = 1,
                Layers = 1,
                Frames = 1,
                PatternsX = 1,
                PatternsY = 1,
                PatternsZ = 1,
                ExactSize = 32,
                SpriteIDs = new uint[1],
                IsAnimation = false,
                AnimationMode = AnimationMode.Asynchronous,
                LoopCount = 0,
                StartFrame = 0,
                FrameDurations = null
            };

            return group;
        }
    }
}
