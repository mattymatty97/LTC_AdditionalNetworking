### v2.4.0
- Update to V80

### v2.3.5
- use MonkeyInjectionLibrary for a more robust way of editing base-game classes

### v2.3.4
- update to v73 and use Interfaces to add properties more cleanly

### v2.3.3
- prevent host asking itself for values

### v2.3.2
- do not pocket and restore held items if they are already the correct item

### v2.3.1

- fix a conversion error that makes inventory re-sync each frame

### v2.3.0
- improve security of the namedMessages
- sync shotgun values on grab
- add replacement rpc for syncing grabbable values
- add option to suppress vanilla grabbable sync

### v2.2.2
- actually upload the updated mod

### v2.2.1
- sync `isCrouching` state to Host so Host AIs can make use of it

### v2.2.0
- sync sound state for noise making animated objects ( eg: ToyRobot and Dentures )

### v2.1.3
- allow certain item types to skip updating the ScanNode when syncing the scrap value

### v2.1.2
- add more explicit logs in case of syncing errors
- add try catches on network calls to prevent hard-crashes

### v2.1.0
- Do things the Unity Intended way

### v2.0.0
- Vanilla compatibility!!

### v1.1.2
- hide logs behind config toggle

### v1.1.0
- bundle our own Preloader to add the fields we need to the classes
- less spammy check for scrap without a value

### v1.0.9
- Forgot to remove Cecil dependent code

### v1.0.8
- Rollback to Stable
- Added request of 0-value scrap

### v1.0.7
- Remove Enemy syncing

### v1.0.6
- Remove position handling from Grabables
- Remove Parent syncing
- Grabbables now only sync Rotation

### v1.0.5
- revert v1.0.4
- add Networking to sync object parents

### v1.0.4
- change Network Transform to World Space

### v1.0.3
- Added Config options

### v1.0.2
- Add Enemy transform sync
- Add Nutcracker torso rotation sync
- only compute Nutcracker rotation on Owner

### v1.0.1
- Add Item transform sync
- only set item position to floor once
- only perform fall calculations on Owner
