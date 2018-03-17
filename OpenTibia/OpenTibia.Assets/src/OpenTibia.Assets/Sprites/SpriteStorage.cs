﻿#region Licence
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
            public SpritePixelFormat Format;
        }

        private SpritesFileReader m_reader;
        private Dictionary<uint, Sprite> m_sprites;
        private uint m_rawSpriteCount;
        private int m_headerSize;
        private Sprite m_emptySprite;
        private bool m_transparent;
        private BackgroundWorker m_worker;
        private CompilationData m_compilationData;

        public SpriteStorage(SpritePixelFormat format)
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

        public event StorageHandler StorageLoaded;

        public event SpriteListChangedHandler StorageChanged;

        public event StorageHandler StorageCompiled;

        public event StorageHandler StorageCompilationCanceled;

        public event ProgressHandler ProgressChanged;

        public string FilePath { get; private set; }

        public AssetsVersion Version { get; private set; }

        public uint Count { get; private set; }

        public AssetsFeatures Features { get; private set; }

        public SpritePixelFormat PixelFormat { get; }

        public bool IsTemporary => Loaded && FilePath == null;

        public bool IsFull => !Features.HasFlag(AssetsFeatures.Extended) && Count >= 0xFFFF;

        public bool Changed { get; private set; }

        public bool Loaded { get; private set; }

        public bool Compiling { get; private set; }

        public bool Disposed { get; private set; }

        public void Create(AssetsVersion version, AssetsFeatures features)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (InternalCreate(version, features))
            {
                StorageLoaded?.Invoke(this);
            }
        }

        public void Create(AssetsVersion version)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (InternalCreate(version, AssetsFeatures.None))
            {
                StorageLoaded?.Invoke(this);
            }
        }

        public void Load(string path, AssetsVersion version, AssetsFeatures features)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (InternalLoad(path, version, features, false))
            {
                StorageLoaded?.Invoke(this);
            }
        }

        public void Load(string path, AssetsVersion version)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(SpriteStorage));
            }

            if (InternalLoad(path, version, AssetsFeatures.None, false))
            {
                StorageLoaded?.Invoke(this);
            }
        }

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
                replacedSprite = m_reader.ReadSprite(replaceId);
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
                    replacedSprite = m_reader.ReadSprite(id);
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
                    return m_emptySprite;
                }

                if (m_sprites.ContainsKey(id))
                {
                    return m_sprites[id];
                }

                return m_reader.ReadSprite(id);
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

            if (!Changed && Version.Equals(version) && Features == features)
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

            m_compilationData = new CompilationData
            {
                Path = path,
                TmpPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(path) + ".tmp"),
                Version = version,
                Features = features,
                Format = PixelFormat
            };

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
                return Save(FilePath, Version, Features);
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

            if (m_reader != null)
            {
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
            m_emptySprite = null;
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
            Features = features;
            m_transparent = features.HasFlag(AssetsFeatures.Transparency);
            m_headerSize = features.HasFlag(AssetsFeatures.Extended) ? SpritesFileSize.HeaderU32 : SpritesFileSize.HeaderU16;
            m_emptySprite = new Sprite(0, m_transparent, PixelFormat);
            m_sprites.Add(1, new Sprite(1, m_transparent, PixelFormat));
            m_rawSpriteCount = 0;
            Count = 1;
            Changed = true;
            Loaded = true;
            Compiling = false;
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
                    m_reader.Dispose();
                    m_reader = null;
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

            m_reader = new SpritesFileReader(features, PixelFormat);
            m_reader.Open(path);

            if (m_reader.Signature != version.SprSignature)
            {
                string message = "Invalid SPR signature. Expected signature is {0:X} and loaded signature is {1:X}.";
                throw new Exception(string.Format(message, version.SprSignature, m_reader.Signature));
            }

            m_rawSpriteCount = m_reader.SpritesCount;
            m_transparent = m_reader.Transparent;
            m_emptySprite = new Sprite(0, m_transparent, PixelFormat);

            FilePath = path;
            Version = version;
            Features = features;
            Count = m_rawSpriteCount;
            Changed = false;
            Loaded = true;

            return true;
        }

        private void DoWork_Handler(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            using (SpritesFileWriter writer = new SpritesFileWriter(m_compilationData.Features, m_compilationData.Format))
            {
                writer.Open(m_compilationData.TmpPath);

                // write the signature
                writer.WriteSignature(m_compilationData.Version.SprSignature);

                // write the sprite count
                writer.WriteCount(Count);

                bool changed = m_transparent != writer.Transparent;

                for (uint id = 1, count = writer.Count; id <= count; id++)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }

                    if (id <= m_rawSpriteCount && !changed)
                    {
                        writer.WriteRawPixels(m_reader.ReadRawPixels(id));
                    }
                    else
                    {
                        m_sprites.TryGetValue(id, out Sprite sprite);

                        if (sprite == null)
                        {
                            sprite = m_reader.ReadSprite(id);
                        }

                        writer.WriteSprite(sprite);
                    }

                    if ((id % 500) == 0)
                    {
                        Thread.Sleep(10);
                        worker.ReportProgress((int)((id * 100) / count));
                    }
                }
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
    }
}
