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
#endregion

namespace OpenTibia.Geom
{
    public class Position
    {
        #region Contructors

        public Position(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Position() : this(-1, -1, -1)
        {
            ////
        }

        public Position(Position position) : this(position.X, position.Y, position.Z)
        {
            ////
        }

        #endregion

        #region Public Properties

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        #endregion

        #region Public Methods

        public override bool Equals(object position)
        {
            if (position == null)
            {
                return false;
            }

            Position pos = position as Position;

            if ((object)pos == null)
            {
                return false;
            }

            return (this.X == pos.X && this.Y == pos.Y && this.Z == pos.Z);
        }

        public bool Equals(Position position)
        {
            if ((object)position == null)
            {
                return false;
            }

            return (this.X == position.X && this.Y == position.Y && this.Z == position.Z);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(this.X, this.Y, this.Z).GetHashCode();
        }

        public Position SetTo(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            return this;
        }

        public Position CopyFrom(Position position)
        {
            this.X = position.X;
            this.Y = position.Y;
            this.Z = position.Z;
            return this;
        }

        public Position CopyTo(Position position)
        {
            position.X = this.X;
            position.Y = this.Y;
            position.Z = this.Z;
            return position;
        }

        public bool IsValid()
        {
            return (this.X >= 0 && this.X <= 0xFFFF && this.Y >= 0 && this.Y <= 0xFFFF && this.Z >= 0 && this.Z <= 15);
        }

        #endregion

        #region Operators

        public static Position operator +(Position p1, Position p2)
        {
            if ((object)p1 == null || (object)p2 == null)
            {
                return null;
            }

            return new Position(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p1.Z);
        }

        public static Position operator -(Position p1, Position p2)
        {
            if ((object)p1 == null || (object)p2 == null)
            {
                return null;
            }

            return new Position(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p1.Z);
        }

        public static bool operator ==(Position p1, Position p2)
        {
            if (object.ReferenceEquals(p1, p2))
            {
                return true;
            }

            if ((object)p1 == null || (object)p2 == null)
            {
                return false;
            }

            return p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
        }

        public static bool operator !=(Position p1, Position p2)
        {
            if (!object.ReferenceEquals(p1, p2))
            {
                return true;
            }

            if ((object)p1 == null || (object)p2 == null)
            {
                return true;
            }

            return p1.X != p2.X || p1.Y != p2.Y || p1.Z != p2.Z;
        }

        #endregion
    }
}
