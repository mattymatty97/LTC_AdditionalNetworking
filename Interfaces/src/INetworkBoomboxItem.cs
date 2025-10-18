using AdditionalNetworking.Preloader.Utils;

namespace AdditionalNetworking.Interfaces;

[InjectInterface(nameof(BoomboxItem))]
public interface INetworkBoomboxItem
{
    bool AdditionalNetworking_Changed { get; set; }
}