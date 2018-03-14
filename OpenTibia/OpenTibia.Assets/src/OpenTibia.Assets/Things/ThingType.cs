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

using OpenTibia.Animation;
using System;
using System.Collections.Generic;

namespace OpenTibia.Assets
{
    public enum StackOrder : byte
    {
        Commom = 0,
        Ground = 1,
        Border = 2,
        Bottom = 3,
        Top = 4
    }

    public enum DefaultAction : byte
    {
        None = 0,
        Look = 1,
        Use = 2,
        Open = 3,
        AutowalkHighlight = 4
    }

    public class ThingType
    {
        public ThingType(ushort id, ObjectCategory category)
        {
            ID = id;
            Category = category;
            StackOrder = StackOrder.Commom;
            FrameGroups = new Dictionary<FrameGroupType, FrameGroup>();
        }

        public ThingType(ObjectCategory category) : this(0, category)
        {
            ////
        }

        public ushort ID { get; set; }
        public ObjectCategory Category { get; private set; }
        public StackOrder StackOrder { get; set; }

        public ushort GroundSpeed { get; set; }
        public bool IsContainer { get; set; }
        public bool Stackable { get; set; }
        public bool ForceUse { get; set; }
        public bool MultiUse { get; set; }
        public bool IsFluidContainer { get; set; }
        public bool IsFluid { get; set; }
        public bool Unpassable { get; set; }
        public bool Unmovable { get; set; }
        public bool BlockMissiles { get; set; }
        public bool BlockPathfinder { get; set; }
        public bool NoMoveAnimation { get; set; }
        public bool Pickupable { get; set; }
        public bool Hangable { get; set; }
        public bool HookEast { get; set; }
        public bool HookSouth { get; set; }
        public bool Rotatable { get; set; }
        public bool FullGround { get; set; }
        public bool IgnoreLook { get; set; }
        public bool DontHide { get; set; }
        public bool Translucent { get; set; }
        public bool LyingObject { get; set; }
        public bool AnimateAlways { get; set; }
        public bool Usable { get; set; }
        public bool HasCharges { get; set; }
        public bool FloorChange { get; set; }
        public bool Wrappable { get; set; }
        public bool Unwrappable { get; set; }
        public bool IsTopEffect { get; set; }
        public bool Writable { get; set; }
        public bool WritableOnce { get; set; }
        public ushort MaxTextLength { get; set; }
        public bool HasLight { get; set; }
        public ushort LightLevel { get; set; }
        public ushort LightColor { get; set; }
        public bool HasOffset { get; set; }
        public ushort OffsetX { get; set; }
        public ushort OffsetY { get; set; }
        public bool HasElevation { get; set; }
        public ushort Elevation { get; set; }
        public bool Minimap { get; set; }
        public ushort MinimapColor { get; set; }
        public bool IsLensHelp { get; set; }
        public ushort LensHelp { get; set; }
        public bool IsCloth { get; set; }
        public ClothSlot ClothSlot { get; set; }
        public bool IsMarketItem { get; set; }
        public string MarketName { get; set; }
        public MarketCategory MarketCategory { get; set; }
        public ushort MarketTradeAs { get; set; }
        public ushort MarketShowAs { get; set; }
        public ushort MarketRestrictVocation { get; set; }
        public ushort MarketRestrictLevel { get; set; }
        public bool HasAction { get; set; }
        public DefaultAction DefaultAction { get; set; }

        internal Dictionary<FrameGroupType, FrameGroup> FrameGroups { get; private set; }
        public int FrameGroupCount => FrameGroups.Count;

        public override string ToString()
        {
            if (MarketName != null)
            {
                return ID.ToString() + " - " + MarketName;
            }

            return ID.ToString();
        }

        public bool HasFrameGroup(FrameGroupType type)
        {
            return FrameGroups.ContainsKey(type);
        }

        public FrameGroup GetFrameGroup(FrameGroupType groupType)
        {
            if (FrameGroups.ContainsKey(groupType))
            {
                return FrameGroups[groupType];
            }

            return null;
        }

        public FrameGroup SetFrameGroup(FrameGroupType groupType, FrameGroup group)
        {
            if (groupType == FrameGroupType.Walking && (!FrameGroups.ContainsKey(FrameGroupType.Default) || FrameGroups.Count == 0))
            {
                FrameGroups.Add(FrameGroupType.Default, group);
            }

            if (!FrameGroups.ContainsKey(groupType))
            {
                FrameGroups.Add(groupType, group);
            }

            return group;
        }

