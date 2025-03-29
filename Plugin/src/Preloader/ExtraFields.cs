using System;
using System.Reflection;
using AdditionalNetworking.Utils;
using GameNetcodeStuff;

namespace AdditionalNetworking.Preloader;

public static class ExtraFields
{
    //GrabbableObject.AdditionalNetworking_isInitialized
    private static readonly FieldInfo GrabbableIsInitialized = typeof(GrabbableObject)
        .GetField("AdditionalNetworking_isInitialized", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<GrabbableObject, bool> _getterGrabbableIsInitialized;
    private static Func<GrabbableObject, bool, bool> _setterGrabbableIsInitialized;

    public static bool GetIsInitialized(this GrabbableObject @this)
    {
        _getterGrabbableIsInitialized ??= (Func<GrabbableObject, bool>)GrabbableIsInitialized.FastGetter();
        return _getterGrabbableIsInitialized(@this);
    }

    public static bool SetIsInitialized(this GrabbableObject @this, in bool value)
    {
        _setterGrabbableIsInitialized ??= (Func<GrabbableObject, bool, bool>)GrabbableIsInitialized.FastSetter();
        return _setterGrabbableIsInitialized(@this, value);
    }

    //GrabbableObject.AdditionalNetworking_hasRequestedSync
    private static readonly FieldInfo GrabbableHasRequestedSync = typeof(GrabbableObject)
        .GetField("AdditionalNetworking_hasRequestedSync", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<GrabbableObject, bool> _getterGrabbableHasRequestedSync;
    private static Func<GrabbableObject, bool, bool> _setterGrabbableHasRequestedSync;

    public static bool GetHasRequestedSync(this GrabbableObject @this)
    {
        _getterGrabbableHasRequestedSync ??= (Func<GrabbableObject, bool>)GrabbableHasRequestedSync.FastGetter();
        return _getterGrabbableHasRequestedSync(@this);
    }

    public static bool SetHasRequestedSync(this GrabbableObject @this, in bool value)
    {
        _setterGrabbableHasRequestedSync ??= (Func<GrabbableObject, bool, bool>)GrabbableHasRequestedSync.FastSetter();
        return _setterGrabbableHasRequestedSync(@this, value);
    }

    //PlayerControllerB.AdditionalNetworking_dirtySlots
    private static readonly FieldInfo PlayerControllerBDirtySlots = typeof(PlayerControllerB)
        .GetField("AdditionalNetworking_dirtySlots", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<PlayerControllerB, bool> _getterPlayerControllerBDirtySlots;
    private static Func<PlayerControllerB, bool, bool> _setterPlayerControllerBDirtySlots;

    public static bool GetDirtySlots(this PlayerControllerB @this)
    {
        _getterPlayerControllerBDirtySlots ??= (Func<PlayerControllerB, bool>)PlayerControllerBDirtySlots.FastGetter();
        return _getterPlayerControllerBDirtySlots(@this);
    }

    public static bool SetDirtySlots(this PlayerControllerB @this, in bool value)
    {
        _setterPlayerControllerBDirtySlots ??=
            (Func<PlayerControllerB, bool, bool>)PlayerControllerBDirtySlots.FastSetter();
        return _setterPlayerControllerBDirtySlots(@this, value);
    }

    //PlayerControllerB.AdditionalNetworking_dirtyInventory
    private static readonly FieldInfo PlayerControllerBDirtyInventory = typeof(PlayerControllerB)
        .GetField("AdditionalNetworking_dirtyInventory", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<PlayerControllerB, bool> _getterPlayerControllerBDirtyInventory;
    private static Func<PlayerControllerB, bool, bool> _setterPlayerControllerBDirtyInventory;

    public static bool GetDirtyInventory(this PlayerControllerB @this)
    {
        _getterPlayerControllerBDirtyInventory ??=
            (Func<PlayerControllerB, bool>)PlayerControllerBDirtyInventory.FastGetter();
        return _getterPlayerControllerBDirtyInventory(@this);
    }

    public static bool SetDirtyInventory(this PlayerControllerB @this, in bool value)
    {
        _setterPlayerControllerBDirtyInventory ??=
            (Func<PlayerControllerB, bool, bool>)PlayerControllerBDirtyInventory.FastSetter();
        return _setterPlayerControllerBDirtyInventory(@this, value);
    }

    //PlayerControllerB.AdditionalNetworking_lastCrouchState
    private static readonly FieldInfo PlayerControllerBLastCrouchState = typeof(PlayerControllerB)
        .GetField("AdditionalNetworking_lastCrouchState", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<PlayerControllerB, bool> _getterPlayerControllerBLastCrouchState;
    private static Func<PlayerControllerB, bool, bool> _setterPlayerControllerBLastCrouchState;

    public static bool GetLastCrouchState(this PlayerControllerB @this)
    {
        _getterPlayerControllerBLastCrouchState ??=
            (Func<PlayerControllerB, bool>)PlayerControllerBLastCrouchState.FastGetter();
        return _getterPlayerControllerBLastCrouchState(@this);
    }

    public static bool SetLastCrouchState(this PlayerControllerB @this, in bool value)
    {
        _setterPlayerControllerBLastCrouchState ??=
            (Func<PlayerControllerB, bool, bool>)PlayerControllerBLastCrouchState.FastSetter();
        return _setterPlayerControllerBLastCrouchState(@this, value);
    }

    //RoundManager.AdditionalNetworking_spawnedScrapPendingSync
    private static readonly FieldInfo RoundManagerSpawnedScrapPendingSync = typeof(RoundManager)
        .GetField("AdditionalNetworking_spawnedScrapPendingSync", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<RoundManager, bool> _getterRoundManagerSpawnedScrapPendingSync;
    private static Func<RoundManager, bool, bool> _setterRoundManagerSpawnedScrapPendingSync;

    public static bool GetSpawnedScrapPendingSync(this RoundManager @this)
    {
        _getterRoundManagerSpawnedScrapPendingSync ??=
            (Func<RoundManager, bool>)RoundManagerSpawnedScrapPendingSync.FastGetter();
        return _getterRoundManagerSpawnedScrapPendingSync(@this);
    }

    public static bool SetSpawnedScrapPendingSync(this RoundManager @this, in bool value)
    {
        _setterRoundManagerSpawnedScrapPendingSync ??=
            (Func<RoundManager, bool, bool>)RoundManagerSpawnedScrapPendingSync.FastSetter();
        return _setterRoundManagerSpawnedScrapPendingSync(@this, value);
    }

    //StartOfRound.AdditionalNetworking_unlockablesSynced
    private static readonly FieldInfo StartOfRoundUnlockablesSynced = typeof(StartOfRound)
        .GetField("AdditionalNetworking_unlockablesSynced", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<StartOfRound, bool> _getterStartOfRoundUnlockablesSynced;
    private static Func<StartOfRound, bool, bool> _setterStartOfRoundUnlockablesSynced;

    public static bool GetUnlockablesSynced(this StartOfRound @this)
    {
        _getterStartOfRoundUnlockablesSynced ??= (Func<StartOfRound, bool>)StartOfRoundUnlockablesSynced.FastGetter();
        return _getterStartOfRoundUnlockablesSynced(@this);
    }

    public static bool SetUnlockablesSynced(this StartOfRound @this, in bool value)
    {
        _setterStartOfRoundUnlockablesSynced ??=
            (Func<StartOfRound, bool, bool>)StartOfRoundUnlockablesSynced.FastSetter();
        return _setterStartOfRoundUnlockablesSynced(@this, value);
    }

    //ShotgunItem.AdditionalNetworking_dirtyAmmo
    private static readonly FieldInfo ShotgunItemDirtyAmmo = typeof(ShotgunItem)
        .GetField("AdditionalNetworking_dirtyAmmo", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<ShotgunItem, bool> _getterShotgunItemDirtyAmmo;
    private static Func<ShotgunItem, bool, bool> _setterShotgunItemDirtyAmmo;

    public static bool GetDirtyAmmo(this ShotgunItem @this)
    {
        _getterShotgunItemDirtyAmmo ??= (Func<ShotgunItem, bool>)ShotgunItemDirtyAmmo.FastGetter();
        return _getterShotgunItemDirtyAmmo(@this);
    }

    public static bool SetDirtyAmmo(this ShotgunItem @this, in bool value)
    {
        _setterShotgunItemDirtyAmmo ??= (Func<ShotgunItem, bool, bool>)ShotgunItemDirtyAmmo.FastSetter();
        return _setterShotgunItemDirtyAmmo(@this, value);
    }

    //ShotgunItem.AdditionalNetworking_dirtySafety
    private static readonly FieldInfo ShotgunItemDirtySafety = typeof(ShotgunItem)
        .GetField("AdditionalNetworking_dirtySafety", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<ShotgunItem, bool> _getterShotgunItemDirtySafety;
    private static Func<ShotgunItem, bool, bool> _setterShotgunItemDirtySafety;

    public static bool GetDirtySafety(this ShotgunItem @this)
    {
        _getterShotgunItemDirtySafety ??= (Func<ShotgunItem, bool>)ShotgunItemDirtySafety.FastGetter();
        return _getterShotgunItemDirtySafety(@this);
    }

    public static bool SetDirtySafety(this ShotgunItem @this, in bool value)
    {
        _setterShotgunItemDirtySafety ??= (Func<ShotgunItem, bool, bool>)ShotgunItemDirtySafety.FastSetter();
        return _setterShotgunItemDirtySafety(@this, value);
    }

    //BoomBoxItem.AdditionalNetworking_dirtyStatus
    private static readonly FieldInfo BoomboxItemDirtyStatus = typeof(BoomboxItem)
        .GetField("AdditionalNetworking_dirtyStatus", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<BoomboxItem, bool> _getterBoomboxItemDirtyStatus;
    private static Func<BoomboxItem, bool, bool> _setterBoomboxItemDirtyStatus;

    public static bool GetDirtyStatus(this BoomboxItem @this)
    {
        _getterBoomboxItemDirtyStatus ??= (Func<BoomboxItem, bool>)BoomboxItemDirtyStatus.FastGetter();
        return _getterBoomboxItemDirtyStatus(@this);
    }

    public static bool SetDirtyStatus(this BoomboxItem @this, in bool value)
    {
        _setterBoomboxItemDirtyStatus ??= (Func<BoomboxItem, bool, bool>)BoomboxItemDirtyStatus.FastSetter();
        return _setterBoomboxItemDirtyStatus(@this, value);
    }

    //AnimatedItem.AdditionalNetworking_dirtyStatus
    private static readonly FieldInfo AnimatedItemDirtyStatus = typeof(AnimatedItem)
        .GetField("AdditionalNetworking_dirtyStatus", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Func<AnimatedItem, bool> _getterAnimatedItemDirtyStatus;
    private static Func<AnimatedItem, bool, bool> _setterAnimatedItemDirtyStatus;

    public static bool GetDirtyStatus(this AnimatedItem @this)
    {
        _getterAnimatedItemDirtyStatus ??= (Func<AnimatedItem, bool>)AnimatedItemDirtyStatus.FastGetter();
        return _getterAnimatedItemDirtyStatus(@this);
    }

    public static bool SetDirtyStatus(this AnimatedItem @this, in bool value)
    {
        _setterAnimatedItemDirtyStatus ??= (Func<AnimatedItem, bool, bool>)AnimatedItemDirtyStatus.FastSetter();
        return _setterAnimatedItemDirtyStatus(@this, value);
    }
}
