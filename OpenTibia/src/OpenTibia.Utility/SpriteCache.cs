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

using OpenTibia.Assets;
using System.Collections.Generic;
using System.Drawing;

namespace OpenTibia.Utility
{
    public class SpriteCache
    {
        private Dictionary<ushort, Bitmap> m_items;
        private Dictionary<ushort, Bitmap> m_outfits;
        private Dictionary<ushort, Bitmap> m_effects;
        private Dictionary<ushort, Bitmap> m_missiles;

        public SpriteCache()
        {
            m_items = new Dictionary<ushort, Bitmap>();
            m_outfits = new Dictionary<ushort, Bitmap>();
            m_effects = new Dictionary<ushort, Bitmap>();
            m_missiles = new Dictionary<ushort, Bitmap>();
        }

        public void SetPicture(ushort id, ThingCategory category, Bitmap bitmap)
        {
            switch (category)
            {
                case ThingCategory.Item:
                    m_items[id] = bitmap;
                    break;

                case ThingCategory.Outfit:
                    m_outfits[id] = bitmap;
                    break;

                case ThingCategory.Effect:
                    m_effects[id] = bitmap;
                    break;

                case ThingCategory.Missile:
                    m_missiles[id] = bitmap;
                    break;
            }
        }

        public Bitmap GetPicture(ushort id, ThingCategory category)
        {
            if (category == ThingCategory.Item && m_items.ContainsKey(id))
            {
                return m_items[id];
            }
            else if (category == ThingCategory.Outfit && m_outfits.ContainsKey(id))
            {
                return m_outfits[id];
            }
            else if (category == ThingCategory.Effect && m_effects.ContainsKey(id))
            {
                return m_effects[id];
            }
            else if (category == ThingCategory.Missile && m_missiles.ContainsKey(id))
            {
                return m_missiles[id];
            }

            return null;
        }

        public void Clear()
        {
            m_items.Clear();
            m_outfits.Clear();
            m_effects.Clear();
            m_missiles.Clear();
        }
    }
}
