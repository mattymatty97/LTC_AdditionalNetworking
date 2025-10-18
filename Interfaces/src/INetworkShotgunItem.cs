using AdditionalNetworking.Preloader.Utils;

namespace AdditionalNetworking.Interfaces;

[InjectInterface(nameof(ShotgunItem))]
public interface INetworkShotgunItem
{
    bool AdditionalNetworking_AmmoCountChanged { get; set; }
    bool AdditionalNetworking_SafetyChanged { get; set; }
}