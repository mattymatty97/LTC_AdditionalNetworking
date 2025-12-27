using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace AdditionalNetworking.Interfaces;

[InjectInterface(typeof(ShotgunItem))]
public interface INetworkShotgunItem
{
    bool AdditionalNetworking_AmmoCountChanged { get; set; }
    bool AdditionalNetworking_SafetyChanged { get; set; }
}