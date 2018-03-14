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

using OpenTibia.Animation;
using OpenTibia.Assets;
using OpenTibia.IO;
using OpenTibia.Utility;
using System;
using System.IO;
using System.Text;

namespace OpenTibia.Obd
{
    public static class ObdEncoder
    {
        public static byte[] Encode(ObjectData data, ObdVersion obdVersion)
        {
            if (obdVersion == ObdVersion.Version3)
            {
                return EncodeV3(data);
            }
            else if (obdVersion == ObdVersion.Version2)
            {
                return EncodeV2(data);
            }
            else if (obdVersion == ObdVersion.Version1)
            {
                return EncodeV1(data);
            }

            return null;
        }

        private static byte[] EncodeV1(ObjectData data)
        {
            using (FlagsBinaryWriter writer = new FlagsBinaryWriter(new MemoryStream()))
            {
                // write client version
                writer.Write((ushort)MetadataFormat.Format_1010);

                // write category
                string category = string.Empty;
                switch (data.Category)
                {
                    case ObjectCategory.Item:
                        category = "item";
                        break;

                    case ObjectCategory.Outfit:
                        category = "outfit";
                        break;

                    case ObjectCategory.Effect:
                        category = "effect";
                        break;

                    case ObjectCategory.Missile:
                        category = "missile";
                        break;
                }

                writer.Write((ushort)category.Length);
                writer.Write(Encoding.UTF8.GetBytes(category));

                if (!ThingTypeSerializer.WriteProperties(data.ThingType, MetadataFormat.Format_1010, writer))
                {
                    return null;
                }

                FrameGroup group = data.GetFrameGroup(FrameGroupType.Default);

                writer.Write(group.Width);
                writer.Write(group.Height);

                if (group.Width > 1 || group.Height > 1)
                {
                    writer.Write(group.ExactSize);
                }

                writer.Write(group.Layers);
                writer.Write(group.PatternsX);
                writer.Write(group.PatternsY);
                writer.Write(group.PatternsZ);
                writer.Write(group.Frames);

                Sprite[] sprites = data.Sprites[FrameGroupType.Default];
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sprite = sprites[i];
                    writer.Write((uint)sprite.ID);
                    writer.Write((uint)sprite.Length);
                    writer.Write(sprite.Pixels);
                }

                return LZMACoder.Compress(((MemoryStream)writer.BaseStream).ToArray());
            }
        }

        private static byte[] EncodeV2(ObjectData data)
        {
            using (FlagsBinaryWriter writer = new FlagsBinaryWriter(new MemoryStream()))
            {
                // write obd version
                writer.Write((ushort)ObdVersion.Version2);

                // write client version
                writer.Write((ushort)MetadataFormat.Format_1050);

                // write category
                writer.Write((byte)data.Category);

                // skipping the texture patterns position.
                int patternsPosition = (int)writer.BaseStream.Position;
                writer.Seek(4, SeekOrigin.Current);

                if (!ThingTypeSerializer.WriteProperties(data.ThingType, MetadataFormat.Format_1050, writer))
                {
                    return null;
                }

                // write the texture patterns position.
                int position = (int)writer.BaseStream.Position;
                writer.Seek(patternsPosition, SeekOrigin.Begin);
                writer.Write((uint)writer.BaseStream.Position);
                writer.Seek(position, SeekOrigin.Begin);

                FrameGroup group = data.GetFrameGroup(FrameGroupType.Default);

                writer.Write(group.Width);
                writer.Write(group.Height);

                if (group.Width > 1 || group.Height > 1)
                {
                    writer.Write(group.ExactSize);
                }

                writer.Write(group.Layers);
                writer.Write(group.PatternsX);
                writer.Write(group.PatternsY);
                writer.Write(group.PatternsZ);
                writer.Write(group.Frames);

                if (group.IsAnimation)
                {
                    writer.Write((byte)group.AnimationMode);
                    writer.Write(group.LoopCount);
                    writer.Write(group.StartFrame);

                    for (int i = 0; i < group.Frames; i++)
                    {
                        writer.Write((uint)group.FrameDurations[i].Minimum);
                        writer.Write((uint)group.FrameDurations[i].Maximum);
                    }
                }

                Sprite[] sprites = data.Sprites[FrameGroupType.Default];
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sprite = sprites[i];
                    writer.Write(sprite.ID);
                    writer.Write(sprite.Pixels);
                }

                return LZMACoder.Compress(((MemoryStream)writer.BaseStream).ToArray());
            }
        }

        private static byte[] EncodeV3(ObjectData data)
        {
            throw new NotImplementedException();
        }
    }
}
