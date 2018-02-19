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
using OpenTibia.Client.Things;
using System;
using System.Collections.Generic;
using System.Drawing;
#endregion

namespace OpenTibia.Utils
{
    public class SpriteCache
    {
        #region Private Properties

        private Dictionary<ushort, Bitmap> items;
        private Dictionary<ushort, Bitmap> outfits;
        private Dictionary<ushort, Bitmap> effects;
        private Dictionary<ushort, Bitmap> missiles;

        #endregion

        #region Constructor

        public SpriteCache()
        {
            this.items = new Dictionary<ushort, Bitmap>();
            this.outfits = new Dictionary<ushort, Bitmap>();
            this.effects = new Dictionary<ushort, Bitmap>();
            this.missiles = new Dictionary<ushort, Bitmap>();
        }

        #endregion

        #region Public Methods

        public void SetPicture(ushort id, ThingCategory category, Bitmap bitmap)
        {
            switch (category)
            {
                case ThingCategory.Item:
                    this.items[id] = bitmap;
                    break;

                case ThingCategory.Outfit:
                    this.outfits[id] = bitmap;
                    break;

                case ThingCategory.Effect:
                    this.effects[id] = bitmap;
                    break;

                case ThingCategory.Missile:
                    this.missiles[id] = bitmap;
                    break;
            }
        }

        public Bitmap GetPicture(ushort id, ThingCategory category)
        {
            if (category == ThingCategory.Item && this.items.ContainsKey(id))
            {
                return this.items[id];
            }
            else if (category == ThingCategory.Outfit && this.outfits.ContainsKey(id))
            {
                return this.outfits[id];
            }
            else if (category == ThingCategory.Effect && this.effects.ContainsKey(id))
            {
                return this.effects[id];
            }
            else if (category == ThingCategory.Missile && this.missiles.ContainsKey(id))
            {
                return this.missiles[id];
            }

            return null;
        }

        public void Clear()
        {
            this.items.Clear();
            this.outfits.Clear();
            this.effects.Clear();
            this.missiles.Clear();
        }

        #endregion
    }
}
