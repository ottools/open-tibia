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
using OpenTibia.Common;
using OpenTibia.IO;
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace OpenTibia.Assets
{
    public class ThingTypeStorage : IStorage, IDisposable
    {
        #region | Constructor|

        private ThingTypeStorage()
        {
            this.Items = new Dictionary<ushort, ThingType>();
            this.Outfits = new Dictionary<ushort, ThingType>();
            this.Effects = new Dictionary<ushort, ThingType>();
            this.Missiles = new Dictionary<ushort, ThingType>();
        }

        #endregion

        #region | Events |

        public event ThingListChangedHandler StorageChanged;

        public event StorageHandler StorageCompiled;

        public event StorageHandler StorageDisposed;

        public event ProgressHandler ProgressChanged;

        #endregion

        #region | Public Propeties |

        public string FilePath { get; private set; }

        public AssetsVersion Version { get; private set; }

        public Dictionary<ushort, ThingType> Items { get; private set; }

        public ushort ItemCount { get; private set; }

        public Dictionary<ushort, ThingType> Outfits { get; private set; }

        public ushort OutfitCount { get; private set; }

        public Dictionary<ushort, ThingType> Effects { get; private set; }

        public ushort EffectCount { get; private set; }

        public Dictionary<ushort, ThingType> Missiles { get; private set; }

        public ushort MissileCount { get; private set; }

        public AssetsFeatures ClientFeatures { get; private set; }

        public bool IsTemporary
        {
            get
            {
                return this.Loaded && this.FilePath == null;
            }
        }

        public bool Changed { get; private set; }

        public bool Loaded { get; private set; }

        public bool Disposed { get; private set; }

        #endregion

        #region | Public Methods |

        public bool AddThing(ThingType thing)
        {
            if (thing == null)
            {
                throw new ArgumentNullException(nameof(thing));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded)
            {
                return false;
            }

            ThingType changedThing = this.InternalAddThing(thing);
            if (changedThing != null)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Add));
                }

                return true;
            }

            return false;
        }

        public bool AddThings(ThingType[] things)
        {
            if (things == null)
            {
                throw new ArgumentNullException(nameof(things));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || things.Length == 0)
            {
                return false;
            }

            List<ThingType> changedThings = new List<ThingType>();

            for (int i = 0; i < things.Length; i++)
            {
                ThingType thing = this.InternalAddThing(things[i]);
                if (thing != null)
                {
                    changedThings.Add(thing);
                }
            }

            if (changedThings.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new ThingListChangedArgs(changedThings.ToArray(), StorageChangeType.Add));
                }

                return true;
            }

            return false;
        }

        public bool ReplaceThing(ThingType thing, ushort replaceId)
        {
            if (thing == null)
            {
                throw new ArgumentNullException(nameof(thing));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded)
            {
                return false;
            }

            ThingType changedThing = this.InternalReplaceThing(thing, this.GetThing(replaceId, thing.Category));
            if (changedThing != null)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Replace));
                }

                return true;
            }

            return false;
        }

        public bool ReplaceThing(ThingType thing)
        {
            if (thing == null)
            {
                throw new ArgumentNullException(nameof(thing));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded)
            {
                return false;
            }

            ThingType changedThing = this.InternalReplaceThing(thing, this.GetThing(thing.ID, thing.Category));
            if (changedThing != null)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Replace));
                }

                return true;
            }

            return false;
        }

        public bool ReplaceThings(ThingType[] things)
        {
            if (things == null)
            {
                throw new ArgumentNullException(nameof(things));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || things.Length == 0)
            {
                return false;
            }

            List<ThingType> changedThings = new List<ThingType>();

            for (int i = 0; i < things.Length; i++)
            {
                ThingType thing = things[i];
                if (thing != null)
                {
                    thing = this.InternalReplaceThing(thing, this.GetThing(thing.ID, thing.Category));
                    if (thing != null)
                    {
                        changedThings.Add(thing);
                    }
                }
            }

            if (changedThings.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new ThingListChangedArgs(changedThings.ToArray(), StorageChangeType.Replace));
                }

                return true;
            }

            return false;
        }

        public bool RemoveThing(ushort id, ThingCategory category)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || category == ThingCategory.Invalid)
            {
                return false;
            }

            ThingType changedThing = this.InternalRemoveThing(id, category);
            if (changedThing != null)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Remove));
                }

                return true;
            }

            return false;
        }

        public bool RemoveThing(ThingType thing)
        {
            if (thing == null)
            {
                throw new ArgumentNullException(nameof(thing));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || thing.Category == ThingCategory.Invalid)
            {
                return false;
            }

            ThingType changedThing = this.InternalRemoveThing(thing.ID, thing.Category);
            if (changedThing != null)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Remove));
                }

                return true;
            }

            return false;
        }

        public bool RemoveThings(ThingType[] things)
        {
            if (things == null)
            {
                throw new ArgumentNullException(nameof(things));
            }

            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || things.Length == 0)
            {
                return false;
            }

            List<ThingType> changedThings = new List<ThingType>();

            for (int i = 0; i < things.Length; i++)
            {
                ThingType thing = things[i];
                if (thing != null)
                {
                    thing = this.InternalRemoveThing(thing.ID, thing.Category);
                    if (thing != null)
                    {
                        changedThings.Add(thing);
                    }
                }
            }

            if (changedThings.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new ThingListChangedArgs(changedThings.ToArray(), StorageChangeType.Remove));
                }

                return true;
            }

            return false;
        }

        public bool HasThing(ushort id, ThingCategory category)
        {
            if (!this.Loaded || category == ThingCategory.Invalid)
            {
                return false;
            }

            switch (category)
            {
                case ThingCategory.Item:
                    return this.Items.ContainsKey(id);

                case ThingCategory.Outfit:
                    return this.Outfits.ContainsKey(id);

                case ThingCategory.Effect:
                    return this.Effects.ContainsKey(id);

                case ThingCategory.Missile:
                    return this.Missiles.ContainsKey(id);
            }

            return false;
        }

        public ThingType GetThing(ushort id, ThingCategory category)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.Loaded || id == 0 || category == ThingCategory.Invalid)
            {
                return null;
            }

            switch (category)
            {
                case ThingCategory.Item:
                    {
                        if (this.Items.ContainsKey(id))
                        {
                            return this.Items[id];
                        }

                        break;
                    }

                case ThingCategory.Outfit:
                    {
                        if (this.Outfits.ContainsKey(id))
                        {
                            return this.Outfits[id];
                        }

                        break;
                    }

                case ThingCategory.Effect:
                    {
                        if (this.Effects.ContainsKey(id))
                        {
                            return this.Effects[id];
                        }

                        break;
                    }

                case ThingCategory.Missile:
                    {
                        if (this.Missiles.ContainsKey(id))
                        {
                            return this.Missiles[id];
                        }

                        break;
                    }
            }

            return null;
        }

        public ThingType GetItem(ushort id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (this.Loaded && this.Items.ContainsKey(id))
            {
                return this.Items[id];
            }

            return null;
        }

        public ThingType GetOutfit(ushort id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (this.Loaded && this.Outfits.ContainsKey(id))
            {
                return this.Outfits[id];
            }

            return null;
        }

        public ThingType GetEffect(ushort id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (this.Loaded && this.Effects.ContainsKey(id))
            {
                return this.Effects[id];
            }

            return null;
        }

        public ThingType GetMissile(ushort id)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (this.Loaded && this.Missiles.ContainsKey(id))
            {
                return this.Missiles[id];
            }

            return null;
        }

        public bool Save(string path, AssetsVersion version, AssetsFeatures features)
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

            if (!this.Loaded)
            {
                return false;
            }

            if (features == AssetsFeatures.None || features == AssetsFeatures.Transparency)
            {
                features |= version.Value >= (ushort)MetadataFormat.Format_755 ? AssetsFeatures.PatternsZ : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_960 ? AssetsFeatures.Extended : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_1050 ? AssetsFeatures.FramesDuration : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_1057 ? AssetsFeatures.FrameGroups : features;
            }

            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!this.Changed && this.Version.Equals(version) && this.ClientFeatures == features &&
                this.FilePath != null && !this.FilePath.Equals(path))
            {
                // just copy the content if nothing has changed.
                File.Copy(this.FilePath, path, true);

                if (this.ProgressChanged != null)
                {
                    this.ProgressChanged(this, 100);
                }
            }
            else
            {
                string tmpPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(path) + ".tmp");

                using (FlagsBinaryWriter writer = new FlagsBinaryWriter(new FileStream(tmpPath, FileMode.Create)))
                {
                    // write the signature.
                    writer.Write(version.DatSignature);

                    // write item, outfit, effect and missile count.
                    writer.Write(this.ItemCount);
                    writer.Write(this.OutfitCount);
                    writer.Write(this.EffectCount);
                    writer.Write(this.MissileCount);

                    int total = this.ItemCount + this.OutfitCount + this.EffectCount + this.MissileCount;
                    int compiled = 0;

                    // write item list.
                    for (ushort id = 100; id <= this.ItemCount; id++)
                    {
                        ThingType item = this.Items[id];
                        if (!ThingTypeSerializer.WriteProperties(item, version.Format, writer) ||
                            !ThingTypeSerializer.WriteTexturePatterns(item, features, writer))
                        {
                            throw new Exception("Items list cannot be compiled.");
                        }
                    }

                    // update progress.
                    if (this.ProgressChanged != null)
                    {
                        compiled += this.ItemCount;
                        this.ProgressChanged(this, (compiled * 100) / total);
                    }

                    bool onlyOneGroup = ((this.ClientFeatures & AssetsFeatures.FrameGroups) == AssetsFeatures.FrameGroups) &&
                                        ((features & AssetsFeatures.FrameGroups) != AssetsFeatures.FrameGroups);

                    // write outfit list.
                    for (ushort id = 1; id <= this.OutfitCount; id++)
                    {
                        ThingType outfit = onlyOneGroup ? ThingType.ToSingleFrameGroup(this.Outfits[id]) : this.Outfits[id];
                        if (!ThingTypeSerializer.WriteProperties(outfit, version.Format, writer) ||
                            !ThingTypeSerializer.WriteTexturePatterns(outfit, features, writer))
                        {
                            throw new Exception("Outfits list cannot be compiled.");
                        }
                    }

                    // update progress.
                    if (this.ProgressChanged != null)
                    {
                        compiled += this.OutfitCount;
                        this.ProgressChanged(this, (compiled * 100) / total);
                    }

                    // write effect list.
                    for (ushort id = 1; id <= this.EffectCount; id++)
                    {
                        ThingType effect = this.Effects[id];
                        if (!ThingTypeSerializer.WriteProperties(effect, version.Format, writer) ||
                            !ThingTypeSerializer.WriteTexturePatterns(effect, features, writer))
                        {
                            throw new Exception("Effects list cannot be compiled.");
                        }
                    }

                    // update progress.
                    if (this.ProgressChanged != null)
                    {
                        compiled += this.EffectCount;
                        this.ProgressChanged(this, (compiled * 100) / total);
                    }

                    // write missile list.
                    for (ushort id = 1; id <= this.MissileCount; id++)
                    {
                        ThingType missile = this.Missiles[id];
                        if (!ThingTypeSerializer.WriteProperties(missile, version.Format, writer) ||
                            !ThingTypeSerializer.WriteTexturePatterns(missile, features, writer))
                        {
                            throw new Exception("Missiles list cannot be compiled.");
                        }
                    }

                    // update progress.
                    if (this.ProgressChanged != null)
                    {
                        compiled += this.MissileCount;
                        this.ProgressChanged(this, (compiled * 100) / total);
                    }
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tmpPath, path);
            }

            this.FilePath = path;
            this.Version = version;
            this.ClientFeatures = features;
            this.Changed = false;

            if (this.StorageCompiled != null)
            {
                this.StorageCompiled(this);
            }

            return true;
        }

        public bool Save(string path, AssetsVersion version)
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            return this.Save(path, version, AssetsFeatures.None);
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

        public void Dispose()
        {
            this.Disposed = true;

            if (!this.Loaded)
            {
                return;
            }

            this.FilePath = null;
            this.Version = null;
            this.Items.Clear();
            this.Items = null;
            this.ItemCount = 0;
            this.Outfits.Clear();
            this.Outfits = null;
            this.OutfitCount = 0;
            this.Effects.Clear();
            this.Effects = null;
            this.EffectCount = 0;
            this.Missiles.Clear();
            this.Missiles = null;
            this.MissileCount = 0;
            this.Changed = false;
            this.Loaded = false;

            if (this.StorageDisposed != null)
            {
                this.StorageDisposed(this);
            }
        }

        #endregion

        #region | Private Methods |

        private bool InternalCreate(AssetsVersion version, AssetsFeatures features)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (this.Loaded)
            {
                return true;
            }

            if (features == AssetsFeatures.None || features == AssetsFeatures.Transparency)
            {
                features |= version.Value >= (ushort)MetadataFormat.Format_755 ? AssetsFeatures.PatternsZ : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_960 ? AssetsFeatures.Extended : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_1050 ? AssetsFeatures.FramesDuration : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_1057 ? AssetsFeatures.FrameGroups : features;
            }

            this.Version = version;
            this.ClientFeatures = features;
            this.Items.Add(100, ThingType.Create(100, ThingCategory.Item));
            this.ItemCount = 100;
            this.Outfits.Add(1, ThingType.Create(1, ThingCategory.Outfit));
            this.OutfitCount = 1;
            this.Effects.Add(1, ThingType.Create(1, ThingCategory.Effect));
            this.EffectCount = 1;
            this.Missiles.Add(1, ThingType.Create(1, ThingCategory.Missile));
            this.MissileCount = 1;
            this.Changed = true;
            this.Loaded = true;
            this.Disposed = false;
            return true;
        }

        private bool InternalLoad(string path, AssetsVersion version, AssetsFeatures features)
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
                throw new FileNotFoundException($"File not found: {path}", path);
            }

            if (this.Loaded)
            {
                return true;
            }

            if (features == AssetsFeatures.None || features == AssetsFeatures.Transparency)
            {
                features |= version.Value >= (ushort)MetadataFormat.Format_755 ? AssetsFeatures.PatternsZ : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_960 ? AssetsFeatures.Extended : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_1050 ? AssetsFeatures.FramesDuration : features;
                features |= version.Value >= (ushort)MetadataFormat.Format_1057 ? AssetsFeatures.FrameGroups : features;
            }

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(stream);

                uint signature = reader.ReadUInt32();
                if (signature != version.DatSignature)
                {
                    string message = "Invalid DAT signature. Expected signature is {0:X} and loaded signature is {1:X}.";
                    throw new Exception(string.Format(message, version.DatSignature, signature));
                }

                this.ItemCount = reader.ReadUInt16();
                this.OutfitCount = reader.ReadUInt16();
                this.EffectCount = reader.ReadUInt16();
                this.MissileCount = reader.ReadUInt16();

                int total = this.ItemCount + this.OutfitCount + this.EffectCount + this.MissileCount;
                int loaded = 0;

                // load item list.
                for (ushort id = 100; id <= this.ItemCount; id++)
                {
                    ThingType item = new ThingType(id, ThingCategory.Item);
                    if (!ThingTypeSerializer.ReadProperties(item, version.Format, reader) ||
                        !ThingTypeSerializer.ReadTexturePatterns(item, features, reader))
                    {
                        throw new Exception("Items list cannot be loaded.");
                    }

                    this.Items.Add(id, item);
                }

                // update progress.
                if (this.ProgressChanged != null)
                {
                    loaded += this.ItemCount;
                    this.ProgressChanged(this, (loaded * 100) / total);
                }

                // load outfit list.
                for (ushort id = 1; id <= this.OutfitCount; id++)
                {
                    ThingType outfit = new ThingType(id, ThingCategory.Outfit);
                    if (!ThingTypeSerializer.ReadProperties(outfit, version.Format, reader) ||
                        !ThingTypeSerializer.ReadTexturePatterns(outfit, features, reader))
                    {
                        throw new Exception("Outfits list cannot be loaded.");
                    }

                    this.Outfits.Add(id, outfit);
                }

                // update progress.
                if (this.ProgressChanged != null)
                {
                    loaded += this.OutfitCount;
                    this.ProgressChanged(this, (loaded * 100) / total);
                }

                // load effect list.
                for (ushort id = 1; id <= this.EffectCount; id++)
                {
                    ThingType effect = new ThingType(id, ThingCategory.Effect);
                    if (!ThingTypeSerializer.ReadProperties(effect, version.Format, reader) ||
                        !ThingTypeSerializer.ReadTexturePatterns(effect, features, reader))
                    {
                        throw new Exception("Effects list cannot be loaded.");
                    }

                    this.Effects.Add(id, effect);
                }

                // update progress.
                if (this.ProgressChanged != null)
                {
                    loaded += this.EffectCount;
                    this.ProgressChanged(this, (loaded * 100) / total);
                }

                // load missile list.
                for (ushort id = 1; id <= this.MissileCount; id++)
                {
                    ThingType missile = new ThingType(id, ThingCategory.Missile);
                    if (!ThingTypeSerializer.ReadProperties(missile, version.Format, reader) ||
                        !ThingTypeSerializer.ReadTexturePatterns(missile, features, reader))
                    {
                        throw new Exception("Missiles list cannot be loaded.");
                    }

                    this.Missiles.Add(id, missile);
                }

                // update progress.
                if (this.ProgressChanged != null)
                {
                    loaded += this.MissileCount;
                    this.ProgressChanged(this, (loaded * 100) / total);
                }
            }

            this.FilePath = path;
            this.Version = version;
            this.ClientFeatures = features;
            this.Changed = false;
            this.Loaded = true;
            this.Disposed = false;
            return true;
        }

        private ThingType InternalAddThing(ThingType thing)
        {
            if (thing == null || thing.Category == ThingCategory.Invalid)
            {
                return null;
            }

            ushort id = 0;

            switch (thing.Category)
            {
                case ThingCategory.Item:
                    id = ++this.ItemCount;
                    this.Items.Add(id, thing);
                    break;

                case ThingCategory.Outfit:
                    id = ++this.OutfitCount;
                    this.Outfits.Add(id, thing);
                    break;

                case ThingCategory.Effect:
                    id = ++this.EffectCount;
                    this.Effects.Add(id, thing);
                    break;

                case ThingCategory.Missile:
                    id = ++this.MissileCount;
                    this.Missiles.Add(id, thing);
                    break;
            }

            thing.ID = id;
            return thing;
        }

        private ThingType InternalReplaceThing(ThingType newThing, ThingType oldThing)
        {
            if (newThing == null || oldThing == null || newThing.Category != oldThing .Category || !this.HasThing(oldThing.ID, oldThing.Category))
            {
                return null;
            }

            switch (oldThing.Category)
            {
                case ThingCategory.Item:
                    this.Items[oldThing.ID] = newThing;
                    break;

                case ThingCategory.Outfit:
                    this.Outfits[oldThing.ID] = newThing;
                    break;

                case ThingCategory.Effect:
                    this.Effects[oldThing.ID] = newThing;
                    break;

                case ThingCategory.Missile:
                    this.Missiles[oldThing.ID] = newThing;
                    break;
            }

            newThing.ID = oldThing.ID;
            return oldThing;
        }

        private ThingType InternalRemoveThing(ushort id, ThingCategory category)
        {
            if (id == 0 || category == ThingCategory.Invalid || !this.HasThing(id, category))
            {
                return null;
            }

            ThingType changedThing = null;

            if (category == ThingCategory.Item)
            {
                changedThing = this.Items[id];

                if (id == this.ItemCount && id != 100)
                {
                    this.ItemCount = (ushort)(this.ItemCount - 1);
                    this.Items.Remove(id);
                }
                else
                {
                    this.Items[id] = ThingType.Create(id, category);
                }
            }
            else if (category == ThingCategory.Outfit)
            {
                changedThing = this.Outfits[id];

                if (id == this.OutfitCount && id != 1)
                {
                    this.OutfitCount = (ushort)(this.OutfitCount - 1);
                    this.Outfits.Remove(id);
                }
                else
                {
                    this.Outfits[id] = ThingType.Create(id, category);
                }
            }
            else if (category == ThingCategory.Effect)
            {
                changedThing = this.Effects[id];

                if (id == this.EffectCount && id != 1)
                {
                    this.EffectCount = (ushort)(this.EffectCount - 1);
                    this.Effects.Remove(id);
                }
                else
                {
                    this.Effects[id] = ThingType.Create(id, category);
                }
            }
            else if (category == ThingCategory.Missile)
            {
                changedThing = this.Missiles[id];

                if (id == this.MissileCount && id != 1)
                {
                    this.MissileCount = (ushort)(this.MissileCount - 1);
                    this.Missiles.Remove(id);
                }
                else
                {
                    this.Missiles[id] = ThingType.Create(id, category);
                }
            }

            return changedThing;
        }

        #endregion

        #region | Public Static Methods |

        public static ThingTypeStorage Create(AssetsVersion version, AssetsFeatures features)
        {
            ThingTypeStorage storage = new ThingTypeStorage();
            if (storage.InternalCreate(version, features))
            {
                return storage;
            }

            return null;
        }

        public static ThingTypeStorage Create(AssetsVersion version)
        {
            ThingTypeStorage storage = new ThingTypeStorage();
            if (storage.InternalCreate(version, AssetsFeatures.None))
            {
                return storage;
            }

            return null;
        }

        public static ThingTypeStorage Load(string path, AssetsVersion version, AssetsFeatures features)
        {
            ThingTypeStorage storage = new ThingTypeStorage();
            if (storage.InternalLoad(path, version, features))
            {
                return storage;
            }

            return null;
        }

        public static ThingTypeStorage Load(string path, AssetsVersion version)
        {
            ThingTypeStorage storage = new ThingTypeStorage();
            if (storage.InternalLoad(path, version, AssetsFeatures.None))
            {
                return storage;
            }

            return null;
        }

        #endregion
    }
}
