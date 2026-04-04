using GameNetcodeStuff;
using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace AdditionalNetworking.Interfaces;

[InjectInterface(typeof(PlayerControllerB))]
public interface INetworkPlayerControllerB
{
    bool AdditionalNetworking_InventoryChanged { get; set; }
    bool AdditionalNetworking_SlotChanged { get; set; }
}
