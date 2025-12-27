using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace AdditionalNetworking.Interfaces;

[InjectInterface(typeof(AnimatedItem))]
public interface INetworkAnimatedItem
{
    bool AdditionalNetworking_Changed { get; set; }
}