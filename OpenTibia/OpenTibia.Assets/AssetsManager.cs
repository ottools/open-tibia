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
using OpenTibia.Collections;
using OpenTibia.Common;
using OpenTibia.Geom;
using OpenTibia.Obd;
using OpenTibia.Utilities;
using OpenTibia.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
#endregion

namespace OpenTibia.Assets
{
    public class AssetsManager : IAssetsManager
    {
        #region | Private properties |

        private readonly OutfitData outfitDataHelper = new OutfitData();
        private SpriteCache spriteCache;

        #endregion

        #region | Constructor |

        public AssetsManager()
        {
            this.spriteCache = new SpriteCache();
        }

        #endregion

        #region | Events |

        public event EventHandler ClientLoaded;

        public event EventHandler ClientChanged;

        public event EventHandler ClientCompiled;

        public event EventHandler ClientUnloaded;

        public event ProgressHandler ProgressChanged;

        #endregion

        #region | Public Properties |

        public ThingTypeStorage Things { get; private set; }

        public SpriteStorage Sprites { get; private set; }

        public bool Changed
        {
            get
            {
                return this.Things != null && this.Sprites != null && (this.Things.Changed || this.Sprites.Changed);
            }
        }

        public bool Loaded
        {
            get
            {
                return this.Things != null && this.Sprites != null && this.Things.Loaded && this.Sprites.Loaded;
            }
        }

        #endregion

        #region | Public Methods |

        public bool CreateEmpty(AssetsVersion version, AssetsFeatures features)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            this.Things = ThingTypeStorage.Create(version, features);
            if (this.Things == null)
            {
                return false;
            }

            this.Sprites = SpriteStorage.Create(version, features);
            if (this.Sprites == null)
            {
                return false;
            }

            this.Things.ProgressChanged += new ProgressHandler(this.StorageProgressChanged_Handler);
            this.Things.StorageChanged += new ThingListChangedHandler(this.ThingListChanged_Handler);
            this.Things.StorageCompiled += new StorageHandler(this.StorageCompiled_Handler);
            this.Things.StorageDisposed += new StorageHandler(this.StorageDisposed_Handler);
            this.Sprites.StorageChanged += new SpriteListChangedHandler(this.SpriteListChanged_Handler);
            this.Sprites.ProgressChanged += new ProgressHandler(this.StorageProgressChanged_Handler);
            this.Sprites.StorageCompiled += new StorageHandler(this.StorageCompiled_Handler);
            this.Sprites.StorageDisposed += new StorageHandler(this.StorageDisposed_Handler);

            if (this.Loaded && this.ClientLoaded != null)
            {
                this.ClientLoaded(this, new EventArgs());
            }

            return this.Loaded;
        }

        public bool CreateEmpty(AssetsVersion version)
        {
            return this.CreateEmpty(version, AssetsFeatures.None);
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

            this.Things = ThingTypeStorage.Load(datPath, version, features);
            if (this.Things == null)
            {
                return false;
            }

            this.Sprites = SpriteStorage.Load(sprPath, version, features);
            if (this.Sprites == null)
            {
                return false;
            }

            this.Things.ProgressChanged += new ProgressHandler(this.StorageProgressChanged_Handler);
            this.Things.StorageChanged += new ThingListChangedHandler(this.ThingListChanged_Handler);
            this.Things.StorageCompiled += new StorageHandler(this.StorageCompiled_Handler);
            this.Things.StorageDisposed += new StorageHandler(this.StorageDisposed_Handler);
            this.Sprites.StorageChanged += new SpriteListChangedHandler(this.SpriteListChanged_Handler);
            this.Sprites.ProgressChanged += new ProgressHandler(this.StorageProgressChanged_Handler);
            this.Sprites.StorageCompiled += new StorageHandler(this.StorageCompiled_Handler);
            this.Sprites.StorageDisposed += new StorageHandler(this.StorageDisposed_Handler);

            if (this.Loaded && this.ClientLoaded != null)
            {
                this.ClientLoaded(this, new EventArgs());
            }

            return this.Loaded;
        }

        public bool Load(string datPath, string sprPath, AssetsVersion version)
        {
            return this.Load(datPath, sprPath, version, AssetsFeatures.None);
        }

        public FrameGroup GetFrameGroup(ushort id, ThingCategory category, FrameGroupType groupType)
        {
            ThingType type = this.Things.GetThing(id, category);
            if (type != null)
            {
                return type.GetFrameGroup(groupType);
            }

            return null;
        }

