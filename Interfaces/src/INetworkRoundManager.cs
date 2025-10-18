using AdditionalNetworking.Preloader.Utils;

namespace AdditionalNetworking.Interfaces;

[InjectInterface(nameof(RoundManager))]
public interface INetworkRoundManager
{
    bool AdditionalNetworking_ScrapPendingSync { get; set; }
}