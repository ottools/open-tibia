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
using OpenTibia.Assets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
#endregion

namespace OpenTibia.Common
{
    public delegate void VersionListChangedHandler(object sender, VersionListChangedArgs e);

    public class VersionStorage : IStorage
    {
        #region Constructor

        public VersionStorage()
        {
            this.Versions = new List<AssetsVersion>();
        }

        #endregion

        #region Events

        public event EventHandler StorageLoaded;

        public event VersionListChangedHandler StorageChanged;

        public event EventHandler StorageCompiled;

        public event EventHandler StorageUnloaded;

        #endregion

        #region Public Properties

        public List<AssetsVersion> Versions { get; private set; }

        public string FilePath { get; private set; }

        public bool IsTemporary
        {
            get
            {
                return this.Loaded && this.FilePath == null;
            }
        }

        public bool Changed { get; private set; }

        public bool Loaded { get; private set; }

        #endregion

        #region Public Methods

        public bool Load(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                string message = $"File not found: {path}"; // TODO: ResourceManager.GetString("Exception.FileNotFound");
                throw new FileNotFoundException(message, "path");
            }

            if (this.Loaded && !this.Unload())
            {
                return false;
            }

            XmlDocument versionsXML = new XmlDocument();
            versionsXML.Load(path);

            XmlNodeList versionList = versionsXML.GetElementsByTagName("version");
            foreach (XmlNode node in versionList)
            {
                ushort value = ushort.Parse(node.Attributes["value"].Value, NumberStyles.Integer);
                string description = node.Attributes["description"] != null ? node.Attributes["description"].Value : null;
                uint datSignature = uint.Parse(node.Attributes["dat"].Value, NumberStyles.HexNumber);
                uint sprSignature = uint.Parse(node.Attributes["spr"].Value, NumberStyles.HexNumber);
                ushort otbValue = ushort.Parse(node.Attributes["otb"].Value, NumberStyles.Integer);

                if (value == 0)
                {
                    throw new Exception("Invalid version value.");
                }

                if (datSignature == 0 || sprSignature == 0)
                {
                    throw new Exception("Invalid signatures.");
                }

                for (int i = 0; i < this.Versions.Count; i++)
                {
                    AssetsVersion version = this.Versions[i];
                    if (version.DatSignature == datSignature && version.SprSignature == sprSignature)
                    {
                        throw new Exception("Duplicated signatures.");
                    }
                }

                this.Versions.Add(new AssetsVersion(value, description, datSignature, sprSignature, otbValue));
            }

            this.FilePath = path;
            this.Changed = false;
            this.Loaded = true;

            if (this.StorageLoaded != null)
            {
                this.StorageLoaded(this, new EventArgs());
            }

            return true;
        }

