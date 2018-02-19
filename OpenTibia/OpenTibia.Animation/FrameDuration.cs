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
#endregion

namespace OpenTibia.Animation
{
    public class FrameDuration
    {
        #region | Private Properties |

        private static readonly Random Random = new Random();

        #endregion

        #region | Constructor |

        public FrameDuration(int minimum, int maximum)
        {
            this.SetTo(minimum, maximum);
        }

        public FrameDuration(uint minimum, uint maximum)
        {
            this.SetTo((int)minimum, (int)maximum);
        }

        public FrameDuration(ThingCategory category)
        {
            switch (category)
            {
                case ThingCategory.Item:
                    this.SetTo(500, 500);
                    break;

                case ThingCategory.Outfit:
                    this.SetTo(300, 300);
                    break;

                case ThingCategory.Effect:
                    this.SetTo(100, 100);
                    break;
            }

            this.SetTo(0, 0);
        }

        #endregion

        #region | Public Properties |

        public int Minimum { get; private set; }

        public int Maximum { get; private set; }

        public int Duration
        {
            get
            {
                if (this.Minimum == this.Maximum)
                {
                    return this.Minimum;
                }

                return (this.Minimum + Random.Next(0, this.Maximum - this.Minimum));
            }
        }

        #endregion

        #region | Public Methods |

        public FrameDuration SetTo(int minimum, int maximum)
        {
            if (minimum > maximum)
            {
                throw new ArgumentException("The minimum value may not be greater than the maximum value.");
            }

            this.Minimum = minimum;
            this.Maximum = maximum;
            return this;
        }

        public FrameDuration CopyFrom(FrameDuration fd)
        {
            return this.SetTo(fd.Minimum, fd.Maximum);
        }

        public FrameDuration CopyTo(FrameDuration fd)
        {
            return fd.SetTo(this.Minimum, this.Maximum);
        }

        public FrameDuration Clone()
        {
            return new FrameDuration(this.Minimum, this.Maximum);
        }

        #endregion
    }
}
