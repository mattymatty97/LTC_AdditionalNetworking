using Unity.Netcode.Components;

namespace AdditionalNetworking.Components;

public class GrabbableNetworkTransform: NetworkTransform
{
    public override void Awake()
    {
        base.Awake();
        SyncScaleX = false;
        SyncScaleY = false;
        SyncScaleZ = false;
        SyncPositionX = false;
        SyncPositionY = false;
        SyncPositionZ = false;
        UseHalfFloatPrecision = true;
        InLocalSpace = false;
        
    }

    public override bool OnIsServerAuthoritative()
    {
        return false;
    }
}