        public bool AddVersion(AssetsVersion version)
        {
            if (!this.Loaded || version == null || !version.IsValid || this.GetBySignatures(version.DatSignature, version.SprSignature) != null)
            {
                return false;
            }

            this.Versions.Add(version);
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new VersionListChangedArgs(version, StorageChangeType.Add));
            }

            return true;
        }

        public bool ReplaceVersion(AssetsVersion newVersion, uint oldDatSignature, uint oldSprSignature)
        {
            if (!this.Loaded || newVersion == null || !newVersion.IsValid || oldDatSignature == 0 || oldSprSignature == 0)
            {
                return false;
            }

            AssetsVersion oldVersion = this.GetBySignatures(oldDatSignature, oldSprSignature);
            if (oldVersion == null)
            {
                return false;
            }

            int index = this.Versions.IndexOf(oldVersion);
            this.Versions[index] = newVersion;
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new VersionListChangedArgs(oldVersion, StorageChangeType.Replace));
            }

            return true;
        }

        public bool ReplaceVersion(AssetsVersion newVersion)
        {
            if (!this.Loaded || newVersion == null || !newVersion.IsValid)
            {
                return false;
            }

            AssetsVersion oldVersion = this.GetBySignatures(newVersion.DatSignature, newVersion.SprSignature);
            if (oldVersion == null)
            {
                return false;
            }

            int index = this.Versions.IndexOf(oldVersion);
            this.Versions[index] = newVersion;
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new VersionListChangedArgs(oldVersion, StorageChangeType.Replace));
            }

            return true;
        }

        public bool RemoveVersion(uint datSignature, uint sprSignature)
        {
            if (!this.Loaded || datSignature == 0 || sprSignature == 0)
            {
                return false;
            }

            AssetsVersion version = this.GetBySignatures(datSignature, sprSignature);
            if (version == null)
            {
                return false;
            }

            this.Versions.Remove(version);
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new VersionListChangedArgs(version, StorageChangeType.Remove));
            }

            return true;
        }

        public AssetsVersion GetBySignatures(uint dat, uint spr)
        {
            if (dat != 0 && spr != 0)
            {
                for (int i = 0; i < this.Versions.Count; i++)
                {
                    AssetsVersion version = this.Versions[i];
                    if (version.DatSignature == dat && version.SprSignature == spr)
                    {
                        return version;
                    }
                }
            }

            return null;
        }

        public AssetsVersion GetBySignatures(int dat, int spr)
        {
            if (dat > 0 && spr > 0)
            {
                return this.GetBySignatures((uint)dat, (uint)spr);
            }

            return null;
        }

        public List<AssetsVersion> GetByVersionValue(uint value)
        {
            List<AssetsVersion> found = new List<AssetsVersion>();

            if (value != 0)
            {
                for (int i = 0; i < this.Versions.Count; i++)
                {
                    AssetsVersion version = this.Versions[i];
                    if (version.Value == value)
                    {
                        found.Add(version);
                    }
                }
            }

            return found;
        }

        public List<AssetsVersion> GetByVersionValue(int value)
        {
            if (value > 0)
            {
                return this.GetByVersionValue((uint)value);
            }

            return null;
        }

        public List<AssetsVersion> GetByOtbValue(uint otb)
        {
            List<AssetsVersion> found = new List<AssetsVersion>();

            if (otb != 0)
            {
                for (int i = 0; i < this.Versions.Count; i++)
                {
                    AssetsVersion version = this.Versions[i];
                    if (version.OtbValue == otb)
                    {
                        found.Add(version);
                    }
                }
            }

            return found;
        }

        public List<AssetsVersion> GetByOtbValue(int otb)
        {
            if (otb > 0)
            {
                return this.GetByOtbValue((uint)otb);
            }

            return null;
        }

        public AssetsVersion[] GetAllVersions()
        {
            return this.Versions.ToArray();
        }

        public bool Save()
        {
            if (this.Changed && !this.IsTemporary)
            {
                return this.Save(this.FilePath);
            }

            return false;
        }

        public bool Save(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!this.Loaded)
            {
                return false;
            }

            if (!this.Changed && this.FilePath != null && path.Equals(this.FilePath))
            {
                return true;
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = @"    ";
            settings.NewLineChars = Environment.NewLine;
            settings.NewLineHandling = NewLineHandling.Replace;

            try
            {
                using (XmlWriter writer = XmlWriter.Create(path, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("versions");

                    foreach (AssetsVersion version in this.Versions)
                    {
                        writer.WriteStartElement("version");
                        writer.WriteAttributeString("value", version.Value.ToString());
                        writer.WriteAttributeString("description", version.Description);
                        writer.WriteAttributeString("dat", string.Format("{0:X}", version.DatSignature));
                        writer.WriteAttributeString("spr", string.Format("{0:X}", version.SprSignature));
                        writer.WriteAttributeString("otb", version.OtbValue.ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();
                }
            }
            catch
            {
                return false;
            }

            this.FilePath = path;
            this.Changed = false;

            if (this.StorageCompiled != null)
            {
                this.StorageCompiled(this, new EventArgs());
            }

            return true;
        }

        public bool Unload()
        {
            if (!this.Loaded)
            {
                return false;
            }

            this.Versions.Clear();
            this.FilePath = null;
            this.Changed = false;
            this.Loaded = false;

            if (this.StorageUnloaded != null)
            {
                this.StorageUnloaded(this, new EventArgs());
            }

            return true;
        }

        #endregion
    }
}
