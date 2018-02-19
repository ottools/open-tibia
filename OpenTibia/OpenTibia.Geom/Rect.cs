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
using System;
using System.Drawing;
#endregion

namespace OpenTibia.Geom
{
    public class Rect
    {
        #region Constructor

        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect() : this(0, 0, 0, 0)
        {
            ////
        }

        public Rect(Rect rect) : this(rect.X, rect.Y, rect.Width, rect.Height)
        {
            ////
        }

        #endregion

        #region Public Properties

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Left
        {
            get
            {
                return this.X;
            }
        }

        public int Top
        {
            get
            {
                return this.Y;
            }
        }

        public int Right
        {
            get
            {
                return this.Width;
            }
        }

        public int Bottom
        {
            get
            {
                return this.Height;
            }
        }

        public bool IsValid
        {
            get
            {
                return this.X <= this.Width && this.Y <= this.Height;
            }
        }

        #endregion

        #region Public Methods

        public Rect SetTo(int x, int y, int width, int height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            return this;
        }

        public Rect CopyFrom(Rect rect)
        {
            this.X = rect.X;
            this.Y = rect.Y;
            this.Width = rect.Width;
            this.Height = rect.Height;
            return this;
        }

        public Rect CopyFrom(Rectangle rectangle)
        {
            this.X = rectangle.X;
            this.Y = rectangle.Y;
            this.Width = rectangle.Width;
            this.Height = rectangle.Height;
            return this;
        }

        public Rect CopyTo(Rect rect)
        {
            rect.X = this.X;
            rect.Y = this.Y;
            rect.Width = this.Width;
            rect.Height = this.Height;
            return rect;
        }

        public Rectangle CopyTo(Rectangle rectangle)
        {
            rectangle.X = this.X;
            rectangle.Y = this.Y;
            rectangle.Width = this.Width;
            rectangle.Height = this.Height;
            return rectangle;
        }

        #endregion
    }
}
