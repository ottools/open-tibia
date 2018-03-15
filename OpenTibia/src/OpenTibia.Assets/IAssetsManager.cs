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
using OpenTibia.Common;
using OpenTibia.Geom;
using OpenTibia.Obd;
using OpenTibia.Utility;
using System;
using System.Drawing;

namespace OpenTibia.Assets
{
    public interface IAssetsManager : IDisposable
    {
        event AssetsHandler AssetsLoaded;

        event AssetsHandler AssetsChanged;

        event AssetsHandler AssetsCompiled;

        event ProgressHandler ProgressChanged;

        ThingTypeStorage Objects { get; }

        SpriteStorage Sprites { get; }

        bool Changed { get; }

        bool Loaded { get; }

        void CreateEmpty(AssetsVersion version, AssetsFeatures features, SpritePixelFormat pixelFormat);

        void CreateEmpty(AssetsVersion version, AssetsFeatures features);

        void CreateEmpty(AssetsVersion version);

        void Load(string metadataPath, string spritesPath, AssetsVersion version, AssetsFeatures features, SpritePixelFormat pixelFormat);

        void Load(string metadataPath, string spritesPath, AssetsVersion version, AssetsFeatures features);

        void Load(string metadataPath, string spritesPath, AssetsVersion version);

        FrameGroup GetFrameGroup(ushort id, ObjectCategory category, FrameGroupType groupType);

        ObjectData GetThingData(ushort id, ObjectCategory category, bool singleFrameGroup);

        ObjectData GetThingData(ushort id, ObjectCategory category);

        Bitmap GetObjectImage(ushort id, ObjectCategory category, FrameGroupType groupType);

        Bitmap GetObjectImage(ushort id, ObjectCategory category);

        Bitmap GetObjectImage(ushort id, Direction direction, OutfitData data, bool mount);

        Bitmap GetObjectImage(ushort id, Direction direction, OutfitData data);

        Bitmap GetObjectImage(ThingType thing, FrameGroupType groupType);

        Bitmap GetObjectImage(ThingType thing);

        SpriteSheet GetSpriteSheet(ushort id, ObjectCategory category, FrameGroupType groupType);

        SpriteSheet GetSpriteSheet(ushort id, ObjectCategory category, FrameGroupType groupType, OutfitData outfitData);

        ThingType[] GetAllItems();

        ThingType[] GetAllOutfits();

        ThingType[] GetAllEffects();

        ThingType[] GetAllMissiles();

        bool Save(string datPath, string sprPath, AssetsVersion version, AssetsFeatures features);

        bool Save(string datPath, string sprPath, AssetsVersion version);

        bool Save();
    }
}
