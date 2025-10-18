using AdditionalNetworking.Preloader.Utils;

namespace AdditionalNetworking.Interfaces;

[InjectInterface(nameof(AnimatedItem))]
public interface INetworkAnimatedItem
{
    bool AdditionalNetworking_Changed { get; set; }
}