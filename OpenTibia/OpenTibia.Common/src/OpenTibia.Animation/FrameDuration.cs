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
using System;

namespace OpenTibia.Animation
{
    public class FrameDuration
    {
        private static readonly Random Random = new Random();

        public FrameDuration(int minimum, int maximum)
        {
            SetTo(minimum, maximum);
        }

        public FrameDuration(uint minimum, uint maximum)
        {
            SetTo((int)minimum, (int)maximum);
        }

        public FrameDuration(ObjectCategory category)
        {
            switch (category)
            {
                case ObjectCategory.Item:
                    SetTo(500, 500);
                    break;

                case ObjectCategory.Outfit:
                    SetTo(300, 300);
                    break;

                case ObjectCategory.Effect:
                    SetTo(100, 100);
                    break;
            }

            SetTo(0, 0);
        }

        public int Minimum { get; private set; }

        public int Maximum { get; private set; }

        public int Duration
        {
            get
            {
                if (Minimum == Maximum)
                {
                    return Minimum;
                }

                return (Minimum + Random.Next(0, Maximum - Minimum));
            }
        }

        public FrameDuration SetTo(int minimum, int maximum)
        {
            if (minimum > maximum)
            {
                throw new ArgumentException("The minimum value may not be greater than the maximum value.");
            }

            Minimum = minimum;
            Maximum = maximum;
            return this;
        }

        public FrameDuration CopyFrom(FrameDuration fd)
        {
            return SetTo(fd.Minimum, fd.Maximum);
        }

        public FrameDuration CopyTo(FrameDuration fd)
        {
            return fd.SetTo(Minimum, Maximum);
        }

        public FrameDuration Clone()
        {
            return new FrameDuration(Minimum, Maximum);
        }
    }
}
