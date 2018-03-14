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
using OpenTibia.Utils;
using System;
using System.IO;
using System.Text;

namespace OpenTibia.Obd
{
    public class ObdDecoder
    {
        public static ObjectData Decode(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            bytes = LZMACoder.Uncompress(bytes);

            using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
            {
                ushort version = reader.ReadUInt16();

                if (version == (ushort)ObdVersion.Version3)
                {
                    return DecodeV3(reader);
                }
                else if (version == (ushort)ObdVersion.Version2)
                {
                    return DecodeV2(reader);
                }
                else if (version >= (ushort)MetadataFormat.Format_710)
                {
                    return DecodeV1(reader);
                }
                else
                {
                    ////
                }
            }

            return null;
        }

        private static ObjectData DecodeV1(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;

            Console.WriteLine(reader.ReadUInt16());

            ushort nameLength = reader.ReadUInt16();
            byte[] buffer = reader.ReadBytes(nameLength);
            string categoryStr = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            ThingCategory category = ThingCategory.Invalid;

            switch (categoryStr)
            {
                case "item":
                    category = ThingCategory.Item;
                    break;

                case "outfit":
                    category = ThingCategory.Outfit;
                    break;

                case "effect":
                    category = ThingCategory.Effect;
                    break;

                case "missile":
                    category = ThingCategory.Missile;
                    break;
            }

            ThingType thing = new ThingType(category);

            if (!ThingTypeSerializer.ReadProperties(thing, MetadataFormat.Format_1010, reader))
            {
                return null;
            }

            FrameGroup group = new FrameGroup();

            group.Width = reader.ReadByte();
            group.Height = reader.ReadByte();

            if (group.Width > 1 || group.Height > 1)
            {
                group.ExactSize = reader.ReadByte();
            }
            else
            {
                group.ExactSize = Sprite.DefaultSize;
            }

            group.Layers = reader.ReadByte();
            group.PatternsX = reader.ReadByte();
            group.PatternsY = reader.ReadByte();
            group.PatternsZ = reader.ReadByte();
            group.Frames = reader.ReadByte();

            if (group.Frames > 1)
            {
                group.IsAnimation = true;
                group.AnimationMode = AnimationMode.Asynchronous;
                group.LoopCount = 0;
                group.StartFrame = 0;
                group.FrameDurations = new FrameDuration[group.Frames];

                for (byte i = 0; i < group.Frames; i++)
                {
                    group.FrameDurations[i] = new FrameDuration(category);
                }
            }

            int totalSprites = group.GetTotalSprites();
            if (totalSprites > 4096)
            {
                throw new Exception("The ThingData has more than 4096 sprites.");
            }

            group.SpriteIDs = new uint[totalSprites];
            SpriteGroup spriteGroup = new SpriteGroup();
            Sprite[] sprites = new Sprite[totalSprites];

            for (int i = 0; i < totalSprites; i++)
            {
                uint spriteID = reader.ReadUInt32();
                group.SpriteIDs[i] = spriteID;

                uint dataSize = reader.ReadUInt32();
                if (dataSize > Sprite.PixelsDataSize)
                {
                    throw new Exception("Invalid sprite data size.");
                }

                byte[] pixels = reader.ReadBytes((int)dataSize);

                Sprite sprite = new Sprite(spriteID, true);
                sprite.SetPixelsARGB(pixels);
                sprites[i] = sprite;
            }

            thing.SetFrameGroup(FrameGroupType.Default, group);
            spriteGroup.Add(FrameGroupType.Default, sprites);
            return new ObjectData(thing, spriteGroup);
        }

        private static ObjectData DecodeV2(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        private static ObjectData DecodeV3(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
