using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace AdditionalNetworking.Interfaces;

[InjectInterface(typeof(BoomboxItem))]
public interface INetworkBoomboxItem
{
    bool AdditionalNetworking_Changed { get; set; }
}