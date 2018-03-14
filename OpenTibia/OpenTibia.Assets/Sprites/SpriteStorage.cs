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
using OpenTibia.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
#endregion

namespace OpenTibia.Assets
{
    public class SpriteStorage : IStorage, IDisposable
    {
        private struct CompilationData
        {
            public string Path;
            public string TmpPath;
            public Core.Version Version;
            public ClientFeatures Features;
        }

        #region | Constants |

        private const byte HeaderU16 = 6;
        private const byte HeaderU32 = 8;

        #endregion

        #region | Private Properties |

        private FileStream stream;
        private BinaryReader reader;
        private Dictionary<uint, Sprite> sprites;
        private uint rawSpriteCount;
        private byte headSize;
        private Sprite blankSprite;
        private bool transparency;
        private BackgroundWorker worker;
        private CompilationData compilationData;

        #endregion

        #region | Constructor |

        private SpriteStorage()
        {
            this.sprites = new Dictionary<uint, Sprite>();
            this.worker = new BackgroundWorker();
            this.worker.WorkerSupportsCancellation = true;
            this.worker.WorkerReportsProgress = true;
            this.worker.DoWork += new DoWorkEventHandler(this.DoWork_Handler);
            this.worker.ProgressChanged += new ProgressChangedEventHandler(this.WorkerProgressChanged_Handler);
            this.worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.RunWorkerCompleted_Handler);
        }

        #endregion

        #region | Events |

        public event SpriteListChangedHandler StorageChanged;

        public event StorageHandler StorageCompiled;

        public event StorageHandler StorageCompilationCanceled;

        public event StorageHandler StorageDisposed;

        public event ProgressHandler ProgressChanged;

        #endregion

        #region | Public Properties |

        public string FilePath { get; private set; }

        public Core.Version Version { get; private set; }

        public uint Count { get; private set; }

        public ClientFeatures ClientFeatures { get; private set; }

        public bool IsTemporary
        {
            get
            {
                return this.Loaded && this.FilePath == null;
            }
        }

        public bool IsFull
        {
            get
            {
                if ((this.ClientFeatures & ClientFeatures.Extended) == ClientFeatures.Extended)
                {
                    return false;
                }

                return this.Count >= 0xFFFF;
            }
        }

        public bool Changed { get; private set; }

        public bool Loaded { get; private set; }

        public bool Compiling { get; private set; }

        public bool Disposed { get; private set; }

        #endregion

        #region | Public Methods |

