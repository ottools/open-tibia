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

using OpenTibia.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace OpenTibia.Assets
{
    public class SpriteStorage : IStorage, IDisposable
    {
        private struct CompilationData
        {
            public string Path;
            public string TmpPath;
            public AssetsVersion Version;
            public AssetsFeatures Features;
        }

        private const byte HeaderU16 = 6;
        private const byte HeaderU32 = 8;

        private FileStream m_stream;
        private BinaryReader m_reader;
        private Dictionary<uint, Sprite> m_sprites;
        private uint m_rawSpriteCount;
        private byte m_headSize;
        private Sprite m_blankSprite;
        private bool m_transparent;
        private SpritePixelFormat m_format;
        private BackgroundWorker m_worker;
        private CompilationData m_compilationData;

        private SpriteStorage(SpritePixelFormat format)
        {
            PixelFormat = format;

            m_sprites = new Dictionary<uint, Sprite>();
            m_worker = new BackgroundWorker();
            m_worker.WorkerSupportsCancellation = true;
            m_worker.WorkerReportsProgress = true;
            m_worker.DoWork += new DoWorkEventHandler(DoWork_Handler);
            m_worker.ProgressChanged += new ProgressChangedEventHandler(WorkerProgressChanged_Handler);
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RunWorkerCompleted_Handler);
        }

        public event SpriteListChangedHandler StorageChanged;

        public event StorageHandler StorageCompiled;

        public event StorageHandler StorageCompilationCanceled;

        public event ProgressHandler ProgressChanged;

        public string FilePath { get; private set; }

        public AssetsVersion Version { get; private set; }

        public uint Count { get; private set; }

        public AssetsFeatures ClientFeatures { get; private set; }

        public SpritePixelFormat PixelFormat { get; }

        public bool IsTemporary => Loaded && FilePath == null;

        public bool IsFull
        {
            get
            {
                if ((ClientFeatures & AssetsFeatures.Extended) == AssetsFeatures.Extended)
                {
                    return false;
                }

                return Count >= 0xFFFF;
            }
        }

        public bool Changed { get; private set; }

        public bool Loaded { get; private set; }

        public bool Compiling { get; private set; }

        public bool Disposed { get; private set; }

        public bool AddSprite(Sprite sprite)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            if (!Loaded || Compiling)
            {
                return false;
            }

            uint id = ++Count;

            sprite.ID = id;
            sprite.Transparent = m_transparent;

            m_sprites.Add(id, sprite);
            Changed = true;

            StorageChanged?.Invoke(this, new SpriteListChangedArgs(new Sprite[] { sprite }, StorageChangeType.Add));

            return true;
        }

        public bool AddSprites(Sprite[] sprites)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (sprites == null)
            {
                throw new ArgumentNullException(nameof(sprites));
            }

            if (!Loaded || Compiling || sprites.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            foreach (Sprite sprite in sprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                if (IsFull)
                {
                    throw new Exception("The limit of sprites was reached.");
                }

                uint id = ++Count;
                sprite.ID = id;
                sprite.Transparent = m_transparent;
                m_sprites.Add(id, sprite);
                changedSprites.Add(sprite);
            }

            if (changedSprites.Count != 0)
            {
                Changed = true;

                StorageChanged?.Invoke(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Add));

                return true;
            }

            return false;
        }

        public bool ReplaceSprite(Sprite newSprite, uint replaceId)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (newSprite == null)
            {
                throw new ArgumentNullException(nameof(newSprite));
            }

            if (!Loaded || Compiling || replaceId == 0 || replaceId > Count)
            {
                return false;
            }

            newSprite.ID = replaceId;
            newSprite.Transparent = m_transparent;

            Sprite replacedSprite = null;

            if (m_sprites.ContainsKey(replaceId))
            {
                replacedSprite = m_sprites[replaceId];
                m_sprites[replaceId] = newSprite;
            }
            else
            {
                replacedSprite = ReadSprite(replaceId);
                m_sprites.Add(replaceId, newSprite);
            }

            Changed = true;

            StorageChanged?.Invoke(this, new SpriteListChangedArgs(new Sprite[] { replacedSprite }, StorageChangeType.Replace));

            return true;
        }

        public bool ReplaceSprite(Sprite newSprite)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (newSprite == null)
            {
                throw new ArgumentNullException(nameof(newSprite));
            }

            if (!Loaded || Compiling)
            {
                return ReplaceSprite(newSprite, newSprite.ID);
            }

            return false;
        }

        public bool ReplaceSprites(Sprite[] newSprites)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (newSprites == null)
            {
                throw new ArgumentNullException(nameof(newSprites));
            }

            if (!Loaded || Compiling || newSprites.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            foreach (Sprite sprite in newSprites)
            {
                if (sprite == null || sprite.ID == 0 || sprite.ID > Count)
                {
                    continue;
                }

                uint id = sprite.ID;
                Sprite replacedSprite = null;

                sprite.Transparent = m_transparent;

                if (m_sprites.ContainsKey(id))
                {
                    replacedSprite = m_sprites[id];
                    m_sprites[id] = sprite;
                }
                else
                {
                    replacedSprite = ReadSprite(id);
                    m_sprites.Add(id, sprite);
                }

                changedSprites.Add(replacedSprite);
            }

            if (changedSprites.Count != 0)
            {
                Changed = true;

                StorageChanged?.Invoke(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Replace));

                return true;
            }

            return false;
        }

        public bool RemoveSprite(uint id, SpritePixelFormat format)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (!Loaded || Compiling || id == 0 || id > Count)
            {
                return false;
            }

            Sprite removedSprite = GetSprite(id);

            if (id == Count && id != 1)
            {
                if (m_sprites.ContainsKey(id))
                {
                    m_sprites.Remove(id);
                }

                Count--;
            }
            else
            {
                if (m_sprites.ContainsKey(id))
                {
                    m_sprites[id] = new Sprite(id, m_transparent, format);
                }
                else
                {
                    m_sprites.Add(id, new Sprite(id, m_transparent, format));
                }
            }

            Changed = true;

            StorageChanged?.Invoke(this, new SpriteListChangedArgs(new Sprite[] { removedSprite }, StorageChangeType.Remove));

            return true;
        }

        public bool RemoveSprites(uint[] ids)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (!Loaded || Compiling || ids == null || ids.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            for (int i = 0; i < ids.Length; i++)
            {
                uint id = ids[i];

                if (id == 0 || id > Count)
                {
                    continue;
                }

                changedSprites.Add(GetSprite(id));

                if (id == Count && id != 1)
                {
                    if (m_sprites.ContainsKey(id))
                    {
                        m_sprites.Remove(id);
                    }

                    Count--;
                }
                else
                {
                    if (m_sprites.ContainsKey(id))
                    {
                        m_sprites[id] = new Sprite(id, m_transparent, PixelFormat);
                    }
                    else
                    {
                        m_sprites.Add(id, new Sprite(id, m_transparent, PixelFormat));
                    }
                }
            }

            if (changedSprites.Count != 0)
            {
                Changed = true;

                StorageChanged?.Invoke(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Remove));
            }

            return false;
        }

        public bool HasSpriteID(uint id)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            return id <= Count;
        }

        public Sprite GetSprite(uint id)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (id <= Count)
            {
                if (id == 0)
                {
                    return m_blankSprite;
                }

                if (m_sprites.ContainsKey(id))
                {
                    return m_sprites[id];
                }

                return ReadSprite(id);
            }

            return null;
        }

        public Sprite GetSprite(int id)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (id >= 0)
            {
                return GetSprite((uint)id);
            }

            return null;
        }

        public bool Save(string path, AssetsVersion version, AssetsFeatures features)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (!Loaded || Compiling)
            {
                return false;
            }

            if (features == AssetsFeatures.None || features == AssetsFeatures.Transparency)
            {
                features |= version.Format >= MetadataFormat.Format_755 ? AssetsFeatures.PatternsZ : features;
                features |= version.Format >= MetadataFormat.Format_960 ? AssetsFeatures.Extended : features;
                features |= version.Format >= MetadataFormat.Format_1050 ? AssetsFeatures.FramesDuration : features;
                features |= version.Format >= MetadataFormat.Format_1057 ? AssetsFeatures.FrameGroups : features;
            }

            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!Changed && Version.Equals(version) && ClientFeatures == features)
            {
                //  just copy the content and reload if nothing has changed.
                if (FilePath != null && !FilePath.Equals(path))
                {
                    File.Copy(FilePath, path, true);

                    if (!InternalLoad(path, version, features, true))
                    {
                        return false;
                    }

                    ProgressChanged?.Invoke(this, 100);
                    StorageCompiled?.Invoke(this);
                }

                return true;
            }

            m_compilationData = new CompilationData();
            m_compilationData.Path = path;
            m_compilationData.TmpPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(path) + ".tmp");
            m_compilationData.Version = version;
            m_compilationData.Features = features;
            Compiling = true;
            m_worker.RunWorkerAsync();
            return true;
        }

        public bool Save(string path, AssetsVersion version)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            return Save(path, version, AssetsFeatures.None);
        }

        public bool Save()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (Changed && !IsTemporary)
            {
                return Save(FilePath, Version, ClientFeatures);
            }

            return true;
        }

        public bool Cancel()
        {
            if (Compiling)
            {
                Compiling = false;
                m_worker.CancelAsync();
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;

            if (m_stream != null)
            {
                m_stream.Dispose();
                m_stream = null;
                m_reader.Dispose();
                m_reader = null;
            }

            FilePath = null;
            m_sprites.Clear();
            m_sprites = null;
            m_rawSpriteCount = 0;
            Count = 0;
            m_transparent = false;
            Changed = false;
            Loaded = false;
            Compiling = false;
            m_blankSprite = null;
        }

        private bool InternalCreate(AssetsVersion version, AssetsFeatures features)
        {
            if (Compiling)
            {
                return false;
            }

            if (Loaded)
            {
                return true;
            }

            if (features == AssetsFeatures.None || features == AssetsFeatures.Transparency)
            {
                features |= version.Format >= MetadataFormat.Format_755 ? AssetsFeatures.PatternsZ : features;
                features |= version.Format >= MetadataFormat.Format_960 ? AssetsFeatures.Extended : features;
                features |= version.Format >= MetadataFormat.Format_1050 ? AssetsFeatures.FramesDuration : features;
                features |= version.Format >= MetadataFormat.Format_1057 ? AssetsFeatures.FrameGroups : features;
            }

            Version = version;
            ClientFeatures = features;
            m_transparent = features.HasFlag(AssetsFeatures.Transparency);
            m_headSize = features.HasFlag(AssetsFeatures.Extended) ? HeaderU32 : HeaderU16;
            m_blankSprite = new Sprite(0, m_transparent, PixelFormat);
            m_sprites.Add(1, new Sprite(1, m_transparent, PixelFormat));
            m_rawSpriteCount = 0;
            Count = 1;
            Changed = true;
            Loaded = true;
            Compiling = false;
            Disposed = false;
            return true;
        }

        private bool InternalLoad(string path, AssetsVersion version, AssetsFeatures features, bool reloading)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (!File.Exists(path))
            {
                string message = $"File not found: {path}"; // TODO: ResourceManager.GetString("Exception.FileNotFound");
                throw new FileNotFoundException(message, path);
            }

            if (Compiling)
            {
                return false;
            }

            if (Loaded)
            {
                if (reloading)
                {
                    m_stream.Close();
                    m_stream = null;
                }
                else
                {
                    return true;
                }
            }

            if (features == AssetsFeatures.None || features == AssetsFeatures.Transparency)
            {
                features |= version.Format >= MetadataFormat.Format_755 ? AssetsFeatures.PatternsZ : features;
                features |= version.Format >= MetadataFormat.Format_960 ? AssetsFeatures.Extended : features;
                features |= version.Format >= MetadataFormat.Format_1050 ? AssetsFeatures.FramesDuration : features;
                features |= version.Format >= MetadataFormat.Format_1057 ? AssetsFeatures.FrameGroups : features;
            }

            m_stream = new FileStream(path, FileMode.Open);
            m_reader = new BinaryReader(m_stream);

            uint signature = m_reader.ReadUInt32();
            if (signature != version.SprSignature)
            {
                string message = "Invalid SPR signature. Expected signature is {0:X} and loaded signature is {1:X}.";
                throw new Exception(string.Format(message, version.SprSignature, signature));
            }

            if (features.HasFlag(AssetsFeatures.Extended))
            {
                m_headSize = HeaderU32;
                m_rawSpriteCount = m_reader.ReadUInt32();
            }
            else
            {
                m_headSize = HeaderU16;
                m_rawSpriteCount = m_reader.ReadUInt16();
            }

            FilePath = path;
            Version = version;
            ClientFeatures = features;
            m_transparent = features.HasFlag(AssetsFeatures.Transparency);
            Count = m_rawSpriteCount;
            m_blankSprite = new Sprite(0, m_transparent, PixelFormat);
            Changed = false;
            Loaded = true;
            Disposed = false;
            return true;
        }

        private Sprite ReadSprite(uint id)
        {
            try
            {
                if (id > m_rawSpriteCount)
                {
                    return null;
                }

                if (id == 0)
                {
                    return m_blankSprite;
                }

                // O id 1 no arquivo spr é o endereço 0, então subtraímos
                // o id fornecido e mutiplicamos pela quantidade de bytes
                // de cada endereço.
                m_stream.Position = ((id - 1) * 4) + m_headSize;

                // Lê o endereço do sprite.
                uint spriteAddress = m_reader.ReadUInt32();

                // O endereço 0 representa um sprite em branco,
                // então retornamos um sprite sem a leitura dos dados.
                if (spriteAddress == 0)
                {
                    return new Sprite(id, m_transparent, PixelFormat);
                }

                // Posiciona o stream para o endereço do sprite.
                m_stream.Position = spriteAddress;

                // Leitura da cor magenta usada como referência
                // para remover o fundo do sprite.
                m_reader.ReadByte(); // red key color
                m_reader.ReadByte(); // green key color
                m_reader.ReadByte(); // blue key color

                Sprite sprite = new Sprite(id, m_transparent, PixelFormat);

                // O tamanho dos pixels compressados.
                ushort pixelDataSize = m_reader.ReadUInt16();
                if (pixelDataSize != 0)
                {
                    sprite.Data = m_reader.ReadBytes(pixelDataSize);
                }

                return sprite;
            }
            catch /*(Exception ex)*/
            {
                // TODO ErrorManager.ShowError(ex);
            }

            return null;
        }

        private void DoWork_Handler(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            using (BinaryWriter writer = new BinaryWriter(new FileStream(m_compilationData.TmpPath, FileMode.Create)))
            {
                uint count = 0;
                bool extended = (m_compilationData.Features & AssetsFeatures.Extended) == AssetsFeatures.Extended;
                bool transparent = (m_compilationData.Features & AssetsFeatures.Transparency) == AssetsFeatures.Transparency;
                bool changeTransparency = m_transparent != transparent;

                // write the signature
                writer.Write(m_compilationData.Version.SprSignature);

                // write the sprite count
                if (extended)
                {
                    count = Count;
                    writer.Write(count);
                }
                else
                {
                    count = Count >= 0xFFFF ? 0xFFFF : Count;
                    writer.Write((ushort)count);
                }

                int addressPosition = m_headSize;
                int address = (int)((count * 4) + m_headSize);
                byte[] bytes = null;

                for (uint id = 1; id <= count; id++)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }

                    writer.Seek(addressPosition, SeekOrigin.Begin);

                    if (m_sprites.ContainsKey(id) || changeTransparency)
                    {
                        Sprite sprite = null;
                        m_sprites.TryGetValue(id, out sprite);

                        if (sprite == null)
                        {
                            sprite = ReadSprite(id);
                        }

                        sprite.Transparent = transparent;

                        if (sprite.Length == 0)
                        {
                            // write address 0
                            writer.Write((uint)0);
                            writer.Seek(address, SeekOrigin.Begin);
                        }
                        else
                        {
                            bytes = sprite.Data;

                            // write address
                            writer.Write((uint)address);
                            writer.Seek(address, SeekOrigin.Begin);

                            // write colorkey
                            writer.Write((byte)0xFF); // red
                            writer.Write((byte)0x00); // blue
                            writer.Write((byte)0xFF); // green

                            // write sprite data size
                            writer.Write((short)bytes.Length);

                            if (bytes.Length > 0)
                            {
                                writer.Write(bytes);
                            }
                        }
                    }
                    else if (id <= m_rawSpriteCount)
                    {
                        m_stream.Seek(((id - 1) * 4) + m_headSize, SeekOrigin.Begin);

                        uint spriteAddress = m_reader.ReadUInt32();

                        if (spriteAddress == 0)
                        {
                            // write address 0
                            writer.Write((uint)0);
                            writer.Seek(address, SeekOrigin.Begin);
                        }
                        else
                        {
                            // write address
                            writer.Write((uint)address);
                            writer.Seek(address, SeekOrigin.Begin);

                            // write colorkey
                            writer.Write((byte)0xFF); // red
                            writer.Write((byte)0x00); // blue
                            writer.Write((byte)0xFF); // green

                            // sets the position to the pixel data size.
                            m_stream.Seek(spriteAddress + 3, SeekOrigin.Begin);

                            // read the data size from the current stream
                            ushort pixelDataSize = m_reader.ReadUInt16();

                            // write sprite data size
                            writer.Write(pixelDataSize);

                            // write sprite compressed pixels
                            if (pixelDataSize != 0)
                            {
                                bytes = m_reader.ReadBytes(pixelDataSize);
                                writer.Write(bytes);
                            }
                        }
                    }

                    address = (int)writer.BaseStream.Position;
                    addressPosition += 4;

                    if ((id % 500) == 0)
                    {
                        Thread.Sleep(10);
                        worker.ReportProgress((int)((id * 100) / count));
                    }
                }

                writer.Close();
            }

            if (File.Exists(m_compilationData.Path))
            {
                File.Delete(m_compilationData.Path);
            }

            File.Move(m_compilationData.TmpPath, m_compilationData.Path);

            worker.ReportProgress(100);
        }

        private void WorkerProgressChanged_Handler(object sender, ProgressChangedEventArgs e)
        {
            ProgressChanged?.Invoke(this, e.ProgressPercentage);
        }

        private void RunWorkerCompleted_Handler(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                Compiling = false;

                if (InternalLoad(m_compilationData.Path, m_compilationData.Version, m_compilationData.Features, true))
                {
                    StorageCompiled?.Invoke(this);
                    ProgressChanged?.Invoke(this, 100);
                }
            }
            else if (File.Exists(m_compilationData.TmpPath))
            {
                File.Delete(m_compilationData.TmpPath);

                ProgressChanged?.Invoke(this, 0);
            }

            if (e.Cancelled && StorageCompilationCanceled != null)
            {
                StorageCompilationCanceled(this);
            }
        }

        public static SpriteStorage Create(AssetsVersion version, AssetsFeatures features)
        {
            SpriteStorage storage = new SpriteStorage(SpritePixelFormat.Bgra);
            if (storage.InternalCreate(version, features))
            {
                return storage;
            }

            return null;
        }

        public static SpriteStorage Create(AssetsVersion version)
        {
            SpriteStorage storage = new SpriteStorage(SpritePixelFormat.Bgra);
            if (storage.InternalCreate(version, AssetsFeatures.None))
            {
                return storage;
            }

            return null;
        }

        public static SpriteStorage Load(string path, AssetsVersion version, AssetsFeatures features)
        {
            SpriteStorage storage = new SpriteStorage(SpritePixelFormat.Bgra);
            if (storage.InternalLoad(path, version, features, false))
            {
                return storage;
            }

            return null;
        }

        public static SpriteStorage Load(string path, AssetsVersion version)
        {
            SpriteStorage storage = new SpriteStorage(SpritePixelFormat.Bgra);
            if (storage.InternalLoad(path, version, AssetsFeatures.None, false))
            {
                return storage;
            }

            return null;
        }
    }
}
