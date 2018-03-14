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
using OpenTibia.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenTibia.Assets
{
    public class ThingTypeStorage : IStorage, IDisposable
    {
        private ThingTypeStorage()
        {
            Items = new Dictionary<ushort, ThingType>();
            Outfits = new Dictionary<ushort, ThingType>();
            Effects = new Dictionary<ushort, ThingType>();
            Missiles = new Dictionary<ushort, ThingType>();
        }

        public event ThingListChangedHandler StorageChanged;

        public event StorageHandler StorageCompiled;

        public event StorageHandler StorageDisposed;

        public event ProgressHandler ProgressChanged;

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
                return Loaded && FilePath == null;
            }
        }

        public bool Changed { get; private set; }

        public bool Loaded { get; private set; }

        public bool Disposed { get; private set; }

        public bool AddThing(ThingType thing)
        {
            if (thing == null)
            {
                throw new ArgumentNullException(nameof(thing));
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded)
            {
                return false;
            }

            ThingType changedThing = InternalAddThing(thing);
            if (changedThing != null)
            {
                Changed = true;

                if (StorageChanged != null)
                {
                    StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Add));
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

            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded || things.Length == 0)
            {
                return false;
            }

            List<ThingType> changedThings = new List<ThingType>();

            for (int i = 0; i < things.Length; i++)
            {
                ThingType thing = InternalAddThing(things[i]);
                if (thing != null)
                {
                    changedThings.Add(thing);
                }
            }

            if (changedThings.Count != 0)
            {
                Changed = true;

                if (StorageChanged != null)
                {
                    StorageChanged(this, new ThingListChangedArgs(changedThings.ToArray(), StorageChangeType.Add));
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

            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded)
            {
                return false;
            }

            ThingType changedThing = InternalReplaceThing(thing, GetThing(replaceId, thing.Category));
            if (changedThing != null)
            {
                Changed = true;

                if (StorageChanged != null)
                {
                    StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Replace));
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

            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded)
            {
                return false;
            }

            ThingType changedThing = InternalReplaceThing(thing, GetThing(thing.ID, thing.Category));
            if (changedThing != null)
            {
                Changed = true;

                if (StorageChanged != null)
                {
                    StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Replace));
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

            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded || things.Length == 0)
            {
                return false;
            }

            List<ThingType> changedThings = new List<ThingType>();

            for (int i = 0; i < things.Length; i++)
            {
                ThingType thing = things[i];
                if (thing != null)
                {
                    thing = InternalReplaceThing(thing, GetThing(thing.ID, thing.Category));
                    if (thing != null)
                    {
                        changedThings.Add(thing);
                    }
                }
            }

            if (changedThings.Count != 0)
            {
                Changed = true;

                if (StorageChanged != null)
                {
                    StorageChanged(this, new ThingListChangedArgs(changedThings.ToArray(), StorageChangeType.Replace));
                }

                return true;
            }

            return false;
        }

        public bool RemoveThing(ushort id, ThingCategory category)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded || category == ThingCategory.Invalid)
            {
                return false;
            }

            ThingType changedThing = InternalRemoveThing(id, category);
            if (changedThing != null)
            {
                Changed = true;

                if (StorageChanged != null)
                {
                    StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Remove));
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

            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded || thing.Category == ThingCategory.Invalid)
            {
                return false;
            }

            ThingType changedThing = InternalRemoveThing(thing.ID, thing.Category);
            if (changedThing != null)
            {
                Changed = true;

                if (StorageChanged != null)
                {
                    StorageChanged(this, new ThingListChangedArgs(new ThingType[] { changedThing }, StorageChangeType.Remove));
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

            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded || things.Length == 0)
            {
                return false;
            }

            List<ThingType> changedThings = new List<ThingType>();

            for (int i = 0; i < things.Length; i++)
            {
                ThingType thing = things[i];
                if (thing != null)
                {
                    thing = InternalRemoveThing(thing.ID, thing.Category);
                    if (thing != null)
                    {
                        changedThings.Add(thing);
                    }
                }
            }

            if (changedThings.Count != 0)
            {
                Changed = true;

                if (StorageChanged != null)
                {
                    StorageChanged(this, new ThingListChangedArgs(changedThings.ToArray(), StorageChangeType.Remove));
                }

                return true;
            }

            return false;
        }

        public bool HasThing(ushort id, ThingCategory category)
        {
            if (!Loaded || category == ThingCategory.Invalid)
            {
                return false;
            }

            switch (category)
            {
                case ThingCategory.Item:
                    return Items.ContainsKey(id);

                case ThingCategory.Outfit:
                    return Outfits.ContainsKey(id);

                case ThingCategory.Effect:
                    return Effects.ContainsKey(id);

                case ThingCategory.Missile:
                    return Missiles.ContainsKey(id);
            }

            return false;
        }

        public ThingType GetThing(ushort id, ThingCategory category)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded || id == 0 || category == ThingCategory.Invalid)
            {
                return null;
            }

            switch (category)
            {
                case ThingCategory.Item:
                    {
                        if (Items.ContainsKey(id))
                        {
                            return Items[id];
                        }

                        break;
                    }

                case ThingCategory.Outfit:
                    {
                        if (Outfits.ContainsKey(id))
                        {
                            return Outfits[id];
                        }

                        break;
                    }

                case ThingCategory.Effect:
                    {
                        if (Effects.ContainsKey(id))
                        {
                            return Effects[id];
                        }

                        break;
                    }

                case ThingCategory.Missile:
                    {
                        if (Missiles.ContainsKey(id))
                        {
                            return Missiles[id];
                        }

                        break;
                    }
            }

            return null;
        }

        public ThingType GetItem(ushort id)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (Loaded && Items.ContainsKey(id))
            {
                return Items[id];
            }

            return null;
        }

        public ThingType GetOutfit(ushort id)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (Loaded && Outfits.ContainsKey(id))
            {
                return Outfits[id];
            }

            return null;
        }

        public ThingType GetEffect(ushort id)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (Loaded && Effects.ContainsKey(id))
            {
                return Effects[id];
            }

            return null;
        }

        public ThingType GetMissile(ushort id)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (Loaded && Missiles.ContainsKey(id))
            {
                return Missiles[id];
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

            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (!Loaded)
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

            if (!Changed && Version.Equals(version) && ClientFeatures == features &&
                FilePath != null && !FilePath.Equals(path))
            {
                // just copy the content if nothing has changed.
                File.Copy(FilePath, path, true);

                if (ProgressChanged != null)
                {
                    ProgressChanged(this, 100);
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
                    writer.Write(ItemCount);
                    writer.Write(OutfitCount);
                    writer.Write(EffectCount);
                    writer.Write(MissileCount);

                    int total = ItemCount + OutfitCount + EffectCount + MissileCount;
                    int compiled = 0;

                    // write item list.
                    for (ushort id = 100; id <= ItemCount; id++)
                    {
                        ThingType item = Items[id];
                        if (!ThingTypeSerializer.WriteProperties(item, version.Format, writer) ||
                            !ThingTypeSerializer.WriteTexturePatterns(item, features, writer))
                        {
                            throw new Exception("Items list cannot be compiled.");
                        }
                    }

                    // update progress.
                    if (ProgressChanged != null)
                    {
                        compiled += ItemCount;
                        ProgressChanged(this, (compiled * 100) / total);
                    }

                    bool onlyOneGroup = ((ClientFeatures & AssetsFeatures.FrameGroups) == AssetsFeatures.FrameGroups) &&
                                        ((features & AssetsFeatures.FrameGroups) != AssetsFeatures.FrameGroups);

                    // write outfit list.
                    for (ushort id = 1; id <= OutfitCount; id++)
                    {
                        ThingType outfit = onlyOneGroup ? ThingType.ToSingleFrameGroup(Outfits[id]) : Outfits[id];
                        if (!ThingTypeSerializer.WriteProperties(outfit, version.Format, writer) ||
                            !ThingTypeSerializer.WriteTexturePatterns(outfit, features, writer))
                        {
                            throw new Exception("Outfits list cannot be compiled.");
                        }
                    }

                    // update progress.
                    if (ProgressChanged != null)
                    {
                        compiled += OutfitCount;
                        ProgressChanged(this, (compiled * 100) / total);
                    }

                    // write effect list.
                    for (ushort id = 1; id <= EffectCount; id++)
                    {
                        ThingType effect = Effects[id];
                        if (!ThingTypeSerializer.WriteProperties(effect, version.Format, writer) ||
                            !ThingTypeSerializer.WriteTexturePatterns(effect, features, writer))
                        {
                            throw new Exception("Effects list cannot be compiled.");
                        }
                    }

                    // update progress.
                    if (ProgressChanged != null)
                    {
                        compiled += EffectCount;
                        ProgressChanged(this, (compiled * 100) / total);
                    }

                    // write missile list.
                    for (ushort id = 1; id <= MissileCount; id++)
                    {
                        ThingType missile = Missiles[id];
                        if (!ThingTypeSerializer.WriteProperties(missile, version.Format, writer) ||
                            !ThingTypeSerializer.WriteTexturePatterns(missile, features, writer))
                        {
                            throw new Exception("Missiles list cannot be compiled.");
                        }
                    }

                    // update progress.
                    if (ProgressChanged != null)
                    {
                        compiled += MissileCount;
                        ProgressChanged(this, (compiled * 100) / total);
                    }
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tmpPath, path);
            }

            FilePath = path;
            Version = version;
            ClientFeatures = features;
            Changed = false;

            if (StorageCompiled != null)
            {
                StorageCompiled(this);
            }

            return true;
        }

        public bool Save(string path, AssetsVersion version)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return Save(path, version, AssetsFeatures.None);
        }

        public bool Save()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (Changed && !IsTemporary)
            {
                return Save(FilePath, Version, ClientFeatures);
            }

            return true;
        }

        public void Dispose()
        {
            Disposed = true;

            if (!Loaded)
            {
                return;
            }

            FilePath = null;
            Version = null;
            Items.Clear();
            Items = null;
            ItemCount = 0;
            Outfits.Clear();
            Outfits = null;
            OutfitCount = 0;
            Effects.Clear();
            Effects = null;
            EffectCount = 0;
            Missiles.Clear();
            Missiles = null;
            MissileCount = 0;
            Changed = false;
            Loaded = false;

            if (StorageDisposed != null)
            {
                StorageDisposed(this);
            }
        }

        private bool InternalCreate(AssetsVersion version, AssetsFeatures features)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
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
            Items.Add(100, ThingType.Create(100, ThingCategory.Item));
            ItemCount = 100;
            Outfits.Add(1, ThingType.Create(1, ThingCategory.Outfit));
            OutfitCount = 1;
            Effects.Add(1, ThingType.Create(1, ThingCategory.Effect));
            EffectCount = 1;
            Missiles.Add(1, ThingType.Create(1, ThingCategory.Missile));
            MissileCount = 1;
            Changed = true;
            Loaded = true;
            Disposed = false;
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

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(stream);

                uint signature = reader.ReadUInt32();
                if (signature != version.DatSignature)
                {
                    string message = "Invalid DAT signature. Expected signature is {0:X} and loaded signature is {1:X}.";
                    throw new Exception(string.Format(message, version.DatSignature, signature));
                }

                ItemCount = reader.ReadUInt16();
                OutfitCount = reader.ReadUInt16();
                EffectCount = reader.ReadUInt16();
                MissileCount = reader.ReadUInt16();

                int total = ItemCount + OutfitCount + EffectCount + MissileCount;
                int loaded = 0;

                // load item list.
                for (ushort id = 100; id <= ItemCount; id++)
                {
                    ThingType item = new ThingType(id, ThingCategory.Item);
                    if (!ThingTypeSerializer.ReadProperties(item, version.Format, reader) ||
                        !ThingTypeSerializer.ReadTexturePatterns(item, features, reader))
                    {
                        throw new Exception("Items list cannot be loaded.");
                    }

                    Items.Add(id, item);
                }

                // update progress.
                if (ProgressChanged != null)
                {
                    loaded += ItemCount;
                    ProgressChanged(this, (loaded * 100) / total);
                }

                // load outfit list.
                for (ushort id = 1; id <= OutfitCount; id++)
                {
                    ThingType outfit = new ThingType(id, ThingCategory.Outfit);
                    if (!ThingTypeSerializer.ReadProperties(outfit, version.Format, reader) ||
                        !ThingTypeSerializer.ReadTexturePatterns(outfit, features, reader))
                    {
                        throw new Exception("Outfits list cannot be loaded.");
                    }

                    Outfits.Add(id, outfit);
                }

                // update progress.
                if (ProgressChanged != null)
                {
                    loaded += OutfitCount;
                    ProgressChanged(this, (loaded * 100) / total);
                }

                // load effect list.
                for (ushort id = 1; id <= EffectCount; id++)
                {
                    ThingType effect = new ThingType(id, ThingCategory.Effect);
                    if (!ThingTypeSerializer.ReadProperties(effect, version.Format, reader) ||
                        !ThingTypeSerializer.ReadTexturePatterns(effect, features, reader))
                    {
                        throw new Exception("Effects list cannot be loaded.");
                    }

                    Effects.Add(id, effect);
                }

                // update progress.
                if (ProgressChanged != null)
                {
                    loaded += EffectCount;
                    ProgressChanged(this, (loaded * 100) / total);
                }

                // load missile list.
                for (ushort id = 1; id <= MissileCount; id++)
                {
                    ThingType missile = new ThingType(id, ThingCategory.Missile);
                    if (!ThingTypeSerializer.ReadProperties(missile, version.Format, reader) ||
                        !ThingTypeSerializer.ReadTexturePatterns(missile, features, reader))
                    {
                        throw new Exception("Missiles list cannot be loaded.");
                    }

                    Missiles.Add(id, missile);
                }

                // update progress.
                if (ProgressChanged != null)
                {
                    loaded += MissileCount;
                    ProgressChanged(this, (loaded * 100) / total);
                }
            }

            FilePath = path;
            Version = version;
            ClientFeatures = features;
            Changed = false;
            Loaded = true;
            Disposed = false;
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
                    id = ++ItemCount;
                    Items.Add(id, thing);
                    break;

                case ThingCategory.Outfit:
                    id = ++OutfitCount;
                    Outfits.Add(id, thing);
                    break;

                case ThingCategory.Effect:
                    id = ++EffectCount;
                    Effects.Add(id, thing);
                    break;

                case ThingCategory.Missile:
                    id = ++MissileCount;
                    Missiles.Add(id, thing);
                    break;
            }

            thing.ID = id;
            return thing;
        }

        private ThingType InternalReplaceThing(ThingType newThing, ThingType oldThing)
        {
            if (newThing == null || oldThing == null || newThing.Category != oldThing .Category || !HasThing(oldThing.ID, oldThing.Category))
            {
                return null;
            }

            switch (oldThing.Category)
            {
                case ThingCategory.Item:
                    Items[oldThing.ID] = newThing;
                    break;

                case ThingCategory.Outfit:
                    Outfits[oldThing.ID] = newThing;
                    break;

                case ThingCategory.Effect:
                    Effects[oldThing.ID] = newThing;
                    break;

                case ThingCategory.Missile:
                    Missiles[oldThing.ID] = newThing;
                    break;
            }

            newThing.ID = oldThing.ID;
            return oldThing;
        }

        private ThingType InternalRemoveThing(ushort id, ThingCategory category)
        {
            if (id == 0 || category == ThingCategory.Invalid || !HasThing(id, category))
            {
                return null;
            }

            ThingType changedThing = null;

            if (category == ThingCategory.Item)
            {
                changedThing = Items[id];

                if (id == ItemCount && id != 100)
                {
                    ItemCount = (ushort)(ItemCount - 1);
                    Items.Remove(id);
                }
                else
                {
                    Items[id] = ThingType.Create(id, category);
                }
            }
            else if (category == ThingCategory.Outfit)
            {
                changedThing = Outfits[id];

                if (id == OutfitCount && id != 1)
                {
                    OutfitCount = (ushort)(OutfitCount - 1);
                    Outfits.Remove(id);
                }
                else
                {
                    Outfits[id] = ThingType.Create(id, category);
                }
            }
            else if (category == ThingCategory.Effect)
            {
                changedThing = Effects[id];

                if (id == EffectCount && id != 1)
                {
                    EffectCount = (ushort)(EffectCount - 1);
                    Effects.Remove(id);
                }
                else
                {
                    Effects[id] = ThingType.Create(id, category);
                }
            }
            else if (category == ThingCategory.Missile)
            {
                changedThing = Missiles[id];

                if (id == MissileCount && id != 1)
                {
                    MissileCount = (ushort)(MissileCount - 1);
                    Missiles.Remove(id);
                }
                else
                {
                    Missiles[id] = ThingType.Create(id, category);
                }
            }

            return changedThing;
        }

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
    }
}
