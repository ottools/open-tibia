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
using OpenTibia.Client.Things;
using System;
#endregion

namespace OpenTibia.Core
{
    public class Version
    {
        #region Constructor

        public Version(ushort value, string description, uint datSignature, uint sprSignature, uint otbValue)
        {
            this.Value = value;
            this.Description = string.IsNullOrEmpty(description) ? $"Client {value / 100}.{value % 100}" : description;
            this.DatSignature = datSignature;
            this.SprSignature = sprSignature;
            this.OtbValue = otbValue;
            this.Format = VesionValueToDatFormat(value);
        }

        public Version(ushort value, uint datSignature, uint sprSignature, uint otbValue) : this(value, null, datSignature, sprSignature, otbValue)
        {
            ////
        }

        #endregion

        #region Public Properties

        public ushort Value { get; private set; }

        public string Description { get; private set; }

        public uint DatSignature { get; private set; }

        public uint SprSignature { get; private set; }

        public uint OtbValue { get; private set; }

        public DatFormat Format { get; private set; }

        public bool IsValid
        {
            get
            {
                return this.Value != 0 && !string.IsNullOrEmpty(this.Description) && this.DatSignature != 0 && this.SprSignature != 0 && this.OtbValue != 0;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return this.Description;
        }

        #endregion

        #region Public Static Methods

        public static DatFormat VesionValueToDatFormat(ushort value)
        {
            if (value == 0)
            {
                return DatFormat.InvalidFormat;
            }

            if (value < 740)
            {
                return DatFormat.Format_710;
            }
            else if (value < 755)
            {
                return DatFormat.Format_740;
            }
            else if (value < 780)
            {
                return DatFormat.Format_755;
            }
            else if (value < 860)
            {
                return DatFormat.Format_780;
            }
            else if (value < 960)
            {
                return DatFormat.Format_860;
            }
            else if (value < 1010)
            {
                return DatFormat.Format_960;
            }
            else if (value < 1050)
            {
                return DatFormat.Format_1010;
            }
            else if (value < 1057)
            {
                return DatFormat.Format_1050;
            }
            else if (value < 1092)
            {
                return DatFormat.Format_1057;
            }
            else if (value < 1093)
            {
                return DatFormat.Format_1092;
            }
            else if (value >= 1093)
            {
                return DatFormat.Format_1093;
            }

            return DatFormat.InvalidFormat;
        }

        #endregion
    }
}
