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

#region Using Statements
using OpenTibia.Animation;
using OpenTibia.Client.Sprites;
using OpenTibia.Client.Things;
using OpenTibia.Core;
using OpenTibia.Geom;
using OpenTibia.Obd;
using OpenTibia.Utils;
using System;
using System.Drawing;
#endregion

namespace OpenTibia.Client
{
    [Flags]
    public enum ClientFeatures
    {
        None = 0,
        PatternZ = 1 << 0,
        Extended = 1 << 1,
        FrameDurations = 1 << 2,
        FrameGroups = 1 << 3,
        Transparency = 1 << 4
    }

    public interface IClient : IDisposable
    {
        #region | Events |

        event EventHandler ClientLoaded;

        event EventHandler ClientChanged;

        event EventHandler ClientCompiled;

        event EventHandler ClientUnloaded;

        event ProgressHandler ProgressChanged;

        #endregion

        #region | Properties |

        ThingTypeStorage Things { get; }

        SpriteStorage Sprites { get; }

        bool Changed { get; }

        bool Loaded { get; }

        #endregion

        #region | Methods |

        bool CreateEmpty(Core.Version version, ClientFeatures features);

        bool CreateEmpty(Core.Version version);

        bool Load(string datPath, string sprPath, Core.Version version, ClientFeatures features);

        bool Load(string datPath, string sprPath, Core.Version version);

        FrameGroup GetFrameGroup(ushort id, ThingCategory category, FrameGroupType groupType);

        ObjectData GetThingData(ushort id, ThingCategory category, bool singleFrameGroup);

        ObjectData GetThingData(ushort id, ThingCategory category);

        Bitmap GetObjectImage(ushort id, ThingCategory category, FrameGroupType groupType);

        Bitmap GetObjectImage(ushort id, ThingCategory category);

        Bitmap GetObjectImage(ushort id, Direction direction, OutfitData data, bool mount);

        Bitmap GetObjectImage(ushort id, Direction direction, OutfitData data);

        Bitmap GetObjectImage(ThingType thing, FrameGroupType groupType);

        Bitmap GetObjectImage(ThingType thing);

        SpriteSheet GetSpriteSheet(ushort id, ThingCategory category, FrameGroupType groupType);

        SpriteSheet GetSpriteSheet(ushort id, ThingCategory category, FrameGroupType groupType, OutfitData outfitData);

        ThingType[] GetAllItems();

        ThingType[] GetAllOutfits();

        ThingType[] GetAllEffects();

        ThingType[] GetAllMissiles();

        bool Save(string datPath, string sprPath, Core.Version version, ClientFeatures features);

        bool Save(string datPath, string sprPath, Core.Version version);

        bool Save();

        #endregion
    }
}
