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
using OpenTibia.IO;
using System;
using System.IO;
using System.Text;

namespace OpenTibia.Assets
{
    public static class ThingTypeSerializer
    {
        public static bool ReadProperties(ThingType thing, MetadataFormat format, BinaryReader reader)
        {
            if (format >= MetadataFormat.Format_1010)
            {
                if (!ReadProperties_1010_1099(thing, reader, format))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ReadTexturePatterns(ThingType thing, AssetsFeatures features, BinaryReader reader)
        {
            bool patternZEnabled = (features & AssetsFeatures.PatternsZ) == AssetsFeatures.PatternsZ;
            bool extendedEnabled = (features & AssetsFeatures.Extended) == AssetsFeatures.Extended;
            bool frameDurationsEnabled = (features & AssetsFeatures.FramesDuration) == AssetsFeatures.FramesDuration;
            bool frameGroupsEnabled = (features & AssetsFeatures.FrameGroups) == AssetsFeatures.FrameGroups;

            byte groupCount = 1;
            if (frameGroupsEnabled && thing.Category == ThingCategory.Outfit)
            {
                groupCount = reader.ReadByte();
            }

            for (byte k = 0; k < groupCount; k++)
            {
                FrameGroupType groupType = FrameGroupType.Default;
                if (frameGroupsEnabled && thing.Category == ThingCategory.Outfit)
                {
                    groupType = (FrameGroupType)reader.ReadByte();
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
                group.PatternsZ = patternZEnabled ? reader.ReadByte() : (byte)1;
                group.Frames = reader.ReadByte();

                if (frameDurationsEnabled && group.Frames > 1)
                {
                    group.IsAnimation = true;
                    group.AnimationMode = (AnimationMode)reader.ReadByte();
                    group.LoopCount = reader.ReadInt32();
                    group.StartFrame = reader.ReadSByte();
                    group.FrameDurations = new FrameDuration[group.Frames];

                    for (int i = 0; i < group.Frames; i++)
                    {
                        uint minimum = reader.ReadUInt32();
                        uint maximum = reader.ReadUInt32();
                        group.FrameDurations[i] = new FrameDuration(minimum, maximum);
                    }
                }

                int totalSprites = group.GetTotalSprites();
                if (totalSprites > 4096)
                {
                    throw new Exception("A thing type has more than 4096 sprites.");
                }

                group.SpriteIDs = new uint[totalSprites];

                if (extendedEnabled)
                {
                    for (int i = 0; i < totalSprites; i++)
                    {
                        group.SpriteIDs[i] = reader.ReadUInt32();
                    }
                }
                else
                {
                    for (int i = 0; i < totalSprites; i++)
                    {
                        group.SpriteIDs[i] = reader.ReadUInt16();
                    }
                }

                thing.SetFrameGroup(groupType, group);
            }

            return true;
        }

        public static bool WriteProperties(ThingType thing, MetadataFormat format, FlagsBinaryWriter writer)
        {
            if (format >= MetadataFormat.Format_1010)
            {
                if (!WriteProperties_1010_1099(thing, writer, format))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool WriteTexturePatterns(ThingType thing, AssetsFeatures features, BinaryWriter writer)
        {
            bool patternZEnabled = (features & AssetsFeatures.PatternsZ) == AssetsFeatures.PatternsZ;
            bool extendedEnabled = (features & AssetsFeatures.Extended) == AssetsFeatures.Extended;
            bool frameDurationsEnabled = (features & AssetsFeatures.FramesDuration) == AssetsFeatures.FramesDuration;
            bool frameGroupsEnabled = (features & AssetsFeatures.FrameGroups) == AssetsFeatures.FrameGroups;
            int groupCount = 1;

            // write frame group count.
            if (frameGroupsEnabled && thing.Category == ThingCategory.Outfit)
            {
                groupCount = thing.FrameGroupCount;
                writer.Write(groupCount);
            }

            for (byte k = 0; k < groupCount; k++)
            {
                // write frame group type.
                if (frameGroupsEnabled && thing.Category == ThingCategory.Outfit)
                {
                    writer.Write(k);
                }

                FrameGroup group = thing.GetFrameGroup((FrameGroupType)k);

                writer.Write(group.Width);  // write width
                writer.Write(group.Height); // write heigh

                // write exact size
                if (group.Width > 1 || group.Height > 1)
                {
                    writer.Write(group.ExactSize);
                }

                writer.Write(group.Layers);     // write layers
                writer.Write(group.PatternsX);   // write pattern X
                writer.Write(group.PatternsY);   // write pattern Y

                if (patternZEnabled)
                {
                    writer.Write(group.PatternsZ); // write pattern Z
                }

                writer.Write(group.Frames); // write frames

                if (frameDurationsEnabled && group.Frames > 1)
                {
                    writer.Write((byte)group.AnimationMode); // write animation type
                    writer.Write(group.LoopCount); // write frame strategy
                    writer.Write(group.StartFrame); // write start frame

                    FrameDuration[] durations = group.FrameDurations;
                    for (int i = 0; i < durations.Length; i++)
                    {
                        writer.Write((uint)durations[i].Minimum); // write minimum duration
                        writer.Write((uint)durations[i].Maximum); // write maximum duration
                    }
                }

                uint[] sprites = group.SpriteIDs;
                for (int i = 0; i < sprites.Length; i++)
                {
                    // write sprite index
                    if (extendedEnabled)
                    {
                        writer.Write(sprites[i]);
                    }
                    else
                    {
                        writer.Write((ushort)sprites[i]);
                    }
                }
            }

            return true;
        }

        private static bool ReadProperties_1010_1099(ThingType thing, BinaryReader reader, MetadataFormat format)
        {
            MetadataFlags_1010_1099 flag;

            do
            {
                flag = (MetadataFlags_1010_1099)reader.ReadByte();

                if (flag == MetadataFlags_1010_1099.LastFlag)
                {
                    break;
                }

                switch (flag)
                {
                    case MetadataFlags_1010_1099.Ground: // 0x00
                        thing.StackOrder = StackOrder.Ground;
                        thing.GroundSpeed = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.GroundBorder: // 0x01
                        thing.StackOrder = StackOrder.Border;
                        break;

                    case MetadataFlags_1010_1099.OnBottom: // 0x02
                        thing.StackOrder = StackOrder.Bottom;
                        break;

                    case MetadataFlags_1010_1099.OnTop: // 0x03
                        thing.StackOrder = StackOrder.Top;
                        break;

                    case MetadataFlags_1010_1099.Container: // 0x04
                        thing.IsContainer = true;
                        break;

                    case MetadataFlags_1010_1099.Stackable:
                        thing.Stackable = true;
                        break;

                    case MetadataFlags_1010_1099.ForceUse:
                        thing.ForceUse = true;
                        break;

                    case MetadataFlags_1010_1099.MultiUse:
                        thing.MultiUse = true;
                        break;

                    case MetadataFlags_1010_1099.Writable:
                        thing.Writable = true;
                        thing.MaxTextLength = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.WritableOnce:
                        thing.WritableOnce = true;
                        thing.MaxTextLength = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.FluidContainer:
                        thing.IsFluidContainer = true;
                        break;

                    case MetadataFlags_1010_1099.Fluid:
                        thing.IsFluid = true;
                        break;

                    case MetadataFlags_1010_1099.IsUnpassable:
                        thing.Unpassable = true;
                        break;

                    case MetadataFlags_1010_1099.IsUnmovable:
                        thing.Unmovable = true;
                        break;

                    case MetadataFlags_1010_1099.BlockMissiles:
                        thing.BlockMissiles = true;
                        break;

                    case MetadataFlags_1010_1099.BlockPathfinder:
                        thing.BlockPathfinder = true;
                        break;

                    case MetadataFlags_1010_1099.NoMoveAnimation: // 0x10
                        thing.NoMoveAnimation = true;
                        break;

                    case MetadataFlags_1010_1099.Pickupable:
                        thing.Pickupable = true;
                        break;

                    case MetadataFlags_1010_1099.Hangable:
                        thing.Hangable = true;
                        break;

                    case MetadataFlags_1010_1099.HookSouth:
                        thing.HookSouth = true;
                        break;

                    case MetadataFlags_1010_1099.HookEast:
                        thing.HookEast = true;
                        break;

                    case MetadataFlags_1010_1099.Rotatable:
                        thing.Rotatable = true;
                        break;

                    case MetadataFlags_1010_1099.HasLight:
                        thing.HasLight = true;
                        thing.LightLevel = reader.ReadUInt16();
                        thing.LightColor = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.DontHide:
                        thing.DontHide = true;
                        break;

                    case MetadataFlags_1010_1099.Translucent:
                        thing.Translucent = true;
                        break;

                    case MetadataFlags_1010_1099.HasOffset:
                        thing.HasOffset = true;
                        thing.OffsetX = reader.ReadUInt16();
                        thing.OffsetY = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.HasElevation:
                        thing.HasElevation = true;
                        thing.Elevation = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.LyingObject:
                        thing.LyingObject = true;
                        break;

                    case MetadataFlags_1010_1099.Minimap:
                        thing.Minimap = true;
                        thing.MinimapColor = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.AnimateAlways:
                        thing.AnimateAlways = true;
                        break;

                    case MetadataFlags_1010_1099.LensHelp:
                        thing.IsLensHelp = true;
                        thing.LensHelp = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.FullGround:
                        thing.FullGround = true;
                        break;

                    case MetadataFlags_1010_1099.IgnoreLook:
                        thing.IgnoreLook = true;
                        break;

                    case MetadataFlags_1010_1099.Cloth:
                        thing.IsCloth = true;
                        thing.ClothSlot = (ClothSlot)reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.Market:
                        thing.IsMarketItem = true;
                        thing.MarketCategory = (MarketCategory)reader.ReadUInt16();
                        thing.MarketTradeAs = reader.ReadUInt16();
                        thing.MarketShowAs = reader.ReadUInt16();

                        ushort nameLength = reader.ReadUInt16();
                        byte[] buffer = reader.ReadBytes(nameLength);
                        thing.MarketName = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                        thing.MarketRestrictVocation = reader.ReadUInt16();
                        thing.MarketRestrictLevel = reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.DefaultAction:
                        thing.HasAction = true;
                        thing.DefaultAction = (DefaultAction)reader.ReadUInt16();
                        break;

                    case MetadataFlags_1010_1099.Wrappable:
                        thing.Wrappable = true;
                        break;

                    case MetadataFlags_1010_1099.Unwrappable:
                        thing.Unwrappable = true;
                        break;

                    case MetadataFlags_1010_1099.TopEffect:
                        thing.IsTopEffect = true;
                        break;

                    case MetadataFlags_1010_1099.Usable:
                        thing.Usable = true;
                        break;

                    default:
                        throw new Exception(string.Format("Error while parsing, unknown flag 0x{0:X} at object id {1}, category {2}.", flag, thing.ID, thing.Category));
                }
            }
            while (flag != MetadataFlags_1010_1099.LastFlag);

            return true;
        }

        private static bool WriteProperties_1010_1099(ThingType thing, FlagsBinaryWriter writer, MetadataFormat format)
        {
            if (thing.Category == ThingCategory.Item)
            {
                if (thing.StackOrder == StackOrder.Ground)
                {
                    writer.Write(MetadataFlags_1010_1099.Ground);
                    writer.Write(thing.GroundSpeed);
                }
                else if (thing.StackOrder == StackOrder.Border)
                {
                    writer.Write(MetadataFlags_1010_1099.GroundBorder);
                }
                else if (thing.StackOrder == StackOrder.Bottom)
                {
                    writer.Write(MetadataFlags_1010_1099.OnBottom);
                }
                else if (thing.StackOrder == StackOrder.Top)
                {
                    writer.Write(MetadataFlags_1010_1099.OnTop);
                }

                if (thing.IsContainer)
                {
                    writer.Write(MetadataFlags_1010_1099.Container);
                }

                if (thing.Stackable)
                {
                    writer.Write(MetadataFlags_1010_1099.Stackable);
                }

                if (thing.ForceUse)
                {
                    writer.Write(MetadataFlags_1010_1099.ForceUse);
                }

                if (thing.MultiUse)
                {
                    writer.Write(MetadataFlags_1010_1099.MultiUse);
                }

                if (thing.Writable)
                {
                    writer.Write(MetadataFlags_1010_1099.Writable);
                    writer.Write(thing.MaxTextLength);
                }

                if (thing.WritableOnce)
                {
                    writer.Write(MetadataFlags_1010_1099.WritableOnce);
                    writer.Write(thing.MaxTextLength);
                }

                if (thing.IsFluidContainer)
                {
                    writer.Write(MetadataFlags_1010_1099.FluidContainer);
                }

                if (thing.IsFluid)
                {
                    writer.Write(MetadataFlags_1010_1099.Fluid);
                }

                if (thing.Unpassable)
                {
                    writer.Write(MetadataFlags_1010_1099.IsUnpassable);
                }

                if (thing.Unmovable)
                {
                    writer.Write(MetadataFlags_1010_1099.IsUnmovable);
                }

                if (thing.BlockMissiles)
                {
                    writer.Write(MetadataFlags_1010_1099.BlockMissiles);
                }

                if (thing.BlockPathfinder)
                {
                    writer.Write(MetadataFlags_1010_1099.BlockPathfinder);
                }

                if (thing.NoMoveAnimation)
                {
                    writer.Write(MetadataFlags_1010_1099.NoMoveAnimation);
                }

                if (thing.Pickupable)
                {
                    writer.Write(MetadataFlags_1010_1099.Pickupable);
                }

                if (thing.Hangable)
                {
                    writer.Write(MetadataFlags_1010_1099.Hangable);
                }

                if (thing.HookSouth)
                {
                    writer.Write(MetadataFlags_1010_1099.HookSouth);
                }

                if (thing.HookEast)
                {
                    writer.Write(MetadataFlags_1010_1099.HookEast);
                }

                if (thing.Rotatable)
                {
                    writer.Write(MetadataFlags_1010_1099.Rotatable);
                }

                if (thing.DontHide)
                {
                    writer.Write(MetadataFlags_1010_1099.DontHide);
                }

                if (thing.Translucent)
                {
                    writer.Write(MetadataFlags_1010_1099.Translucent);
                }

                if (thing.HasElevation)
                {
                    writer.Write(MetadataFlags_1010_1099.HasElevation);
                    writer.Write(thing.Elevation);

                }
                if (thing.LyingObject)
                {
                    writer.Write(MetadataFlags_1010_1099.LyingObject);
                }

                if (thing.Minimap)
                {
                    writer.Write(MetadataFlags_1010_1099.Minimap);
                    writer.Write(thing.MinimapColor);
                }

                if (thing.IsLensHelp)
                {
                    writer.Write(MetadataFlags_1010_1099.LensHelp);
                    writer.Write(thing.LensHelp);
                }

                if (thing.FullGround)
                {
                    writer.Write(MetadataFlags_1010_1099.FullGround);
                }

                if (thing.IgnoreLook)
                {
                    writer.Write(MetadataFlags_1010_1099.IgnoreLook);
                }

                if (thing.IsCloth)
                {
                    writer.Write(MetadataFlags_1010_1099.Cloth);
                    writer.Write((ushort)thing.ClothSlot);
                }

                if (thing.IsMarketItem)
                {
                    writer.Write(MetadataFlags_1010_1099.Market);
                    writer.Write((ushort)thing.MarketCategory);
                    writer.Write(thing.MarketTradeAs);
                    writer.Write(thing.MarketShowAs);
                    writer.Write((ushort)thing.MarketName.Length);
                    writer.Write(Encoding.UTF8.GetBytes(thing.MarketName));
                    writer.Write(thing.MarketRestrictVocation);
                    writer.Write(thing.MarketRestrictLevel);
                }

                if (thing.HasAction)
                {
                    writer.Write(MetadataFlags_1010_1099.DefaultAction);
                    writer.Write((ushort)thing.DefaultAction);
                }

                if (format >= MetadataFormat.Format_1092)
                {
                    if (thing.Wrappable)
                    {
                        writer.Write(MetadataFlags_1010_1099.Wrappable);
                    }

                    if (thing.Unwrappable)
                    {
                        writer.Write(MetadataFlags_1010_1099.Unwrappable);
                    }
                }

                if (format >= MetadataFormat.Format_1093 && thing.IsTopEffect)
                {
                    writer.Write(MetadataFlags_1010_1099.TopEffect);
                }

                if (thing.Usable)
                {
                    writer.Write(MetadataFlags_1010_1099.Usable);
                }
            }

            if (thing.AnimateAlways)
            {
                writer.Write(MetadataFlags_1010_1099.AnimateAlways);
            }

            if (thing.HasLight)
            {
                writer.Write(MetadataFlags_1010_1099.HasLight);
                writer.Write(thing.LightLevel);
                writer.Write(thing.LightColor);
            }

            if (thing.HasOffset)
            {
                writer.Write(MetadataFlags_1010_1099.HasOffset);
                writer.Write(thing.OffsetX);
                writer.Write(thing.OffsetY);
            }

            // close flags
            writer.Write(MetadataFlags_1010_1099.LastFlag);
            return true;
        }
    }
}
