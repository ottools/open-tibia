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
    public class BinaryTreeReader : IDisposable
    {
        private BinaryReader m_reader;
        private long m_currentNodePosition;
        private uint m_currentNodeSize;

        public BinaryTreeReader(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            m_reader = new BinaryReader(new FileStream(path, FileMode.Open));
        }

        public bool Disposed { get; private set; }

        public BinaryReader GetRootNode()
        {
            return GetChildNode();
        }

        public BinaryReader GetChildNode()
        {
            Advance();
            return GetNodeData();
        }

        public BinaryReader GetNextNode()
        {
            m_reader.BaseStream.Seek(m_currentNodePosition, SeekOrigin.Begin);

            SpecialChar value = (SpecialChar)m_reader.ReadByte();
            if (value != SpecialChar.NodeStart)
            {
                return null;
            }

            value = (SpecialChar)m_reader.ReadByte();

            int level = 1;
            while (true)
            {
                value = (SpecialChar)m_reader.ReadByte();
                if (value == SpecialChar.NodeEnd)
                {
                    --level;
                    if (level == 0)
                    {
                        value = (SpecialChar)m_reader.ReadByte();
                        if (value == SpecialChar.NodeEnd)
                        {
                            return null;
                        }
                        else if (value != SpecialChar.NodeStart)
                        {
                            return null;
                        }
                        else
                        {
                            m_currentNodePosition = m_reader.BaseStream.Position - 1;
                            return GetNodeData();
                        }
                    }
                }
                else if (value == SpecialChar.NodeStart)
                {
                    ++level;
                }
                else if (value == SpecialChar.EscapeChar)
                {
                    m_reader.ReadByte();
                }
            }
        }

        public void Dispose()
        {
            if (m_reader != null)
            {
                m_reader.Dispose();
                m_reader = null;
                Disposed = true;
            }
        }

        private BinaryReader GetNodeData()
        {
            m_reader.BaseStream.Seek(m_currentNodePosition, SeekOrigin.Begin);

            // read node type
            byte value = m_reader.ReadByte();

            if ((SpecialChar)value != SpecialChar.NodeStart)
            {
                return null;
            }

            MemoryStream ms = new MemoryStream(200);

            m_currentNodeSize = 0;
            while (true)
            {
                value = m_reader.ReadByte();
                if ((SpecialChar)value == SpecialChar.NodeEnd || (SpecialChar)value == SpecialChar.NodeStart)
                {
                    break;
                }
                else if ((SpecialChar)value == SpecialChar.EscapeChar)
                {
                    value = m_reader.ReadByte();
                }

                m_currentNodeSize++;
                ms.WriteByte(value);
            }

            m_reader.BaseStream.Seek(m_currentNodePosition, SeekOrigin.Begin);
            ms.Position = 0;
            return new BinaryReader(ms);
        }

        private bool Advance()
        {
            try
            {
                long seekPos = 0;
                if (m_currentNodePosition == 0)
                {
                    seekPos = 4;
                }
                else
                {
                    seekPos = m_currentNodePosition;
                }

                m_reader.BaseStream.Seek(seekPos, SeekOrigin.Begin);

                SpecialChar value = (SpecialChar)m_reader.ReadByte();
                if (value != SpecialChar.NodeStart)
                {
                    return false;
                }

                if (m_currentNodePosition == 0)
                {
                    m_currentNodePosition = m_reader.BaseStream.Position - 1;
                    return true;
                }
                else
                {
                    value = (SpecialChar)m_reader.ReadByte();

                    while (true)
                    {
                        value = (SpecialChar)m_reader.ReadByte();
                        if (value == SpecialChar.NodeEnd)
                        {
                            return false;
                        }
                        else if (value == SpecialChar.NodeStart)
                        {
                            m_currentNodePosition = m_reader.BaseStream.Position - 1;
                            return true;
                        }
                        else if (value == SpecialChar.EscapeChar)
                        {
                            m_reader.ReadByte();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
