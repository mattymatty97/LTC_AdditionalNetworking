using Unity.Netcode.Components;

namespace AdditionalNetworking.Components;

public class ClientNetworkTransform: NetworkTransform
{
    public override void Awake()
    {
        base.Awake();
        SyncScaleX = false;
        SyncScaleY = false;
        SyncScaleZ = false;
        UseHalfFloatPrecision = true;
        InLocalSpace = true;
    }

    public override bool OnIsServerAuthoritative()
    {
        return false;
    }
}