        public ObjectData GetThingData(ushort id, ThingCategory category, bool singleFrameGroup)
        {
            ThingType thing = this.Things.GetThing(id, category);
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
                    sprites[s] = this.Sprites.GetSprite(frameGroup.SpriteIDs[s]);
                }

                spriteGroups.Add(groupType, sprites);
            }

            return new ObjectData(thing, spriteGroups);
        }

        public ObjectData GetThingData(ushort id, ThingCategory category)
        {
            return this.GetThingData(id, category, false);
        }

        public Bitmap GetObjectImage(ushort id, ThingCategory category, FrameGroupType groupType)
        {
            ThingType thing = this.Things.GetThing(id, category);
            if (thing == null)
            {
                return null;
            }

            Bitmap bitmap = this.spriteCache.GetPicture(id, category);
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
                x = (byte)(2 % group.PatternX);
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

                        lockBitmap.CopyPixels(this.Sprites.GetSpriteBitmap(spriteId), px, py);
                    }
                }
            }

            lockBitmap.UnlockBits();
            lockBitmap.Dispose();

            this.spriteCache.SetPicture(id, category, bitmap);
            return bitmap;
        }

        public Bitmap GetObjectImage(ushort id, ThingCategory category)
        {
            return this.GetObjectImage(id, category, FrameGroupType.Default);
        }

        public Bitmap GetObjectImage(ushort id, Direction direction, OutfitData data, bool mount)
        {
            ThingType thing = this.Things.GetThing(id, ThingCategory.Outfit);
            if (thing == null)
            {
                return null;
            }

            FrameGroup group = thing.GetFrameGroup(FrameGroupType.Default);
            if (group.Layers < 2)
            {
                return this.GetObjectImage(id, ThingCategory.Outfit, FrameGroupType.Default);
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

            byte x = (byte)((byte)direction % group.PatternX);
            byte z = mount && group.PatternZ > 1 ? (byte)1 : (byte)0;

            for (int y = 0; y < group.PatternY; y++)
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
                            grayLocker.CopyPixels(this.Sprites.GetSpriteBitmap(spriteId), px, py);

                            index = group.GetSpriteIndex(w, h, 1, x, y, z, 0);
                            spriteId = group.SpriteIDs[index];
                            blendLocker.CopyPixels(this.Sprites.GetSpriteBitmap(spriteId), px, py);
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
            return this.GetObjectImage(id, direction, data, false);
        }

        public Bitmap GetObjectImage(ThingType thing, FrameGroupType groupType)
        {
            if (thing != null)
            {
                return this.GetObjectImage(thing.ID, thing.Category, groupType);
            }

            return null;
        }

        public Bitmap GetObjectImage(ThingType thing)
        {
            if (thing != null)
            {
                return this.GetObjectImage(thing.ID, thing.Category, FrameGroupType.Default);
            }

            return null;
        }

        public SpriteSheet GetSpriteSheet(ushort id, ThingCategory category, FrameGroupType groupType)
        {
            ThingType thing = this.Things.GetThing(id, category);
            if (thing == null)
            {
                return null;
            }

            FrameGroup group = thing.GetFrameGroup(groupType);
            int totalX = group.PatternZ * group.PatternX * group.Layers;
            int totalY = group.Frames * group.PatternY;
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
                for (int z = 0; z < group.PatternZ; z++)
                {
                    for (int y = 0; y < group.PatternY; y++)
                    {
                        for (int x = 0; x < group.PatternX; x++)
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
                                        lockBitmap.CopyPixels(this.Sprites.GetSpriteBitmap(spriteId), px + fx, py + fy);
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
            ThingType thing = this.Things.GetThing(id, category);
            if (thing == null)
            {
                return null;
            }

            SpriteSheet rawSpriteSheet = this.GetSpriteSheet(id, category, groupType);

            FrameGroup group = thing.GetFrameGroup(groupType);
            if (group.Layers < 2)
            {
                return rawSpriteSheet;
            }

            outfitData = outfitData == null ? outfitDataHelper : outfitData;

            int totalX = group.PatternZ * group.PatternX * group.Layers;
            int totalY = group.Frames * group.PatternY;
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
                for (int z = 0; z < group.PatternZ; z++)
                {
                    for (int x = 0; x < group.PatternX; x++)
                    {
                        int index = (((f % group.Frames * group.PatternZ + z) * group.PatternY) * group.PatternX + x) * group.Layers;
                        rectList[index] = new Rect((z * group.PatternX + x) * pixelsWidth, f * pixelsHeight, pixelsWidth, pixelsHeight);
                    }
                }
            }

            BitmapLocker grayLocker = new BitmapLocker(grayBitmap);
            BitmapLocker blendLocker = new BitmapLocker(blendBitmap);
            BitmapLocker bitmapLocker = new BitmapLocker(bitmap);

            grayLocker.LockBits();
            blendLocker.LockBits();
            bitmapLocker.LockBits();

            for (int y = 0; y < group.PatternY; y++)
            {
                if (y == 0 || (outfitData.Addons & 1 << (y - 1)) != 0)
                {
                    for (int f = 0; f < group.Frames; f++)
                    {
                        for (int z = 0; z < group.PatternZ; z++)
                        {
                            for (int x = 0; x < group.PatternX; x++)
                            {
                                // gets gray bitmap
                                int i = (((f % group.Frames * group.PatternZ + z) * group.PatternY + y) * group.PatternX + x) * group.Layers;
                                Rect rect = rawSpriteSheet.RectList[i];
                                int rx = rect.X;
                                int ry = rect.Y;
                                int rw = rect.Width;
                                int rh = rect.Height;
                                int index = (((f * group.PatternZ + z) * group.PatternY) * group.PatternX + x) * group.Layers;
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
            return Enumerable.ToArray(this.Things.Items.Values);
        }

        public ThingType[] GetAllOutfits()
        {
            return Enumerable.ToArray(this.Things.Outfits.Values);
        }

        public ThingType[] GetAllEffects()
        {
            return Enumerable.ToArray(this.Things.Effects.Values);
        }

        public ThingType[] GetAllMissiles()
        {
            return Enumerable.ToArray(this.Things.Missiles.Values);
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

            if (!this.Things.Save(datPath, version, features))
            {
                return false;
            }

            if (!this.Sprites.Save(sprPath, version, features))
            {
                return false;
            }

            return true;
        }

        public bool Save(string datPath, string sprPath, AssetsVersion version)
        {
            return this.Save(datPath, sprPath, version, AssetsFeatures.None);
        }

        public bool Save()
        {
            return this.Things.Save() && this.Sprites.Save();
        }

        public bool Unload()
        {
            if (!this.Loaded)
            {
                return false;
            }

            if (this.Things != null)
            {
                this.Things.ProgressChanged -= new ProgressHandler(this.StorageProgressChanged_Handler);
                this.Things.StorageChanged -= new ThingListChangedHandler(this.ThingListChanged_Handler);
                this.Things.StorageCompiled -= new StorageHandler(this.StorageCompiled_Handler);
                this.Things.StorageDisposed -= new StorageHandler(this.StorageDisposed_Handler);
                this.Things.Dispose();
                this.Things = null;
            }

            if (this.Sprites != null)
            {
                this.Sprites.ProgressChanged -= new ProgressHandler(this.StorageProgressChanged_Handler);
                this.Sprites.StorageChanged -= new SpriteListChangedHandler(this.SpriteListChanged_Handler);
                this.Sprites.StorageCompiled -= new StorageHandler(this.StorageCompiled_Handler);
                this.Sprites.StorageDisposed -= new StorageHandler(this.StorageDisposed_Handler);
                this.Sprites.Dispose();
                this.Sprites = null;
            }

            this.spriteCache.Clear();

            if (this.ClientUnloaded != null)
            {
                this.ClientUnloaded(this, new EventArgs());
            }

            return true;
        }

        public void Dispose()
        {
            this.Unload();
        }

        #endregion

        #region | Event Handlers |

        private void StorageCompiled_Handler(IStorage sender)
        {
            if (!this.Changed && sender == this.Sprites && this.ClientCompiled != null)
            {
                this.ClientCompiled(this, new EventArgs());
            }
        }

        private void StorageDisposed_Handler(IStorage sender)
        {
            if (!this.Loaded && this.ClientUnloaded != null)
            {
                this.ClientUnloaded(this, new EventArgs());
            }
        }

        private void ThingListChanged_Handler(object sender, ThingListChangedArgs e)
        {
            if (this.ClientChanged != null)
            {
                this.ClientChanged(this, new EventArgs());
            }
        }

        private void SpriteListChanged_Handler(object sender, SpriteListChangedArgs e)
        {
            if (this.ClientChanged != null)
            {
                this.ClientChanged(this, new EventArgs());
            }
        }

        private void StorageProgressChanged_Handler(object sender, int percentage)
        {
            if (this.ProgressChanged != null)
            {
                if (sender == this.Things)
                {
                    this.ProgressChanged(this, percentage / 2);
                }
                else
                {
                    this.ProgressChanged(this, (percentage / 2) + 50);
                }
            }
        }

        #endregion
    }
}
