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
using OpenTibia.Utilities;
using OpenTibia.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace OpenTibia.Assets
{
    public class AssetsManager : IAssetsManager
    {
        private SpriteCache m_spriteCache;

        public AssetsManager()
        {
            m_spriteCache = new SpriteCache();
        }

        public event EventHandler AssetsLoaded;

        public event EventHandler AssetsChanged;

        public event EventHandler AssetsCompiled;

        public event EventHandler AssetsUnloaded;

        public event ProgressHandler ProgressChanged;

        public ThingTypeStorage Things { get; private set; }

        public SpriteStorage Sprites { get; private set; }

        public bool Changed => Things != null && Sprites != null && (Things.Changed || Sprites.Changed);

        public bool Loaded => Things != null && Sprites != null && Things.Loaded && Sprites.Loaded;

        public bool CreateEmpty(AssetsVersion version, AssetsFeatures features)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            Things = ThingTypeStorage.Create(version, features);
            if (Things == null)
            {
                return false;
            }

            Sprites = SpriteStorage.Create(version, features);
            if (Sprites == null)
            {
                return false;
            }

            Things.ProgressChanged += StorageProgressChanged_Handler;
            Things.StorageChanged += ThingListChanged_Handler;
            Things.StorageCompiled += StorageCompiled_Handler;
            Things.StorageDisposed += StorageDisposed_Handler;
            Sprites.StorageChanged += SpriteListChanged_Handler;
            Sprites.ProgressChanged += StorageProgressChanged_Handler;
            Sprites.StorageCompiled += StorageCompiled_Handler;
            Sprites.StorageDisposed += StorageDisposed_Handler;

            if (Loaded && AssetsLoaded != null)
            {
                AssetsLoaded(this, new EventArgs());
            }

            return Loaded;
        }

        public bool CreateEmpty(AssetsVersion version)
        {
            return CreateEmpty(version, AssetsFeatures.None);
        }

        public bool Load(string datPath, string sprPath, AssetsVersion version, AssetsFeatures features)
        {
            if (datPath == null)
            {
                throw new ArgumentNullException(nameof(datPath));
            }

            if (sprPath == null)
            {
                throw new ArgumentNullException(nameof(sprPath));
            }

            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            Things = ThingTypeStorage.Load(datPath, version, features);
            if (Things == null)
            {
                return false;
            }

            Sprites = SpriteStorage.Load(sprPath, version, features);
            if (Sprites == null)
            {
                return false;
            }

            Things.ProgressChanged += StorageProgressChanged_Handler;
            Things.StorageChanged += ThingListChanged_Handler;
            Things.StorageCompiled += StorageCompiled_Handler;
            Things.StorageDisposed += StorageDisposed_Handler;
            Sprites.StorageChanged += SpriteListChanged_Handler;
            Sprites.ProgressChanged += StorageProgressChanged_Handler;
            Sprites.StorageCompiled += StorageCompiled_Handler;
            Sprites.StorageDisposed += StorageDisposed_Handler;

            if (Loaded && AssetsLoaded != null)
            {
                AssetsLoaded(this, new EventArgs());
            }

            return Loaded;
        }

        public bool Load(string datPath, string sprPath, AssetsVersion version)
        {
            return Load(datPath, sprPath, version, AssetsFeatures.None);
        }

        public FrameGroup GetFrameGroup(ushort id, ThingCategory category, FrameGroupType groupType)
        {
            ThingType type = Things.GetThing(id, category);
            if (type != null)
            {
                return type.GetFrameGroup(groupType);
            }

            return null;
        }

        public ObjectData GetThingData(ushort id, ThingCategory category, bool singleFrameGroup)
        {
            ThingType thing = Things.GetThing(id, category);
            if (thing == null)
            {
                return null;
            }

            if (singleFrameGroup)
            {
                thing = ThingType.ToSingleFrameGroup(thing);
            }

            SpriteGroup spriteGroups = new SpriteGroup();

            Console.WriteLine(thing.FrameGroupCount);

            for (byte i = 0; i < thing.FrameGroupCount; i++)
            {
                FrameGroupType groupType = (FrameGroupType)i;
                FrameGroup frameGroup = thing.GetFrameGroup(groupType);
                int length = frameGroup.SpriteIDs.Length;
                Sprite[] sprites = new Sprite[length];

                for (int s = 0; s < length; s++)
                {
                    sprites[s] = Sprites.GetSprite(frameGroup.SpriteIDs[s]);
                }

                spriteGroups.Add(groupType, sprites);
            }

            return new ObjectData(thing, spriteGroups);
        }

        public ObjectData GetThingData(ushort id, ThingCategory category)
        {
            return GetThingData(id, category, false);
        }

        public Bitmap GetObjectImage(ushort id, ThingCategory category, FrameGroupType groupType)
        {
            ThingType thing = Things.GetThing(id, category);
            if (thing == null)
            {
                return null;
            }

            Bitmap bitmap = m_spriteCache.GetPicture(id, category);
            if (bitmap != null)
            {
                return bitmap;
            }

            FrameGroup group = thing.GetFrameGroup(groupType);
            int width = Sprite.DefaultSize * group.Width;
            int height = Sprite.DefaultSize * group.Height;

            bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapLocker lockBitmap = new BitmapLocker(bitmap);
            lockBitmap.LockBits();

            byte layers = group.Layers;
            byte x = 0;

            if (category == ThingCategory.Outfit)
            {
                layers = 1;
                x = (byte)(2 % group.PatternsX);
            }

            // draw sprite
            for (byte l = 0; l < layers; l++)
            {
                for (byte w = 0; w < group.Width; w++)
                {
                    for (byte h = 0; h < group.Height; h++)
                    {
                        int index = group.GetSpriteIndex(w, h, l, x, 0, 0, 0);
                        uint spriteId = group.SpriteIDs[index];
                        int px = (group.Width - w - 1) * Sprite.DefaultSize;
                        int py = (group.Height - h - 1) * Sprite.DefaultSize;

                        lockBitmap.CopyPixels(Sprites.GetSpriteBitmap(spriteId), px, py);
                    }
                }
            }

            lockBitmap.UnlockBits();
            lockBitmap.Dispose();

            m_spriteCache.SetPicture(id, category, bitmap);
            return bitmap;
        }

        public Bitmap GetObjectImage(ushort id, ThingCategory category)
        {
            return GetObjectImage(id, category, FrameGroupType.Default);
        }

        public Bitmap GetObjectImage(ushort id, Direction direction, OutfitData data, bool mount)
        {
            ThingType thing = Things.GetThing(id, ThingCategory.Outfit);
            if (thing == null)
            {
                return null;
            }

            FrameGroup group = thing.GetFrameGroup(FrameGroupType.Default);
            if (group.Layers < 2)
            {
                return GetObjectImage(id, ThingCategory.Outfit, FrameGroupType.Default);
            }

            int width = Sprite.DefaultSize * group.Width;
            int height = Sprite.DefaultSize * group.Height;
            Bitmap grayBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Bitmap blendBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapLocker grayLocker = new BitmapLocker(grayBitmap);
            BitmapLocker blendLocker = new BitmapLocker(blendBitmap);
            BitmapLocker bitmapLocker = new BitmapLocker(bitmap);

            grayLocker.LockBits();
            blendLocker.LockBits();
            bitmapLocker.LockBits();

            byte x = (byte)((byte)direction % group.PatternsX);
            byte z = mount && group.PatternsZ > 1 ? (byte)1 : (byte)0;

            for (int y = 0; y < group.PatternsY; y++)
            {
                if (y == 0 || (data.Addons & 1 << (y - 1)) != 0)
                {
                    for (byte w = 0; w < group.Width; w++)
                    {
                        for (byte h = 0; h < group.Height; h++)
                        {
                            int index = group.GetSpriteIndex(w, h, 0, x, y, z, 0);
                            uint spriteId = group.SpriteIDs[index];
                            int px = (group.Width - w - 1) * Sprite.DefaultSize;
                            int py = (group.Height - h - 1) * Sprite.DefaultSize;
                            grayLocker.CopyPixels(Sprites.GetSpriteBitmap(spriteId), px, py);

                            index = group.GetSpriteIndex(w, h, 1, x, y, z, 0);
                            spriteId = group.SpriteIDs[index];
                            blendLocker.CopyPixels(Sprites.GetSpriteBitmap(spriteId), px, py);
                        }
                    }

                    bitmapLocker.ColorizePixels(grayLocker.Pixels, blendLocker.Pixels, data.Head, data.Body, data.Legs, data.Feet);
                }
            }

            grayLocker.UnlockBits();
            grayLocker.Dispose();
            blendLocker.UnlockBits();
            blendLocker.Dispose();
            bitmapLocker.UnlockBits();
            bitmapLocker.Dispose();
            grayBitmap.Dispose();
            blendBitmap.Dispose();
            return bitmap;
        }

        public Bitmap GetObjectImage(ushort id, Direction direction, OutfitData data)
        {
            return GetObjectImage(id, direction, data, false);
        }

        public Bitmap GetObjectImage(ThingType thing, FrameGroupType groupType)
        {
            if (thing != null)
            {
                return GetObjectImage(thing.ID, thing.Category, groupType);
            }

            return null;
        }

        public Bitmap GetObjectImage(ThingType thing)
        {
            if (thing != null)
            {
                return GetObjectImage(thing.ID, thing.Category, FrameGroupType.Default);
            }

            return null;
        }

        public SpriteSheet GetSpriteSheet(ushort id, ThingCategory category, FrameGroupType groupType)
        {
            ThingType thing = Things.GetThing(id, category);
            if (thing == null)
            {
                return null;
            }

            FrameGroup group = thing.GetFrameGroup(groupType);
            int totalX = group.PatternsZ * group.PatternsX * group.Layers;
            int totalY = group.Frames * group.PatternsY;
            int bitmapWidth = (totalX * group.Width) * Sprite.DefaultSize;
            int bitmapHeight = (totalY * group.Height) * Sprite.DefaultSize;
            int pixelsWidth = group.Width * Sprite.DefaultSize;
            int pixelsHeight = group.Height * Sprite.DefaultSize;
            Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight, PixelFormat.Format32bppArgb);
            Dictionary<int, Rect> rectList = new Dictionary<int, Rect>();

            BitmapLocker lockBitmap = new BitmapLocker(bitmap);
            lockBitmap.LockBits();

            for (int f = 0; f < group.Frames; f++)
            {
                for (int z = 0; z < group.PatternsZ; z++)
                {
                    for (int y = 0; y < group.PatternsY; y++)
                    {
                        for (int x = 0; x < group.PatternsX; x++)
                        {
                            for (int l = 0; l < group.Layers; l++)
                            {
                                int index = group.GetTextureIndex(l, x, y, z, f);
                                int fx = (index % totalX) * pixelsWidth;
                                int fy = (int)(Math.Floor((decimal)(index / totalX)) * pixelsHeight);
                                rectList.Add(index, new Rect(fx, fy, pixelsWidth, pixelsHeight));

                                for (int w = 0; w < group.Width; w++)
                                {
                                    for (int h = 0; h < group.Height; h++)
                                    {
                                        index = group.GetSpriteIndex(w, h, l, x, y, z, f);
                                        int px = ((group.Width - w - 1) * Sprite.DefaultSize);
                                        int py = ((group.Height - h - 1) * Sprite.DefaultSize);
                                        uint spriteId = group.SpriteIDs[index];
                                        lockBitmap.CopyPixels(Sprites.GetSpriteBitmap(spriteId), px + fx, py + fy);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            lockBitmap.UnlockBits();

            return new SpriteSheet(bitmap, rectList);
        }

        public SpriteSheet GetSpriteSheet(ushort id, ThingCategory category, FrameGroupType groupType, OutfitData outfitData)
        {
            ThingType thing = Things.GetThing(id, category);
            if (thing == null)
            {
                return null;
            }

            SpriteSheet rawSpriteSheet = GetSpriteSheet(id, category, groupType);

            FrameGroup group = thing.GetFrameGroup(groupType);
            if (group.Layers < 2)
            {
                return rawSpriteSheet;
            }

            int totalX = group.PatternsZ * group.PatternsX * group.Layers;
            int totalY = group.Frames * group.PatternsY;
            int bitmapWidth = (totalX * group.Width) * Sprite.DefaultSize;
            int bitmapHeight = (totalY * group.Height) * Sprite.DefaultSize;
            int pixelsWidth = group.Width * Sprite.DefaultSize;
            int pixelsHeight = group.Height * Sprite.DefaultSize;
            Bitmap grayBitmap = new Bitmap(bitmapWidth, bitmapHeight, PixelFormat.Format32bppArgb);
            Bitmap blendBitmap = new Bitmap(bitmapWidth, bitmapHeight, PixelFormat.Format32bppArgb);
            Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight, PixelFormat.Format32bppArgb);
            Dictionary<int, Rect> rectList = new Dictionary<int, Rect>();

            for (int f = 0; f < group.Frames; f++)
            {
                for (int z = 0; z < group.PatternsZ; z++)
                {
                    for (int x = 0; x < group.PatternsX; x++)
                    {
                        int index = (((f % group.Frames * group.PatternsZ + z) * group.PatternsY) * group.PatternsX + x) * group.Layers;
                        rectList[index] = new Rect((z * group.PatternsX + x) * pixelsWidth, f * pixelsHeight, pixelsWidth, pixelsHeight);
                    }
                }
            }

            BitmapLocker grayLocker = new BitmapLocker(grayBitmap);
            BitmapLocker blendLocker = new BitmapLocker(blendBitmap);
            BitmapLocker bitmapLocker = new BitmapLocker(bitmap);

            grayLocker.LockBits();
            blendLocker.LockBits();
            bitmapLocker.LockBits();

            for (int y = 0; y < group.PatternsY; y++)
            {
                if (y == 0 || (outfitData.Addons & 1 << (y - 1)) != 0)
                {
                    for (int f = 0; f < group.Frames; f++)
                    {
                        for (int z = 0; z < group.PatternsZ; z++)
                        {
                            for (int x = 0; x < group.PatternsX; x++)
                            {
                                // gets gray bitmap
                                int i = (((f % group.Frames * group.PatternsZ + z) * group.PatternsY + y) * group.PatternsX + x) * group.Layers;
                                Rect rect = rawSpriteSheet.RectList[i];
                                int rx = rect.X;
                                int ry = rect.Y;
                                int rw = rect.Width;
                                int rh = rect.Height;
                                int index = (((f * group.PatternsZ + z) * group.PatternsY) * group.PatternsX + x) * group.Layers;
                                rect = rectList[index];
                                int px = rect.X;
                                int py = rect.Y;
                                grayLocker.CopyPixels(rawSpriteSheet.Bitmap, rx, ry, rw, rh, px, py);

                                // gets blend bitmap
                                i++;
                                rect = rawSpriteSheet.RectList[i];
                                rx = rect.X;
                                ry = rect.Y;
                                rw = rect.Width;
                                rh = rect.Height;
                                blendLocker.CopyPixels(rawSpriteSheet.Bitmap, rx, ry, rw, rh, px, py);
                            }
                        }
                    }

                    bitmapLocker.ColorizePixels(grayLocker.Pixels, blendLocker.Pixels, outfitData.Head, outfitData.Body, outfitData.Legs, outfitData.Feet);
                }
            }

            grayLocker.UnlockBits();
            grayLocker.Dispose();
            blendLocker.UnlockBits();
            blendLocker.Dispose();
            bitmapLocker.UnlockBits();
            bitmapLocker.Dispose();
            grayBitmap.Dispose();
            blendBitmap.Dispose();
            return new SpriteSheet(bitmap, rectList);
        }

        public ThingType[] GetAllItems()
        {
            return Enumerable.ToArray(Things.Items.Values);
        }

        public ThingType[] GetAllOutfits()
        {
            return Enumerable.ToArray(Things.Outfits.Values);
        }

        public ThingType[] GetAllEffects()
        {
            return Enumerable.ToArray(Things.Effects.Values);
        }

        public ThingType[] GetAllMissiles()
        {
            return Enumerable.ToArray(Things.Missiles.Values);
        }

        public bool Save(string datPath, string sprPath, AssetsVersion version, AssetsFeatures features)
        {
            if (datPath == null)
            {
                throw new ArgumentNullException(nameof(datPath));
            }

            if (sprPath == null)
            {
                throw new ArgumentNullException(nameof(sprPath));
            }

            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (!Things.Save(datPath, version, features))
            {
                return false;
            }

            if (!Sprites.Save(sprPath, version, features))
            {
                return false;
            }

            return true;
        }

        public bool Save(string datPath, string sprPath, AssetsVersion version)
        {
            return Save(datPath, sprPath, version, AssetsFeatures.None);
        }

        public bool Save()
        {
            return Things.Save() && Sprites.Save();
        }

        public bool Unload()
        {
            if (!Loaded)
            {
                return false;
            }

            if (Things != null)
            {
                Things.ProgressChanged -= StorageProgressChanged_Handler;
                Things.StorageChanged -= ThingListChanged_Handler;
                Things.StorageCompiled -= StorageCompiled_Handler;
                Things.StorageDisposed -= StorageDisposed_Handler;
                Things.Dispose();
                Things = null;
            }

            if (Sprites != null)
            {
                Sprites.ProgressChanged -= StorageProgressChanged_Handler;
                Sprites.StorageChanged -= SpriteListChanged_Handler;
                Sprites.StorageCompiled -= StorageCompiled_Handler;
                Sprites.StorageDisposed -= StorageDisposed_Handler;
                Sprites.Dispose();
                Sprites = null;
            }

            m_spriteCache.Clear();

            AssetsUnloaded?.Invoke(this, new EventArgs());

            return true;
        }

        public void Dispose()
        {
            Unload();
        }

        private void StorageCompiled_Handler(IStorage sender)
        {
            if (!Changed && sender == Sprites && AssetsCompiled != null)
            {
                AssetsCompiled(this, new EventArgs());
            }
        }

        private void StorageDisposed_Handler(IStorage sender)
        {
            if (!Loaded && AssetsUnloaded != null)
            {
                AssetsUnloaded(this, new EventArgs());
            }
        }

        private void ThingListChanged_Handler(object sender, ThingListChangedArgs e)
        {
            AssetsChanged?.Invoke(this, new EventArgs());
        }

        private void SpriteListChanged_Handler(object sender, SpriteListChangedArgs e)
        {
            AssetsChanged?.Invoke(this, new EventArgs());
        }

        private void StorageProgressChanged_Handler(object sender, int percentage)
        {
            if (ProgressChanged != null)
            {
                if (sender == Things)
                {
                    ProgressChanged(this, percentage / 2);
                }
                else
                {
                    ProgressChanged(this, (percentage / 2) + 50);
                }
            }
        }
    }
}
