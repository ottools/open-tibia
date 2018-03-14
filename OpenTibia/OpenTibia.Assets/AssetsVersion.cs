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

namespace OpenTibia.Assets
{
    public class AssetsVersion
    {
        public AssetsVersion(ushort value, string description, uint datSignature, uint sprSignature, uint otbValue)
        {
            Value = value;
            Description = string.IsNullOrEmpty(description) ? $"Client {value / 100}.{value % 100}" : description;
            DatSignature = datSignature;
            SprSignature = sprSignature;
            OtbValue = otbValue;
            Format = VesionValueToDatFormat(value);
        }

        public AssetsVersion(ushort value, uint datSignature, uint sprSignature, uint otbValue) : this(value, null, datSignature, sprSignature, otbValue)
        {
            ////
        }

        public ushort Value { get; private set; }

        public string Description { get; private set; }

        public uint DatSignature { get; private set; }

        public uint SprSignature { get; private set; }

        public uint OtbValue { get; private set; }

        public MetadataFormat Format { get; private set; }

        public bool IsValid => Value != 0 && DatSignature != 0 && SprSignature != 0 && OtbValue != 0;

        public override string ToString()
        {
            return Description;
        }

        public static MetadataFormat VesionValueToDatFormat(ushort value)
        {
            if (value == 0)
            {
                return MetadataFormat.InvalidFormat;
            }

            if (value < 740)
            {
                return MetadataFormat.Format_710;
            }
            else if (value < 755)
            {
                return MetadataFormat.Format_740;
            }
            else if (value < 780)
            {
                return MetadataFormat.Format_755;
            }
            else if (value < 860)
            {
                return MetadataFormat.Format_780;
            }
            else if (value < 960)
            {
                return MetadataFormat.Format_860;
            }
            else if (value < 1010)
            {
                return MetadataFormat.Format_960;
            }
            else if (value < 1050)
            {
                return MetadataFormat.Format_1010;
            }
            else if (value < 1057)
            {
                return MetadataFormat.Format_1050;
            }
            else if (value < 1092)
            {
                return MetadataFormat.Format_1057;
            }
            else if (value < 1093)
            {
                return MetadataFormat.Format_1092;
            }
            else if (value >= 1093 && value <= 1099)
            {
                return MetadataFormat.Format_1093;
            }

            return MetadataFormat.InvalidFormat;
        }
    }
}
