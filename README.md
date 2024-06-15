AdditionalNetworking
============
[![GitHub Release](https://img.shields.io/github/v/release/mattymatty97/LTC_AdditionalNetworking?display_name=release&logo=github&logoColor=white)](https://github.com/mattymatty97/LTC_AdditionalNetworking/releases/latest)
[![GitHub Pre-Release](https://img.shields.io/github/v/release/mattymatty97/LTC_AdditionalNetworking?include_prereleases&display_name=release&logo=github&logoColor=white&label=preview)](https://github.com/mattymatty97/LTC_AdditionalNetworking/releases)  
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/mattymatty/AdditionalNetworking?style=flat&logo=thunderstore&logoColor=white&label=thunderstore)](https://thunderstore.io/c/lethal-company/p/mattymatty/AdditionalNetworking/)

Towards a future with less de-syncs!

Use more Explicit networking for stuff like selected slot and inventory

Currently patched:
- Current Held Slot ( using explicit slot id instead of forward/backward)
- GrabbedObject slot ( streaming the entire inventory snapshot instead of relying on the other clients to guess where the objects are )
- Shotgun ammo ( Owner will broadcast the explicit ammo amount )
- Shotgun safety ( Owner will broadcast the explicit safety status instead of toggle )
- Shotgun status ( Clients will request shotgun status from Host upon spawn )
- Boombox playing ( Owner will broadcast the explicit track id and playing status )
- Boombox status ( Clients will request Boombox status from Host upon spawn )
- Player Username ( Owner will sync the name of his playerObject )
- Sync scrap value if value is missing ( Client will request values from server )

Planned:
- Television sync ( status / play time )

Planned but might not happen:
- Vanilla compatibility ( allow vanilla clients to join )

### **WARNING!**
this mod will work only if both the host and the client have the mod

Installation
------------

- Install [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/)
- Unzip this mod into your `BepInEx/plugins` folder

Or use the mod manager to handle the installing for you.
