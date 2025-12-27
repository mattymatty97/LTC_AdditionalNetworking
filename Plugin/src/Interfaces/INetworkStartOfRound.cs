using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace AdditionalNetworking.Interfaces;

[InjectInterface(typeof(StartOfRound))]
public interface INetworkStartOfRound
{
    bool AdditionalNetworking_ValuablesSynced { get; set; }
}