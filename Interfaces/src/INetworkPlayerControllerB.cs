using AdditionalNetworking.Preloader.Utils;
using GameNetcodeStuff;

namespace AdditionalNetworking.Interfaces;

[InjectInterface(nameof(PlayerControllerB))]
public interface INetworkPlayerControllerB
{
    bool AdditionalNetworking_InventoryChanged { get; set; }
    bool AdditionalNetworking_SlotChanged { get; set; }
    bool AdditionalNetworking_LastCrouchState { get; set; }
}