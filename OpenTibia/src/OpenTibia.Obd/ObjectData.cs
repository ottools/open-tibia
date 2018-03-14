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
using OpenTibia.Geom;
using OpenTibia.Utilities;
using OpenTibia.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace OpenTibia.Obd
{
    public class ObjectData
    {
        private ThingType m_type;
        private SpriteGroup m_sprites;

        public ObjectData(ThingType type, SpriteGroup sprites, MetadataFormat format)
        {
            m_type = type ?? throw new ArgumentNullException(nameof(type));
            m_sprites = sprites ?? throw new ArgumentNullException(nameof(sprites));
            Format = format;
        }

        public ObjectData(ThingType type, SpriteGroup sprites)
        {
            m_type = type ?? throw new ArgumentNullException(nameof(type));
            m_sprites = sprites ?? throw new ArgumentNullException(nameof(sprites));
            Format = MetadataFormat.Format_Last;
        }

        public ThingType ThingType
        {
            get => m_type;
            set => m_type = value ?? throw new ArgumentNullException(nameof(ThingType));
        }

        public SpriteGroup Sprites
        {
            get => m_sprites;
            set => m_sprites = value ?? throw new ArgumentNullException(nameof(Sprites));
        }

        public ushort ID => m_type.ID;

        public ThingCategory Category => m_type.Category;

        public int FrameGroupCount => ThingType.FrameGroupCount;

        public MetadataFormat Format { get; }

        public override string ToString()
        {
            return $"({nameof(ObjectData)} id={ID}, category={Category}";
        }

        public bool HasFrameGroup(FrameGroupType type)
        {
            return m_type.FrameGroups.ContainsKey(type);
        }

        public FrameGroup GetFrameGroup(FrameGroupType groupType)
        {
            return m_type.GetFrameGroup(groupType);
        }

        public FrameGroup GetFrameGroup()
        {
            return m_type.GetFrameGroup(FrameGroupType.Default);
        }

        public Sprite[] GetSprites(FrameGroupType groupType)
        {
            if (m_sprites.ContainsKey(groupType))
            {
                return m_sprites[groupType];
            }

            return null;
        }

        public SpriteSheet GetSpriteSheet(FrameGroupType groupType)
        {
            FrameGroup frameGroup = GetFrameGroup(groupType);
            if (frameGroup == null)
            {
                return null;
            }

            Sprite[] sprites = m_sprites[groupType];
            int totalX = frameGroup.PatternsZ * frameGroup.PatternsX * frameGroup.Layers;
            int totalY = frameGroup.Frames * frameGroup.PatternsY;
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
                for (int z = 0; z < frameGroup.PatternsZ; z++)
                {
                    for (int y = 0; y < frameGroup.PatternsY; y++)
                    {
                        for (int x = 0; x < frameGroup.PatternsX; x++)
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
            SpriteSheet rawSpriteSheet = GetSpriteSheet(groupType);

            FrameGroup group = ThingType.GetFrameGroup(groupType);
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

        public SpriteSheet GetSpriteSheet()
        {
            return GetSpriteSheet(FrameGroupType.Default);
        }

        public ObjectData Clone()
        {
            return new ObjectData(m_type.Clone(), m_sprites.Clone());
        }

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
    }
}
