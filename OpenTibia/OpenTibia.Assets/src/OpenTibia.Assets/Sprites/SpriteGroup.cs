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
using System;
using System.Collections.Generic;

namespace OpenTibia.Assets
{
    public class SpriteGroup : Dictionary<FrameGroupType, Sprite[]>
    {
        public Sprite GetSprite(int index, FrameGroupType groupType)
        {
            if (ContainsKey(groupType))
            {
                Sprite[] group = this[groupType];

                if (index >= 0 && index < group.Length)
                {
                    return group[index];
                }
            }

            return null;
        }

        public Sprite GetSprite(int index)
        {
            Sprite[] group = this[FrameGroupType.Default];

            if (index >= 0 && index < group.Length)
            {
                return group[index];
            }

            return null;
        }

        public void SetSprites(FrameGroupType groupType, Sprite[] sprites)
        {
            if (sprites == null)
            {
                throw new ArgumentNullException(nameof(sprites));
            }

            if (ContainsKey(groupType))
            {
                this[groupType] = sprites;
            }
            else
            {
                Add(groupType, sprites);
            }
        }

        public SpriteGroup Clone()
        {
            SpriteGroup clone = new SpriteGroup();

            foreach (var kpv in this)
            {
                Sprite[] sprites = kpv.Value;
                int length = sprites.Length;
                Sprite[] cloneSprites = new Sprite[length];

                for (int i = 0; i < length; i++)
                {
                    cloneSprites[i] = sprites[i].Clone();
                }

                clone.Add(kpv.Key, cloneSprites);
            }

            return clone;
        }
    }
}