        public ThingType Clone()
        {
            ThingType clone = (ThingType)MemberwiseClone();
            clone.FrameGroups = new Dictionary<FrameGroupType, FrameGroup>();

            foreach (KeyValuePair<FrameGroupType, FrameGroup> keyValue in FrameGroups)
            {
                clone.FrameGroups.Add(keyValue.Key, keyValue.Value.Clone());
            }

            return clone;
        }

        public static ThingType Create(ushort id, ObjectCategory category)
        {
            if (category == ObjectCategory.Invalid)
            {
                throw new ArgumentException("Invalid category.");
            }

            ThingType thing = new ThingType(id, category);

            if (category == ObjectCategory.Outfit)
            {
                for (int i = 0; i < 2; i++)
                {
                    FrameGroup group = FrameGroup.Create();
                    group.PatternsX = 4; // directions
                    group.Frames = 3;   // animations
                    group.IsAnimation = true;
                    group.SpriteIDs = new uint[group.GetTotalSprites()];
                    group.FrameDurations = new FrameDuration[group.Frames];

                    for (int f = 0; f < group.Frames; f++)
                    {
                        group.FrameDurations[f] = new FrameDuration(category);
                    }

                    thing.SetFrameGroup((FrameGroupType)i, group);
                }
            }
            else
            {
                FrameGroup group = FrameGroup.Create();

                if (category == ObjectCategory.Missile)
                {
                    group.PatternsX = 3;
                    group.PatternsY = 3;
                    group.SpriteIDs = new uint[group.GetTotalSprites()];
                }

                thing.SetFrameGroup(FrameGroupType.Default, group);
            }

            return thing;
        }

        public static ThingType ToSingleFrameGroup(ThingType thing)
        {
            if (thing.Category != ObjectCategory.Outfit || thing.FrameGroupCount != 2)
            {
                return thing;
            }

            FrameGroup walkingFrameGroup = thing.GetFrameGroup(FrameGroupType.Walking);
            FrameGroup newGroup = walkingFrameGroup.Clone();

            if (walkingFrameGroup.Frames > 1)
            {
                newGroup.Frames = (byte)(newGroup.Frames + 1);
                newGroup.SpriteIDs = new uint[newGroup.GetTotalSprites()];
                newGroup.IsAnimation = true;
                newGroup.FrameDurations = new FrameDuration[newGroup.Frames];

                for (int i = 0; i < newGroup.Frames; i++)
                {
                    if (newGroup.FrameDurations[i] != null)
                    {
                        newGroup.FrameDurations[i] = newGroup.FrameDurations[i];
                    }
                    else
                    {
                        newGroup.FrameDurations[i] = new FrameDuration(ObjectCategory.Outfit);
                    }
                }
            }

            for (byte k = 0; k < thing.FrameGroupCount; k++)
            {
                FrameGroup group = thing.GetFrameGroup((FrameGroupType)k);

                for (byte f = 0; f < group.Frames; f++)
                {
                    for (byte z = 0; z < group.PatternsZ; z++)
                    {
                        for (byte y = 0; y < group.PatternsY; y++)
                        {
                            for (byte x = 0; x < group.PatternsX; x++)
                            {
                                for (byte l = 0; l < group.Layers; l++)
                                {
                                    for (byte w = 0; w < group.Width; w++)
                                    {
                                        for (byte h = 0; h < group.Height; h++)
                                        {
                                            if (k == (byte)FrameGroupType.Default && f == 0)
                                            {
                                                int i = group.GetSpriteIndex(w, h, l, x, y, z, f);
                                                int ni = newGroup.GetSpriteIndex(w, h, l, x, y, z, f);
                                                newGroup.SpriteIDs[ni] = group.SpriteIDs[i];
                                            }
                                            else if (k == (byte)FrameGroupType.Walking)
                                            {
                                                int i = group.GetSpriteIndex(w, h, l, x, y, z, f);
                                                int ni = newGroup.GetSpriteIndex(w, h, l, x, y, z, f + 1);
                                                newGroup.SpriteIDs[ni] = group.SpriteIDs[i];
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            thing.FrameGroups = new Dictionary<FrameGroupType, FrameGroup>();
            thing.FrameGroups.Add(FrameGroupType.Default, newGroup);
            return thing;
        }
    }
}
