using AdditionalNetworking.Preloader.Utils;

namespace AdditionalNetworking.Interfaces;

[InjectInterface(nameof(GrabbableObject))]
public interface INetworkGrabbableObject
{
    bool AdditionalNetworking_IsInitialized { get; set; }
    bool AdditionalNetworking_RequestedSync { get; set; }
}