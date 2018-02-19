### Open Tibia Framework

---

#### Loading versions from a xml file

XML format sample
``` XML
<versions>
    <version value="1079" description="Client 10.79" dat="3A71" spr="557A5E34" otb="56" />
</versions>
```
Code
``` C#
// path to the versions xml file.
string path = @"versions.xml";

// creates a VersionStorage instance.
OpenTibia.Core.VersionStorage versions = new OpenTibia.Core.VersionStorage();

// loads the xml
versions.Load(path);

// gets a version from the storage by the signatures.
OpenTibia.Core.Version version = versions.GetBySignatures(0x3A71, 0x557A5E34);

// gets all versions 10.79
System.Collections.Generic.List<OpenTibia.Core.Version> result = versions.GetByVersionValue(1079);

// adds a new version.
versions.AddVersion(new OpenTibia.Core.Version(1078, "Client 10.78", 0x39CC, 0x554C7373, 56));

// replaces a version by the signatures.
versions.ReplaceVersion(new OpenTibia.Core.Version(1078, "My description 10.78", 0x39CC, 0x554C7373, 56), 0x39CC, 0x554C7373);

// removes a version by the signatures.
versions.RemoveVersion(0x39CC, 0x554C7373);

// saves the xml.
versions.Save();
```

---

#### Loading and compiling a SPR file

``` C#
// creates a Version 10.79.
OpenTibia.Core.Version version = new OpenTibia.Core.Version(1079, "Client 10.79", 0x3A71, 0x557A5E34, 0);

// the path to the spr file.
string path = @"C:\Clients\10.79\Tibia.spr";

// loads the spr file.
OpenTibia.Client.Sprites.SpriteStorage sprites = OpenTibia.Client.Sprites.SpriteStorage.Load(path, version);

// gets a sprite from the storage
OpenTibia.Client.Sprites.Sprite sprite = sprites.GetSprite(100);

// adding a sprite.
sprites.AddSprite(new OpenTibia.Client.Sprites.Sprite());

// replacing a sprite.
sprites.ReplaceSprite(new OpenTibia.Client.Sprites.Sprite(), 12);

// removing a sprite.
sprites.RemoveSprite(10);

// compiles the spr file.
sprites.Save();
```

---

#### Loading and displaying sprites

``` C#
// Assuming that you have a SpriteListBox named 'spriteListBox' in the form.

// creates a Version 10.79.
OpenTibia.Core.Version version = new OpenTibia.Core.Version(1079, "Client 10.79", 0x3A71, 0x557A5E34, 0);

// the path to the spr file.
string path = @"C:\Clients\10.79\Tibia.spr";

// loads the spr file.
OpenTibia.Client.Sprites.SpriteStorage sprites = OpenTibia.Client.Sprites.SpriteStorage.Load(path, version);

// gets 100 sprites from the storage and displays in the SpriteListBox
OpenTibia.Client.Sprites.Sprite[] list = new OpenTibia.Client.Sprites.Sprite[100];

for (uint i = 0; i < list.Length; i++)
{
    list[i] = sprites.GetSprite(i);
}

this.spriteListBox.AddRange(list);
```

---

#### SpriteListBox control

A control to display a list of sprites.

![SpriteListBox](http://s22.postimg.org/48ttg8xpd/Sprite_List_Box.png)

#### EightBitColorGrid control

A control that displays minimap and light colors.

![EightBitColorGrid](http://s15.postimg.org/ah0eeme8b/Hsi_Color_Grid.png)

#### HsiColorGrid control

A control that displays outfit colors.

![HsiColorGrid](http://s13.postimg.org/526xlym2f/Eight_Bit_Color_Grid.png)
