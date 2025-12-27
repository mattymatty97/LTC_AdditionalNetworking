using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace AdditionalNetworking.Interfaces;

[InjectInterface(typeof(RoundManager))]
public interface INetworkRoundManager
{
    bool AdditionalNetworking_ScrapPendingSync { get; set; }
}