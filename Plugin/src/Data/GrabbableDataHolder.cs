using Unity.Netcode;

namespace AdditionalNetworking.Utils;

// Your existing struct definition
public record struct GrabbableDataHolder(NetworkObjectReference Reference, int ScrapValue, int DataValue)
{
    public GrabbableDataHolder(GrabbableObject grabbable) : this(grabbable.NetworkObject, grabbable.scrapValue,
        grabbable.itemProperties.saveItemVariable ? grabbable.GetItemDataToSave() : 0)
    {
    }

    public void Deconstruct(out NetworkObjectReference reference, out int scrapValue, out int dataValue)
    {
        reference = Reference;
        scrapValue = ScrapValue;
        dataValue = DataValue;
    }
}

// Extension method to write a GrabbableDataHolder to a buffer
public static class GrabbableDataHolderSerializer
{
    public static int GrabbableDataHolderSize => FastBufferWriter.GetWriteSize<NetworkObjectReference>() +
                                                 (FastBufferWriter.GetWriteSize<int>() * 2);

    // Write a single GrabbableDataHolder to a stream
    public static void WriteGrabbableDataHolder(this FastBufferWriter writer, GrabbableDataHolder data)
    {
        writer.WriteNetworkSerializable(data.Reference);
        writer.WriteValueSafe(data.ScrapValue);
        writer.WriteValueSafe(data.DataValue);
    }

    // Write an array of GrabbableDataHolder to a stream
    public static void WriteGrabbableDataHolderArray(this FastBufferWriter writer, GrabbableDataHolder[] dataArray)
    {
        // Write the length first
        writer.WriteValueSafe(dataArray.Length);

        // Write each GrabbableDataHolder
        foreach (var data in dataArray)
        {
            writer.WriteGrabbableDataHolder(data);
        }
    }

    // Read a single GrabbableDataHolder from a stream
    public static void ReadGrabbableDataHolder(this FastBufferReader reader, out GrabbableDataHolder data)
    {
        reader.ReadNetworkSerializable(out NetworkObjectReference reference);

        reader.ReadValueSafe(out int scrapValue);

        reader.ReadValueSafe(out int dataValue);

        data = new GrabbableDataHolder(reference, scrapValue, dataValue);
    }

    // Read an array of GrabbableDataHolder from a stream
    public static void ReadGrabbableDataHolderArray(this FastBufferReader reader, out GrabbableDataHolder[] dataArray)
    {
        // Read the length first
        reader.ReadValueSafe(out int length);

        // Create the array
        dataArray = new GrabbableDataHolder[length];

        // Read each GrabbableDataHolder
        for (var i = 0; i < length; i++)
        {
            reader.ReadGrabbableDataHolder(out dataArray[i]);
        }
    }
}
