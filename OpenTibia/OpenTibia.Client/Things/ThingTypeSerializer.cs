#region Licence
/**
* Copyright (C) 2015 Open Tibia Tools <https://github.com/ottools/open-tibia>
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

#region Using Statements
using OpenTibia.Animation;
using OpenTibia.Client.Sprites;
using OpenTibia.IO;
using System;
using System.IO;
using System.Text;
#endregion

namespace OpenTibia.Client.Things
{
    public static class ThingTypeSerializer
    {
        #region | Public Static Methods |

        public static bool ReadProperties(ThingType thing, DatFormat format, BinaryReader reader)
        {
            if (format >= DatFormat.Format_1010)
            {
                if (!ReadProperties_1010_1093(thing, reader, format))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ReadTexturePatterns(ThingType thing, ClientFeatures features, BinaryReader reader)
        {
            bool patternZEnabled = (features & ClientFeatures.PatternZ) == ClientFeatures.PatternZ;
            bool extendedEnabled = (features & ClientFeatures.Extended) == ClientFeatures.Extended;
            bool frameDurationsEnabled = (features & ClientFeatures.FrameDurations) == ClientFeatures.FrameDurations;
            bool frameGroupsEnabled = (features & ClientFeatures.FrameGroups) == ClientFeatures.FrameGroups;

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
                group.PatternX = reader.ReadByte();
                group.PatternY = reader.ReadByte();
                group.PatternZ = patternZEnabled ? reader.ReadByte() : (byte)1;
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

        public static bool WriteProperties(ThingType thing, DatFormat format, FlagsBinaryWriter writer)
        {
            if (format >= DatFormat.Format_1010)
            {
                if (!WriteProperties_1010_1093(thing, writer, format))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool WriteTexturePatterns(ThingType thing, ClientFeatures features, BinaryWriter writer)
        {
            bool patternZEnabled = (features & ClientFeatures.PatternZ) == ClientFeatures.PatternZ;
            bool extendedEnabled = (features & ClientFeatures.Extended) == ClientFeatures.Extended;
            bool frameDurationsEnabled = (features & ClientFeatures.FrameDurations) == ClientFeatures.FrameDurations;
            bool frameGroupsEnabled = (features & ClientFeatures.FrameGroups) == ClientFeatures.FrameGroups;
            byte groupCount = 1;

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
                writer.Write(group.PatternX);   // write pattern X
                writer.Write(group.PatternY);   // write pattern Y

                if (patternZEnabled)
                {
                    writer.Write(group.PatternZ); // write pattern Z
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

        #endregion

        #region | Private Static Methods |

        private static bool ReadProperties_1010_1093(ThingType thing, BinaryReader reader, DatFormat format)
        {
            DatFlags1010 flag;

            do
            {
                flag = (DatFlags1010)reader.ReadByte();

                if (flag == DatFlags1010.LastFlag)
                {
                    break;
                }

                switch (flag)
                {
                    case DatFlags1010.Ground: // 0x00
                        thing.StackOrder = StackOrder.Ground;
                        thing.GroundSpeed = reader.ReadUInt16();
                        break;

                    case DatFlags1010.GroundBorder: // 0x01
                        thing.StackOrder = StackOrder.Border;
                        break;

                    case DatFlags1010.OnBottom: // 0x02
                        thing.StackOrder = StackOrder.Bottom;
                        break;

                    case DatFlags1010.OnTop: // 0x03
                        thing.StackOrder = StackOrder.Top;
                        break;

                    case DatFlags1010.Container: // 0x04
                        thing.IsContainer = true;
                        break;

                    case DatFlags1010.Stackable:
                        thing.Stackable = true;
                        break;

                    case DatFlags1010.ForceUse:
                        thing.ForceUse = true;
                        break;

                    case DatFlags1010.MultiUse:
                        thing.MultiUse = true;
                        break;

                    case DatFlags1010.Writable:
                        thing.Writable = true;
                        thing.MaxTextLength = reader.ReadUInt16();
                        break;

                    case DatFlags1010.WritableOnce:
                        thing.WritableOnce = true;
                        thing.MaxTextLength = reader.ReadUInt16();
                        break;

                    case DatFlags1010.FluidContainer:
                        thing.IsFluidContainer = true;
                        break;

                    case DatFlags1010.Fluid:
                        thing.IsFluid = true;
                        break;

                    case DatFlags1010.IsUnpassable:
                        thing.Unpassable = true;
                        break;

                    case DatFlags1010.IsUnmovable:
                        thing.Unmovable = true;
                        break;

                    case DatFlags1010.BlockMissiles:
                        thing.BlockMissiles = true;
                        break;

                    case DatFlags1010.BlockPathfinder:
                        thing.BlockPathfinder = true;
                        break;

                    case DatFlags1010.NoMoveAnimation: // 0x10
                        thing.NoMoveAnimation = true;
                        break;

                    case DatFlags1010.Pickupable:
                        thing.Pickupable = true;
                        break;

                    case DatFlags1010.Hangable:
                        thing.Hangable = true;
                        break;

                    case DatFlags1010.HookSouth:
                        thing.HookSouth = true;
                        break;

                    case DatFlags1010.HookEast:
                        thing.HookEast = true;
                        break;

                    case DatFlags1010.Rotatable:
                        thing.Rotatable = true;
                        break;

                    case DatFlags1010.HasLight:
                        thing.HasLight = true;
                        thing.LightLevel = reader.ReadUInt16();
                        thing.LightColor = reader.ReadUInt16();
                        break;

                    case DatFlags1010.DontHide:
                        thing.DontHide = true;
                        break;

                    case DatFlags1010.Translucent:
                        thing.Translucent = true;
                        break;

                    case DatFlags1010.HasOffset:
                        thing.HasOffset = true;
                        thing.OffsetX = reader.ReadUInt16();
                        thing.OffsetY = reader.ReadUInt16();
                        break;

                    case DatFlags1010.HasElevation:
                        thing.HasElevation = true;
                        thing.Elevation = reader.ReadUInt16();
                        break;

                    case DatFlags1010.LyingObject:
                        thing.LyingObject = true;
                        break;

                    case DatFlags1010.Minimap:
                        thing.Minimap = true;
                        thing.MinimapColor = reader.ReadUInt16();
                        break;

                    case DatFlags1010.AnimateAlways:
                        thing.AnimateAlways = true;
                        break;

                    case DatFlags1010.LensHelp:
                        thing.IsLensHelp = true;
                        thing.LensHelp = reader.ReadUInt16();
                        break;

                    case DatFlags1010.FullGround:
                        thing.FullGround = true;
                        break;

                    case DatFlags1010.IgnoreLook:
                        thing.IgnoreLook = true;
                        break;

                    case DatFlags1010.Cloth:
                        thing.IsCloth = true;
                        thing.ClothSlot = (ClothSlot)reader.ReadUInt16();
                        break;

                    case DatFlags1010.Market:
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

                    case DatFlags1010.DefaultAction:
                        thing.HasAction = true;
                        thing.DefaultAction = (DefaultAction)reader.ReadUInt16();
                        break;

                    case DatFlags1010.Wrappable:
                        thing.Wrappable = true;
                        break;

                    case DatFlags1010.Unwrappable:
                        thing.Unwrappable = true;
                        break;

                    case DatFlags1010.TopEffect:
                        thing.IsTopEffect = true;
                        break;

                    case DatFlags1010.Usable:
                        thing.Usable = true;
                        break;

                    default:
                        throw new Exception(string.Format("Error while parsing, unknown flag 0x{0:X} at object id {1}, category {2}.", flag, thing.ID, thing.Category));
                }
            }
            while (flag != DatFlags1010.LastFlag);

            return true;
        }

        private static bool WriteProperties_1010_1093(ThingType thing, FlagsBinaryWriter writer, DatFormat format)
        {
            if (thing.Category == ThingCategory.Item)
            {
                if (thing.StackOrder == StackOrder.Ground)
                {
                    writer.Write(DatFlags1010.Ground);
                    writer.Write(thing.GroundSpeed);
                }
                else if (thing.StackOrder == StackOrder.Border)
                {
                    writer.Write(DatFlags1010.GroundBorder);
                }
                else if (thing.StackOrder == StackOrder.Bottom)
                {
                    writer.Write(DatFlags1010.OnBottom);
                }
                else if (thing.StackOrder == StackOrder.Top)
                {
                    writer.Write(DatFlags1010.OnTop);
                }

                if (thing.IsContainer)
                {
                    writer.Write(DatFlags1010.Container);
                }

                if (thing.Stackable)
                {
                    writer.Write(DatFlags1010.Stackable);
                }

                if (thing.ForceUse)
                {
                    writer.Write(DatFlags1010.ForceUse);
                }

                if (thing.MultiUse)
                {
                    writer.Write(DatFlags1010.MultiUse);
                }

                if (thing.Writable)
                {
                    writer.Write(DatFlags1010.Writable);
                    writer.Write(thing.MaxTextLength);
                }

                if (thing.WritableOnce)
                {
                    writer.Write(DatFlags1010.WritableOnce);
                    writer.Write(thing.MaxTextLength);
                }

                if (thing.IsFluidContainer)
                {
                    writer.Write(DatFlags1010.FluidContainer);
                }

                if (thing.IsFluid)
                {
                    writer.Write(DatFlags1010.Fluid);
                }

                if (thing.Unpassable)
                {
                    writer.Write(DatFlags1010.IsUnpassable);
                }

                if (thing.Unmovable)
                {
                    writer.Write(DatFlags1010.IsUnmovable);
                }

                if (thing.BlockMissiles)
                {
                    writer.Write(DatFlags1010.BlockMissiles);
                }

                if (thing.BlockPathfinder)
                {
                    writer.Write(DatFlags1010.BlockPathfinder);
                }

                if (thing.NoMoveAnimation)
                {
                    writer.Write(DatFlags1010.NoMoveAnimation);
                }

                if (thing.Pickupable)
                {
                    writer.Write(DatFlags1010.Pickupable);
                }

                if (thing.Hangable)
                {
                    writer.Write(DatFlags1010.Hangable);
                }

                if (thing.HookSouth)
                {
                    writer.Write(DatFlags1010.HookSouth);
                }

                if (thing.HookEast)
                {
                    writer.Write(DatFlags1010.HookEast);
                }

                if (thing.Rotatable)
                {
                    writer.Write(DatFlags1010.Rotatable);
                }

                if (thing.DontHide)
                {
                    writer.Write(DatFlags1010.DontHide);
                }

                if (thing.Translucent)
                {
                    writer.Write(DatFlags1010.Translucent);
                }

                if (thing.HasElevation)
                {
                    writer.Write(DatFlags1010.HasElevation);
                    writer.Write(thing.Elevation);

                }
                if (thing.LyingObject)
                {
                    writer.Write(DatFlags1010.LyingObject);
                }

                if (thing.Minimap)
                {
                    writer.Write(DatFlags1010.Minimap);
                    writer.Write(thing.MinimapColor);
                }

                if (thing.IsLensHelp)
                {
                    writer.Write(DatFlags1010.LensHelp);
                    writer.Write(thing.LensHelp);
                }

                if (thing.FullGround)
                {
                    writer.Write(DatFlags1010.FullGround);
                }

                if (thing.IgnoreLook)
                {
                    writer.Write(DatFlags1010.IgnoreLook);
                }

                if (thing.IsCloth)
                {
                    writer.Write(DatFlags1010.Cloth);
                    writer.Write((ushort)thing.ClothSlot);
                }

                if (thing.IsMarketItem)
                {
                    writer.Write(DatFlags1010.Market);
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
                    writer.Write(DatFlags1010.DefaultAction);
                    writer.Write((ushort)thing.DefaultAction);
                }

                if (format >= DatFormat.Format_1092)
                {
                    if (thing.Wrappable)
                    {
                        writer.Write(DatFlags1010.Wrappable);
                    }

                    if (thing.Unwrappable)
                    {
                        writer.Write(DatFlags1010.Unwrappable);
                    }
                }

                if (format >= DatFormat.Format_1093 && thing.IsTopEffect)
                {
                    writer.Write(DatFlags1010.TopEffect);
                }

                if (thing.Usable)
                {
                    writer.Write(DatFlags1010.Usable);
                }
            }

            if (thing.AnimateAlways)
            {
                writer.Write(DatFlags1010.AnimateAlways);
            }

            if (thing.HasLight)
            {
                writer.Write(DatFlags1010.HasLight);
                writer.Write(thing.LightLevel);
                writer.Write(thing.LightColor);
            }

            if (thing.HasOffset)
            {
                writer.Write(DatFlags1010.HasOffset);
                writer.Write(thing.OffsetX);
                writer.Write(thing.OffsetY);
            }

            // close flags
            writer.Write(DatFlags1010.LastFlag);
            return true;
        }

        #endregion
    }
}
