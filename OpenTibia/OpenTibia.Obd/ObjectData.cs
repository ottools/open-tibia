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
using OpenTibia.Assets;
using OpenTibia.Collections;
using OpenTibia.Geom;
using OpenTibia.Utilities;
using OpenTibia.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
#endregion

namespace OpenTibia.Obd
{
    public class ObjectData
    {
        #region | Private Properties |

        private static readonly OutfitData OUTFIT_DATA = new OutfitData();

        private ThingType type;
        private SpriteGroup sprites;

        #endregion

        #region | Constructor |

        public ObjectData(ThingType type, SpriteGroup sprites, MetadataFormat format)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (sprites == null)
            {
                throw new ArgumentNullException(nameof(sprites));
            }

            this.type = type;
            this.sprites = sprites;
            this.Format = format;
        }

        public ObjectData(ThingType type, SpriteGroup sprites)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (sprites == null)
            {
                throw new ArgumentNullException(nameof(sprites));
            }

            this.type = type;
            this.sprites = sprites;
            this.Format = MetadataFormat.Format_Last;
        }

        #endregion

        #region | Public Properties |

        public ThingType ThingType
        {
            get
            {
                return this.type;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ThingType");
                }

                this.type = value;
            }
        }

        public SpriteGroup Sprites
        {
            get
            {
                return this.sprites;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Sprites");
                }

                this.sprites = value;
            }
        }

        public ushort ID
        {
            get
            {
                return this.type.ID;
            }
        }

        public ThingCategory Category
        {
            get
            {
                return this.type.Category;
            }
        }

        public byte FrameGroupCount
        {
            get
            {
                return (byte)this.ThingType.FrameGroupCount;
            }
        }

        public MetadataFormat Format { get; private set; }

        #endregion

        #region | Public Methods |

        public override string ToString()
        {
            return $"(ThingData id={this.ID}, category={this.Category}";
        }

        public bool HasFrameGroup(FrameGroupType type)
        {
            return this.type.FrameGroups.ContainsKey(type);
        }

        public FrameGroup GetFrameGroup(FrameGroupType groupType)
        {
            return this.type.GetFrameGroup(groupType);
        }

        public FrameGroup GetFrameGroup()
        {
            return this.type.GetFrameGroup(FrameGroupType.Default);
        }

        public Sprite[] GetSprites(FrameGroupType groupType)
        {
            if (this.sprites.ContainsKey(groupType))
            {
                return this.sprites[groupType];
            }

            return null;
        }

        public SpriteSheet GetSpriteSheet(FrameGroupType groupType)
        {
            FrameGroup frameGroup = this.GetFrameGroup(groupType);
            if (frameGroup == null)
            {
                return null;
            }

            Sprite[] sprites = this.sprites[groupType];
            int totalX = frameGroup.PatternZ * frameGroup.PatternX * frameGroup.Layers;
            int totalY = frameGroup.Frames * frameGroup.PatternY;
            int bitmapWidth = (totalX * frameGroup.Width) * Sprite.DefaultSize;
            int bitmapHeight = (totalY * frameGroup.Height) * Sprite.DefaultSize;
            int pixelsWidth = frameGroup.Width * Sprite.DefaultSize;
            int pixelsHeight = frameGroup.Height * Sprite.DefaultSize;
            Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight, PixelFormat.Format32bppArgb);
            Dictionary<int, Rect> rectList = new Dictionary<int, Rect>();

            BitmapLocker lockBitmap = new BitmapLocker(bitmap);
            lockBitmap.LockBits();

            for (int f = 0; f < frameGroup.Frames; f++)
            {
                for (int z = 0; z < frameGroup.PatternZ; z++)
                {
                    for (int y = 0; y < frameGroup.PatternY; y++)
                    {
                        for (int x = 0; x < frameGroup.PatternX; x++)
                        {
                            for (int l = 0; l < frameGroup.Layers; l++)
                            {
                                int index = frameGroup.GetTextureIndex(l, x, y, z, f);
                                int fx = (index % totalX) * pixelsWidth;
                                int fy = (int)(Math.Floor((decimal)(index / totalX)) * pixelsHeight);
                                rectList.Add(index, new Rect(fx, fy, pixelsWidth, pixelsHeight));

                                for (int w = 0; w < frameGroup.Width; w++)
                                {
                                    for (int h = 0; h < frameGroup.Height; h++)
                                    {
                                        index = frameGroup.GetSpriteIndex(w, h, l, x, y, z, f);
                                        int px = (frameGroup.Width - w - 1) * Sprite.DefaultSize;
                                        int py = (frameGroup.Height - h - 1) * Sprite.DefaultSize;
                                        lockBitmap.CopyPixels(sprites[index].GetBitmap(), px + fx, py + fy);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            lockBitmap.UnlockBits();
            lockBitmap.Dispose();

            return new SpriteSheet(bitmap, rectList);
        }

        public SpriteSheet GetSpriteSheet(FrameGroupType groupType, OutfitData outfitData)
        {
            SpriteSheet rawSpriteSheet = this.GetSpriteSheet(groupType);

            FrameGroup group = this.ThingType.GetFrameGroup(groupType);
            if (group.Layers < 2)
            {
                return rawSpriteSheet;
            }

            outfitData = outfitData == null ? OUTFIT_DATA : outfitData;

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

        public SpriteSheet GetSpriteSheet()
        {
            return this.GetSpriteSheet(FrameGroupType.Default);
        }

        public ObjectData Clone()
        {
            return new ObjectData(this.type.Clone(), this.sprites.Clone());
        }

        #endregion

        #region | Public Static Methods |

        public static ObjectData Load(string path)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
                {
                    byte[] bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                    reader.Close();
                    return ObdDecoder.Decode(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        public static bool Save(string path, ObjectData data, ObdVersion version)
        {
            if (data == null)
            {
                return false;
            }

            byte[] bytes = ObdEncoder.Encode(data, version);
            if (bytes == null)
            {
                return false;
            }

            using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
            {
                writer.Write(bytes);
                writer.Close();
            }

            return true;
        }

        public static bool Save(string path, ObjectData data)
        {
            return Save(path, data, ObdVersion.Version2);
        }

        #endregion
    }
}
