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
using System;
#endregion

using SevenZip;
using System.IO;
using SevenZip.Compression.LZMA;

namespace OpenTibia.Utility
{
    public static class LZMACoder
    {
        private static CoderPropID[] propIDs =
        {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker
        };

        private static object[] properties =
        {
            1 << 21, // DictionarySize
            2,       // PosStateBits
            3,       // LitContextBits
            0,       // LitPosBits
            2,       // Algorithm
            128,     // NumFastBytes
            "bt4",   // MatchFinder
            false    // EndMarker
        };

        public static byte[] Uncompress(byte[] bytes)
        {
            if (bytes.Length < 5)
            {
                throw new Exception("LZMA data is too short.");
            }

            using (MemoryStream input = new MemoryStream(bytes))
            {
                // read the decoder properties
                byte[] properties = new byte[5];
                if (input.Read(properties, 0, 5) != 5)
                {
                    throw (new Exception("LZMA data is too short."));
                }

                long outSize = 0;

                if (BitConverter.IsLittleEndian)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        int v = input.ReadByte();
                        if (v < 0)
                        {
                            throw (new Exception("Can't Read 1."));
                        }

                        outSize |= ((long)v) << (8 * i);
                    }
                }

                MemoryStream output = new MemoryStream();
                long compressedSize = input.Length - input.Position;
                Decoder decoder = new Decoder();
                decoder.SetDecoderProperties(properties);
                decoder.Code(input, output, compressedSize, outSize, null);
                return output.ToArray();
            }
        }

        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream input = new MemoryStream(bytes))
            {
                MemoryStream output = new MemoryStream();
                Encoder encoder = new Encoder();
                encoder.SetCoderProperties(propIDs, properties);
                encoder.WriteCoderProperties(output);

                if (BitConverter.IsLittleEndian)
                {
                    byte[] LengthHeader = BitConverter.GetBytes(input.Length);
                    output.Write(LengthHeader, 0, LengthHeader.Length);
                }

                encoder.Code(input, output, input.Length, -1, null);
                return output.ToArray();
            }
        }
    }
}
