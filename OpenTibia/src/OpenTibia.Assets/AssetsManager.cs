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
using System.Runtime.InteropServices;

namespace OpenTibia.Assets
{
    public delegate void AssetsHandler(AssetsManager assets);

    public class AssetsManager : IAssetsManager
    {
        private SpriteCache m_spriteCache;
        private Rectangle m_rectangle;

        private string m_metadataPath;
        private string m_spritesPath;
        private AssetsVersion m_version;
        private AssetsFeatures m_features;
        private SpritePixelFormat m_pixelFormat;
        private int m_processIndex;

        public AssetsManager()
        {
            m_spriteCache = new SpriteCache();
            m_rectangle = new Rectangle(0, 0, Sprite.DefaultSize, Sprite.DefaultSize);
        }

        public event AssetsHandler AssetsLoaded;

        public event AssetsHandler AssetsChanged;

        public event AssetsHandler AssetsCompiled;

        public event ProgressHandler ProgressChanged;

        public ThingTypeStorage Objects { get; private set; }

        public SpriteStorage Sprites { get; private set; }

        public bool Changed => Objects != null && Sprites != null && (Objects.Changed || Sprites.Changed);

        public bool Loaded => Objects != null && Sprites != null && Objects.Loaded && Sprites.Loaded;

        public bool Disposed { get; private set; }

        public void CreateEmpty(AssetsVersion version, AssetsFeatures features, SpritePixelFormat pixelFormat)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            m_version = version ?? throw new ArgumentNullException(nameof(version));
            m_features = features;
            m_pixelFormat = pixelFormat;

            Objects = new ThingTypeStorage();
            Objects.StorageLoaded += StorageLoaded_Handler;
            Objects.StorageChanged += ThingListChanged_Handler;
            Objects.StorageCompiled += StorageCompiled_Handler;
            Objects.ProgressChanged += StorageProgressChanged_Handler;

            Sprites = new SpriteStorage(m_pixelFormat);
            Sprites.StorageLoaded += StorageLoaded_Handler;
            Sprites.StorageChanged += SpriteListChanged_Handler;
            Sprites.StorageCompiled += StorageCompiled_Handler;
            Sprites.ProgressChanged += StorageProgressChanged_Handler;

            m_processIndex = -1;
            ProcessNext();
        }

        public void CreateEmpty(AssetsVersion version, AssetsFeatures features)
            => CreateEmpty(version, features, SpritePixelFormat.Bgra);

        public void CreateEmpty(AssetsVersion version)
            => CreateEmpty(version, AssetsFeatures.None);

        public void Load(string metadataPath, string spritesPath, AssetsVersion version, AssetsFeatures features, SpritePixelFormat pixelFormat)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            m_metadataPath = metadataPath ?? throw new ArgumentNullException(nameof(metadataPath));
            m_spritesPath = spritesPath ?? throw new ArgumentNullException(nameof(spritesPath));
            m_version = version ?? throw new ArgumentNullException(nameof(version));
            m_features = features;
            m_pixelFormat = pixelFormat;

            Objects = new ThingTypeStorage();
            Objects.StorageLoaded += StorageLoaded_Handler;
            Objects.StorageChanged += ThingListChanged_Handler;
            Objects.StorageCompiled += StorageCompiled_Handler;
            Objects.ProgressChanged += StorageProgressChanged_Handler;

            Sprites = new SpriteStorage(pixelFormat);
            Sprites.StorageLoaded += StorageLoaded_Handler;
            Sprites.StorageChanged += SpriteListChanged_Handler;
            Sprites.StorageCompiled += StorageCompiled_Handler;
            Sprites.ProgressChanged += StorageProgressChanged_Handler;

