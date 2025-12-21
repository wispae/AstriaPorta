namespace AstriaPorta.Content;

public interface IStargateStateManager
{
    public EnumStargateState State { get; }

    public void ProcessStatePacket(EnumStargatePacketType packetType, byte[] data);
}