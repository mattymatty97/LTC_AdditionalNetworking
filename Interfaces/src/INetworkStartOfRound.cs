using AdditionalNetworking.Preloader.Utils;

namespace AdditionalNetworking.Interfaces;

[InjectInterface(nameof(StartOfRound))]
public interface INetworkStartOfRound
{
    bool AdditionalNetworking_ValuablesSynced { get; set; }
}