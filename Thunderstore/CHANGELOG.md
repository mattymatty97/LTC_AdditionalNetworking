### v1.1.0
- bundle our own Preloader to add the fields we need to the classes
- less spammy check for scrap without a value

### v1.0.9
- Forgot to remove Ceccil dependent code

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