            m_processIndex = -1;
            ProcessNext();
        }

        public void Load(string metadataPath, string spritesPath, AssetsVersion version, AssetsFeatures features)
            => Load(metadataPath, spritesPath, version, features, SpritePixelFormat.Bgra);

        public void Load(string metadataPath, string spritesPath, AssetsVersion version)
            => Load(metadataPath, spritesPath, version, AssetsFeatures.None);

        public FrameGroup GetFrameGroup(ushort id, ObjectCategory category, FrameGroupType groupType)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            ThingType type = Objects.GetThing(id, category);
            if (type != null)
            {
                return type.GetFrameGroup(groupType);
            }

            return null;
        }

        public ObjectData GetThingData(ushort id, ObjectCategory category, bool singleFrameGroup)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            ThingType thing = Objects.GetThing(id, category);
            if (thing == null)
            {
                return null;
            }

            if (singleFrameGroup)
            {
                thing = ThingType.ToSingleFrameGroup(thing);
            }

            SpriteGroup spriteGroups = new SpriteGroup();

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

        public ObjectData GetThingData(ushort id, ObjectCategory category)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return GetThingData(id, category, false);
        }

        public Bitmap GetSpriteBitmap(uint id)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            if (!Loaded)
            {
                return null;
            }

            Sprite sprite = Sprites.GetSprite(id);
            if (sprite == null)
            {
                return null;
            }

            Bitmap bitmap = new Bitmap(Sprite.DefaultSize, Sprite.DefaultSize, PixelFormat.Format32bppArgb);

            if (!sprite.IsEmpty)
            {
                BitmapData bitmapData = bitmap.LockBits(m_rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                Marshal.Copy(sprite.Pixels, 0, bitmapData.Scan0, sprite.Pixels.Length);
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        public Bitmap GetObjectImage(ushort id, ObjectCategory category, FrameGroupType groupType)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            ThingType thing = Objects.GetThing(id, category);
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

            if (category == ObjectCategory.Outfit)
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

                        lockBitmap.CopyPixels(GetSpriteBitmap(spriteId), px, py);
                    }
                }
            }

            lockBitmap.UnlockBits();
            lockBitmap.Dispose();

            m_spriteCache.SetPicture(id, category, bitmap);
            return bitmap;
        }

        public Bitmap GetObjectImage(ushort id, ObjectCategory category)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return GetObjectImage(id, category, FrameGroupType.Default);
        }

        public Bitmap GetObjectImage(ushort id, Direction direction, OutfitData data, bool mount)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            ThingType thing = Objects.GetThing(id, ObjectCategory.Outfit);
            if (thing == null)
            {
                return null;
            }

            FrameGroup group = thing.GetFrameGroup(FrameGroupType.Default);
            if (group.Layers < 2)
            {
                return GetObjectImage(id, ObjectCategory.Outfit, FrameGroupType.Default);
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
                            grayLocker.CopyPixels(GetSpriteBitmap(spriteId), px, py);

                            index = group.GetSpriteIndex(w, h, 1, x, y, z, 0);
                            spriteId = group.SpriteIDs[index];
                            blendLocker.CopyPixels(GetSpriteBitmap(spriteId), px, py);
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
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return GetObjectImage(id, direction, data, false);
        }

        public Bitmap GetObjectImage(ThingType thing, FrameGroupType groupType)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            if (thing != null)
            {
                return GetObjectImage(thing.ID, thing.Category, groupType);
            }

            return null;
        }

        public Bitmap GetObjectImage(ThingType thing)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            if (thing != null)
            {
                return GetObjectImage(thing.ID, thing.Category, FrameGroupType.Default);
            }

            return null;
        }

        public SpriteSheet GetSpriteSheet(ushort id, ObjectCategory category, FrameGroupType groupType)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            ThingType thing = Objects.GetThing(id, category);
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
                                        lockBitmap.CopyPixels(GetSpriteBitmap(spriteId), px + fx, py + fy);
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

        public SpriteSheet GetSpriteSheet(ushort id, ObjectCategory category, FrameGroupType groupType, OutfitData outfitData)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            ThingType thing = Objects.GetThing(id, category);
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
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return Enumerable.ToArray(Objects.Items.Values);
        }

        public ThingType[] GetAllOutfits()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return Enumerable.ToArray(Objects.Outfits.Values);
        }

        public ThingType[] GetAllEffects()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return Enumerable.ToArray(Objects.Effects.Values);
        }

        public ThingType[] GetAllMissiles()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return Enumerable.ToArray(Objects.Missiles.Values);
        }

        public bool Save(string datPath, string sprPath, AssetsVersion version, AssetsFeatures features)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

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

            if (!Objects.Save(datPath, version, features))
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
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return Save(datPath, sprPath, version, AssetsFeatures.None);
        }

        public bool Save()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(AssetsManager));
            }

            return Objects.Save() && Sprites.Save();
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;

            if (Objects != null)
            {
                Objects.StorageLoaded -= StorageLoaded_Handler;
                Objects.StorageChanged -= ThingListChanged_Handler;
                Objects.StorageCompiled -= StorageCompiled_Handler;
                Objects.ProgressChanged -= StorageProgressChanged_Handler;
                Objects.Dispose();
                Objects = null;
            }

            if (Sprites != null)
            {
                Sprites.StorageLoaded -= StorageLoaded_Handler;
                Sprites.StorageChanged -= SpriteListChanged_Handler;
                Sprites.StorageCompiled -= StorageCompiled_Handler;
                Sprites.ProgressChanged -= StorageProgressChanged_Handler;
                Sprites.Dispose();
                Sprites = null;
            }

            m_spriteCache.Clear();
            m_spriteCache = null;
        }

        private void ProcessNext()
        {
            m_processIndex++;
            if (m_processIndex == 0)
            {
                if (m_metadataPath == null)
                {
                    Objects.Create(m_version, m_features);
                }
                else
                {
                    Objects.Load(m_metadataPath, m_version, m_features);
                }
            }
            else if (m_processIndex == 1)
            {
                if (m_spritesPath == null)
                {
                    Sprites.Create(m_version, m_features);
                }
                else
                {
                    Sprites.Load(m_spritesPath, m_version, m_features);
                }
            }
            else
            {
                AssetsLoaded?.Invoke(this);
            }
        }

        private void StorageLoaded_Handler(IStorage sender)
        {
            ProcessNext();
        }

        private void StorageCompiled_Handler(IStorage sender)
        {
            if (!Changed && sender == Sprites && AssetsCompiled != null)
            {
                AssetsCompiled(this);
            }
        }

        private void ThingListChanged_Handler(object sender, ThingListChangedArgs e)
        {
            AssetsChanged?.Invoke(this);
        }

        private void SpriteListChanged_Handler(object sender, SpriteListChangedArgs e)
        {
            AssetsChanged?.Invoke(this);
        }

        private void StorageProgressChanged_Handler(object sender, int percentage)
        {
            if (ProgressChanged != null)
            {
                if (sender == Objects)
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
