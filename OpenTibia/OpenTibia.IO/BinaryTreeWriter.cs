#region Licence
/**
* Copyright © 2015-2018 OTTools <https://github.com/ottools/open-tibia>
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License along
* with this program; if not, write to the Free Software Foundation, Inc.,
* 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
#endregion

using System;
using System.IO;

namespace OpenTibia.IO
{
    public class BinaryTreeWriter : IDisposable
    {
        private BinaryReader m_writer;

        public BinaryTreeWriter(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            m_writer = new BinaryReader(new FileStream(path, FileMode.Create));
        }

        public bool Disposed { get; private set; }

        public void CreateNode(byte type)
        {
            WriteByte((byte)SpecialChar.NodeStart, false);
            WriteByte(type);
        }

        public void WriteByte(byte value)
        {
            WriteBytes(new byte[1] { value }, true);
        }

        public void WriteByte(byte value, bool unescape)
        {
            WriteBytes(new byte[1] { value }, unescape);
        }

        public void WriteUInt16(ushort value)
        {
            WriteBytes(BitConverter.GetBytes(value), true);
        }

        public void WriteUInt16(ushort value, bool unescape)
        {
            WriteBytes(BitConverter.GetBytes(value), unescape);
        }

        public void WriteUInt32(uint value)
        {
            WriteBytes(BitConverter.GetBytes(value), true);
        }

        public void WriteUInt32(uint value, bool unescape)
        {
            WriteBytes(BitConverter.GetBytes(value), unescape);
        }

        public void WriteProp(RootAttribute attribute, BinaryWriter writer)
        {
            writer.BaseStream.Position = 0;
            byte[] bytes = new byte[writer.BaseStream.Length];
            writer.BaseStream.Read(bytes, 0, (int)writer.BaseStream.Length);
            writer.BaseStream.Position = 0;
            writer.BaseStream.SetLength(0);

            WriteProp((byte)attribute, bytes);
        }

        public void WriteBytes(byte[] bytes, bool unescape)
        {
            foreach (byte b in bytes)
            {
                if (unescape && (b == (byte)SpecialChar.NodeStart || b == (byte)SpecialChar.NodeEnd || b == (byte)SpecialChar.EscapeChar))
                {
                    m_writer.BaseStream.WriteByte((byte)SpecialChar.EscapeChar);
                }

                m_writer.BaseStream.WriteByte(b);
            }
        }

        public void CloseNode()
        {
            WriteByte((byte)SpecialChar.NodeEnd, false);
        }

        public void Dispose()
        {
            if (m_writer != null)
            {
                m_writer.Dispose();
                m_writer = null;
                Disposed = true;
            }
        }

        private void WriteProp(byte attr, byte[] bytes)
        {
            WriteByte((byte)attr);
            WriteUInt16((ushort)bytes.Length);
            WriteBytes(bytes, true);
        }
    }
}