        public bool AddSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling)
            {
                return false;
            }

            uint id = ++this.Count;

            sprite.ID = id;
            sprite.Transparent = this.transparency;

            this.sprites.Add(id, sprite);
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { sprite }, StorageChangeType.Add));
            }

            return true;
        }

        public bool AddSprite(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling || bitmap.Width != Sprite.DefaultSize || bitmap.Height != Sprite.DefaultSize)
            {
                return false;
            }

            if (this.IsFull)
            {
                throw new Exception("The limit of sprites was reached.");
            }

            uint id = ++this.Count;
            Sprite sprite = new Sprite(id, this.transparency);
            sprite.SetBitmap(bitmap);
            this.sprites.Add(id, sprite);
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { sprite }, StorageChangeType.Add));
            }

            return true;
        }

        public bool AddSprites(Sprite[] sprites)
        {
            if (sprites == null)
            {
                throw new ArgumentNullException(nameof(sprites));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling || sprites.Length == 0)
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

                if (this.IsFull)
                {
                    throw new Exception("The limit of sprites was reached.");
                }

                uint id = ++this.Count;
                sprite.ID = id;
                sprite.Transparent = this.transparency;
                this.sprites.Add(id, sprite);
                changedSprites.Add(sprite);
            }

            if (changedSprites.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Add));
                }

                return true;
            }

            return false;
        }

        public bool AddSprites(Bitmap[] sprites)
        {
            if (sprites == null)
            {
                throw new ArgumentNullException(nameof(sprites));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling || sprites.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            foreach (Bitmap bitmap in sprites)
            {
                if (bitmap == null || bitmap.Width != Sprite.DefaultSize || bitmap.Height != Sprite.DefaultSize)
                {
                    continue;
                }

                if (this.IsFull)
                {
                    throw new Exception("The limit of sprites was reached.");
                }

                uint id = ++this.Count;
                Sprite sprite = new Sprite(id, this.transparency);
                this.sprites.Add(id, sprite);
                changedSprites.Add(sprite);
            }

            if (changedSprites.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Add));
                }

                return true;
            }

            return false;
        }

        public bool ReplaceSprite(Sprite newSprite, uint replaceId)
        {
            if (newSprite == null)
            {
                throw new ArgumentNullException(nameof(newSprite));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling || replaceId == 0 || replaceId > this.Count)
            {
                return false;
            }

            newSprite.ID = replaceId;
            newSprite.Transparent = this.transparency;

            Sprite replacedSprite = null;

            if (this.sprites.ContainsKey(replaceId))
            {
                replacedSprite = this.sprites[replaceId];
                this.sprites[replaceId] = newSprite;
            }
            else
            {
                replacedSprite = this.ReadSprite(replaceId);
                this.sprites.Add(replaceId, newSprite);
            }

            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { replacedSprite }, StorageChangeType.Replace));
            }

            return true;
        }

        public bool ReplaceSprite(Sprite newSprite)
        {
            if (newSprite == null)
            {
                throw new ArgumentNullException(nameof(newSprite));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling)
            {
                return this.ReplaceSprite(newSprite, newSprite.ID);
            }

            return false;
        }

        public bool ReplaceSprite(Bitmap newBitmap, uint replaceId)
        {
            if (newBitmap == null)
            {
                throw new ArgumentNullException(nameof(newBitmap));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling || newBitmap.Width != Sprite.DefaultSize || newBitmap.Height != Sprite.DefaultSize || replaceId == 0 || replaceId > this.Count)
            {
                return false;
            }

            Sprite newSprite = new Sprite(replaceId, this.transparency);
            Sprite replacedSprite = null;

            if (this.sprites.ContainsKey(replaceId))
            {
                replacedSprite = this.sprites[replaceId];
                this.sprites[replaceId] = newSprite;
            }
            else
            {
                replacedSprite = this.ReadSprite(replaceId);
                this.sprites.Add(replaceId, newSprite);
            }

            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { replacedSprite }, StorageChangeType.Replace));
            }

            return true;
        }

        public bool ReplaceSprites(Sprite[] newSprites)
        {
            if (newSprites == null)
            {
                throw new ArgumentNullException(nameof(newSprites));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling || newSprites.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            foreach (Sprite sprite in newSprites)
            {
                if (sprite == null || sprite.ID == 0 || sprite.ID > this.Count)
                {
                    continue;
                }

                uint id = sprite.ID;
                Sprite replacedSprite = null;

                sprite.Transparent = this.transparency;

                if (this.sprites.ContainsKey(id))
                {
                    replacedSprite = this.sprites[id];
                    this.sprites[id] = sprite;
                }
                else
                {
                    replacedSprite = this.ReadSprite(id);
                    this.sprites.Add(id, sprite);
                }

                changedSprites.Add(replacedSprite);
            }

            if (changedSprites.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Replace));
                }

                return true;
            }

            return false;
        }

        public bool RemoveSprite(uint id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling || id == 0 || id > this.Count)
            {
                return false;
            }

            Sprite removedSprite = this.GetSprite(id);

            if (id == this.Count && id != 1)
            {
                if (this.sprites.ContainsKey(id))
                {
                    this.sprites.Remove(id);
                }

                this.Count--;
            }
            else
            {
                if (this.sprites.ContainsKey(id))
                {
                    this.sprites[id] = new Sprite(id, this.transparency);
                }
                else
                {
                    this.sprites.Add(id, new Sprite(id, this.transparency));
                }
            }

            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { removedSprite }, StorageChangeType.Remove));
            }

            return true;
        }

        public bool RemoveSprites(uint[] ids)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling || ids == null || ids.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            for (int i = 0; i < ids.Length; i++)
            {
                uint id = ids[i];

                if (id == 0 || id > this.Count)
                {
                    continue;
                }

                changedSprites.Add(this.GetSprite(id));

                if (id == this.Count && id != 1)
                {
                    if (this.sprites.ContainsKey(id))
                    {
                        this.sprites.Remove(id);
                    }

                    this.Count--;
                }
                else
                {
                    if (this.sprites.ContainsKey(id))
                    {
                        this.sprites[id] = new Sprite(id, this.transparency);
                    }
                    else
                    {
                        this.sprites.Add(id, new Sprite(id, this.transparency));
                    }
                }
            }

            if (changedSprites.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Remove));
                }
            }

            return false;
        }

        public bool HasSpriteID(uint id)
        {
            return id <= this.Count;
        }

        public Sprite GetSprite(uint id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (id <= this.Count)
            {
                if (id == 0)
                {
                    return this.blankSprite;
                }

                if (this.sprites.ContainsKey(id))
                {
                    return this.sprites[id];
                }

                return this.ReadSprite(id);
            }

            return null;
        }

        public Sprite GetSprite(int id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (id >= 0)
            {
                return this.GetSprite((uint)id);
            }

            return null;
        }

        public Bitmap GetSpriteBitmap(uint id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (id <= this.Count)
            {
                return this.ReadSprite(id).GetBitmap();
            }

            return null;
        }

        public Bitmap GetSpriteBitmap(int id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (id >= 0 && id <= this.Count)
            {
                return this.ReadSprite((uint)id).GetBitmap();
            }

            return null;
        }

        public bool Save(string path, Core.Version version, ClientFeatures features)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || this.Compiling)
            {
                return false;
            }

            if (features == ClientFeatures.None || features == ClientFeatures.Transparency)
            {
                features |= version.Value >= (ushort)DatFormat.Format_755 ? ClientFeatures.PatternZ : features;
                features |= version.Value >= (ushort)DatFormat.Format_960 ? ClientFeatures.Extended : features;
                features |= version.Value >= (ushort)DatFormat.Format_1050 ? ClientFeatures.FrameDurations : features;
                features |= version.Value >= (ushort)DatFormat.Format_1057 ? ClientFeatures.FrameGroups : features;
            }

            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!this.Changed && this.Version.Equals(version) && this.ClientFeatures == features)
            {
                //  just copy the content and reload if nothing has changed.
                if (this.FilePath != null && !this.FilePath.Equals(path))
                {
                    File.Copy(this.FilePath, path, true);

                    if (!this.InternalLoad(path, version, features, true))
                    {
                        return false;
                    }

                    if (this.ProgressChanged != null)
                    {
                        this.ProgressChanged(this, 100);
                    }

                    if (this.StorageCompiled != null)
                    {
                        this.StorageCompiled(this);
                    }
                }

                return true;
            }

            this.compilationData = new CompilationData();
            this.compilationData.Path = path;
            this.compilationData.TmpPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(path) + ".tmp");
            this.compilationData.Version = version;
            this.compilationData.Features = features;
            this.Compiling = true;
            this.worker.RunWorkerAsync();
            return true;
        }

        public bool Save(string path, Core.Version version)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            return this.Save(path, version, ClientFeatures.None);
        }

        public bool Save()
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (this.Changed && !this.IsTemporary)
            {
                return this.Save(this.FilePath, this.Version, this.ClientFeatures);
            }

            return true;
        }

        public bool Cancel()
        {
            if (this.Compiling)
            {
                this.Compiling = false;
                this.worker.CancelAsync();
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            this.Disposed = true;

            if (!this.Loaded)
            {
                return;
            }

            if (this.stream != null)
            {
                this.stream.Dispose();
                this.stream = null;
                this.reader.Dispose();
                this.reader = null;
            }

            this.FilePath = null;
            this.sprites.Clear();
            this.sprites = null;
            this.rawSpriteCount = 0;
            this.Count = 0;
            this.transparency = false;
            this.Changed = false;
            this.Loaded = false;
            this.Compiling = false;
            this.blankSprite = null;

            if (this.StorageDisposed != null)
            {
                this.StorageDisposed(this);
            }
        }

        #endregion

        #region | Private Methods |

        private bool InternalCreate(Core.Version version, ClientFeatures features)
        {
            if (this.Compiling)
            {
                return false;
            }

            if (this.Loaded)
            {
                return true;
            }

            if (features == ClientFeatures.None || features == ClientFeatures.Transparency)
            {
                features |= version.Value >= (ushort)DatFormat.Format_755 ? ClientFeatures.PatternZ : features;
                features |= version.Value >= (ushort)DatFormat.Format_960 ? ClientFeatures.Extended : features;
                features |= version.Value >= (ushort)DatFormat.Format_1050 ? ClientFeatures.FrameDurations : features;
                features |= version.Value >= (ushort)DatFormat.Format_1057 ? ClientFeatures.FrameGroups : features;
            }

            this.Version = version;
            this.ClientFeatures = features;
            this.transparency = (features & ClientFeatures.Transparency) == ClientFeatures.Transparency ? true : false;
            this.headSize = (features & ClientFeatures.Extended) == ClientFeatures.Extended ? HeaderU32 : HeaderU16;
            this.blankSprite = new Sprite(0, this.transparency);
            this.sprites.Add(1, new Sprite(1, this.transparency));
            this.rawSpriteCount = 0;
            this.Count = 1;
            this.Changed = true;
            this.Loaded = true;
            this.Compiling = false;
            this.Disposed = false;
            return true;
        }

        private bool InternalLoad(string path, Core.Version version, ClientFeatures features, bool reloading)
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

            if (this.Compiling)
            {
                return false;
            }

            if (this.Loaded)
            {
                if (reloading)
                {
                    this.stream.Close();
                    this.stream = null;
                }
                else
                {
                    return true;
                }
            }

            if (features == ClientFeatures.None || features == ClientFeatures.Transparency)
            {
                features |= version.Value >= (ushort)DatFormat.Format_755 ? ClientFeatures.PatternZ : features;
                features |= version.Value >= (ushort)DatFormat.Format_960 ? ClientFeatures.Extended : features;
                features |= version.Value >= (ushort)DatFormat.Format_1050 ? ClientFeatures.FrameDurations : features;
                features |= version.Value >= (ushort)DatFormat.Format_1057 ? ClientFeatures.FrameGroups : features;
            }

            this.stream = new FileStream(path, FileMode.Open);
            this.reader = new BinaryReader(this.stream);

            uint signature = this.reader.ReadUInt32();
            if (signature != version.SprSignature)
            {
                string message = "Invalid SPR signature. Expected signature is {0:X} and loaded signature is {1:X}.";
                throw new Exception(string.Format(message, version.SprSignature, signature));
            }

            if ((features & ClientFeatures.Extended) == ClientFeatures.Extended)
            {
                this.headSize = HeaderU32;
                this.rawSpriteCount = this.reader.ReadUInt32();
            }
            else
            {
                this.headSize = HeaderU16;
                this.rawSpriteCount = this.reader.ReadUInt16();
            }

            this.FilePath = path;
            this.Version = version;
            this.ClientFeatures = features;
            this.transparency = (features & ClientFeatures.Transparency) == ClientFeatures.Transparency ? true : false;
            this.Count = this.rawSpriteCount;
            this.blankSprite = new Sprite(0, this.transparency);
            this.Changed = false;
            this.Loaded = true;
            this.Disposed = false;
            return true;
        }

        private Sprite ReadSprite(uint id)
        {
            try
            {
                if (id > this.rawSpriteCount)
                {
                    return null;
                }

                if (id == 0)
                {
                    return this.blankSprite;
                }

                // O id 1 no arquivo spr é o endereço 0, então subtraímos
                // o id fornecido e mutiplicamos pela quantidade de bytes
                // de cada endereço.
                this.stream.Position = ((id - 1) * 4) + this.headSize;

                // Lê o endereço do sprite.
                uint spriteAddress = this.reader.ReadUInt32();

                // O endereço 0 representa um sprite em branco,
                // então retornamos um sprite sem a leitura dos dados.
                if (spriteAddress == 0)
                {
                    return new Sprite(id, this.transparency);
                }

                // Posiciona o stream para o endereço do sprite.
                this.stream.Position = spriteAddress;

                // Leitura da cor magenta usada como referência
                // para remover o fundo do sprite.
                this.reader.ReadByte(); // red key color
                this.reader.ReadByte(); // green key color
                this.reader.ReadByte(); // blue key color

                Sprite sprite = new Sprite(id, this.transparency);

                // O tamanho dos pixels compressados.
                ushort pixelDataSize = this.reader.ReadUInt16();
                if (pixelDataSize != 0)
                {
                    sprite.CompressedPixels = this.reader.ReadBytes(pixelDataSize);
                }

                return sprite;
            }
            catch /*(Exception ex)*/
            {
                // TODO ErrorManager.ShowError(ex);
            }

            return null;
        }

        #endregion

        #region | Event Handlers |

        private void DoWork_Handler(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            using (BinaryWriter writer = new BinaryWriter(new FileStream(this.compilationData.TmpPath, FileMode.Create)))
            {
                uint count = 0;
                bool extended = (this.compilationData.Features & ClientFeatures.Extended) == ClientFeatures.Extended;
                bool transparent = (this.compilationData.Features & ClientFeatures.Transparency) == ClientFeatures.Transparency;
                bool changeTransparency = this.transparency != transparent;

                // write the signature
                writer.Write(this.compilationData.Version.SprSignature);

                // write the sprite count
                if (extended)
                {
                    count = this.Count;
                    writer.Write(count);
                }
                else
                {
                    count = this.Count >= 0xFFFF ? 0xFFFF : this.Count;
                    writer.Write((ushort)count);
                }

                int addressPosition = headSize;
                int address = (int)((count * 4) + headSize);
                byte[] bytes = null;

                for (uint id = 1; id <= count; id++)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }

                    writer.Seek(addressPosition, SeekOrigin.Begin);

                    if (this.sprites.ContainsKey(id) || changeTransparency)
                    {
                        Sprite sprite = null;
                        this.sprites.TryGetValue(id, out sprite);

                        if (sprite == null)
                        {
                            sprite = this.ReadSprite(id);
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
                            bytes = sprite.CompressedPixels;

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
                    else if (id <= this.rawSpriteCount)
                    {
                        this.stream.Seek(((id - 1) * 4) + this.headSize, SeekOrigin.Begin);

                        uint spriteAddress = this.reader.ReadUInt32();

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
                            this.stream.Seek(spriteAddress + 3, SeekOrigin.Begin);

                            // read the data size from the current stream
                            ushort pixelDataSize = this.reader.ReadUInt16();

                            // write sprite data size
                            writer.Write(pixelDataSize);

                            // write sprite compressed pixels
                            if (pixelDataSize != 0)
                            {
                                bytes = this.reader.ReadBytes(pixelDataSize);
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

            if (File.Exists(this.compilationData.Path))
            {
                File.Delete(this.compilationData.Path);
            }

            File.Move(this.compilationData.TmpPath, this.compilationData.Path);

            worker.ReportProgress(100);
        }

        private void WorkerProgressChanged_Handler(object sender, ProgressChangedEventArgs e)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, e.ProgressPercentage);
            }
        }

        private void RunWorkerCompleted_Handler(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                this.Compiling = false;

                if (this.InternalLoad(this.compilationData.Path, this.compilationData.Version, this.compilationData.Features, true))
                {
                    if (this.StorageCompiled != null)
                    {
                        this.StorageCompiled(this);
                    }

                    if (this.ProgressChanged != null)
                    {
                        this.ProgressChanged(this, 100);
                    }
                }
            }
            else if (File.Exists(this.compilationData.TmpPath))
            {
                File.Delete(this.compilationData.TmpPath);

                if (this.ProgressChanged != null)
                {
                    this.ProgressChanged(this, 0);
                }
            }

            if (e.Cancelled && this.StorageCompilationCanceled != null)
            {
                this.StorageCompilationCanceled(this);
            }
        }

        #endregion

        #region | Public Static Methods |

        public static SpriteStorage Create(Core.Version version, ClientFeatures features)
        {
            SpriteStorage storage = new SpriteStorage();
            if (storage.InternalCreate(version, features))
            {
                return storage;
            }

            return null;
        }

        public static SpriteStorage Create(Core.Version version)
        {
            SpriteStorage storage = new SpriteStorage();
            if (storage.InternalCreate(version, ClientFeatures.None))
            {
                return storage;
            }

            return null;
        }

        public static SpriteStorage Load(string path, Core.Version version, ClientFeatures features)
        {
            SpriteStorage storage = new SpriteStorage();
            if (storage.InternalLoad(path, version, features, false))
            {
                return storage;
            }

            return null;
        }

        public static SpriteStorage Load(string path, Core.Version version)
        {
            SpriteStorage storage = new SpriteStorage();
            if (storage.InternalLoad(path, version, ClientFeatures.None, false))
            {
                return storage;
            }

            return null;
        }

        #endregion
    }
}
