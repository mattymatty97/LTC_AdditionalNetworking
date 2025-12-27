using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace AdditionalNetworking.Interfaces;

[InjectInterface(typeof(GrabbableObject))]
public interface INetworkGrabbableObject
{
    bool AdditionalNetworking_IsInitialized { get; set; }
    bool AdditionalNetworking_RequestedSync { get; set; }
}